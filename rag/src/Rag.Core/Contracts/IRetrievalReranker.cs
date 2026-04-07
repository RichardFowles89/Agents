using Rag.Core.Models;

namespace Rag.Core.Contracts;

public interface IRetrievalReranker
{
    Task<IReadOnlyList<RetrievalHit>> RerankAsync(
        string question,
        IReadOnlyList<RetrievalHit> candidates,
        int top,
        CancellationToken cancellationToken = default);
}
