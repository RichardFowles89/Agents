using Rag.Core.Models;

namespace Rag.Core.Contracts;

public interface IAnswerAgent
{
    Task<AnswerDraft> GenerateAsync(string question, IReadOnlyList<RetrievalHit> approvedHits, CancellationToken cancellationToken = default);
}
