# Measurement conventions

What CrapSharp does today. Each rule has a unit test unless stated otherwise.

## Formula and threshold

- `CRAP = CC² × (1 − coverage)³ + CC`, coverage as a fraction (0 to 1). Example: CC 3, 75 % coverage → 3.14.
- `--threshold N`: **fails if any raw score is ≥ N** (the goal is "everything strictly below N", as in ub-mode). The comparison
  uses the unrounded value.
- The display (table and JSON) is **truncated**, never rounded up: 9.996 shows as 9.99 and 10.004 as 10.00. What you read is
  therefore consistent with the verdict, for any threshold of at most two decimals.
- Consequence: a method with CC ≥ N fails even at 100 % coverage. Its complexity must be reduced (simplify the decisions, delete
  unused code or extract a responsibility), not only tested more.

## Cyclomatic complexity (Roslyn syntax analysis, no semantic model)

CC = 1 + the number of decision points in the member.

| Construct | Counts | Example → CC |
|---|---|---|
| `if`, `else if` | +1 each (`else`: 0) | `if (a) …; else if (b) …;` → 3 |
| `for`, `foreach`, `while`, `do` | +1 | → 2 |
| `catch` | +1 per clause (`finally`: 0) | two `catch` → 3 |
| `case` of a `switch` | +1 per label, `default`: 0 | 3 `case` + `default` → 4 |
| `switch` expression arm | +1, except the final `_ =>` without `when` | `1 =>, 2 =>, _ =>` → 3 |
| `when` (guard) | +1 on top of the `case` or arm | `case int n when n > 0:` → 3 |
| `?:` | +1 | → 2 |
| `&&`, `\|\|` | +1 each | `a && b ? … : …` → 3 |
| `??`, `??=` | +1 each | `s ?? s ?? "a"` → 3 |
| `?.` | +1 each | `s?.Trim()?.Length ?? 0` → 4 |
| `and` / `or` patterns | +1 each | `is "a" or "b" ? … : …` → 3 |
| lambdas, local functions | counted **in the enclosing member** | |
| `goto`, `break`, `return`, `throw`, plain `is`, `not` | 0 | |

**Measured differences with Microsoft's CA1502 analyzer** (fixture `constructs`, pinned in `ComplexityOracleTests`):

| Construct | CrapSharp | CA1502 |
|---|---|---|
| `switch` statement with 4 `case` + `default` | 5 (`default`: 0) | 6 (`default` counted) |
| `??=` | +1 | 0 (`first ?? second ?? "a"` then `??=`: 3 in total instead of 4) |
| `catch` (two clauses) | +1 each → 3 | nothing (1) |
| `switch` expression arms | +1 each except the final `_` → 3 | nothing (1) |
| `or` pattern (`is "a" or "b"`) | +1 → 2 | nothing (1) |

The other constructs of the fixture (`if`/`else if`, `for`/`foreach`/`while`/`do`, `when`, `?:`, `&&`/`||`, `??`, `?.`, lambda)
give the same number. **The convention was kept.** The
[source of Microsoft's metrics computation](https://source.dot.net/Microsoft.CodeAnalysis.Analyzers/src/roslyn/src/RoslynAnalyzers/Utilities/Compiler/CodeMetrics/MetricsHelper.cs.html)
confirms that CA1502 counts case clauses, coalesce, conditional, conditional access, loops and some binary operators, but not the
four constructs listed as "nothing (1)". Microsoft's documentation says modern Visual Studio and CLI metrics use a similar source
analysis; these five examples were not measured in the Visual Studio interface itself.

The convention is a **syntactic approximation of the complexity of the control-flow graph**, not a strict "McCabe" definition that
prescribes one count per keyword. The [NIST publication by Watson and McCabe](https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication500-235.pdf)
counts the distinct outcomes of a multi-way decision and in general models several labels leading to the same body as one outcome.
**CrapSharp's choice: one label = one outcome**, including in `case 3: case 4:`; each declared value stays a distinct entry to verify.
This is the other consistent modelling the NIST text mentions, but it can give a higher CC than the model that groups labels. The
`default` or implicit no-match path already occupies the base outcome. The same publication recommends **measuring** a large
`switch`, then allows a justified exception to the threshold for a simple multi-way decision; it advises against removing its
branches from the score. `catch` and modern C# constructs need a published modelling convention, since the historical text does not
impose a single syntactic count. ReportGenerator shows a different complexity (it reads coverlet's IL-based figure) and always
exits 0, so it cannot serve as the gate.

## What is measured

- **Members** with a body: methods, constructors, destructors, operators, conversions, accessors (`get`, `set`, `init`, `add`,
  `remove`), expression-bodied properties and indexers. Abstract members, `extern` members and auto-properties: nothing to measure.
- **Top-level statements** (`Program.cs` without a class): **one entry**, `<top-level statements>`, from the first to the last
  statement (the compiler makes a single `Main` of them).
- **Excluded**: members carrying `[ExcludeFromCodeCoverage]` or `[ExcludeFromCoverage]`, on the member, on its property or event,
  or on an enclosing type (simple name, `Attribute`-suffixed or qualified). Coverlet removes them from its report by default:
  without this rule they would be scored at 0 %. **Confirmed** on the `excluded` fixture (coverlet 6.0.0): an excluded member,
  property and type are absent from the Cobertura file and from the table.
- **Files**: the `.cs` files of non-test project folders, outside `bin`, `obj`, `.git`, `node_modules`, and outside `*.g.cs`,
  `*.generated.cs`, `*.designer.cs`. `--filter`, `--exclude`, `--changed` and `--changed-since` narrow this selection.
- **Not measured, known**: lambdas in field or property initializers; code in inactive `#if` branches (no symbol defined); linked
  files outside the project folder; projects of a `.sln` located outside the root.

## Coverage

- Source: **Cobertura** reports from coverlet (`dotnet test --collect:"XPlat Code Coverage"`), or from the Microsoft.Testing.Platform
  coverage extensions.
- A member's coverage = lines hit / lines reported within the member's `[start, end]`. This is an **approximation**: it includes the
  member's lambdas and local functions; two members on the same line share the same lines.
- A member with no reported line = 0 % (crapper's rule).
- Several reports: merged, keeping the maximum hits per line. A multi-target `dotnet test` writes one report per framework: by
  default the tool empties `.metrics/coverage` before running the tests and reads **all** the reports found there. With
  `--use-existing-coverage` it keeps, for each results folder, the reports written in the 10 minutes preceding the most recent one
  (one run); an older run is discarded.
- A test project that succeeds without producing a report (no `coverlet.collector`, for example): a warning names it; what only its
  tests cover is scored 0 % (missing coverage can only raise scores). If no test project produces a report: error (code 1).
- Limits measured on fixtures: a continuation line of a statement that creates a lambda can be reported as hit although the lambda
  body did not run (`};`, fixture `closures`); two members on the same line share the same result, so a member never called can look
  covered (fixture `sameline`).
- **Branch** coverage (`condition-coverage`): ignored (lines only).
- If some analyzed files are absent from the report, their members stay at 0 % and a warning names the projects concerned. A
  production project that no test runs is a normal case of this rule.
- If **no** analyzed file appears in the reports: error (code 1, no report produced). Check in particular the `/_/…` paths of
  deterministic builds. Coverlet 6.0.0 puts real paths back in its report by default; only `DeterministicReport=true` keeps the
  `/_/…` paths there (fixtures `deterministic` and `deterministic-paths`).
- Empty selection: no production file found → code 1; a `--filter` that matches nothing → code 1 (probable typo); `--changed`
  with no modified file → code 0, nothing is run and the previous JSON is kept. A `--changed`, `--changed-since`, `--filter` or
  `--exclude` report carries `scope.complete = false` in the JSON.

## Exit codes

| Code | Meaning | Report produced? |
|---|---|---|
| 0 | OK | yes |
| 1 | Error: invalid options, missing path, no readable coverage report, no analyzed file matched, `--changed` outside a git repository | no |
| 2 | `--threshold N`: at least one method at N or more | yes |
| 3 | `dotnet test` or the coverage command failed: coverage may be incomplete | yes |

Priority when several cases apply: 1, then 3, then 2. Unknown options, a missing value (including a known option in place of the
value), a second path, a threshold that is not a finite number above 0, or several coverage sources (`--coverage`,
`--coverage-command`, `--use-existing-coverage`, `--no-coverage`): code 1, with a pointer to `--help`.

## JSON (`.metrics/crap.json`)

`scope` (`complete`, `changed`, `changedSince`, `filter`, `exclude`, `files`), `coverage` (`source`, `reports`: the Cobertura paths
used) and `entries`. Each entry carries `crap` and `coverage` **truncated** for display, and `crapRaw`, the exact score compared
with the threshold. A partial run (`--changed`, `--changed-since`, `--filter`, `--exclude`) overwrites the file; `scope.complete`
is then false.
