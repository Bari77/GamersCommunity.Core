# Publishing (tags → Release + packages)

## Principle

Pushing a `vX.Y.Z` tag runs `.github/workflows/release.yml`, which:

1. Aligns NuGet package versions with the tag
2. Packs + pushes to **GitHub Packages** (`nuget.pkg.github.com/Bari77`)
3. Creates a **GitHub Release** with `.nupkg` attachments

Both packages (`GamersCommunity.Core` and `GamersCommunity.Core.Logging`) **share the same version** as the tag (e.g. `v9.5.0` → `9.5.0`).

## Publish

```bash
git tag v9.5.0
git push origin v9.5.0
```

## Consume (game teams)

`nuget.config` (already in game repos) points at GitHub Packages. Local auth (once):

```powershell
# GitHub PAT with read:packages (and write:packages for leads)
dotnet nuget update source github `
  --username YOUR_USER `
  --password ghp_xxx `
  --store-password-in-clear-text
```

Then in the `.csproj`:

```xml
<PackageReference Include="GamersCommunity.Core" Version="9.5.0" />
<PackageReference Include="GamersCommunity.Core.Logging" Version="9.5.0" />
```

No Core repo checkout is required for game teams.
