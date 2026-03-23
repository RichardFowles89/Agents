namespace Rag.Core.Models;

public enum PlannerDecision
{
    Refuse = 0,
    NeedsMoreContext = 1,
    Answerable = 2
}
