#!/bin/bash
# PreToolUse hook — protect sensitive files from accidental editing.
# Exit code 2 = block the operation.

input=$(cat)

tool_name=$(echo "$input" | jq -r '.tool_name // empty')
file_path=$(echo "$input" | jq -r '.tool_input.file_path // empty')

if [[ "$tool_name" != "Edit" && "$tool_name" != "Write" ]]; then
  exit 0
fi

if [[ -z "$file_path" ]]; then
  exit 0
fi

case "$file_path" in
  *.env|*.env.local|*.env.production)
    echo "BLOCKED: .env files are protected"
    exit 2
    ;;
  */secrets/*|*/credentials/*)
    echo "BLOCKED: Secrets/credentials directory is protected"
    exit 2
    ;;
  *config.json)
    echo "BLOCKED: config.json may contain API keys (edit manually)"
    exit 2
    ;;
esac

exit 0
