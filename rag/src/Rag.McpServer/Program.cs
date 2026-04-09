using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rag.McpServer.Tools;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();

builder.Services
	.AddMcpServer()
	.WithStdioServerTransport()
	.WithTools<HealthTools>()
	.WithTools<AskTools>()
	.WithTools<IngestTools>();

IHost host = builder.Build();
await host.RunAsync();
