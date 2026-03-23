namespace Rag.Core.Models;

public sealed record SourceDocument(
    string SourceId,
    string Title,
    string Content,
    string? SourceUrl,
    IReadOnlyList<string> Tags
);
