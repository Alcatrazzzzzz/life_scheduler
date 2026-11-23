#!/usr/bin/env bash
# Запускает API (dotnet watch) и фронтенд (vite) одновременно. Ctrl+C останавливает оба.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET="$(command -v dotnet || echo '/c/Program Files/dotnet/dotnet.exe')"

# Windows PID фиксируем сразу после старта: в момент cleanup /proc/<msys-pid> может уже не быть,
# и тогда taskkill получит MSYS-PID, который не совпадает ни с одним Windows-процессом.
winpid() {
  local i
  for i in 1 2 3 4 5; do
    if [[ -r "/proc/$1/winpid" ]]; then
      cat "/proc/$1/winpid"
      return
    fi
    sleep 0.1
  done
}

# Windows не переписывает ParentProcessId после смерти родителя, поэтому обход находит
# и осиротевшие процессы: dotnet watch успевает выйти по Ctrl+C раньше, чем сработает cleanup.
kill_tree() {
  local roots="$*"
  [ -z "${roots// /}" ] && return 0

  ROOTS="$roots" powershell.exe -NoProfile -Command '
    $rootIds = $env:ROOTS -split "\s+" | Where-Object { $_ } | ForEach-Object { [int] $_ }
    $all = Get-CimInstance Win32_Process | Select-Object ProcessId, ParentProcessId
    $seen = [System.Collections.Generic.HashSet[int]]::new()
    $queue = [System.Collections.Generic.Queue[int]]::new()

    foreach ($rootId in $rootIds) { $queue.Enqueue($rootId) }

    while ($queue.Count -gt 0) {
      $parentId = $queue.Dequeue()
      if (-not $seen.Add($parentId)) { continue }
      foreach ($child in $all) {
        if ($child.ParentProcessId -eq $parentId) {
          $queue.Enqueue([int] $child.ProcessId)
        }
      }
    }

    foreach ($procId in $seen) { Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue }
  ' >/dev/null 2>&1 || true
}

API_WINPID=''
WEB_WINPID=''

cleanup() {
  trap - EXIT INT TERM
  kill_tree "$API_WINPID" "$WEB_WINPID"
}

echo 'API: http://localhost:5148'
echo 'Web: http://localhost:5173'

"$DOTNET" watch --project "$ROOT/src/LifeScheduler.Api" run --launch-profile http &
API_PID=$!
API_WINPID="$(winpid "$API_PID")"

(cd "$ROOT/src/Web" && npm run dev) &
WEB_PID=$!
WEB_WINPID="$(winpid "$WEB_PID")"

trap cleanup EXIT INT TERM
wait
