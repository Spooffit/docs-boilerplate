# Q15. Контракты и исходники
## Вопрос и известные границы
Где актуальные Application/API/Infrastructure, клиент и договорённые контракты VR School? Какие DTO, маршруты и форматы ошибок уже есть?
**Ответ:** Current CM API/Application/EF частично инвентаризированы по snapshot 01.10.2026; VR School wire-контракты, full host composition, source ApiClient и внешний Delivery API остаются неподтверждёнными.
## Что уже дают предоставленные примеры
`rest.md` описывает HTTP-профиль: MVC controllers, CQRS buses, общий result mapping, ApiResponse<T>, ProblemDetails/ValidationProblemDetails и явно определяемую конкурентную ревизию. На его основе создан [S16](../standards/S16-rest.md). Это задаёт форму реализации, но не является инвентаризацией существующих endpoint Content Manager.
`F02-news.md` является примером глубины технической спецификации другой фичи. Его 204/FormErrorResponse, ETag, admin-маршруты, языки и бизнес-ограничения не считаются действующим договором Content Manager. Полнота описаний backend-фич не достигается подстановкой этих значений.
## Что требуется подтвердить для конкретных операций
Нужны реальные маршруты и версии API, DTO и их null/empty-семантика, result/envelope/status каждой операции, общий error mapping, существующие регистрации controllers/handlers/validators и ports. Отдельно требуется проверить используемый механизм конкуренции, повтор запросов после потерянного ответа и необходимость внешнего endpoint у внутренних действий.
Наличие имени метода в старом Domain не подтверждает текущий handler, controller, миграцию или consumer. До проверки соответствующие ячейки транспортных таблиц остаются `Уточняется`.

## MVP: concurrency и recovery boundary

Выбран [общий PostgreSQL transaction-level guard Q20](Q20-persistence-and-migration.md#mvp-протокол-consistency-guard): owner Read Committed transaction → acquire до facts/row locks → fresh own roots и independent primary public reads → own commit/rollback. Общий gate не отменяет strong numeric own ETag/If-Match, missing428/own stale412/independent basis409. Current foreign DbContext enlistment в caller UoW требуется исключить; public port сам по себе не доказательство isolated read.

Acquisition timeout/cancel и dependency read unavailable — технические результаты, не business CatalogConflict или пустой compatible preview. Exact status/code/retry metadata уточняются здесь; numeric limits Q22. Old user precondition сохраняется при safe technical retry. Lost commit ACK остаётся unknown outcome: inquiry/operation identity/readback владельца, no blind retry с новым tag. Guard не гарантирует session revoke/in-flight cutoff/effective history Q16/Q17.

Exact DTO/repair basis/header/source evidence signatures остаются инженерными контрактами фич. Выбор механизма, own-only transaction и порядок checks через commit закрыты; state/defaults/permission/late-Approval precedence не выводятся из lock.

## Publishing: command execution и ClientOperationId

F09 Product/Variant CreateRelease, F10 CreateRevision, F11 StartPromotion и F17 Submit требуют request-body `ClientOperationId:Guid`, client-generated/non-empty. Server PromotionOperationId/PromotionItemId/RevisionId/ModerationId не подменяются этим ID. Exact JSON casing/response/status/OpenAPI carrier материализуются здесь; смысл и body ownership уже выбраны.

| Действие | ClientOperationId и исходное основание | Confirmed replay |
| --- | --- | --- |
| F09 CreateRelease | IDs/comment, original source If-Match, validation context, exact captured predecessor/review basis | Original created artifact receipt; не new Create/Restore, source может уже измениться |
| F10 CreateRevision | Exact sources/comment/composition request basis; source/workspace If-Match не вводится | Original RevisionId/execution result; не new moderation/activation и не hash reuse |
| F11 StartPromotion | Product/base/mode/subset input и собственные agreed preconditions | Original OperationId/start receipt; не новый membership capture/intent reservation |
| F17 Submit | Exact RevisionId/выбранный Submit input; own initial submit convention сохраняется | Original ModerationId/iteration receipt; не новая iteration после Withdraw и не повтор AlreadyApproved workflow |

Current authentication/authorization → owning guard → lookup committed [CommandExecution Q20](Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение) → compare original normalized meaningful input/trusted actor → original receipt **до** новых source If-Match/lifecycle checks. Same bound ID/different input либо actor конфликтует; доступ к чужому result не раскрывается обходом scope gate. Не сохранять raw credentials/request secrets ради fingerprint. Original receipt не объявляется текущим lifecycle/новым resource ETag; current exact view — самостоятельный read.

Если confirmed execution отсутствует, выполняются обычные domain/current checks и owning commit атомарно сохраняет result/identity. Failure до commit не successful record. Lost response/ACK сохраняет тот же ID и исходные If-Match/payload/context/comment. Absence в отдельном inquiry/read ещё не доказывает, что original in-flight request не завершится; safe повтор использует **тот же** ID/input, а не новый ID/подменённый tag. После подтверждённого исхода новый пользовательский intent получает новый ID.

Exact inquiry exposure/response metadata, normalized codec/literal equality, namespace enum wire, unique-constraint classification и retention/result adapter остаются инженерной материализацией. Это не Idempotency-Key header по догадке и не exactly-once внешних side effects. Replay не генерирует новый F18 business action/outbox/activation.

Normalized comparison строится из исходного parsed caller input и trusted actor, не из заново вычисленного latest local release/current membership/server capture. Frozen server-selected inputs сохраняются в operation/result; они не перечитываются до original receipt lookup. Current authorization здесь означает staff/action/resource scope, а не new-use lifecycle eligibility: эти проверки относятся только к новому execution после lookup.

Для public/manual F17 Submit ClientOperationId обязателен. Internal automatic Submit внутри Publishing preparation не создаёт искусственный client request: idempotency задают exact PromotionItemId/current stage/atomically bound tuple. Нельзя применять один batch Start ClientOperationId как ключ Submit всех items либо генерировать новый random client ID на каждом worker retry. Submitted/initiator metadata берутся из trusted accepted Start context; body actor/fake staff credential не вводятся.

### F09 review и F10 closure

Empty Product release и fully empty Variant patch допустимы; F10 ≥1 explicit source/nonempty final result отдельны. Intentional identical-content/comment Create получает новый artifact identity. Captured predecessor semantic review — latest-created same-owner ReleaseNumber включая Retired, nullable first baseline. Позднее созданный release не меняет уже confirmed predecessor; source/context/necessary dependency drift требует new review. Request/review/result носит exact previous ID без fake baseline/current-active fallback.

F10 current new-use/runtime-target checks относятся к saved final effective instance/schema/media closure; source Available/owner/intrinsic trusted exclusion evidence отдельно. Suppressed A не blocker B, shared B dependency остаётся в checks. Historical retention/evidence шире и не удаляется. Exact closure read models/profile/types в owning F/Q; новые media lifecycle values не выбираются.

### F11 items, Cancel и продолжение

Operation captures immutable ProductReleaseId/ProductPublicationState.Version/applied membership/local source IDs; item — own mutable Version. Summary/list/operation immutable inputs не дают own token каждого item. CancelAutomaticItem — отдельная logical operation с exact ItemId и own item If-Match/internal expected version, не VariantId-only отмена и не fake publication transition. Exact URI/method/result/header carrier/access codes уточняются здесь; body не задаёт actor/publication Sequence/foreign Version.

Captured Product state Version и Variant publication Sequence — internal continuation facts, не client authority. Late Approval/automatic worker проверяет exact tuple/current head и fresh owning facts под gate. Prepare Revision+automatic Submit+binding+AwaitingModeration — one Publishing commit; human decision/activation отдельны. Frontend не оркестрирует Create→Submit HTTP loopback. Duplicate outcome не повторяет activation/history после manual action.

### F17 comparison consent

Approve передаёт shown Delivery-effective comparison basis **отдельно** от own moderation If-Match; basis предоставлен сервером, не вычислен браузером. Команда свежим public reader проверяет actual effective basis под gate, включая confirmed no-effective vs unavailable. Drift → independent basis conflict409 + refresh/new consent, own stale →412, required missing header→428. Effective-switch writers участвуют в том же gate до commit Approval.

Exact basis CLR/wire/token representation, missing-basis validation и combined-error priority уточняются здесь. После committed Approval дальнейшая baseline drift сама по себе decision не отзывает; F11 activation использует собственный intent/captured generations/current restrictions. Withdraw iteration cancels exact bound item; новый manual Submit не inherits старый automatic link. Cancellation/supersede item не автоматически Withdraw human moderation.
## Затронутые фичи
[F01](../../../business/features/F01-create-entity-schema.md) · [F02](../../../business/features/F02-create-schema-version.md) · [F03](../../../business/features/F03-publish-schema-version.md) · [F04](../../../business/features/F04-entity-instances.md) · [F05](../../../business/features/F05-media-assets.md) · [F06](../../../business/features/F06-product-catalog.md) · [F07](../../../business/features/F07-product-content-assignment.md) · [F08](../../../business/features/F08-variant-content-assignment.md) · [F09](../../../business/features/F09-content-releases.md) · [F10](../../../business/features/F10-publication-revisions.md) · [F11](../../../business/features/F11-publication-lifecycle.md) · [F12](../../../business/features/F12-content-entitlement-policy.md) · [F13](../../../business/features/F13-content-delivery.md)
## Как применить ответ
Уточнить модели, транспортные и внутренние DF-потоки, реализацию по слоям и точные Functional assertions в соответствующих backend-фичах. Обновить использование контракта фронтендом без создания второй копии его определения. Проектные значения не переносить в стандарты; заглушку не считать принятым решением.
## Current-state snapshot 01.10.2026
Подробная карта: [current-state code snapshot](../../../archive/current-state-code-snapshot-2026-10-01.md). Наличие endpoint/DTO в snapshot не делает его целевым контрактом автоматически.
Подтверждены versioned MVC APIs для ContentModeling, MediaLibrary, CatalogAccess и Publishing. Общий result mapping использует `ApiResponse<T>`, ValidationProblemDetails/400, 404/409/403/401 и business 400. ContentModeling использует `ExpectedRevision`/`Revision` и выдаёт conflict при concurrent edit.
ContentModeling current API покрывает CRUD/read для schemas, schema versions (create/update/publish/archive/delete) и instances/instance versions. Publishing имеет assignments, releases, preview/revision, activation/unpublish/revoke, policy и promotion-update operations. CatalogAccess current API read-only: products/product-variants/licenses + lookup. MediaLibrary имеет upload/read/download.
**Не найден внешний Delivery controller/API project.** Также frontend ссылается на generated `ContentManager.ApiClient`, но его source project отсутствует в frontend archive. Поэтому Q15 нельзя считать полностью закрытым: exact Delivery routes, runtime sync DTO, generated client configuration и VR School event contracts этим snapshot не подтверждены.
## Решение блока 23: внутренняя optimistic concurrency

Блок 23 зафиксировал внутреннюю модель конкурентной записи, а HTTP transport окончательно определён блоком 25 ниже.

Каждый mutable owner Content Manager, для которого возможна конкурентная запись, хранит внутренний `int Version`. Это технический concurrency token Domain/Persistence, а не поле публичного DTO.

Application command для mutation существующего mutable state получает expected `int Version` от transport adapter. Backend сравнивает expected/current Version перед commit; successful state-changing commit атомарно увеличивает Version. При mismatch mutation не применяется, last-write-wins и automatic merge запрещены.

Текущие `Revision/ExpectedRevision` ContentModeling являются legacy naming того же механизма и подлежат унификации во внутренних моделях до `Version` / expected `Version`. Это **не означает** публикацию Version/ExpectedVersion в HTTP DTO или query string.

Publish/Activate/Delete/Archive/Restore/Moderation decision и другие state-changing operations используют ту же внутреннюю concurrency-проверку, даже если операция не называется Update. Внешний HTTP precondition для них — `ETag` / `If-Match` по итоговому решению блока 25.

### SourceVersion

Событийный `Version` VR School на integration boundary маппится в CM projection как `SourceVersion`. Он не используется клиентом и не является optimistic-concurrency token HTTP.

У projection entity при необходимости одновременно существуют:
- `SourceVersion` — ordering/freshness source snapshots;
- внутренний `Version` — optimistic concurrency локальной записи.

### Stale-base

Schema/Instance Draft publish может вернуть отдельный business/domain stale-base conflict даже при совпавшем `If-Match`/внутреннем expected Version.

- HTTP concurrency conflict означает: mutable resource изменился после полученного ETag (`412`).
- stale-base означает: изменилась Published-база, относительно которой готовился Draft (`409` domain conflict).

UI/API не смешивают эти причины.

## Решение блока 25: ETag / If-Match

Внутренний `int Version` остаётся optimistic-concurrency token Domain/Persistence, но **не публикуется в DTO**.

HTTP exact resource response возвращает strong `ETag`, построенный из current Version. Mutation существующего mutable resource требует `If-Match`.

Api-слой преобразует ETag во внутренний expected Version для Application command.

Семантика:
- missing required `If-Match` → `428 Precondition Required`;
- stale/mismatched `If-Match` → `412 Precondition Failed`;
- `409 Conflict` остаётся для предметного конфликта, например stale-base;
- successful response mutable resource возвращает новый ETag;
- list DTO не содержит per-item technical Version.

Текущий `Revision/ExpectedRevision` и промежуточное решение `Version/ExpectedVersion` в transport подлежат замене на ETag/If-Match. Внутренние commands могут использовать expected `int Version`, но это не wire DTO.

Frontend хранит ETag отдельно от NSwag-generated DTO и не создаёт дублирующую form/view model.

## Согласованный Demo профиль authoring API

Пользователь принял Demo решения 14–20. Это target requirements, не описание готового кода. Нормативные правила перенесены в F01–F05; здесь остаются основания и точные границы инженерной материализации.

1. Authoring values — явные recursive Scalar/Object/List variants: named value имеет `namedFieldId`, anonymous List item не имеет fake FieldId. Runtime keyed JSON — отдельная projection. Все 15 типов и nested Lists буквально проходят OpenAPI → NSwag serialization/deserialization round-trip; exact discriminator/property encoding и CLR signatures ещё материализуются.
2. Instance Save передаёт FULL desired authoring snapshot: absent optional normal value очищается; collapsed/unrendered nodes остаются в request. Orphan/raw incompatible work удаляется только explicit discard. Demo PATCH не вводится. Missing required и retained incompatible work Save допускает; newly entered type-invalid отклоняется.
3. ItemKey opaque, client-generated-on-add только для List<Object>; unique per конкретная List occurrence и validated server-side. Copy/Reorder сохраняют key; он не EntityId/FieldId. Scalar List не имеет ItemKey. Diagnostic path использует FieldId/ItemKey, а не index List<Object>.
4. Persisted orphan имеет stable server ID. Computed incompatibility имеет deterministic location token, связанный с exact schema/content basis. GET не пишет и не придумывает persisted ID. Save валидирует basis и explicit discard; изменившаяся basis отклоняет действие до изменения content.
5. Own ETag относится к saved mutable root; diagnostics имеют отдельную basis identity exact schema/content/baseline observation. Accept/delete consent связываются с показанным context/exact impact set, а не только own ETag. Accept не меняет payload и не требует strict Publish completeness: missing required/retained orphan допустимы при валидных identity/evolution/context checks.
6. Create/clean-create/copy/upload используют operation ID клиента и durable original result owning BC. Same ID/input → original committed result; same ID/different input → conflict. No blind new Create after lost response. В одной local transaction находятся created roots и result; upload stages/recovery сохраняют тот же operation/AssetId.
7. Common issue/error shape содержит stable machine code, message, address; list APIs следуют единым paging/sort conventions со stable tie-breaker. Binary/ZIP success не JSON wrapper; ошибки common profile. Exact codes/routes/header carriers/defaults/limits публичного transport остаются инженерной детализацией, без произвольных чисел.

**Logical `CreateOperationResult` (target durable model у owning BC; новое имя предложено, не существующий исходник):**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Operation identity (имя уточняется)` | Уточняется | Да | ID созданной клиентом попытки | Stable; unique в согласованном owner/operation scope; carrier/type Q15 |
| `Input identity (имя уточняется)` | Уточняется | Да | Проверяемое согласованное содержание исходного запроса | Exact comparison representation уточняется; нельзя сравнить только filename/SchemaKey |
| `Committed result (имя уточняется)` | Уточняется | Условно | Durable original identities/result операции | Фиксируется вместе с локальным созданием; повтор возвращает тот же result |
| `Stage (имя уточняется)` | Уточняется | Условно | Durable upload Pending/Available/recovery observation | Только staged media operation; exact composition Q21 |

**Logical `AuthoringIssue` (target common model, имя предложено):**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Code` | `string` | Да | Stable machine-readable причина | Concrete code vocabulary уточняется; не display message |
| `Message` | `string` | Да | Читаемое описание | Не full payload/stack trace |
| `Address` | Уточняется | Да | Typed/logical field/value/request/summary location | FieldId/ItemKey stable occurrence address; exact wire Q15 |

Открыто: exact DTO union/discriminator/null serialization, HTTP conditional representation boundary и размещение supplemental diagnostics без неверного 304, names/signatures/error vocabulary, operation ID transport/result persistence/input comparison, paging defaults/limits и codegen configuration. Не открыты заново принятые full-snapshot/discard/ItemKey/basis/idempotent-create policies.

## F01: детализация целевого контракта

Связанные документы: [backend F01](../features/F01-create-entity-schema.md), [frontend F01](../../frontend/features/F01-create-entity-schema.md), [UI composition Q13](../../frontend/questions/Q13-ui-scope.md). Общие решения о server IDs, нескольких Draft, nullable published number и ETag не пересматриваются. Ниже недостающие детали для реализации уже согласованного сценария.

### Подтверждённая исходная точка

В актуальном checkout F01 просмотрены CreateSchemaCommand/Handler/Validator, новый CreateEntitySchemaRequest, EntitySchemaDetailsDto, Domain roots/receipt, EF mappings, F01SchemaQueryHandlers, SchemaCreationRepository и controllers. Ссылки на конкретные исходники находятся в [backend F01 §1](../features/F01-create-entity-schema.md#1-область-реализации); frontend pages/flow/generated client — в [frontend F01 §11](../../frontend/features/F01-create-entity-schema.md#11-последовательность-изменений-и-открытые-вопросы).

- Create request имеет OperationId + InitialDraft.Title/ChangeComment/recursive Fields; create fields без ID, все identities создаёт backend. POST возвращает Details DTO + logical ETag.
- Collection GET `/api/v1/entity-schemas` возвращает paged list без trees/tokens; root GET — Details с ≤20 summaries/VersionsTotalCount и own ETag; exact Draft GET — recursive Content и own Draft ETag.
- ContentModeling хранит independent schema/version roots и SchemaCreationReceipt. Receipt сохраняет normalized input/original DTO/original logical Version атомарно; replay не зависит от process memory и выполняется до нового key precheck.
- Repository flush выполняется внутри owner transaction; command UoW commit остаётся границей success. PostgreSQL locks operation→key и specific ix_entity_schemas_key mapping различают replay input conflict и duplicate key.
- UI использует `/schemas`, `/schemas/new`, `/schemas/{id}/versions/{versionId}`; F01 workspace readonly. Legacy F02/F03 mutations не доказательство их целевого ETag/If-Match contract.

Контракт ниже выбран. Выполненные F01 проверки зафиксированы в [backend §14](../features/F01-create-entity-schema.md#14-проверенное-поведение-demo-f01): Unit405/405, PostgreSQL Integration23/23, scoped Functional/OpenAPI34/34, UI build/runner, real browser exact Draft и replay original body/ETag после backend restart. Это не production migration и не готовность остальных Demo-фич. Legacy Update/Publish modern snapshot отклоняется400 до F02/F03, legacy stored schemas остаются writeable.

### Детали контракта и оставшиеся вопросы

| Вопрос | Известная граница | Что требуется выбрать / проверить |
| --- | --- | --- |
| Ограничения metadata | Принято: Name/Title/FieldName/optionName required 1–255; Description/ChangeComment/HelpText optional ≤1024; внешний Trim до проверок/сохранения, optional blank → null. Demo schema/field keys определены ниже | Продуктовое решение закрыто; реализовать единые constraints/VO/FV/mapper/EF/форму и пограничные fixtures. Длина инженерно измеряется в UTF-16 code units (.NET String.Length/FluentValidation MaximumLength); wire annotations материализуются инженерно |
| Key contract | Demo: explicit camelCase `^[a-z][a-zA-Z0-9]*$`, 1–50 символов; case-sensitive; SchemaKey global unique, field key unique whole-tree; casing сохраняется без Camelize | Продуктовое правило Demo принято. Проверить согласованность VO/FV/precheck/DB index; переход Demo → MVP с kebab-case suggestion отдельно уточняется |
| Recursive wire representation | Выбраны camelCase properties/string enum names, definition.type + applicable payload, recursive Item/Fields/Options; required IsRequired/Deprecated/option Deprecated; unknown members reject | Проверить OpenAPI → NSwag serialization/deserialization всех15 types и nested Lists; MVP constraints/comparer не включаются в текущий Demo request |
| List query | EntitySchemaListRequest: Search≤255/SkipCount0/MaxResultCount20max200/Sorting Name или ModificationTime ASC/DESC + Id ASC; Details summaries≤20 + VersionsTotalCount | Проверить SQL filters/count/paging/order и bounded details; full history/read rail F02 отдельно; explicit exact version не обязан входить в20 summaries |
| Routes | POST/GET collection, GET exact root и nested exact Draft по F01; lookup alias не сохраняется | Проверить API/OpenAPI/NSwag вместе; legacy F02/F03 routes остаются отдельной target-работой |
| Response metadata и errors | Internal VersionedResource<T>, own strong numeric ETag/no-store на create/root GET/Draft GET; numeric token отсутствует в public DTO | Verify headers/generated transport; create paths Request.InitialDraft.Fields[index]..., duplicate409 entity_schema.duplicate_key, mismatched OperationId409 entity_schema.operation_input_conflict |
| Неизвестный исход POST | Body OperationId:Guid non-empty; durable SchemaCreationReceipt normalized InputJson/original ResultJson/LogicalVersion в одном owning commit | Проверить same normalized input/original DTO+ETag replay после restart и дальнейшего resource change; different input409, concurrent first attempts; SchemaKey не identity попытки |

**Ответ:** F01 Demo wire/read/replay contract реализован и проверен в указанном локальном контуре. Future MVP/F02/F03 обсуждаются и реализуются отдельно; перечисленные инженерные детали F01 не требуют повторного запроса пользовательских решений. Auth, шина и внешние BC не становятся dependencies Demo.

### Принятый Demo key contract

Для `EntitySchema.Key` и `SchemaField.Key` принят явный `camelCase`: regex `^[a-z][a-zA-Z0-9]*$`, длина 1–50 символов. Первый символ — строчная латинская буква; далее допустимы латинские буквы обоих регистров и цифры. Регистр сохраняется без `Camelize`/case folding. Сравнение case-sensitive (`StringComparison.Ordinal` / `StringComparer.Ordinal`); schema key глобально уникален, field keys уникальны во всём дереве одной SchemaVersion, включая поля во вложенных Object/List. `fieldKey` и `fieldkey` могут сосуществовать; повтор одного точного key в разных ветках запрещён.

Нормализация key удаляет внешние whitespace до проверки и хранения; длина, формат и уникальность проверяются по этому значению. Внешний Trim согласован пользователем и для metadata в разделе ниже. VO, FluentValidation, existence precheck и DB unique index должны применять одну семантику; автоматическое исправление регистра или формата запрещено. Recursive Demo wire/list query/durable replay выбраны в таблице выше; их conformance проверяется инженерно.

### Принятый F01 metadata contract

Пользователь подтвердил следующие ограничения; они заменяют прежний открытый вопрос о limits/null-empty/whitespace normalization и не означают, что код уже реализован.

| Metadata | Required | Лимит после внешнего Trim | Blank / отсутствие |
| --- | --- | --- | --- |
| Schema Name, Draft Title, FieldName (field Name), optionName (Enum/FlagEnum option Name) | Да | 1–255 символов | Отклоняется |
| Description, ChangeComment, HelpText | Нет | ≤1024 символов | null |

Удаляются только внешние пробельные символы до валидации и сохранения; внутренние пробелы и регистр сохраняются. Optional empty/whitespace нормализуется в null, required whitespace не становится валидным значением. Обрезание до лимита, автоподстановка служебного текста и перенос этой политики на runtime values не допускаются. Field/option Name rules действуют во всём recursive дереве Object/List. Demo key contract 1–50/camelCase/digits/Ordinal остаётся прежним; metadata limits не заменяют его.

Нормативное применение находится в [business F01](../../../business/features/F01-create-entity-schema.md#метаданные-и-нормализация), [backend F01 §2.3](../features/F01-create-entity-schema.md#23-инварианты-и-constraints) и [frontend F01 §6.2](../../frontend/features/F01-create-entity-schema.md#62-состав-представления-и-формы). Для совместимости с существующими VO инженерно выбрано измерение длины в UTF-16 code units (.NET String.Length/FluentValidation MaximumLength), без отдельного продуктового решения о Unicode counting. Требуется реализовать одну семантику/annotations в validators/factories/mapping/persistence/UI; fixtures покрывают 1/255/256, 1024/1025, внешние/внутренние whitespace, blank/null и recursive paths. Остальные вопросы таблицы выше сохраняются и не требуют повторного выбора этих ограничений.

### MVP DateTime temporal contract

[Q34](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость) согласовал immutable TemporalSemantics=Naive/Offset. Model/enum owner F01 §2.1, scalar/pure TemporalCodec owner F04 §2.1/§4.4. Новые MVP fields Offset; existing Naive→Offset требует нового FieldId; Copy historical definitions retains Naive. Audit/lifecycle dates не subject dynamic field migration.

Logical Scalar temporal payload:string. Offset exact yyyy-MM-dd'T'HH:mm:ss.fffffffzzz либо UTC ...fffffffZ; Naive /1 ...fffffff без offset. Parse без local timezone, seven digits, known whole-minute offset ±14/local+UTC range, rejects leap/-00/missingoffset. Value не переопределяет schema semantics; exact definition/binding guides parsing. Generated Naive DateTime(Unspecified), Offset DateTimeOffset; explicit converters preserve authored offset/100ns, runtime UTCZ. Valid constraint violation Draft issue, intrinsic invalid input Save reject.

Temporal product syntax/precision/range/comparison согласованы. Concrete DTO union/discriminator/member carrier, OpenAPI/NSwag converter integration и whole new-profile codecs/golden corpus остаются engineering Q15/Q19/Q31. No extra route/migration command/timezone/profile UI selector.

### MVP typed constraints и comparer transport

[Q35](../../../business/questions/Q35-value-constraints.md) согласовал первый MVP набор и поведение Save/Copy/Publish. Logical models/поля принадлежат [F01 §2.1/§3.3](../features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects), read comparer DTO — [F02 §3.3](../features/F02-create-schema-version.md#33-dto-и-parameter-models). Это не расширяет Demo и не подтверждает current source implementation.

**Уточняется до реализации:** concrete known DTO union/discriminator, typed numeric bounds encoding/temporal bound DTO carrier (Offset syntax согласован Q34) и nullable/omitted setting semantics; logical clear/replacement/unchanged envelope для definition settings, включая recursive anonymous List.Item; точные diagnostic code/address fixtures. Rules не отправляются как произвольный expression/script/JSON bag. OpenAPI → NSwag typed recursive round-trip должен быть подтверждён до UI integration.

ObjectFieldPath frozen definition хранит ordered server SchemaFieldIds, но create/add новые fields по F01 не содержат IDs. Нужно определить transport reference к new request fields, mapping после полного server ID assignment и atomic validation resulting path. Client-generated Guid, nullable ID/IsNew hybrid и скрытое сохранение invalid comparer не являются обходом. Existing fields адресуются stable IDs. Exact new-field reference wire здесь пока не выбран; Q35 семантика comparator уже согласована.

Instance explicit «Сортировать» — local stable Draft operation, затем существующий Create без own token либо Save с исходным If-Match; новый HTTP sort endpoint отсутствует. Type-valid extra-rule issues не превращают Save в error, strict Publish блокируют. Concrete result/error metadata используют общий issue/basis profile F04; exact whole new schema/instance/Registry/C# profile bytes остаются Q19/Q31; Offset product semantics согласованы Q34.

## F02: детализация целевого контракта

Связанные документы: [backend F02](../features/F02-create-schema-version.md), [frontend F02](../../frontend/features/F02-create-schema-version.md), [product profiles Q09](../../../business/questions/Q09-editing-and-deletion.md#уточнения-f02-создание-и-удаление-draft), [persistence Q20](Q20-persistence-and-migration.md). Общий strong numeric ETag/If-Match profile остаётся приоритетным относительно legacy Revision/ExpectedRevision DTO.

### Подтверждённая исходная точка F02

Сверка source 07.10.2026: F01 уже реализовал независимые EntitySchema/EntitySchemaVersion roots, собственные int Version, multiple ненумерованные Draft, recursive definition tree всех 15 типов, JSONB и durable operation receipt. Прежний EditorCommandHandler/major-minor/one-Draft/parent Revision — legacy surface, не исходная модель нового F02.

Материализованы [SchemaDraftRequests](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Models/SchemaDraftRequests.cs), [commands](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Commands/SchemaDraftCommands.cs), [queries](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Queries/SchemaDraftQueries.cs), [SchemaVersionDetailsDto](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Models/SchemaVersionDetailsDto.cs), [SchemaDraftCommandHandlers](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntitySchemas/CommandHandlers/SchemaDraftCommandHandlers.cs), [domain change set/applier](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/ValueObjects/SchemaDraftChangeSet.cs) и [draft repository](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/EntitySchemas/Repositories/SchemaDraftRepository.cs). Полные поля/DF/ports определены один раз в backend F02; они не дублируются как второй контракт здесь.

### Принятые и материализованные Demo детали

| Область | Решение / source evidence | Граница проверки |
| --- | --- | --- |
| Clean/Copy | Clean local-first; CreateCleanSchemaDraftRequest содержит OperationId/Title/ChangeComment/Fields. CopySchemaDraftRequest — OperationId/Title/ChangeComment; source exact route. Original result/IDs/token durable в owning receipt | Server empty Schema Draft не создаётся; Browser проверил clean empty Save local block и valid first Save server Create |
| Recursive Save | UpdateSchemaDraftRequest требует Creates/Updates/Deletes/Restores, включая []; Creates: ParentFieldId+ItemDepth+F01 Field без IDs. Updates: existing FieldId и mutable metadata/Definition; recursive Definition.Item адресует anonymous List.Item. Restores: exact SourceVersionId+FieldId в existing Save | Whole-set conflict/identity/parent-chain/minimum/monotonic guards Domain; no fake ItemId/new restore endpoint |
| Options | Desired option set; Key immutable, Name mutable, Deprecated monotonic. Never-Published key разрешено удалить; key, встречавшийся в любом Published history, удалять нельзя — deprecate. Recursive List.Item policy та же | Это Save guard, не только PublishIssue; Q09 подтверждает product choice |
| Text constraints | F01 metadata normalization: outer Trim; Title/field/option Name1–255, optional comment/help≤1024 UTF-16 code units; blank optional→null | Никаких новых literals/лимитов; OpenAPI/generated request boundary проверена suite/runner |
| List | SchemaVersionListRequest: Status, SkipCount default0, MaxResultCount default20/range1–200, Sorting allow-list CreatedAt ASC/DESC или VersionNumber ASC/DESC. Default rail Draft first, Draft newest, Published business-number newest, stable Id tie-breaker | Missing owner404; summary без полного дерева/per-row ETag; Query/HTTP behavior подтверждён scoped Functional suite |
| Read/write result | GetSchemaVersionDetailsQuery и clean/copy/Save/Accept возвращают VersionedResource<SchemaVersionDetailsDto>; Api Resource+own ETag. DTO содержит full modern tree/IDs, metadata/current-baseline/stale/issues/base changes без technical Version | No hidden HTTP GET; legacy definition explicit diagnostic, не lossy rewrite |
| Copy precondition/replay | Source If-Match обязателен для Draft **и Published**. Operation lock→receipt/input comparison происходит до current owner/source/precondition; successful replay возвращает original committed DTO/token. Input включает original source ExpectedVersion | Same OperationId+same input/header; different input409; missing/malformed header Api всё равно отклоняет |
| Accept basis | AcceptSchemaDraftBaseRequest.PublishedVersionId required nullable; подтверждает именно показанную current Published identity. При смене pointer409 schema-draft.review-basis-changed | Own stale412 отдельно; baseline-only, no merge; unavailable current basis409 |
| Delete consent/composition | SchemaDeleteRequest body: ConsentToken+Cascade, без query Version/ExpectedRevision/versions/count. SchemaDeleteImpactDto: full counts/blockers/token/SchemaDraftIds/DependentDrafts; dependency summary contains exact instance owner/version/schemaVersion IDs и name | Preview observation; client IDs не removal authority; 5-second UI delay no DB lock |
| Consent freshness | Opaque SHA256 binds scope, schema lifecycle/revision/history/status/IDs/current pointers, exact dependency IDs/status/schemaVersionId, owner revision и dependency content. Under gate current basis сравнивается целиком; same-count change/edited dependency требует новый preview+consent | 409 schema-delete.impact-changed; Cascade=true required when dependencies exist; no Published cascade |
| Owner cleanup | Draft-version cascade удаляет dependent Instance Draft versions, сохраняет logical instance owners, включая пустые. Logical schema Delete блокируется любым instance owner; отдельный explicit F04 owner Delete необходим. Any Published schema history blocks physical delete | Last Draft leaves logical schema; nullable deprecated internal latest pointer не public DTO member и не schema Version bump |
| Logical metadata | PUT /api/v1/entity-schemas/{id}: UpdateSchemaMetadataRequest(Name,Description), own schema If-Match, VersionedResource<EntitySchemaDetailsDto>. Flush within owner transaction + in-process query; final response after outer commit | Key/version roots unchanged; no hidden HTTP GET |
| Demo transaction gate | Domain ISchemaDraftRepository: operation gate shared F01 namespace, затем per-SchemaId advisory gate на physical ContentModeling owner transaction; fresh exact tracking/history/dependencies после gate. Legacy EditorRepository schema/version/instance loads участвуют | Не global MVP guard и не new external BC; queries/preview не locks; PostgreSQL Integration confirms contention/freshness |

Подтверждены Unit **427/427** (coverage 91.98% lines/87.72% branches), real PostgreSQL Integration **36/36** после standalone fixture query-port DI и Functional HTTP/OpenAPI **50/50**. Последняя suite проверяет concurrent Save один 200 / один 412, Copy replay original header после source change, Accept basis drift 409/no merge, immutable Published metadata capture и nested create atomicity. Historical Restore HTTP прошёл: exact source FieldId + metadata в том же Save, historical source неизменен. Scoped App Publishing/Delivery 9/9 также прошли; они подтверждают совместимость выбранного контура, не весь будущий publishing workflow.

UI build/runner F01/F02/legacy и real HtmlRenderer прошли. Actual browser проверил Copy→Save→Reopen/source FieldIds,412 с сохранением local form до explicit Discard/Reload и clean local-first valid Create. Full delete composition/unchecked separate consent/disabled action проверены browser с Cancel; permanent GUI delete не выполнялся, deletion commit доказан real PostgreSQL suites. Nullable LegacyFields — read-only historical compatibility для старого F04 editor, не modern form conversion; modern instance definition editor следующий F04. F02 Demo реализован в указанном scope, но это не весь Demo/Publish F03/MVP/deployment/production migration.

### Оставшаяся граница и MVP детали

Scoped query/controller/DI, strict HTTP contract, generated client/recursive requests и UI/browser flows подтверждены проверками выше. Historical Restore HTTP подтверждён; отдельного Restore GUI действия нет. Browser destructive commit отдельно не проверялся. In-process/HTTP tests не доказывают каждый будущий UI/MVP/production scenario; подробная матрица evidence — backend/frontend F02.

Retire/Restore Published, Archive/Restore logical schema, repeated lifecycle/no-op profile и typed constraints/comparer transport относятся к MVP. Глобальный меж-BC consistency gate/public foreign reads и retention принадлежат Q20; per-schema Demo adapter их не подменяет. Неизвестные MVP детали остаются `Уточняется`; они не вводят auth, broker или дополнительные BC в Demo F02.

## F03: детализация целевого контракта

Связано с [backend F03](../features/F03-publish-schema-version.md), [frontend F03](../../frontend/features/F03-publish-schema-version.md), [Q19](Q19-content-hashes.md#f03-профиль-схемы-и-эталонные-векторы), [Q20](Q20-persistence-and-migration.md). F03 использует F01/F02 models/read contracts и общий strong ETag profile блока25. Bodyless Publish200 exact Published DTO+ETag, integer numbering и sibling stale409 не открываются повторно.

### Реализованный Demo contract F03

Source links находятся в [backend F03](../features/F03-publish-schema-version.md) и [frontend F03](../../frontend/features/F03-publish-schema-version.md). Реализация использует независимые roots F01/F02 и существующую ContentModeling UoW; подробный scope и проверки — [implementation plan](../f03-demo-implementation-plan.md).

| Область | Конкретный Demo contract |
| --- | --- |
| HTTP input/result | Bodyless `POST /api/v1/entity-schemas/{schemaId}/versions/{versionId}/publish`; required strong own `If-Match`; 200 `ApiResponse<SchemaVersionDetailsDto>`/новый own ETag/no-store. Непустой body, включая chunked, 400 `schema-publish.unexpected-body`; zero-length body допустим. Номер/hash/time/base/tree не caller authority |
| Parser/precedence | Общий `StrongIfMatch`: missing428; weak/wildcard/multi/leading-zero/nonpositive/overflow400. После fresh owner load: missing/foreign404 → own stale412 `concurrency.precondition_failed` → nonDraft409 `schema-version.invalid-state` → availability/base/strict guards. Старый token повторного Published Publish даёт412, текущий409 |
| Base/state | Base != latest Published →409 `schema-draft.base-outdated`, без auto-rebase/number retry. Active owner и доступный Draft обязательны; unavailable selected owner/version409 `schema-version.unavailable`; unavailable current Published basis409 `schema-draft.base-unavailable`. Retire/Archive UI не вводятся; legacy Archive отвергает modern definitions |
| Strict issues | Structural400 `schema-publish.structure-invalid`; history evolution409 `schema-publish.evolution-invalid`; unsupported legacy400 `schema-version.legacy-definition`. ProblemDetails содержит `code`, `detail`, `issues:[{code,message,address}]` и `errors:{address:[message]}`. Все distinct field/history evolution violations собираются; structural malformed snapshot даёт общий `content` blocker |
| Addresses/history | Полная owner history, без UI paging/Registry reads. Stable paths: `content.fields[FieldId].key/.definition/.isRequired/.deprecated`; Object fields/anonymous `.item` сохраняются; option identity — `.options[key]`. Rules одни с F02 Applier; Save сохраняет first-failure поведение, Publish возвращает адресуемый набор. Unrelated unpublished legacy snapshots допускают clean modern tree с новыми IDs; unsupported Published history и reused legacy identity отвергаются без reinterpretation |
| Guard/commit | Existing F02 per-schema transaction advisory gate до authoritative reads и до owner commit/rollback, отдельные optimistic own tokens. Publish/Register изменяют version+schema одним increment каждый в одной transaction. Concurrent different Draft даёт200/409; один resource со старым token412. Номер не резервируется; rollback не расходует sequence. Storage uniqueness failure не объявляется own stale412: existing generic409 `concurrency.conflict`, без выдуманного stale-base reason |
| DTO/metadata | Same version ID, immutable authoring snapshot/Title/comment/CreatedAt/base, Published number/time/frozen Schema Name-Key, `contentHashProfile`/`contentHash`. Internal `VersionedResource<T>` не wire DTO. Header/rail обновляются отдельно от применённого полного mutation result; hidden exact GET для результата отсутствует |
| Canonical/storage | Pure `ContentManager.Canonicalization` netstandard2.1 без Domain/EF/ASP.NET deps; typed `cm-schema-content/1` writer/strict restore. Lowercase SHA-256 по exact UTF-8 без BOM/LF; captions/comments/help/owner/time excluded. Atomic bytea/profile/hash/text; text — strict same-byte decode. Migration `20261008085953_F03CanonicalPublication`, positive/completeness/text-byte CHECKs и existing unique number index. `20261008094758_F03ScopeLegacySchemaNumbers` scopes old raw semver index to NULL-profile legacy rows; unsafe mixed-history Down refuses. Legacy rows не relabel/renumber, historical /1 не rehash-ится |
| UI/outcome | Clean saved modern Draft only; dirty не вызывает Save. Captured IDs/own ETag/sequence, one request/no double/no replay. 412 сохраняет input/конфликт до explicit reload; stale409 refresh diagnostic basis без подмены own token/input. Unknown result требует explicit exact read; Published показывает durable факт, не авторство потерянного attempt. Подтверждённый read Draft разрешает новое явное действие |
| Generation/events | NSwag регенерируется через `.run/generate-api-client.run.xml` → `.tools/generate-api-client.sh`; generated source не редактируется. Schema publication domain fact не означает Registry update/Delivery/Product Publish. Required broker/moderation/local external consumer отсутствуют |

### Оставшаяся детализация MVP

Новые constraints/comparer/Offset profiles, exact new-codec fixtures и межконтекстный global consistency guard остаются Q19/Q20/Q31. Подробный сценарий нового Schema Publish от уже Retired/Withdrawn baseline уточняется вместе с соответствующим lifecycle use case; Demo не вводит эти действия и не принимает unavailable basis автоматически. Foreign lifecycle facts/fresh post-rollback classification и guard wait configuration проверяются при реализации MVP контура. Ни один из этих пунктов не меняет готовый Demo wire/profile и не добавляет auth/moderation/шину в Demo.

## F04: детализация целевого контракта

Связано с [backend F04](../features/F04-entity-instances.md), [frontend F04](../../frontend/features/F04-entity-instances.md), [Q36](../../../business/questions/Q36-entity-instance-lifecycle.md#f04-оставшиеся-продуктовые-границы), [Q19](Q19-content-hashes.md#f04-профиль-значений-экземпляра), [Q20](Q20-persistence-and-migration.md), [Q21](Q21-media-storage.md). Own-root ETag, multiple Draft, target baseline и immutable Published уже приняты; здесь уточняется реализация их границ.

### Concrete Demo source contract

Current server implementation and source links are collected in [F04 §1.1](../features/F04-entity-instances.md#11-конкретный-серверный-профиль-demo). Independent owner/version roots, own ETags, multiple Draft, same-target baseline, full snapshots/orphans, global committed Published sequence и frozen `cm-instance-content/1` реализованы в scoped Demo contour. Legacy data остаются отдельным сохранённым profile; их наличие не означает автоматическую modern conversion. Это source evidence, не декларация полной Functional/OpenAPI/browser acceptance или реализации MVP/F05/F13.
### Принятые параметры Demo F04

- Instance Key: trim без смены регистра, `^[a-z][a-zA-Z0-9]*$`, 1–50 символов, Ordinal case-sensitive, уникален внутри логической схемы и неизменяем после создания.
- Restricted HTML Text: максимум по умолчанию 32 768 символов после sanitization; серверная настройка `ContentModeling:InstanceAuthoring:TextMaxLength`, чтение UI через authoring-settings и version details.
- Save: `POST /api/v1/entity-instances/{id}/versions/{versionId}/save`, strong own If-Match, полный подтверждённый transformed result + новый ETag.

### Материализованные инженерные решения Demo

| Область | Concrete Demo contract | Оставшаяся проверка / следующий этап |
| --- | --- | --- |
| Recursive DTO / scalar precision | `InstanceValuesDto.Fields[{NamedFieldId,Value}]`, `Value.Kind=Scalar/Object/List`; exactly matching ScalarJson/Fields/Items. ScalarJson — JSON literal string, сохраняющий Int64/Decimal и temporal precision. Anonymous items без fake FieldId; Object item имеет opaque ItemKey | Полная generated NSwag/browser/Unity conformance отдельно; Offset/new MVP profiles не вводятся Demo |
| Orphan/provenance | Version хранит SavedSchemaContent; saved OrphanValue хранит Definition, FieldId, Path, raw ValueJson и stable server Id. GET preview не пишет; computed orphan получает SHA256 opaque token от diagnostics basis/location/raw work | Publishing/F05 historical retention и full physical storage policies относятся к своим owner features |
| Save / explicit discard | SaveInstanceDraftRequest: full Content + DiagnosticsBasisToken + DiscardOrphanValueIds + ComputedDiscardTokens; отсутствующий optional normal value очищается, raw/orphan work удаляется только explicit discard | UI сохраняет collapsed/unrendered work; отдельная browser acceptance. Advanced Q35 constraints — MVP |
| Freshness / representation | Own ETag только saved root; GET no-store; DiagnosticsBasisToken связывает saved version state, exact target schema snapshot/Version, same-target base, relevant withdrawal/lifecycle, configured Text maximum и current media facts; unrelated logical instance/schema Rename не входит в basis | Нельзя объявить whole derived DTO fresh по unchanged own tag; future caching/access profiles отдельно |
| Accept / deletion consent | AcceptInstanceBaseRequest: explicit nullable PublishedInstanceVersionId + DiagnosticsBasisToken; no merge/content mutation. DeleteInstanceRequest: server ConsentToken exact owner/history/state. F02 cascade basis включает own instance-version Version/orphans/provenance; dependent-reference removal берёт shared AssetId guard через owner commit после exact confirmation | MVP historical reference authority fail-closed через public Publishing port; absent Demo Publishing не вызывается |
| Create / recovery | CreateInstanceRequest: OperationId/SchemaId/TargetSchemaVersionId/Key/Name/Content. Clean/copy request: OperationId/TargetSchemaVersionId. Receipt хранится вместе с roots, same normalized input возвращает original result; different input conflicts | Lost existing-write outcome не разрешает blind retry с новым tag/operation ID |
| HTTP / headers / queries | Create/clean/copy возвращают initial/new InstanceVersionDetailsDto и version ETag; logical GET отдельно. Rename `PUT /entity-instances/{id}/metadata`; Return `POST /entity-instances/{id}/return`; Save выбранный POST ниже. Exact writes strong own If-Match; missing428/stale412/independent409/value400. Lists SkipCount/MaxResultCount default20/max200, summary без full Content | Generated header access и full browser flow подтверждаются отдельными tests; permission/auth отсутствуют в Demo |
| Media boundary | Public IMediaAssetReader/IMediaAssetCapture/IMediaAssetGuard; independent primary read scope, one shared singleton AssetId registry, owner lease through commit/rollback disposal. Strict capture требует Available/verified compatible kind и frozen five-field descriptor | Полный F05 policy/delete/YARP/MinIO recovery contour и multi-replica MVP SQL guard не считаются готовыми по этому минимальному seam |
| Published asset read | `InstanceVersionDetailsDto.PublishedAssetReferences` → `PublishedInstanceAssetReferenceDto(AssetId,AssetKind,ContentType,SizeBytes,Sha256Hash)`; проекция читает verified stored canonical bytes/profile/hash по SavedSchemaContent, без live enrichment/URL/mode. Recursive identical AssetId facts дедуплицируются; corrupt/conflicting snapshot отклоняется. Draft/legacy collection пуст | Read-view carrier не меняет `/1` bytes/hash. DeliveryMode carrier/profile/same-FieldId остаются post-Demo Q37/Q34; current F05 download отдельно |
| Withdrawal / explicit return | Bodyless logical own-token Withdraw/Return. Return проверяет retained Published target/media/canonical representation и отвергает exact Withdrawn/Retired target, меняет только WithdrawnAt/Version; Publish также проверяет RetiredAt и не очищает withdrawal | F13 новый plan должен потреблять committed availability; old plan/runtime/invalidation acceptance принадлежит F13 |
| Errors / commit | Concrete machine codes находятся в handlers/mapper; distinct own precondition, diagnostics/base/delete basis, lifecycle, values/media errors. Prepared resource не persisted outcome до CQRS owner Complete | Полный error/status/OpenAPI/late commit conflict matrix проверяется Functional; transport failure не success |
| Persistence / legacy | Own int Version (logical physical legacy revision bigint сохраняется), nullable modern Draft ordinal, exact profile/bytea/digest, JSONB authoring/provenance/orphans. Published legacy ordinal переносится в derived counter без изменения старых hashes/bytes/status; inconsistent sequence rejects Publish | Legacy CreateNextDraftVersion требует loaded history и не допускает modern versions. Явная future legacy conversion/repair требует отдельного решения; запрещены inferred profile, silent rehash и reset |

Авторские members в запросах не принимают runtime hash/number/profile/actor/technical Version как authority. Unknown members и неоднозначный variant — error. Legacy exact read имеет readonly LegacyFields/LegacySchemaFields и IsLegacyContent; эти raw поля не являются входом скрытой modern conversion. Application InstanceDetailsProjector владеет public media I/O; query Persistence загружает только own BC snapshots. Typed value errors сохраняют machine Code и Path: /fields/{FieldId}, /items/@{escaped ItemKey} для keyed Object occurrence, /items/{index} для anonymous scalar/List occurrence.

ScalarJson literal `null` отличается от omitted named value и CLR default, но ни один из 15 Demo value types не допускает новый nullable input: intrinsic error сохраняет typed path. Raw null в retained legacy/orphan work сохраняется без silent conversion.

Persisted orphan ID и computed token не взаимозаменяемы; latter действует только с соответствующим DiagnosticsBasisToken. `ItemKey` сохраняется Copy/Reorder, unique per List occurrence; positional scalar/list addresses не подменяют stable Object item identity.

Save POST возвращает full confirmed sanitized/reconciled DTO с own ETag после commit. Требование full desired snapshot сохраняется; limitation successful transformed PUT validator [RFC 9110 §9.3.4](https://www.rfc-editor.org/rfc/rfc9110.html#section-9.3.4) к выбранному POST не относится. Имя `VersionedResource<T>` — внутренний result carrier; Version не попадает в JSON.

### Оставшиеся границы MVP и acceptance

- Offset DateTime и new canonical/schema/formal/C# profiles, Q35 advanced constraints/portable equality/order conformance.
- Logical Archive/Restore, Published Retire/Restore, staff access/permission names и MVP repeated-action/audit profiles. Demo withdrawal не приравнивается к этому lifecycle.
- Historical Publishing reference authority, full F05 usage/delete/public gateway/recovery и complete upload-policy verification; minimal VerifiedKind/descriptor seam не закрывает F05.
- MVP common PostgreSQL consistency guard, foreign primary scopes/connection partition и real multi-replica writer matrix; Demo application AssetId guard не доказывает эту coordination.
- F13 new-plan selection/withdrawal/removal/runtime consumption и SDK/Unity byte conformance; F04 не реализует Delivery side effect.
- Complete Functional/OpenAPI/generated recursive all-type/browser acceptance и unknown-outcome/late commit classification. Source types и scoped PG tests не заменяют эту проверку.

**Ответ:** перечисленные Demo carriers и guards материализованы в source F04; прежняя таблица больше не оставляет их открытым product/API выбором. Future MVP/foreign owner и итоговая acceptance остаются отдельными обязательствами; документация не объявляет их завершёнными.
## F05: детализация целевого контракта

Связано с [backend F05](../features/F05-media-assets.md), [frontend F05](../../frontend/features/F05-media-assets.md), [Q21 F05](Q21-media-storage.md#upload-и-lifecycle), [Q12](../../../business/questions/Q12-media-and-retention.md). Target /api/v1/assets metadata/mutations и CM-owned YARP gateway `/storage/assets/{assetId}` Q21, stable AssetId и own metadata ETag уже описаны в F05; current /media-assets/download и ObjectKey DTO не являются выбранным контрактом.

| Область | Известный contract | Требуемая детализация |
| --- | --- | --- |
| Multipart upload | POST новый asset, no If-Match,200 Available AssetDto+ETag | Part name/file count, filename/type/length metadata, no ContentManager JSON wrapper around bytes; declared/actual mismatch/empty file и 400/413 Q21; body limits и generated multipart signature |
| DTO / metadata | AssetDto/list без location/credentials/technical Version; size/hash incomplete nullable, final Available complete | Field names/status enum serialization/time/null/digest format; audit RegisteredAt, query normal/recovery/archive scope; inherited current Length/Hash/ObjectKey не сохранять случайно |
| Library query | Search FileName, server page/count/supported sort/status, Available picker | Query names/defaults/limits/comparison/sort stable tie-breaker; recovery status visibility/filter; public count и parent picker state |
| Metadata ETag | Exact stored metadata own Version strong tag; missing428/stale412 | Shared tag parser/post-commit carrier/NSwag header support; absence of computed reference impact from own representation; token update/no-op semantics |
| Binary HTTP | GET/HEAD CM-owned YARP media gateway, не ApiResponse<byte[]>/MediaDownloadDto/Application OpenRead pump | Content headers/status/errors, range/conditional/cache Q21, client/forwarder ownership/disposal/non-JSON error handling; gateway contract отдельно от NSwag metadata API; preview безопасного media type не trust client MIME |
| Impact | CanDelete/full Draft/Published/historical facts и blockers; authoritative recheck в Delete | Count numeric type/unit (occurrences/versions/owners), overlap/dedup, blocker address/paging/scope, incomplete/unavailable authority error; нет exposure full content/history secrets |
| Delete result / recovery | Own If-Match обязателен; окончательный success после object+metadata deletion | HTTP success code/result shape, промежуточный Deleting visible/readable, resume binding/new ETag after BeginDelete Q21; stale old token/404 после lost response не присваивать silently idempotent success |
| Archive/Restore | MVP полный confirmed AssetDto/new own ETag; old bytes retained | HTTP success code и repeated/same-state transition/code/token increment; exact Archived read/UI, availability for existing Draft references Q12 |
| Access | Demo anonymous; MVP authoring access и runtime Delivery membership разные | Roles/permission names/session/revision context/route exposure/error403vs404 F16/Q10/F13; gateway/optional cache/304/range не обходят current entitlement/lifecycle через знание AssetId |
| Errors | asset.not-available/asset.in-use,404/428/412 vs409; upload/storage failure не Available | Machine codes/ProblemDetails upload field path,400/413, nonavailable lifecycle, missing storage object, unknown storage outcome/DB commit и incomplete reference authority. Current asset.unavailable404 не универсальный target |
| Unknown upload outcome | Same operation ID/input восстанавливает durable original attempt/AssetId/result; новый blind upload не разрешён | Exact carrier/result/stage recovery signatures; после storage success + DB failure verify size/hash → Available commit либо cleanup собственного attempt Q21 |
| Events/DI verification | Current AvailableDomainEvent есть; command orchestration target в Application | Нужный local consumer и durable dispatch timestamp/rollback, physical new Application composition; broker/outbox не вводятся по имени event. NSwag regen/header/binary/multipart integration после OpenAPI |

**Ответ для Demo:** выбранный wire contract приведён ниже и реализован F05. Protected MVP access/Archive/Restore остаются вне Demo; SDK deployment/streaming вопросы остаются Q21/F19.


### Выбранный Demo wire contract

- Multipart: один `file`, `kind` (Image/Audio/Video) и непустой Guid `operationId`. Нормализуются basename/trim filename и lowercase MIME; actual length/SHA-256 входят в fingerprint. Повтор того же OperationId с иными исходными данными — 409. Completed upload возвращает исходный AssetDto и исходный own ETag; Pending восстанавливается только после сверки реального объекта.
- Лимиты обязательны в `MediaUpload:ImageMaxBytes/AudioMaxBytes/VideoMaxBytes`; стартовые значения 20 MiB / 100 MiB / 1 GiB утверждены. Нет unlimited fallback; HTTP envelope следует максимуму настроенных caps плюс 1 MiB, RAM buffering 64 KiB, остальное временный spool.
- DTO exact/upload: Id, FileName, Kind, Status, ContentType, Format, SizeBytes, Sha256Hash, RegisteredAt, UploadedAt, ContentUrl. Не содержит ObjectKey, Version и вычисляемый usage. Pending/Failed/Deleting допускают nullable неподтверждённые факты. List добавляет current UsageCount (long?, null при incomplete authority) и UsageIsComplete.
- Query: search (FileName ordinal ignore case), kind, format, uploadedFrom/uploadedTo (inclusive), used, schemaId, status; skipCount=0, maxResultCount=50 (1–200). Status по умолчанию Available; диагностический All включает остальные состояния. Сортировка FileName ordinal ignore case, затем Id; все фильтры до count/page.
- Current usage означает Draft/current Published leaf occurrences, без orphan/history; schema filter использует те же current references. Delete-impact считает все retained references, включая orphan/history, по адресу version/path/orphan. Неизвестная legacy Publishing/Delivery authority даёт incomplete blocker, а не count0.
- DELETE body: Guid OperationId и показанный ConsentToken; обязательный own If-Match. После окончательного object+metadata удаления 200 ApiResponse<Guid>. Same completed attempt повторяет исходный результат. Deleting возобновляется только с original OperationId/ConsentToken и текущим own ETag. Иная операция — 409; старый own tag — 412, отсутствующий — 428.
- Binary: attachment по `?download=true` только на CM response (upstream query не передаётся); только GET/HEAD `/storage/assets/{assetId}`; SHA-256 ETag отдельно от numeric metadata ETag, Last-Modified, configurable cache max-age (24h default), immutable, 304/206/416/If-Range. Gate читает authoritative lifecycle до conditional/forwarding; provider topology/credentials не входят в response.

Archive/Restore и защищённый MVP access остаются за пределами Demo. Физический [план реализации](../f05-demo-implementation-plan.md) описывает границы слоёв и проверок.


## F06: детализация read-контракта

Связано с [backend F06](../features/F06-product-catalog.md), [frontend F06](../../frontend/features/F06-product-catalog.md), [Q23](Q23-source-synchronization.md#f06-проекция-pending-и-конкурентная-обработка), [frontend Q13](../../frontend/questions/Q13-ui-scope.md), [Q10](../../../business/questions/Q10-permissions-and-isolation.md), [Q27](../../../business/questions/Q27-catalog-archiving.md#f06-источник-lifecycle-и-допустимость-нового-использования).

### Подтверждённая исходная точка и целевая поверхность

Текущие [ProductController](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Api/Product/Controllers/ProductController.cs)/[ProductVariantController](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Api/ProductVariant/Controllers/ProductVariantController.cs) дают list/lookup без exact-ID reads. [CatalogQueryHandler](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.EntityFramework.Persistence/Catalog/CatalogQueryHandler.cs) читает local EF projection: Search ILike, IncludeDeleted, optional Variant ProductId, Id ordering, offset и page clamp. Current DTO содержит Name/Description/SourceRevision/IsDeleted; Variant — ProductId. Это не normalized lifecycle/eligibility/pending/F15 contract.

В UI [generated ICatalogClient](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs) имеет ProductsAsync/VariantsAsync с `/api/v1/catalog/...`, list parameters skip/take/search/includeDeleted/productId; нет exact Product/Variant методов DF04/DF06. В нём также присутствует BootstrapAsync. Generated contract требует сверки с актуальным backend/OpenAPI: типизированный клиент сам по себе не доказывает соответствие последнему target. Bootstrap не переносится в F06.

Согласованная target F06 поверхность: GET `/api/v1/products`, GET `/api/v1/products/{productId}`, GET `/api/v1/product-variants`, GET `/api/v1/product-variants/{productVariantId}`. Exact Variant читает stable VariantId без обязательного parent route. HTTP read-only не требует If-Match/ETag; F15 mutation metadata остаётся в F15. Browser routes `/products/...` приняты Q13, не выводятся из API prefix и не становятся неизвестными из-за отставания current code.

### Открытая детализация

| Область | Подтверждённая граница | Что уточняется |
| --- | --- | --- |
| DTO shape | Source stable IDs, V2 Name Product1–120/Variant1–200 и optional Description≤500, Active/Archived и processing separately; no root Version | Exact read CLR/property names/null shape/enum wire/eligibility codes; no second source DTO; summary conflicts/counts только по нуждам экрана |
| Initial pending | Durable Variant без parent существует; нет fake applied state или ложного 404 | Representation AppliedState absent, pending parent/name/version отдельно; доступ к pending-only identity из F15/diagnostics, entry point при отсутствии canonical applied parent |
| Applied vs source | Receiver ACK/accepted watermark≠applied; old AppliedState intact, newest target/readiness/assessment и ingestion fault separately | Read carrier last applied/latest accepted/fault version/stage/assessment basis/source vs processing times, permissions/exact conflict links; sourceVersion not ETag, old Issues not fresh new-target evidence |
| Lists / selectors | Server search/filter/page; ProductId означает applied parent; recipient type задан владельцем формы | Separate diagnostic pending listing vs new-use candidate filters; как вернуть selected inactive/conflicted historical ID; какие lifecycle/availability filters и допустим ли list без ProductId |
| Query profile | Whitelist sort, stable Id tie-breaker, offset envelope S16 | Exact query names/offset DTO mapping, defaults/limits, search semantics/case/normalization, sort keys; count/page consistency при concurrent snapshots; no full catalog browser preload |
| Access / errors | MVP only, source CRUD отсутствует; pending GET читаем, absent identity404 | Concrete permissions/isolation Q10, error machine codes/ValidationProblemDetails, malformed Guid routing, auth metadata/OpenAPI; shared catalog vs organization-specific eligibility |
| API / NSwag | Четыре target GET из F06; query handlers Persistence | Actual route constants/OpenAPI alignment, list vs lookup/prefix current surface migration, generated names/signatures и response metadata; не создавать handwritten client или guessed endpoint для компенсации |
| Read freshness | Local projection eventual; dual receive/work and scoped technical fault selected Q23; GET doesn't sync source | Safe diagnostic field codecs/source vs received/processed times и manual refresh composition; not bootstrap/sync UI, no polling interval/realtime guessed |

**Ответ:** Product/Variant V2 shape/limits, dual-stage processing, single newest pending, fault/supersede/readiness и operational reconciliation выбраны [Q23](Q23-source-synchronization.md#принятый-протокол-каталога-v2). Read models distinguish Accepted/Applied/WaitingParent/assessment/technical ingestion fault; fault не human-resolvable F15 и не missing/empty entity. Exact HTTP field/error/reference/query/NSwag literals остаются engineering четырёх GET и public ports. No root Version/source mutation/CM reconciliation POST.

## F07: read-write и reference контракт

Связано с [backend F07](../features/F07-product-content-assignment.md), [frontend F07](../../frontend/features/F07-product-content-assignment.md), [Q07](../../../business/questions/Q07-exclusions-and-local-content.md#f07-допустимость-рабочих-references), [Q20](Q20-persistence-and-migration.md#f07-initialization-и-конкурентные-guards). Exact assignment GET/PUT routes, whole-set IDs-only request и own ETag уже описаны F07; здесь уточняется детализация, а не отменяется target из-за current code.

### Подтверждённая исходная точка F07 на 04.10.2026

- [PublishingController](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Api/PublishingController.cs) даёт GET `/api/v1/publishing/products/{id}` и PUT `/api/v1/publishing/products/{id}/assignments`, body SaveAssignmentsRequest. Separate target content-assignment resource/If-Match headers там не реализованы.
- [PublicationModels](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Application.Abstractions/PublicationModels.cs) содержит body `ExpectedRevision: long`, Items с version ID и SortOrder, optional Exclusions; workspace response содержит Revision и полные release/revision/policy/history groups. Target request содержит только exact EntityInstanceVersionIds, response не публикует technical Version и относится к own assignment.
- [PublishingService.SaveAssignmentsAsync](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.cs) Touch-ит ProductPublicationState.Revision, по одному ResolveItem читает ContentModeling DbContext/roots, проверяет Published/hash/canonical content/schema state/assets, увеличивает revisions logical instance/schema и затем заменяет items. Stable uniqueness проверяется по string Keys. После CompleteAsync вызывается GetWorkspaceAsync: это новый read после commit, а не committed assignment DTO carrier.
- UI [PublicationWorkspacePage](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor) использует current Publishing client/workspace and manual rows. [Generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs) отражает current body Revision и envelope, не target own token/result signature. Конкретный header extension/NSwag configuration сверены во frontend F07, не считаются готовыми по имени generated method.

### Требуемая детализация

| Область | Подтверждённая граница | Что уточняется |
| --- | --- | --- |
| Write/read model | Whole set exact version IDs; derived stable instance/schema IDs; own root ETag | Exact CLR collection/property names, null/empty/duplicate behavior, maximum count, display/diagnostics fields и их nullability/fallback |
| Reference metadata | No Key matching; no latest; chosen version/schema target exact | Current names vs stored display metadata; PublishedVersionNumber read source; нужны ли saved hash/dependencies/audit fields; ручной SortOrder исключён из target, canonical representation order принадлежит F10/Q19 |
| Availability | Unchanged exact unavailable refs may remain on Save with ReleaseBlocker; new position/version replacement eligible; limited inactive removal/eligible replacement; F15 no bypass | Exact retained/new-use resolver context/reason DTO/nullability, schema/media scope and outage mapping; display reorder not newuse |
| Public ports | Batch authoritative resolve, no foreign DbContext/root writes | Signature/ownership of facts/cancellation/guard context, semantics returned partial/missing IDs, dependency issue classification |
| ETag/result | Own strong numeric token/If-Match; PUT200 saved DTO+header without result GET; SaveBlockers/ReleaseBlockers/Warnings and derived basis separate own token | Internal result/header carrier/parser, post-disposal race/error priority, concrete diagnostic basis/address/codes; saved != release-ready |
| HTTP/OpenAPI | GET/PUT `/api/v1/products/{productId}/content-assignment` target; no per-item/source CRUD/publish-on-save | Actual route/controller migration от `/publishing/...`, ProducesResponseType и header metadata, machine codes/reference addressing, permissions/isolation Q10, NSwag typed header result/CORS exposure |
| Read consistency | Own items/token одного saved assignment; labels не меняют reference ID | Query snapshot/materialization protocol; external display freshness; metadata capture для successful mutation result, без whole workspace post-commit GET |
| No-op/unknown outcome | State-changing mutation atomically advances own Version; old tag never grants blind overwrite | Identical body/current tag response/audit policy Q20; нужен ли operation idempotency/correlation, recovery exact read/intent compare и UX, который не обещает авторство по равенству набора |
| Picker | Reuse F04 logical and exact version list; explicit selection, Published alone не enough | Actual filters/query/generated methods для multi-target Published candidates и eligibility reasons, old selected unavailable reference; no new all-versions endpoint invented |

**Ответ:** Save/reference/maintenance/diagnostic semantics согласованы Q07. Exact wire/reason/header/basis/permission/concurrency protocol — инженерная детализация. Текущий общий workspace не переносится целиком; saved DTO/own ETag не запускают release/revision/Delivery и не обещают release readiness.

## F08: patch, preview и parent route

Связано с [backend F08](../features/F08-variant-content-assignment.md), [frontend F08](../../frontend/features/F08-variant-content-assignment.md), [Q07](../../../business/questions/Q07-exclusions-and-local-content.md#f08-сохранение-patch-и-проверка-live-базы), [Q20](Q20-persistence-and-migration.md#f08-patch-concurrency-и-preview-consistency). Stable four-group patch, exact result reference и контекстный assignment GET/PUT приняты; уточняется их техническое представление.

### Подтверждённая исходная точка F08 на 04.10.2026

- [ProductVariantContentAssignment](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Assignments/ProductVariantContentAssignment.cs) имеет Items/Exclusions и Key-based Assign/Remove. Explicit target A отсутствует; Assign удаляет совпавший instance exclusion, ExcludeInheritedInstance удаляет local item, schema exclusion удаляет прежние schema/instance rules. Это расходится с coexistence/stable intent целевой F08.
- [ContentExclusion](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Assignments/ValueObjects/ContentExclusion.cs) хранит schema/instance IDs вместе с Keys, Matches работает по Keys. Target rule matching должен использовать stable IDs и inherited слой, не изменять local decisions.
- [PublicationModels](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Application.Abstractions/PublicationModels.cs) содержит SaveAssignmentsRequest с long ExpectedRevision, Items(version ID/SortOrder) и общий Exclusions schema/nullable instance input. Four-group/explicit target/result contract отсутствует. [PublishingController](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Api/PublishingController.cs) использует GET `/api/v1/publishing/variants/{id}` и PUT `/api/v1/publishing/variants/{id}/assignments`, без target parent route/own If-Match.
- [PublishingService](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.cs) Save touches Variant publication Revision, resolves result versions по одной через ContentModeling DbContext/foreign root revision writes, заменяет local Items/Exclusions и после commit вызывает GetWorkspaceAsync. Current workspace Variant assignment read показывает local items, не typed inherited/local/override/excluded working preview.
- [PublishingService.Releases](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.Releases.cs) BuildRevision читает выбранные immutable Product/Variant releases. [ProductVariantPublicationRevision.Create](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Publications/ProductVariantPublicationRevision.cs) использует Dictionary по string Keys и local overwrite. Это source release preview/composition, не F08 live Product assignment preview и не explicit A→B collision-checking core.
- Current UI [PublicationWorkspacePage](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor) и [generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs) отражают этот общий workspace/Revision contract. Точные факты picker/base/Save и отсутствие target/header-result support приведены frontend F08. Наличие старого preview route не выбирает transport для несохранённого working candidate.

### Что требуется уточнить

| Область | Подтверждённая граница | Открытая детализация |
| --- | --- | --- |
| Patch wire | Four explicit collections; override required target/result version IDs; server derives target SchemaId/result reference | Exact generated CLR/property names, required/null/empty/duplicates/issues; retained target evidence при обновлении result; no ContentSlot/nullable target ambiguity |
| Read/preview DTO | Saved intent отдельно derived preview; live collision allowed Save/ReleaseBlocker только captured basis, не global F09block; intrinsic errors SaveBlocker/Warn allowed | Exact categorized issue/path/participant/context-basis DTO, target absent-live vs missing, metadata/count/order; F09othercontext evaluates independently; readfailure notemptyvalid |
| Candidate evaluation | Unsaved intent не confirmed saved state; authoritative preview pure, no writes | Нужен ли external candidate request, его method/transport/URL/access/DTO/result/errors/stamp/cancellation; либо выбранный UX без подтверждённого candidate preview. Не считать F10 release preview заменой и не выдумывать POST |
| ETag scope | Own int Version/strong numeric token patch; F07 base меняет preview без patch mutation | Representation boundary/header semantics/caching при body с changing derived preview; composite input freshness mechanism без fake own Version/base token в DTO; input stamps не mandatory base If-Match без принятого profile |
| Parent route | GET/PUT `/api/v1/products/{productId}/variants/{variantId}/content-assignment`; context проверяется по applied parent | Parent mismatch status/machine code/read canonical guidance; initial pending Variant without parent и setup readiness; stale PUT не redirect/resubmit automatic |
| Successful result | DTO+own committed token без new GetWorkspace; historical immutable unchanged | Captured preview input version/context, freshness after later base/move, labels captured vs derived; lost response/explicit comparison/reload, idempotency needs |
| Availability/targets/rules | NEWoverride target current inherited only; retained absent target preserves B; unchanged unavailable retainedSave, new result eligible; original-layer noncascading/order-independent rules/cycles allowed if unique | Batch trusted target/schema/evidence/retained-vs-new use facts and guards; maintenance semantic removal/replacement incl exclusions/overrides, no unrelated expansion/F15 bypass |
| OpenAPI/NSwag/access | Accepted own GET/PUT, no technical Version body; public F04 picker reuse | Реальные controllers/routes/ProducesResponseType/header result/CORS/errors/permissions Q10; F04 filter/profile; не ручной generated client/поддельная browser authority |

**Ответ:** Save/new-intent/original-layer/cycle/maintenance и diagnostics policies согласованы Q07; общий MVP guard protocol выбран Q20. Exact candidate-preview/read basis/DTO/header/error/adapter signature материализуется инженерно; missing target/base change не разрешают automatic rewrite patch.

## F09: release contracts и semantic review

Связано с [backend F09](../features/F09-content-releases.md), [frontend F09](../../frontend/features/F09-content-releases.md), [Q05](../../../business/questions/Q05-initial-publication.md#f09-пустой-snapshot-и-повторное-создание), [Q07](../../../business/questions/Q07-exclusions-and-local-content.md#f09-live-collision-и-release-validation), [Q20](Q20-persistence-and-migration.md#f09-snapshot-creation-и-lifecycle-races). Прежние target F09 source-assignment action/list/exact/lifecycle routes сохранены; наличие legacy /publishing API их не подтверждает.

### Подтверждённая исходная точка F09 на 04.10.2026

- [ProductContentRelease](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Releases/ProductContentRelease.cs) / [Variant release](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Releases/ProductVariantContentRelease.cs) используют ReleaseNumber, Draft→Published→Retired, ManifestHash/PublishedAt, Items и Variant Exclusions. Есть Retire, нет Restore/ChangeComment/provenance/own Version/four-group explicit override.
- [PublicationModels](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Application.Abstractions/PublicationModels.cs) содержит PublishReleaseRequest(long ExpectedRevision, UpdateMode), ReleaseDto(Id,Number,PublishedAt,Items,Exclusions), PublishReleaseResultDto(Release,Updates); release list входит общий workspace. [Controller](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Api/PublishingController.cs) POST `/api/v1/publishing/products|variants/{id}/releases` возвращает200. Accepted source-assignment If-Match/exact release read/Retire/Restore surface здесь отсутствует.
- [PublishingService.Releases.PublishReleaseAsync](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.Releases.cs) touches publication state Revision, synthesizes empty source when assignment absent, resolves Items через foreign context, Max(Number)+1, publishes snapshot. Product path автоматически ActivateRelease + activation history и optionally creates/processes promotions. Target F09 отделяет Create от F11; current variant snapshot не explicit four-group patch.
- [PublishingService.GetWorkspaceAsync/ReleaseDto](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.cs) читает только Published releases и сортирует ReleaseNumber descending, maps Number/Items/Exclusions без lifecycle/audit/comment и own token. Это не принятый list/default/history contract.
- [ReleaseSnapshotMapper](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Application/ReleaseSnapshotMapper.cs) отображает legacy snapshots/IsPublished. [ReleaseTests](../../../../contentmanager.api/tests/BoundedContexts/Publishing/ContentManager.Publishing.Tests.Unit/ReleaseTests.cs) подтверждают source snapshot independence и legacy lifecycle; наличие теста не доказывает target HTTP/Restore/atomic source precondition.
- UI [PublicationWorkspacePage](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor) вызывает ReleasesAsync/Releases2Async с ExpectedRevision/UpdateMode, затем Load workspace. [Generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs) отражает те же legacy routes/DTO, не даёт caller typed success ETag/per-call If-Match. Current PublicationDiffRow comparison относится к revision/active composition F10, не saved assignment→previous release F09. Детали frontend F09.

### Детали contracts, которые требуется выбрать

| Область | Подтверждённая граница | Уточняется |
| --- | --- | --- |
| Create request/result | Source assignment If-Match/saved source, immutable artifact/source unchanged. Product: nullable ChangeComment; Variant: explicit ValidationContext ExactProductRelease or EmptyBase + review basis/comment | Exact union/member casing/review-basis carrier/headers and success DTO/Location/own ETag; absent context not empty/defaultactive/live; no UpdateMode/Title/client Items/Version |
| Content/metadata DTO | Product refs / Variant four groups incl target schema/result, optional provenance, Available/Retired/created/comment | Generated names/typed actor/nullability/historical names/Keys/hash/order/availability diagnostics, provenance labels after move; no invented mutable parent or ReleaseNumber |
| Comment/audit/lifecycle | Comment fixed on Create; trusted actor/time; Retire/Restore metadata only | Comment length/plain-text normalization/null handling, actor integration Q10, Restore audit retention, request body, repeated-status no-op/error/Version/result profile |
| List/exact/history | Accepted nested lists and independent exact release IDs; Retired history readable; list not ETag source | Paging/default/order/tie-break/filter/include-Retired/count/summary, context resolution after source move/inactive, exact UI route Q13; no copy legacy Published-only Number order |
| Semantic review | Captured latest-created same-owner predecessor incl Retired, nullable first baseline; exact validation context/collision | Exact review DTO/basis carrier/confirmation/errors; later new release alone does not invalidate captured predecessor |
| Guards/errors | Create revalidates reviewed saved patch/context/current necessary source-result eligibility; retained unavailable result blocksRelease; own source412/missing428/domain409 | Exact codes/priority/addresses/permissions, review basis mismatch mapping, coherent parent/trusted target/context guards, transient failure no false readiness |
| Recovery/transport | Mandatory ClientOperationId, confirmed original receipt lookup before new checks, intentional repeated artifact allowed | Codec/inquiry/status/CORS/header result/OpenAPI/NSwag profile; source tag/hash not dedup, no blind retry |

**Ответ:** Explicit context/no fallback/selected collision blocker/provenance-only reuse и diagnostic categories согласованы Q07. Exact request/review carrier/DTO/headers/status/concurrency implementation остаются engineering. F10 revalidates any different base fully; current activation-on-create не requirement F09.

## F10: revision contracts и preview

Связано с [backend F10](../features/F10-publication-revisions.md), [frontend F10](../../frontend/features/F10-publication-revisions.md), [Q19](Q19-content-hashes.md#f10-materialized-representation-и-hash), [Q20](Q20-persistence-and-migration.md#f10-composition-capture-и-withdrawal), [Q17](Q17-publication-availability.md#f10-withdrawal-и-historical-distribution), [Q05](../../../business/questions/Q05-initial-publication.md#f10-повторное-создание-revision), [Q09](../../../business/questions/Q09-editing-and-deletion.md#f10-зависимости-исключённого-слоя).

### Подтверждённая исходная точка F10 на 04.10.2026

- [Domain revision](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Publications/ProductVariantPublicationRevision.cs) stores Product/Variant IDs, source release IDs, ItemsJson/ManifestJson/ManifestHash/CreatedAt и IsRevoked/RevokedAt/RevocationReason. Create uses string Key dictionary overwrite, legacy exclusions/local items; no explicit target/four groups/collision error/finalnonempty guard. Revoke only bool flag, repeat no-op; Available/Retired/Withdrawn/Restore/own Version/comment absent.
- [PublicationModels](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Application.Abstractions/PublicationModels.cs) has PrepareRevisionRequest(long ExpectedRevision, ProductReleaseId?, VariantReleaseId?) and PublicationRevisionDto(IDs,ManifestHash,CreatedAt,IsRevoked,Items). [Controller](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Api/PublishingController.cs) exposes POST `/api/v1/publishing/variants/{id}/preview|revisions` and `/publishing/revisions/{id}/revoke`; not contextual target routes/exact revision read/Retire/Restore/own lifecycle If-Match.
- [PublishingService.Releases.BuildRevision/PrepareRevisionAsync](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.Releases.cs) validates Published owners, resolves content/assets through foreign DbContexts, compares common VariantState.Revision even preview, instantiates revision for both flows; persist=true touches state and inserts. Revoke touches state token, mutates Delivery PublishedComposition directly, clears matching active pointer/history and returns GetWorkspace after commit. Target pure preview/own revision lifecycle/async ban are different.
- [GetWorkspaceAsync](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.cs) loads revision history with same workspace assignments/releases/policy/current state; no distinct per-row token/exact lifecycle DTO. [UI page](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor) substitutes null chosen releases by active IDs, sends workspace ExpectedRevision, compares preview to active revision, uses Revoke with workspace token and then Load. [Generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs) exposes the legacy contracts, not target per-call own If-Match/typed ETag. Detailed frontend source evidence in F10.

### Открытая детализация

| Область | Подтверждённая граница | Уточняется |
| --- | --- | --- |
| Preview/create | Exact independently nullable sources, ≥1; same full validation; Create repeats, no client Items/hash/preview authority | Separate request names/null/omitted semantics, preview IsValid/issues/candidate/source/envelope/HTTP400/404/409 vs typedinvalid200, success status/result+own ETag/Location, no permanent preview root ID |
| Read/list/models | Stored materialized Items/metadata/hash/source IDs/lifecycle/audit, no historical recomposition | SourceKind/target/captured product metadata/display/profile/assets/hash shape, actor integration/nullability, chosen filter/paging/order/tie/count/include-Retired/Withdrawn summary, admin exact vs public distribution read |
| Context/source picker | Applied F06 parent, F09 exact releases; effective new-use closure Q09 and broader retained evidence | Canonical source/readiness/restriction DTOs and reasons, no latest/active fallback; closure business policy closed |
| Lifecycle | Own numeric revision ETag, required If-Match, source/current-state/hash not token; terminal Withdrawn | Action body, comment/reason constraints/audit Restore, repeated action/no-op/error priority/status/header carrier, active-state/propagation summary result without second mandatory state header |
| Availability/history | Retire preserves old content; Withdraw bans frozen/server distribution, admin history distinct | Permission/machinecodes/issue paths/severity, propagated/readiness indicators Q17, in-flight request/session/data-stream errors; no fake admin missing or immediate Delivery-success claim |
| Recovery/NSwag | Mandatory ClientOperationId original receipt; intentional new RevisionId allowed, no hash/Approval reuse | Exact result metadata/codec/inquiry/CORS/NSwag generation; original source/body preserved on unknown outcome |
| Moderation boundary | F17 exact revision target; Create not approved/active; withdrawal iteration differs terminal root | Public F17 status/link/actions/lifetime and manual submission profile, automatic operation handled F11, not source-supposed mandatory create side effect |

**Ответ:** Уточняется. Source content immutability объясняет отсутствие выбранного source If-Match, но не снимает проверку mutable lifecycle/ownership/dependency facts. Current ExpectedRevision не принимается как target Version body.

## F11: publication state и automatic promotion

Связано с [backend F11](../features/F11-publication-lifecycle.md), [frontend F11](../../frontend/features/F11-publication-lifecycle.md), [Q18](Q18-activation-history.md), [Q20](Q20-persistence-and-migration.md#f11-state-transitions-и-promotion-coordination), [Q17](Q17-publication-availability.md#f11-publishing-active-и-delivery-effective), [Q06](../../../business/questions/Q06-automatic-promotion.md), [Q28](../../../business/questions/Q28-content-review-scope.md).

Current [PublishingController](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Api/PublishingController.cs) / [PublicationModels](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Application.Abstractions/PublicationModels.cs) expose combined workspace under `/api/v1/publishing`, ExpectedRevision body, publish+base-select UpdateMode, combined Activate/Rollback free-text Reason и release-bound updates/retry. Current [UI](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor) показывает active workspace pointer, но не Delivery effective; это не proof target state API/moderation/readiness. Target F11 разделяет F09 create, base promotion, F10 immutable revision, F17 decision, Publishing active и Delivery effective.

| Область | Принятое требование | Открытый контракт |
| --- | --- | --- |
| State API | Exact mutable Product/Variant own int Version as numeric strong ETag; no Version/ExpectedRevision JSON | Final target publication-state routes/signatures/body/status/access/OpenAPI; initial absent resource GET/tag/creation; malformed-header/no-op/stale priority |
| Base actions | Manual leaves Variant unchanged; captured automatic operation; same-base new intent supersedes by item head | Exact body/URI/ClientOperationId result/Cancel-own-item carrier/access, Clear comments/audit profile; capture/precedence closed Q20 |
| Activation | Same Variant exact Approved Available target; manual/rollback/unpublish own state If-Match | Request shapes/comment reason constraints; target summaries/error codes/issue paths; response metadata own committed generation, no hidden mandatory GET |
| Active/effective | Publishing active and Delivery effective/readiness are different clocks/owners | Separate read route/projection or scoped representation/token; strong ETag cannot cover independently changing Delivery/F17 body using unchanged state Version; applied Sequence/pending/outage indicators |
| History | Append-only Sequence ordering, actor/correlation, null records; no same-active/already-null record | Paging/cursor/order/max/include summaries, historical current-parent route/access after move, audit visibility and missing source metadata |
| Operation | Exact operation ID/per-Variant progress incl AwaitingModeration/Rejected/Blocked/Failed; Activated != effective | Overall status/CompletedAt, response/Location/recovery ID carrier, own operation ETag scope, correlation to moderation/readiness, polling/load limits |
| Retry/recovery | Safe transient retry, fixed successful result not reapplied; blocked subset after correction possible | Public retry endpoint/command/body/access/dedup/input refresh protocol; unknown outcome reconcile, request identity retention; no inherited legacy updates/retry contract |
| NSwag/UI | Per-call If-Match/scoped returned ETag/result, explicit source/base/Approved action/effective status | CORS exposed headers/client metadata/regeneration, stale form handling, canonical parent redirect for reads only, operation recovery without duplicate intent |

**Ответ:** Уточняется. Exact approved-only/no-op/per-Variant moderation/async Delivery правила закрыты. Не считать workspace Revision или current UI active badge готовым state ETag/Delivery proof. Старый Q06 create+activate описывается через отдельные F17 стадийные commits, не общей транзакцией.

## F12: policy contracts и diagnostics

Связано с [backend F12](../features/F12-content-entitlement-policy.md), [frontend F12](../../frontend/features/F12-content-entitlement-policy.md), [Q20](Q20-persistence-and-migration.md#f12-policy-binding-и-effective-history), [Q17](Q17-publication-availability.md#f12-freeze-cutoff-и-effective-time), [Q02](../../../business/questions/Q02-policy-changes.md), [Q10](../../../business/questions/Q10-permissions-and-isolation.md).

Current [PublishingController](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Api/PublishingController.cs) / [PublicationModels](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Application.Abstractions/PublicationModels.cs) expose combined workspace+shared Revision, PUT products/variants policy under `/api/v1/publishing`, nullable Policy body+ExpectedRevision. [SavePolicyAsync/GetWorkspace](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.cs) uses shared publication state token and returns workspace after new read. [UI](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor) saves assignments then policy via shared Save, substitutes Product effective when own Policy=null and rejects Product null. Это не accepted own-state header/clear/history contract.

| Область | Требование | Открытый технический контракт |
| --- | --- | --- |
| Own policy API | Target Product/nested Variant content-policy GET/PUT, null explicitly clears local value; own int Version ETag | Final routes/signatures/body/read-carrier/status/OpenAPI, initial missing state/default tag/first mutation, repeat/no-op/header/error priority |
| Inherited/effective read | Variant→applied Product→system, own configured null distinct | DTO composition/separate read/token scoping: whole composite strong tag не unchanged local Version при parent policy/move change; same-generation committed response, no hidden required GET |
| Access/scope | Specialist policy operation with Q10 equality, no invented exclusive admin policy rights | Concrete permission identifiers/scoped maintenance/F15 behavior/error hiding/canonical read after move; no automatic wrong-parent mutation redirect |
| History | Immutable policy versions including clear markers, server Sequence | Summary vs paged exact history surface/paging/order/actor/comment profile; no arbitrary UI-size threshold or Archive/Delete |
| Mutation/recovery | One own policy action, assignments/publications/bindings untouched | Lost-response reconcile/no-op/repeated-action audit profile, per-call token/result metadata/NSwag/CORS; no shared workspace ExpectedRevision body |
| Entitlement diagnostics | Explainable policy source/version, cutoff/unlimited, selected whole revision/license/period basis, Denied vs Indeterminate | Internal/public DTO/machine strings/F13 HTTP mapping/access/minimal disclosure; diagnostics endpoint/screen only if chosen, no raw key/token |
| Runtime evidence | Delivery-effective selection, old period binding non-retroactive, late Unpublish doesn't cancel Freeze | Source public readers/provenance/complete-history/readiness result, private admission/F13 session outcomes; code current reason strings not final API |

**Ответ:** Уточняется. Two policy values/default/precedence/clear/non-retroactivity и whole Freeze semantics закрыты. Strong ETag чужого root/body Revision не own policy contract, current UI пояснение не implementation proof runtime selection.

## F12: PolicyAgreement integration boundary

Согласование принадлежит authenticated VR School backend integration, не staff policy edit и не Unity activation. Publishing F12 определяет Agreement/request/command/result/public read ports; [Q20](Q20-persistence-and-migration.md#f12-policyagreement-и-подтверждение-периода) фиксирует owners/commit, [Q23](Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow) — durable source workflow. Raw activation key не передаётся для quote, CM staff actor не подставляется вместо source identity. Quote не требует License projection и не подтверждает entitlement.

Request несёт stable RequestId, exact LicenseId/ProductVariantId/SourcePeriodId, исходные planned parameters и expected source context. Response возвращает exact immutable AgreementId, bound scope и frozen policy/provenance. После auth original-request replay comparison идёт до fresh new-request checks; same input возвращает прежний Agreement, conflicting input не меняет его. Fresh context mismatch/pending/ambiguous F15 — NotReady/context conflict. AgreementId в confirmed source period не подменяется, unknown facts — DataNotReady, а не InvalidKey/default.

**Принято:** promise checkpoint — CM Agreement commit, durable exact-plan reservation без MVP autoexpiry; binding уникальна по license/Variant/source period; policy immutable, current commercial facts versioned. Diagnostics различают Agreement/frozen policy, current configured policy, current confirmed cutoff/unlimited/current period/completeness и Denied/Indeterminate/revoke. Никаких raw key/token в diagnostics/history.

**Уточняется инженерный контракт:** exact versioned HTTP/package URI/auth mechanism/scopes и source identity binding; CLR/wire planned parameters/expected context и canonical replay equality; ProblemDetails machine codes/status/retry metadata; public read carrier/version/completeness/quarantine details. Эти поля не объявляются выбранными по аналогии с staff API. Existing policy GET/PUT/history contracts выше ещё требуют собственной финализации. Tests обязаны проверить authenticated identity/scope, same-request recovery, changed input, mismatched context, substituted Agreement и incomplete entitlement set.

## F13: runtime contracts и source evidence

Связано с [backend F13](../features/F13-content-delivery.md), [frontend F13](../../frontend/features/F13-content-delivery.md), [Q33](Q33-sync-resume-transport.md#f13-wire-protocol-и-execution-ownership), [Q16](Q16-activation-and-sessions.md#f13-session-credential-и-revocation), [Q20](Q20-persistence-and-migration.md#f13-projection-session-и-plan-storage), [F12](../features/F12-content-entitlement-policy.md).

Current [InternalDeliveryContracts](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Application/InternalDeliveryContracts.cs) accepts trusted ResolvedLicenseContext(LicenseId), returns SessionId/Variant/revision/hash but no external credential/TTL. [InternalDeliveryService](../../../../contentmanager.api/src/App/ContentManager.Application/Delivery/InternalDeliveryService.cs) reads foreign CatalogAccess/Publishing/ContentModeling, current-pointer guard, live exact payload and Media presigned URL. [Session/composition](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Domain/Publications/PublishedComposition.cs) lacks target token/expiry/status model. Scanned src *.cs excluding generated build/migrations has no SynchronizationPlan/StartSynchronization/text-event-stream/key-validator/SessionTokenHash or target delivery-sessions/content-synchronizations routes. Authoring API/Blazor generated Catalog Synchronize isn't runtime sync/controller/SDK proof.

| Area | Accepted requirement | Contract still open |
| --- | --- | --- |
| Demo routes | Start JSON/status/resume/one data stream/receipts/stop/discard/freshness, no licenses | All Demo routes/models/framing/cursor/Status generation/control/Freshness agreed Q33/F13; literal reason registry/ApiResponse mapping/OpenAPI and actual handlers still require implementation |
| Selection/group shape | Mandatory complete exact schema-version selection, explicitempty deliberate managed removals, per-group errors not failed empty | Required-presence/identity/content/end fields and duplicate/cursor/missing/tombstone rules fixed Q33; implement codecs/validation/error registry |
| Cache/runtime state | SDK ID, immutable exact target, conditional ownership/progression | Plan/status generation/header/receipt/control replay/fencing/lease/recovery agreed Q33; numeric sizing and actual adapter tests remain; own Version/If-Match not substitute |
| Freshness | Separate post-success current comparison, no auto-loop, failed check client Unknown | POST /api/v1/content-synchronizations/freshness, outcome/checkedAt/changedGroups agreed; no partial successful invalid comparison; implement source/error mapping |
| MVP session | Key only new admission, CM server validation, pinned whole entitlement | CmDeliverySession opaque Bearer, absolute TTL, encrypted replay и leases приняты Q16; current implementation не доказана, final endpoint/DTO/error literals и adapters уточняются здесь |
| Snapshot reads | Exact protected manifest/item/asset/schema needs, membership before304/bytes | Canonical manifest/content/formal schema reader representation, schema endpoint vs F21 reuse, envelope vs binary/cache/range profile |
| Media | Target Host YARP gateway via F05; separate sync data/assets | Current presigned URL not target; exact runtime access/cache/range/in-flight behavior Q21/Q16, no storage ObjectKey/secret identity |
| Scope/diagnostics | Distinct invalid key/data-not-ready/no grant/revoke/expired/withdrawn/invalid session/outside membership | Final machine codes/status/permissions/redaction/traces/CORS/SDK transport wiring, no blanket all403; no invented admin delivery page |

**Ответ:** Уточняется. Known application method/direct DB tests don't prove public authentication/wire/streaming/Unity compatibility. Demo accepted behaviors Q30/Q33 and MVP F12 rights remain unchanged; final external protocol must be concrete before NSwag/SDK integration.

## Delivery visibility contracts

Модели определяются один раз в [backend F13](../features/F13-content-delivery.md); выбранные workflow/storage boundaries — [Q20](Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости), cutoff — [Q17](Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery). Logical result carriers нужны для source transition processing outcome, actual effective history, pending/confirmed visibility evidence, exact revision restriction и history completeness read.

Publishing ordered-history public port возвращает exact Variant scope, committed transitions/range watermark и completeness. Broker trigger/EventId не становится истиной catch-up. Delivery carrier различает LastProcessedPublishingSequence, committed effective pointer/order и saved VisibilityConfirmedAt/temporal readiness. Same Variant+Sequence с другим meaningful payload — integrity conflict; duplicate сохраняет original outcome/time. Canonical envelope/input equality codec ещё materialize, нет выбора новой версии package по аналогии.

Для temporal read отдельно представлены complete empty, known null, necessary pending confirmation и incomplete/corrupt/unavailable history. Required unknown — Indeterminate/DataNotReady; known null/no grant/revoke/ban имеют отдельный reason. Same candidate licenses требуют stable selected provenance; final tie algorithm не выбран. Diagnostics показывают Publishing active vs Delivery committed effective, processed range и confirmation state/gap/materialization/terminal restriction; matching IDs не full readiness. Нет выдуманного нового staff route/polling/form.

VisibilityConfirmedAt — persisted primary UTC observation уже committed exact transition, **не** timestamp до original commit и не commit confirmation transaction. It is assigned once; lost ACK reconciles saved identity/result, otherwise fresh observation without backdating. Public read и DTO не предоставляют клиенту ability назначать time/order/ban/readiness. Authority exact ban проверяется перед new admission; protected read restrictions — перед cache/304/Range.

**Уточняется:** final public port signatures/read carriers/enum literals/serialization precision profile и nullable/required wire rules, package/HTTP exposure/access/scoped representation tags/ProblemDetails machine codes; normalized payload comparison/tie codec; public ban read freshness/failure mapping/Host adapter; diagnostic screen data contract. Semantic conservative cutoff/ordered catch-up/once saved time/null and completeness уже закрыты, session token/TTL/revoke/in-flight — Q16. Tests include exact contract fixture/order/replay/unknown/pending reason mapping and committed observation recovery.

Public Application.Abstractions history/visibility carriers содержат immutable nontracking read values, без mutable Domain/EF owners, methods или internal int Version. Persistence mapper проецирует их из own rows; pure Domain evaluator получает mapped owner-domain value input и не зависит от Application.Abstractions. Staff wire projection имеет отдельный metadata/access profile, а не serialization aggregate с concurrency token.

## Delivery session credential и replay contracts

[Q16](Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases) выбирает separate runtime CmDeliverySession scheme, 32-byte CSPRNG Base64Url opaque Bearer/HTTPS/header, SHA-256 verifier-only Session, scoped current pinned authorization и short replay capsule exception. [Backend F13](../features/F13-content-delivery.md) определяет модели/поля/handlers/layers/DF один раз; [F12](../features/F12-content-entitlement-policy.md) — pure EvaluatePinnedRevision. Public immutable read values не Domain/EF roots/internal Version/secret material; staff scheme не принимает runtime identity.

Creation transport logical input = CreationRequestId, requested Variant, ephemeral key(s) и выбранный caller context. Client не задаёт derived LicenseId/verified credential/security epoch/grants/revision/TTL/lease duration. Server после VRS validation normalizes stable credential/license identities для immutable original meaning; volatile fresh source epochs/timestamps не caller input fingerprint. Same confirmed request returns original Session/pin/token/ExpiresAt while currently authorized; other actor не читает receipt, changed meaning conflict. No reselect current candidate during replay.

Session secret replay available5min capped expiry via encrypted capsule, not retrieved from hash. Allocate identity/token+Protect bound only credential before gate, atomic Session/verifier/original result/receipt/capsule commit before response. Same shared persistent keyring/application discriminator/versioned purpose + explicit at-rest encryption across replicas required. Decrypted token must match stored verifier and exact scope. Creation/replay replies no public cache; raw key/token/hash/cipher/log body excluded.

Temporary DB/keyring unknown -> same ID retryable failure. Proven replay expiry/irrecoverable capsule -> explicit NoReplay/new RequestId/new admission, no implicit credential rotation/session restore/closing all license sessions. Original confirmed metadata remains; retention cannot erase idempotency then pretend original operation absent. Wire representation for original receipt/current unusable Session separately states outcome vs present permission.

Source validation contract: complete credential proof set, stable ID/bound license/CM validity, credential security epoch/deadline and required license watermark, not grants. **CM monotonic freshness** includes whole attempt before VRS through final guarded check; source ValidatedAt audit only. [Source profile Q23](Q23-source-synchronization.md#credential-validation-и-security-epoch). Lease checkpoint before protected cache/304/Range binds resolved Session, resource membership/pin/current rights and exact ban; lease deadline/cancellation/renewal loss are transport facts, never JSON payload authorizing access by hash.

Accepted pre-header semantic status groups: 401 bad/expired session credential, 403 confirmed right denial, 409 changed basis/new admission, 503 validation unavailable/needed unknown, 404 outside authorized snapshot scope. Different source credential errors/unusable replay/time/order problems need stable reason classes; valid key+missing source projection stays DataNotReady. After headers invalidation/abort follows selected framing; truncated transfer by itself means SDK Unknown, not proved revoke. SessionId in URL is selector only; token not URL/query/S3-forwarded.

**Уточняется final wire:** exact source/session/create/replay/read/check/close endpoint surfaces/signatures/RequestId carrier; token canonical parse/hash/constant-time lookup fixtures/profile names, response metadata/capsule/status/reason enum literals/WWW-Authenticate/Retry-After/access/error precedence, normalized caller-context equality and credential collection duplicate policy, JSON secret input logging exclusions, SDK credential-provider/result bindings and keyring deployment. Crypto/TTL/epoch/replay/pin/lease mechanisms и defaults уже закрыты; no new admin forms/control channel/refresh endpoint. Golden request/response/error fixtures and real HTTP tests required before generated-client integration.

## F15: conflict API и read contracts

[Backend F15](../features/F15-catalog-conflicts.md) / [Frontend F15](../../frontend/features/F15-catalog-conflicts.md) описывают queue/history/exact core/resolve. Прежняя target поверхность: GET /api/v1/catalog-conflicts, GET /api/v1/catalog-conflicts/{conflictId}, POST /api/v1/catalog-conflicts/{conflictId}/resolve. Текущий source search не подтвердил их реализацию, CatalogConflict model и generated conflict operations; runtime/API тесты в этом проходе не запускались.

Core exact DTO и strong own numeric ETag принадлежат сохранённому состоянию конфликта: Applied/Pending snapshots, captured issue evidence/as-of, status/resolution metadata. Live impact/current labels/eligibility Publishing меняются независимо; их нельзя помечать тем же root ETag как всю representation. В F15 предусмотрен отдельный logical impact read без собственного conflict ETag; точная публичная exposure/DTO/external route пока уточняется. GET не обновляет root diagnostics/status.

Resolve не получает ProductId или local correction payload; использует stored authoritative newest PendingState, required If-Match, internal expected int Version. DTO Version не содержит. Own stale412/missing428 отличаются от remaining blockers/independent changed basis409. Успех — подтверждённый local apply + Resolved history, body core DTO/new ETag.

**Принято:** отдельная corrective repair command принадлежит Publishing и изменяет только working Variant patch в пределах [Q25](../../../business/questions/Q25-catalog-conflicts.md#f15-технические-границы-разрешения). Ordinary Save не получает исключения или `ignoreConflict`. Repair input логически содержит `ConflictId`, newest `SourceVersion`, captured repair basis и proposed patch; обязательный own patch `If-Match` не заменяется conflict ETag. Source/patch change до commit отвергает старую попытку. Parent берётся из authoritative pending, альтернативный `ProductId` request не принимается. Preview раздельно показывает applied, prospective newest-parent и proposed repaired composition. Repair outcome подтверждает только patch commit, затем отдельно выполняется Resolve; success DTO не изображает применённый move.

Resolve re-reads newest source/dependencies, не выполняет hidden Publishing mutation и не auto-publishes. Если после repair-v21 принят v22, repair сохраняется, старый Resolve basis отвергается; missing newest parent даёт waiting/read reason без stale apply. Repair/Resolve доступны authorized specialist с fresh roles/scope/preconditions и раздельным F18 audit. Unpublish/Withdrawal сохраняют safety actions при Archive согласно Q27, а обычные activation/rollback/new releases/revisions/promotion ограничены applied own/parent lifecycle.

**Уточняется:** nested snapshot/issue/affected-scope read shapes, event/actor identity representations, final response metadata carrier, list search/filter/status/paging/sort names/default/limits, permission names в модели F16/Q10, dependency-unavailable status/codes, already-Resolved/repeated resolve outcome, live impact exposure и composite freshness profile. Для repair ещё выбираются exact CLR command/preview DTO, URI/method, basis carrier, response/status/error codes и unknown-outcome recovery; two-step boundary уже выбрана. Bodyless Resolve не скрывает новые изменения Publishing; physical race guard Q20 обязателен.

## F16: staff API и auth контракты

[F16 backend](../features/F16-staff-access.md) и [frontend](../../frontend/features/F16-staff-access.md) определяют роли/current access/recovery. Проверенный src от 05.10.2026 содержит modular Host/API phases и ResultError401/403 mapper, но поиск без bin/obj/Migrations не подтвердил StaffUser/Identity store/auth registration/staff API. ApiWebApplicationExtensions сам auth middleware не добавляет. Это не доказательство external deployment enforcement и не Identity MVP проверка.

Прежняя /auth/login,/auth/logout,/auth/me,/staff-users(+exact/roles/lifecycle) поверхность и URI /api/identity/* из source остаются **candidate**, пока final wire не закреплён здесь. Q24 уже выбрал ASP.NET Core Identity/cookie/Guid/IdentityAccess, required Login/DisplayName без обязательного email, setup/reset и независимый hosting. Data API различает invalid/expired auth401 и forbidden action403, без HTML login как successful JSON; exact challenges/DTO/status/CSRF headers материализуются здесь.

CurrentStaffDto отражает verified identity/fresh assigned roles/optional safe capabilities; literals/enum/metadata policy уточняются. Core StaffUserDto own role/active/profile state даёт numeric strong ETag из own internal int Version. Independently provider-owned profile/security fields нельзя помечать тем же token как весь composite. List не источник per-row ETag; DTO не содержит Version/ExpectedVersion/stamps/hash/proof/token.

**Уточняется:** final routes/scheme literals, sign-in/out/provision/lifecycle/setup/recovery DTO/results/status, role/capability serialization, Login normalization/format/limits, отдельный unselected profile-edit use case, list/filter/page/sort limits/defaults, inactive/unknown/provider-unavailable/duplicate/no-op/unknown-outcome reason codes/messages. Credential action preconditions не выводятся автоматически из access-state ETag. Q24 закрывает absolute8h/logout current Session/setup20min/password15–128/lockout5failed15min; эти defaults и IdentityAccess ownership не переоткрываются здесь. Audit event — F18.

Форма после same-user reauth удерживает original resource baseline/ETag. Fresh resource read не является согласием перезаписать changed object; generic metadata/header infrastructure NSwag должна сохранять это различие. Server current access read на каждом staff request задаёт точку применения role/deactivation commit; URI auth не применяется blanket к runtime Delivery F13.

## IdentityAccess contracts и capabilities

Основание — [Q24](../../frontend/questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы), [source](../../../archive/identity-access-architecture.md), полные модели/handlers/DF один раз [F16](../features/F16-staff-access.md), physical stages [Q20](Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots). IdentityAccess — separate BC, Core Identity provider user/role Guid, server-fixed Specialist/Administrator/RolePermission catalog MVP. Public registration/direct-grant/custom-role/permission-editor/refresh/RememberMe surfaces не вводятся. Source routes/DTO/examples не final OpenAPI.

Application metadata/ICurrentUser/PermissionSnapshot принадлежат Authorization.Abstractions; ASP.NET attributes/ClaimsPrincipal/provider models не входят в business commands/DTO. Explicit requirement/public classification каждой user-facing operation, owner resource rules и Domain invariants сохраняются. Background/source execution не impersonation staff/admin. Permissions computed once per request from coherent fresh committed account+session+memberships+registered grants, no cross-request cache.

Browser topology: **Blazor Standalone UI и API independently hosted на HTTPS-поддоменах общего базового домена**. Это cross-origin, same-site, не общий UI proxy. Host-only API cookie Secure/HttpOnly/SameSite=Lax без shared Domain; UI transport credentials include. API CORS exact configured UI-origin allowlist+credentials/methods/headers, no wildcard/reflection. Allowed preflight не требует cookie, safe metadata exposure выбрано по потребности. Cookie/antiforgery secret не в /me/body/log; antiforgery request token получается API response contract и возвращается header. CORS не CSRF/auth. Login/logout/setup/reset и content unsafe requests проверяют antiforgery; identity switch обновляет token.

/me возвращает verified identity, assigned roles и global permissions; resource DTO имеет AllowedActions, рассчитанные owner capability resolver. Ни одно значение не authorizes mutation; unknown dependencies не guessed Allowed. No internal Version/provider stamp/hash/cipher/token in safe staff/resource DTO. Cookie contains minimal identity/session, not full permission set. Runtime Delivery credential не принимается staff scheme, staff cookie не runtime entitlement.

**Representation boundary:** capabilities персональны и меняются независимо от own resource concurrency. Current numeric core tag может защищать own write basis, но не весь composite JSON после добавления AllowedActions. Exact dedicated concurrency carrier/whole-representation validator следует выбрать до NSwag integration; no shared output cache и no stale304 capabilities по одному core Version. This separation does not add capability claims as commit permit or use newly fetched tag for old edits.

Staff Session absolute8h/no sliding; current Session logout, credential reset/deactivation invalidates prior staff Sessions, Reactivate requires new login. Provider setup/reset code20min one-use exact purpose/user/security generation; completion consumes action/provider password/security state atomically. Secrets display/transmission, lookup of safe confirmed original outcome and conclusive reissue contract требуют explicit wire: lost response не blind duplicate provision/password replay. Bootstrap manual operator action/durable first identity, emergency separate/no public bypass. Login unique/immutable, DisplayName required, email optional; password15–128/no mandatory mixture+weak-password checking, lockout5failed15min configurable; Unicode measurement/normalization/blocklist/rate-limit numbers engineering.

Mutation expected login context — **precondition only**, сравнивается с trusted current staff Session; не client ActorId/role/permission authority. A-bound queued request после cookie switch B отвергается до use case. Context tag/carrier/reason и ordering с permission/resource/If-Match conditions финализируются здесь. Cookie state-change response races/auth coordination должны проверяться реальным browser flow, а не одним local notification.

In-memory same-tab recovery без reload сохраняет original owner/object/version/ETag/base/actual input/File/pending operation context. New same-user session + fresh exact read не заменяет baseline/autosaves/replays unknown command. Changed resource needs owner-specific resolution, different account/explicit logout destroys previous snapshot. Reload/close/browser restart не обещаны.

Semantic results401 invalid/expired auth,403 known access denial,503 dependency unavailable,404 missing resource plus owner concurrency/domain errors различаются. Inactive/revoked/setup-required/error priority и safe generic login failure не выбираются путём превращения outage в bad password. Final literal request/result/errors/headers/routes/CSRF/context/catalog fixtures — обязательный engineering result before UI integration. Protocol/defaults/hosting уже приняты.

## F17: moderation API и review reads

[F17](../features/F17-content-review.md) target list/core/history/Submit/Comment/Withdraw/Approve/Reject отсутствуют в проверенном src от 05.10.2026. PublishingDbContext не имеет iterations/comments; current activation проверяет IsRevoked без Approved gate. Legacy direct Delivery write не доказывает F17/F11 target. Source links находятся в feature, API/runtime tests не запускались.

Own ModerationDto состоит из exact target/status/actors/times/immutable comments/reason; technical Version только strong numeric header. F10 content/context и live dependency/F15/Delivery basis/actor labels читаются отдельно и не помечаются own iteration ETag как весь composite. Exposure GetModerationReviewContextQuery и nested DTOs/status/wire/labels/history/comment paging/header carrier/permissions/list filters/defaults остаются уточнением. Role-specific views не personal SubmittedBy authorization.

Logical surface прежней F17: /api/v1/moderations(+exact/comments/withdraw/approve/reject) и /api/v1/publication-revisions/{id}/moderations,/moderation-history. No submit If-Match, existing core mutations required If-Match:428/own stale412/domain independent409. Text/Reason no actor/Version/body content edit. Exact success status/Location/no-op/duplicate-pending/profile/error codes/technical unavailable решаются здесь, не automatic201/204.

Submit теперь требует ClientOperationId и confirmed original execution receipt lookup до new Submit checks; atomic automatic preparation не теряет exact tuple. Comment/final-decision unknown outcome inquiry/correlation wire материализуется отдельно: text+actor+time не dedup, новое own ETag не automatic retry. Pending unique защищает cardinality, но не заменяет ClientOperationId/item binding. Approve shown comparison basis отдельно own token: drift 409 требует new review consent. Automatic actor/system metadata/profile Q20; auth recovery same actor+original baseline F16/Q24.

## F18: activity API и safe records

[F18](../features/F18-staff-activity-history.md) закрепляет read-only target GET `/api/v1/activity`, exact `/{activityId}` и `/filter-options`. Они отсутствовали в verified src от 05.10.2026; generated UI Activity API не доказан, API/runtime не запускались. UI `/admin/activity` не означает admin-only common history: common/staff-access visibility выбрана ниже.

Safe immutable DTO содержит actual action/actor/source time/target/exact context/confirmed outcome; no Version/If-Match/undo/body actor override. Actor/target captions optional snapshots, no foreign Identity/source joins or latest fallback. Hidden/denied rows applied before list count/page. Target-gone/read-error != missing audit, empty page не promise transport backlog drained.

**Инженерная детализация:** final action-code/target/profile literals, DTO member/casing/enum/nullability, normalized ID/time/metadata codecs, cursor wire/integrity/version/size, filter-options search/paging carrier и record404/403/owner-target error priority. Actor/scope/metadata/granularity/filters/keyset/defaults/retention ниже приняты, не повторный выбор продукта.

F17 Return сохраняет exact remark reference; полное замечание остаётся authoritative protected F17 read. Partial safe preview не автоматически весь Reason; SourceEventId/inbox/fingerprint не public detail по умолчанию. Current actor revalidation F16 относится к reader staff gate, не меняет actor historical fact. Обязательные export/realtime/notifications не вводятся.

## F18: принятый activity contract

Пользователь согласовал весь блок 1–24. [Q10](../../../business/questions/Q10-permissions-and-isolation.md#f18-history-scope-и-visibility) задаёт scope/privacy/retention; [F18](../features/F18-staff-activity-history.md) определяет models/layers/DF один раз; [Q20](Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read) — owner boundaries/cursor read. Это target contract, не реализация.

| Surface / access | Принятый контракт |
| --- | --- |
| GET /api/v1/activity | Server filtered visible keyset rows + NextCursor/HasMore; no required total count |
| GET /api/v1/activity/{activityId} | Exact immutable retained record + safe typed details; no edit token/Undo |
| GET /api/v1/activity/filter-options | Allowed action/type/actor-kind options и authors из visible audit history, не private staff directory |
| audit.activity.read | Fixed role grants Specialist + Administrator, common content rows; no forced personal actor scope |
| audit.staff-access.read | Administrator only, staff provisioning/roles/lifecycle/setup/reset/bootstrap/emergency rows |

Filter input: ActorStaffId/ActorKind, action, TargetType+TargetId/exact context, From/To UTC `[From,To)`, CorrelationId/OperationId. Default temporal bound absent. Source OccurredAt DESC, immutable record Id DESC tie; default50/max200 rows. Cursor opaque, bound to normalized query and reader context, current F16 authorization/visibility every call. Query/context change resets cursor. Late append possible: cursor не frozen snapshot/full causal order/source-completeness proof. UI manual Refresh возвращает first page, no realtime/poll/notifications. Exact index/cursor codec/signing/time precision Q20/Q15 materialization.

Visibility applies **before keyset/page/options/count**, not client hidden rows or after paging. Staff actor filter не запрашивает admin-only IdentityAccess directory; names/suggestions derive only from current-reader-visible historical captions. Unknown metadata/action profile is not accepted as safe generic JSON. No row field permits arbitrary producer-selected public exposure. Safe source profile/classification checked by Audit registry.

Read data: ActivityId, source OccurredAt/audit RecordedAt UTC, ActorKind Staff/System/Integration/Operator with appropriate trusted identifier/caption, optional durable Staff initiator distinct from executor, ActionCode, exact Target reference, Outcome=Committed, optional safe Summary and typed Metadata. Profile code versioned/stable; Id/SourceEventId are not operation success/permissions. Private source payload fingerprint/inbox/retry state/provider secrets/Version/ETag not normal detail fields. OccurredAt is source action time, не физический SQL commit или global order.

Committed означает durable source business result, **не all-success workflow**. Partial/failed business terminal state lives in typed outcome details and clear display text. Failed request/validation/rollback/no-op/confirmed ClientOperationId replay produces no new performed row; intentional new artifact even same content gets a new fact. Whole-tree Save one semantic row; automatic revision+moderation+item preparation one tuple row; human decision and activation separate. Final catalog excludes unsupported owner lifecycle actions and doesn't create those routes.

Metadata allowlists cover exact related IDs, old/new lifecycle/roles/policy, small counts/operation references. No content field values/full before-after/raw payload/storage URLs/ETags/passwords/code/hash/stamp/cookie. Captions as-of source capture may be absent; use safe IDs, never current-user/latest-directory fallback. Rename/deactivation/deleted target do not rewrite history. Mutable Draft link opens current state of same exact Draft identity, not alleged historical content copy. Full F17 Comment/RejectReason remains separate authorized owner read by exact remark reference; denied/gone/unavailable not empty remark.

Audit read doesn't grant target read: owner permission separate. Missing target doesn't remove retained record. UI list/detail/ref errors scoped; empty confirmed query not source-backlog drained or failed source command. No local final row after button and no source response waiting for AuditId/append. MVP retains rows without auto purge/user DELETE/cascade; export/archive/exceptional anonymization require later explicit policy.

Open literal contract work: source ActionProfile/metadata version codecs/public captions, dictionary search/value carrier, public exact-reference navigation, lossless timestamp wire/storage codecs implementing Q20 UTC100ns ordering, cursor integrity/schema implementing canonical SQL UUID ordering, safe operation trace exposure/errors/access fixtures. Agreed routes/permissions/defaults/read-only/eventual/granularity/retention semantics remain fixed. Golden source/event/read/cursor/error fixtures and real auth/SQL/browser tests required before NSwag regeneration.

## F13: agreed public sync wire v1

[Q33](Q33-sync-resume-transport.md#public-wire-contract-v1) fixed Start/Status/Data/Commit routes, required arrays/empty/duplicate policy, DTO camelCase/types, changed-only summaries/base-target hashes/counts, signed opaque cursor73bytes/98Base64URLchars, six known record kinds and normal1..N vs invalidation0/no cursor, contentJson string exact UTF8 before typed parse, original receipts/status distinctions. Owner field tables [F13 §3.3](../features/F13-content-delivery.md#33-dto-и-parameter-models); [literal fixtures](../sync-wire-v1-vectors.md) examples/byte evidence, not second model. JSON200 ApiResponse<T>, rawNDJSON Data; malformed400/context409/unknown404/known410 via bounded tombstone accepted.

Demo материализован public DTO/ports в `Delivery.Application.Abstractions.Synchronization`, CQRS command handlers в Application и observer/Freshness query handlers в Persistence. `SynchronizationController` возвращает выбранный JSON200 ApiResponse и raw NDJSON200; scoped codecs требуют canonical Guid, positive Int64, supported profile/hash, обязательные массивы и отклоняют unknown/duplicate request members. `X-CM-Sync-Execution-Generation` — canonical positive Int64 header, описан в OpenAPI и exposed CORS. Data errors до headers остаются application/problem+json; после headers подтверждённая terminal restriction/integrity выдаёт seq0 без cursor, технический/неизвестный отказ прекращает transfer для безопасного восстановления. SDK должен отличать эти результаты.

| Observable Demo code | HTTP / значение |
| --- | --- |
| validation.failed | 400: существующий общий ValidationProblemDetails для JSON/required/duplicate/Guid/hash/profile/cursor validation; внутренние sync.invalid_input/invalid_request/invalid_selection/invalid_cursor не выдаются вместо этого общего code |
| sync.not_found | 404: unknown operation, restart либо eviction evidence |
| sync.selection_unavailable | 404: выбранная exact Published структура отсутствует/не соответствует SchemaId |
| sync.context_conflict | 409: другой original input, plan, generation либо receipt evidence |
| sync.execution_conflict | 409: competing/stopped/invalidated ownership context |
| sync.target_unavailable | 409 до headers; confirmed terminal invalidation после headers |
| sync.expired / sync.discarded | 410 только при retained bounded tombstone; identical retained control replay возвращает исходный JSON200 |
| sync.capacity | 503: заполнен retained plan cache, active plans не вытесняются |
| sync.timeout | 503 до headers: finite stage watchdog; поток после headers обрывается recoverably |
| sync.source_unavailable | 503: неизвестный/временно недоступный authoritative source, не desired absence |
| sync.source_integrity | 503 до headers; confirmed terminal invalidation после headers: повреждённые pinned canonical facts/bytes |
| sync.record_too_large | 503 до headers либо interrupted transfer: превышен выбранный record byte budget |

Нормативные counts/framing/cursor/controls/Freshness не меняются; literal `fixtureWithdrawn` остаётся только примером. Numeric host profile выбран в [Q33](Q33-sync-resume-transport.md#числовой-профиль-demo-f13). MVP session/license credentials separate; synchronizationId/cursor не credential. HTTP/3/proxy/SDK/Unity conformance не выводится из наличия этих CLR types.

## F21: Schema API и source evidence

[Registry F21](../features/F21-schema-registry.md) реализован в ContentModeling: SchemaRegistryController, SchemaContractSourceQueries, Persistence query handlers и pure JsonSchemaContractExporter. F03 предоставляет frozen integer number/name/key, recursive tree и canonical source triple. Authoring version controller остаётся отдельным read, а F22 C# generator не вводится этим изменением.

Exact/internal batch queries используют SchemaContractMetadata, PublishedSchemaContractSource (Metadata + complete SchemaContentSnapshotDto) и owned SchemaContractDocument. Typed CanonicalProfileId/CanonicalDigest переиспользуют neutral building block; Domain/EF graph не выходит в contract. CQRS/DI подключены через существующий Application module. Source size/status preflight и full capture выполняются в одной read-only repeatable-read transaction, pure export после commit. Source writes/cache/SQL migrations отсутствуют.

**Принято:** exact GET /api/v1/schema-registry/schema-versions/{schemaVersionId}, raw application/schema+json, Draft2020-12 cm-json-schema/1, anonymous Demo, no latest/public Demo batch. F22 internal batch in-process/full coverage; Published historical Withdrawn/Retired/Archived tooling read сохраняется Q31. OpenAPI 200 — binary string под application/schema+json, без ApiResponse envelope; ошибки — existing JSON ProblemDetails/ValidationProblemDetails. URI binding/empty ID и query-validator input failures возвращают 400 по существующему validation profile.

VersionNumber — positive published business int, не технический Version/ETag. Registry read не require If-Match/428/412. Representation ETag — strong quoted 64lowerhex SHA-256 deterministic export bytes, отдельно от source ContentHash. Cache-Control=no-store; conditional 304 не добавляется. Root/property/annotation byte layout и explicit required numeric settings — Q31 §3. Отдельного UI F21 нет; generated raw client использует FileResponse и bypass application/schema+json в envelope handler, F22 выбирает exact IDs через ordinary source lists.

| Machine code | Demo result |
| --- | --- |
| schema_registry.invalid_selection | 400 internal source input: nonempty bounded unique IDs required |
| schema_registry.not_found | 404: один exact ID отсутствует; whole batch refused |
| schema_registry.not_published | 409: Draft/nonPublished, без latest fallback |
| schema_registry.profile_unsupported | 400 internal exporter: unknown formal profile; configured unsupported profile blocks startup |
| schema_registry.source_invalid | 503: missing/corrupt/unsupported frozen source или invalid JSONB definition |
| schema_registry.source_unavailable | 503: database/source unavailable |
| schema_registry.source_limit_exceeded | 503: SQL input preflight превышает per-source/batch source-byte limit |
| schema_registry.document_limit_exceeded | 503: formal document превышает output byte limit |
| schema_registry.batch_limit_exceeded | 400 internal query: combined formal documents exceed batch output budget |
| schema_registry.timeout | 503: finite read/export deadline; caller cancellation propagates отдельно |

## F22: binary generation API и source evidence

[F22](../features/F22-csharp-model-generation.md) Demo source materialization 09.10.2026: neutral `src/BuildingBlocks/ContentManager.Schema.CodeGeneration.Core` (`net10.0`, Roslyn4.14.0/Newtonsoft13.0.3); `ContentModeling.Application.Abstractions/CodeGeneration/CodeGenerationContracts.cs`; `ContentModeling.Application/CodeGeneration/GenerateCSharpModelsCommandHandler.cs`, `CSharpGenerationOptions.cs`, `GenerateCSharpModelsCommandValidator.cs`, `DeterministicZipWriter.cs`; `ContentModeling.Api/CodeGeneration/CodeGenerationController.cs` и `CodeGenerationOpenApiTransformer.cs`. Core ABI/naming/source/ZIP literals находятся в F22/Q31; Domain/EF/SDK production references у Core отсутствуют.

`src/App/ContentManager.Application/ContentManagerApplicationModule.cs` binds `ContentModeling:CSharpGeneration`, ValidateOnStart, registers Core/ZIP singleton и добавляет GenerateCSharpModelsCommand в SelfManagedCommands. Handler не создаёт SQL UoW для генерации; F21 Persistence boundary самостоятельно захватывает exact source set в своём read-only snapshot. `ContentManagerApiModule` registers binary OpenAPI transformer. IDs-only input, no expected Version/If-Match/business mutation/job/history.

**Actual Demo contract:** anonymous `POST /api/v1/code-generation/csharp`, request `{"schemaVersionIds":["22222222-2222-2222-2222-222222222222"]}`. Mandatory nonnull/nonempty array, nonempty unique GUIDs, selected count ≤ configured limit; missing/unknown body properties rejected. Нет public profile/options/namespace/serializer/code/source/actor fields. `CodeGenerationController.CSharp` returns raw `application/zip`, attachment filename `content-manager-csharp-models.zip`, `Cache-Control: no-store`; OpenAPI200 is string/binary under application/zip, без ApiResponse/base64/job DTO, ETag/If-Match или новых GET/jobs routes.

Handler создаёт linked total-operation deadline, получает весь F21 in-process exact batch, проверяет formal input bytes, вызывает Core, проверяет diagnostics/exact root/file coverage, полностью собирает ZIP и только затем возвращает owned GeneratedArchiveResult. Fatal Core/invalid/unpublished/missing target, unresolved type/file collision, source/archive guardrail error не дают partial ZIP. Exact Published Retired/Withdrawn/Archived historical tooling read разрешён; lifecycle не является filter для удаления immutable formal history. Info/Warning не требуют ZIP manifest или HTTP diagnostics headers. Caller cancellation propagates; own deadline становится 503; после binary headers JSON error не вставляется в stream. UI /code-generation не выбирает profile/options, F16 не становится скрытой Demo dependency.

| Observable code / internal origin | HTTP / result |
| --- | --- |
| validation.failed | 400 ValidationProblemDetails: body/IDs/count, Core contract/naming/depth/input/source rejection, archive path/byte rejection. Internal code_generation.selection_invalid/selection_limit/input_limit/contract_invalid/depth_limit/type_unsupported/naming_collision/source_limit/input_limit_exceeded/archive_rejected не подменяют common public code |
| schema_registry.not_found | 404: хотя бы один exact ID отсутствует; successful artifact отсутствует |
| schema_registry.not_published | 409: Draft/nonPublished, no latest fallback |
| schema_registry.source_invalid/source_unavailable/source_limit_exceeded/document_limit_exceeded/timeout | 503 forwarded F21 source/read/export failure; F21 caps/deadline независимы |
| code_generation.incomplete_output | 503: root/file coverage не совпадает с requested set |
| code_generation.timeout | 503: собственный whole-generation deadline; caller cancellation отдельна |

Public error projection задаёт existing `Api.Shared/Extensions/ResultMappingExtensions.cs`: validation errors группируются по safe target и получают `code=validation.failed`; остальные ResultError kinds сохраняют свой machine code/status в ProblemDetails. Core diagnostics сохраняют exact SchemaVersionId?/FieldPath?, не raw schema/source/server paths. Unexpected infrastructure/server exceptions не превращаются в successful binary output.

Выбранный section `ContentModeling:CSharpGeneration`: Profile=`cm-csharp/1`, MaxSelectedSchemas=64, MaxInputBytes=33554432, MaxFiles=64, MaxFileBytes=4194304, MaxSourceBytes=33554432, MaxArchiveBytes=41943040, OperationTimeout=`00:01:00`. Core MaxDepth=64. Missing/unsupported/invalid/inconsistent settings block startup; exact source/ZIP bytes от изменения допустимых thresholds не меняются. F21 source/formal caps остаются отдельными. Подробнее [Q31 §7.2.1](Q31-schema-code-generation.md#721-локальный-demo-f22-operational-profile).

Verification boundary 09.10.2026: Core41/41 (independent literals/C#8 syntax), actual emitted SDK consumer71 assertions, scoped API78 checks/architecture44; after final ZIP whitelist change F22 HTTP9 и ZIP/options26 прошли отдельно. PostgreSQL F22 HTTP coverage проверяет multi-version deterministic ZIP/headers/historical rename+Retired+Withdrawn, missing/Draft/invalid input, budgets, own deadline и caller cancellation. Реальный browser Code Generation flow проверен отдельно после исправления UI error binding; screenshot подтверждает actual UI result, а UI/transport evidence находится в frontend F22. Эти результаты не доказывают Unity IL2CPP/package distribution/full F19 sync или MVP CLI; browser screenshot сам по себе не заменяет file-integrity/Player proof.
