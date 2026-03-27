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
        FakeQueryRewriteAgent rewriter = new(question => question);
        FakeSafetyReviewer safety = new(new SafetyReviewResult(true, "ok"));

        RagPipeline pipeline = new(retriever, rewriter, planner, answerer, safety);
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
        FakeQueryRewriteAgent rewriter = new(question => question);
        FakeSafetyReviewer safety = new(new SafetyReviewResult(true, "ok"));

        RagPipeline pipeline = new(retriever, rewriter, planner, answerer, safety);
        AskResponse response = await pipeline.AskAsync(new AskRequest("Unsafe question", 3));

        Assert.False(response.Answered);
        Assert.Contains("Planner refused", response.RefusalReason);
        Assert.Equal(0, answerer.CallCount);
        Assert.Equal(0, safety.CallCount);
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
        FakeQueryRewriteAgent rewriter = new(question => question);
        FakeSafetyReviewer safety = new(new SafetyReviewResult(false, "Missing citations"));

        RagPipeline pipeline = new(retriever, rewriter, planner, answerer, safety);
        AskResponse response = await pipeline.AskAsync(new AskRequest("Question", 3));

        Assert.False(response.Answered);
        Assert.Equal("Missing citations", response.RefusalReason);
        Assert.Equal(1, answerer.CallCount);
        Assert.Equal(1, safety.CallCount);
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
