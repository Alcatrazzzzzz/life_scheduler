# LifeScheduler

Личный планировщик задач и привычек: .NET 10 Minimal API + React 19. Всё работает
локально, включая голосового помощника: распознавание речи и разбор команд выполняются
в процессе приложения, без облачных API, Ollama и внешних серверов.

- STT: Whisper.net (whisper.cpp), модель `ggml-small-q8_0.bin`.
- NLU: ONNX Runtime GenAI + Phi-4-mini, JSON-schema constrained decoding.
  LLamaSharp/GGUF не используется.

## Требования

- .NET 10 SDK
- Node.js 20+ и npm
- Windows 10/11 x64 (голосовой ввод использует Windows Media Foundation для webm/opus)
- 16 ГБ RAM рекомендуется; 8 ГБ может не хватить для обеих моделей
- ~5.5 ГБ на диске под модели (Whisper ~260 МБ + ONNX ~4.6 ГБ)
- Микрофон и браузер Chromium/Firefox для голосового ввода

## Модели

Модели не хранятся в репозитории. Положите их в `src/LifeScheduler.Api/models/`.

```bash
mkdir -p src/LifeScheduler.Api/models

# Whisper small (int8), ~260 МБ
curl -L -o src/LifeScheduler.Api/models/ggml-small-q8_0.bin \
  https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small-q8_0.bin

# Phi-4-mini ONNX (CPU int4), ~4.6 ГБ
base=https://huggingface.co/microsoft/Phi-4-mini-instruct-onnx/resolve/main/cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4
mkdir -p src/LifeScheduler.Api/models/phi-4-mini-onnx-cpu-int4
for f in model.onnx model.onnx.data tokenizer.json tokenizer_config.json \
         vocab.json merges.txt added_tokens.json special_tokens_map.json \
         config.json genai_config.json; do
  curl -L --fail -o "src/LifeScheduler.Api/models/phi-4-mini-onnx-cpu-int4/$f" "$base/$f"
done
```

Важно: **путь к ONNX-модели должен быть ASCII**. Guidance-токенизатор ONNX Runtime GenAI
не открывает пути с кириллицей. Держите репозиторий (и модель) в каталоге, путь к которому
не содержит не-ASCII символов; не переносите модель в папку с русскими буквами.

Если качество распознавания на ваших командах окажется недостаточным, верните более
крупную модель (`ggml-medium.bin` ~1.5 ГБ), поменяв `LocalAi:WhisperModelPath`.

## Запуск

Из папки `life_scheduler/`:

```bash
dotnet ef database update --project src/LifeScheduler.Api
scripts/dev.ps1
```

`scripts/dev.ps1` поднимает API на `http://localhost:5148`, фронтенд на
`http://localhost:5173` и открывает браузер. Вручную:

```bash
dotnet run --project src/LifeScheduler.Api --launch-profile http
cd src/Web && npm install && npm run dev
```

При первом старте API загружает Whisper и ONNX-модель в память (10–30 с). Это ожидаемо:
модели создаются как singleton и больше не перезагружаются. Если моделей нет, приложение
завершится с понятной ошибкой — это сделано специально.

## Голосовой помощник

Кнопка микрофона находится в правом нижнем углу на всех страницах. Сценарии:

- «Отметь планёрку» — отметить привычку за сегодня.
- «Я сделал зарядку вчера» — отметить за указанный день.
- «Какие у меня привычки на сегодня» — показать список.
- «Создай привычку читать каждый день» — создать привычку.
- «Встреча с Аней завтра в 15:00» — создать событие в календаре.
- «Позвонить маме через час» — относительное время считается от текущего.
- «Встреча до обеда» — событие 12:00–13:00. Если названа одна граница времени,
  вторая берётся из длительности по умолчанию в 1 час.

Поддерживаются только безопасные действия; удаление и изменение данных голосом
недоступны. Модель получает JSON-схему ответа и не может вернуть невалидный JSON.

### Шорткат с клавиатуры

Шорткат настраивается в диалоге «Настройки» (иконка шестерёнки рядом с переключателем
темы). По умолчанию — `Ctrl+Shift+Space`. Комбинация обязана содержать хотя бы один
модификатор (`Ctrl`, `Alt`, `Shift`, `Meta`). Режимы:

- **Обычное нажатие** — нажатие начинает запись, повторное нажатие останавливает.
- **Удержание** — запись начинается, только если держать клавишу дольше задержки
  (0–2000 мс); отпускание клавиши останавливает запись.

Настройки хранятся в таблице `AppSettings` в SQLite и читаются/пишутся через
`GET`/`PUT /api/settings/voice-shortcut`. Шорткат не срабатывает, когда фокус в поле
ввода или открыт диалог.

Проверка без микрофона:

```bash
curl -s -X POST http://localhost:5148/api/assistant/command \
  -H "Content-Type: application/json" \
  -d '{"text":"отметь планёрку"}'
```

Проверка аудио (webm/opus или wav):

```bash
curl -s -X POST http://localhost:5148/api/assistant/command \
  -F "audio=@sample.webm;type=audio/webm"
```

Ожидаемое время ответа на типичном 8-ядерном CPU: 10–30 с, первый запрос после старта
может быть дольше. В прототипе ONNX-модель с JSON-schema guidance отвечала за ~13 с
вместе с загрузкой.

## Настройки

Секция `LocalAi` в `src/LifeScheduler.Api/appsettings.json`:

| Ключ | По умолчанию | Назначение |
|---|---|---|
| `Enabled` | `true` | `false` — не грузить модели (помощник вернёт 503) |
| `WhisperModelPath` | `models/ggml-small-q8_0.bin` | путь к ggml-модели Whisper |
| `OnnxModelPath` | `models/phi-4-mini-onnx-cpu-int4` | папка ONNX-модели |
| `WhisperLanguage` | `ru` | язык распознавания |
| `WhisperThreads` | `0` | `0` = число логических ядер |
| `OnnxMaxLength` | `4096` | максимальная длина последовательности ONNX |
| `MaxAudioBytes` | `10485760` | лимит загружаемого аудио (10 МБ) |

Пути относительны папки проекта API. Можно указывать абсолютные пути, но они должны
быть ASCII (см. выше).

## Тесты

```bash
dotnet build --no-restore
dotnet test --filter "FullyQualifiedName~Features.VoiceAssistant"
dotnet format --verify-no-changes

cd src/Web
npm run typecheck
npm run build
npm run test -- --run
```

Тесты не загружают модели: `LocalAi:Enabled=false` в тестовой фабрике, а голосовые
зависимости подменяются фейками.

Интеграционный тест с живой моделью (медленный, ~5 ГБ RAM):

```bash
RUN_LOCAL_AI_TESTS=1 dotnet test --filter "Category=LocalAi"
```

Другую папку ONNX-модели можно проверить без изменения `appsettings.json`:

```bash
RUN_LOCAL_AI_TESTS=1 LOCALAI_ONNX_PATH="models/phi-4-mini-onnx-cpu-int4" \
  dotnet test --filter "Category=LocalAi"
```

## Troubleshooting

- **«ONNX-модель не найдена»** — проверьте `LocalAi:OnnxModelPath` и наличие
  `genai_config.json`, `model.onnx` и `model.onnx.data` в папке модели.
- **Ошибка guidance-токенизатора / «Unable to open guidance tokenizer data»** — путь к
  ONNX-модели содержит не-ASCII символы (кириллицу). Перенесите модель/repo в ASCII-путь.
- **422 «Не удалось декодировать аудио»** — Windows Media Foundation не смог открыть
  webm/opus. Установите «Web Media Extensions» из Microsoft Store или используйте WAV.
  На Windows N также нужен Media Feature Pack.
- **Мало RAM / всё тормозит** — возьмите ещё меньшую ggml-модель Whisper (например,
  `ggml-base-q8_0.bin`); для NLU используйте более компактную ONNX-модель (если она
  совместима с ONNX Runtime GenAI).
- **`dotnet watch` долго перезапускается** — на каждое изменение C# модели грузятся
  заново. Для работы над не-AI кодом поставьте `"Enabled": false` или запустите
  `scripts/dev.ps1 -NoLocalAi`.
- **E2E без моделей** — `playwright.config.ts` запускает API с
  `LocalAi__Enabled=false`.

## Приватность

Во время работы приложение не обращается к внешним LLM/STT-сервисам. Скачивание моделей
с Hugging Face — разовая настройка, а не рантайм-зависимость. В коде нет обращений к
`api.anthropic.com`, `api.openai.com`, `speech.yandex.net`, Ollama и т.п.
