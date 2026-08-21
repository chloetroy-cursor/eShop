#!/usr/bin/env bash
set -euo pipefail

if docker info >/dev/null 2>&1; then
  exit 0
fi

sudo rm -f /var/run/docker.pid
sudo nohup dockerd >/tmp/eshop-dockerd.log 2>&1 &

for _ in $(seq 1 60); do
  if docker info >/dev/null 2>&1; then
    exit 0
  fi
  sleep 1
done

echo "Docker daemon did not become ready" >&2
tail -n 100 /tmp/eshop-dockerd.log >&2
exit 1
