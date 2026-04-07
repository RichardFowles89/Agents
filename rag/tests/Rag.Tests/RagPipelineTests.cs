using Rag.Core.Contracts;
using Rag.Core.Models;
using Rag.Core.Pipeline;

namespace Rag.Tests;

public class RagPipelineTests
{
    [Fact]
    public async Task AskAsync_ReturnsAnswer_WhenPlannerAndSafetyApprove()
    {
        FakeRetriever retriever = new(new[]
        {
            new RetrievalHit("c1", "doc-1", "Guide", "Intro", "Relevant context", null, 0.9)
        });

        FakePlanner planner = new(PlannerDecision.Answerable);
        FakeAnswerAgent answerer = new(new AnswerDraft("Answer from context", new[]
        {
            new Citation("doc-1", "Guide", "Intro", null)
        }));
        FakeReranker reranker = new();
        FakeQueryRewriteAgent rewriter = new(question => question);
        FakeSafetyReviewer safety = new(new SafetyReviewResult(true, "ok"));

        RagPipeline pipeline = new(retriever, reranker, rewriter, planner, answerer, safety);
        AskResponse response = await pipeline.AskAsync(new AskRequest("What is this?", 3));

        Assert.True(response.Answered);
        Assert.Equal("Answer from context", response.AnswerText);
        Assert.Single(response.Citations);
        Assert.Null(response.RefusalReason);
    }

    [Fact]
    public async Task AskAsync_Refuses_WhenPlannerRefuses()
    {
        FakeRetriever retriever = new([]);
        FakePlanner planner = new(PlannerDecision.Refuse);
        FakeAnswerAgent answerer = new(new AnswerDraft("Should not run", []));
        FakeReranker reranker = new();
        FakeQueryRewriteAgent rewriter = new(question => "rewritten query");
        FakeSafetyReviewer safety = new(new SafetyReviewResult(true, "ok"));

        RagPipeline pipeline = new(retriever, reranker, rewriter, planner, answerer, safety);
        AskResponse response = await pipeline.AskAsync(new AskRequest("Unsafe question", 3));

        Assert.False(response.Answered);
        Assert.Contains("Planner refused", response.RefusalReason);
        Assert.Equal(0, answerer.CallCount);
        Assert.Equal(0, safety.CallCount);
        Assert.NotNull(response.Diagnostics);
        Assert.True(response.Diagnostics.RetryTriggered);
        Assert.Equal(2, response.Diagnostics.AttemptsUsed);
        Assert.Equal("rewritten query", response.Diagnostics.RewrittenQuery);
    }

    [Fact]
    public async Task AskAsync_Refuses_WhenSafetyRejectsDraft()
    {
        FakeRetriever retriever = new(new[]
        {
            new RetrievalHit("c1", "doc-1", "Guide", "Intro", "Context", null, 0.9)
        });
        FakePlanner planner = new(PlannerDecision.Answerable);
        FakeAnswerAgent answerer = new(new AnswerDraft("Draft", []));
        FakeReranker reranker = new();
        FakeQueryRewriteAgent rewriter = new(question => question);
        FakeSafetyReviewer safety = new(new SafetyReviewResult(false, "Missing citations"));

        RagPipeline pipeline = new(retriever, reranker, rewriter, planner, answerer, safety);
        AskResponse response = await pipeline.AskAsync(new AskRequest("Question", 3));

        Assert.False(response.Answered);
        Assert.Equal("Missing citations", response.RefusalReason);
        Assert.Equal(1, answerer.CallCount);
        Assert.Equal(1, safety.CallCount);
        Assert.NotNull(response.Diagnostics);
        Assert.Equal(1, response.Diagnostics.AttemptsUsed);
    }

    [Fact]
    public async Task AskAsync_HonorsMaxAgentRetries_WhenPlannerKeepsRefusing()
    {
        FakeRetriever retriever = new([]);
        FakePlanner planner = new(PlannerDecision.Refuse);
        FakeAnswerAgent answerer = new(new AnswerDraft("Should not run", []));
        FakeReranker reranker = new();
        int rewriteCount = 0;
        FakeQueryRewriteAgent rewriter = new(_ => $"rewrite-{++rewriteCount}");
        FakeSafetyReviewer safety = new(new SafetyReviewResult(true, "ok"));

        RagPipeline pipeline = new(retriever, reranker, rewriter, planner, answerer, safety);
        AskResponse response = await pipeline.AskAsync(new AskRequest("Question", Top: 3, MaxAgentRetries: 2));

        Assert.False(response.Answered);
        Assert.NotNull(response.Diagnostics);
        Assert.Equal(3, response.Diagnostics.AttemptsUsed);
        Assert.Equal(2, response.Diagnostics.MaxRetries);
        Assert.True(response.Diagnostics.RetryTriggered);
    }

    [Fact]
    public async Task AskAsync_AppliesReranking_WhenCandidatesExceedTop()
    {
        FakeRetriever retriever = new(new[]
        {
            new RetrievalHit("c1", "doc-1", "Guide", "Intro", "Context A", null, 0.9),
            new RetrievalHit("c2", "doc-2", "Guide", "Intro", "Context B", null, 0.8),
            new RetrievalHit("c3", "doc-3", "Guide", "Intro", "Context C", null, 0.7),
            new RetrievalHit("c4", "doc-4", "Guide", "Intro", "Context D", null, 0.6),
            new RetrievalHit("c5", "doc-5", "Guide", "Intro", "Context E", null, 0.5),
            new RetrievalHit("c6", "doc-6", "Guide", "Intro", "Context F", null, 0.4)
        });
        FakeReranker reranker = new(
            (question, candidates, top) =>
                Task.FromResult<IReadOnlyList<RetrievalHit>>([candidates[4], candidates[2], candidates[0]]));
        FakePlanner planner = new(PlannerDecision.Answerable);
        FakeAnswerAgent answerer = new(new AnswerDraft("Answer", []));
        FakeQueryRewriteAgent rewriter = new(question => question);
        FakeSafetyReviewer safety = new(new SafetyReviewResult(true, "ok"));

        RagPipeline pipeline = new(retriever, reranker, rewriter, planner, answerer, safety);
        AskResponse response = await pipeline.AskAsync(new AskRequest("Question", Top: 3, MaxAgentRetries: 0));

        Assert.True(response.Answered);
        Assert.NotNull(response.Diagnostics);
        AskAttemptTrace attempt = Assert.Single(response.Diagnostics.Attempts);
        Assert.Equal(6, attempt.HitCount);
        Assert.Equal(3, attempt.RerankedHitCount);
        Assert.True(attempt.RerankApplied);
        Assert.Equal(1, reranker.CallCount);
    }

    [Fact]
    public async Task AskAsync_FallsBack_WhenRerankerThrows()
    {
        FakeRetriever retriever = new(new[]
        {
            new RetrievalHit("c1", "doc-1", "Guide", "Intro", "Context A", null, 0.9),
            new RetrievalHit("c2", "doc-2", "Guide", "Intro", "Context B", null, 0.8),
            new RetrievalHit("c3", "doc-3", "Guide", "Intro", "Context C", null, 0.7),
            new RetrievalHit("c4", "doc-4", "Guide", "Intro", "Context D", null, 0.6),
            new RetrievalHit("c5", "doc-5", "Guide", "Intro", "Context E", null, 0.5),
            new RetrievalHit("c6", "doc-6", "Guide", "Intro", "Context F", null, 0.4)
        });
        FakeReranker reranker = new((_, _, _) => throw new InvalidOperationException("rerank failed"));
        FakePlanner planner = new(PlannerDecision.Answerable);
        FakeAnswerAgent answerer = new(new AnswerDraft("Answer", []));
        FakeQueryRewriteAgent rewriter = new(question => question);
        FakeSafetyReviewer safety = new(new SafetyReviewResult(true, "ok"));

        RagPipeline pipeline = new(retriever, reranker, rewriter, planner, answerer, safety);
        AskResponse response = await pipeline.AskAsync(new AskRequest("Question", Top: 3, MaxAgentRetries: 0));

        Assert.True(response.Answered);
        Assert.NotNull(response.Diagnostics);
        AskAttemptTrace attempt = Assert.Single(response.Diagnostics.Attempts);
        Assert.Equal(6, attempt.HitCount);
        Assert.Equal(3, attempt.RerankedHitCount);
        Assert.True(attempt.RerankApplied);
        Assert.Equal(1, reranker.CallCount);
    }

    private sealed class FakeRetriever : ISearchRetriever
    {
        private readonly IReadOnlyList<RetrievalHit> _hits;

        public FakeRetriever(IReadOnlyList<RetrievalHit> hits)
        {
            _hits = hits;
        }

        public Task<IReadOnlyList<RetrievalHit>> RetrieveAsync(string question, int top, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_hits.Take(top).ToList() as IReadOnlyList<RetrievalHit>);
        }

        public Task IngestAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlanner : IPlannerAgent
    {
        private readonly PlannerDecision _decision;

        public FakePlanner(PlannerDecision decision)
        {
            _decision = decision;
        }

        public Task<PlannerDecision> AssessAsync(string question, IReadOnlyList<RetrievalHit> retrievalHits, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_decision);
        }
    }

    private sealed class FakeReranker : IRetrievalReranker
    {
        private readonly Func<string, IReadOnlyList<RetrievalHit>, int, Task<IReadOnlyList<RetrievalHit>>> _rerank;

        public int CallCount { get; private set; }

        public FakeReranker()
            : this((_, candidates, top) => Task.FromResult(candidates.Take(top).ToList() as IReadOnlyList<RetrievalHit>))
        {
        }

        public FakeReranker(Func<string, IReadOnlyList<RetrievalHit>, int, Task<IReadOnlyList<RetrievalHit>>> rerank)
        {
            _rerank = rerank;
        }

        public Task<IReadOnlyList<RetrievalHit>> RerankAsync(
            string question,
            IReadOnlyList<RetrievalHit> candidates,
            int top,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return _rerank(question, candidates, top);
        }
    }

    private sealed class FakeAnswerAgent : IAnswerAgent
    {
        private readonly AnswerDraft _draft;
        public int CallCount { get; private set; }

        public FakeAnswerAgent(AnswerDraft draft)
        {
            _draft = draft;
        }

        public Task<AnswerDraft> GenerateAsync(string question, IReadOnlyList<RetrievalHit> approvedHits, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_draft);
        }
    }

    private sealed class FakeQueryRewriteAgent : IQueryRewriteAgent
    {
        private readonly Func<string, string> _rewrite;

        public FakeQueryRewriteAgent(Func<string, string> rewrite)
        {
            _rewrite = rewrite;
        }

        public Task<string> RewriteForRetrievalAsync(
            string question,
            PlannerDecision plannerDecision,
            IReadOnlyList<RetrievalHit> retrievalHits,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_rewrite(question));
        }
    }

    private sealed class FakeSafetyReviewer : ISafetyReviewerAgent
    {
        private readonly SafetyReviewResult _result;
        public int CallCount { get; private set; }

        public FakeSafetyReviewer(SafetyReviewResult result)
        {
            _result = result;
        }

        public Task<SafetyReviewResult> ReviewAsync(string question, AnswerDraft draft, IReadOnlyList<RetrievalHit> approvedHits, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }
}
