---
description: "Run /lint-security to review .NET code and config files for common security risks"
mode: ask
parameters:
  - name: target
    description: "File path or pasted code snippet to review"
    required: true
---

# /lint-security

Review {{target}} for common .NET security risks using all active instructions in this workspace.

## Required Output Format

Rule 1 (Hardcoded Secrets): PASS/FAIL with line-specific reason
Rule 2 (Credentials in Config): PASS/FAIL with line-specific reason
Rule 3 (Input Validation): PASS/FAIL with line-specific reason
Rule 4 (SQL/Command Injection): PASS/FAIL with line-specific reason
Rule 5 (Insecure Deserialization): PASS/FAIL with line-specific reason
Rule 6 (Cryptography and Secret Storage): PASS/FAIL with line-specific reason
Rule 7 (Sensitive Data Exposure): PASS/FAIL with line-specific reason
Rule 8 (Authentication/Authorization): PASS/FAIL with line-specific reason

Suggested Fixes:
- Include concrete edits with line references
- Prioritize highest-risk fixes first

Highest-Risk Issue: one sentence
Severity: CRITICAL/HIGH/MEDIUM/LOW
Quick Win: one fix that can be applied in under 5 minutes

## Additional Behavior

- If the target is a config file, include: "Config file type detected: <type>" before findings.
- If any secret, token, connection string, or credential is found, always list it first.
- If there is evidence of possible remote code execution, injection, or credential leakage, set Severity to CRITICAL or HIGH.
- If a rule cannot be proven safe from the provided file alone, mark FAIL and explain what is missing.
- Do not skip any rule.