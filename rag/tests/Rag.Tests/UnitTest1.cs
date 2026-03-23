using Rag.Core.Models;
using Rag.Infrastructure.Chunking;

namespace Rag.Tests;

public class UnitTest1
{
    [Fact]
    public void Chunk_SplitsTextIntoMultipleChunks_WhenTextExceedsLimit()
    {
        DocumentChunker chunker = new();
        SourceDocument source = new(
            SourceId: "doc-001",
            Title: "Runbook",
            Content: "# Intro\n" + new string('A', 90),
            SourceUrl: "https://example/runbook",
            Tags: new[] { "ops" });

        ChunkingOptions options = new(MaxChunkCharacters: 50, OverlapCharacters: 10);

        IReadOnlyList<DocumentChunk> chunks = chunker.Chunk(source, options);

        Assert.True(chunks.Count >= 2);
        Assert.All(chunks, chunk => Assert.Equal("Intro", chunk.SectionPath));
        Assert.All(chunks, chunk => Assert.StartsWith("doc-001:", chunk.Id));
    }
}
