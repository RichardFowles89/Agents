using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Rag.Core.Contracts;
using Rag.Core.Models;

namespace FunctionsApp.Functions;

/// <summary>
/// HTTP trigger function to ingest documents into the search index.
/// Accepts a list of documents, chunks them, generates embeddings, and indexes them.
/// </summary>
public sealed class IngestFunction
{
    private readonly IDocumentChunker _chunker;
    private readonly IEmbeddingService _embeddingService;
    private readonly ISearchIndexer _indexer;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly ChunkingOptions DefaultChunkingOptions = new(
        MaxChunkCharacters: 1024,
        OverlapCharacters: 100
    );

    public IngestFunction(IDocumentChunker chunker, IEmbeddingService embeddingService, ISearchIndexer indexer)
    {
        _chunker = chunker;
        _embeddingService = embeddingService;
        _indexer = indexer;
    }

    [Function("Ingest")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ingest")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        IngestRequest? request = await JsonSerializer.DeserializeAsync<IngestRequest>(
            req.Body, JsonOptions, cancellationToken);

        if (request is null || request.Documents.Count == 0)
        {
            HttpResponseData badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Documents array is required and must not be empty.", cancellationToken);
            return badRequest;
        }

        try
        {
            List<DocumentChunk> allChunks = new();

            foreach (SourceDocument document in request.Documents)
            {
                IReadOnlyList<DocumentChunk> chunks = _chunker.Chunk(document, DefaultChunkingOptions);
                allChunks.AddRange(chunks);
            }

            List<DocumentChunkWithEmbedding> chunksWithEmbeddings = new(allChunks.Count);
            foreach (DocumentChunk chunk in allChunks)
            {
                IReadOnlyList<float> vector = await _embeddingService.CreateEmbeddingAsync(chunk.ChunkText, cancellationToken);
                chunksWithEmbeddings.Add(new DocumentChunkWithEmbedding(chunk, vector));
            }

            await _indexer.IndexChunksAsync(chunksWithEmbeddings, cancellationToken);

            var response = new
            {
                success = true,
                documentsProcessed = request.Documents.Count,
                chunksCreated = allChunks.Count,
                message = $"Successfully ingested {request.Documents.Count} document(s) ({allChunks.Count} chunk(s))."
            };

            HttpResponseData ok = req.CreateResponse(HttpStatusCode.OK);
            ok.Headers.Add("Content-Type", "application/json");
            await ok.WriteStringAsync(
                JsonSerializer.Serialize(response, JsonOptions), cancellationToken);

            return ok;
        }
        catch (ArgumentException ex)
        {
            HttpResponseData badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync($"Invalid input: {ex.Message}", cancellationToken);
            return badRequest;
        }
        catch (Exception ex)
        {
            HttpResponseData error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync($"Error processing ingest: {ex.Message}", cancellationToken);
            return error;
        }
    }
}
