# RAG Learning Project — Tuesday Checkpoint

**Date:** March 24, 2026  
**Status:** Functions host verified, real Azure OpenAI planner + answer agent active, safety still stubbed  
**Next Step:** Replace `StubSafetyReviewerAgent` with Azure AI Content Safety and add refusal-path smoke test to routine checks

## Session Continuity Rule (Permanent)

`CHECKPOINT.md` must always be kept up to date at all times after each meaningful change or validation step, so progress is recoverable if the session breaks.

---

## What We Built This Session

### Phase: Functions Host Validation + Real Answer Agent
We validated the Azure Functions isolated worker host end-to-end and confirmed the full HTTP -> pipeline -> retriever -> planner -> answer -> safety -> response chain is running locally. The answer agent is now Azure OpenAI-backed (planner + answer are real), while safety remains stubbed.

**Files Created:**
- `FunctionsApp/Agents/AzureOpenAIPlannerAgent.cs` — uses Azure OpenAI to decide if context is sufficient (Answerable/Refuse)
- `FunctionsApp/Agents/AzureOpenAIAnswerAgent.cs` — uses Azure OpenAI to generate grounded answers from retrieved context
- `FunctionsApp/Agents/StubSafetyReviewerAgent.cs` — always approves (stub)
- `FunctionsApp/Functions/AskFunction.cs` — POST /ask HTTP trigger, JSON in/out
- `FunctionsApp/Data/SampleDocumentStore.cs` — 5 seeded documents (RAG, Azure Search, Azure OpenAI, chunking)
- `.devcontainer/devcontainer.json` — Codespace setup with .NET 8, Node, Core Tools, C# + Copilot extensions

**Files Modified:**
- `FunctionsApp/FunctionsApp.csproj` — includes required Azure/OpenAI packages and project references
- `FunctionsApp/Program.cs` — DI registration: retriever + Azure OpenAI planner + Azure OpenAI answer + stub safety + pipeline
- `FunctionsApp/local.settings.json` — local runtime values for host and Azure OpenAI settings
- `rag/global.json` — `rollForward: latestFeature` for SDK flexibility

### What Works
✅ RAG pipeline orchestration (retrieve -> plan -> answer -> review)  
✅ Keyword search retriever with in-memory seed data  
✅ Functions host starts and discovers both routes (`Ask`, `Health`)  
✅ `GET /api/health` responds successfully  
✅ `POST /api/ask` responds with grounded answer + citations  
✅ Unit tests pass (`RagPipelineTests`: 3 passed)  
✅ Functions app build succeeds (0 errors)  
✅ DI wiring complete (real Azure OpenAI planner + real Azure OpenAI answer + stub safety)  

### What Doesn't Work Yet
❌ `ISafetyReviewerAgent` is still a stub (no Azure AI Content Safety integration yet)  
❌ Host reports `azure.functions.webjobs.storage` unhealthy when `AzureWebJobsStorage` is empty (HTTP flow still works)  
❌ No dedicated refusal-path smoke test has been automated in routine validation yet  

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

**Short Term (next 2–3 sessions):**
1. ✅ **Get Functions host running** on VM/Codespace
2. ✅ **Replace `IAnswerAgent` stub with Azure OpenAI grounded generation**
3. **Replace `ISafetyReviewerAgent` stub with Azure AI Content Safety**
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

## Next Session Checklist

- [x] Run `cd rag/src/FunctionsApp && func start`
- [x] Confirm Functions banner appears (Ask & Health routes visible)
- [x] Test POST /ask, confirm 200 with answer JSON
- [x] Test GET /health, confirm 200 OK
- [ ] Test explicit refusal path (`{"question":"kubernetes deployment strategies"}`), verify `answered=false`
- [ ] Replace `StubSafetyReviewerAgent` with Azure AI Content Safety review
- [ ] Add `POST /ingest` endpoint
- [ ] Keep `CHECKPOINT.md` updated immediately after each meaningful progress step

---

This file is now the source of truth for session continuity. Update it continuously as work progresses.
