using Azure.AI.OpenAI;
using OpenAI.Embeddings;
using Rag.Core.Contracts;

namespace FunctionsApp.Agents;

/// <summary>
/// Embedding service backed by Azure OpenAI text-embedding-3-small.
/// Converts text into a vector of floats suitable for semantic similarity search.
/// </summary>
internal sealed class AzureOpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;

    public AzureOpenAIEmbeddingService(EmbeddingClient embeddingClient)
    {
        _embeddingClient = embeddingClient;
    }

    /// <summary>
    /// Creates a vector embedding for the given text using the configured Azure OpenAI deployment.
    /// </summary>
    public async Task<IReadOnlyList<float>> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        OpenAIEmbedding embedding = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return embedding.ToFloats().ToArray();
    }
}
