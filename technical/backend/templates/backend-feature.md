# FXX. [Название фичи]: бэкенд

[Бизнесовая спецификация] · [Модуль] · [Фронтенд] · [Связанные Q/ADR]

> Feature-документ описывает **текущее целевое состояние реализации**. Он не является журналом обсуждений. Не добавлять разделы `Решение блока N`, `Уточнение блока N`, хронологию переговоров и старые отменённые варианты. История выбора остаётся в Q/ADR. При новом решении переписать соответствующий нормативный раздел feature.

**Единый формат описания моделей.** Для entities/VO, DTO, command, query, parameter/read/result models, domain/integration event payloads и других моделей каждый определённый field/property занимает отдельную строку таблицы `Поле | Тип | Обязательное | Описание | Constraints`. Имя модели указать перед таблицей. Не писать тип через `Поле:Тип`, не перечислять несколько полей через `;`, `/` или `<br>` в одной ячейке. Обзорные таблицы сохраняют роль/владельца/lifecycle, result/concurrency и trigger/consumer; они не заменяют таблицы полей. Операционные таблицы методов, endpoints и data flows сохраняют свой формат.

- **Тип:** точный объявленный CLR/contract тип, nullable `?`, элемент коллекции и вложенная модель. Business enum/type alternatives объяснить в Constraints, не маскировать невыбранный CLR type. Если тип неизвестен, написать `Уточняется` со ссылкой на Q; форматирование не выбирает контракт.
- **Обязательное:** `Да`, `Нет`, `Условно` или `Уточняется`. Различать обязательность поля запроса, nullable состояние и обязательное присутствие server-generated/internal/read-only свойства. Для условного значения указать условие (например, только Published); пустая коллекция не равна отсутствующему полю. Nullable и JSON requiredness не считать автоматически одинаковыми.
- **Описание:** назначение, источник значения, server-generated/read-only/computed/internal характер и согласованный default, если есть.
- **Constraints:** известные пределы/формат, уникальность, принадлежность, lifecycle/immutability, межполевые правила и ссылка на единый Domain constraints. Не придумывать ограничения; незакрытое оставить `Уточняется` со ссылкой на Q. Не переносить constraints из текущего кода в целевые требования без основания.

Модель определяется один раз у владельца; при переиспользовании дать ссылку. Если принят только логический состав, а имена/типы wire properties ещё неизвестны, так и обозначить логические элементы и Q, не выдавать их за готовый DTO. Если модель или событие не вводится, не создавать таблицу фиктивных полей ради шаблона.

## 1. Область реализации

[Конечный результат фичи, входящие use cases, потребители, что явно не входит.]

## 2. Domain

### 2.1. Aggregates, entities и value objects

| Модель | Роль | Состояние / lifecycle | Владелец / граница |
| --- | --- | --- | --- |
| `Entity` | Aggregate Root / Entity / Value Object | [identity, mutable/immutable state, lifecycle] | [aggregate/BC] |

**Поля `Entity`:**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `EntityId` | Да | Identity; [кто создаёт значение] | [stable ID, правила уникальности / принадлежности] |
| `Version` | `int` | Да | Внутренний concurrency token mutable owner | Не входит в DTO; [правила изменения по persistence convention] |

Для каждой модели указать identity, mutable/immutable state, lifecycle и ссылки на stable IDs. Mutable concurrency owner имеет внутренний `int Version`, который не публикуется в DTO.

### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| `Aggregate` | `Method(...)` | [точные параметры] | [инварианты] | [state/event/result] |

Domain methods выражают предметные действия, а не CRUD-setters. Published immutable snapshot не получает `Update`.

### 2.3. Инварианты и constraints

[Ссылка на Q/ADR и S21. Указать единственный источник ограничений, cross-version rules, uniqueness, lifecycle guards.]

### 2.4. Domain events

| Event | Когда создаётся | Модель payload | Потребитель |
| --- | --- | --- | --- |
| [если нужен] | [commit-worthy fact] | [имя модели; поля отдельно ниже] | [внутренний/внешний] |

**Поля `[EventPayload]` (только если событие нужно):**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `[OwnerId]` | `[точный тип ID]` | Да | Identity объекта, о котором сообщается факт | [принадлежность, stable identity] |
| `[OccurredAt]` | `DateTimeOffset` | Да | Серверное время произошедшего факта | [временная семантика и источник] |

Для integration events использовать тот же формат полей в разделе owning adapter; envelope и payload различать. Если событие не вводится, написать это явно, не заполнять пример фиктивной моделью.

Не создавать domain event только ради логирования.

## 3. Application.Abstractions

### 3.1. Commands

| Command | Назначение | Result | Concurrency |
| --- | --- | --- | --- |
| `...Command` | [предметное действие] | [тип результата] | [expected Version внутри command, если mutation existing resource] |

**Поля `...Command`:**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Request` | `[точная application/input модель]` | Да | Вход конкретного действия | [ссылка на единственное определение полей Request, правила действия] |

Для mutation существующего owner отдельно описать поле expected Version с типом `int`, обязательностью и проверкой конкуренции. Не добавлять его в команду создания нового ресурса без основания.

HTTP `If-Match` преобразуется Api-слоем во внутренний expected `int Version`. Технический Version не входит в DTO.

### 3.2. Queries

| Query | Назначение | Result |
| --- | --- | --- |
| `...Query` | [чтение ресурса / фильтрация / paging] | [точный тип DTO / paged result] |

**Поля `...Query`:**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `[FieldName]` | `[точный тип конкретного route/filter/page/sort поля]` | [Да / Нет / Условно / Уточняется] | [роль поля, default и источник] | [допустимые значения / комбинации / ссылка на Q] |

Каждый реальный route parameter, filter, page и sort описать отдельной строкой.

### 3.3. DTO и parameter models

Для каждого DTO, parameter object, application/read/result model привести точный состав в отдельной таблице полей. Не использовать один универсальный DTO как create/update/read/publish contract.

**Поля `[Имя конкретной модели]`:**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `[FieldName]` | `[точный CLR / contract тип]` | [Да / Нет / Условно / Уточняется] | [предметный смысл, источник, default] | [формат, пределы, uniqueness, lifecycle / ссылки на constraints и Q] |

Для вложенной модели поставить ссылку на её единственное определение; её собственные поля описать тем же форматом. Не упаковывать вложенные поля в ячейку родителя.

Create-модель не содержит server-generated ID. Update существующего child содержит обязательный ID.

Технический concurrency `Version` не входит в DTO. ETag — HTTP metadata.

### 3.4. Ports

| Port | Назначение | Направление |
| --- | --- | --- |
| `I...` | [внешняя зависимость/другой BC/storage/hash/clock] | inbound/outbound |

### 3.5. Specifications / read contracts

[Конкретные Ardalis specifications, query projections, sorting/pagination. Не придумывать repository на каждый child.]

## 4. Application

### 4.1. Handlers / use cases

Для каждого command/query:
1. что загружается;
2. какие external facts проверяются;
3. какие Domain methods вызываются;
4. transaction boundary;
5. side effects after commit;
6. result mapping.

### 4.2. Validators

[FluentValidation composition, reuse Domain constraints, async external validation.]

### 4.3. Mappers

[Только реальные преобразования DTO ↔ command/domain/read model. Не создавать дублирующие модели без семантической причины.]

### 4.4. Services / algorithms

[Hashing, composition, schema validation, diff, authorization-independent orchestration и т.п.]

## 5. Api

### 5.1. Endpoints

| Method | Route | Request | Response | Access |
| --- | --- | --- | --- | --- |
| `GET/POST/...` | `/api/v1/...` | [body/query/header] | `ApiResponse<T>` | [permission/Demo] |

### 5.2. ETag / If-Match

Для exact mutable resource:
- GET/successful mutation response возвращает strong `ETag`;
- технический `int Version` не входит в DTO;
- mutation существующего resource требует `If-Match`;
- Api преобразует ETag во внутренний expected Version;
- missing required `If-Match` → `428`;
- stale `If-Match` → `412`;
- domain conflict → `409`.

List-response не является источником per-row ETag.

### 5.3. OpenAPI и ошибки

[ProducesResponseType, ProblemDetails/ValidationProblemDetails, machine codes, binary responses.]

## 6. EntityFramework

### 6.1. DbContext ownership

[DbSet/owned JSON/value converters в schema данного BC. Чужой DbContext не используется.]

### 6.2. Configurations

| Entity | Table | Key/index | Concurrency | Mapping |
| --- | --- | --- | --- | --- |
| [тип] | [schema.table] | [PK/unique/index] | `Version` concurrency token | [jsonb/columns] |

### 6.3. Constraints / indexes

[Unique keys, filtered indexes, checks, FK только внутри BC.]

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

[Конкретные implementations ports/repositories/specifications.]

### 7.2. Transactions

[Что фиксируется атомарно внутри BC. Cross-BC transaction запрещена.]

### 7.3. Migrations

[Нужные destructive/forward changes. Для незапущенного CM не сохранять legacy schema ради несуществующих production data.]

## 8. External adapters

Добавлять только реально используемые подразделы.

### 8.1. S3 / MinIO

[Object key, metadata, upload/download/delete, integrity, ownership.]

### 8.2. RabbitMQ / MassTransit

[Contracts, consumer, outbox/inbox, topology по S22.]

### 8.3. HTTP integrations

[VR School/другой сервис, request/response, timeout/failure semantics.]

### 8.4. Другие adapters

[Code generation, clock, hashing, filesystem и т.п.]

## 9. Data flows

Для сложных потоков дать короткую последовательность `Api → Application → Domain → Persistence/Adapter → response/event`.

Не повторять здесь уже описанные поля моделей.

## 10. Ошибки и диагностика

[Domain/application errors, machine codes, логирование без секретов/full payload.]

## 11. Тестирование

| ID | Уровень | Сценарий | Ожидаемый результат |
| --- | --- | --- | --- |
| ... | Unit / Integration / Functional / Architecture | ... | ... |

Обязательно проверить optimistic concurrency через HTTP ETag/If-Match, persistence constraints и историческую неизменяемость там, где применимо.

## 12. Порядок реализации

[Конкретная последовательность изменений по слоям.]

## 13. Открытые технические вопросы

Только реально незакрытые технические TBD со ссылкой на Q/ADR. Историю уже принятых решений сюда не копировать.
