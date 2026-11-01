---
description: "Run the CRAP gate, Stryker.NET and jscpd on the C# code just implemented and report what could not be verified"
---

# CrapSharp quality gate

Check the C# code changed by the implementation with deterministic commands. The scores come from the commands, not from your
impression of the code. If the `dotnet-craft` skill is available, follow its sections 3 (CRAP), 4 (Stryker.NET) and 5 (jscpd)
and its rules for refactoring; this command only fixes the order and the reporting.

## User Input

```text
$ARGUMENTS
```

If the input names a branch or a commit, use it as `<ref>` below; otherwise use the default branch of the repository.

## Steps

1. **Starting point.** Run the project's tests. Do not go on while they fail or while a test command does not finish: tell the user
   which one, and, if the environment blocks it (a sandbox), ask permission to run it outside the sandbox.
2. **CRAP gate**, on what this feature changed:

   ```bash
   dotnet tool restore            # or: dotnet tool install CrapSharp --prerelease
   dotnet crapsharp . --changed-since <ref> --threshold 10
   ```

   Read the exit code yourself (never pipe the gate into `tail` or `head`): 0 passes, 2 means at least one method scores 10 or
   more, 3 means the tests or the coverage command failed, 1 is an error. `--no-coverage` is not the gate.
3. **If a method scores 10 or more**, simplify it behind tests that pin its behavior first (boundaries included), then run the
   gate again. Never add a test only to lower a number.
4. **Mutation testing** on the files you touched (from the test project directory):

   ```bash
   dotnet stryker --project <Project.csproj> --mutate "**/File.cs" --break-at 80
   ```

   Report the score. A score of 0 with "capture failed" after a simplification usually means no mutable operator is left in the
   tested code, not a failure of the tool.
5. **Duplication:**

   ```bash
   npx --yes jscpd@5.4.2 src --format csharp --min-tokens 50 --min-lines 3 --threshold 0 --ignore "**/bin/**,**/obj/**"
   ```

## Report

End with the commands you ran and their results, and list the checks you could not run and why. Never say the gate passed
without a real score from step 2.
