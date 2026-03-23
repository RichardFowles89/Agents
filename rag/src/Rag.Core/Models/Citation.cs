namespace Rag.Core.Models;

public sealed record Citation(
    string SourceId,
    string Title,
    string SectionPath,
    string? SourceUrl
);
