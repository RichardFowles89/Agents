using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using FunctionsApp.Agents;
using FunctionsApp.Data;
using FunctionsApp.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Rag.Core.Contracts;
using Rag.Core.Models;
using Rag.Core.Pipeline;
using Rag.Infrastructure.Chunking;
using Rag.Infrastructure.Retrieval;

static string GetRequiredSetting(IConfiguration config, string key)
{
    string? value = config[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Missing required configuration value '{key}'. " +
            "For local debugging, set it in local.settings.json under Values or as an environment variable.");
    }

    return value;
}

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        IConfiguration config = context.Configuration;

        string endpointValue = GetRequiredSetting(config, "AZURE_OPENAI_ENDPOINT");
        string apiKey = GetRequiredSetting(config, "AZURE_OPENAI_KEY");
        string deployment = GetRequiredSetting(config, "AZURE_OPENAI_DEPLOYMENT");
        string embeddingDeployment = GetRequiredSetting(config, "AZURE_OPENAI_EMBEDDING_DEPLOYMENT");
        string searchEndpointValue = GetRequiredSetting(config, "AZURE_SEARCH_ENDPOINT");
        string searchApiKey = GetRequiredSetting(config, "AZURE_SEARCH_KEY");
        string searchIndexName = GetRequiredSetting(config, "AZURE_SEARCH_INDEX_NAME");

        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out Uri? endpoint))
        {
            throw new InvalidOperationException(
                "Configuration value 'AZURE_OPENAI_ENDPOINT' is not a valid absolute URI.");
        }

        if (!Uri.TryCreate(searchEndpointValue, UriKind.Absolute, out Uri? searchEndpoint))
        {
            throw new InvalidOperationException(
                "Configuration value 'AZURE_SEARCH_ENDPOINT' is not a valid absolute URI.");
        }

        AzureKeyCredential credential = new(apiKey);
        AzureOpenAIClient aoaiClient = new(endpoint, credential);
        ChatClient chatClient = aoaiClient.GetChatClient(deployment);
        EmbeddingClient embeddingClient = aoaiClient.GetEmbeddingClient(embeddingDeployment);
        services.AddSingleton(chatClient);
        services.AddSingleton(embeddingClient);
        services.AddSingleton<IEmbeddingService, AzureOpenAIEmbeddingService>();

        AzureKeyCredential searchCredential = new(searchApiKey);
        SearchIndexClient searchIndexClient = new(searchEndpoint, searchCredential);
        SearchClient searchClient = new(searchEndpoint, searchIndexName, searchCredential);
        services.AddSingleton(searchIndexClient);
        services.AddSingleton(searchClient);
        services.AddHostedService(provider =>
            new AzureSearchIndexBootstrapper(
                provider.GetRequiredService<SearchIndexClient>(),
                searchIndexName));
        services.AddSingleton<ISearchRetriever>(provider =>
            new AzureSearchRetriever(
                provider.GetRequiredService<SearchClient>(),
                provider.GetRequiredService<IEmbeddingService>()));
        services.AddSingleton<ISearchIndexer, AzureSearchIndexer>();
        services.AddSingleton<IDocumentChunker, DocumentChunker>();
        services.AddSingleton<IQueryRewriteAgent, AzureOpenAIQueryRewriteAgent>();
        services.AddSingleton<IPlannerAgent, AzureOpenAIPlannerAgent>();
        services.AddSingleton<IAnswerAgent, AzureOpenAIAnswerAgent>();
        services.AddSingleton<ISafetyReviewerAgent, AzureOpenAISafetyReviewerAgent>();
        services.AddSingleton<IRagPipeline, RagPipeline>();
    })
    .Build();

host.Run();
