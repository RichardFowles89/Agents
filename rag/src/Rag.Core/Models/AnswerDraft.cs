namespace Rag.Core.Models;

public sealed record AnswerDraft(
    string AnswerText,
    IReadOnlyList<Citation> Citations
);
