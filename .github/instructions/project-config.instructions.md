---
description: "Use when: reviewing or editing .NET project files, build configuration, or application settings files"
applyTo: "**/*.csproj,**/Directory.Build.props,**/Directory.Build.targets,**/global.json,**/appsettings*.json"
---

# .NET Project and Config File Rules

Apply these checks when reviewing project or configuration files:

## Project Files (.csproj, Directory.Build.props, Directory.Build.targets)

1. TargetFramework must be explicit — do not leave it unset or use wildcards.
2. Nullable must be set to enable. If it is absent, flag it as a required addition.
3. ImplicitUsings must be declared (enable or disable) — ambiguity causes inconsistent builds.
4. Do not embed secrets, connection strings, or credentials in project files.
5. Package references must include an explicit Version attribute. Floating versions (e.g. *) are not allowed.
6. TreatWarningsAsErrors is recommended for production projects. Flag if absent.

## Application Settings (appsettings*.json)

1. Do not store secrets, passwords, API keys, or connection strings with real credentials in any appsettings file committed to source control.
2. Use placeholder values (e.g. "REPLACE_ME" or "") for any sensitive key so the shape is documented without exposing data.
3. Logging levels must be explicitly configured — do not rely on defaults.
4. Environment-specific overrides belong in appsettings.{Environment}.json, not in the base file.

## Response Additions

When this instruction is active, prefix the response with:
- Config file type detected: (e.g. "csproj", "appsettings")
- Highest-risk finding first (security issues always rank above style issues)
