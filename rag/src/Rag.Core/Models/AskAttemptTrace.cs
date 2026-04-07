namespace Rag.Core.Models;

public sealed record AskAttemptTrace(
    int Attempt,
    string RetrievalQuery,
    int HitCount,
    int RerankedHitCount,
    bool RerankApplied,
    PlannerDecision PlannerDecision,
    bool QueryRewritten,
    bool RetrievalImproved
);