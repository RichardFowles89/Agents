using OpenAI.Chat;
using Rag.Core.Contracts;
using Rag.Core.Models;
using System.Text;

namespace FunctionsApp.Agents;

/// <summary>
/// Safety reviewer backed by Azure OpenAI GPT-4o.
/// Reviews generated answers for potential safety issues such as harmful content,
/// hallucinations, or claims unsupported by the provided context.
/// </summary>
internal sealed class AzureOpenAISafetyReviewerAgent : ISafetyReviewerAgent
{
    private readonly ChatClient _chatClient;

    private const string SystemPrompt =
        """
        You are a safety reviewer. Your job is to evaluate whether an answer is safe to return to the user.
        You must check for:
        1. Hallucinations: Does the answer only use information from the provided context?
        2. Harmful content: Does the answer contain threats, abuse, or dangerous instructions?
        3. Misleading claims: Does the answer accurately reflect the source material?
        
        If the answer is safe, respond with: APPROVED
        If the answer is NOT safe, respond with: BLOCKED
        Then provide a brief reason (one sentence).
        """;

    /// <summary>Initialises the agent with a pre-configured <see cref="ChatClient"/>.</summary>
    public AzureOpenAISafetyReviewerAgent(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    /// <inheritdoc/>
    public async Task<SafetyReviewResult> ReviewAsync(
        string question,
        AnswerDraft draft,
        IReadOnlyList<RetrievalHit> approvedHits,
        CancellationToken cancellationToken = default)
    {
        string contextText = BuildContextText(approvedHits);
        string reviewMessage = BuildReviewMessage(question, draft.AnswerText, contextText);

        ChatCompletion response = await _chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(reviewMessage)
            ],
            cancellationToken: cancellationToken);

        string result = response.Content[0].Text.Trim();

        // Parse the response to determine approval
        bool isApproved = result.StartsWith("APPROVED", StringComparison.OrdinalIgnoreCase);
        string reason = ExtractReason(result);

        return new SafetyReviewResult(isApproved, reason);
    }

    private static string BuildReviewMessage(string question, string answer, string context)
    {
        return $"""
            Question: {question}
            
            Answer: {answer}
            
            Context provided to generate the answer:
            {context}
            
            Is this answer safe to return? Review for hallucinations, harmful content, and accuracy.
            """;
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

    private static string ExtractReason(string result)
    {
        // Try to extract the reason after APPROVED or BLOCKED
        string[] lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // If there's only one line (just APPROVED or BLOCKED), provide a generic reason
        if (lines.Length == 1)
        {
            return lines[0].StartsWith("APPROVED", StringComparison.OrdinalIgnoreCase)
                ? "Approved by safety reviewer."
                : "Blocked by safety reviewer.";
        }

        // Otherwise, take everything after the first line as the reason
        return string.Join(" ", lines[1..]).Trim();
    }
}
