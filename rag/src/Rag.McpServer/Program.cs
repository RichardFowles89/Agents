using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
	.AddMcpServer()
	.WithStdioServerTransport();

IHost host = builder.Build();
await host.RunAsync();
