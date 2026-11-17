#!/usr/bin/env bash
# Запускает API (dotnet watch) и фронтенд (vite) одновременно. Ctrl+C останавливает оба.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET="$(command -v dotnet || echo '/c/Program Files/dotnet/dotnet.exe')"

# taskkill ждёт Windows PID, а $! в Git Bash — это MSYS PID
kill_tree() {
  local winpid
  winpid="$(cat "/proc/$1/winpid" 2>/dev/null || echo "$1")"
  taskkill //PID "$winpid" //T //F >/dev/null 2>&1 || true
}

cleanup() {
  trap - EXIT INT TERM
  kill_tree "$API_PID"
  kill_tree "$WEB_PID"
}

echo 'API: http://localhost:5148'
echo 'Web: http://localhost:5173'

"$DOTNET" watch --project "$ROOT/src/LifeScheduler.Api" run --launch-profile http &
API_PID=$!

(cd "$ROOT/src/Web" && npm run dev) &
WEB_PID=$!

trap cleanup EXIT INT TERM
wait
