# RAG Learning Project Checkpoint

Date: April 7, 2026
Status: **Strong Agent RAG v1 + Reranking Integration Complete (Code + Tests + Build)**. Candidate retrieval now supports reranking with fail-open fallback.
Next Step (Immediate): Run endpoint smoke tests with reranking path (`/api/health`, `/api/ingest`, `/api/ask`) and inspect diagnostics in live responses.

## Session Continuity Rule (Permanent)

This file is the source of truth for session continuity.
You must update CHECKPOINT.md after every meaningful change, validation step, or decision so work is recoverable if the session breaks.

---

## Current State Summary

- Azure Functions isolated worker host is running locally.
- End-to-end request flow is working:
  - POST /api/ask -> retriever -> planner -> answer -> safety -> response
- Endpoints verified:
  - POST /api/ask
  - GET /api/health
  - POST /api/ingest
- Ingestion works and newly ingested content is retrievable in subsequent ask calls.
- Planner, answer, and safety reviewer are wired to Azure OpenAI implementations.
- Vector/hybrid retrieval implementation is complete and smoke-validated.
- Agentic RAG v1 retry orchestration is implemented in pipeline (query rewrite + one re-retrieval attempt).
- Batch 1 strong-agentic controls implemented: configurable retry limit + attempt diagnostics metadata.
- Unit tests were previously green for RagPipelineTests (5 passing at last checkpoint verification).

---

## What Was Implemented

### Agents and Pipeline

- rag/src/FunctionsApp/Agents/AzureOpenAIPlannerAgent.cs
  - Uses Azure OpenAI to decide if available context is sufficient.
- rag/src/FunctionsApp/Agents/AzureOpenAIAnswerAgent.cs
  - Uses Azure OpenAI to produce grounded answers.
- rag/src/FunctionsApp/Agents/AzureOpenAISafetyReviewerAgent.cs
  - Uses Azure OpenAI for safety and hallucination review.
- rag/src/Rag.Core/Pipeline/RagPipeline.cs
  - Orchestrates retrieval, planning, answer generation, and safety review.

### Functions and Data Flow

- rag/src/FunctionsApp/Functions/AskFunction.cs
  - HTTP POST /ask endpoint (JSON in/out).
- rag/src/FunctionsApp/Functions/IngestFunction.cs
  - HTTP POST /ingest endpoint for adding source documents.
- rag/src/FunctionsApp/Functions/HealthFunction.cs
  - HTTP GET /health endpoint.
- rag/src/FunctionsApp/Data/SampleDocumentStore.cs
  - Seeded local documents for retrieval.

### Contracts/Infrastructure/Test Updates

- rag/src/Rag.Core/Contracts/ISearchRetriever.cs
  - Includes IngestAsync for runtime ingestion.
- rag/src/Rag.Infrastructure/Retrieval/KeywordSearchRetriever.cs
  - Mutable in-memory index with locking for ingest and query operations.
- rag/src/Rag.Core/Models/IngestRequest.cs
  - Request model for /ingest payload.
- rag/tests/Rag.Tests/RagPipelineTests.cs
  - Fake retriever updated for IngestAsync and pipeline coverage.

---

## Validated Behavior

- Host starts and discovers Ask, Health, and Ingest routes.
- GET /api/health responds successfully.
- POST /api/ask happy path returns answered=true with citations.
- POST /api/ask refusal path returns answered=false with refusalReason when context is insufficient.
- POST /api/ingest accepts documents, chunks content, and adds to retriever index.
- Newly ingested content can be retrieved and used by /ask.

---

## Known Gaps / Risks

- AzureWebJobsStorage reports unhealthy when unset locally; HTTP endpoints still function.
- Ingested data persists in Azure AI Search with embeddings — survives host restarts.
- Agentic retry path is build/test validated but not yet smoke-validated through live `/api/ask` calls.
- Comprehensive test suite is not complete yet (additional branch/error/concurrency coverage needed).

---

## Environment Notes

- OS: Windows
- Functions Core Tools: v4.x (previously observed v4.8.0)
- .NET SDK: 10.0.201
- Function runtime: v4 isolated worker
- Local port: 7071

---

## How To Run Locally

### Start Host

```bash
cd rag/src/FunctionsApp
func start
```

Expected routes:

- Ask: POST http://localhost:7071/api/ask
- Health: GET http://localhost:7071/api/health
- Ingest: POST http://localhost:7071/api/ingest

### Quick Smoke Requests

Health:

```http
GET http://localhost:7071/api/health
```

Ask (happy path):

```http
POST http://localhost:7071/api/ask
Content-Type: application/json

{"question":"what is RAG?","top":3}
```

Ask (refusal path):

```http
POST http://localhost:7071/api/ask
Content-Type: application/json

{"question":"kubernetes deployment strategies"}
```

Ingest:

```http
POST http://localhost:7071/api/ingest
Content-Type: application/json

{
  "documents": [
    {
      "id": "doc-001",
      "title": "Sample",
      "content": "RAG combines retrieval with generation for grounded answers."
    }
  ]
}
```

---

## Next Work Queue (March 27, 2026 - Deferred)

**Batch 1 (Strong Agentic Controls) - COMPLETE ✅**
- ✅ Bounded retry loop with configurable max retries (0-3 supported)
- ✅ Query rewrite agent integrated (LLM-based query improvement)
- ✅ Planner gate with lexical fallback for robustness
- ✅ Diagnostics metadata with per-attempt tracing
- ✅ Validated: in-scope questions answered, out-of-scope questions refused

**Batch 2 (Second Agent Action) - Optional Future Enhancement**
- Choose one:
  - **Option A:** Query decomposition (split multi-hop questions → sub-questions loop)
  - **Option B:** Clarification agent (detect ambiguity, ask user for follow-up)
- Expected timeline: 2-3 days for implementation + validation
- Recommendation: Option B for faster delivery (simpler test surface)

**Batch 3 (Reranking Stack) - Optional Future Enhancement**
- Fetch top 50 candidates instead of 5
- Add reranker layer (LLM or cross-encoder) to re-rank 50 → 5
- Generate answers from reranked results
- Expected impact: higher precision, lower false-omissions (FOD)
- Expected timeline: 3-4 days for implementation + validation

**Batch 4 (Integration/Packaging) - Optional Future Enhancement**
- Package as MCP (Model Context Protocol) server
- Define agent capability manifest with tool schemas
- Release as APM package version 0.1
- Expected timeline: 2-3 days

**Hardening Tasks** (if needed before production):
- Comprehensive error path and branch coverage testing
- Performance profiling and optimization
- Cloud deployment setup (Azure Container Registry, App Service, etc.)

## Session Update (March 27, 2026)

### Agent-RAG Learning Kickoff

- Created baseline evaluation dataset at `rag/tests/evals/baseline-questions.json`.
- Dataset contains 20 questions split across:
  - `in_scope_direct` (8)
  - `in_scope_paraphrased` (8)
  - `out_of_scope` (4)
- Each item includes `id`, `question`, `expected` (`answer` or `refuse`), and `category`.
- Purpose: establish an objective benchmark before implementing agentic retrieval loops and reranking stacks.

### Agentic RAG v1 Implementation

- Added `IQueryRewriteAgent` contract in `rag/src/Rag.Core/Contracts/IQueryRewriteAgent.cs`.
- Added `AzureOpenAIQueryRewriteAgent` implementation in `rag/src/FunctionsApp/Agents/AzureOpenAIQueryRewriteAgent.cs`.
- Updated DI registration in `rag/src/FunctionsApp/Program.cs` for `IQueryRewriteAgent`.
- Updated `rag/src/Rag.Core/Pipeline/RagPipeline.cs` to perform one retry when planner initially refuses or requests more context:
  - Assess initial retrieval
  - Rewrite query for retrieval
  - Re-retrieve once and re-assess
  - Continue to answer/safety only when decision is `Answerable`
- Validation:
  - `dotnet build rag/RagAssistant.sln` succeeded
  - `RagPipelineTests` pass (`3/3`)

### Batch 1 Strong-Agentic Controls

- Added `AskRequest.MaxAgentRetries` (default `1`) to control retry depth.
- Added diagnostics models:
  - `rag/src/Rag.Core/Models/AskDiagnostics.cs`
  - `rag/src/Rag.Core/Models/AskAttemptTrace.cs`
- Extended `AskResponse` to include optional `Diagnostics` payload.
- Updated `rag/src/Rag.Core/Pipeline/RagPipeline.cs`:
  - bounded retry loop (`0..MaxAgentRetries`)
  - duplicate-rewrite guard (stop when rewritten query repeats)
  - basic no-gain tracking per attempt via `RetrievalImproved`
  - diagnostics emitted on success/refusal/safety-reject paths
- Updated `rag/tests/Rag.Tests/RagPipelineTests.cs` for new behavior.
- Validation:
  - `dotnet build rag/RagAssistant.sln` succeeded
  - `RagPipelineTests` pass (`4/4`)

### Ask Refusal Fix (March 27, 2026)

- Symptom observed: both in-scope and out-of-scope `/api/ask` calls returned planner refusal.
- Root causes identified:
  - Index was being recreated on each host start (data loss risk).
  - Ingest payload commonly used `id` while model expected `sourceId`, causing key collisions and poor retrieval context.
  - Retrieval parsing mode was too strict for natural-language ask queries.
- Fixes applied:
  - `rag/src/FunctionsApp/Search/AzureSearchIndexBootstrapper.cs` now creates index only when missing (no unconditional delete).
  - `rag/src/FunctionsApp/Functions/IngestFunction.cs` now accepts both `id` and `sourceId` input fields and maps them safely to `SourceDocument.SourceId`.
  - `rag/src/Rag.Infrastructure/Retrieval/AzureSearchRetriever.cs` switched to `SearchQueryType.Simple` with `SearchMode.Any` for better recall.
  - `rag/src/FunctionsApp/Agents/AzureOpenAIPlannerAgent.cs` includes lexical-grounding fallback when LLM planner is overly strict.
- Validation:
  - In-scope question (`What does RAG stand for?`) now returns `answered: true` with citations.
  - Out-of-scope question (`How do I implement Kubernetes blue-green deployments?`) still returns planner refusal.

---

## Housekeeping Update (March 25, 2026)

- Root git ignore was tightened to prevent committing local build artifacts and binary outputs (dll/exe/pdb/cache and related generated files).
- Note: ignore rules do not untrack files that were already committed; tracked artifacts require explicit index cleanup when ready.
- VS Code Run/Debug flow was corrected to use Azure Functions host startup instead of launching the app DLL directly.
- `Program.cs` now validates required Azure OpenAI settings with clear errors instead of crashing with a null URI exception.
- Validation: `func host start --pause-on-error` now starts and advertises Ask, Health, and Ingest routes from `rag/src/FunctionsApp`.
- Known local warning remains: AzureWebJobsStorage unhealthy when unset.

## Azure Search Migration Update (March 25, 2026)

- Decision: next stub removal target is local in-memory retrieval/indexing; migration target is Azure AI Search.
- Added `Azure.Search.Documents` dependency to `rag/src/Rag.Infrastructure/Rag.Infrastructure.csproj`.
- Implemented `rag/src/Rag.Infrastructure/Retrieval/AzureSearchRetriever.cs`:
  - `RetrieveAsync` now queries Azure AI Search and maps results to `RetrievalHit`.
  - `IngestAsync` now uses `MergeOrUpload` to index chunks into Azure AI Search.
- Updated DI/config wiring in `rag/src/FunctionsApp/Program.cs`:
  - `ISearchRetriever` now resolves to `AzureSearchRetriever`.
  - Added required settings validation for:
    - `AZURE_SEARCH_ENDPOINT`
    - `AZURE_SEARCH_KEY`
    - `AZURE_SEARCH_INDEX_NAME`
- Updated `rag/src/FunctionsApp/local.settings.json` to placeholder values (`REPLACE_ME`) and added Azure Search keys.
- Validation: `dotnet build rag/RagAssistant.sln` completed without compile errors.

## Azure Search Startup Bootstrap Update (March 25, 2026)

- Added `rag/src/FunctionsApp/Search/AzureSearchIndexBootstrapper.cs` as an `IHostedService`.
- Startup behavior now checks for configured index and creates it automatically when missing.
- Index schema created on startup:
  - `id` (key, string)
  - `sourceId` (string)
  - `title` (searchable string)
  - `sectionPath` (searchable string)
  - `chunkText` (searchable string)
  - `sourceUrl` (string)
- Updated `rag/src/FunctionsApp/Program.cs`:
  - Registered `SearchIndexClient`.
  - Registered hosted bootstrap service with `AZURE_SEARCH_INDEX_NAME`.
  - Kept `SearchClient` registration for runtime retrieval/ingest.
- Validation: `dotnet build rag/RagAssistant.sln` succeeded after bootstrap integration.

### Remaining Work For This Migration

1. Set real values for Azure Search settings in local environment.
2. Run host and smoke test:
   - POST `/api/ingest` to push chunks to Azure Search.
   - POST `/api/ask` to confirm retrieval-backed answers.

## Session Update (March 26, 2026)

### Fixes Applied

- **SearchMode.All → SearchMode.Any** in `rag/src/Rag.Infrastructure/Retrieval/AzureSearchRetriever.cs`.
  - `All` required every query token to match, causing "what is RAG?" to return no hits.
  - `Any` returns documents matching any token, which is correct for keyword retrieval.
- **Chunk ID colon fix** was already in place from previous session (`doc-001_0000` format with `SanitizeIdSegment`).

### Validated Behaviour (March 26, 2026)

- `POST /api/ingest` — document accepted, 1 chunk indexed in Azure AI Search (no key validation error).
- `POST /api/ask {"question":"what is RAG?"}` — answered=true, grounded answer returned from cloud index.
- `POST /api/ask {"question":"kubernetes deployment strategies"}` — answered=false, planner refusal as expected.

## Vector Embeddings Implementation (March 26, 2026)

### Batch 1: Embedding Service + DI Wiring

- Created `rag/src/FunctionsApp/Agents/AzureOpenAIEmbeddingService.cs`
  - Implements `IEmbeddingService`
  - Uses Azure OpenAI `text-embedding-3-small` deployment
  - Returns `IReadOnlyList<float>` vectors (1536 dimensions)
- Added `AzureOpenAI_EMBEDDING_DEPLOYMENT: text-embedding-3-small` config in `local.settings.json`
- Updated `Program.cs`:
  - Added `using OpenAI.Embeddings`
  - Registered `EmbeddingClient` from Azure OpenAI client
  - Registered `IEmbeddingService → AzureOpenAIEmbeddingService`
- Validation: Build passes, embedding service callable from DI container

### Batch 2: Vector Index + Indexer + Ingest Pipeline

- Updated `rag/src/FunctionsApp/Search/AzureSearchIndexBootstrapper.cs`
  - Now deletes and recreates the index on startup (enables schema changes)
  - Added `embedding` field (1536 dimensions, HNSW vector search algorithm)
  - Added `VectorSearch` configuration with HNSW profile
- Created `rag/src/Rag.Infrastructure/Retrieval/AzureSearchIndexer.cs`
  - Implements `ISearchIndexer`
  - Takes `IReadOnlyList<DocumentChunkWithEmbedding>`, uploads to Azure Search with embedding vectors
  - Uses MergeOrUpload semantics for idempotent indexing
- Updated `rag/src/FunctionsApp/Functions/IngestFunction.cs`
  - Replaced `ISearchRetriever` dependency with `IEmbeddingService` + `ISearchIndexer`
  - New ingest flow: chunk → embed each chunk → store with embeddings
  - Removed keyword-only fallback; all indexing now vector-backed
- Updated `Program.cs`:
  - Registered `ISearchIndexer → AzureSearchIndexer`
- Validation: Build passes, Postman test confirms ingest works with vector indexing

### Known Behavior (March 26, 2026 - Batch 2 Complete)

- ✅ Embedding deployment `text-embedding-3-small` deployed in Azure OpenAI resource
- ✅ Index now has vector field; recreated on host startup
- ✅ Ingest function embeds chunks and indexes them with vectors
- ✅ `POST /api/ingest` succeeds and creates vector embeddings
- ⏳ `POST /api/ask` still uses keyword-only retrieval (Batch 3 will add vector search)

---

## Previous Session Validated Behaviour (March 26, 2026)

- `POST /api/ingest` — document accepted, 1 chunk indexed in Azure AI Search (no key validation error).
- `POST /api/ask {"question":"what is RAG?"}` — answered=true, grounded answer returned from cloud index.
- `POST /api/ask {"question":"kubernetes deployment strategies"}` — answered=false, planner refusal as expected.

### Batch 3: Vector/Hybrid Retrieval

- Updated `rag/src/Rag.Infrastructure/Retrieval/AzureSearchRetriever.cs`
  - Now takes `IEmbeddingService` as constructor dependency
  - `RetrieveAsync` now:
    - Embeds the query using `IEmbeddingService.CreateEmbeddingAsync`
    - Performs hybrid search combining keyword (BM25) and vector similarity
    - Uses `VectorizedQuery` with 1536-dimensional embedding
    - Returns fused results from both signals
  - Query flow: keyword search + vector search → combined ranking by relevance score
- Updated `rag/src/FunctionsApp/Program.cs`
  - DI registration for `ISearchRetriever` now uses factory pattern
  - Injects both `SearchClient` and `IEmbeddingService` into `AzureSearchRetriever`
- Validation: Build passes; ready for end-to-end testing

### Known Behavior (March 26, 2026 - Batch 3 Complete)

- ✅ Full embedding pipeline: question → embed → hybrid search → results
- ✅ Hybrid search combines keyword BM25 + vector semantic signals
- ✅ All three batches built and compiled successfully
- ⏳ **Ready for end-to-end smoke testing**

---

## Previous Session Validated Behaviour (March 26, 2026)

- `POST /api/ingest` — document accepted, 1 chunk indexed in Azure AI Search (no key validation error).
- `POST /api/ask {"question":"what is RAG?"}` — answered=true, grounded answer returned from cloud index.
- `POST /api/ask {"question":"kubernetes deployment strategies"}` — answered=false, planner refusal as expected.

---

## Session End Checkpoint (March 25, 2026 - Session End)

### Blocker Encountered

Attempted first `/api/ingest` call against Azure AI Search cloud index with Postman payload:
```json
{
  "documents": [
    {
      "id": "doc-001",
      "title": "RAG Basics",
      "content": "Retrieval-Augmented Generation combines search retrieval with LLM generation...",
      "sourceUrl": "https://example.com/rag-basics"
    }
  ]
}
```

**Error**: Azure Search validation rejected document key (chunk ID).
- HTTP 400 (Bad Request)
- Error code: `InvalidName`
- Message: `Invalid document key: ':0000'. Keys can only contain letters, digits, underscore (_), dash (-), or equal sign (=).`

**Root Cause**: DocumentChunker creates chunk IDs like `doc-001:0000` (colon-separated chunks). Azure Search indexes reject IDs containing colons.

**Location**: [rag/src/Rag.Infrastructure/Chunking/DocumentChunker.cs](rag/src/Rag.Infrastructure/Chunking/DocumentChunker.cs)
- Current format uses colon (`sourceId:chunkNumber`) which is invalid for Azure Search.
- Fix needed: Replace colon with hyphen or underscore (e.g., `doc-001_0000` or `doc-001-0000`).

### Next Session Action Plan

1. **Fix chunk ID format** in DocumentChunker to use hyphen or underscore instead of colon.
2. **Rebuild and test** `dotnet build rag/RagAssistant.sln`.
3. **Run host** and retry `/api/ingest` smoke test.
4. **Verify `/api/ask`** retrieves ingested content from cloud index.
5. **Update checkpoint** once smoke tests pass.

### Session Summary

- ✅ Azure AI Search service created (rag20260325, free tier, uksouth).
- ✅ Azure Search retriever implementation (AzureSearchRetriever.cs).
- ✅ Startup index bootstrap (auto-creates rag-chunks index on host start).
- ✅ DI wiring and config validation for Search settings.
- ✅ Solution builds successfully.
- ❌ Ingest blocked on chunk ID validation (colon character not allowed).
- ⏳ Ask endpoint not yet tested against cloud index.

### Credentials and Resources

- Azure OpenAI Resource: `rdfrdfrag-aoai` in `rdfrdfrag-rg` (uksouth)
  - Deployment `gpt-4o` for planner/answer/safety reasoning
  - Deployment `text-embedding-3-small` for chunk embeddings (1536 dims)
- Azure Search Service: `rag20260325` in `rdfrdfrag-rg` (uksouth)
  - Index Name: `rag-chunks`
  - Fields: id, sourceId, title, sectionPath, chunkText, sourceUrl, embedding (1536d vector)
  - Vector search: HNSW algorithm profile
- Settings in [rag/src/FunctionsApp/local.settings.json](rag/src/FunctionsApp/local.settings.json):
  - `AZURE_OPENAI_ENDPOINT`: https://uksouth.api.cognitive.microsoft.com/
  - `AZURE_OPENAI_KEY`: (keep safe, not in source control)
  - `AZURE_OPENAI_DEPLOYMENT`: gpt-4o
  - `AZURE_OPENAI_EMBEDDING_DEPLOYMENT`: text-embedding-3-small
  - `AZURE_SEARCH_ENDPOINT`: https://rag20260325.search.windows.net
  - `AZURE_SEARCH_KEY`: (keep safe, not in source control)
  - `AZURE_SEARCH_INDEX_NAME`: rag-chunks

---

## Session Handoff Notes

When resuming:

1. **Start from this file first** — CHECKPOINT.md is the source of truth.
2. **Build status**: Last successful build was with Batches 1-3 complete. All projects should build cleanly.
3. **Host status**: Previous host started successfully with vector field index creation.
4. **What's next**: Focus on optional hardening and production readiness tasks.
5. **Testing approach**: Core smoke path is complete and passing; future work is broader regression/performance coverage.
6. **Update checkpoint** after each meaningful action — don't lose context.

## Session Update (March 27, 2026)

### End-to-End Validation Complete

- Full smoke test run completed and passing.
- `POST /api/ingest` succeeded with embedding-backed indexing.
- `POST /api/ask` happy path succeeded with grounded answer and citations.
- `POST /api/ask` refusal path succeeded for out-of-scope question.
- Result: project is now feature-complete from a functional RAG perspective.

## Session Update (April 7, 2026)

### MCP Local Server - Step 1 (Scaffold Only)

- Created new project: `rag/src/Rag.McpServer/Rag.McpServer.csproj` (net10.0 console app).
- Added project to solution: `rag/RagAssistant.sln`.
- Validation: `dotnet build rag/src/Rag.McpServer/Rag.McpServer.csproj` succeeded.
- Scope intentionally limited for incremental testing: no MCP protocol package/tool wiring yet.

### Next Immediate Step

- Add MCP server package references and a minimal host startup that can run locally.
- Keep behavior minimal and build-verified before adding `ask`/`ingest`/`health` tools.

## Session Update (March 27, 2026 - End of Session Handoff)

### Confirmed First Task For Next Session

- Start Functions host locally.
- Call endpoints in order:
  - `GET /api/health`
  - `POST /api/ingest`
  - `POST /api/ask`
- Step through the live request flow in debug mode to refresh current architecture and control flow:
  - AskFunction -> RagPipeline.AskAsync -> RetrieveAndAssessAsync -> Query rewrite retry loop (if triggered) -> GenerateReviewedResponseAsync -> safety review -> HTTP response
- After refresher walkthrough, decide whether to begin Batch 2 enhancement (clarification agent preferred, decomposition as alternative).

## Session Update (April 7, 2026)

### Reranking Implementation Started and Integrated

- Decision: clarification-agent path moved to **nice-to-have/deferred** due time constraints.
- Implemented reranking contract:
  - `rag/src/Rag.Core/Contracts/IRetrievalReranker.cs`
- Implemented Azure OpenAI reranker:
  - `rag/src/FunctionsApp/Agents/AzureOpenAIRetrievalReranker.cs`
  - Prompt asks model to return ordered candidate indexes only.
  - Response parsing validates bounds, uniqueness, and max result count.
  - Safe fallback to retrieval ordering when parsing fails.
- Updated pipeline integration:
  - `rag/src/Rag.Core/Pipeline/RagPipeline.cs`
  - Retrieves broad candidate set (`candidateTop = max(top, 50)`).
  - Applies reranking before planner assessment.
  - Uses fail-open fallback (`candidateHits.Take(top)`) if reranker errors.
- Updated diagnostics model:
  - `rag/src/Rag.Core/Models/AskAttemptTrace.cs`
  - Added `RerankedHitCount` and `RerankApplied`.
- Updated DI wiring:
  - `rag/src/FunctionsApp/Program.cs`
  - Registered `IRetrievalReranker -> AzureOpenAIRetrievalReranker`.
- Updated tests:
  - `rag/tests/Rag.Tests/RagPipelineTests.cs`
  - Constructor updates for new reranker dependency.
  - Added test: reranking applied when candidate pool > top.
  - Added test: reranker failure falls back safely.

### Validation (April 7, 2026)

- `dotnet build rag/RagAssistant.sln` succeeded.
- `RagPipelineTests` passed (`6/6`).

### Next Immediate Validation Task

- Run live host smoke calls and verify diagnostics include rerank metadata:
  - `GET /api/health`
  - `POST /api/ingest`
  - `POST /api/ask`
- Confirm out-of-scope refusal behavior remains unchanged with reranking enabled.
