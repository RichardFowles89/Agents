using Rag.Core.Models;

namespace Rag.Core.Contracts;

public interface ISearchRetriever
{
    Task<IReadOnlyList<RetrievalHit>> RetrieveAsync(string question, int top, CancellationToken cancellationToken = default);
}
