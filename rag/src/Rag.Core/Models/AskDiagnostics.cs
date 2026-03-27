namespace Rag.Core.Models;

public sealed record AskDiagnostics(
    int AttemptsUsed,
    int MaxRetries,
    bool RetryTriggered,
    string? RewrittenQuery,
    IReadOnlyList<AskAttemptTrace> Attempts
);