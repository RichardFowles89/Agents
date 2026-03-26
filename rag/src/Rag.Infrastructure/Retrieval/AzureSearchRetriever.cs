using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Rag.Core.Contracts;
using Rag.Core.Models;

namespace Rag.Infrastructure.Retrieval;

/// <summary>
/// Azure AI Search implementation for retrieval and ingestion.
/// </summary>
public sealed class AzureSearchRetriever : ISearchRetriever
{
    private readonly SearchClient _searchClient;

    public AzureSearchRetriever(SearchClient searchClient)
    {
        _searchClient = searchClient;
    }

    public async Task<IReadOnlyList<RetrievalHit>> RetrieveAsync(string question, int top, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question) || top <= 0)
        {
            return [];
        }

        var options = new SearchOptions
        {
            Size = top,
            QueryType = SearchQueryType.Simple,
            SearchMode = SearchMode.Any,
            IncludeTotalCount = false
        };

        options.Select.Add("id");
        options.Select.Add("sourceId");
        options.Select.Add("title");
        options.Select.Add("sectionPath");
        options.Select.Add("chunkText");
        options.Select.Add("sourceUrl");

        SearchResults<SearchDocument> results = await _searchClient.SearchAsync<SearchDocument>(
            question,
            options,
            cancellationToken);

        var hits = new List<RetrievalHit>();

        await foreach (SearchResult<SearchDocument> result in results.GetResultsAsync().WithCancellation(cancellationToken))
        {
            SearchDocument document = result.Document;

            string id = GetString(document, "id");
            string sourceId = GetString(document, "sourceId");
            string title = GetString(document, "title");
            string sectionPath = GetString(document, "sectionPath");
            string chunkText = GetString(document, "chunkText");
            string sourceUrl = GetString(document, "sourceUrl");
            double score = result.Score ?? 0;

            hits.Add(new RetrievalHit(id, sourceId, title, sectionPath, chunkText, sourceUrl, score));
        }

        return hits;
    }

    public async Task IngestAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks is null || chunks.Count == 0)
        {
            return;
        }

        var batch = IndexDocumentsBatch.MergeOrUpload(
            chunks.Select(chunk =>
                new SearchDocument
                {
                    ["id"] = chunk.Id,
                    ["sourceId"] = chunk.SourceId,
                    ["title"] = chunk.Title,
                    ["sectionPath"] = chunk.SectionPath,
                    ["chunkText"] = chunk.ChunkText,
                    ["sourceUrl"] = chunk.SourceUrl
                })
            .ToList());

        await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
    }

    private static string GetString(SearchDocument document, string key)
    {
        if (document.TryGetValue(key, out object? value) && value is not null)
        {
            return value.ToString() ?? string.Empty;
        }

        return string.Empty;
    }
}
