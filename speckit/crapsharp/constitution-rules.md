# Constitution rules for C# projects (CrapSharp)

Add these principles to the project's constitution (`.specify/memory/constitution.md`), for example by passing them to
`/speckit.constitution`. Adjust the threshold to your team's.

### Quality gate (NON-NEGOTIABLE)
Every C# method has a CRAP score strictly below 10, measured by `crapsharp` with real coverage. A feature is not done until
`dotnet crapsharp . --changed-since <ref> --threshold 10` exits with 0, and the exit code was read, not assumed.

### Test-first refactoring
A method that is too complex is simplified behind tests that pin its current behavior, boundaries included. A test is never
written only to lower a score.

### Honest reporting
A check that could not run (sandbox, missing tool, tests that never finish) is reported as not run. It is never counted as passed.
