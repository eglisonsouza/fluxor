# Troubleshooting GitHub Actions

## Workflows do not appear or never start

1. Open **https://github.com/eglisonsouza/fluxor/actions**
2. If you see “Workflows aren’t being run on this repository”, enable **Actions** under **Settings → Actions → General**.
3. Workflow files must exist on **`main`**. Merging a PR that only adds workflows will start Actions from that merge onward.
4. A commit message containing **`[skip ci]`** or **`[skip actions]`** skips runs.

## Workflow runs but nothing is published (green check, no tag)

Open the **Publish NuGet package** run → **Summary**.

The release is **skipped** when:

- There is **no** `feat:`, `fix:`, or breaking change in commits on `main` since the last `v*` tag, or
- Tag **`vX.Y.Z`** already exists for the calculated version.

**Examples**

| Merge to `main` | Result |
|-----------------|--------|
| Only `chore: update ci` since `v0.2.0` | Skipped |
| Includes `feat: new handler` | Publishes next minor |
| Squash merge titled `feat: add pipeline` | Publishes |

**Fix:** Use a squash-merge title like `feat: ...`, or run **Actions → Publish NuGet package → Run workflow** with version `0.2.0` (and optional **force**).

## Publish job fails (red X)

| Error | Fix |
|--------|-----|
| NuGet push 403 / 400 | Package id must be lowercase (`fluxor`). Ensure **Settings → Actions → General → Workflow permissions** is **Read and write**. |
| `dotnet` / SDK not found | Repo uses .NET 10; `setup-dotnet` installs `10.0.x`. |
| Tag creation failed | **Settings → Branches** → edit protection on `main` → allow **github-actions[bot]** to bypass or disable “Block tag creation”. |
| Script `pipefail` invalid | Line endings must be LF (see `.gitattributes`). |

## CI vs Publish on push to `main`

Both run on every push to **`main`**:

- **CI** — build only
- **Publish NuGet package** — version, package, tag (may skip release)

## Manual first release

1. **Actions** → **Publish NuGet package** → **Run workflow**
2. Branch: `main`
3. Version: `0.1.0`
4. Force: `true` (if there are no `feat`/`fix` commits yet)
5. Run

This creates tag `v0.1.0` and package **fluxor** `0.1.0`.
