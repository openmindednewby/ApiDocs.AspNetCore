# Building and Publishing ApiDocs.AspNetCore

## Prerequisites

- .NET 10 SDK
- nuget.org API key with push permissions for `ApiDocs.AspNetCore*`

## Build

```bash
dotnet restore src/ApiDocs.AspNetCore/ApiDocs.AspNetCore.csproj
dotnet build src/ApiDocs.AspNetCore/ApiDocs.AspNetCore.csproj -c Release --no-restore
dotnet pack  src/ApiDocs.AspNetCore/ApiDocs.AspNetCore.csproj -c Release -o artifacts --no-build
```

## Test

```bash
dotnet test ApiDocs.AspNetCore.slnx -c Debug
```

## Publish

```powershell
.\publish.ps1 -Bump patch -ApiKey YOUR_KEY
```

The script cleans, auto-bumps the version in `Directory.Build.props`, builds, packs,
pushes to nuget.org (`--skip-duplicate`), and rolls back the version on failure.

## Vendored assets

The `src/ApiDocs.AspNetCore/Assets/` directory holds the vendored browser bundles
(`scalar.standalone.js`, `swagger-ui-bundle.js`, `swagger-ui.css`) plus the docs chrome
(`docs.css`, `docs.js`). They are embedded into the assembly (`<EmbeddedResource>`), so the
package is fully self-contained — no CDN at runtime.

To refresh a vendored bundle to a newer version, replace the file, confirm it contains no
external resource loads that would break CSP, and bump the package version.

## Versioning

Semantic versioning: `patch` for fixes, `minor` for new features, `major` for breaking changes.

## Release Checklist

- [ ] `dotnet test` green
- [ ] `dotnet build -c Release` clean (0 warnings)
- [ ] `.\publish.ps1 -Bump <part> -ApiKey ...`
- [ ] Wait 5–15 min for nuget.org indexing
- [ ] Verify on https://www.nuget.org/packages/ApiDocs.AspNetCore
- [ ] Bump consumers' `Directory.Packages.props`
