using Rag.Core.Models;

namespace Rag.Core.Contracts;

public interface IDocumentChunker
{
    IReadOnlyList<DocumentChunk> Chunk(SourceDocument document, ChunkingOptions options);
}
