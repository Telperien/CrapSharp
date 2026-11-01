# Changelog

All notable changes to CrapSharp. The format follows [Keep a Changelog](https://keepachangelog.com/); versions follow
[Semantic Versioning](https://semver.org/), and `0.x` versions may change their options without notice.

## 0.1.0-preview.1

First public preview.

### Added
- Per-method CRAP score for C# (`CRAP = CC² × (1 − coverage)³ + CC`) and a strict gate: `--threshold N` exits with code 2
  unless every method is strictly below N. Exit codes: 0 ok, 1 error, 2 threshold not met, 3 tests or coverage command failed.
- Cyclomatic complexity from the Roslyn syntax tree, with documented differences from CA1502.
- Coverage from Cobertura reports: the tool runs the tests itself (coverlet through VSTest, or the Microsoft.Testing.Platform
  extensions), or reads reports with `--coverage`, `--use-existing-coverage` or `--coverage-command`.
- Selecting what to score: `--changed` (uncommitted files), `--changed-since <ref>` (everything changed since a branch, for a
  pull request check), `--filter`, `--exclude`.
- `.metrics/crap.json` with the exact score (`crapRaw`), the coverage sources and a `scope` that says whether the report is
  complete.
- Installation as a .NET tool (`dotnet tool install CrapSharp`, command `crapsharp`).
- The `dotnet-craft` skill (Claude Code, Codex, GitHub Copilot): strict TDD and refactoring discipline that runs the CRAP gate,
  Stryker.NET and jscpd, and reports what it could not verify.

### Known limits
- Tried on Windows and Linux (CI) with xUnit and MSTest. macOS, NUnit, xunit.v3 and TUnit are not verified.
- Coverage is by lines; duplication checks (jscpd) miss renamed clones.
- A partial run (`--changed`, `--filter`, `--exclude`) overwrites `.metrics/crap.json`; `scope.complete` is then false.
- The skill needs a shell that can run `dotnet`, `npx` and the coverage tools; inside a restrictive sandbox its gates cannot run.
