# Publication (tags → Release + packages)

## Principe

Un **push de tag** `vX.Y.Z` déclenche le workflow `.github/workflows/release.yml` qui :

1. Aligne la version des packages NuGet sur le tag
2. Pack + push vers **GitHub Packages** (`nuget.pkg.github.com/Bari77`)
3. Crée une **GitHub Release** avec les `.nupkg` en pièces jointes

Les deux packages (`GamersCommunity.Core` et `GamersCommunity.Core.Logging`) **partagent la même version** que le tag (ex. `v9.5.0` → `9.5.0`).

## Publier

```bash
git tag v9.5.0
git push origin v9.5.0
```

## Consommer (équipes jeu)

`nuget.config` (déjà dans les repos jeu) pointe vers GitHub Packages. Authentification locale (une fois) :

```powershell
# PAT GitHub avec scope read:packages (et write:packages pour les leads)
dotnet nuget update source github `
  --username VOTRE_USER `
  --password ghp_xxx `
  --store-password-in-clear-text
```

Puis dans le `.csproj` :

```xml
<PackageReference Include="GamersCommunity.Core" Version="9.5.0" />
<PackageReference Include="GamersCommunity.Core.Logging" Version="9.5.0" />
```

Aucun checkout du repo Core n’est requis pour les équipes jeu.
