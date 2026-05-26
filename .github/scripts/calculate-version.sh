#!/usr/bin/env bash
# Calculates SemVer from Conventional Commits since the latest v* tag.
# https://www.conventionalcommits.org/
set -euo pipefail

DEFAULT_MAJOR=0
DEFAULT_MINOR=1
DEFAULT_PATCH=0

LAST_TAG="$(git tag -l 'v*' --sort=-v:refname | head -n1 || true)"

if [[ -z "$LAST_TAG" ]]; then
  MAJOR=$DEFAULT_MAJOR
  MINOR=$DEFAULT_MINOR
  PATCH=$DEFAULT_PATCH
  LOG_RANGE="--no-merges"
else
  VERSION_STR="${LAST_TAG#v}"
  IFS='.' read -r MAJOR MINOR PATCH <<< "$VERSION_STR"
  LOG_RANGE="${LAST_TAG}..HEAD --no-merges"
fi

BUMP="none"

while IFS= read -r subject; do
  [[ -z "$subject" ]] && continue

  if echo "$subject" | grep -qiE '^[a-zA-Z]+(\([^)]+\))?!:|^\+semver:\s?(breaking|major)'; then
    BUMP="major"
    break
  fi

  if echo "$subject" | grep -qiE '^feat(\([^)]+\))?:'; then
    [[ "$BUMP" != "major" ]] && BUMP="minor"
  fi

  if echo "$subject" | grep -qiE '^fix(\([^)]+\))?:'; then
    [[ "$BUMP" == "none" ]] && BUMP="patch"
  fi
done < <(git log $LOG_RANGE --pretty=format:%s 2>/dev/null || true)

if [[ "$BUMP" != "major" ]]; then
  if git log $LOG_RANGE --pretty=format:%B 2>/dev/null | grep -qiE 'BREAKING CHANGE'; then
    BUMP="major"
  fi
fi

case "$BUMP" in
  major)
    MAJOR=$((MAJOR + 1))
    MINOR=0
    PATCH=0
    ;;
  minor)
    MINOR=$((MINOR + 1))
    PATCH=0
    ;;
  patch)
    PATCH=$((PATCH + 1))
    ;;
  none)
    ;;
esac

VERSION="${MAJOR}.${MINOR}.${PATCH}"
TAG="v${VERSION}"

{
  echo "version=${VERSION}"
  echo "tag=${TAG}"
  echo "bump=${BUMP}"
} >> "${GITHUB_OUTPUT}"

echo "Calculated version: ${VERSION} (bump: ${BUMP}, last tag: ${LAST_TAG:-none})"
echo "Commits considered:"
git log $LOG_RANGE --oneline 2>/dev/null || true
