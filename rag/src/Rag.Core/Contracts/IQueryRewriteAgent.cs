using Rag.Core.Models;

namespace Rag.Core.Contracts;

public interface IQueryRewriteAgent
{
    Task<string> RewriteForRetrievalAsync(
        string question,
        PlannerDecision plannerDecision,
        IReadOnlyList<RetrievalHit> retrievalHits,
        CancellationToken cancellationToken = default);
}