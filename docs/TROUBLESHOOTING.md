# Troubleshooting GitHub Actions

## “Failed to queue workflow run. Please try again.”

This error appears **before** the workflow starts (nothing in the run list). Check:

1. **Settings → Actions → General** → allow **Actions** for this repository.
2. **Workflow permissions** → **Read and write permissions** (not read-only).
3. **Organization / account billing** — private repos need Actions minutes (free tier has a limit). For org `egilson-souza`, check **Organization Settings → Billing**.
4. When clicking **Run workflow**, select branch **`main`** (workflows are read from the default branch).
5. Enter **version** (e.g. `0.1.0`) — manual runs require a version string.
6. Wait a few minutes and retry (GitHub sometimes returns this error temporarily).
7. Merge the latest `publish.yml` to **`main`** (older files with `boolean` inputs can fail to queue).

## Workflows do not appear or never start

1. Open your repo **Actions** tab.
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

1. Merge latest workflow fixes to **`main`**.
2. **Actions** → **Publish NuGet package** → **Run workflow**
3. Branch: **`main`**
4. Version: **`0.1.0`**
5. **Run workflow**

This creates tag `v0.1.0` and package **fluxor** `0.1.0`.
