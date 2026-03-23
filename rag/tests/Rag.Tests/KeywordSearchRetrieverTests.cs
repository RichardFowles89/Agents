using Rag.Core.Models;
using Rag.Infrastructure.Retrieval;

namespace Rag.Tests;

public class KeywordSearchRetrieverTests
{
    [Fact]
    public async Task RetrieveAsync_ReturnsRankedHits_AndRespectsTop()
    {
        List<RetrievalHit> index =
        [
            new("1", "doc-a", "Runbook", "Deploy", "Steps for safe deployment", null, 0),
            new("2", "doc-b", "Incident Guide", "Recover", "Deployment rollback and recovery", null, 0),
            new("3", "doc-c", "API Notes", "Auth", "Token validation details", null, 0)
        ];

        KeywordSearchRetriever retriever = new(index);

        IReadOnlyList<RetrievalHit> results = await retriever.RetrieveAsync("deployment recovery", top: 2);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.SourceId == "doc-a");
        Assert.Contains(results, r => r.SourceId == "doc-b");
    }
}
