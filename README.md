# LifeScheduler

Личный планировщик задач, привычек и календаря. Всё работает локально, включая
голосового помощника: распознавание речи и разбор команд выполняются внутри
приложения — без облачных API, Ollama и внешних серверов.

Репозиторий `project_alpha` мультипроектный; активный проект — [`life_scheduler/`](life_scheduler/).
Полная инструкция по запуску, моделям и настройкам — в [`life_scheduler/README.md`](life_scheduler/README.md).

## Возможности

- **Календарь.** Сетка недели по часам и сетка месяца. Клик по слоту создаёт запись
  с уже заполненным временем, клик по дню — дело без времени.
- **Три типа записей.** Событие (не отмечается), задача (одна отметка выполнения)
  и привычка (отметки по дням, серии и пропуски).
- **Повторения.** Ежедневные и еженедельные записи; `Date` — первое вхождение,
  остальные вычисляются от него.
- **Привычки.** Стрики, habit stacking, тепловая карта, спарклайны и недельные
  графики; отдельная страница прогресса.
- **История.** Прошедшие записи переводит в архив фоновый сервис; они остаются
  в календаре и доступны в разделе истории с серверными фильтрами.
- **Вложения.** Файлы к записи: presigned-загрузка напрямую в хранилище (S3 или
  локальный диск в dev), предпросмотр изображений, PDF и DOCX.
- **Голосовой помощник.** Локальные Whisper.net (STT) и Phi-4-mini ONNX (NLU):
  «отметь планёрку», «встреча с Аней завтра в 15:00», «позвонить маме через час».
  Поддерживаются только безопасные действия — удаление и изменение данных голосом
  недоступны.

## Стек

| Слой | Технологии |
|---|---|
| Бэкенд | .NET 10, ASP.NET Core Minimal APIs, OpenAPI, ProblemDetails |
| Данные | SQLite + EF Core 10, миграции |
| Фронтенд | React 19, TypeScript (strict, без `any`), Vite, TanStack Query |
| Локальный AI | Whisper.net (whisper.cpp) для STT; ONNX Runtime GenAI + Phi-4-mini для NLU |
| Хранилище файлов | `IFileStorage`: локальный диск (`App_Data/attachments`) или S3 |
| Тесты | xUnit, Vitest, Playwright |

## Архитектура

Бэкенд построен по **Vertical Slice Architecture**: единица структуры — не технический
слой, а пользовательский сценарий. Каждая фича живёт в
`src/LifeScheduler.Api/Features/{Feature}/` и владеет своими эндпоинтами, контрактами
запроса/ответа, валидацией и handler'ом. Срезы не вызывают друг друга, а общий код
попадает в `Shared/` только тогда, когда действительно не принадлежит одной фиче.
Подробнее — в [`docs/architecture/vsa.md`](life_scheduler/docs/architecture/vsa.md).

Фронтенд зеркалит структуру: `src/Web/src/features/{feature}/` с `api.ts`,
`queries.ts` и компонентами; всё серверное состояние проходит через TanStack Query.

### Ключевые решения (ADR)

| № | Решение |
|---|---|
| [001](life_scheduler/docs/decisions/001-vertical-slices.md) | Vertical slices вместо Clean Architecture |
| [002](life_scheduler/docs/decisions/002-design-system-claude.md) | Дизайн-система по референсам claude.com |
| [003](life_scheduler/docs/decisions/003-records-without-time.md) | Записи без времени |
| [004](life_scheduler/docs/decisions/004-calendar-component.md) | Собственный календарь вместо библиотеки |
| [005](life_scheduler/docs/decisions/005-theme-switching.md) | Переключатель темы и плавная смена цветов |
| [006](life_scheduler/docs/decisions/006-recurring-events.md) | Повторяющиеся записи |
| [007](life_scheduler/docs/decisions/007-attachments.md) | Вложения: SQLite + S3-совместимое хранилище |
| [008](life_scheduler/docs/decisions/008-entry-history.md) | История прошедших записей |
| [009](life_scheduler/docs/decisions/009-attachment-preview.md) | Предпросмотр вложений на фронтенде |
| [010](life_scheduler/docs/decisions/010-schedule-prefetch-batching.md) | Месячные батчи и фоновая предзагрузка календаря |
| [011](life_scheduler/docs/decisions/011-completion-tracking.md) | Типы записей и отметки выполнения |
| [012](life_scheduler/docs/decisions/012-history-in-calendar-and-search.md) | История в календаре и фильтры истории |
| [013](life_scheduler/docs/decisions/013-custom-date-time-select-fields.md) | Кастомные поля даты, времени и выбора |
| [014](life_scheduler/docs/decisions/014-atomic-habits.md) | Atomic Habits: стрики, stacking и прогресс |
| [015](life_scheduler/docs/decisions/015-voice-assistant-onnx.md) | NLU голосового помощника на ONNX Runtime GenAI |
| [016](life_scheduler/docs/decisions/016-voice-assistant-latency.md) | Кеш префикса и диета промпта |

## Структура репозитория

```text
project_alpha/
├── life_scheduler/
│   ├── src/LifeScheduler.Api/   # .NET API: Features/, Shared/
│   ├── src/Web/                 # React-фронтенд
│   ├── tests/                   # xUnit-тесты
│   ├── scripts/                 # dev.ps1 / dev.sh
│   └── docs/                    # ADR, архитектура, дизайн-референсы
├── e_brain/                     # второй проект, пока не начат
└── docs/sessions/               # логи сессий разработки
```

## API

Основные маршруты:

| Метод | Маршрут | Назначение |
|---|---|---|
| `POST` | `/api/schedules` | создать запись |
| `GET` | `/api/schedules?from&to` | записи диапазона: активные, архивные и отметки привычек |
| `GET` | `/api/schedules/{id}` | одна запись |
| `PUT` | `/api/schedules/{id}` | изменить запись |
| `DELETE` | `/api/schedules/{id}` | удалить запись |
| `POST`/`DELETE` | `/api/schedules/{id}/completion` | отметить / снять отметку выполнения |
| `GET` | `/api/schedules/history` | история с фильтрами |
| `GET` | `/api/habits` | привычки со статистикой |
| `POST` | `/api/schedules/{id}/attachments` | запросить загрузку вложения |
| `POST` | `/api/schedules/{id}/attachments/{attachmentId}/confirm` | подтвердить загрузку |
| `GET` | `/api/schedules/{id}/attachments/{attachmentId}/download` | скачать вложение |
| `DELETE` | `/api/schedules/{id}/attachments/{attachmentId}` | удалить вложение |
| `PUT`/`GET` | `/api/storage/uploads/{attachmentId}`, `/api/storage/download/{attachmentId}` | локальное хранилище в dev |
| `POST` | `/api/assistant/command` | голосовая команда: JSON-текст или аудио |

OpenAPI доступен в режиме разработки.

## Быстрый старт

Нужны .NET 10 SDK, Node.js 20+ и Windows 10/11 x64. Модели локального AI (около
5.5 ГБ) в репозиторий не входят — скачайте их по инструкции из
[`life_scheduler/README.md`](life_scheduler/README.md#модели).

```bash
cd life_scheduler

dotnet tool restore
dotnet ef database update --project src/LifeScheduler.Api
scripts/dev.ps1
```

API поднимется на `http://localhost:5148`, фронтенд — на `http://localhost:5173`.
Для работы над UI без моделей: `scripts/dev.ps1 -NoLocalAi`.

## Тесты

```bash
cd life_scheduler
dotnet build --no-restore
dotnet test
dotnet format --verify-no-changes

cd src/Web
npm install
npm run typecheck
npm run test -- --run
npm run e2e
```

Тесты не загружают AI-модели: в тестовой фабрике `LocalAi:Enabled=false`,
а голосовые зависимости подменяются фейками.

## Документация

- [`life_scheduler/README.md`](life_scheduler/README.md) — запуск, модели, настройки,
  голосовые команды, troubleshooting.
- [`life_scheduler/CLAUDE.md`](life_scheduler/CLAUDE.md) — соглашения проекта и
  критерии готовности.
- [`docs/decisions/`](life_scheduler/docs/decisions/) — ADR.
- [`docs/architecture/`](life_scheduler/docs/architecture/) — вертикальные срезы,
  концепты Atomic Habits.
- [`docs/design-refs/DESIGN.md`](life_scheduler/docs/design-refs/DESIGN.md) —
  дизайн-система: палитра, типографика, отступы.

## Приватность

Во время работы приложение не обращается к внешним LLM/STT-сервисам. Скачивание
моделей с Hugging Face — разовая настройка, а не рантайм-зависимость.
