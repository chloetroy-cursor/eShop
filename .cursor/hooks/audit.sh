#!/usr/bin/env bash
# Demo observability hook: logs every event it receives to /tmp/agent-audit.log
input=$(cat)
echo "$(date '+%H:%M:%S') $input" >> /tmp/agent-audit.log
echo '{}'
