namespace Rag.Core.Models;

public sealed record ChunkingOptions(
    int MaxChunkCharacters = 1200,
    int OverlapCharacters = 150
);
