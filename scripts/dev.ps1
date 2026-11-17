# Запускает API (dotnet watch) и фронтенд (vite) одновременно. Ctrl+C останавливает оба.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnet) { $dotnet = 'C:\Program Files\dotnet\dotnet.exe' }

Write-Host 'API: http://localhost:5148' -ForegroundColor Cyan
Write-Host 'Web: http://localhost:5173' -ForegroundColor Cyan

$api = Start-Process $dotnet `
  -ArgumentList 'watch', '--project', "$root\src\LifeScheduler.Api", 'run', '--launch-profile', 'http' `
  -NoNewWindow -PassThru

$web = Start-Process 'npm.cmd' -ArgumentList 'run', 'dev' `
  -WorkingDirectory "$root\src\Web" `
  -NoNewWindow -PassThru

try {
  Wait-Process -Id $api.Id, $web.Id -ErrorAction SilentlyContinue
} finally {
  # /T — вместе с дочерними процессами (vite, dotnet build)
  foreach ($p in $api, $web) {
    if ($p -and -not $p.HasExited) { taskkill /PID $p.Id /T /F | Out-Null }
  }
}
