using System.Net.Http.Json;
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

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            endpoint,
            new AskRequest(question, top, maxAgentRetries),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            return new AskResponse(false, string.Empty, [], $"ask endpoint call failed: {(int)response.StatusCode} {response.ReasonPhrase}. {error}");
        }

        AskResponse? askResponse = await response.Content.ReadFromJsonAsync<AskResponse>(cancellationToken: cancellationToken);
        return askResponse ?? new AskResponse(false, string.Empty, [], "ask endpoint returned an empty response.");
    }
}
