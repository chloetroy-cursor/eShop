#!/usr/bin/env bash
set -euo pipefail

dotnet --version
docker --version

bash .cursor/populate-libman-cache.sh
dotnet restore eShop.Web.slnf
dotnet build eShop.Web.slnf --no-restore
npm ci
