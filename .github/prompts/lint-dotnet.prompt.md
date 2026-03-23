---
description: "Run /lint-dotnet to review .NET code against active workspace and scoped instructions"
mode: ask
parameters:
  - name: target
    description: "File path or pasted code snippet to review"
    required: true
---

# /lint-dotnet

Review {{target}} using all active instructions in this workspace.

## Required Output Format

Rule 1 (4-Space Indent): PASS/FAIL with line-specific reason
Rule 2 (const and typing): PASS/FAIL with line-specific reason
Rule 3 (Document Methods): PASS/FAIL with line-specific reason
Rule 4 (Keep Methods Small): PASS/FAIL with line-specific reason
Rule 5 (Avoid dynamic): PASS/FAIL with line-specific reason

Suggested Fixes:
- Include concrete edits with line references
- Prioritize highest-impact fixes first

Highest-Impact Issue: one sentence
Quick Win: one fix that can be applied in under 5 minutes

## Additional Behavior

- If the target is a config file, include: "Config file type detected: <type>" before findings.
- If any security issue is found (secrets/credentials), always list it first.
- Do not skip any rule. If uncertain, mark FAIL and explain.
