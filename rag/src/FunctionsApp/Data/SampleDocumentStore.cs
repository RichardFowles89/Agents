using Rag.Core.Models;

namespace FunctionsApp.Data;

internal static class SampleDocumentStore
{
    internal static IReadOnlyList<RetrievalHit> GetSeededHits()
    {
        return
        [
            new RetrievalHit(
                ChunkId: "rag-001",
                SourceId: "doc-rag-overview",
                Title: "What is RAG",
                SectionPath: "Overview > RAG",
                ChunkText: "RAG stands for Retrieval-Augmented Generation. It is a pattern where a language model is grounded by first retrieving relevant documents from a search index and then generating an answer based only on the retrieved content. This prevents hallucination because the model is constrained to the retrieved context.",
                SourceUrl: null,
                Score: 0),

            new RetrievalHit(
                ChunkId: "rag-002",
                SourceId: "doc-rag-overview",
                Title: "What is RAG",
                SectionPath: "Overview > Why RAG matters",
                ChunkText: "Without RAG, a language model answers from its training data, which may be stale or wrong. With RAG, every answer is grounded in documents you control. You can update the index without retraining the model.",
                SourceUrl: null,
                Score: 0),

            new RetrievalHit(
                ChunkId: "azure-search-001",
                SourceId: "doc-azure-search",
                Title: "Azure AI Search",
                SectionPath: "Azure AI Search > Overview",
                ChunkText: "Azure AI Search is a managed cloud search service that supports both keyword (BM25) and vector (ANN) search. In a RAG pipeline it acts as the retrieval layer, storing document chunks and returning the most relevant ones for a given query.",
                SourceUrl: null,
                Score: 0),

            new RetrievalHit(
                ChunkId: "azure-openai-001",
                SourceId: "doc-azure-openai",
                Title: "Azure OpenAI",
                SectionPath: "Azure OpenAI > Overview",
                ChunkText: "Azure OpenAI provides access to GPT-4o and other large language models via a REST API. In a RAG pipeline it is used to generate grounded answers from retrieved chunks, and can also be used as the planner and safety reviewer agent.",
                SourceUrl: null,
                Score: 0),

            new RetrievalHit(
                ChunkId: "chunking-001",
                SourceId: "doc-chunking",
                Title: "Document Chunking",
                SectionPath: "Chunking > Why chunk documents",
                ChunkText: "Chunking splits large documents into smaller pieces so they fit within a model's context window. Good chunking preserves semantic meaning by splitting on heading boundaries and adding overlap between consecutive chunks so context is not lost at boundaries.",
                SourceUrl: null,
                Score: 0),
        ];
    }
}
