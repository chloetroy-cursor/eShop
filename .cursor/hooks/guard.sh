#!/usr/bin/env bash
# Demo guardrail hook: denies destructive shell commands, allows everything else.
input=$(cat)
command=$(echo "$input" | jq -r '.command // empty')
echo "$(date '+%H:%M:%S') SHELL: $command" >> /tmp/agent-audit.log

if [[ "$command" =~ (rm[[:space:]]+-rf|git[[:space:]]+push[[:space:]]+--force|DROP[[:space:]]+TABLE) ]]; then
  cat <<RESP
{"continue": true, "permission": "deny",
 "user_message": "Blocked by demo guardrail: $command",
 "agent_message": "This command was blocked by a security hook. Choose a non-destructive approach and explain what you were trying to do."}
RESP
else
  echo '{"continue": true, "permission": "allow"}'
fi
