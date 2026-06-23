$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $PSScriptRoot
Set-Location $RootDir

if (Test-Path ".env") {
    Get-Content ".env" | ForEach-Object {
        if ($_ -match '^\s*#' -or $_ -match '^\s*$') { return }
        $parts = $_ -split '=', 2
        if ($parts.Count -eq 2) {
            Set-Item -Path "env:$($parts[0].Trim())" -Value $parts[1].Trim()
        }
    }
}

$postgresPort = if ($env:POSTGRES_PORT) { $env:POSTGRES_PORT } else { "5432" }
$postgresDb = if ($env:POSTGRES_DB) { $env:POSTGRES_DB } else { "modular_monolith" }
$postgresUser = if ($env:POSTGRES_USER) { $env:POSTGRES_USER } else { "postgres" }
$postgresPassword = if ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { "dev_postgres_password_change_me" }

$connectionString = "Host=localhost;Port=$postgresPort;Database=$postgresDb;Username=$postgresUser;Password=$postgresPassword"

Write-Host "Applying EF Core migrations against Host=localhost;Port=$postgresPort;Database=$postgresDb;Username=$postgresUser;Password=***"

dotnet ef database update --project src/Modules/Identity/Identity.Infrastructure --startup-project src/ApiHost --context IdentityDbContext --connection $connectionString
dotnet ef database update --project src/Modules/AuditLogs/AuditLogs.Infrastructure --startup-project src/ApiHost --context AuditLogsDbContext --connection $connectionString
dotnet ef database update --project src/Modules/Categories/Categories.Infrastructure --startup-project src/ApiHost --context CategoriesDbContext --connection $connectionString
dotnet ef database update --project src/Modules/Files/Files.Infrastructure --startup-project src/ApiHost --context FilesDbContext --connection $connectionString
dotnet ef database update --project src/Modules/Notifications/Notifications.Infrastructure --startup-project src/ApiHost --context NotificationsDbContext --connection $connectionString
dotnet ef database update --project src/Modules/BackgroundJobs/BackgroundJobs.Infrastructure --startup-project src/ApiHost --context BackgroundJobsDbContext --connection $connectionString

Write-Host "Migrations applied."
