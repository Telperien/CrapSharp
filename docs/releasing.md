# Releasing CrapSharp to NuGet

The package is prepared but has not been published. Keep `0.1.0-preview.1` until the external pilot and the supported agent
hosts have been checked. Choose a stable version only after that validation. Publishing a GitHub Release is the explicit
publication step; pushing a tag alone does not run the NuGet workflow.

## Before the first release

1. Check that `CrapSharp` is available as a package ID on [NuGet.org](https://www.nuget.org/packages/CrapSharp) and that the
   chosen version is unused. Confirm the public name and version.
2. Update `<Version>` and `<PackageReleaseNotes>` in `src/CrapSharp/CrapSharp.csproj`, plus the installation examples in
   `README.md` and `docs/using-the-skill.md`. Remove the README's pending-release wording before packaging. The skill
   currently documents installation from a local source; update it when NuGet installation has actually been verified.
3. Run `dotnet test --configuration Release`, the end-to-end tests (`CRAPSHARP_E2E=1`), and
   `dotnet pack src/CrapSharp -c Release -o artifacts/nuget`. Install that package in a clean directory and run
   `crapsharp --help` and a gate on a fixture. The release workflow repeats these checks.
4. In the repository settings, create an Actions variable `NUGET_USER` with the **nuget.org profile name** of the publishing
   account (not its email address). No long-lived NuGet API key is stored in GitHub.
5. On [nuget.org Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing), create a GitHub
   policy for the owner that will publish the package. Use repository owner `Telperien`, repository `CrapSharp`, and workflow
   file name `publish-nuget.yml` (just the file name, without `.github/workflows/`). Leave Environment blank: the workflow
   does not use a GitHub environment. Ensure the policy permits publishing the `CrapSharp` package, including its first
   version. If the repository or NuGet owner changes, change the policy accordingly.

## Publish

1. Merge the release changes, then create a tag `v<Version>` at that commit, such as `v0.1.0-preview.1`.
2. Create and **publish** a GitHub Release for that tag. Mark a version containing `-` as a prerelease. The
   `.github/workflows/publish-nuget.yml` workflow checks that the tag matches the project version and that the prerelease
   flag agrees with it, runs tests, packs, installs the package into a new directory, requests a temporary key through
   `NuGet/login@v1`, and pushes the `.nupkg` to NuGet.org.
3. Wait for NuGet indexing. From a new empty directory on another machine or clean runner, verify installation from the
   public feed, without `--add-source`:

   ```bash
   dotnet tool install CrapSharp --tool-path ./tools --version 0.1.0-preview.1
   ./tools/crapsharp --help
   ```

   On Windows use `./tools/crapsharp.exe`. For a stable release, replace the example version. Then run a real gate in a
   separate .NET project with a supported coverage collector; `--help` alone checks installation, not coverage or scoring.

The workflow needs GitHub's `id-token: write` permission and the matching NuGet policy. See the
[official trusted publishing instructions](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) if the login
step fails. A repeat push of the same package version will fail; publish a new version for another release.
