namespace Rag.Core.Models;

public sealed record AskRequest(
    string Question,
    int Top = 5,
    int MaxAgentRetries = 1
);
