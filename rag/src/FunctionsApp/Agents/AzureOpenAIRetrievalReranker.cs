using OpenAI.Chat;
using Rag.Core.Contracts;
using Rag.Core.Models;
using System.Text;

namespace FunctionsApp.Agents;

internal sealed class AzureOpenAIRetrievalReranker : IRetrievalReranker
{
    private readonly ChatClient _chatClient;

    private const string SystemPrompt =
        """
        You are a search reranker.
        Given a question and numbered candidate passages, return the best passage numbers in order of relevance.
        Return only a comma-separated list of integers, e.g. 3,1,5,2,4
        Do not include explanations.
        """;

    public AzureOpenAIRetrievalReranker(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<IReadOnlyList<RetrievalHit>> RerankAsync(
        string question,
        IReadOnlyList<RetrievalHit> candidates,
        int top,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question) || candidates.Count == 0 || top <= 0)
        {
            return [];
        }

        if (candidates.Count <= top)
        {
            return candidates;
        }

        string candidateBlock = BuildCandidateBlock(candidates);
        ChatCompletion response = await _chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(
                    $"Question: {question}\n" +
                    $"Select up to {top} passage numbers.\n\n" +
                    $"Candidates:\n{candidateBlock}\n\n" +
                    "Best passage numbers:")
            ],
            cancellationToken: cancellationToken);

        string text = response.Content.Count > 0 ? response.Content[0].Text : string.Empty;
        IReadOnlyList<int> selectedIndexes = ParseIndexes(text, candidates.Count, top);
        if (selectedIndexes.Count == 0)
        {
            return candidates.Take(top).ToList();
        }

        List<RetrievalHit> reranked = new(selectedIndexes.Count);
        foreach (int index in selectedIndexes)
        {
            reranked.Add(candidates[index]);
        }

        return reranked;
    }

    private static string BuildCandidateBlock(IReadOnlyList<RetrievalHit> candidates)
    {
        StringBuilder sb = new();
        for (int i = 0; i < candidates.Count; i++)
        {
            RetrievalHit hit = candidates[i];
            sb.AppendLine($"[{i + 1}] Title: {hit.Title}");
            sb.AppendLine($"Section: {hit.SectionPath}");
            sb.AppendLine($"Text: {hit.ChunkText}");
            if (i < candidates.Count - 1)
            {
                sb.AppendLine("---");
            }
        }

        return sb.ToString();
    }

    private static IReadOnlyList<int> ParseIndexes(string responseText, int candidateCount, int maxCount)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return [];
        }

        HashSet<int> uniqueIndexes = [];
        List<int> ordered = [];
        string[] tokens = responseText.Split([',', ' ', '\n', '\r', '\t', ';'], StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            if (!int.TryParse(token.Trim(), out int oneBased))
            {
                continue;
            }

            int zeroBased = oneBased - 1;
            if (zeroBased < 0 || zeroBased >= candidateCount)
            {
                continue;
            }

            if (uniqueIndexes.Add(zeroBased))
            {
                ordered.Add(zeroBased);
            }

            if (ordered.Count == maxCount)
            {
                break;
            }
        }

        return ordered;
    }
}
