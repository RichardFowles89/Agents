using Rag.Core.Contracts;
using Rag.Core.Models;

namespace Rag.Infrastructure.Retrieval;

public sealed class KeywordSearchRetriever : ISearchRetriever
{
    private readonly List<RetrievalHit> _index;
    private readonly object _lockObject = new();

    public KeywordSearchRetriever(IReadOnlyList<RetrievalHit>? seededIndex = null)
    {
        _index = seededIndex is not null ? new List<RetrievalHit>(seededIndex) : new List<RetrievalHit>();
    }

    public Task<IReadOnlyList<RetrievalHit>> RetrieveAsync(string question, int top, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question) || top <= 0)
        {
            return Task.FromResult<IReadOnlyList<RetrievalHit>>([]);
        }

        lock (_lockObject)
        {
            if (_index.Count == 0)
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
    }

    public Task IngestAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks is null || chunks.Count == 0)
        {
            return Task.CompletedTask;
        }

        lock (_lockObject)
        {
            foreach (DocumentChunk chunk in chunks)
            {
                var hit = new RetrievalHit(
                    chunk.Id,
                    chunk.SourceId,
                    chunk.Title,
                    chunk.SectionPath,
                    chunk.ChunkText,
                    chunk.SourceUrl,
                    0);

                _index.Add(hit);
            }
        }

        return Task.CompletedTask;
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
