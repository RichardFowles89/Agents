namespace Rag.Core.Models;

public sealed record SafetyReviewResult(
    bool IsApproved,
    string Reason
);
