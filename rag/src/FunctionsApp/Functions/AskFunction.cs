using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Rag.Core.Contracts;
using Rag.Core.Models;

namespace FunctionsApp.Functions;

public sealed class AskFunction
{
    private readonly IRagPipeline _pipeline;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AskFunction(IRagPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    [Function("Ask")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ask")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        // Debug: log raw request body
        //using var reader = new StreamReader(req.Body);
        //string rawBody = await reader.ReadToEndAsync();
        //System.Diagnostics.Debug.WriteLine($"[AskFunction] Received raw body: {rawBody}");
        //req.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rawBody));

        AskRequest? request = await JsonSerializer.DeserializeAsync<AskRequest>(
            req.Body, JsonOptions, cancellationToken);

        if (request is null)
        {
            HttpResponseData badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Request body is required.", cancellationToken);
            return badRequest;
        }

        AskResponse response = await _pipeline.AskAsync(request, cancellationToken);

        HttpResponseData ok = req.CreateResponse(HttpStatusCode.OK);
        ok.Headers.Add("Content-Type", "application/json");
        await ok.WriteStringAsync(
            JsonSerializer.Serialize(response, JsonOptions), cancellationToken);

        return ok;
    }
}
