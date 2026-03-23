using Rag.Core.Models;

namespace Rag.Core.Contracts;

public interface IPlannerAgent
{
    Task<PlannerDecision> AssessAsync(string question, IReadOnlyList<RetrievalHit> retrievalHits, CancellationToken cancellationToken = default);
}
