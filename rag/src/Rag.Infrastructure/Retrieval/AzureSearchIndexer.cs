using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Rag.Core.Contracts;
using Rag.Core.Models;

namespace Rag.Infrastructure.Retrieval;

/// <summary>
/// Indexes document chunks with their vector embeddings into Azure AI Search.
/// </summary>
public sealed class AzureSearchIndexer : ISearchIndexer
{
    private readonly SearchClient _searchClient;

    public AzureSearchIndexer(SearchClient searchClient)
    {
        _searchClient = searchClient;
    }

    /// <summary>
    /// Uploads a batch of chunks with embeddings to the Azure AI Search index using merge-or-upload semantics.
    /// </summary>
    public async Task IndexChunksAsync(IReadOnlyList<DocumentChunkWithEmbedding> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks is null || chunks.Count == 0)
        {
            return;
        }

        IndexDocumentsBatch<SearchDocument> batch = IndexDocumentsBatch.MergeOrUpload(
            chunks.Select(item => new SearchDocument
            {
                ["id"] = item.Chunk.Id,
                ["sourceId"] = item.Chunk.SourceId,
                ["title"] = item.Chunk.Title,
                ["sectionPath"] = item.Chunk.SectionPath,
                ["chunkText"] = item.Chunk.ChunkText,
                ["sourceUrl"] = item.Chunk.SourceUrl,
                ["embedding"] = item.Vector.ToArray()
            }));

        await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
    }
}
