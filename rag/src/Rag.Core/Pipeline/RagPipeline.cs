using Rag.Core.Contracts;
using Rag.Core.Models;

namespace Rag.Core.Pipeline;

public sealed class RagPipeline : IRagPipeline
{
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

        (IReadOnlyList<RetrievalHit> hits, PlannerDecision decision) = await RetrieveAndAssessAsync(
            request.Question,
            request.Question,
            request.Top,
            cancellationToken);

        if (decision != PlannerDecision.Answerable)
        {
            string rewrittenQuery = await _queryRewriteAgent.RewriteForRetrievalAsync(
                request.Question,
                decision,
                hits,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(rewrittenQuery) &&
                !string.Equals(rewrittenQuery, request.Question, StringComparison.OrdinalIgnoreCase))
            {
                (hits, decision) = await RetrieveAndAssessAsync(
                    request.Question,
                    rewrittenQuery,
                    request.Top,
                    cancellationToken);
            }
        }

        if (decision == PlannerDecision.Refuse)
        {
            return new AskResponse(false, string.Empty, [], "Planner refused to answer with current context.");
        }

        if (decision == PlannerDecision.NeedsMoreContext)
        {
            return new AskResponse(false, string.Empty, [], "Planner requested more context before answering.");
        }

        return await GenerateReviewedResponseAsync(request.Question, hits, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        AnswerDraft draft = await _answerAgent.GenerateAsync(question, hits, cancellationToken);
        SafetyReviewResult review = await _safetyReviewer.ReviewAsync(question, draft, hits, cancellationToken);

        if (!review.IsApproved)
        {
            return new AskResponse(false, string.Empty, [], review.Reason);
        }

        return new AskResponse(true, draft.AnswerText, draft.Citations, null);
    }
}
