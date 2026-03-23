using Rag.Core.Contracts;
using Rag.Core.Models;

namespace FunctionsApp.Agents;

internal sealed class StubSafetyReviewerAgent : ISafetyReviewerAgent
{
    public Task<SafetyReviewResult> ReviewAsync(
        string question,
        AnswerDraft draft,
        IReadOnlyList<RetrievalHit> approvedHits,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SafetyReviewResult(true, "Approved by stub reviewer."));
    }
}
