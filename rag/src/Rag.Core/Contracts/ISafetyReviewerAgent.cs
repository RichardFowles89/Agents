using Rag.Core.Models;

namespace Rag.Core.Contracts;

public interface ISafetyReviewerAgent
{
    Task<SafetyReviewResult> ReviewAsync(string question, AnswerDraft draft, IReadOnlyList<RetrievalHit> approvedHits, CancellationToken cancellationToken = default);
}
