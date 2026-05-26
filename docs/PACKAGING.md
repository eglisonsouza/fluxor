# Fluxor — GitHub Packages & Visual Studio

This guide covers publishing **Fluxor** to a **private GitHub** repository’s **GitHub Packages** NuGet feed and consuming it from **Visual Studio**.

## Prerequisites

1. Push this repo to a **private** GitHub repository (e.g. `https://github.com/your-org/fluxor`).
2. In `src/Fluxor.csproj`, set `RepositoryUrl` to that repo URL (or rely on CI, which sets it automatically on publish).
3. Ensure the repo has **Actions** enabled.

## CI (develop + pull requests)

Workflow: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)

- Runs on pushes to **`develop`** and on PRs to **`main`** / **`develop`**.
- Builds and packs (artifact only)—does **not** publish.

## Publish to GitHub Packages (merge to `main`)

Workflow: [`.github/workflows/publish.yml`](../.github/workflows/publish.yml)

**When you merge `develop` → `main`**, the workflow:

1. Calculates the version from **Conventional Commits** since the last tag ([calculate-version.sh](../.github/scripts/calculate-version.sh)).
2. Publishes **Fluxor** to GitHub Packages.
3. Creates and pushes git tag **`vX.Y.Z`** automatically.

See **[VERSIONING.md](VERSIONING.md)** for commit message rules (`feat:`, `fix:`, `feat!:`, etc.).

### Manual run (optional)

1. GitHub → **Actions** → **Publish NuGet package** → **Run workflow** on **`main`**.
2. Optionally set **version** to override GitVersion (e.g. `1.0.0`).

### Feed URL

After publish, packages appear under the **repository owner** feed:

```text
https://nuget.pkg.github.com/YOUR_GITHUB_ORG_OR_USER/index.json
```

Package id: **Fluxor**.

`GITHUB_TOKEN` in Actions already has permission to push packages when `packages: write` is set (configured in the workflow).

## GitHub: link package to the repo

1. Repo → **Packages** (right sidebar) or org **Packages**.
2. Open **Fluxor** → **Package settings** → connect to this repository if prompted.
3. For a **private** repo, the package stays private to users with access to the repo/org and a valid PAT.

## Personal Access Token (for Visual Studio / CLI)

Create a **classic** PAT on GitHub:

| Scope | Why |
|--------|-----|
| `read:packages` | Download packages |
| `repo` | Required if the **repository** is private |

Fine-grained tokens: enable **Packages: Read** and **Contents: Read** for the repo.

Store the PAT securely (Windows Credential Manager, environment variable, or `packageSourceCredentials` — see below).

## Visual Studio — add the GitHub feed

### Method 1 — UI (user-wide)

1. **Tools** → **NuGet Package Manager** → **Package Manager Settings**.
2. **Package sources** → **+**
   - Name: `GitHub Fluxor`
   - Source: `https://nuget.pkg.github.com/YOUR_GITHUB_ORG_OR_USER/index.json`
3. **Update** (or when prompted), authenticate:
   - Username: your GitHub username
   - Password: the PAT (not your GitHub password)

### Method 2 — `nuget.config` (solution or user)

Copy [`nuget.config.example`](../nuget.config.example) to `nuget.config` next to your solution, replace placeholders, and restart Visual Studio.

User-level file (affects all solutions):

```text
%AppData%\NuGet\NuGet.Config
```

### Install the package

1. Right-click project → **Manage NuGet Packages**.
2. Package source: **GitHub Fluxor** (or your source name).
3. Search **Fluxor** → install.

Or Package Manager Console:

```powershell
Install-Package Fluxor -Source "GitHub Fluxor"
```

### `dotnet` CLI

```bash
dotnet nuget add source "https://nuget.pkg.github.com/YOUR_GITHUB_ORG_OR_USER/index.json" \
  --name github-fluxor \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_PAT \
  --store-password-in-clear-text

dotnet add package Fluxor --source github-fluxor
```

Prefer `--store-password-in-clear-text` only on dev machines; on CI use `GITHUB_TOKEN` or secrets.

## Consumer `PackageReference` (other repos)

```xml
<PackageReference Include="Fluxor" Version="1.0.0" />
```

Use the same `nuget.config` / VS package source so restore finds GitHub Packages.

## Troubleshooting

| Issue | Fix |
|--------|-----|
| 401 Unauthorized | PAT missing `read:packages` / `repo` for private repos |
| Package not listed | Wrong owner in feed URL; package not published yet |
| NU1101 unable to find package | No GitHub source in NuGet config; wrong package source in VS |
| Duplicate publish | Workflow uses `--skip-duplicate`; bump version or delete package version in GitHub |
| .NET SDK not found in Actions | Install .NET 10 SDK on runner (`setup-dotnet` with `10.0.x`) |

## Local pack (without CI)

```bash
dotnet pack src/Fluxor.csproj -c Release -o ./artifacts -p:Version=0.1.0-preview
```
