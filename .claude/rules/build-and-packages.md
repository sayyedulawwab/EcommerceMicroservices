---
paths:
  - "**/*.csproj"
  - "Directory.Build.props"
  - "Directory.Packages.props"
  - "global.json"
  - ".editorconfig"
  - "**/Dockerfile"
  - "**/Migrations/**"
---

# Build and packages

- **Vulnerable packages fail every build**: locally, in CI and in the Docker builds. NU1901–NU1904 are errors; only NU1900 (vulnerability data couldn't be downloaded) stays a warning. There are no automatic package upgrades, so the audit is the signal. When it fails:
  1. Upgrade the package: change its `PackageVersion` in `Directory.Packages.props`. For a transitive package, add a `PackageVersion` for it; transitive pinning moves every project to that version. Or remove the package that pulls it in.
  2. Only when no fixed version exists yet, or an urgent fix can't wait for the upgrade to be tested, suppress that one advisory in `Directory.Packages.props` with `<NuGetAuditSuppress Include="https://github.com/advisories/GHSA-..." />` and a comment saying why it's safe for now and when to remove it. Never turn the audit off or add NU1901–NU1904 to `WarningsNotAsErrors`.
- Each service Dockerfile must `COPY` `Directory.Build.props`, `Directory.Packages.props` and every referenced `.csproj`, including `src/SharedKernel/SharedKernel.csproj`, before `dotnet restore`.
- `dotnet ef` scaffolds code that Sonar flags (e.g. S4581); `.editorconfig` turns such rules off under `**/Migrations/**`.
