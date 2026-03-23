using Rag.Core.Contracts;
using Rag.Core.Models;

namespace FunctionsApp.Agents;

internal sealed class StubAnswerAgent : IAnswerAgent
{
    public Task<AnswerDraft> GenerateAsync(
        string question,
        IReadOnlyList<RetrievalHit> approvedHits,
        CancellationToken cancellationToken = default)
    {
        string answerText = approvedHits.Count > 0
            ? $"Based on {approvedHits.Count} retrieved document(s): {approvedHits[0].ChunkText}"
            : "No relevant documents found.";

        IReadOnlyList<Citation> citations = approvedHits
            .Select(h => new Citation(h.SourceId, h.Title, h.SectionPath, h.SourceUrl))
            .ToList();

        return Task.FromResult(new AnswerDraft(answerText, citations));
    }
}
