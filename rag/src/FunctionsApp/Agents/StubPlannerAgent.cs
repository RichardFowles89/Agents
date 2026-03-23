using Rag.Core.Contracts;
using Rag.Core.Models;

namespace FunctionsApp.Agents;

internal sealed class StubPlannerAgent : IPlannerAgent
{
    public Task<PlannerDecision> AssessAsync(
        string question,
        IReadOnlyList<RetrievalHit> retrievalHits,
        CancellationToken cancellationToken = default)
    {
        PlannerDecision decision = retrievalHits.Count > 0
            ? PlannerDecision.Answerable
            : PlannerDecision.Refuse;

        return Task.FromResult(decision);
    }
}
