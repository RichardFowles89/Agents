namespace Rag.Core.Models;

public sealed record AskAttemptTrace(
    int Attempt,
    string RetrievalQuery,
    int HitCount,
    PlannerDecision PlannerDecision,
    bool QueryRewritten,
    bool RetrievalImproved
);