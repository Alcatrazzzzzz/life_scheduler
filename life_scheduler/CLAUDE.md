# CLAUDE.md — life_scheduler

Трекер задач: бэкенд .NET 10 + фронтенд React. Все пути ниже — от папки `life_scheduler/`.
Общие правила репозитория — в `../CLAUDE.md`.

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

Все команды выполняются из папки `life_scheduler/`.

Существующие (есть в репозитории):

- `dotnet build --no-restore` — должен проходить перед любым коммитом
- `dotnet test --filter "FullyQualifiedName~Features.Schedule"` — тесты одной фичи (замени `Schedule` на целевую фичу; фильтр по `~Schedule` матчит всё, т.к. в имени сборки есть `LifeScheduler`)
- `dotnet format --verify-no-changes` — CI-гейт
- `cd src/Web && npm run build` — сборка фронтенда (`tsc -b && vite build`)
- `cd src/Web && npm run typecheck` — проверка типов без сборки (`tsc -b --noEmit`, composite project references)
- `cd src/Web && npm run lint` — ESLint
- `cd src/Web && npm run test -- --run` — однократный прогон Vitest
- `cd src/Web && npm run e2e -- --grep "<pattern>"` — E2E по паттерну (имя сценария — из `test(...)` в `tests/*.spec.ts`)

EF Core (SQLite, `dotnet-ef` — локальный tool, манифест в корне репозитория: `../.config/dotnet-tools.json`):

- `dotnet ef migrations add <Name> --project src/LifeScheduler.Api --output-dir Shared/Data/Migrations`
- `dotnet ef database update --project src/LifeScheduler.Api`

## Соглашения

- Никакого `any` в TypeScript. Предпочитай `unknown` + narrowing.
- Бэкенд: один файл на концепт среза (`CreateSchedule.cs`, `ListSchedule.cs`), никаких зависимостей между срезами.
- Никогда не редактируй сгенерированные миграции; всегда добавляй новые.
- Валидация — на границе (запросы API, внешние данные); внутренний код и гарантии фреймворка не перепроверяем.

## Особенности окружения

- Фронтенд проксирует `/api` на `http://localhost:5148`; не меняй `vite.config` без запроса.
- Скрипты запуска dev-окружения: `scripts/dev.ps1` (Windows) и `scripts/dev.sh`.

## Известные пробелы

- Нет `Directory.Build.props` — общие настройки сборки (Nullable, LangVersion, TreatWarningsAsErrors) пока не централизованы.

## Документация

- `docs/decisions/` — ADR-lite, один файл на решение, шаблон в `_template.md`.
  Пиши новый ADR только по явной просьбе; не создавай файлы в этой папке самостоятельно.
- `docs/api/` — заметки по API, которых нет в официальной доке или где важна версия.
  Создавай файл только по явной просьбе, после того как проблема проверена вручную.

## Критерии готовности

Прежде чем объявить задачу выполненной (из папки `life_scheduler/`):
1. `dotnet build --no-restore` проходит.
2. Соответствующие тесты проходят (`dotnet test --filter "FullyQualifiedName~Features.Schedule"`).
3. `dotnet format --verify-no-changes` проходит.
4. Для изменений во фронтенде: `cd src/Web && npm run build` и `npm run test -- --run` проходят.
5. Если изменено больше одного файла — покажи сводку изменений и какие команды ты запускал.

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
- In dark theme, footnote/hint text must be clearly readable: increase text contrast or lighten the hint's background.
- Add hover animation to large buttons: scale the button border slightly, not the text, and add a subtle semi-transparent border on hover.
- Keep vertical slices independent: no slice references another; new features follow this slice structure.
- When writing an ADR, read only the last three ADR files for context, not more.
<!-- end claude-reflect -->
