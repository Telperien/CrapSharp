# Using the dotnet-craft skill

The skill is a folder, `skill/dotnet-craft/`, holding one `SKILL.md` (frontmatter `name` and `description`, then the text). Both
hosts below read that same file; copy the folder, do not edit it per host.

The skill only helps in an environment that can read the repository **and run commands**. It tells the agent to run
`dotnet crapsharp`, `dotnet stryker` and `npx jscpd`; in a chat without a shell it can be read, but those checks cannot be run,
and the agent must say so instead of claiming a result.

## Claude Code

From Anthropic's documentation (<https://code.claude.com/docs/en/skills>):

| Scope | Folder |
|---|---|
| This repository (commit it to share it) | `.claude/skills/dotnet-craft/SKILL.md` |
| All your repositories | `~/.claude/skills/dotnet-craft/SKILL.md` |

```bash
mkdir -p .claude/skills && cp -r skill/dotnet-craft .claude/skills/dotnet-craft
```

Claude loads the skill when the task matches its `description`, or you invoke it with `/dotnet-craft`.

## Codex and ChatGPT

From OpenAI's documentation (<https://learn.chatgpt.com/docs/build-skills>):

| Scope | Folder |
|---|---|
| This repository | `.agents/skills/dotnet-craft/SKILL.md` (also searched in parent folders up to the repository root) |
| Your user | `$HOME/.agents/skills/dotnet-craft/SKILL.md` |

```bash
mkdir -p .agents/skills && cp -r skill/dotnet-craft .agents/skills/dotnet-craft
```

Invoke it from the Codex CLI or the IDE extension with `$` or `/skills`; in the ChatGPT desktop app with `@`. It is also picked
implicitly from its `description`. The documentation lists standalone skills as working in the ChatGPT desktop app, the Codex CLI
and the IDE extension; web and mobile need the skill packaged in a plugin. That page does not say whether a skill can run shell
commands, so check that your surface can before relying on the gates.

## The tools the skill calls

```bash
dotnet pack src/CrapSharp -c Release -o ./nupkg
dotnet new tool-manifest
dotnet tool install CrapSharp --version 0.1.0-preview.1 --add-source ./nupkg
dotnet tool install dotnet-stryker --version 5.0.0
```

`jscpd` needs Node.js and is run through `npx --yes jscpd@5.4.2`. Pin the versions in `dotnet-tools.json` and commit that file.

## What was tested

"Tested" means run by the maintainers; the CI row is the workflow in `.github/workflows/ci.yml`.

| Item | Windows | Linux | macOS |
|---|---|---|---|
| `crapsharp`: unit tests, self-gate, end-to-end tests on the fixtures (including packing and installing it as a local tool) | tested (local, CI) | tested (CI) | not tested |
| jscpd duplication gate on `src` | tested (local, CI) | tested (CI) | not tested |
| Stryker.NET on a test project (clean clone, pinned manifest) | tested (local) | not tested | not tested |
| Test projects: xUnit and MSTest, VSTest and Microsoft.Testing.Platform | tested | tested (CI) | not tested |
| NUnit, xunit.v3, TUnit | not tested | not tested | not tested |

| Host | Status |
|---|---|
| Claude Code | documented from the official page (paths and invocation); **not tried by the maintainers** |
| Codex CLI / IDE extension | documented from the official page; **not tried** |
| ChatGPT desktop | documented; **not tried**; shell access not stated by the documentation |
| ChatGPT web and mobile | not supported without a plugin; a skill without a shell cannot run the gates |

Not covered anywhere: installation from NuGet (the package is not published), global tool installation (`-g`), and a pilot on a
project other than this repository.
