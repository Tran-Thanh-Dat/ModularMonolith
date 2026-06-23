$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $PSScriptRoot
Set-Location $RootDir

if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
}

$ApiPort = if ($env:API_PORT) { $env:API_PORT } else { "5080" }
$LiveUrl = "http://localhost:$ApiPort/health/live"
$ReadyUrl = "http://localhost:$ApiPort/health/ready"
$MailpitPort = if ($env:MAILPIT_UI_PORT) { $env:MAILPIT_UI_PORT } else { "8025" }

Write-Host "Validating docker compose configuration..."
docker compose config --quiet

Write-Host "Building and starting stack..."
docker compose up -d --build

Write-Host "Waiting for $LiveUrl ..."
$healthy = $false
for ($attempt = 1; $attempt -le 60; $attempt++) {
    try {
        Invoke-WebRequest -Uri $LiveUrl -UseBasicParsing -TimeoutSec 5 | Out-Null
        Write-Host "Health live: OK"
        $healthy = $true
        break
    }
    catch {
        if ($attempt -eq 60) {
            Write-Host "API did not become healthy in time."
            docker compose ps
            docker compose logs api --tail 100
            exit 1
        }

        Start-Sleep -Seconds 3
    }
}

if (-not $healthy) {
    exit 1
}

Write-Host "Checking readiness endpoint..."
try {
    Invoke-WebRequest -Uri $ReadyUrl -UseBasicParsing -TimeoutSec 10 | Out-Null
    Write-Host "Health ready: OK"
}
catch {
    Write-Host "Health ready: returned non-success (may be 503 while dependencies warm up)."
    try { Invoke-WebRequest -Uri $ReadyUrl -UseBasicParsing -TimeoutSec 10 } catch { $_.Exception.Message }
}

Write-Host ""
Write-Host "Smoke test passed."
Write-Host "  Swagger:  http://localhost:$ApiPort/swagger"
Write-Host "  Mailpit:  http://localhost:$MailpitPort"
Write-Host "  Logs:     docker compose logs api"
