# CLAUDE.md

Приоритет читаемости над хитроумностью. Спрашивай перед архитектурными изменениями.

## Стек

- Бэкенд: .NET 10 (`net10.0`), Minimal APIs + OpenAPI, вертикальные срезы в `src/LifeScheduler.Api/Features/{Feature}/`
- Данные: SQLite + EF Core 10 (`Microsoft.EntityFrameworkCore.Sqlite`), connection string в `appsettings.Development.json`
- Фронтенд: React 19 + TypeScript strict, Vite, TanStack Query (`src/Web/`)
- Тесты: xUnit (бэкенд), Vitest + Playwright (фронтенд/E2E)
- Аутентификация / кэширование / messaging: пока не используются

## Структура

- Бэкенд: вертикальные срезы в `src/LifeScheduler.Api/Features/{Feature}/`. Каждый срез владеет эндпоинтом, моделями запроса/ответа, handler'ом и валидацией — всё в одной папке.
- Никакого общего слоя controller/service/repository между фичами. Cross-cutting код идёт в `src/LifeScheduler.Api/Shared/`, никогда внутрь `src/LifeScheduler.Api/Features/{Feature}/`.
- Регистрируй эндпоинты через `MapGroup` в одном файле на фичу (например, `ScheduleEndpoints.cs`).
- EF Core: `DbContext` и миграции — в `Shared/Data/`; EF-конфигурации и сущности — в папке своего среза. Миграции никогда не редактируем, только добавляем новые.
- Фронтенд: всё серверное состояние через TanStack Query. Никакого `useEffect`-фетчинга, никакого параллельного кэша в компонентах.

## Команды

Существующие (есть в репозитории):

- `dotnet build --no-restore` — должен проходить перед любым коммитом
- `dotnet test --filter "FullyQualifiedName~Features.Schedule"` — тесты одной фичи (замени `Schedule` на целевую фичу; фильтр по `~Schedule` матчит всё, т.к. в имени сборки есть `LifeScheduler`)
- `dotnet format --verify-no-changes` — CI-гейт
- `cd src/Web && npm run build` — сборка фронтенда (`tsc -b && vite build`)
- `cd src/Web && npm run typecheck` — проверка типов без сборки (`tsc -b --noEmit`, composite project references)
- `cd src/Web && npm run lint` — ESLint

Планируемые (сейчас НЕ определены в `src/Web/package.json` — добавь перед использованием):

- `npm run test -- --run` — однократный прогон Vitest
- `npm run e2e -- --grep "<pattern>"` — E2E по паттерну (имя сценария — из `test(...)` в `tests/*.spec.ts`)

EF Core (SQLite, `dotnet-ef` — локальный tool в `.config/dotnet-tools.json`):

- `dotnet ef migrations add <Name> --project src/LifeScheduler.Api --output-dir Shared/Data/Migrations`
- `dotnet ef database update --project src/LifeScheduler.Api`

## Соглашения

- Никакого `any` в TypeScript. Предпочитай `unknown` + narrowing.
- Бэкенд: один файл на концепт среза (`CreateSchedule.cs`, `ListSchedule.cs`), никаких зависимостей между срезами.
- Никогда не редактируй сгенерированные миграции; всегда добавляй новые.
- Валидация — на границе (запросы API, внешние данные); внутренний код и гарантии фреймворка не перепроверяем.

## Особенности окружения

- `dotnet` установлен в `C:\Program Files\dotnet` (SDK 10.0.404); вызывай его полным путём (см. «Bash: запуск .NET-команд»).
- Фронтенд проксирует `/api` на `http://localhost:5148`; не меняй `vite.config` без запроса.
- Roslyn MCP поставляет плагин dotnet-claude-kit; отдельный `.mcp.json` в проекте не нужен.

## Известные пробелы

- `src/Web/package.json` не содержит скриптов `test` / `e2e` — добавь их, прежде чем ссылаться на эти команды.
- Нет `Directory.Build.props` — общие настройки сборки (Nullable, LangVersion, TreatWarningsAsErrors) пока не централизованы.

## Документация

- `docs/decisions/` — ADR-lite, один файл на решение, шаблон в `_template.md`.
  Пиши новый ADR только по явной просьбе; не создавай файлы в этой папке самостоятельно.
- `docs/api/` — заметки по API, которых нет в официальной доке или где важна версия.
  Создавай файл только по явной просьбе, после того как проблема проверена вручную.

## Память проекта

- `docs/sessions/` — логи сессий, пишутся автоматически Stop-хуком. При старте новой сессии прочитай последний лог, если я попрошу «вспомнить контекст».
- Auto memory (`~/.claude/projects/.../memory/`) — курируется мной через `/memory`; не полагайся на неё как на источник архитектурных правил.

## Критерии готовности

Прежде чем объявить задачу выполненной:
1. `dotnet build --no-restore` проходит.
2. Соответствующие тесты проходят (`dotnet test --filter "FullyQualifiedName~Features.Schedule"`).
3. `dotnet format --verify-no-changes` проходит.
4. Для изменений во фронтенде: `cd src/Web && npm run build` проходит (и `npm run test -- --run`, когда скрипт добавлен).
5. Если изменено больше одного файла — покажи сводку изменений и какие команды ты запускал.

## Браузерная автоматизация

- Используй `playwright-cli` для E2E-проверок, скриншотов и взаимодействия с UI.
- Всегда начинай со `snapshot`, чтобы получить ref'ы элементов. Не угадывай ref'ы.
- Для полей ввода используй `fill`, для кнопок — `click`.
- Сохраняй скриншоты с описательными именами: `screenshot --filename=schedule-created.png`.
- После завершения работы закрывай браузер: `playwright-cli close`.

## Code Intelligence
Для навигации по C#-коду предпочитай LSP-инструменты вместо Grep/Glob:
- `goToDefinition` / `findReferences` для поиска использований и определений.
- `workspaceSymbol` для поиска символов по всему проекту.
- `hover` для получения информации о типе без чтения файла.

Используй Grep/Glob только для поиска по тексту (комментарии, строки, конфиги).
После редактирования кода проверяй диагностику LSP, чтобы сразу видеть ошибки типов.

## Bash: запуск .NET-команд

- Для запуска `dotnet` и других CLI-инструментов **всегда используй полный путь** к исполняемому файлу, а не `export PATH=...`.
- **Правильно:** `& "C:\Program Files\dotnet\dotnet.exe" build --no-restore` (PowerShell) или `"/c/Program Files/dotnet/dotnet.exe" build --no-restore` (bash).
- **Неправильно:** `export PATH="$HOME/.dotnet:$PATH"; dotnet build ...`
- **Почему:** команды с `export` и `$()` в путях Claude Code считает «слишком сложными для автоматического анализа» и запрашивает подтверждение. Это ломает автономную работу.[reference:0]

## Дизайн-система

Перед созданием или изменением любого UI-компонента, страницы или вёрстки:
1. Прочитай `docs/design-refs/DESIGN.md`.
2. Используй только токены из этого файла. Не хардкодь цвета, размеры шрифтов и отступы.
3. Переиспользуй компоненты из `docs/design-refs/UI_KIT.md` (если существует), прежде чем создавать новые.
4. Если нужного токена или компонента нет — предложи добавление, но не подставляй дефолтные значения.
5. Референсные скриншоты лежат в `docs/design-refs/`. Сверяйся с ними визуально.

<!-- claude-reflect: learned rules (saved from the band; edit freely) -->
## Learned rules
- In this app's UI, hover effects must animate smoothly with a gradual fill rather than applying instantly.
- Store session data files only in docs/sessions, never in the web folder or repo root.
- In dark theme, footnote/hint text must be clearly readable: increase text contrast or lighten the hint's background.
<!-- end claude-reflect -->
