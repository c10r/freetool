#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

run_step() {
  local label="$1"
  shift

  echo
  echo "==> $label"
  "$@"
}

ensure_not_default_branch() {
  local current_branch
  current_branch="$(git branch --show-current)"

  if [ "$current_branch" = "master" ]; then
    echo "Refusing to run on 'master'. Create a feature branch and retry."
    exit 1
  fi
}

pick_remote() {
  if git remote get-url origin >/dev/null 2>&1; then
    echo "origin"
    return
  fi

  git remote | head -n 1
}

ensure_upstream_tracking() {
  local branch remote
  branch="$(git branch --show-current)"
  remote="$(pick_remote)"

  if [ -z "$remote" ]; then
    echo "No git remotes found. Add a remote and retry."
    exit 1
  fi

  if git rev-parse --abbrev-ref --symbolic-full-name "@{upstream}" >/dev/null 2>&1; then
    return
  fi

  echo "No upstream configured for '$branch'; pushing and setting upstream on '$remote'."
  git push -u "$remote" "$branch"
}

ensure_pull_request() {
  local branch base_branch existing_pr_url
  branch="$(git branch --show-current)"
  base_branch="master"

  existing_pr_url="$(gh pr list \
    --head "$branch" \
    --base "$base_branch" \
    --state open \
    --json url \
    --jq '.[0].url' 2>/dev/null || true)"

  if [ -n "$existing_pr_url" ] && [ "$existing_pr_url" != "null" ]; then
    echo "Found existing pull request: $existing_pr_url"
    return
  fi

  echo "No pull request found for '$branch'; creating one against '$base_branch'."
  gh pr create --head "$branch" --base "$base_branch" --fill
}

ensure_not_default_branch

if ! git rev-parse --verify master >/dev/null 2>&1; then
  echo "Could not find local 'master' branch for diff baseline."
  echo "Fetch or create 'master', then retry."
  exit 1
fi

CHANGED_FILES="$(git diff --name-only master...HEAD)"
RUN_BACKEND=false
RUN_FRONTEND=false

if grep -qE '^src/' <<<"$CHANGED_FILES"; then
  RUN_BACKEND=true
fi

if grep -qE '^www/' <<<"$CHANGED_FILES"; then
  RUN_FRONTEND=true
fi

if [ "$RUN_BACKEND" = true ]; then
  run_step "Running formatter" dotnet tool run fantomas .
  run_step "Building solution (Release)" dotnet build CustomerDash.sln -c Release
  run_step "Running backend tests" dotnet test CustomerDash.sln
else
  echo "No changes under src/; skipping backend verification steps."
fi

if [ "$RUN_FRONTEND" = true ]; then
  run_step "Running frontend type checks" bash -lc "cd www && npm run check"
  run_step "Running frontend lint" bash -lc "cd www && npm run lint"
  run_step "Running frontend format" bash -lc "cd www && npm run format"
else
  echo "No changes under www/; skipping frontend verification steps."
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "GitHub CLI ('gh') is required but was not found in PATH."
  echo "Install: https://cli.github.com/"
  exit 1
fi

if ! gh help signoff >/dev/null 2>&1; then
  cat <<'MSG'
The 'gh signoff' command is not available.
Install the extension and retry:
  gh extension install basecamp/gh-signoff
MSG
  exit 1
fi

ensure_upstream_tracking
ensure_pull_request

echo "Signing off PR with gh-signoff..."
gh signoff "$@"
