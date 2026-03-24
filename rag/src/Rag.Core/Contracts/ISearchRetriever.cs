using Rag.Core.Models;

namespace Rag.Core.Contracts;

public interface ISearchRetriever
{
    Task<IReadOnlyList<RetrievalHit>> RetrieveAsync(string question, int top, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingests documents into the search index.
    /// </summary>
    /// <param name="chunks">Collection of document chunks to add to the index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IngestAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);
}
