namespace Rag.Core.Models;

public sealed record DocumentChunk(
    string Id,
    string SourceId,
    string Title,
    string SectionPath,
    string ChunkText,
    string? SourceUrl,
    IReadOnlyList<string> Tags
);
