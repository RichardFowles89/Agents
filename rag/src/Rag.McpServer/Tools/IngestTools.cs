using System.Net.Http;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Rag.McpServer.Tools;

[McpServerToolType]
internal sealed class IngestTools
{
    private const string DefaultIngestEndpoint = "http://localhost:7071/api/ingest";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public IngestTools(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [McpServerTool]
    public async Task<IngestToolResult> ingest_documents(
        IReadOnlyList<IngestDocumentInput> documents,
        CancellationToken cancellationToken = default)
    {
        if (documents.Count == 0)
        {
            return new IngestToolResult(false, 0, 0, "No documents were provided.", "documents must not be empty.");
        }

        List<IngestEndpointDocument> endpointDocuments = new(documents.Count);
        foreach (IngestDocumentInput document in documents)
        {
            if (string.IsNullOrWhiteSpace(document.SourceId) && string.IsNullOrWhiteSpace(document.Id))
            {
                return new IngestToolResult(false, 0, 0, "Document validation failed.", "Each document must include sourceId or id.");
            }

            if (string.IsNullOrWhiteSpace(document.Title))
            {
                return new IngestToolResult(false, 0, 0, "Document validation failed.", "Each document must include title.");
            }

            if (string.IsNullOrWhiteSpace(document.Content))
            {
                return new IngestToolResult(false, 0, 0, "Document validation failed.", "Each document must include content.");
            }

            endpointDocuments.Add(new IngestEndpointDocument(
                Id: document.Id,
                SourceId: document.SourceId,
                Title: document.Title,
                Content: document.Content,
                SourceUrl: document.SourceUrl,
                Tags: document.Tags));
        }

        string endpoint = Environment.GetEnvironmentVariable("RAG_FUNCTIONS_INGEST_ENDPOINT") ?? DefaultIngestEndpoint;

        HttpClient client = _httpClientFactory.CreateClient();
        IngestEndpointRequest request = new(endpointDocuments);
        string jsonBody = JsonSerializer.Serialize(request, JsonOptions);

        using StringContent content = new(jsonBody, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(endpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return new IngestToolResult(
                false,
                0,
                0,
                "ingest endpoint call failed.",
                $"{(int)response.StatusCode} {response.ReasonPhrase}. {TrimForError(errorBody)}");
        }

        string rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return new IngestToolResult(false, endpointDocuments.Count, 0, "ingest endpoint returned an empty response body.");
        }

        try
        {
            IngestEndpointResponse? parsed = JsonSerializer.Deserialize<IngestEndpointResponse>(rawResponse, JsonOptions);
            if (parsed is null)
            {
                return new IngestToolResult(false, endpointDocuments.Count, 0, "ingest endpoint returned empty JSON.");
            }

            return new IngestToolResult(
                parsed.Success,
                parsed.DocumentsProcessed,
                parsed.ChunksCreated,
                parsed.Message,
                parsed.Success ? null : "ingest endpoint indicated failure.");
        }
        catch (JsonException)
        {
            return new IngestToolResult(false, endpointDocuments.Count, 0, "ingest endpoint returned non-JSON content.", TrimForError(rawResponse));
        }
    }

    private static string TrimForError(string value)
    {
        string trimmed = value.Trim();
        return trimmed.Length <= 200 ? trimmed : trimmed[..200];
    }

    public sealed record IngestDocumentInput(
        string? SourceId,
        string? Id,
        string Title,
        string Content,
        string? SourceUrl,
        IReadOnlyList<string>? Tags);

    public sealed record IngestToolResult(
        bool Success,
        int DocumentsProcessed,
        int ChunksCreated,
        string Message,
        string? Error = null);

    private sealed record IngestEndpointRequest(IReadOnlyList<IngestEndpointDocument> Documents);

    private sealed record IngestEndpointDocument(
        string? Id,
        string? SourceId,
        string Title,
        string Content,
        string? SourceUrl,
        IReadOnlyList<string>? Tags);

    private sealed record IngestEndpointResponse(
        bool Success,
        int DocumentsProcessed,
        int ChunksCreated,
        string Message);
}
