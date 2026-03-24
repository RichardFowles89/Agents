# RAG Learning Project — Tuesday Checkpoint

**Date:** March 24, 2026 (Updated 17:30)  
**Status:** Functions host verified, real Azure OpenAI pipeline active, POST /ingest endpoint functional  
**Next Step:** Create comprehensive test suite, then prepare for cloud deployment

## Session Continuity Rule (Permanent)

`CHECKPOINT.md` must always be kept up to date at all times after each meaningful change or validation step, so progress is recoverable if the session breaks.

---

## What We Built This Session

### Phase 1: Functions Host Validation + Real Answer Agent + Ingest
We validated the Azure Functions isolated worker host end-to-end and confirmed the full HTTP -> pipeline -> retriever -> planner -> answer -> safety -> response chain. All three Azure OpenAI agents are real. The retriever now supports dynamic document ingestion.

**Files Created (Phase 1):**
- `FunctionsApp/Agents/AzureOpenAIPlannerAgent.cs` — uses Azure OpenAI to decide if context is sufficient
- `FunctionsApp/Agents/AzureOpenAIAnswerAgent.cs` — uses Azure OpenAI to generate grounded answers
- `FunctionsApp/Agents/AzureOpenAISafetyReviewerAgent.cs` — uses Azure OpenAI to check for hallucinations/harm
- `FunctionsApp/Functions/AskFunction.cs` — POST /ask HTTP trigger
- `FunctionsApp/Functions/IngestFunction.cs` — POST /ingest HTTP trigger (NEW)
- `FunctionsApp/Data/SampleDocumentStore.cs` — 5 seeded documents
- `Rag.Core/Models/IngestRequest.cs` — request model for /ingest endpoint
- `FunctionsApp/Functions/AskFunction.cs` — POST /ask HTTP trigger, JSON in/out
- `FunctionsApp/Data/SampleDocumentStore.cs` — 5 seeded documents (RAG, Azure Search, Azure OpenAI, chunking)
- `.devcontainer/devcontainer.json` — Codespace setup with .NET 8, Node, Core Tools, C# + Copilot extensions

**Files Modified (Phase 1):**chunker + all three Azure OpenAI agents + pipeline
- `FunctionsApp/local.settings.json` — local runtime values for host and Azure OpenAI settings
- `rag/global.json` — `rollForward: latestFeature` for SDK flexibility
- `Rag.Core/Contracts/ISearchRetriever.cs` — added `IngestAsync` method  
- `Rag.Infrastructure/Retrieval/KeywordSearchRetriever.cs` — mutable index with thread-safe locking for ingest
- `Rag.Tests/RagPipelineTests.cs` — added `IngestAsync` stub to `FakeRetriever`I planner + Azure OpenAI answer + Azure OpenAI safety + pipeline
- `FunctionsApp/local.settings.json` — local runtime values for host and Azure OpenAI settings
- `rag/global.json` — `rollForward: latestFeature` for SDK flexibility
+ dynamic ingestion  
✅ Functions host starts and discovers all three routes (`Ask`, `Health`, `Ingest`)  
✅ `GET /api/health` responds successfully  
✅ `POST /api/ask` responds with grounded answer + citations  
✅ `POST /api/ingest` accepts documents, chunks them, adds to index  
✅ Unit tests pass (`RagPipelineTests`: 5 passed)  
✅ Functions app build succeeds (0 errors)  
✅ DI wiring complete (real Azure OpenAI planner + answer + safety + document chunker)  
✅ Safety reviewer checks for hallucinations, harmful content, and accuracy  
✅ Ingested documents are retrievable and used for generating answers  

### What Doesn't Work Yet
❌ Host reports `azure.functions.webjobs.storage` unhealthy when `AzureWebJobsStorage` is empty (HTTP flow still works)  
❌ `/ingest` endpoint does not persist documents across restarts (in-memory only)
❌ Host reports `azure.functions.webjobs.storage` unhealthy when `AzureWebJobsStorage` is empty (HTTP flow still works)  
❌ No dedicated refusal-path smoke test has been explicitly validated yet (needs test run)  
❌ `POST /ingest` endpoint not yet created  

### Environment Notes
- **Current machine:** Core Tools v4.8.0, .NET SDK 10.0.201
- **Function runtime:** v4, isolated worker
- **Port:** 7071

---

## The Architecture (Why Each Piece Exists)

**Request -> Pipeline:**
```
POST /api/ask {"question": "...", "top": 5}
    ↓
AskFunction deserializes -> calls RagPipeline.AskAsync()
```

**Pipeline Flow:**
1. **Retriever** (`ISearchRetriever`) — finds docs (today: keyword scorer; later: Azure AI Search)
2. **Planner** (`IPlannerAgent`) — decides if we have enough context (today: Azure OpenAI)
3. **Answer Agent** (`IAnswerAgent`) — generates answer from hits (today: Azure OpenAI)
4. **Safety Reviewer** (`ISafetyReviewerAgent`) — checks for harm/hallucination (today: stub; later: Azure AI Content Safety)

**Why partial stubs?**
- Full chain works end-to-end while remaining components are swapped incrementally
- Each interface can be replaced with minimal DI changes in `Program.cs`
- Tests pass with fake implementations, proving pipeline logic is stable

Current state: planner and answer use real Azure OpenAI; safety remains stubbed.

---

## How to Run It (Current)

### Prerequisites
```bash
dotnet --version          # 8+ (currently 10.0.201)
func --version            # 4.x (currently 4.8.0)
```

### Start the Host
```bash
cd rag/src/FunctionsApp
func start
```

**Success looks like:**
```
Functions:
    Ask: [POST] http://localhost:7071/api/ask
    Health: [GET] http://localhost:7071/api/health
```

### Smoke Tests

**Health check:**
```bash
GET http://localhost:7071/api/health
```

**Ask happy path:**
```bash
POST http://localhost:7071/api/ask
Content-Type: application/json

{"question": "what is RAG?", "top": 3}
```

Expected: `answered: true` with answer text and citations.

**Ask refusal path (pending verification in routine):**
```json
{"question": "kubernetes deployment strategies"}
```
Expected: `answered: false` with `refusalReason`.

---

## Broader Learning Path (Updated)

**S✅ **Add `POST /ingest` endpoint** to seed the retriever from
1. ✅ **Get Functions host running** on VM/Codespace
2. ✅ **Replace `IAnswerAgent` stub with Azure OpenAI grounded generation**
3. ✅ **Replace `ISafetyReviewerAgent` stub with Azure OpenAI safety review**
4. **Add `POST /ingest` endpoint** to seed the retriever from real documents

**Medium Term (1–2 weeks):**
- `ISearchRetriever` -> Azure AI Search (vector + keyword hybrid)
- `IEmbeddingService` -> Azure OpenAI embeddings
- Evaluation harness (measure answer quality, latency, cost)
- Red-team tests (jailbreak attempts, off-topic, hallucination)

**Long Term (ongoing):**
- Copilot RAG-specific customizations (`azure-functions.instructions.md`, `rag.instructions.md`, `lint-rag.prompt.md`)
- Unit test coverage for Azure integrations
- Azure deployment (Function App, Search, OpenAI, Content Safety)

---

## Files to Know

**Core Logic:**
- `rag/src/Rag.Core/Pipeline/RagPipeline.cs` — orchestration (the "brain")
- `rag/src/Rag.Infrastructure/Retrieval/KeywordSearchRetriever.cs` — retrieval seam
- `rag/src/FunctionsApp/Agents/AzureOpenAIPlannerAgent.cs` — real planner
- `rag/src/FunctionsApp/Agents/AzureOpenAIAnswerAgent.cs` — real answer generator

**Next Edits:**
- `FunctionsApp/Agents/StubSafetyReviewerAgent.cs` — replace with Azure AI Content Safety implementation
- `FunctionsApp/Functions/IngestFunction.cs` (new) — POST /ingest endpoint
- `FunctionsApp/Program.cs` — DI swap for safety reviewer

**Testing & Validation:**
- `rag/tests/Rag.Tests/RagPipelineTests.cs` — pipeline unit tests
- Postman or curl for HTTP smoke tests

---

## Current Session Checklist

- [x] Run `cd rag/src/FunctionsApp && func start`
- [x] Confirm Functions banner appears (Ask & Health routes visible)
- [x] Test POST /ask, confirm 200 with answer JSON
- [x] Add `POST /ingest` endpoint
- [x] Test ingest endpoint with new documents
- [x] Verify ingested documents are retrievable by /ask endpoint
- [x] Keep `CHECKPOINT.md` updated immediately after each meaningful progress step

### Session Results & Test Evidence (March 24, 17:30)
- **Refusal path:** `{"question":"kubernetes deployment strategies"}` → `answered=false`, `refusalReason` ✅
- **Happy path (seeded):** `{"question":"what is RAG?"}` → `answered=true`, 3 citations ✅
- **Ingest test 1:** Document ingested, 1 chunk created ✅
- **Ingest test 2:** Azure testing document ingested and successfully retrieved for question "What is Azure testing?" ✅
- **Safety reviewer:** Approved all responses (refusal, seeded answer, ingested answer)
### Refusal & Happy Path Test Results (March 24, 17:25)
- **Refusal path:** POST /api/ask with `{"question":"kubernetes deployment strategies"}` → `answered=false`, `refusalReason="Planner refused to answer with current context."` ✅
- **Happy path:** POST /api/ask with `{"question":"what is RAG?"}` → `answered=true`, answer text + 3 citations from seeded docs ✅
- **Safety reviewer:** Approved both refusal and answer responses ✅

---

## Session Handoff (End of Day — March 24, 2026)

**Current State:**
- Functions host is running and discoverable (`func start` from `rag/src/FunctionsApp`)
- All 5 unit tests pass
- Full pipeline operational: Ask → Ingest → Health routes all working
- Safety reviewer integrated with real Azure OpenAI

**To Resume Tomorrow:**
1. Kill the background `func start` terminal if still running
2. Run `cd c:\repos\Agents\rag\src\FunctionsApp && func start` to restart the host
3. Begin with next steps from "Medium Term" section (Azure AI Search, embeddings, or evaluation harness)

**Files in Good State:**
- [CHECKPOINT.md](CHECKPOINT.md) — fully updated with session results
- [Program.cs](rag/src/FunctionsApp/Program.cs) — DI complete
- [IngestFunction.cs](rag/src/FunctionsApp/Functions/IngestFunction.cs) — tested and working
- [AzureOpenAISafetyReviewerAgent.cs](rag/src/FunctionsApp/Agents/AzureOpenAISafetyReviewerAgent.cs) — tested and working
- [KeywordSearchRetriever.cs](rag/src/Rag.Infrastructure/Retrieval/KeywordSearchRetriever.cs) — mutable and thread-safe
- All tests passing

This file is now the source of truth for session continuity. Update it continuously as work progresses.
