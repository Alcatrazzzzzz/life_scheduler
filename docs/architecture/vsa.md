Vertical Slice Architecture в LifeScheduler

1. Идея в одном абзаце
Vertical slice architecture (VSA) — это способ организации кода, при котором единицей структуры является не технический слой (контроллер / сервис / репозиторий), а конкретный пользовательский сценарий. Один сценарий — «создать запись в расписании» — живёт целиком в одной папке: эндпоинт, модели запроса и ответа, валидация, handler, иногда запросы к БД. Чтобы понять, как работает создание записи, вы открываете один файл и читаете его сверху вниз, не прыгая между Controllers/, Services/ и Repositories/.

В этом проекте (см. CLAUDE.md) срез называется фичей и лежит в src/LifeScheduler.Api/Features/{Feature}/, а внутри фичи — по файлу на концепт: CreateSchedule.cs, ListSchedule.cs и т.д.

Ключевая мысль: граница изменений совпадает с границей папки. Добавили фичу — добавили папку. Удалили фичу — удалили папку, и ничего не сломалось.

2. Чем VSA отличается от слоистой архитектуры
Классическая слоистая структура:

Controllers/
    ScheduleController.cs      ← тонкий
    TasksController.cs
Services/
    ScheduleService.cs         ← вся логика
    TasksService.cs
Repositories/
    ScheduleRepository.cs      ← доступ к данным
    TasksRepository.cs
Models/
    ScheduleDto.cs
    TaskDto.cs
Чтобы добавить поле Notes в создание записи, вам нужно тронуть минимум четыре папки: DTO, контроллер, сервис, репозиторий. Изменение «размазано» по слоям, а бизнес-операция нигде не собрана целиком.

Структура срезов:

Features/
    Schedule/
        ScheduleEndpoints.cs   ← только регистрация групп
        CreateSchedule.cs      ← request, response, handler, валидация
        ListSchedule.cs
        UpdateSchedule.cs
        DeleteSchedule.cs
    Tasks/
        TasksEndpoints.cs
        CreateTask.cs
        ...
Сравнение по критериям (таблица):

Критерий	Слоистая	Vertical slices
Единица организации	технический слой	сценарий
Где лежит бизнес-операция	разбросана	в одном файле
Добавление поля	3–5 файлов в разных папках	1–2 файла в одной папке
Стоимость удаления фичи	высокая, риск мёртвого кода	удалить папку
Переиспользование	агрессивное, часто преждевременное	дублирование допустимо
Навигация по коду	«где это?»	«какой сценарий?»
Тесты	по слоям, с моками	по сценарию, сквозные
Слоистая архитектура оптимизирована под переиспользование. VSA оптимизирована под стоимость изменения. Для приложения-планировщика, где фичи добавляются и меняются часто, а общий «сервисный» слой растёт медленно, вторая оптимизация выгоднее.

3. Почему это подходит именно этому проекту
Домен вертикальный, а не горизонтальный. «Расписание», «задачи», «привычки» мало пересекаются по данным. Общий слой здесь был бы тонким и надуманным.

Minimal APIs уже подталкивают к срезам. Эндпоинт — это лямбда или статический метод, ему не нужен контроллер как контейнер. Естественно сложить всё рядом.

Малая команда / один разработчик. Нет нужды в жёстких контрактах между командами; важнее скорость и локальность изменений.

Тесты по сценариям. dotnet test --filter "FullyQualifiedName~Features.Schedule" — это ровно фильтр по срезу/фиче, а не по слою. Структура тестов повторяет структуру кода.

Фронтенд симметричен. TanStack Query тоже мыслит сценариями: useSchedules(), useCreateSchedule(). Срез на бэкенде и папка фичи на фронтенде — зеркала друг друга.

4. Карта репозитория
src/
  LifeScheduler.Api/
    Program.cs                 ← композиционный корень, только регистрации
    Features/
      Schedule/
        ScheduleEndpoints.cs
        CreateSchedule.cs
        ListSchedule.cs
        UpdateSchedule.cs
        DeleteSchedule.cs
      Tasks/
        ...
    Shared/
      Data/
        LifeSchedulerDbContext.cs
        ScheduleEntry.cs
        ScheduleEntryConfiguration.cs
        Migrations/
      Errors/
        ProblemDetailsSetup.cs
      Time/
        Clock.cs
  Web/
    src/
      features/
        schedule/
          api.ts
          queries.ts
          components/
          SchedulePage.tsx
        tasks/
          ...
      shared/
        http/
          client.ts
        ui/
      App.tsx
tests/
  LifeScheduler.Tests/
    Features/
      Schedule/
        ScheduleApiFactory.cs
        ScheduleEndpointsTests.cs
Правило из CLAUDE.md: cross-cutting код идёт в Shared/ и никогда внутрь Features/{Feature}/. Внутри фичи не должно быть «общих» для всех срезов хелперов — если хелпер нужен двум фичам, он поднимается в Shared/.

Направление зависимостей: Features → Shared, никогда Shared → Features. DbContext, сущности и EF-конфигурации живут в Shared/Data/, потому что схема БД глобальная. Срез обращается к DbContext напрямую — не наоборот.

5. Анатомия одного среза
Разберём CreateSchedule.cs целиком — это типичный срез «создать запись».

namespace LifeScheduler.Api.Features.Schedule;

public static class CreateSchedule
{
    // 1. Входной контракт. Принадлежит срезу, а не общему Models/.
    public sealed record Request(
        string Title,
        DateOnly Date,
        TimeOnly Start,
        TimeOnly End,
        string? Notes);

    // 2. Выходной контракт. Тоже локальный — не выносится в общий DTO.
    public sealed record Response(
        Guid Id,
        string Title,
        DateOnly Date,
        TimeOnly Start,
        TimeOnly End,
        string? Notes);

    // 3. Регистрация — единственное, что видит внешний мир.
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", HandleAsync)
             .WithName("CreateSchedule")
             .Produces<Response>(StatusCodes.Status201Created)
             .ProducesValidationProblem();

    // 4. Handler. Вся операция здесь, читается сверху вниз.
    private static async Task<Results<Created<Response>, ValidationProblem>> HandleAsync(
        Request request,
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var entry = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Date = request.Date,
            Start = request.Start,
            End = request.End,
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.ScheduleEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(
            $"/api/schedules/{entry.Id}",
            new Response(entry.Id, entry.Title, entry.Date, entry.Start, entry.End, entry.Notes));
    }

    // 5. Валидация рядом с использованием. Не в общем валидаторе.
    private static Dictionary<string, string[]> Validate(Request request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors[nameof(request.Title)] = ["Название обязательно."];
        }

        if (request.End <= request.Start)
        {
            errors[nameof(request.End)] = ["Время окончания должно быть позже начала."];
        }

        return errors;
    }
}
Что здесь важно:

Контракты локальны. Request/Response существуют только ради этого среза. Если для ListSchedule понадобится другой набор полей — у него будет свой Response, и это нормально. Дублирование DTO дешевле, чем связанный общий контракт.

Handler — статический метод. Ему не нужен DI-контейнер для собственного экземпляра; зависимости приходят параметрами Minimal API.

Валидация — приватный метод в том же файле. Она не переиспользуется нигде, значит не выносится.

Нет репозитория. В срезе допустимо работать с DbContext напрямую. Абстракция над EF — это обёртка, которая почти всегда оправдана только для тестируемости, а тестируемость здесь обеспечивается WebApplicationFactory.

Порядок чтения сверху вниз: контракты → регистрация → поведение → детали. Человек, открывший файл, получает ответ за один экран.

6. Регистрация эндпоинтов через MapGroup
По конвенции CLAUDE.md — один файл на фичу, где собираются все её срезы.

namespace LifeScheduler.Api.Features.Schedule;

public static class ScheduleEndpoints
{
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/schedules")
                       .WithTags("Schedule");

        CreateSchedule.Map(group);
        ListSchedule.Map(group);
        UpdateSchedule.Map(group);
        DeleteSchedule.Map(group);

        return app;
    }
}
Program.cs остаётся тонким композиционным корнем:

text
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<LifeSchedulerDbContext>(/* ... */);

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapScheduleEndpoints();

app.Run();
Почему MapGroup, а не app.MapGet("/api/schedules", ...) в каждом файле:

префикс маршрута задаётся один раз;

общие метаданные (WithTags, фильтры, авторизация) применяются к группе;

путь и версия API меняются в одном месте;

регистрация среза в группе — одна строка CreateSchedule.Map(group).

7. Валидация: где и как
Есть три допустимых уровня, и их не надо смешивать.

Формат запроса — то, что отсекается моделью и binding'ом: кривой JSON, отсутствующее обязательное поле. Это делает ASP.NET Core.

Бизнес-правила одного среза — например, «конец позже начала». Живёт в файле среза, как Validate выше.

Инварианты домена, общие для нескольких срезов — например, «запись не может пересекаться с другой записью». Это уже не валидация формы, а правило уровня данных; оно уходит в Shared/ или в метод над DbContext, который переиспользуют срезы.

Ключевое: не заводите единый IValidator<CreateScheduleRequest>, который импортирует пол-проекта. Валидатор, нужный одному срезу, живёт в срезе. Общий валидатор — только когда правило действительно общее, и тогда он в Shared/.

Ответ на ошибку — всегда ValidationProblem (RFC 7807 ProblemDetails):

text
return TypedResults.ValidationProblem(errors);
Пример тела ответа:

{
"type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
"title": "One or more validation errors occurred.",
"status": 400,
"errors": {
"End": ["Время окончания должно быть позже начала."]
}
}

TypedResults вместо IResult даёт компилятору и OpenAPI точную информацию о возможных ответах — это бесплатная документация и типизация.

8. Работа с данными внутри среза
Срез может обращаться к DbContext напрямую: читать, проецировать, писать.

text
public static class ListSchedule
{
    public sealed record Response(Guid Id, string Title, DateOnly Date, TimeOnly Start, TimeOnly End);

    public static void Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", HandleAsync)
             .WithName("ListSchedule")
             .Produces<IReadOnlyList<Response>>();

    private static async Task<Ok<IReadOnlyList<Response>>> HandleAsync(
        DateOnly? from,
        DateOnly? to,
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        var query = db.ScheduleEntries.AsNoTracking();

        if (from is not null)
            query = query.Where(x => x.Date >= from);
        if (to is not null)
            query = query.Where(x => x.Date <= to);

        var items = await query
            .OrderBy(x => x.Date).ThenBy(x => x.Start)
            .Select(x => new Response(x.Id, x.Title, x.Date, x.Start, x.End))
            .ToListAsync(ct);

        return TypedResults.Ok<IReadOnlyList<Response>>(items);
    }
}
Что здесь видно:

Проекция прямо в Response — не тащим сущность наружу. EF сгенерирует SELECT только нужных колонок.

AsNoTracking для чтения — срез сам решает, нужен ли трекинг.

Фильтры — в query, а не в памяти после ToListAsync.

Никакого общего ScheduleRepository. Если появится десятый срез с похожей выборкой, можно вынести функцию-хелпер в Shared/Data/, но не раньше.

Запись идёт через Add + SaveChangesAsync. SaveChangesAsync уже транзакционен. Явная транзакция нужна только когда несколько независимых SaveChanges должны быть всё-или-ничего — и это сигнал, что, возможно, операция должна быть одним срезом, а не двумя.

9. Что живёт в Shared и почему это узкое место
Shared/ — это не «всё общее», а минимальный набор вещей, который не принадлежит ни одной фиче:

Shared/Data/LifeSchedulerDbContext.cs — контекст EF общий, иначе каждая фича тянула бы свой контекст и свою миграцию. Осознанное исключение из принципа изоляции: схема БД — глобальная.

Shared/Data/{Entity}.cs и {Entity}Configuration.cs — сущности и EF-конфигурации тоже глобальны, потому что на них ссылается DbContext и миграции. Сущность, созданная одним срезом, потенциально доступна всем — это цена общей схемы.

Shared/Data/Migrations/ — миграции тоже глобальны. По CLAUDE.md их никогда не редактируют вручную, только добавляют.

Shared/Errors/ — настройка ProblemDetails, общий обработчик исключений.

Shared/Time/Clock.cs — абстракция времени, чтобы тесты были детерминированными. Cross-cutting: «сейчас» нужно расписанию, напоминаниям и привычкам.

Shared/Auth/ — аутентификация и текущий пользователь. Любой срез может спросить ICurrentUser.

Правило: Shared/ растёт медленно и осознанно. Если вы тянетесь туда каждый день — скорее всего, вы строите слой заново, просто под другим именем.

10. Границы между срезами
Главное правило: срезы не должны вызывать друг друга.

CreateSchedule.cs не импортирует ListSchedule.cs и не вызывает его handler.

Фича Schedule не импортирует типы из Features.Tasks.

Если двум фичам нужен общий тип — он поднимается в Shared/Contracts/ или дублируется.

Дублирование допустимо: два DTO с одинаковыми полями, но разными владельцами, эволюционируют независимо. Общий DTO создаёт связанность, из-за которой изменение ради «Задач» ломает «Расписание». В VSA предпочтение отдаётся изоляции, а не DRY. Классическая формула: duplication is far cheaper than the wrong abstraction.

Когда фичи действительно обязаны координироваться (например, «создание задачи создаёт напоминание»), есть два честных варианта:

Оркестрирующий срез. Отдельный файл CreateTaskWithReminder.cs, который делает обе операции в одной транзакции. Он не зависит от внутренностей других срезов, только от DbContext.

Доменное событие. Срез пишет событие в outbox-таблицу, отдельный обработчик его читает. Тяжелее и нужно только при реальной асинхронности.

Чего делать нельзя: «просто вызвать TasksService из Schedule». Сервис между фичами — это тот самый горизонтальный слой, от которого мы уходим, только в миниатюре.

11. Тесты: структура повторяет срезы
Тест — это тоже срез, только по поведению.

tests/LifeScheduler.Tests/
  Features/
    Schedule/
      ScheduleApiFactory.cs
      ScheduleEndpointsTests.cs
Типичный тест идёт через HTTP, а не через внутренние методы, потому что внутренние методы — деталь реализации среза:

text
public class ScheduleEndpointsTests : IClassFixture<ScheduleApiFactory>
{
    private readonly HttpClient _client;

    public ScheduleEndpointsTests(ScheduleApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task CreateSchedule_WithValidData_ReturnsCreated()
    {
        var request = new { title = "Планёрка", date = "2026-11-16", start = "10:00", end = "10:30" };

        var response = await _client.PostAsJsonAsync("/api/schedules", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, created.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task CreateSchedule_WhenEndBeforeStart_ReturnsValidationProblem()
    {
        var request = new { title = "Планёрка", date = "2026-11-16", start = "10:30", end = "10:00" };

        var response = await _client.PostAsJsonAsync("/api/schedules", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("end", out _));
    }
}
ScheduleApiFactory — это WebApplicationFactory<Program> с подменённой БД (in-memory SQLite). Подмена инфраструктурная, а не моками сервисов.

Фильтр --filter "FullyQualifiedName~Features.Schedule" запускает ровно тесты одной фичи. Именно ~Features.Schedule, а не ~Schedule — иначе под фильтр попадёт вся сборка LifeScheduler, в имени которой тоже есть Schedule.

Что тестировать на уровне среза:

happy path;

каждая ветка валидации;

граничные значения (конец == началу, пустая строка, null);

коды ответов и форму тела ошибки;

авторизацию, если срез её требует.

Модульные тесты на приватный Validate не нужны: он покрывается через эндпоинт.

12. Фронтенд: срезы с той же границей
Принцип тот же: папка фичи на фронте соответствует фиче на бэке, а запросы группируются по сценарию.

text
src/Web/src/features/schedule/
  api.ts          ← чистые функции HTTP
  queries.ts      ← ключи TanStack Query + хуки
  components/
    ScheduleList.tsx
    CreateScheduleForm.tsx
  SchedulePage.tsx
api.ts — без состояния, только транспорт и типы:

text
export interface ScheduleEntry {
  id: string
  title: string
  date: string
  start: string
  end: string
  notes: string | null
}

export interface CreateScheduleInput {
  title: string
  date: string
  start: string
  end: string
  notes?: string
}

export async function createSchedule(input: CreateScheduleInput): Promise<ScheduleEntry> {
  const res = await fetch('/api/schedules', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!res.ok) throw await toApiError(res)
  return (await res.json()) as ScheduleEntry
}
queries.ts — ключи и хуки, единственное место, где живёт серверное состояние:

export const scheduleKeys = {
  all: ['schedule'] as const,
  list: (from?: string, to?: string) => [...scheduleKeys.all, 'list', { from, to }] as const,
}

export function useSchedules(from?: string, to?: string) {
  return useQuery({
    queryKey: scheduleKeys.list(from, to),
    queryFn: () => listSchedules(from, to),
  })
}

export function useCreateSchedule() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createSchedule,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scheduleKeys.all }),
  })
}
Правила из CLAUDE.md:

Никакого useEffect-фетчинга. Всё серверное состояние — через useQuery/useMutation.

Никакого второго кэша в компонентах. Не копируйте данные из query в useState — это рассинхронизируется при инвалидации.

Ключи — фабрика. Инвалидация по префиксу scheduleKeys.all сбрасывает и список, и детали.

Типы без any. Ответ API — unknown, затем narrowing или Zod-схема.

Компонент не знает про URL. HTTP-детали спрятаны в api.ts.

Композиция фич на фронте идёт через страницы, а не через общий стор. SchedulePage.tsx собирает компоненты своей фичи; App.tsx — маршруты и провайдеры. Если двум фичам нужен общий компонент (кнопка, модалка), он идёт в src/Web/src/shared/ui/, по той же логике, что Shared/ на бэке.

13. Сквозной сценарий: как проходит изменение
Возьмём задачу «добавить поле location к записи расписания».

Бэкенд, срез создания. В CreateSchedule.cs добавляем string? Location в Request и Response, в Validate проверяем длину, при маппинге — Location = request.Location?.Trim().

Бэкенд, срез чтения. В ListSchedule.cs добавляем Location в Response и в Select.

Сущность и миграция. Поле в ScheduleEntry (Shared/Data/) и новая миграция. Существующую не правим.

Тесты. По одному кейсу в тесты create/list.

Фронтенд. Поле в ScheduleEntry/CreateScheduleInput, поле формы, колонка в списке.

Проверки. dotnet build --no-restore, dotnet test --filter "FullyQualifiedName~Features.Schedule", dotnet format --verify-no-changes, npm run typecheck, npm run test -- --run.

Тронуто четыре файла кода в двух папках, плюс миграция и тесты. В слоистой архитектуре то же изменение разошлось бы по DTO-папке, контроллеру, сервису, репозиторию и их тестам.

14. Антипаттерны: как незаметно вернуться к слоям
VSA ломается не сразу, а постепенно. Типичные трещины:

Features/Schedule/Shared/ — общий хелпер внутри фичи для нескольких срезов. Через месяц его импортируют все, через два он превращается в сервис. Либо срез самодостаточен, либо хелпер уходит в Shared/ верхнего уровня.

IScheduleService — интерфейс, который «чтобы не дублировать код» инжектится во все срезы. Это контроллер+сервис с другим названием.

Общий ScheduleDto для всех срезов. Связанность растёт. Если два среза возвращают одинаковый Response — это ещё не повод объединять; завтра они разойдутся.

Срезы, вызывающие друг друга. UpdateSchedule дёргает ListSchedule.HandleAsync — неявная иерархия вместо плоской структуры.

«Срезы» по слоям. Features/Schedule/Handlers/, Features/Schedule/Validators/, Features/Schedule/Repositories/ внутри фичи — вы просто переименовали старую структуру.

Божественный срез. Один ManageSchedule.cs на 600 строк со всеми действиями. Срез — это одно действие.

Преждевременное переиспользование. Абстракция «на будущее» в проекте, где будущее ещё не наступило. Дублируйте, пока не появится третье повторение.

Shared → Features. Если Shared/Data/LifeSchedulerDbContext.cs импортирует Features.Schedule.ScheduleEntry, вы перевернули направление зависимостей. Сущность должна быть в Shared/Data/, а не в фиче.

Полезный тест на здоровье архитектуры: можно ли удалить папку фичи и остаться с компилируемым проектом? Если нет — есть скрытые зависимости.

15. Чеклист добавления новой фичи
Создать src/LifeScheduler.Api/Features/{Feature}/.

Для каждого сценария — файл: CreateX.cs, ListX.cs, UpdateX.cs, DeleteX.cs, GetX.cs.

В каждом файле: локальные Request/Response (если нужны), Map, HandleAsync, Validate.

Сущность и EF-конфигурация — в Shared/Data/, если это новая таблица.

Добавить {Feature}Endpoints.cs с MapGroup("/api/{resource}").

Зарегистрировать app.Map{Feature}Endpoints() в Program.cs.

Если нужны новые таблицы — модель и миграция в Shared/Data/.

Тесты в tests/LifeScheduler.Tests/Features/{Feature}/.

На фронте — src/Web/src/features/{feature}/ с api.ts, queries.ts, компонентами и страницей.

Прогнать все команды из «Критериев готовности» в CLAUDE.md.

Чего в чеклисте нет — и это осознанно: регистрации сервисов, создания репозиториев, маппингов между слоями, папок под DTO.

16. Когда VSA не стоит применять
Сильно связный домен. Если почти каждая операция затрагивает общее ядро (бухгалтерия, биллинг, сложный workflow), дублирование становится вредным, и доменная модель в духе DDD с богатыми агрегатами может быть лучше.

Очень большая команда. Когда десятки разработчиков работают параллельно, общие контракты и явные слои помогают координироваться.

Жёсткие кросс-срезовые требования. Если отчётность должна одинаково считать по единым правилам во всех фичах, эти правила выносятся в общее ядро — и архитектура дрейфует к слоистой.

Команда, не знакомая с подходом. Без дисциплины VSA вырождается в хаос с дублями.

Для планировщика с его слабо связанными фичами риски умеренные, а выгоды — реальные.

17. Как относиться к дублированию
В VSA дублирование — не долг, а инструмент. Разница между хорошим и плохим:

Хорошее: два среза независимо описывают, какие поля им нужны. Совпали — не страшно, разойдутся — тоже нормально.

Плохое: два среза содержат скопированную бизнес-логику, которую придётся поправить в двух местах.

18. Что делать, когда проект растёт
Фич становится много → группируйте по подсистемам: Features/Planning/Schedule/, Features/Habits/... Глубина папок — не больше трёх уровней внутри Features/.

Появляются общие правила → Shared/ растёт, но остаётся плоским: Shared/Data, Shared/Errors, Shared/Time, Shared/Auth. Не плодите Shared/Common/Helpers.

Нужны события → отдельный Shared/Bus/ с outbox. Только при реальной необходимости.

Нужен OpenAPI-контракт для фронта → генерация типов из /openapi. Срезы с TypedResults и Produces из коробки дают точную схему.

Нужна документация решений → docs/decisions/.

Итог
Vertical slice architecture — это не набор библиотек и не структура папок, а правило границ. Оно говорит: единица структуры — сценарий, а не слой; граница изменений — папка; общее — только то, что физически не принадлежит одной фиче.

Применительно к LifeScheduler это значит:

Features/{Feature}/ — по файлу на действие, контракты и валидация внутри;

Shared/Data/ — DbContext, сущности, EF-конфигурации, миграции;

{Feature}Endpoints.cs — единственное место регистрации группы;

Shared/ — данные, ошибки, время, авторизация, и ничего «на всякий случай»;

срезы не зависят друг от друга; дублирование контрактов допустимо, дублирование логики — нет;

фронтенд зеркалит структуру: src/Web/src/features/{feature}/;

тесты повторяют срезы и ходят через HTTP.

Главный практический критерий здоровья: вы можете открыть один файл и прочитать сценарий от эндпоинта до записи в БД целиком, а удаление папки фичи не ломает сборку. Пока это так — архитектура работает.