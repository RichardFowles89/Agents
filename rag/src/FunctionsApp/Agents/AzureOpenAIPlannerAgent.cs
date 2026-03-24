using OpenAI.Chat;
using Rag.Core.Contracts;
using Rag.Core.Models;
using System.Text;

namespace FunctionsApp.Agents;

/// <summary>
/// Planner agent backed by Azure OpenAI GPT-4o.
/// Assesses whether retrieved document chunks contain sufficient information
/// to answer the user's question.
/// </summary>
internal sealed class AzureOpenAIPlannerAgent : IPlannerAgent
{
    private readonly ChatClient _chatClient;

    private const string SystemPrompt =
        """
        You are a retrieval quality assessor. Given a user question and retrieved document chunks,
        decide if the chunks contain sufficient information to answer the question accurately.

        Respond with exactly one word, nothing else:
        - Answerable — the chunks clearly address the question
        - Refuse — the chunks are irrelevant or do not contain enough information
        """;

    /// <summary>Initialises the agent with a pre-configured <see cref="ChatClient"/>.</summary>
    public AzureOpenAIPlannerAgent(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    /// <inheritdoc/>
    public async Task<PlannerDecision> AssessAsync(
        string question,
        IReadOnlyList<RetrievalHit> retrievalHits,
        CancellationToken cancellationToken = default)
    {
        if (retrievalHits.Count == 0)
        {
            return PlannerDecision.Refuse;
        }

        string chunksText = BuildChunksText(retrievalHits);

        ChatCompletion response = await _chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage($"Question: {question}\n\nRetrieved chunks:\n{chunksText}")
            ],
            cancellationToken: cancellationToken);

        string reply = response.Content[0].Text.Trim();

        return reply.Contains("Answerable", StringComparison.OrdinalIgnoreCase)
            ? PlannerDecision.Answerable
            : PlannerDecision.Refuse;
    }

    private static string BuildChunksText(IReadOnlyList<RetrievalHit> hits)
    {
        StringBuilder sb = new();

        for (int i = 0; i < hits.Count; i++)
        {
            sb.AppendLine($"[{i + 1}] {hits[i].ChunkText}");
            if (i < hits.Count - 1)
            {
                sb.AppendLine("---");
            }
        }

        return sb.ToString();
    }
}
