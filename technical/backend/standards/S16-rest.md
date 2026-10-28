# S16. REST: реализация HTTP API
## Назначение
Этот документ определяет переиспользуемый профиль реализации HTTP API на ASP.NET Core MVC с CQRS. Он задаёт общие правила разработки, но не выбирает ресурсы, полномочия и бизнес-поведение конкретного приложения.
Стандарт используется:
* при проектировании новых API endpoint'ов;
* при описании API в feature-документации;
* при реализации MVC controllers;
* при изменении существующих HTTP-контрактов;
* при генерации или изменении кода AI-агентом.
Контроллер является **transport adapter** между HTTP и Application layer.
Контроллер не содержит бизнес-логику и не реализует application use case самостоятельно.
---
## Применимость и условные обозначения
MVC controllers, CQRS buses и единый result mapper образуют область применения этого стандарта; стандарт не предписывает переделывать приложения другого архитектурного типа. `ICommandBus`, `IQueryBus`, `ApiResponse<T>`, `ToActionResult`, `ApiVersions`, `ApiRoutePrefixes`, `ApiEndpointGroups` и permission-атрибут в примерах обозначают переиспользуемые абстракции инфраструктуры, а не встроенный комплект ASP.NET Core. Их конкретные реализации и пакеты определяются вне стандарта.
`Record`, `records` и команды с этим именем являются нейтральными учебными обозначениями. Примеры формы endpoint не являются готовыми контрактами какого-либо продукта. Список ошибок выбирается для реальной операции, а не копируется из примера.
## 1. Базовая архитектура
HTTP API реализуется через ASP.NET Core MVC controllers.
Контроллер:
1. принимает HTTP request;
2. выполняет model binding;
3. передаёт данные в соответствующий application command или query;
4. передаёт `CancellationToken`;
5. преобразует application result в HTTP response через общий механизм `ToActionResult(...)`.
Типовой поток:
```text
HTTP request
    ↓
MVC Controller
    ↓
Command / Query
    ↓
ICommandBus / IQueryBus
    ↓
Command handler / Query handler
    ↓
Result
    ↓
ToActionResult(...)
    ↓
HTTP response
```
Контроллер не должен:
* содержать бизнес-правила;
* обращаться напрямую к репозиториям;
* обращаться напрямую к `DbContext`;
* управлять транзакциями;
* самостоятельно реализовывать optimistic concurrency;
* самостоятельно преобразовывать application/domain errors в HTTP;
* дублировать validation logic;
* оркестрировать несколько application use cases без отдельного требования;
* ловить application/domain exceptions только ради преобразования их в HTTP response.
---
## 2. Базовая структура контроллера
Контроллер объявляется как:
```csharp
[ApiController]
[ApiVersion(ApiVersions.V1)]
[Route(ApiRoutePrefixes.VersionedApi + "/records")]
[EndpointGroupName(ApiEndpointGroups.V1)]
public sealed class RecordsController(
    ICommandBus commands,
    IQueryBus queries) : ControllerBase
{
}
```
Обязательные соглашения:
* контроллер наследуется от `ControllerBase`;
* используется `[ApiController]`;
* версия API задаётся через `ApiVersions`;
* route prefix задаётся через `ApiRoutePrefixes.VersionedApi`;
* endpoint относится к соответствующей OpenAPI group через `EndpointGroupName`;
* application use cases вызываются через `ICommandBus` и `IQueryBus`;
* зависимости предпочтительно объявляются через primary constructor;
* контроллер объявляется `sealed`, если нет явной причины для наследования.
Не следует дублировать строковые значения, уже определённые shared-константами.
Например:
```csharp
public static class ApiRoutePrefixes
{
    public const string VersionedApi = "api/v{version:apiVersion}";
}
```
Не следует писать:
```csharp
[Route("api/v{version:apiVersion}/records")]
```
если для этого уже существует `ApiRoutePrefixes.VersionedApi`.
---
## 3. Маршрутизация
### 3.1. Общий prefix
Версионируемый API использует:
```text
api/v{version:apiVersion}/...
```
через:
```csharp
ApiRoutePrefixes.VersionedApi
```
Пример:
```csharp
[Route(ApiRoutePrefixes.VersionedApi + "/records")]
```
### 3.2. Resource routes
Для HTTP API предпочтительны resource-oriented routes.
Пример:
```text
GET    /api/v1/records
GET    /api/v1/records/{id}
POST   /api/v1/records
DELETE /api/v1/records/{id}
```
Названия ресурсов в URL:
* lowercase;
* kebab-case;
* обычно во множественном числе.
Не добавлять в URL `admin`, `anonymous`, `public`, `internal` или другие технические префиксы автоматически.
Такие маршруты используются только если они являются частью явно определённого API-контракта конкретной feature.
### 3.3. Route constraints
Если тип параметра известен, использовать соответствующий route constraint.
Пример:
```csharp
[HttpGet("{id:guid}")]
```
вместо:
```csharp
[HttpGet("{id}")]
```
---
## 4. API versioning
Версия API задаётся через общие константы API:
```csharp
[ApiVersion(ApiVersions.V1)]
```
Не использовать строковые литералы:
```csharp
[ApiVersion("1.0")]
```
если версия уже представлена общей константой API.
Endpoint должен быть добавлен в соответствующую endpoint group:
```csharp
[EndpointGroupName(ApiEndpointGroups.V1)]
```
При добавлении новой версии необходимо использовать соответствующие `ApiVersions` и `ApiEndpointGroups`, а не создавать локальные значения внутри controller.
---
## 5. CQRS
### Query
Операции чтения выполняются через:
```csharp
IQueryBus
```
Пример:
```csharp
return (
    await queries.SendAsync(
        new GetRecordQuery(id),
        ct))
    .ToActionResult(this);
```
### Command
Операции изменения состояния выполняются через:
```csharp
ICommandBus
```
Пример:
```csharp
return (
    await commands.SendAsync(
        new CreateRecordCommand(request),
        ct))
    .ToActionResult(this);
```
Контроллер не вызывает обработчики напрямую. Их размещение определяется [S20](../../../catalog/docs/standards/architecture/cqrs.md#handler-responsibilities-and-dependency-direction).
Контроллер не должен реализовывать дополнительную application orchestration поверх command/query без отдельного архитектурного требования.
---
## 6. Model binding
Источник каждого transport-параметра должен быть понятен из сигнатуры endpoint.
Используются:
```csharp
[FromRoute]
[FromQuery]
[FromBody]
```
### Route
```csharp
[FromRoute] Guid id
```
### Query string

Query string используется для поиска, фильтрации, пагинации и других параметров операции, которые действительно являются частью URI.

Optimistic concurrency **не** передаётся через query string: для неё используется `If-Match` по разделу 12.

Для сложных наборов фильтров используется request object:
```csharp
public async Task<
    ActionResult<ApiResponse<OffsetPagedResultDto<RecordDto>>>> List(
        [FromQuery] ListRecordsRequest request,
        CancellationToken ct)
```
Не следует разносить большой набор search/filter/pagination параметров по аргументам action, если для них существует или должен существовать единый request contract.
### Body
```csharp
[FromBody] CreateRecordDto request
```
---
## 7. CancellationToken
Каждый asynchronous endpoint принимает:
```csharp
CancellationToken ct
```
`CancellationToken` располагается последним параметром метода.
Полученный token передаётся в command/query:
```csharp
await commands.SendAsync(command, ct);
```
или:
```csharp
await queries.SendAsync(query, ct);
```
Контроллер не создаёт дополнительные `CancellationTokenSource` без отдельной причины и не заменяет request cancellation token собственным token.
Политика отмены после начала критической application operation определяется Application layer, а не MVC controller.
---
## 8. Формат успешного ответа
API использует общий response envelope:
```csharp
ApiResponse<T>
```
Пример:
```csharp
ActionResult<ApiResponse<RecordDto>>
```
или:
```csharp
ActionResult<ApiResponse<Guid>>
```
Для offset pagination:
```csharp
ApiResponse<OffsetPagedResultDto<T>>
```
Например:
```csharp
ActionResult<ApiResponse<OffsetPagedResultDto<RecordDto>>>
```
Тип `T` должен соответствовать фактическому result соответствующей command/query.
Контроллер не создаёт `ApiResponse<T>` вручную, если result должен преобразовываться через общий механизм:
```csharp
.ToActionResult(this)
```
---
## 9. HTTP success statuses
HTTP status не выбирается автоматически только исходя из HTTP method.
Статус является частью API-контракта.
Для API с JSON-envelope и результатом операции в теле допустим следующий согласованный профиль успешных ответов:
| Операция                 | Типичный статус |
| ------------------------ | --------------- |
| Получение одного ресурса | `200 OK`        |
| Получение списка         | `200 OK`        |
| Создание                 | `200 OK`        |
| Изменение                | `200 OK`        |
| Удаление с response body | `200 OK`        |
Например:
```csharp
[ProducesResponseType(
    typeof(ApiResponse<Guid>),
    StatusCodes.Status200OK)]
```
Не следует автоматически заменять:
* `200` на `201 Created` для `POST`;
* `200` на `204 No Content` для `DELETE`;
только потому, что такой вариант распространён в REST API.
Если feature требует другой статус, это должно быть явно зафиксировано в её HTTP-контракте и отражено в реализации.
Response type и HTTP status должны соответствовать друг другу.
Например, endpoint, возвращающий:
```csharp
ApiResponse<Guid>
```
не должен одновременно документироваться как `204 No Content`.
---
## 10. Ошибки
HTTP errors описываются стандартными ASP.NET Core problem contracts.
### Validation error
```csharp
ValidationProblemDetails
```
Типичный статус:
```text
400 Bad Request
```
Пример:
```csharp
[ProducesResponseType(
    typeof(ValidationProblemDetails),
    StatusCodes.Status400BadRequest)]
```
### Application/domain error
Используется:
```csharp
ProblemDetails
```
с соответствующим HTTP status.
Типичные случаи:
| Status                      | Назначение                                    |
| --------------------------- | --------------------------------------------- |
| `400 Bad Request`           | некорректный request                          |
| `404 Not Found`             | требуемый ресурс не существует                |
| `409 Conflict`              | предметный/domain conflict состояния; HTTP optimistic concurrency через `If-Match` использует `412` |
| `500 Internal Server Error` | необработанная внутренняя ошибка              |
Например:
```csharp
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]


[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]


[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status500InternalServerError)]
```
Не добавлять статус только «на всякий случай».
Каждый документированный status должен быть реально возможен для данного use case.
Контроллер не преобразует application errors вручную.
Используется общий механизм:
```csharp
.ToActionResult(this)
```
---
## 11. Validation
Валидация transport/application-моделей выполняется через FluentValidation по [S18](../../../catalog/docs/standards/dotnet/fluentvalidation.md). Валидаторы собираются из переиспользуемых правил полей и вложенных моделей. Они подключаются к единому механизму валидации; контроллер не получает собственную копию этих правил.
Controller не должен содержать код вида:
```csharp
if (request.Name is null)
{
    return BadRequest(...);
}
```
если это правило относится к request/application validation.
Validation errors должны преобразовываться в:
```csharp
ValidationProblemDetails
```
со статусом:
```text
400 Bad Request
```
Feature-документация должна описывать значимые для API ограничения входных данных, но не должна дублировать внутреннюю реализацию validator.
---
## 12. Optimistic concurrency: ETag / If-Match

Для HTTP optimistic concurrency используются conditional requests, а не техническая версия в DTO/query.

Mutable owner внутри Domain/Persistence имеет внутренний `int Version`. API преобразует его в **strong ETag**:

```http
ETag: "7"
```

Технический `Version`:

- не сериализуется в response DTO;
- не попадает в OpenAPI model;
- не является business `VersionNumber`;
- не является integration `SourceVersion`;
- не является activation `Sequence`.

Exact `GET` mutable resource возвращает representation + `ETag`.

Если successful mutation возвращает актуальное representation того же mutable resource, response также возвращает новый `ETag`.

List-response не обязан предоставлять concurrency ETag каждой строке. Перед mutation клиент получает exact resource/ETag через соответствующий flow.

State-changing request существующего mutable resource передаёт:

```http
If-Match: "7"
```

Controller/API adapter:

1. читает `If-Match`;
2. валидирует допустимый strong-tag format;
3. преобразует tag во внутренний expected `int Version`;
4. передаёт expected Version в Application command;
5. сам не сравнивает business state и не читает repository ради concurrency check.

Application/Persistence сравнивают expected/current Version до commit. Successful state-changing commit атомарно увеличивает Version.

Если обязательный `If-Match` отсутствует:

```text
428 Precondition Required
```

Если ETag устарел:

```text
412 Precondition Failed
```

`409 Conflict` используется для предметного/domain conflict, который остаётся конфликтом даже при совпавшем ETag: stale published baseline, недопустимый lifecycle transition, dependency blocker и т.п.

Некорректный синтаксис заголовка относится к invalid request/binding и не маскируется под `412`.

После `412` клиент перечитывает resource и показывает конфликт; он не подставляет свежий ETag и не повторяет mutation вслепую.

Для create нового независимого resource `If-Match` обычно не нужен. Если операция создаёт resource **из mutable source snapshot**, feature обязана явно указать, какой source ETag является precondition либо выбрать отдельный route/flow, где эта семантика однозначна.

Не использовать `If-Unmodified-Since`, query/body `expectedVersion` или DTO `Version` как параллельную схему concurrency.

Основание протокола: RFC 9110 определяет `If-Match` как precondition для предотвращения lost update и `412 Precondition Failed` при ложном условии; RFC 6585 определяет `428 Precondition Required` для случаев, когда сервер требует conditional request.

---
## 13. OpenAPI metadata
Каждый endpoint должен иметь:
```csharp
[EndpointSummary(...)]
[EndpointDescription(...)]
```
### EndpointSummary
Кратко описывает операцию.
Пример:
```csharp
[EndpointSummary("Get record")]
```
```csharp
[EndpointSummary("Create record")]
```
Summary должен быть коротким и не повторять полную семантику endpoint.
### EndpointDescription
Описывает значимую внешнюю семантику операции.
Пример:
```csharp
[EndpointDescription(
    "Delete a record when its documented state permits deletion; " +
    "the current ETag must be supplied in If-Match")]
```
В description следует указывать важные для API consumer ограничения, например:
* особенности создаваемого ресурса;
* требования к состоянию ресурса;
* concurrency requirement;
* resource state restrictions;
* значимую семантику фильтрации.
Не описывать в `EndpointDescription` внутреннюю реализацию Application/Domain layer.
Например, не нужно писать:
```text
Loads aggregate through repository and invokes domain method.
```
Это не является частью HTTP API.
---
## 14. ProducesResponseType
Каждый endpoint должен явно описывать свои основные responses через:
```csharp
[ProducesResponseType(...)]
```
Пример GET:
```csharp
[ProducesResponseType(
    typeof(ApiResponse<RecordDto>),
    StatusCodes.Status200OK)]


[ProducesResponseType(
    typeof(ValidationProblemDetails),
    StatusCodes.Status400BadRequest)]


[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]


[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status500InternalServerError)]
```
Пример command:
```csharp
[ProducesResponseType(
    typeof(ApiResponse<Guid>),
    StatusCodes.Status200OK)]


[ProducesResponseType(
    typeof(ValidationProblemDetails),
    StatusCodes.Status400BadRequest)]


[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]


[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]


[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status500InternalServerError)]
```
Набор статусов определяется конкретным use case.
Не копировать механически полный набор статусов из другого endpoint.
---
## 15. Authorization
Authorization является частью API-контракта конкретной feature.
Если endpoint требует permission, используется определённый для приложения authorization mechanism, например:
```csharp
[AuthorizePermission(...)]
```
Если endpoint должен быть доступен анонимно, это должно быть явно определено feature.
AI-агент не должен:
* автоматически считать все endpoint'ы административными;
* автоматически добавлять `/admin`;
* автоматически добавлять `[AllowAnonymous]`;
* автоматически добавлять permission;
* делать вывод о доступе только из названия controller.
Feature-документация должна явно фиксировать требования к доступу.
---
## 16. Типовая форма endpoint
### Query by id
```csharp
[HttpGet("{id:guid}")]
[EndpointSummary("Get record")]
[EndpointDescription(
    "Get a record by its identifier")]
[ProducesResponseType(
    typeof(ApiResponse<RecordDto>),
    StatusCodes.Status200OK)]
[ProducesResponseType(
    typeof(ValidationProblemDetails),
    StatusCodes.Status400BadRequest)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<ApiResponse<RecordDto>>> Get(
    [FromRoute] Guid id,
    CancellationToken ct)
{
    return (
        await queries.SendAsync(
            new GetRecordQuery(id),
            ct))
        .ToActionResult(this);
}
```
### List query
```csharp
[HttpGet]
[EndpointSummary("Get records")]
[EndpointDescription(
    "Get records with pagination, search and filters")]
[ProducesResponseType(
    typeof(ApiResponse<OffsetPagedResultDto<RecordDto>>),
    StatusCodes.Status200OK)]
[ProducesResponseType(
    typeof(ValidationProblemDetails),
    StatusCodes.Status400BadRequest)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status500InternalServerError)]
public async Task<
    ActionResult<ApiResponse<OffsetPagedResultDto<RecordDto>>>> List(
        [FromQuery] ListRecordsRequest request,
        CancellationToken ct)
{
    return (
        await queries.SendAsync(
            new ListRecordsQuery(request),
            ct))
        .ToActionResult(this);
}
```
### Command
```csharp
[HttpPost]
[EndpointSummary("Create record")]
[EndpointDescription("Create an record")]
[ProducesResponseType(
    typeof(ApiResponse<Guid>),
    StatusCodes.Status200OK)]
[ProducesResponseType(
    typeof(ValidationProblemDetails),
    StatusCodes.Status400BadRequest)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<ApiResponse<Guid>>> Create(
    [FromBody] CreateRecordDto request,
    CancellationToken ct)
{
    return (
        await commands.SendAsync(
            new CreateRecordCommand(request),
            ct))
        .ToActionResult(this);
}
```
---
## Правила для feature-документации
## 17. Что должна определять feature
HTTP-раздел feature должен описывать **внешний контракт**, а не повторять реализацию controller или handler. Внутренний алгоритм, транзакции и persistence описываются отдельно в той же feature.
Для каждого HTTP flow должны быть определены:
1. HTTP method;
2. route;
3. access requirements;
4. route parameters;
5. query parameters;
6. request body;
7. command или query;
8. response contract;
9. success status;
10. возможные business/application errors;
11. значимые concurrency requirements;
12. особенности поведения, которые видит API consumer.
Если какое-либо поведение отличается от данного стандарта, feature должна указать это явно.
---
## 18. Что feature не должна дублировать
API-раздел может ссылаться на:
* command;
* query;
* request DTO;
* response DTO;
* validator;
* domain concept.
Но он не должен повторно описывать их внутреннюю реализацию.
Не нужно помещать в API documentation:
* алгоритм handler;
* repository calls;
* EF Core queries;
* transaction boundaries;
* domain method implementation;
* внутренние domain events;
* mapping implementation;
* persistence details.
Например:
```text
POST /records
→ CreateRecordCommand
```
достаточно для связи transport layer с Application layer.
Внутреннее устройство `CreateRecordCommandHandler` описывается в разделе внутренней реализации той же серверной фичи. Ограничение данного раздела касается HTTP-описания, а не запрещает полную техническую спецификацию фичи.
---
## Правила для AI-агента
## 19. Источник истины
При реализации или изменении API агент должен сначала проверить существующие соглашения приложения:
* соседние controllers;
* `ApiRoutePrefixes`;
* `ApiVersions`;
* `ApiEndpointGroups`;
* `ToActionResult`;
* `ApiResponse<T>`;
* authorization attributes;
* request/response DTO;
* command/query;
* существующие validation и error conventions.
Нельзя создавать альтернативную инфраструктуру, если необходимая инфраструктура уже существует.
---
## 20. Не придумывать контракт
AI-агент не должен самостоятельно придумывать:
* новый HTTP status;
* новый route prefix;
* `/admin` или `/anonymous`;
* `201 Created`;
* `204 No Content`;
* `ETag`;
* `If-Match`;
* новый response envelope;
* новый error response;
* permission;
* anonymous access;
* новую схему pagination;
* новый transport mechanism;
если это не следует из feature, задачи или существующих соглашений приложения.
Если существует несколько технически допустимых REST-вариантов, агент обязан придерживаться **явно определённых соглашений API**, а не выбирать вариант на основании абстрактных REST-рекомендаций.
---
## 21. Минимальный controller
Controller должен оставаться максимально тонким.
Предпочтительная форма action:
```csharp
public async Task<ActionResult<ApiResponse<T>>> Action(
    ...,
    CancellationToken ct)
{
    return (
        await commands.SendAsync(
            new SomeCommand(...),
            ct))
        .ToActionResult(this);
}
```
или:
```csharp
public async Task<ActionResult<ApiResponse<T>>> Action(
    ...,
    CancellationToken ct)
{
    return (
        await queries.SendAsync(
            new SomeQuery(...),
            ct))
        .ToActionResult(this);
}
```
Если controller начинает содержать:
* branching по бизнес-состоянию;
* несколько repository calls;
* transaction handling;
* domain validation;
* сложный mapping;
* orchestration нескольких use cases;
агент должен рассматривать это как признак того, что логика находится не в том слое.
---
## 22. При изменении существующего API
При изменении endpoint агент обязан проверить согласованность:
```text
Feature documentation
        ↕
Controller route
        ↕
Request contract
        ↕
Command / Query
        ↕
Response contract
        ↕
ProducesResponseType
        ↕
OpenAPI description
```
Изменение одного элемента контракта не должно оставлять остальные элементы в противоречивом состоянии.
Например, mutation существующего mutable resource требует проверить:
* feature documentation;
* наличие `ETag` на exact read;
* обязательность `If-Match`;
* передачу parsed expected Version в Application;
* `428 Precondition Required`;
* `412 Precondition Failed`;
* отдельные domain `409 Conflict`;
* OpenAPI description.
---
## 23. Главное правило
HTTP layer отвечает только за HTTP concerns:
```text
routing
versioning
model binding
authorization
OpenAPI metadata
transport contracts
HTTP result mapping
```
Application layer отвечает за use case.
Domain layer отвечает за бизнес-правила.
Infrastructure layer отвечает за техническую реализацию.
Не переносить ответственность между слоями ради упрощения конкретного controller.
## 24. Протокольные уточнения
Эти уточнения дополняют профиль реализации. Они не означают автоматическую миграцию ранее опубликованного API.
### Маршрут и валидация
Route constraint выбирает маршрут, но не заменяет model binding и validator. При `{id:guid}` невалидный GUID может не сопоставиться с action и дать `404`, а не `400`. Если контракт обещает ошибку ввода `400`, маршрут и обработка должны обеспечивать именно её. Это различие нужно проверить через настоящий HTTP pipeline.
### HTTP-метод и завершение операции
`GET` используется для безопасного чтения, не для запрошенного клиентом изменения бизнес-состояния. Идемпотентность означает одинаковый целевой эффект повторов, а не обязательное совпадение всех ответов. Для `POST` гарантия повторного исполнения задаётся отдельно. Принятие фоновой работы не означает завершённый результат. Если выбран `202 Accepted`, способ наблюдать окончание работы задаётся внешним контрактом.
`200` с JSON-result допустим для согласованных операций и не заменяется автоматически на `201` или `204`. При `204 No Content` тело и JSON-envelope отсутствуют. Потоковая выдача файла описывает бинарное представление и заголовки отдельно, а не помещает байты в `ApiResponse<T>` по инерции.
### Optimistic concurrency и HTTP precondition

Для систем, применяющих этот профиль, механизм optimistic concurrency — strong `ETag` + `If-Match`.

Технический `int Version` остаётся внутренним state persistence/domain. HTTP consumer не получает его в DTO и не передаёт в query/body.

Отсутствующий обязательный `If-Match` даёт `428 Precondition Required`, stale validator — `412 Precondition Failed`. Предметные business conflicts продолжают использовать согласованный `409 Conflict` и не смешиваются с HTTP precondition.
### Доступ и ошибки pipeline
Неавторизованный вызов, отказ права, отсутствие маршрута и ошибка model binding могут возникнуть до application handler. Общая error-конфигурация и OpenAPI должны учитывать эти ответы. `ProducesResponseType` описывает контракт, но не реализует его. Проверка доступа не отменяется совпадением checksum, cache-validator или наличием ID.
### Источники протокольных уточнений
[HTTP Semantics, RFC 9110](https://www.rfc-editor.org/rfc/rfc9110.html), разделы 9.2, 13.1.1, 15.3 и 15.5.13; [маршрутизация ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing); [ASP.NET Core Web API](https://learn.microsoft.com/en-us/aspnet/core/web-api/).
## 25. Проверка соблюдения
Проверяются совпадение метода и маршрута с OpenAPI, источники route/query/body/header, преобразование result, точный response type и status, отсутствующие и неверные входы, доступ, optimistic concurrency, cancellation и отсутствие прикладной логики в controller. Модель ошибки проверяется не только для handler, но и на уровне HTTP pipeline. Тесты не фиксируют невыбранный контракт как действующий.
[Потоки данных](S17-data-flows.md) · [Валидация и ошибки](../../../catalog/docs/standards/aspnet/errors-and-validation.md#response-semantics) · [Functional tests](../../../catalog/docs/standards/dotnet/testing.md#functional-tests).
