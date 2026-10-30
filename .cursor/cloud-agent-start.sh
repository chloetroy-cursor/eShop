#!/usr/bin/env bash
set -euo pipefail

allow_agent_docker_access() {
  sudo chmod 0755 /var/run
  if [[ -S /var/run/docker.sock ]]; then
    sudo chgrp docker /var/run/docker.sock
    sudo chmod 0660 /var/run/docker.sock
  fi
}

if sudo docker info >/dev/null 2>&1; then
  allow_agent_docker_access
  exit 0
fi

sudo mkdir -p /etc/docker
if [[ ! -s /etc/docker/daemon.json ]]; then
  printf '%s\n' '{' '  "storage-driver": "fuse-overlayfs"' '}' | sudo tee /etc/docker/daemon.json >/dev/null
fi

# A dead daemon leaves a pid file and socket behind. Image layers in
# /var/lib/docker stay so the next boot does not re-pull Postgres, Redis, and RabbitMQ.
sudo rm -f /var/run/docker.pid /var/run/docker.sock
sudo nohup dockerd --config-file /etc/docker/daemon.json >/tmp/eshop-dockerd.log 2>&1 &

for _ in $(seq 1 90); do
  if sudo docker info >/dev/null 2>&1; then
    allow_agent_docker_access
    exit 0
  fi
  sleep 1
done

echo "Docker daemon did not become ready" >&2
tail -n 100 /tmp/eshop-dockerd.log >&2
exit 1
