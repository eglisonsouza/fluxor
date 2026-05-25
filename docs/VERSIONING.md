# Versioning (Conventional Commits + GitFlow)

## Branch workflow

| Branch | Role | CI | Publish |
|--------|------|-----|---------|
| `develop` | Day-to-day development | Build on push | No |
| `main` | Production / releases | — | **Publish + git tag** on push (after merge) |

Typical flow:

```text
feature/* → develop → (PR) → main  →  GitHub Actions publishes NuGet + creates tag vX.Y.Z
```

## How the version is calculated

On every push to **`main`**, [GitVersion](https://gitversion.net/) reads commits since the **last tag** (`v*`) and applies [Conventional Commits](https://www.conventionalcommits.org/):

| Commit message | Version bump |
|----------------|--------------|
| `fix: ...` | **Patch** (e.g. 1.0.0 → 1.0.1) |
| `feat: ...` | **Minor** (e.g. 1.0.0 → 1.1.0) |
| `feat!:` or `fix!:` or footer `BREAKING CHANGE:` | **Major** (e.g. 1.0.0 → 2.0.0) |
| `chore:`, `docs:`, `ci:`, `refactor:`, `test:`, etc. | **No bump** |

Configuration: [`GitVersion.yml`](../GitVersion.yml).

### Examples (use on `develop`; they count when merged to `main`)

```text
feat: add query pipeline registration
fix: resolve null handler in CommandPipeline
feat!: rename ICommand to IRequest

BREAKING CHANGE: handlers must implement new interface
chore: update dependencies
docs: packaging guide
```

### Manual override

**Actions** → **Publish NuGet package** → **Run workflow** → set **version** (e.g. `1.0.0`) to force a release.

## Tags and packages

1. Merge lands on `main`.
2. Workflow computes version (e.g. `1.2.0`).
3. If `v1.2.0` does **not** exist yet:
   - Packs and pushes **Fluxor** `1.2.0` to GitHub Packages.
   - Creates annotated git tag **`v1.2.0`** on `main`.
4. If the version did not change (only `chore:` / `docs:` since last tag), the tag already exists → **publish is skipped** (no duplicate release).

First release starts from `next-version: 0.1.0` in `GitVersion.yml` until the first tag is created.

## `develop` pre-release versions

On `develop`, GitVersion would produce versions like `1.1.0-beta.4` locally, but **CI does not publish** from `develop`—only validates the build.

## Tips

- Use **squash merge** carefully: the squash commit message becomes the only message counted for that PR—write it as a proper conventional commit (`feat: ...`).
- For a PR with breaking changes, ensure the merged message includes `!` or `BREAKING CHANGE:`.
- Align PR titles with conventional commits when using squash merge.
