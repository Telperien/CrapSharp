# Security policy

## Supported versions

CrapSharp is in preview. Only the latest published version receives fixes.

## What the tool does

`crapsharp` reads C# source files, reads Cobertura coverage reports, and **runs commands on your machine**: `dotnet test` (with a
coverage collector), `git`, and, with `--coverage-command`, a command you give it. It makes no network request of its own; the
commands it starts (restoring NuGet packages, for example) may.

Treat these as trusted-input boundaries:
- run `crapsharp` only on repositories whose build and tests you would run anyway: tests execute project code;
- `--coverage-command` runs the string you pass through your shell, as you would type it.

## Reporting a vulnerability

Please report privately through GitHub's
[private vulnerability reporting](https://github.com/Telperien/CrapSharp/security/advisories/new) for this repository, rather
than in a public issue. Include the version, the command line, and a minimal project that reproduces the problem. I aim to answer
within a week; this is a one-person project, so there is no service-level guarantee.

If private reporting is not available, open an issue that says only that you have a security report, without the details, and
a way to contact you will be arranged.
