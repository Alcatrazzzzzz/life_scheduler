# Запускает API (dotnet watch) и фронтенд (vite) одновременно. Ctrl+C останавливает оба.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnet) { $dotnet = 'C:\Program Files\dotnet\dotnet.exe' }

# Windows не переписывает ParentProcessId после смерти родителя, поэтому обход находит
# и осиротевшие процессы: dotnet watch успевает выйти по Ctrl+C раньше, чем сработает cleanup,
# а его дочерние dotnet-watch и LifeScheduler.Api.exe остаются жить.
function Get-ProcessTree([int[]] $RootIds) {
  $all = Get-CimInstance Win32_Process | Select-Object ProcessId, ParentProcessId
  $seen = [System.Collections.Generic.HashSet[int]]::new()
  $queue = [System.Collections.Generic.Queue[int]]::new()

  foreach ($rootId in $RootIds) { $queue.Enqueue($rootId) }

  while ($queue.Count -gt 0) {
    $parentId = $queue.Dequeue()
    if (-not $seen.Add($parentId)) { continue }

    foreach ($child in $all) {
      if ($child.ParentProcessId -eq $parentId) {
        $queue.Enqueue([int] $child.ProcessId)
      }
    }
  }

  return @($seen)
}

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
  foreach ($procId in Get-ProcessTree @($api.Id, $web.Id)) {
    Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
  }
}
