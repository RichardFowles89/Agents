using FunctionsApp.Agents;
using FunctionsApp.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rag.Core.Contracts;
using Rag.Core.Models;
using Rag.Core.Pipeline;
using Rag.Infrastructure.Retrieval;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        IReadOnlyList<RetrievalHit> seedHits = SampleDocumentStore.GetSeededHits();
        services.AddSingleton<ISearchRetriever>(new KeywordSearchRetriever(seedHits));
        services.AddSingleton<IPlannerAgent, StubPlannerAgent>();
        services.AddSingleton<IAnswerAgent, StubAnswerAgent>();
        services.AddSingleton<ISafetyReviewerAgent, StubSafetyReviewerAgent>();
        services.AddSingleton<IRagPipeline, RagPipeline>();
    })
    .Build();

host.Run();
