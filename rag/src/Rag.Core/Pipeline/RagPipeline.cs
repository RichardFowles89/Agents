using Rag.Core.Contracts;
using Rag.Core.Models;

namespace Rag.Core.Pipeline;

public sealed class RagPipeline : IRagPipeline
{
    private const int MaxSupportedAgentRetries = 3;

    private readonly ISearchRetriever _retriever;
    private readonly IQueryRewriteAgent _queryRewriteAgent;
    private readonly IPlannerAgent _planner;
    private readonly IAnswerAgent _answerAgent;
    private readonly ISafetyReviewerAgent _safetyReviewer;

    public RagPipeline(
        ISearchRetriever retriever,
        IQueryRewriteAgent queryRewriteAgent,
        IPlannerAgent planner,
        IAnswerAgent answerAgent,
        ISafetyReviewerAgent safetyReviewer)
    {
        _retriever = retriever;
        _queryRewriteAgent = queryRewriteAgent;
        _planner = planner;
        _answerAgent = answerAgent;
        _safetyReviewer = safetyReviewer;
    }

    public async Task<AskResponse> AskAsync(AskRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return new AskResponse(false, string.Empty, [], "Question is required.");
        }

        int maxRetries = Math.Clamp(request.MaxAgentRetries, 0, MaxSupportedAgentRetries);
        List<AskAttemptTrace> attemptTraces = new();
        string retrievalQuery = request.Question;
        string? rewrittenQueryUsed = null;
        bool retryTriggered = false;
        bool hasPreviousHits = false;
        IReadOnlyList<RetrievalHit> previousHits = [];

        (IReadOnlyList<RetrievalHit> hits, PlannerDecision decision) = ([], PlannerDecision.Refuse);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            (hits, decision) = await RetrieveAndAssessAsync(
                request.Question,
                retrievalQuery,
                request.Top,
                cancellationToken);

            bool retrievalImproved = !hasPreviousHits || HasRetrievalImproved(previousHits, hits);

            attemptTraces.Add(new AskAttemptTrace(
                attempt + 1,
                retrievalQuery,
                hits.Count,
                decision,
                QueryRewritten: attempt > 0,
                RetrievalImproved: retrievalImproved));

            if (decision == PlannerDecision.Answerable || attempt == maxRetries)
            {
                break;
            }

            string rewrittenQuery = await _queryRewriteAgent.RewriteForRetrievalAsync(
                request.Question,
                decision,
                hits,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(rewrittenQuery) ||
                string.Equals(rewrittenQuery, retrievalQuery, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            retryTriggered = true;
            rewrittenQueryUsed = rewrittenQuery;
            previousHits = hits;
            hasPreviousHits = true;
            retrievalQuery = rewrittenQuery;
        }

        AskDiagnostics diagnostics = new(
            AttemptsUsed: attemptTraces.Count,
            MaxRetries: maxRetries,
            RetryTriggered: retryTriggered,
            RewrittenQuery: rewrittenQueryUsed,
            Attempts: attemptTraces);

        if (decision == PlannerDecision.Refuse)
        {
            return new AskResponse(false, string.Empty, [], "Planner refused to answer with current context.", diagnostics);
        }

        if (decision == PlannerDecision.NeedsMoreContext)
        {
            return new AskResponse(false, string.Empty, [], "Planner requested more context before answering.", diagnostics);
        }

        return await GenerateReviewedResponseAsync(request.Question, hits, diagnostics, cancellationToken);
    }

    private async Task<(IReadOnlyList<RetrievalHit> Hits, PlannerDecision Decision)> RetrieveAndAssessAsync(
        string question,
        string retrievalQuery,
        int top,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RetrievalHit> hits = await _retriever.RetrieveAsync(retrievalQuery, top, cancellationToken);
        PlannerDecision decision = await _planner.AssessAsync(question, hits, cancellationToken);
        return (hits, decision);
    }

    private async Task<AskResponse> GenerateReviewedResponseAsync(
        string question,
        IReadOnlyList<RetrievalHit> hits,
        AskDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        AnswerDraft draft = await _answerAgent.GenerateAsync(question, hits, cancellationToken);
        SafetyReviewResult review = await _safetyReviewer.ReviewAsync(question, draft, hits, cancellationToken);

        if (!review.IsApproved)
        {
            return new AskResponse(false, string.Empty, [], review.Reason, diagnostics);
        }

        return new AskResponse(true, draft.AnswerText, draft.Citations, null, diagnostics);
    }

    private static bool HasRetrievalImproved(IReadOnlyList<RetrievalHit> previousHits, IReadOnlyList<RetrievalHit> currentHits)
    {
        if (currentHits.Count == 0)
        {
            return false;
        }

        if (previousHits.Count == 0)
        {
            return true;
        }

        HashSet<string> previousIds = previousHits.Select(hit => hit.ChunkId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        int overlap = currentHits.Count(hit => previousIds.Contains(hit.ChunkId));
        return overlap < currentHits.Count;
    }
}
