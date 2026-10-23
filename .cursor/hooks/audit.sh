#!/usr/bin/env bash
input=$(cat)
echo "$(date '+%H:%M:%S') $input" >> /tmp/agent-audit.log
echo '{}'
