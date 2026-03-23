---
description: "Run /lint-architecture to review .NET code and config files for architectural quality and maintainability"
mode: ask
parameters:
  - name: target
    description: "File path or pasted code snippet to review"
    required: true
---

# /lint-architecture

Review {{target}} for .NET architecture quality using all active instructions in this workspace.

## Required Output Format

Rule 1 (Layer Boundaries): PASS/FAIL with line-specific reason
Rule 2 (Dependency Direction): PASS/FAIL with line-specific reason
Rule 3 (Coupling and Cohesion): PASS/FAIL with line-specific reason
Rule 4 (Abstractions and Interfaces): PASS/FAIL with line-specific reason
Rule 5 (Dependency Injection Usage): PASS/FAIL with line-specific reason
Rule 6 (Configuration Placement): PASS/FAIL with line-specific reason
Rule 7 (Error Handling Boundaries): PASS/FAIL with line-specific reason
Rule 8 (Naming and Project Organization): PASS/FAIL with line-specific reason
Rule 9 (Architectural Boundary Integrity): PASS/FAIL with line-specific reason
- Evaluate whether domain/application boundaries are preserved across data flow, transactions, and side effects.
- Verify that orchestration stays in the application/service layer, domain logic stays in the domain layer, and infrastructure concerns are isolated.
- Flag boundary leaks such as domain types depending on infrastructure APIs, transactional logic split across unrelated layers, and cross-layer side effects (I/O, logging, persistence) inside domain logic.
- If full verification is not possible from a single file, mark FAIL and list the exact additional files needed.

Suggested Fixes:
- Include concrete edits with line references
- Prioritize highest-impact architecture fixes first

Highest-Impact Issue: one sentence
Architecture Risk: HIGH/MEDIUM/LOW
Quick Win: one fix that can be applied in under 5 minutes

## Additional Behavior

- If the target is a config file, include: "Config file type detected: <type>" before findings.
- If a dependency cycle or layer violation is detected, list it first.
- If architecture cannot be fully assessed from a single file, mark uncertain rules as FAIL and state what additional files are needed.
- For Rule 9, always include an "Evidence Checked" subsection listing inspected namespaces, type dependencies, and call paths.
- Do not skip any rule.
