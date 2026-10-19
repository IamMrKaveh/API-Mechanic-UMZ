#!/bin/sh
# Blocks staging of environment/settings files that must never be committed.
# Used by .pre-commit-config.yaml; .githooks/pre-commit has the same check inline
# so the framework-free hook does not depend on this script.
set -e

staged=$(git diff --cached --name-only --diff-filter=ACM)
status=0

old_ifs=$IFS
IFS='
'
for file in $staged; do
  case "$file" in
    .env|*/.env|otp.json|*/otp.json|\
    appsettings.Production.json|*/appsettings.Production.json|\
    appsettings.Staging.json|*/appsettings.Staging.json)
      echo "blocked-environment-files: '$file' must not be committed." >&2
      status=1
      ;;
  esac
done
IFS=$old_ifs

exit $status
