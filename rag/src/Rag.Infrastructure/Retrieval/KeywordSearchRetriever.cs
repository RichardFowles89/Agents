using Rag.Core.Contracts;
using Rag.Core.Models;

namespace Rag.Infrastructure.Retrieval;

public sealed class KeywordSearchRetriever : ISearchRetriever
{
    private readonly IReadOnlyList<RetrievalHit> _index;

    public KeywordSearchRetriever(IReadOnlyList<RetrievalHit>? seededIndex = null)
    {
        _index = seededIndex ?? [];
    }

    public Task<IReadOnlyList<RetrievalHit>> RetrieveAsync(string question, int top, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question) || top <= 0 || _index.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<RetrievalHit>>([]);
        }

        string[] terms = question
            .ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\r', '\n', ',', '.', ':', ';', '!', '?', '-', '_', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

        if (terms.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<RetrievalHit>>([]);
        }

        List<RetrievalHit> ranked = _index
            .Select(hit => new
            {
                Hit = hit,
                Score = terms.Sum(term => ScoreTerm(hit, term))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Hit.Score)
            .Take(top)
            .Select(x => x.Hit with { Score = x.Score })
            .ToList();

        return Task.FromResult<IReadOnlyList<RetrievalHit>>(ranked);
    }

    private static double ScoreTerm(RetrievalHit hit, string term)
    {
        double score = 0;

        if (hit.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            score += 3;
        }

        if (hit.SectionPath.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            score += 2;
        }

        if (hit.ChunkText.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            score += 1;
        }

        return score;
    }
}
