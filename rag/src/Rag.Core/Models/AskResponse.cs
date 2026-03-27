namespace Rag.Core.Models;

public sealed record AskResponse(
    bool Answered,
    string AnswerText,
    IReadOnlyList<Citation> Citations,
    string? RefusalReason,
    AskDiagnostics? Diagnostics = null
);
