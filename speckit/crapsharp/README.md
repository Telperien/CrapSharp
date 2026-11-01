# CrapSharp extension for Spec Kit

Adds `/speckit.crapsharp.gate` (skills agents: `/speckit-crapsharp-gate`) and an optional `after_implement` hook that runs it.
The command runs `crapsharp`, Stryker.NET and jscpd on the C# code just implemented, and says what it could not verify.

**Status: not validated.** See `docs/using-the-skill.md` of the repository for what has been tested.

```bash
specify extension add --dev path/to/CrapSharp/speckit/crapsharp
```

Add the principles of `constitution-rules.md` to the project's constitution. The command needs the `crapsharp` tool
(`dotnet tool install CrapSharp --prerelease`), a test project with a coverage collector, and Node.js for jscpd. Without Spec Kit,
use CrapSharp and the `dotnet-craft` skill directly.
