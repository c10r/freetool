#!/bin/bash
set -e

# Detect staged file types
STAGED_FS_FILES=$(git diff --cached --name-only --diff-filter=ACM | grep -E '\.(fs|fsx|fsi)$' || true)
STAGED_TS_FILES=$(git diff --cached --name-only --diff-filter=ACM | grep -E '\.(ts|tsx)$' || true)

EXIT_CODE=0

# Run F# tests if F# files changed
if [ -n "$STAGED_FS_FILES" ]; then
  echo "🔍 F# files changed, running dotnet test..."
  dotnet test Freetool.sln || EXIT_CODE=$?
fi

# Run frontend tests if TypeScript files changed
if [ -n "$STAGED_TS_FILES" ]; then
  echo "🔍 TypeScript files changed, running frontend tests..."
  cd www && npm test || EXIT_CODE=$?
fi

exit $EXIT_CODE
