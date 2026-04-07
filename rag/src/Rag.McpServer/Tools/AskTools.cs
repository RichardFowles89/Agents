using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Server;
using Rag.Core.Models;

namespace Rag.McpServer.Tools;

[McpServerToolType]
internal sealed class AskTools
{
    private const string DefaultAskEndpoint = "http://localhost:7071/api/ask";

    private readonly IHttpClientFactory _httpClientFactory;

    public AskTools(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [McpServerTool]
    public async Task<AskResponse> ask_question(
        string question,
        int top = 5,
        int maxAgentRetries = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return new AskResponse(false, string.Empty, [], "Question is required.");
        }

        string endpoint = Environment.GetEnvironmentVariable("RAG_FUNCTIONS_ASK_ENDPOINT") ?? DefaultAskEndpoint;
        HttpClient client = _httpClientFactory.CreateClient();

        var request = new AskRequest(question, top, maxAgentRetries);
        string jsonBody = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        System.Diagnostics.Debug.WriteLine($"[AskTools] Sending to endpoint: {endpoint}");
        System.Diagnostics.Debug.WriteLine($"[AskTools] Request JSON: {jsonBody}");

        var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(endpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            return new AskResponse(false, string.Empty, [], $"ask endpoint call failed: {(int)response.StatusCode} {response.ReasonPhrase}. {error}");
        }

        string rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return new AskResponse(false, string.Empty, [], "ask endpoint returned an empty response body.");
        }

        try
        {
            AskResponse? askResponse = JsonSerializer.Deserialize<AskResponse>(
                rawResponse,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            return askResponse ?? new AskResponse(false, string.Empty, [], "ask endpoint returned an empty JSON response.");
        }
        catch (JsonException)
        {
            return new AskResponse(
                false,
                string.Empty,
                [],
                $"ask endpoint returned non-JSON content: {TrimForError(rawResponse)}");
        }
    }

    private static string TrimForError(string value)
    {
        string trimmed = value.Trim();
        return trimmed.Length <= 200 ? trimmed : trimmed[..200];
    }
}
