# APM Package Changelog

## 0.1.0 - 2026-04-09

### Added
- Initial APM manifest and schema:
  - `rag/apm/package.manifest.json`
  - `rag/apm/package.manifest.schema.json`
- Local manifest validation script:
  - `rag/apm/Validate-ApmManifest.ps1`
- MCP tool catalog for local package v1:
  - `health_check`
  - `ask_question`
  - `ingest_documents`

### Changed
- Package status set to `validated` for local MCP v1 readiness.

### Validation
- Manifest schema validation passes locally.
- MCP server build passes.
- Inspector validation completed for health, ask, and ingest tools.
