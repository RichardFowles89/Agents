using Rag.Core.Models;

namespace Rag.Core.Contracts;

public interface ISearchIndexer
{
    Task IndexChunksAsync(IReadOnlyList<DocumentChunkWithEmbedding> chunks, CancellationToken cancellationToken = default);
}
