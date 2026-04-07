using ModelContextProtocol.Server;

namespace Rag.McpServer.Tools;

[McpServerToolType]
internal sealed class HealthTools
{
    [McpServerTool]
    public object health_check()
    {
        return new
        {
            status = "ok",
            service = "rag-mcp-server",
            timestampUtc = DateTimeOffset.UtcNow
        };
    }
}
