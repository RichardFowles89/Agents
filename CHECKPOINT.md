# RAG Learning Project — Monday Checkpoint

**Date:** March 23, 2026  
**Status:** RAG core pipeline wired into Functions host, seed data ready, Codespace config complete  
**Next Step:** Start Functions host and test POST /ask endpoint with Postman

---

## What We Built This Session

### Phase: Functions Host Integration
We wired the orchestrated RAG pipeline (`RagPipeline`) into an Azure Functions isolated worker host with three stub agent implementations. The full HTTP → pipeline → retriever → planner → answer → safety → response chain is now ready.

**Files Created:**
- `FunctionsApp/Agents/StubPlannerAgent.cs` — decides if retrieval hits exist (Answerable/Refuse)
- `FunctionsApp/Agents/StubAnswerAgent.cs` — builds answer from first hit + citations
- `FunctionsApp/Agents/StubSafetyReviewerAgent.cs` — always approves (stub)
- `FunctionsApp/Functions/AskFunction.cs` — POST /ask HTTP trigger, JSON in/out
- `FunctionsApp/Data/SampleDocumentStore.cs` — 5 seeded documents (RAG, Azure Search, Azure OpenAI, chunking)
- `.devcontainer/devcontainer.json` — Codespace setup with .NET 8, Node, Core Tools, C# + Copilot extensions

**Files Modified:**
- `FunctionsApp/FunctionsApp.csproj` — added `Rag.Infrastructure` reference
- `FunctionsApp/Program.cs` — DI registration: retriever + 3 stubs + pipeline
- `FunctionsApp/local.settings.json` — empty `AzureWebJobsStorage` (HTTP-only)
- `rag/global.json` — added `rollForward: latestFeature` for SDK flexibility

### What Works
✅ RAG pipeline orchestration (retrieve → plan → answer → review)  
✅ Keyword search retriever with in-memory seed data  
✅ All 5 unit tests passing  
✅ Full build succeeds (0 errors)  
✅ DI wiring complete  
✅ `POST /ask` function signature ready  

### What Doesn't Work Yet
❌ Azure Functions host not discovering/loading functions (Core Tools v4.0.5455 issue on Windows)  
❌ `func start` fails with "No job functions found" despite public function classes  
❌ Local smoke test on original machine inconclusive — work moved to VM/Codespace  

### Environment Notes
- **Original machine:** Core Tools v4.0.5455, .NET 8.0.0, winget upgrade partially stuck
- **VM/Codespace:** Will use .NET 8 base image + Core Tools v4 via npm, guaranteed clean install
- **Port:** 7071 (AWS Functions default, forwarded in devcontainer)

---

## The Architecture (Why Each Piece Exists)

**Request → Pipeline:**
```
POST /api/ask {"question": "...", "top": 5}
    ↓
AskFunction deserializes → calls RagPipeline.AskAsync()
```

**Pipeline Flow:**
1. **Retriever** (`ISearchRetriever`) — finds docs (today: keyword scorer; later: Azure AI Search)
2. **Planner** (`IPlannerAgent`) — decides if we have enough context (today: stub; later: GPT-4o)
3. **Answer Agent** (`IAnswerAgent`) — generates answer from hits (today: stub; later: GPT-4o grounded)
4. **Safety Reviewer** (`ISafetyReviewerAgent`) — checks for harm/hallucination (today: stub; later: Azure AI Content Safety)

**Why stubs?**
- Full chain works end-to-end without Azure dependencies
- Each interface can be swapped 1:1 when ready for real Azure services
- Tests pass with fake implementations, proving the pipeline logic is solid

---

## How to Run It (VM or Codespace)

### Prerequisites (handled by devcontainer.json in Codespace)
```bash
# On VM, run manually:
dotnet --version          # Should be 8.x
func --version            # Should be 4.x (use: npm install -g azure-functions-core-tools@4)
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

Azure Functions Core Tools
...
Host started (139ms)
```

### Test with Postman

**Request:**
```
POST http://localhost:7071/api/ask (or https://[codespace-url]/api/ask)
Content-Type: application/json

{"question": "what is RAG?", "top": 3}
```

**Expected Response (200 OK):**
```json
{
  "answered": true,
  "answerText": "Based on 3 retrieved document(s): RAG stands for Retrieval-Augmented Generation...",
  "citations": [
    {"sourceId": "doc-rag-overview", "title": "What is RAG", "sectionPath": "Overview > RAG", "sourceUrl": null},
    ...
  ],
  "refusalReason": null
}
```

**Test refusal path** (question that matches nothing in seed data):
```json
{"question": "kubernetes deployment strategies"}
```
You should get `"answered": false` with refusalReason explaining why.

---

## Broader Learning Path (After This Checkpoint)

**Short Term (next 2–3 sessions):**
1. ✅ **Get Functions host running** on VM/Codespace
2. **Replace stubs with real Azure services:**
   - `IPlannerAgent` → Azure OpenAI GPT-4o call
   - `IAnswerAgent` → Azure OpenAI grounded generation
   - `ISafetyReviewerAgent` → Azure AI Content Safety
3. **Add `POST /ingest` endpoint** to seed the retriever from real documents

**Medium Term (1–2 weeks):**
- `ISearchRetriever` → Azure AI Search (vector + keyword hybrid)
- `IEmbeddingService` → Azure OpenAI embeddings
- Evaluation harness (measure answer quality, latency, cost)
- Red-team tests (jailbreak attempts, off-topic, hallucination)

**Long Term (ongoing):**
- Copilot RAG-specific customizations (`azure-functions.instructions.md`, `rag.instructions.md`, `lint-rag.prompt.md`)
- Unit test coverage for Azure integrations
- Azure deployment (Function App, Search, OpenAI, Content Safety)

---

## Key Design Insights

**1. Retrieval Quality is a Data Problem First**  
The seed documents determine what questions can be answered. Better chunking and sourcing = better RAG system. The model is secondary.

**2. Interfaces Enable Incremental Swaps**  
Each component is behind an interface. Stub → Real Azure takes 1 line of code in `Program.cs`. The pipeline never changes.

**3. RAG Expertise = Knowing When NOT to Answer**  
The planner and safety reviewer are as important as the answer generator. Most RAG failures come from answering confidently when you shouldn't.

**4. Copilot Customizations Travel with the Code**  
`.github/copilot-instructions.md` and agent definitions live in the repo. On the VM, they're immediately available — Copilot will use them to guide reviews and suggestions.

---

## Files to Know

**Core Logic (read-only after this checkpoint):**
- `rag/src/Rag.Core/Pipeline/RagPipeline.cs` — orchestration (the "brain")
- `rag/src/Rag.Infrastructure/Retrieval/KeywordSearchRetriever.cs` — retrieval seam
- `rag/src/FunctionsApp/Agents/Stub*.cs` — stub implementations (replace these next)

**Next Edits:**
- `Program.cs` — DI registration lines (swap in real implementations)
- `FunctionsApp/Agents/RealPlannerAgent.cs` (new) — Azure OpenAI planner
- `FunctionsApp/Agents/RealAnswerAgent.cs` (new) — Azure OpenAI answer generator
- `FunctionsApp/Functions/IngestFunction.cs` (new) — POST /ingest endpoint

**Testing & Validation:**
- `rag/tests/Rag.Tests/RagPipelineTests.cs` — pipeline unit tests (all pass ✅)
- Postman or curl for HTTP smoke tests

---

## Copilot on the VM

**Automatically available:**
- `.github/copilot-instructions.md` — 5-rule C# style baseline (4-space indent, const/typing, XML docs, method size, no dynamic)
- `.github/instructions/csharp.instructions.md` — C# scoped rules
- `.github/prompts/lint-*.prompt.md` — domain-specific linting prompts
- `.github/agents/smart-reviewer.agent.md` — conditional code review agent

**How to use:**
Open a `.cs` file → Copilot Chat → ask "@smart-reviewer analyze this file" or just reference the instructions in your requests.

---

## Troubleshooting

**"No job functions found"**  
→ Likely Core Tools version mismatch. On VM, ensure `func --version` is 4.8.0+. Reinstall: `npm install -g azure-functions-core-tools@4 --unsafe-perm true`

**"Port 7071 in use"**  
→ Kill the process: `lsof -ti:7071 | xargs kill -9` (Linux/Mac) or `netstat -ano | findstr :7071` (Windows)

**Postman 401 on Codespace URL**  
→ Port visibility must be Public. In Codespaces Ports tab, right-click 7071 → Port Visibility → Public.

---

## Next Session Checklist

- [ ] On VM, run `cd rag/src/FunctionsApp && func start`
- [ ] Confirm Functions banner appears (Ask & Health routes visible)
- [ ] Test POST /ask in Postman, confirm 200 with answer JSON
- [ ] Test GET /health, confirm 200 OK
- [ ] (If refusal path fails, debug planner: may be hitting empty retriever)
- [ ] Once working, replace `StubPlannerAgent` with real GPT-4o call
- [ ] Document insights in session notes

---

**Good luck on the VM! All the code is there. Copilot will be available the moment you open a `.cs` file. Happy building.** 🚀
