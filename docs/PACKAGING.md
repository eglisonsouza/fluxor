# Fluxor — GitHub Packages & Visual Studio

This guide covers publishing **fluxor** to **GitHub Packages** from the public repository and consuming the package in your applications.

## Prerequisites

1. Repository: [https://github.com/eglisonsouza/fluxor](https://github.com/eglisonsouza/fluxor) (public).
2. **Actions** enabled under **Settings → Actions → General** (allow all actions, **read and write** workflow permissions).
3. After the first publish, set the package visibility to **Public** under **Packages → fluxor → Package settings** (if GitHub does not do this automatically for a public repo).

## CI (develop + pull requests)

Workflow: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)

- Runs on pushes to **`main`** / **`develop`** and on PRs targeting those branches.
- Builds and packs (artifact only)—does **not** publish.

## Publish to GitHub Packages (merge to `main`)

Workflow: [`.github/workflows/publish.yml`](../.github/workflows/publish.yml)

**When you merge `develop` → `main`**, the workflow:

1. Calculates the version from **Conventional Commits** since the last tag ([calculate-version.sh](../.github/scripts/calculate-version.sh)).
2. Publishes **fluxor** to GitHub Packages.
3. Creates and pushes git tag **`vX.Y.Z`** automatically.

See **[VERSIONING.md](VERSIONING.md)** for commit message rules (`feat:`, `fix:`, `feat!:`, etc.).

### Manual run (optional)

1. GitHub → **Actions** → **Publish NuGet package** → **Run workflow** on **`main`**.
2. Enter **version** (e.g. `0.1.0`).

### Feed URL

```text
https://nuget.pkg.github.com/eglisonsouza/index.json
```

- **Package id:** `fluxor` (lowercase; required by GitHub Packages)
- **Display name:** Fluxor

CI uses `GITHUB_TOKEN` with `packages: write` to push the package.

## Link package to the repository

1. Repo → **Packages** (right sidebar).
2. Open **fluxor** → **Package settings** → connect to this repository if prompted.
3. For a **public** package, anyone can install it with a GitHub feed and a PAT with **`read:packages`** (no `repo` scope required).

## Personal Access Token (Visual Studio / CLI)

Create a **classic** PAT on GitHub:

| Scope | Why |
|--------|-----|
| `read:packages` | Download packages from GitHub Packages |

Fine-grained token: **Packages → Read** for this repository (or organization).

You do **not** need the `repo` scope when the repository and package are **public**.

Store the PAT securely (Credential Manager, environment variable, or `packageSourceCredentials`).

## Visual Studio — add the GitHub feed

### Method 1 — UI (user-wide)

1. **Tools** → **NuGet Package Manager** → **Package Manager Settings**.
2. **Package sources** → **+**
   - Name: `GitHub Fluxor`
   - Source: `https://nuget.pkg.github.com/eglisonsouza/index.json`
3. When prompted, authenticate:
   - **Username:** your GitHub username
   - **Password:** your PAT (not your GitHub account password)

### Method 2 — `nuget.config` (solution or user)

Copy [`nuget.config.example`](../nuget.config.example) to `nuget.config`, set your username and PAT, and restart Visual Studio.

User-level config:

```text
%AppData%\NuGet\NuGet.Config
```

### Install the package

1. Right-click project → **Manage NuGet Packages**.
2. Package source: **GitHub Fluxor**.
3. Search **`fluxor`** → install.

Package Manager Console:

```powershell
Install-Package fluxor -Source "GitHub Fluxor"
```

### `dotnet` CLI

```bash
dotnet nuget add source "https://nuget.pkg.github.com/eglisonsouza/index.json" \
  --name github-fluxor \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_PAT \
  --store-password-in-clear-text

dotnet add package fluxor --source github-fluxor
```

Use `--store-password-in-clear-text` only on trusted dev machines. In CI, use `GITHUB_TOKEN` or repository secrets.

## Consumer `PackageReference`

```xml
<PackageReference Include="fluxor" Version="1.0.0" />
```

Ensure the GitHub package source is configured so `dotnet restore` resolves the feed.

## Making the repository and package public

1. **Settings** → **General** → **Danger zone** → change repository visibility to **Public**.
2. **Packages** → **fluxor** → **Package settings** → **Change visibility** → **Public**.

After that, others can read the source on GitHub and install **fluxor** using the feed URL above.

## Troubleshooting

| Issue | Fix |
|--------|-----|
| 401 Unauthorized | PAT missing `read:packages`; wrong username; expired token |
| Package not listed | Package not published yet; wrong feed URL owner |
| NU1101 unable to find package | No GitHub source in NuGet config; wrong package id (use `fluxor`) |
| Duplicate publish | Workflow uses `--skip-duplicate`; bump version or remove old package version |
| .NET SDK not found in Actions | Runner uses `setup-dotnet` with `10.0.x` |
| Failed to queue workflow | Check [GitHub Status](https://www.githubstatus.com/); see [TROUBLESHOOTING.md](TROUBLESHOOTING.md) |

## Local pack (without CI)

```bash
dotnet pack src/Fluxor.csproj -c Release -o ./artifacts -p:Version=0.1.0-preview
```
