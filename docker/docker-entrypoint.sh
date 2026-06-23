#!/bin/sh
set -e

APP_USER="${APP_USER:-app}"
UPLOADS_DIR="/app/uploads"
LOGS_DIR="/app/logs"

mkdir -p "$UPLOADS_DIR" "$LOGS_DIR"

if [ "$(id -u)" = "0" ]; then
    chown -R "${APP_USER}:${APP_USER}" "$UPLOADS_DIR" "$LOGS_DIR"
    exec gosu "$APP_USER" dotnet ApiHost.dll "$@"
fi

exec dotnet ApiHost.dll "$@"
