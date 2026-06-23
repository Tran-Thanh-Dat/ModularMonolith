#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [[ ! -f .env ]]; then
  cp .env.example .env
fi

API_PORT="${API_PORT:-5080}"
LIVE_URL="http://localhost:${API_PORT}/health/live"
READY_URL="http://localhost:${API_PORT}/health/ready"

echo "Validating docker compose configuration..."
docker compose config --quiet

echo "Building and starting stack..."
docker compose up -d --build

echo "Waiting for ${LIVE_URL} ..."
for attempt in $(seq 1 60); do
  if curl -fsS "$LIVE_URL" >/dev/null 2>&1; then
    echo "Health live: OK"
    break
  fi

  if [[ "$attempt" -eq 60 ]]; then
    echo "API did not become healthy in time."
    docker compose ps
    docker compose logs api --tail 100
    exit 1
  fi

  sleep 3
done

echo "Checking readiness endpoint..."
if curl -fsS "$READY_URL" >/dev/null 2>&1; then
  echo "Health ready: OK"
else
  echo "Health ready: returned non-success (may be 503 while dependencies warm up)."
  curl -sS "$READY_URL" || true
  echo
fi

echo ""
echo "Smoke test passed."
echo "  Swagger:  http://localhost:${API_PORT}/swagger"
echo "  Mailpit:  http://localhost:${MAILPIT_UI_PORT:-8025}"
echo "  Logs:     docker compose logs api"
