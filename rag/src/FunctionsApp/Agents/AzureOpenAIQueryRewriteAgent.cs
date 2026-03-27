using OpenAI.Chat;
using Rag.Core.Contracts;
using Rag.Core.Models;
using System.Text;

namespace FunctionsApp.Agents;

/// <summary>
/// Rewrites user questions into retrieval-optimised queries when initial retrieval is insufficient.
/// </summary>
internal sealed class AzureOpenAIQueryRewriteAgent : IQueryRewriteAgent
{
    private readonly ChatClient _chatClient;

    private const string SystemPrompt =
        """
        You rewrite user questions into a compact retrieval query for document search.
        Return exactly one line of plain text.
        Keep it concise (up to 12 words), include key nouns/verbs, avoid filler words.
        Do not answer the question.
        """;

    public AzureOpenAIQueryRewriteAgent(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<string> RewriteForRetrievalAsync(
        string question,
        PlannerDecision plannerDecision,
        IReadOnlyList<RetrievalHit> retrievalHits,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return string.Empty;
        }

        string retrievedSummary = BuildRetrievedSummary(retrievalHits);

        ChatCompletion response = await _chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(
                    $"Original question: {question}\n" +
                    $"Planner decision: {plannerDecision}\n" +
                    $"Retrieved snippets:\n{retrievedSummary}\n\n" +
                    "Rewrite for retrieval:")
            ],
            cancellationToken: cancellationToken);

        string rewritten = response.Content[0].Text.Trim();
        rewritten = rewritten.Replace("\r", " ").Replace("\n", " ").Trim();

        return string.IsNullOrWhiteSpace(rewritten) ? question : rewritten;
    }

    private static string BuildRetrievedSummary(IReadOnlyList<RetrievalHit> hits)
    {
        if (hits.Count == 0)
        {
            return "(none)";
        }

        StringBuilder sb = new();
        int max = Math.Min(3, hits.Count);

        for (int i = 0; i < max; i++)
        {
            string snippet = hits[i].ChunkText;
            if (snippet.Length > 180)
            {
                snippet = snippet[..180];
            }

            sb.AppendLine($"- {snippet}");
        }

        return sb.ToString();
    }
}