# Запускает API (dotnet watch) и фронтенд (vite) одновременно. Ctrl+C останавливает оба
# и закрывает окно браузера, открытое скриптом.
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

# Браузер запускаем отдельным процессом со своим профилем во временной папке: только так
# окно можно закрыть по Ctrl+C, не затронув сессии и вкладки основного браузера.
function Find-Browser {
  $candidates = @(
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
    "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe",
    "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
  )

  foreach ($candidate in $candidates) {
    if (Test-Path $candidate) { return $candidate }
  }

  return $null
}

# Второй монитор, если он подключён, иначе основной.
function Get-TargetScreen {
  Add-Type -AssemblyName System.Windows.Forms
  $screens = [System.Windows.Forms.Screen]::AllScreens

  $secondary = $screens | Where-Object { -not $_.Primary } | Select-Object -First 1
  if ($secondary) { return $secondary }

  return $screens | Where-Object { $_.Primary } | Select-Object -First 1
}

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class NativeWindow {
  public delegate bool EnumProc(IntPtr window, IntPtr state);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback, IntPtr state);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out RECT rect);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window, int command);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

# Окно браузера ищем среди видимых окон его процессов: наше - самое крупное из них.
function Get-LargestWindow([int[]] $ProcessIds) {
  $candidates = [System.Collections.Generic.List[IntPtr]]::new()

  $callback = [NativeWindow+EnumProc] {
    param($window, $state)
    if (-not [NativeWindow]::IsWindowVisible($window)) { return $true }

    $windowProcessId = 0
    [void] [NativeWindow]::GetWindowThreadProcessId($window, [ref] $windowProcessId)
    if ($ProcessIds -notcontains [int] $windowProcessId) { return $true }

    $candidates.Add($window)
    return $true
  }

  [void] [NativeWindow]::EnumWindows($callback, [IntPtr]::Zero)

  $largest = [IntPtr]::Zero
  $largestArea = 0
  foreach ($candidate in $candidates) {
    $rect = New-Object NativeWindow+RECT
    [void] [NativeWindow]::GetWindowRect($candidate, [ref] $rect)
    $area = ($rect.Right - $rect.Left) * ($rect.Bottom - $rect.Top)

    if ($area -gt $largestArea) {
      $largestArea = $area
      $largest = $candidate
    }
  }

  return $largest
}

# Chrome не умеет одновременно вставать на заданный монитор и максимизироваться, поэтому
# монитор задаём позицией окна, а разворачиваем его сами.
function Expand-BrowserWindow([int[]] $ProcessIds) {
  # Окно появляется не сразу после Start-Process, поэтому ждём его до 15 секунд.
  $deadline = (Get-Date).AddSeconds(15)
  while ((Get-Date) -lt $deadline) {
    $window = Get-LargestWindow $ProcessIds
    if ($window -ne [IntPtr]::Zero) {
      [void] [NativeWindow]::ShowWindow($window, 3)
      return
    }
    Start-Sleep -Milliseconds 300
  }
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

$browser = $null
$browserProfile = Join-Path $env:TEMP 'life-scheduler-dev-browser'

if ($ready) {
  $browserPath = Find-Browser
  if ($browserPath) {
    $screen = Get-TargetScreen
    $position = '{0},{1}' -f ($screen.WorkingArea.Left + 40), ($screen.WorkingArea.Top + 40)

    $browser = Start-Process $browserPath `
      -ArgumentList '--app=http://localhost:5173', ('--user-data-dir="{0}"' -f $browserProfile), '--no-first-run', '--no-default-browser-check', "--window-position=$position" `
      -PassThru

    Expand-BrowserWindow (Get-ProcessTree @($browser.Id))
  } else {
    # Браузер по умолчанию не даёт handle для закрытия - окно останется открытым.
    Start-Process 'http://localhost:5173'
  }
} else {
  Write-Host 'Vite did not answer in 30s - open http://localhost:5173 manually.' -ForegroundColor Yellow
}

try {
  Wait-Process -Id $api.Id, $web.Id -ErrorAction SilentlyContinue
} finally {
  $rootIds = @($api.Id, $web.Id)
  if ($browser) { $rootIds += $browser.Id }

  foreach ($procId in Get-ProcessTree $rootIds) {
    Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
  }

  if ($browser) {
    Remove-Item $browserProfile -Recurse -Force -ErrorAction SilentlyContinue
  }
}
