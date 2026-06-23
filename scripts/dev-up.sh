#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [[ ! -f .env ]]; then
  echo "Creating .env from .env.example..."
  cp .env.example .env
  echo "Review .env and update secrets before production use."
fi

docker compose up -d --build

echo ""
echo "Stack starting. Useful URLs:"
echo "  API Swagger:  http://localhost:${API_PORT:-5080}/swagger"
echo "  Health live:  http://localhost:${API_PORT:-5080}/health/live"
echo "  Mailpit UI:   http://localhost:${MAILPIT_UI_PORT:-8025}"
echo ""
echo "Run 'docker compose ps' to check service health."
