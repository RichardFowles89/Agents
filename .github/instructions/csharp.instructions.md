---
description: "Use when: reviewing C# source files for style, maintainability, and API documentation quality"
applyTo: "**/*.cs"
---

# C# Source Review Rules

Apply these checks when reviewing C# files:

1. Enforce 4-space indentation and no tabs.
2. Prefer explicit types where var does not improve clarity.
3. Require XML docs for public types and public members.
4. Flag methods over 30 lines and suggest extracting helpers.
5. Disallow dynamic unless there is a clear interoperability reason.

## Response Additions

When this instruction is active, include:
- A short summary of the highest-impact issue first.
- A "Quick Win" fix that can be applied in under 5 minutes.
