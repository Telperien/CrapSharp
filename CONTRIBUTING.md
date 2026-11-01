# Contributing to CrapSharp

Thanks for your interest. CrapSharp is a small tool with a strict rule: it must pass its own gate.

## Before you start

- Open an issue first for anything beyond a small fix, so the direction can be agreed before you write code.
- Measurement conventions (what counts for complexity, how coverage is matched) are in [docs/conventions.md](docs/conventions.md).
  A change to a convention needs a justification and a test, and is never made silently.

## Build and test

```bash
dotnet test                                              # fast tests
CRAPSHARP_E2E=1 dotnet test --filter Category=EndToEnd   # end-to-end tests on tests/fixtures (slow, needs NuGet)
dotnet publish src/CrapSharp -c Release -o ./out
dotnet ./out/CrapSharp.dll . --threshold 10              # the tool scores itself: must exit 0
```

On Windows, run the published DLL (`dotnet publish`), not `dotnet run`, which locks the DLL.

## What a pull request needs

1. **Tests first.** A behavior change starts with a test that fails for the right reason. A test is never written only to lower
   a score.
2. **The self-check passes**: every method of CrapSharp strictly below CRAP 10, with exit code 0.
3. **No new duplication**: `npx --yes jscpd@5.4.2 src --format csharp --min-tokens 50 --min-lines 3 --threshold 0 --ignore "**/bin/**,**/obj/**"`.
4. **Mutation score does not drop.** From `tests/CrapSharp.Tests`, `dotnet stryker --project CrapSharp.csproj --break-at 80`
   (the repository's score is about 97 %).
5. **Code and comments in English.** Keep comments to what the code cannot say.
6. **State what you did not verify.** If you could not run a check (a platform, a framework, a sandbox), say so in the pull request.

## Commits

Commit messages describe what the change does and why, in English, in the imperative ("Add --exclude", not "Step 7").

## Skill and Spec Kit extension

The skill (`skill/dotnet-craft/SKILL.md`) is copied to `.agents/skills/dotnet-craft/` for Codex and Copilot; change the source
and copy it, do not edit both. The Spec Kit extension lives in `speckit/crapsharp/`. What has and has not been tried is in
[docs/using-the-skill.md](docs/using-the-skill.md); update it when you test a new host.

## License

By contributing you agree that your contribution is licensed under the [MIT license](LICENSE) of the project.
