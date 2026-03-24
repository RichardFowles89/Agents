namespace Rag.Core.Models;

/// <summary>
/// Request to ingest documents into the search index.
/// </summary>
public sealed record IngestRequest(
    IReadOnlyList<SourceDocument> Documents
);
