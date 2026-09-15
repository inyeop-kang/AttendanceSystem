# start_demo.ps1 - AI서버(Python) + C#서버 + WPF클라이언트를 한 번에 띄운다.
#
# 사용법(scripts/ 폴더 밖, 프로젝트 루트에서 실행하는 걸 권장):
#   .\scripts\start_demo.ps1            (서버 콘솔은 숨기고, WPF 창만 보임)
#   .\scripts\start_demo.ps1 -Visible   (서버 콘솔도 그대로 보이게 - 디버깅용)
#
# 콘솔을 숨기면 로그가 안 보이므로 logs\ 폴더의 파일로 리다이렉트한다.
#
# 참고: 이 .ps1은 BOM 없이 저장되면 PowerShell 5.1에서 한글 리터럴이 코드페이지와
# 무관하게 깨질 수 있어(다른 프로젝트(smart-access-control-system)의
# start_demo_silent.ps1에서 겪은 문제와 동일), 실행 중 출력 문자열은 영문으로 통일한다.
# 한글 설명은 주석에만 쓴다.

param(
    [switch]$Visible
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $scriptDir
$aiServerDir = Join-Path $root "ai-server"
$serverDir = Join-Path $root "server"
$clientDir = Join-Path $root "client"
$logsDir = Join-Path $root "logs"

New-Item -ItemType Directory -Force -Path $logsDir | Out-Null

function Stop-PortOwner {
    param([int]$Port)
    $owners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($processId in $owners) {
        Write-Host "  Killing stale process on port $Port (PID $processId)"
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "[1/4] Cleaning up stale processes on ports 5080/8001..."
Stop-PortOwner -Port 5080
Stop-PortOwner -Port 8001
Start-Sleep -Seconds 1

$venvPython = Join-Path $aiServerDir "venv\Scripts\python.exe"
if (-not (Test-Path $venvPython)) {
    Write-Host ""
    Write-Host "ERROR: ai-server venv not found at $venvPython"
    Write-Host "Run this first:"
    Write-Host "  cd ai-server"
    Write-Host "  py -3.11 -m venv venv"
    Write-Host "  venv\Scripts\pip install -r requirements.txt"
    exit 1
}

$windowStyle = if ($Visible) { "Normal" } else { "Hidden" }

Write-Host "[2/4] Starting AI server (Python, $windowStyle console)..."
Start-Process -FilePath $venvPython -ArgumentList "-u", "main.py" -WorkingDirectory $aiServerDir `
    -WindowStyle $windowStyle `
    -RedirectStandardOutput (Join-Path $logsDir "ai_server.log") `
    -RedirectStandardError (Join-Path $logsDir "ai_server.err.log")
Write-Host "  -> Waiting for AI server to load face recognition model (8s)..."
Start-Sleep -Seconds 8

Write-Host "[3/4] Starting C# server ($windowStyle console)..."
Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory $serverDir `
    -WindowStyle $windowStyle `
    -RedirectStandardOutput (Join-Path $logsDir "server.log") `
    -RedirectStandardError (Join-Path $logsDir "server.err.log")
Write-Host "  -> Waiting for C# server startup (3s)..."
Start-Sleep -Seconds 3

Write-Host "[4/4] Starting WPF client (visible window)..."
Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory $clientDir

Write-Host ""
Write-Host "Done."
Write-Host "AI server / C# server logs: $logsDir"
if (-not $Visible) {
    Write-Host "Server consoles are hidden. Use -Visible to show them, or check the log files above."
}
Write-Host "Run scripts\stop_demo.ps1 to stop everything."
