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

# Открываем страницу, только когда Vite начнёт принимать соединения, — иначе браузер
# покажет ошибку «сайт недоступен» и её придётся обновлять вручную.
$deadline = (Get-Date).AddSeconds(30)
$ready = $false
while (-not $ready -and (Get-Date) -lt $deadline) {
  $probe = [System.Net.Sockets.TcpClient]::new()
  try {
    $probe.Connect('localhost', 5173)
    $ready = $true
  } catch {
    Start-Sleep -Milliseconds 250
  } finally {
    $probe.Dispose()
  }
}

if ($ready) {
  Start-Process 'http://localhost:5173'
} else {
  Write-Host 'Vite did not answer in 30s - open http://localhost:5173 manually.' -ForegroundColor Yellow
}

try {
  Wait-Process -Id $api.Id, $web.Id -ErrorAction SilentlyContinue
} finally {
  foreach ($procId in Get-ProcessTree @($api.Id, $web.Id)) {
    Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
  }
}
