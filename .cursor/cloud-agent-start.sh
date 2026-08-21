#!/usr/bin/env bash
set -euo pipefail

if [[ "$(docker info --format '{{.Driver}}' 2>/dev/null || true)" == "vfs" ]]; then
  exit 0
fi

sudo mkdir -p /etc/docker
echo '{"storage-driver":"vfs"}' | sudo tee /etc/docker/daemon.json >/dev/null

sudo pkill -x dockerd 2>/dev/null || true
sudo pkill -x containerd 2>/dev/null || true

for _ in $(seq 1 15); do
  if ! pgrep -x dockerd >/dev/null && ! pgrep -x containerd >/dev/null; then
    break
  fi
  sleep 1
done

sudo rm -rf /var/lib/docker/*
sudo rm -f /var/run/docker.pid /var/run/docker.sock
sudo nohup dockerd --config-file /etc/docker/daemon.json >/tmp/eshop-dockerd.log 2>&1 &

for _ in $(seq 1 90); do
  if [[ "$(docker info --format '{{.Driver}}' 2>/dev/null || true)" == "vfs" ]]; then
    exit 0
  fi
  sleep 1
done

echo "Docker daemon did not become ready" >&2
tail -n 100 /tmp/eshop-dockerd.log >&2
exit 1
