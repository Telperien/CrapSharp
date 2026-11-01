---
name: dotnet-craft
description: A three-phase .NET development discipline (make it work through strict TDD, make it right through refactoring guided by deterministic checks, practice cleanly) with a per-method CRAP gate (crapsharp) and mutation testing (Stryker.NET). Use it to write, fix or refactor C# when quality must be verified by commands rather than by impression.
---

# dotnet-craft

A .NET counterpart of the ub-mode discipline. This text is original: it follows the approach, not the wording.
It needs access to the repository and a terminal. Without the ability to run commands the checks below cannot be run, and you
must not claim to have run them.

## 1. Make it work

1. **Smallest possible change.** One reason to touch the code at a time. Do not tidy up on the side.
2. **Strict TDD.** First a test that fails, and fails for the right reason (read the failure message: the expected behavior is
   missing, not a compilation or configuration error). Then the smallest code that makes it pass. Only then clean up.
3. **Data first.** Describe inputs and outputs before classes. A table of examples beats a diagram.
4. **Attack the premise.** Before writing, ask whether the request is the right one, whether the code already exists, whether the
   problem goes away by deleting something.
5. **Remove before adding.** Delete dead code and unused options before building on top of them.
6. **Architecture.** Dependencies point toward the domain; effects (files, processes, network, clock) go through a narrow
   interface that a test can replace.

## 2. Make it right

1. **Fix causes**, not symptoms: a failing test points to a design flaw before it points to a code flaw.
2. **Verifiable units**: small, with explicit inputs and outputs.
3. **Test behavior, not implementation.** A test must survive a refactoring that changes nothing observable. Never write a test
   only to lower a number (see 3.1).
4. **Refactoring**: freeze behavior with tests, change the structure, prove nothing changed (same green tests, same checks).
5. **Deterministic checks**, in this order, once the tests are green:
   - per-method CRAP under the threshold (section 3);
   - mutation tests on the code you touched (section 4);
   - duplication: there is no tool in this repository yet, so do it by hand (look for similar code before writing more).

## 3. CRAP check with `crapsharp`

`CRAP = CC² × (1 − coverage)³ + CC`. CC is the cyclomatic complexity of the method, coverage a fraction of lines.
The goal is **strictly below 10** for every method: a score equal to 10 fails.

```bash
crapsharp . --threshold 10
```

Until the `dotnet tool` package is published, publish the tool from this repository and run
`dotnet <folder>/CrapSharp.dll . --threshold 10` (`dotnet publish src/CrapSharp -o <folder>`). The commands were tried that way;
installation as a `dotnet tool` has not been verified yet.

By default the tool runs the tests with coverage itself (coverlet, or the Microsoft.Testing.Platform extensions), prints a table
(worst score first) and writes `.metrics/crap.json`. Useful options: `--changed` (files modified according to git),
`--filter <fragment>`, `--use-existing-coverage`, `--coverage <report.xml>`, `--no-coverage` (complexity only, no threshold).

| Code | Meaning | What to do |
|---|---|---|
| 0 | Everything is under the threshold | Carry on |
| 1 | Error (option, path, no coverage report, no file matched, empty selection) | Read the message: nothing was measured |
| 2 | At least one method scores 10 or more | See 3.1 |
| 3 | The tests or the coverage command failed | Fix the tests first; the scores shown may be wrong |

A warning is not a success: "coverage report does not mention … file(s)" flags a production project that the tests do not run
(its methods score 0 %); "may be stale" flags sources edited after the report was written.

### 3.1 What to do about a high score

- If **CC ≥ 10**, no amount of coverage is enough: reduce the complexity (simplify the decisions, delete unneeded code, extract a
  responsibility). A very simple lookup table may justify an exception, to be discussed with the human, not decided alone.
- If **CC < 10**, the coverage needed to get under the threshold is `1 − ((10 − CC) / CC²)^(1/3)` (examples: CC 4 → more than
  27.9 %; CC 8 → more than 68.5 %). Write **behavior** tests for the uncovered paths; if a path has no observable behavior, ask
  whether it should stay at all.
- Run again and check that the score drops for the right reason.

### 3.2 What this score does not prove

- Coverage is **by lines**: 100 % of the lines does not establish that every branch was exercised. That is why mutation testing
  (section 4) complements CRAP.
- Matching is done by line ranges: two members on the same line share the same result, and a lambda or local function counts in
  the method that contains it.
- Complexity follows `crapsharp`'s convention (one decision per `case`, `catch`, `switch` expression arm, `??=`, `or` pattern…).
  It can differ from Visual Studio or CA1502 on those constructs; do not compare the numbers.
- A low CRAP is a risk indicator, not a proof of correctness: an uncovered method of CC 1 already scores 2.

## 4. Mutation testing with Stryker.NET

A mutant is a modification of the production code (a `>` becomes `>=`, a condition is inverted…). A solid test **kills** it; a
mutant that **survives** reveals behavior the tests do not protect.

Installation (local tool, versions pinned by the manifest):

```bash
dotnet new tool-manifest
dotnet tool install dotnet-stryker
```

Run it **from the test project folder**:

```bash
dotnet stryker --project <Production.csproj> --break-at 80
```

Stryker exits with code 2 when the mutation score is below `--break-at`. Tried on a fixture of this repository with Stryker.NET
5.0.0 (57 mutants created, 36 killed, 9 survived, score 65.45 %; exit code 0 with `--break-at 60`, 2 with `--break-at 100`).

1. **When.** Not in the fast TDD loop: the check takes from ten seconds to several minutes. Run it on the touched project before
   proposing the change, and at the end of each refactoring.
2. **Kill the survivors.** Open the HTML report (`StrykerOutput/.../mutation-report.html`), read each survivor: write the behavior
   test that kills it, or conclude the code is unnecessary and delete it.
3. **No coverage.** A "NoCoverage" mutant points at code no test runs: cover it or delete it.
4. **Threshold.** `--break-at 80` is a starting proposal, to be confirmed by the human for the project; never lower the threshold
   just to get a pass.

## 5. Practice

- Keep a **journal** of actions and their reasons (what was tried, measured, abandoned): short and dated.
- **Save context**: run commands with options that limit their output, read files by range.
- Clean up the working copies and temporary files that the checks create.
- **Small, ordered commits**, each with: why, what, and the proof (which command, which result). A commit holds one change; its
  message describes the behavior, not an internal task number.
- Never announce something as verified without having read the command's result. Say explicitly what was not run.
