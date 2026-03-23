namespace Rag.Core.Models;

public sealed record RetrievalHit(
    string ChunkId,
    string SourceId,
    string Title,
    string SectionPath,
    string ChunkText,
    string? SourceUrl,
    double Score
);
