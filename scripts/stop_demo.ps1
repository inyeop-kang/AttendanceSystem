# stop_demo.ps1 - start_demo.ps1로 띄운 AI서버/C#서버/WPF클라이언트를 전부 내린다.
# MySQL은 건드리지 않는다(독립적으로 서비스로 떠있는 게 정상이므로).

$ErrorActionPreference = "SilentlyContinue"

function Stop-PortOwner {
    param([int]$Port)
    $owners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($processId in $owners) {
        Write-Host "  Stopping process on port $Port (PID $processId)"
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Stopping AI server (port 8001) and C# server (port 5080)..."
Stop-PortOwner -Port 8001
Stop-PortOwner -Port 5080

Write-Host "Stopping WPF client windows..."
Get-Process -Name "AttendanceClient" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "Done."
