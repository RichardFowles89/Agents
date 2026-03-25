using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Hosting;

namespace FunctionsApp.Search;

/// <summary>
/// Ensures the configured Azure AI Search index exists during host startup.
/// </summary>
public sealed class AzureSearchIndexBootstrapper : IHostedService
{
    private readonly SearchIndexClient _indexClient;
    private readonly string _indexName;

    public AzureSearchIndexBootstrapper(SearchIndexClient indexClient, string indexName)
    {
        _indexClient = indexClient;
        _indexName = indexName;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        bool exists = await _indexClient.GetIndexNamesAsync(cancellationToken)
            .AnyAsync(name => string.Equals(name, _indexName, StringComparison.OrdinalIgnoreCase), cancellationToken);

        if (exists)
        {
            return;
        }

        var fields = new List<SearchField>
        {
            new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true },
            new SimpleField("sourceId", SearchFieldDataType.String) { IsFilterable = true },
            new SearchableField("title") { IsFilterable = false, IsSortable = false, IsFacetable = false },
            new SearchableField("sectionPath") { IsFilterable = false, IsSortable = false, IsFacetable = false },
            new SearchableField("chunkText") { IsFilterable = false, IsSortable = false, IsFacetable = false },
            new SimpleField("sourceUrl", SearchFieldDataType.String)
        };

        SearchIndex index = new(_indexName, fields);
        await _indexClient.CreateIndexAsync(index, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}