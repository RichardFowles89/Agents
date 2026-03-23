namespace Rag.Core.Models;

public sealed record DocumentChunkWithEmbedding(
    DocumentChunk Chunk,
    IReadOnlyList<float> Vector
);
