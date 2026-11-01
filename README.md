# CrapSharp

A .NET counterpart of [unclebob/ub-mode](https://github.com/unclebob/ub-mode):

- **`crapsharp`**, a command-line tool that computes the **CRAP** score of every C# method and can act as a gate
  ("every method strictly below N") in a terminal or in CI;
- **`skill/dotnet-craft`**, a skill (`SKILL.md`) describing the working discipline (strict TDD, refactoring, deterministic checks)
  and calling `crapsharp` and Stryker.NET.

> Status: the tool is usable from source and installs as a local `dotnet tool` from a local package. Publication on NuGet is not done yet
> (see [PLAN.md](PLAN.md), written in French).

## The score

`CRAP = CC² × (1 − coverage)³ + CC`, where CC is the cyclomatic complexity of the method and coverage the fraction of its lines
run by the tests. A score must be **strictly below N** to pass: a method with CC ≥ N fails even at 100 % coverage, so the code has
to be simplified. The exact conventions (what counts for complexity, how coverage is matched, what is excluded) are specified in
[PLAN.md section 4](PLAN.md).

## Usage

Requirements: the .NET 10 SDK. Test projects must produce a Cobertura report: `coverlet.collector` (VSTest), or
`Microsoft.Testing.Extensions.CodeCoverage` / `coverlet.MTP` (Microsoft.Testing.Platform).

```bash
dotnet pack src/CrapSharp -c Release -o ./nupkg
dotnet new tool-manifest          # skip if the repository already has a manifest
dotnet tool install CrapSharp --version 0.1.0-preview.1 --add-source ./nupkg
dotnet crapsharp . --threshold 10
```

The package is not published on NuGet yet. `dotnet publish src/CrapSharp -o ./out` followed by `dotnet ./out/CrapSharp.dll …` also works.

The tool runs the tests with coverage, prints a table (worst score first) and writes `.metrics/crap.json`.

| Option | Effect |
|---|---|
| `[path]` | folder, `.sln`, `.csproj` or `.cs` (default: `.`) |
| `--threshold <N>` | exit code 2 unless every method is strictly below N (N finite, above 0) |
| `--changed` | only the `.cs` files modified according to git |
| `--filter <fragment>` | only the files whose path contains the fragment |
| `--exclude <fragment>` | leave out the files whose path contains the fragment (repeatable), for example engine code your tests cannot run |
| `--coverage <report.xml>` | use this Cobertura report (repeatable), run nothing |
| `--use-existing-coverage` | reuse the reports of an earlier run |
| `--coverage-command "<cmd>"` | run this command and read the `*cobertura*.xml` reports it writes |
| `--no-coverage` | complexity only |

One coverage source at a time. Exit codes: **0** ok, **1** error (option, path, no readable report, no file matched, no member,
empty selection), **2** threshold not met, **3** tests or coverage command failed.

The JSON holds `scope` (complete or partial report), `coverage` (the reports used) and, per method, the score truncated for
display (`crap`) and the exact score compared with the threshold (`crapRaw`).

## Using the skill

Installation in Claude Code and Codex, and a table of what has and has not been tested: [docs/using-the-skill.md](docs/using-the-skill.md).

## Mutation testing

Mutations are left to [Stryker.NET](https://stryker-mutator.io/), without rewriting it; the procedure is in
[`skill/dotnet-craft/SKILL.md`](skill/dotnet-craft/SKILL.md).

## Duplication

Duplicate code is checked with [jscpd](https://github.com/kucherenko/jscpd) (Node.js), as described in the skill:
`npx --yes jscpd@5.4.2 src --format csharp --min-tokens 50 --min-lines 3 --threshold 0 --ignore "**/bin/**,**/obj/**"`.
It finds exact clones only, not renamed ones.

## Known limits

- Coverage is by **lines** only; matching by line ranges is an approximation (two members on the same line share the same result).
- Syntactic analysis without MSBuild: no `DefineConstants`, linked files or `Directory.Build.props`.
- Complexity follows CrapSharp's convention; it differs from CA1502 (Visual Studio) on five constructs (PLAN.md section 4.2).
- Tried on Windows and Linux (CI), with xUnit and MSTest, and once on a real Godot game; macOS, NUnit, xunit.v3 and TUnit are not verified.

## Development

```bash
dotnet test                                              # fast tests
CRAPSHARP_E2E=1 dotnet test --filter Category=EndToEnd   # end-to-end tests on tests/fixtures (slow, needs NuGet)
dotnet publish src/CrapSharp -c Release -o ./out && dotnet ./out/CrapSharp.dll . --threshold 10   # the tool scores itself: must exit 0
```

Tracking: [PLAN.md](PLAN.md) (specification, roadmap, decisions), [PROGRESS.md](PROGRESS.md) (state),
[REVIEWS.md](REVIEWS.md) (reviews). These three files are in French.

## License

[MIT](LICENSE). You may use, copy, modify and distribute this project, but the copyright line (Nicolas Friederich - CrapSharp) and the license text
must be kept in all copies or substantial portions of it. This covers the tool, the skill and the documentation. The package depends on
Roslyn (`Microsoft.CodeAnalysis`), which keeps its own MIT license.
