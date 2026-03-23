using System.Text;
using Rag.Core.Contracts;
using Rag.Core.Models;

namespace Rag.Infrastructure.Chunking;

public sealed class DocumentChunker : IDocumentChunker
{
    public IReadOnlyList<DocumentChunk> Chunk(SourceDocument document, ChunkingOptions options)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (options.MaxChunkCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxChunkCharacters), "MaxChunkCharacters must be greater than zero.");
        }

        if (options.OverlapCharacters < 0 || options.OverlapCharacters >= options.MaxChunkCharacters)
        {
            throw new ArgumentOutOfRangeException(nameof(options.OverlapCharacters), "OverlapCharacters must be non-negative and less than MaxChunkCharacters.");
        }

        List<(string SectionPath, string Text)> sections = SplitIntoSections(document.Content);
        List<DocumentChunk> chunks = new();
        int chunkIndex = 0;

        foreach ((string sectionPath, string sectionText) in sections)
        {
            foreach (string part in SplitWithOverlap(sectionText, options.MaxChunkCharacters, options.OverlapCharacters))
            {
                string chunkId = $"{document.SourceId}:{chunkIndex:D4}";
                chunks.Add(new DocumentChunk(
                    chunkId,
                    document.SourceId,
                    document.Title,
                    sectionPath,
                    part,
                    document.SourceUrl,
                    document.Tags));
                chunkIndex++;
            }
        }

        return chunks;
    }

    private static List<(string SectionPath, string Text)> SplitIntoSections(string content)
    {
        List<(string SectionPath, string Text)> sections = new();
        string currentSection = "root";
        StringBuilder buffer = new();

        foreach (string rawLine in content.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');

            if (line.StartsWith('#'))
            {
                FlushSection();
                currentSection = line.TrimStart('#').Trim();
                continue;
            }

            buffer.AppendLine(line);
        }

        FlushSection();

        return sections;

        void FlushSection()
        {
            string text = buffer.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                sections.Add((currentSection, text));
            }

            buffer.Clear();
        }
    }

    private static IEnumerable<string> SplitWithOverlap(string text, int maxChars, int overlapChars)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        int start = 0;
        while (start < text.Length)
        {
            int length = Math.Min(maxChars, text.Length - start);
            string chunk = text.Substring(start, length).Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                yield return chunk;
            }

            if (start + length >= text.Length)
            {
                yield break;
            }

            start += maxChars - overlapChars;
        }
    }
}
