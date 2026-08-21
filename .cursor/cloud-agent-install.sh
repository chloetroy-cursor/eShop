#!/usr/bin/env bash
set -euo pipefail

dotnet --version
docker --version

dotnet restore eShop.Web.slnf
dotnet build eShop.Web.slnf --no-restore
npm ci
