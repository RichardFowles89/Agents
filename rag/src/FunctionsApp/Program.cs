using Azure;
using Azure.AI.OpenAI;
using FunctionsApp.Agents;
using FunctionsApp.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI.Chat;
using Rag.Core.Contracts;
using Rag.Core.Models;
using Rag.Core.Pipeline;
using Rag.Infrastructure.Retrieval;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        IConfiguration config = context.Configuration;

        Uri endpoint = new(config["AZURE_OPENAI_ENDPOINT"]!);
        AzureKeyCredential credential = new(config["AZURE_OPENAI_KEY"]!);
        string deployment = config["AZURE_OPENAI_DEPLOYMENT"]!;

        AzureOpenAIClient aoaiClient = new(endpoint, credential);
        ChatClient chatClient = aoaiClient.GetChatClient(deployment);
        services.AddSingleton(chatClient);

        IReadOnlyList<RetrievalHit> seedHits = SampleDocumentStore.GetSeededHits();
        services.AddSingleton<ISearchRetriever>(new KeywordSearchRetriever(seedHits));
        services.AddSingleton<IPlannerAgent, AzureOpenAIPlannerAgent>();
        services.AddSingleton<IAnswerAgent, AzureOpenAIAnswerAgent>();
        services.AddSingleton<ISafetyReviewerAgent, StubSafetyReviewerAgent>();
        services.AddSingleton<IRagPipeline, RagPipeline>();
    })
    .Build();

host.Run();
