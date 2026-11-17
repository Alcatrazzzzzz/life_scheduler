# save-session-log.ps1
# Читает stdin как UTF-8, независимо от кодовой страницы консоли Windows.

$stdin = [Console]::OpenStandardInput()
$reader = New-Object System.IO.StreamReader($stdin, [System.Text.Encoding]::UTF8)
$raw = $reader.ReadToEnd()
$reader.Close()

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

# Путь считаем от корня проекта: хук наследует текущую папку оболочки,
# которая может оказаться подпапкой (например, src/Web).
$root = $env:CLAUDE_PROJECT_DIR
if (-not $root) { $root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot) }

$dir = Join-Path $root "docs/sessions"
$file = Join-Path $dir "$date-$time.md"

New-Item -ItemType Directory -Force -Path $dir | Out-Null
$content = "# Сессия $date $time`n`n$summary"

# WriteAllText по умолчанию пишет UTF-8 без BOM.
[System.IO.File]::WriteAllText($file, $content)

exit 0