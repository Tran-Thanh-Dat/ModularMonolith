$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $PSScriptRoot
Set-Location $RootDir

if (-not (Test-Path ".env")) {
    Write-Host "Creating .env from .env.example..."
    Copy-Item ".env.example" ".env"
    Write-Host "Review .env and update secrets before production use."
}

docker compose up -d --build

Write-Host ""
Write-Host "Stack starting. Useful URLs:"
Write-Host "  API Swagger:  http://localhost:5080/swagger"
Write-Host "  Health live:  http://localhost:5080/health/live"
Write-Host "  Mailpit UI:   http://localhost:8025"
Write-Host ""
Write-Host "Run 'docker compose ps' to check service health."
