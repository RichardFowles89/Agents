using OpenAI.Chat;
using Rag.Core.Contracts;
using Rag.Core.Models;
using System.Text;

namespace FunctionsApp.Agents;

/// <summary>
/// Answer agent backed by Azure OpenAI GPT-4o.
/// Generates a grounded answer from approved retrieval hits using only
/// the provided context — never drawing on outside knowledge.
/// </summary>
internal sealed class AzureOpenAIAnswerAgent : IAnswerAgent
{
    private readonly ChatClient _chatClient;

    private const string SystemPrompt =
        """
        You are a helpful assistant. Answer the user's question using ONLY the provided context.
        Do not use any knowledge outside the provided context.
        Be concise, accurate, and reference the source numbers (e.g. [1], [2]) when drawing from them.
        If the context does not fully address the question, say so clearly.
        """;

    /// <summary>Initialises the agent with a pre-configured <see cref="ChatClient"/>.</summary>
    public AzureOpenAIAnswerAgent(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    /// <inheritdoc/>
    public async Task<AnswerDraft> GenerateAsync(
        string question,
        IReadOnlyList<RetrievalHit> approvedHits,
        CancellationToken cancellationToken = default)
    {
        string contextText = BuildContextText(approvedHits);

        ChatCompletion response = await _chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage($"Question: {question}\n\nContext:\n{contextText}")
            ],
            cancellationToken: cancellationToken);

        string answerText = response.Content[0].Text.Trim();

        IReadOnlyList<Citation> citations = approvedHits
            .Select(h => new Citation(h.SourceId, h.Title, h.SectionPath, h.SourceUrl))
            .ToList();

        return new AnswerDraft(answerText, citations);
    }

    private static string BuildContextText(IReadOnlyList<RetrievalHit> hits)
    {
        StringBuilder sb = new();

        for (int i = 0; i < hits.Count; i++)
        {
            sb.AppendLine($"[{i + 1}] (Source: {hits[i].Title}) {hits[i].ChunkText}");
            if (i < hits.Count - 1)
            {
                sb.AppendLine("---");
            }
        }

        return sb.ToString();
    }
}
