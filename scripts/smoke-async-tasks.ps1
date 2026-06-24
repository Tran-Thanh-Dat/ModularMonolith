# Phase 27 AsyncTasks — RabbitMQ smoke test (manual E2E)
# Prerequisites: Docker Desktop running, PostgreSQL local, ApiHost configured (Development)
#
# Usage:
#   .\scripts\smoke-async-tasks.ps1
#   .\scripts\smoke-async-tasks.ps1 -BaseUrl "http://localhost:5080" -SkipDocker

param(
    [string]$BaseUrl = "http://localhost:5080",
    [string]$AdminUser = "admin",
    [string]$AdminPassword = "Admin@123456",
    [switch]$SkipDocker
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

function Write-Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }

function Wait-TaskStatus {
    param([string]$Token, [guid]$TaskId, [string[]]$ExpectedFinal, [int]$TimeoutSeconds = 120)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $last = $null
    while ((Get-Date) -lt $deadline) {
        $r = Invoke-RestMethod -Uri "$BaseUrl/api/v1/async-tasks/$TaskId" -Headers @{ Authorization = "Bearer $Token" }
        $status = $r.data.status
        $last = $status
        Write-Host "  poll: $status"
        if ($ExpectedFinal -contains $status) { return $r.data }
        Start-Sleep -Seconds 2
    }
    throw "Task $TaskId did not reach [$($ExpectedFinal -join ',')] within ${TimeoutSeconds}s (last: $last)"
}

Write-Step "1. Docker + RabbitMQ"
if (-not $SkipDocker) {
    docker info *> $null
    if ($LASTEXITCODE -ne 0) { throw "Docker is not running. Start Docker Desktop or use -SkipDocker if RabbitMQ is already up." }
    Push-Location $repoRoot
    docker compose up -d rabbitmq
    Pop-Location
    Write-Host "Waiting 15s for RabbitMQ..."
    Start-Sleep -Seconds 15
    Write-Host "RabbitMQ UI: http://localhost:15672 (guest/guest)"
}

Write-Step "2. Health checks"
$live = Invoke-RestMethod -Uri "$BaseUrl/health/live"
Write-Host "live: $($live.status)"
try {
    $ready = Invoke-WebRequest -Uri "$BaseUrl/health/ready" -UseBasicParsing
    Write-Host "ready: HTTP $($ready.StatusCode)"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Write-Host "ready: HTTP $code"
}

Write-Step "3. Login"
$loginBody = @{ userNameOrEmail = $AdminUser; password = $AdminPassword } | ConvertTo-Json
$login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/auth/login" -ContentType "application/json" -Body $loginBody
$token = $login.data.accessToken
if (-not $token) { throw "Login failed — check admin seed credentials." }

Write-Step "4. Email demo -> COMPLETED"
$email = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/async-tasks/email-demo" `
    -Headers @{ Authorization = "Bearer $token" } -ContentType "application/json" `
    -Body (@{ emailTo = "smoke@example.com"; subject = "Smoke"; body = "Test" } | ConvertTo-Json)
$emailTask = Wait-TaskStatus -Token $token -TaskId $email.data.id -ExpectedFinal @("COMPLETED")
Write-Host "COMPLETED: $($emailTask.taskNo) progress=$($emailTask.progressPercent)"

Write-Step "5. Fail demo shouldAlwaysFail=true -> FAILED"
$fail = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/async-tasks/fail-demo" `
    -Headers @{ Authorization = "Bearer $token" } -ContentType "application/json" `
    -Body (@{ shouldAlwaysFail = $true; failReason = "smoke fail" } | ConvertTo-Json)
$failTask = Wait-TaskStatus -Token $token -TaskId $fail.data.id -ExpectedFinal @("FAILED") -TimeoutSeconds 90
Write-Host "FAILED: $($failTask.taskNo) error=$($failTask.lastErrorMessage)"

Write-Step "6. Fail demo shouldAlwaysFail=false -> COMPLETED"
$ok = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/async-tasks/fail-demo" `
    -Headers @{ Authorization = "Bearer $token" } -ContentType "application/json" `
    -Body (@{ shouldAlwaysFail = $false } | ConvertTo-Json)
$okTask = Wait-TaskStatus -Token $token -TaskId $ok.data.id -ExpectedFinal @("COMPLETED") -TimeoutSeconds 30
Write-Host "COMPLETED: $($okTask.taskNo)"

Write-Step "Smoke PASS"
Write-Host "Check RabbitMQ queue: async-tasks.process" -ForegroundColor Green
