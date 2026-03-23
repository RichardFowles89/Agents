using Rag.Core.Contracts;
using Rag.Core.Models;

namespace Rag.Core.Pipeline;

public sealed class RagPipeline : IRagPipeline
{
    private readonly ISearchRetriever _retriever;
    private readonly IPlannerAgent _planner;
    private readonly IAnswerAgent _answerAgent;
    private readonly ISafetyReviewerAgent _safetyReviewer;

    public RagPipeline(
        ISearchRetriever retriever,
        IPlannerAgent planner,
        IAnswerAgent answerAgent,
        ISafetyReviewerAgent safetyReviewer)
    {
        _retriever = retriever;
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

        IReadOnlyList<RetrievalHit> hits = await _retriever.RetrieveAsync(request.Question, request.Top, cancellationToken);
        PlannerDecision decision = await _planner.AssessAsync(request.Question, hits, cancellationToken);

        if (decision == PlannerDecision.Refuse)
        {
            return new AskResponse(false, string.Empty, [], "Planner refused to answer with current context.");
        }

        if (decision == PlannerDecision.NeedsMoreContext)
        {
            return new AskResponse(false, string.Empty, [], "Planner requested more context before answering.");
        }

        AnswerDraft draft = await _answerAgent.GenerateAsync(request.Question, hits, cancellationToken);
        SafetyReviewResult review = await _safetyReviewer.ReviewAsync(request.Question, draft, hits, cancellationToken);

        if (!review.IsApproved)
        {
            return new AskResponse(false, string.Empty, [], review.Reason);
        }

        return new AskResponse(true, draft.AnswerText, draft.Citations, null);
    }
}
