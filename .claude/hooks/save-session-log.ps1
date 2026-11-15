$raw = [Console]::In.ReadToEnd()
if (-not $raw) { exit 0 }

try {
    $data = $raw | ConvertFrom-Json
} catch {
    exit 0
}

$summary = $data.last_assistant_message
if (-not $summary -or $summary.Length -lt 200) { exit 0 }

$date = Get-Date -Format "yyyy-MM-dd"
$time = Get-Date -Format "HH-mm"
$dir = "docs/sessions"
$file = Join-Path $dir "$date-$time.md"

New-Item -ItemType Directory -Force -Path $dir | Out-Null
"# Сессия $date $time`n`n$summary" | Set-Content -Path $file -Encoding UTF8

exit 0
