# RAG Learning Project Checkpoint

Date: March 25, 2026
Status: Local pipeline validated, Azure OpenAI agents integrated, Azure AI Search migration active with startup index auto-create enabled; blocked on ingest with invalid document key validation
Next Step: Fix chunk ID sanitization (remove colons and special chars), then retry ingest/ask smoke tests

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

- AzureWebJobsStorage can report unhealthy when unset locally; HTTP endpoints still function.
- Ingested data is currently in-memory and does not persist across host restarts.
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

## Next Work Queue

1. Baseline validation each session start:
   - Build + test + smoke checks for Ask/Health/Ingest.
2. Comprehensive tests:
   - Pipeline branch coverage (refuse, no hits, safety reject).
   - Retriever ingest/retrieval and concurrency tests.
   - Function endpoint validation/error-path tests.
3. Local reliability cleanup:
   - Address or document AzureWebJobsStorage local behavior clearly.
4. Persistence strategy:
   - Decide temporary persistence approach or document in-memory limitation explicitly.
5. Cloud readiness:
   - Define Azure config mapping for Function App, OpenAI, and Search.

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

- Azure Search Service: `rag20260325` in `rdfrdfrag-rg` (uksouth)
- Index Name: `rag-chunks`
- Settings in [rag/src/FunctionsApp/local.settings.json](rag/src/FunctionsApp/local.settings.json):
  - `AZURE_SEARCH_ENDPOINT`: https://rag20260325.search.windows.net
  - `AZURE_SEARCH_KEY`: (keep safe, not in source control)
  - `AZURE_SEARCH_INDEX_NAME`: rag-chunks

---

## Session Handoff Notes

When resuming:

1. Start from this file first.
2. Confirm current branch/repo status.
3. Run local validation (build/tests/smoke).
4. Continue from Next Work Queue.
5. Immediately update CHECKPOINT.md after each meaningful action.
