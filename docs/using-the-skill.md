# Using the dotnet-craft skill

The skill is a folder, `skill/dotnet-craft/`, holding one `SKILL.md` (frontmatter `name` and `description`, then the text). The
hosts below read that same file; copy the folder, do not edit it per host.
The short shell examples below assume you run them from a CrapSharp checkout. For another repository, use the installation
procedure below so the package and skill are copied from the correct source.

The skill only helps in an environment that can read the repository **and run commands**. It tells the agent to run
`dotnet crapsharp`, `dotnet stryker` and `npx jscpd`; in a chat without a shell it can be read, but those checks cannot be run,
and the agent must say so instead of claiming a result.

## Install in another .NET repository (Windows PowerShell)

Use a separate CrapSharp checkout as the source. Replace the two paths below, then run these commands from the target
repository's root. Skip `dotnet new tool-manifest` if the target already has a local tool manifest. If either tool is already in
that manifest, skip its `dotnet tool install` command.

```powershell
$crapSharpCheckout = 'C:\path\to\CrapSharp'
$targetRepository = 'C:\path\to\OtherProject'
dotnet pack (Join-Path $crapSharpCheckout 'src/CrapSharp/CrapSharp.csproj') -c Release -o (Join-Path $crapSharpCheckout 'nupkg')
Set-Location $targetRepository
dotnet new tool-manifest
dotnet tool install CrapSharp --version 0.1.0-preview.1 --add-source (Join-Path $crapSharpCheckout 'nupkg')
dotnet tool install dotnet-stryker --version 5.0.0
New-Item -ItemType Directory -Force '.agents/skills' | Out-Null
Copy-Item -LiteralPath (Join-Path $crapSharpCheckout 'skill/dotnet-craft') -Destination '.agents/skills/dotnet-craft' -Recurse
dotnet crapsharp --help
```

The `.agents/skills/` copy is used by Codex and by GitHub Copilot in VS Code. If you also use Claude Code, copy the same source
folder to `.claude/skills/dotnet-craft/`. In the target repository, keep the local tool manifest and the skill directory under
version control once the pilot succeeds. Do not add another `dotnet-craft` copy under `.github/skills/` when Copilot already reads
the `.agents/skills/` copy.

The package is not published on NuGet. A colleague or CI runner cannot restore CrapSharp from the manifest alone: they also
need the built package from a reachable source and must run `dotnet tool restore --add-source <package-directory>`. Replace the
local folder with a published NuGet feed when CrapSharp is released. Verify installation separately from a real CRAP gate: the
latter also needs a supported coverage collector in the target project.

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

A repository copy is visible only when Codex runs in that repository. To make the skill available in all your local Codex
projects on the same machine, install it once at the personal path instead. That personal installation is not shared with other
developers; commit a project copy in each repository where the team should receive the skill.

```bash
mkdir -p .agents/skills && cp -r skill/dotnet-craft .agents/skills/dotnet-craft
```

Invoke it from the Codex CLI or the IDE extension with `$` or `/skills`. In the desktop app, an enabled skill also appears in
the `/` menu; select `/dotnet-craft` there, or type `$` to select the skill. ChatGPT skill mentions also support `@`. The skill
can be picked implicitly from its `description`. Keeping the file only under `skill/dotnet-craft/` does not install it: copy it
to `.agents/skills/` (or the personal location above) first. See the [OpenAI skill guide](https://learn.chatgpt.com/docs/build-skills)
and [desktop slash command reference](https://learn.chatgpt.com/docs/reference/slash-commands). Standalone skills are available
in the desktop app, Codex CLI and IDE extension; web and mobile need the skill packaged in a plugin. Check that your surface can
run shell commands before relying on the gates.

## GitHub Copilot in VS Code

In VS Code with GitHub Copilot in **Agent** mode, use a project skill at `.github/skills/dotnet-craft/SKILL.md` or a personal skill
at `~/.copilot/skills/dotnet-craft/SKILL.md`. VS Code also discovers project skills under `.claude/skills/` and `.agents/skills/`,
but `.github/skills/` is the native Copilot location. See the [VS Code Agent Skills documentation](https://code.visualstudio.com/docs/agent-customization/agent-skills).
If this project already has `.agents/skills/dotnet-craft/` for Codex, use that same installation for Copilot in VS Code; do not
install a second copy with the same name in `.github/skills/`.

```bash
mkdir -p .github/skills && cp -r skill/dotnet-craft .github/skills/dotnet-craft
```

Open a fresh Copilot Agent chat in the project. Check that `dotnet-craft` appears in **Open Customizations > Skills** or the `/`
menu, then invoke `/dotnet-craft` with a small C# task. In another fresh chat, describe the same task without naming the skill to
check automatic selection. Inspect the commands Copilot actually ran and their exit codes; merely finding the skill does not prove
that the CRAP, mutation and duplication checks ran. These steps are documented, **not yet tried by the maintainers**.

[GitHub Copilot CLI](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-skills) also supports project
skills in `.github/skills/`, `.claude/skills/` or `.agents/skills/`, and personal skills in `~/.copilot/skills/` or
`~/.agents/skills/`. Test its tool access and invocation separately before claiming CLI support. Other Copilot surfaces are not
covered by the VS Code test.

## The tools the skill calls

The Windows example above builds and installs both local .NET tools. On another OS, use the same `dotnet pack`,
`dotnet tool install` and `dotnet tool restore --add-source` commands with paths valid on that system.

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
| Claude Code | tried once on a small test project, invoked with `/dotnet-craft`: it added tests first, refactored, ran the CRAP gate, the tests, Stryker and jscpd and reported honestly what it could not verify; automatic loading **not tried** |
| Codex desktop app | tried once on the same test project, invoked with `$dotnet-craft`: it refactored with a boundary test, reached CRAP 1.00 and jscpd 0 clones, and reported what the sandbox blocked (coverage collection, Stryker); the CRAP gate needed a run outside the sandbox; automatic loading **not tried** |
| Codex CLI / IDE extension | documented from the official page; **not tried** |
| ChatGPT desktop | documented; **not tried**; shell access not stated by the documentation |
| GitHub Copilot in VS Code (Agent mode) | documented from the official page; **not tried** |
| GitHub Copilot CLI | installation path documented from GitHub's official page; **not tried** |
| Other Copilot surfaces | **not tried**; support must be checked separately |
| ChatGPT web and mobile | not supported without a plugin; a skill without a shell cannot run the gates |

Not covered anywhere: installation from NuGet (the package is not published), global tool installation (`-g`), and a pilot on a
project other than this repository.
