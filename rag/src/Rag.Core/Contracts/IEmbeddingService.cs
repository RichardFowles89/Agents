namespace Rag.Core.Contracts;

public interface IEmbeddingService
{
    Task<IReadOnlyList<float>> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
