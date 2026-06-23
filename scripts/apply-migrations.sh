#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [[ -f .env ]]; then
  set -a
  # shellcheck disable=SC1091
  source .env
  set +a
fi

CONN="${ConnectionStrings__DefaultConnection:-Host=localhost;Port=${POSTGRES_PORT:-5432};Database=${POSTGRES_DB:-modular_monolith};Username=${POSTGRES_USER:-postgres};Password=${POSTGRES_PASSWORD:-dev_postgres_password_change_me}}"

echo "Applying EF Core migrations against: ${CONN%%Password=*}Password=***"

dotnet ef database update --project src/Modules/Identity/Identity.Infrastructure --startup-project src/ApiHost --context IdentityDbContext --connection "$CONN"
dotnet ef database update --project src/Modules/AuditLogs/AuditLogs.Infrastructure --startup-project src/ApiHost --context AuditLogsDbContext --connection "$CONN"
dotnet ef database update --project src/Modules/Categories/Categories.Infrastructure --startup-project src/ApiHost --context CategoriesDbContext --connection "$CONN"
dotnet ef database update --project src/Modules/Files/Files.Infrastructure --startup-project src/ApiHost --context FilesDbContext --connection "$CONN"
dotnet ef database update --project src/Modules/Notifications/Notifications.Infrastructure --startup-project src/ApiHost --context NotificationsDbContext --connection "$CONN"
dotnet ef database update --project src/Modules/BackgroundJobs/BackgroundJobs.Infrastructure --startup-project src/ApiHost --context BackgroundJobsDbContext --connection "$CONN"

echo "Migrations applied."
