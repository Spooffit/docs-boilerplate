# F07. Общий контент Product: бэкенд

[Бизнесовая спецификация](../../../business/features/F07-product-content-assignment.md) · [Модуль M04](../modules/M04-publishing.md) · [Фронтенд](../../frontend/features/F07-product-content-assignment.md) · [Q07](../../../business/questions/Q07-exclusions-and-local-content.md#f07-допустимость-рабочих-references) · [Q15](../questions/Q15-api-and-source-code.md#f07-read-write-и-reference-контракт) · [Q20](../questions/Q20-persistence-and-migration.md#f07-initialization-и-конкурентные-guards).

## 1. Область реализации

F07 относится к полному Publishing в MVP. Специалист сохраняет назначения схем: exact Published SchemaVersion и AllExcept/SelectedOnly. Exact InstanceVersion references являются pins SelectedOnly либо результатом разрешения правила, а не единственным видом assignment. Состав служит источником immutable ProductContentRelease F09 и базой preview F08. Demo не зависит от полного Publishing. [Q39](../../../business/questions/Q39-schema-content-selection.md) определяет новые границы; текущий flat AssignmentItemInput ещё их не реализует.

Publishing владеет assignment по стабильному ProductId; Product source fields принадлежат VR School/CatalogAccess F06. F07 не переименовывает Product, не меняет lifecycle каталога, не создаёт release/revision, не активирует публикацию и не отправляет новый состав клиентам. Изменения assignment не переписывают существующую историю. Add/Replace/Remove — локальные действия подготовки одного whole-set Save, а не отдельные серверные mutation endpoints.

Физические слои: `ContentManager.Publishing.Domain`, `.Application.Abstractions`, `.Application`, `.Api`, `.EntityFramework`; query/repository adapters должны располагаться в целевом `.EntityFramework.Persistence` по S20. Сейчас отдельного Publishing.Persistence нет. Основной orchestration находится в [App PublishingService](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.cs), а [PublishingController](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Api/PublishingController.cs) обслуживает широкий workspace API. Это исходная точка, не доказательство собственного assignment ETag/public-port target. Новые имена contracts/handlers ниже — целевые обозначения; сверка исходников и неизвестные механизмы вынесены в Q15/Q20.

Archive Product допускает read/retain unchanged/remove/eligible replacement уже проблемного reference как ограниченное обслуживание без expansion; добавление новых позиций и расширение запрещены, eligible replacement проверяется как new use по [F06 §2.3](F06-product-catalog.md#23-инварианты-и-constraints). Open conflict блокирует ordinary Save только authoritative affected scope; отдельный corrective [F15 DF07](F15-catalog-conflicts.md#df07-local-repair-boundary) ограничен Variant patch и не разрешает Product whole-set Save bypass.

## 2. Domain

### 2.1. Aggregates, entities и value objects

| Тип | Роль | Владелец / граница |
| --- | --- | --- |
| `ProductId` | Strong stable ID | Publishing reference на CatalogAccess, без Domain зависимости на чужой root |
| `EntityInstanceId`, `EntityInstanceVersionId`, `EntitySchemaId`, `EntitySchemaVersionId` | Strong reference IDs | Publishing reference types; наружу Guid |
| `PublishedInstanceVersionReference` | Immutable value object | Owned item assignment; создаётся из authoritative ContentModeling facts |
| `ProductContentAssignment` | Mutable aggregate root | Publishing; один working assignment на Product |
| `SchemaContentSelection` | Owned immutable-by-replacement rule | Product/Variant assignment; одна logical SchemaId на owner |
| `SchemaInstanceSelectionMode` | Enum: AllExcept, SelectedOnly | JSON string codec §3.6; AllExcept без exclusions отображается «Все экземпляры» |
| `ResolvedPublishedInstanceVersion` | Application fact, не Domain root | ContentModeling public read boundary → Application |

**ProductId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Stable exact reference identity | Non-empty, immutable; source authority CatalogAccess/ContentModeling, наружу Guid |

**EntityInstanceId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Stable exact reference identity | Non-empty, immutable; source authority CatalogAccess/ContentModeling, наружу Guid |

**EntityInstanceVersionId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Stable exact reference identity | Non-empty, immutable; source authority CatalogAccess/ContentModeling, наружу Guid |

**EntitySchemaId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Stable exact reference identity | Non-empty, immutable; source authority CatalogAccess/ContentModeling, наружу Guid |

**EntitySchemaVersionId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Stable exact reference identity | Non-empty, immutable; source authority CatalogAccess/ContentModeling, наружу Guid |

**PublishedInstanceVersionReference**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntityInstanceId | `EntityInstanceId` | Да | Logical item identity | Unique within Product assignment; authoritative resolution |
| EntityInstanceVersionId | `EntityInstanceVersionId` | Да | Pinned exact Published version | No latest substitution; new-use/lifecycle checks F04/Q07 |
| EntitySchemaId | `EntitySchemaId` | Да | Logical schema identity | Authoritative owner/target; не Key matching |
| SchemaVersionId | `EntitySchemaVersionId` | Да | Exact schema target | Required/non-empty; подтвердить из ContentModeling, не UI |

**ProductContentAssignment**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | `ProductId` | Да | Root semantic identity | Один assignment на Product; stable source reference |
| SchemaSelections | `IReadOnlyCollection<SchemaContentSelection>` | Да | Whole working rule set | Non-null; empty допустим; unique logical SchemaId; no individual rule ETag |
| Version | `int` | Да | Own internal root concurrency | Whole-set conditional writes; no DTO; initial/increment/no-op Q20 |
| UpdatedAt | `DateTimeOffset?` | Условно | Optional audit last successful save time | Audit profile Q15; server clock; initial empty может null; no-op Q15/Q20 |

**ResolvedPublishedInstanceVersion**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntityInstanceId (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Authoritative logical instance ID | Carrier port Q15/Q20; не UI identity |
| EntityInstanceVersionId (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Exact requested version identity | Carrier port Q15/Q20; no latest |
| EntitySchemaId (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Authoritative logical schema identity | Подтверждает ContentModeling |
| SchemaVersionId (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Exact schema target identity | Подтверждает ContentModeling; не Key matching |
| Published number (имя уточняется) | Уточняется | Уточняется | Exact published business number | Read/display profile Q15, не technical Version |
| Display labels (имена уточняются) | Уточняется | Уточняется | Name/Key enrichment | Authority/nullability/fallback Q15; не matching identity |
| Lifecycle/new-use availability (имена уточняются) | Уточняется | Да | Authoritative content availability/issue facts | Unchanged exact retention при Save; new position/replacement requires current eligibility; Q15 shape |
| Guard context (имя уточняется) | Уточняется | Уточняется | Necessary protected reference-use facts | Public boundary Q20; не user-assigned родство |

Assignment selector identity — SchemaId внутри Product; instance identity — EntityInstanceId внутри selector. Root Version защищает весь набор правил. SelectedOnly pins и SchemaVersionId immutable внутри сохранённого значения; AllExcept вычисляет maximum Published VersionNumber каждого owner в выбранном exact target при F09 review, с повторной проверкой basis при Create. Новая SchemaVersion никогда не подставляется; недопустимый winner не заменяется старой версией молча.

**SchemaContentSelection (целевая Domain model):**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaId | `EntitySchemaId` | Да | Logical scope | Authoritative owner выбранной структуры; unique внутри assignment |
| SchemaVersionId | `EntitySchemaVersionId` | Да | Exact Published target | Не current/latest; включённые versions принадлежат этому target |
| Mode | `SchemaInstanceSelectionMode` | Да | AllExcept либо SelectedOnly | Closed enum; unknown rejected |
| IncludedVersions | `IReadOnlyCollection<PublishedInstanceVersionReference>` | Да | Exact SelectedOnly pins | Empty для AllExcept; unique EntityInstanceId; пустой SelectedOnly допустим |
| Exclusions | `IReadOnlyCollection<SchemaInstanceExclusion>` | Да | Stable AllExcept exclusions | Empty для SelectedOnly; owned VO ниже; не весь complement universe |

Domain хранит policy, а не кеш всех автоматически включённых экземпляров. AllExcept с пустым exclusions и SelectedOnly с пустым includes — разные состояния. Whole-rule switch не инвертирует списки молча; пользователь проверяет diff. Существующие неизменные pins сохраняют retention semantics ниже; selector с нулём результатов не удаляется. Domain methods принимают authoritative resolved facts, не обращаются к ContentModeling/EF.

**SchemaInstanceExclusion**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntityInstanceId | `EntityInstanceId` | Да | Stable excluded identity внутри собственного selector | Non-empty; unique внутри scope; не inherited F08 exclusion |

Keys/names — display/runtime metadata, не matching identity. Ручной SortOrder/«Порядок в релизе» исключён из Domain/DTO/persistence назначения. Hash, AssetIds или content JSON из текущего `ContentAssignmentItem` не входят автоматически в подтверждённый минимальный reference contract. Нужность сохранённых display/hash/dependency metadata и их authority уточняются Q15; клиент эти поля не присылает. F09/F10 определяют свои immutable snapshot payload, а F07 не вычисляет новый content hash и не копирует/редактирует контент версии.

`UpdatedAt`, если сохраняется, означает время successful whole-set mutation от server clock; initial empty state может иметь null. Audit/changed-by и no-op timestamp/token policy ещё Q15/Q20, не произвольная user-сущность. Root не имеет lifecycle Published/Retired: это рабочий набор, пустой набор допустим; требования к непустому release/revision принадлежат F09/F10.

### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| `ProductContentAssignment` | `Create` | `ProductId` | Authoritative Product identity существует; assignment ещё нет | Пустой working set, initial local Version по общей convention |
| `PublishedInstanceVersionReference` | `CreateFromResolved` | Четыре authoritative IDs | Версия принадлежит указанному instance/schema и допустима в контексте операции | Immutable exact reference; не принимает labels/родство на веру от UI |
| `ProductContentAssignment` | `ReplaceSelections` | `IReadOnlyCollection<SchemaContentSelection>`; server mutation time при audit | Валидный полный candidate, unique SchemaId и selected EntityInstanceId; authoritative facts проверены | Атомарно заменяет правила; immutable history не меняется |

Domain method не выполняет HTTP/EF/query чужого BC. Existing root не пересоздаётся при Save/новом source snapshot. Persisted Version увеличивается вместе с state-changing mutation; конкретное место increment Domain/EF convention задаёт Q20, не два независимых increments. Идентичный повтор с прежним ETag не получает право игнорировать concurrency.

### 2.3. Инварианты и constraints

1. Один assignment на Product; в нём не более одной exact version каждого EntityInstanceId. Две версии одного instance — ошибка, не last-write-wins.
2. UI передаёт schema selector с exact SchemaVersionId, mode и pins/exclusions. Backend подтверждает SchemaId/target и принадлежность pins/excluded IDs через ContentModeling; клиентские labels не authority.
3. Для нового добавления/замены разрешены только Published и допустимые для нового использования версии. Draft, Retired/Withdrawn, Archived logical owner и иные authoritative blockers не обходятся выбором старого known ID.
4. Maximum Published instance version разрешается только явным AllExcept и только внутри выбранного target. SelectedOnly и сама SchemaVersion не получают latest substitution. Mutable keys не объединяют identities.
5. Удаление item не удаляет instance/version/schema/assets. Existing immutable releases/revisions/history/Delivery не меняются.
6. F15 blocks проверяются в scope конкретного Product/операции; чтение существующего состава остаётся возможным.
7. Продуктовые source lifecycle/new-use restrictions F06/Q27 не заменяют own assignment Version. Catalog ETag или publication-state revision не является assignment ETag.

[S21](../../../catalog/docs/standards/architecture/ddd.md#aggregate-owned-rule-definitions): `ProductContentAssignmentConstraints` — целевой единый контейнер root/Items/reference constraints. Non-empty IDs и uniqueness применяются Domain/FV/EF согласованно. Максимальный размер набора, order semantics и строковые limits display metadata не выбраны; не переносить page size/catalog Name constraints на количество items.

Неизменённая ранее сохранённая exact reference может остаться в whole-set Save после Retired/Withdrawn версии, Archived logical owner/schema либо иной недоступности. Backend сохраняет прежние authoritative IDs, не заменяет latest и не требует new-use eligibility для удержания этой же reference. Это не разрешение создать release: такая dependency попадает в ReleaseBlockers F09. Новая позиция и замена exact version являются новым использованием и требуют полной текущей eligibility; display reorder сам по себе новым использованием не является.

Для inactive/archived Product разрешён ограниченный maintenance: Remove проблемной reference и Replace существующей проблемной позиции на допустимую exact reference. Add и расширение состава запрещены; результат не содержит новых logical positions сверх исходного набора. Точный source lifecycle carrier/reactivation остаётся F06/Q27. OPEN CatalogConflict F15 продолжает блокировать обычный Save, включая maintenance; этот путь не получает repair bypass Q25. Сохранённый working set может иметь ReleaseBlockers и не быть release-ready.

### 2.4. Domain events

Сохранение F07 не требует release-published/activation/Delivery event. Отдельный domain event ради логирования или запуска automatic promotion не вводится. Internal setup после появления Product требует надёжного idempotent trigger DF03; событие/catalog handler hook и transport выбираются Q20. Старое предположение «CatalogAccess всегда публикует internal Product event и создаёт сразу все roots Publishing» не считается подтверждённым механизмом F06. Если выбран event, его факт/registration/durable delivery описываются реализацией; in-memory dispatch не доказывает recovery.

## 3. Application.Abstractions

### 3.1. Commands

| Command / use case | Result | Concurrency |
| --- | --- | --- |
| `UpdateProductContentAssignmentCommand` | Resulting ProductContentAssignmentDto + internal committed Version metadata | ExpectedVersion из assignment If-Match; root-own atomic conditional write |
| `EnsureProductContentAssignment` | Existing/created root identity, без public DTO | Internal idempotency по ProductId + unique key; не If-Match пользователя |

**UpdateProductContentAssignmentCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | `Guid` | Да | Source owner из route | Не body override |
| ExpectedVersion | `int` | Да | Internal own token из If-Match | Не DTO/body; root-own conditional write |
| SchemaSelections | `IReadOnlyList<SchemaContentSelectionInput>` | Да | Полный набор schema rules | Non-null; empty допустим; unique SchemaId; exact scope; модель ниже |

**EnsureProductContentAssignment**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | `Guid` | Да | Existing authoritative Product identity | Internal idempotency unique ProductId; no public create/source CRUD |
| Trigger metadata (имена уточняются) | Уточняется | Уточняется | Recoverable setup input | Только после выбора Q20; existing filled root не reset |

Первый command заменяет весь набор. Body не содержит ExpectedVersion/Revision, ProductId override, schema/instance IDs, SortOrder или F08 exclusions. Internal result carrier возвращает DTO и metadata одной successful mutation; Version не сериализуется. Setup не public POST/PUT создания source Product и не побочное действие GET.

### 3.2. Queries

| Query | Result |
| --- | --- |
| `GetProductContentAssignmentQuery` | ProductContentAssignmentDto + internal own Version для ETag |
| Batch exact-content resolution port | Mapping по каждому requested ID: authoritative reference, availability/guard/display facts |
| Batch schema-selection resolution port | Exact targets, publication universe/basis, maximum Published versions per instance, resolved membership и diagnostics; public ContentModeling boundary |
| Product/block facts port | Existence, current eligibility и F15 blocker facts |

**GetProductContentAssignmentQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | `Guid` | Да | Exact owner | Own Version metadata отдельно; no creation-on-read |

**Batch exact-content resolution port input**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntityInstanceVersionIds | Уточняется | Да | Requested exact IDs | Batch requested subset; carrier Q15/Q20 |
| Usage context (имя уточняется) | Уточняется | Да | New/retained reference classification | Unchanged exact retention vs new position/replacement; shape Q20 |

**Product/block facts port input**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Target source owner | Public CatalogAccess boundary; carrier Q15/Q20 |
| Вид затронутой операции (имя уточняется) | Уточняется | Да | Scoped eligibility/F15 facts | Q15/Q20; не blanket блок всех reads |

Public picker reads переиспользуют list logical instances и list Published versions F04; F07 не заводит отдельный endpoint «все версии всех instances» или новый search backend. Query handlers own assignment находятся в Persistence; reads не создают root, не очищают unavailable items и не увеличивают Version.

### 3.3. DTO и parameter models

| Model | Назначение |
| --- | --- |
| `ProductContentAssignmentDto` | Собственная модель F07; поля ниже |
| `ProductContentAssignmentItemDto` | Собственная модель F07; поля ниже |
| `UpdateProductContentAssignmentRequest` | Собственная модель F07; поля ниже |
| Internal resolved facts | Переиспользует ResolvedPublishedInstanceVersion §2.1; без новой копии полей |

**ProductContentAssignmentDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | `Guid` | Да | Stable assignment owner | No technical Version/Revision/ExpectedVersion |
| SchemaSelections | `IReadOnlyList<SchemaContentSelectionDto>` | Да | Saved rule set со структурой и policy | Не подменяется resolved preview; тип §3.6 |
| ResolvedPreview | `SchemaSelectionPreviewDto` | Да | Текущий read result | Basis не own ETag; read failure не empty set; §3.6 |

**ProductContentAssignmentItemDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntityInstanceId | `Guid` | Да | Stable logical item identity | Сохраняется при unavailable labels |
| EntityInstanceVersionId | `Guid` | Да | Exact pinned version | Не latest |
| EntitySchemaId | `Guid` | Да | Authoritative logical schema | Не string Key matching |
| SchemaVersionId | `Guid` | Да | Exact schema target | Не client-assigned родство |
| EntityInstanceName | string (nullability уточняется) | Уточняется | Display label | Authority/current-vs-saved/fallback Q15; не identity |
| EntityInstanceKey | string (nullability уточняется) | Уточняется | Display/runtime key | Q15; rename не replacement |
| EntitySchemaName | string (nullability уточняется) | Уточняется | Display schema label | Q15 fallback при unavailable |
| PublishedVersionNumber | Уточняется | Уточняется | Exact published business number | Type/nullability Q15; не root Version |

**UpdateProductContentAssignmentRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaSelections | `IReadOnlyList<SchemaContentSelectionInput>` | Да | Whole-set rule choice | Empty означает empty assignment; own If-Match; no implicit Publish |

**SchemaContentSelectionInput (Application.Abstractions; JSON camelCase):**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaVersionId | `Guid` | Да | Exact target | SchemaId выводится/проверяется сервером, не доверяется из UI |
| Mode | `SchemaInstanceSelectionMode` | Да | Rule intent | String AllExcept/SelectedOnly; numeric/unknown →400 |
| IncludedVersionIds | `IReadOnlyList<Guid>` | Да | SelectedOnly exact pins | Не duplicates; same target; empty в AllExcept |
| ExcludedInstanceIds | `IReadOnlyList<Guid>` | Да | AllExcept stable IDs | Не duplicates; same logical schema; empty в SelectedOnly |

Reference IDs — required и сохраняются при unavailable display enrichment. Обычные labels — strings, number — exact published business number; nullability/fallback и availability diagnostics при missing historical display source уточняются Q15. Read contract должен позволять показать существующую reference с причиной недоступности, а не сбрасывать весь assignment в пустой. Возможный diagnostics submodel не добавляет Version в DTO. Источник labels/current names vs saved metadata и нужность UpdatedAt/changed-by — Q15.

Request/model нельзя переиспользовать как read DTO, F08 patch или F09 release snapshot. Batch lookup не доверяет client-supplied EntityInstanceId/SchemaVersionId; reference error должен быть привязан к requested version ID/item. Result DTO отражает сохранённый candidate, а не повторный workspace read, который уже мог увидеть чужой Save.

**AssignmentValidationDiagnostics (логическая модель, CLR/wire carrier — Q15)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SaveBlockers | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Причины отказа whole-set Save | Intrinsic invalid candidate, запрещённое новое использование/expansion, OPEN F15; empty collection означает нет blockers |
| ReleaseBlockers | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Причины отказа нового release | Retained unavailable references и current release guards; Save success не release-ready |
| Warnings | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Неблокирующие сведения | Не заменяют blockers; отсутствие актуальных facts не warning о валидности |
| InputBasis | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Derived basis данного вычисления | Own assignment generation и captured catalog/content facts; отдельный от own ETag контекст |

**AssignmentDiagnosticIssue (логическая модель, CLR/machine DTO — Q15)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Address (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Адрес item/rule/field или reference | Collection/group + stable identity; exact path format Q15, не guessed JSON pointer |
| ParticipatingIds (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | IDs involved in error/collision | Relevant requested/reference/target/result IDs; empty при global issue |
| Reason (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Typed machine reason | Exact codes/DTO Q15; semantics SaveBlockers/ReleaseBlockers/Warnings из owning collection |

Каждая issue содержит логический адрес item/field или reference ID и участвующие IDs; exact CLR fields/machine codes определяются Q15. Логические категории являются разными коллекциями, а не одним общим флагом IsValid. Read outage не превращается в пустой допустимый набор. Допустимый degraded read сохраняет IDs и явно помечает basis неполным; policy error/degraded carrier уточняется Q15.

### 3.4. Ports

| Port | Назначение | Направление |
| --- | --- | --- |
| `IProductContentAssignmentsRepository` | Root tracked по ProductId; Add только internal setup; commit через Publishing UoW | Outbound → Publishing.Persistence |
| `IContentModelingPublishingQueries` | Batch exact pins и schema targets/universe/basis; owner/target/lifecycle/availability и scoped max-Published resolution | Outbound → публичный ContentModeling application boundary |
| `ICatalogPublishingQueries` | Product existence/eligibility и scoped F15 write blockers | Outbound → публичный CatalogAccess boundary |
| Reference-use coordination contract | Согласовать новый/снятый assignment reference с concurrent archive/delete/retire/withdraw и catalog move | Public read facts F04/F15; serialization через MVP gate §7.2; carrier Q15 |
| Assignment dependency/read port | Working set/version для F08 preview, F09 snapshot и reference/impact checks | Inbound Publishing; без прямого чужого DbContext |
| Server clock | UpdatedAt при выбранном audit profile | Existing TimeProvider/clock adapter, без нового package |
| MVP consistency guard / owner UoW boundary | Один global transaction exclusive gate по Q20, owner-only atomic commit и independent primary public reads | Shared infrastructure adapter; owner connection, не foreign transaction |

Не создавать repository на каждый item и отдельную БД references без принятого протокола. Coordination port обозначает необходимую границу, не выдуманный готовый SDK API. Для F09 требуется consistent snapshot assignment; F08 видит live base, но существующий local patch не перезаписывается F07.

### 3.5. Specifications / read contracts

[S19](../../../catalog/docs/standards/dotnet/ardalis-specification.md): целевой `ProductContentAssignmentByProductIdSpecification` читает own root/items; atomic ProductId condition переиспользуется exact query/repository, tracking задаётся use case. Batch ContentModeling adapter ищет только requested IDs; stable-ID uniqueness проверяется после resolution, а не по labels.

Read projection AsNoTracking получает whole assignment и внутренний token одной согласованной версии; item view не смешивает разные saves. Display/lifecycle enrichment батчами через public port, без N+1 на item и десериализации всех content graphs ради списка. Правила read consistency при concurrent Save и external metadata change — Q15/Q20. Assignment — набор; presentation grouping по EntitySchema не вводит runtime order/SortOrder. Picker search/page/sort принадлежат F04 specifications. Composition условий — builder conditions, не выдуманный `spec.And`.

### Массовая фиксация и подготовка выпуска

Ниже описан целевой orchestration контракт [Q38](../../../business/questions/Q38-bulk-version-freeze-and-release-preparation.md), а не существующие типы/API. Точные имена CLR/wire и execution mechanism уточняются там. Обычный assignment DTO/PUT не расширяется Draft references: только отдельный явный workflow переводит проверенные Draft в Published до назначения.

**Логическая application model `FreezeSelectionItem` (предлагаемое обозначение, не request DTO):**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `DraftVersionId` | `Guid` | Да | Exact выбранная SchemaVersion либо EntityInstanceVersion | Не latest; принадлежность authoritative resolver |
| `ExpectedVersion` | `int` | Да | Own optimistic token выбранного Draft | Transport per-item precondition carrier уточняется Q38; не публичное поле Version существующих DTO |
| `VersionKind` | Уточняется | Да | Различает schema/instance selection | Closed discriminator/wire encoding Q38; не определяется по Guid |

**Логическая application model `ReleasePreparationPlan`:**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Selection` | `IReadOnlyList<FreezeSelectionItem>` | Да | Подтверждаемые exact Draft inputs | Без duplicates и скрытого выбора конкурирующих Draft одного baseline/target |
| `PublishedVersionIds` | `IReadOnlyList<Guid>` | Да | Явно выбранные уже Published instance references | Могут быть empty; authoritative owner/schema resolution, no latest |
| `SchemaDependencies` | `IReadOnlyList<FreezeSelectionItem>` | Да | Exact Draft-схемы, необходимые выбранным instance Draft | Deduplicated dependency graph; каждый mutable target подтверждён пользователем |
| `ProductId` | `Guid?` | Условно | Контекст полного выпуска | Только для Product/Variant wizard; standalone freeze не зависит от Publishing |
| `VariantId` | `Guid?` | Условно | Контекст variant patch/revision | При выбранном Variant; current ownership/lifecycle F06 |
| `ReviewBasis` | Уточняется | Да | Связь review с exact входами, зависимостями и source releases | Формат/хранение/expiration Q38; own ETag не заменяет foreign basis |
| `Diagnostics` | Уточняется | Да | Поэлементные blockers/warnings и stage readiness | Адресация exact IDs/stage; DTO/issue codes Q38 |

**Логическая application model `ReleasePreparationItemResult`:**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InputVersionId` | `Guid` | Да | Exact исходный input | Связывает результат с выбором, не index списка |
| `PublishedVersionId` | `Guid?` | Условно | Подтверждённый immutable результат фиксации | Только подтверждённый commit; Publish сохраняет exact version identity по F03/F04 |
| `Outcome` | Уточняется | Да | Success/refused/not-started/unknown outcome | Exact result enum и продолжение Q38; unknown не false failure |
| `Diagnostics` | Уточняется | Да | Причины отказа/дальнейшие действия | Без full content/secrets; per-item и stage context |

### 3.6. Контракты schema selection

Имена ниже фиксируют целевой CLR/JSON контракт Q39 в `ContentManager.Publishing.Application.Abstractions`; это не inventory текущей сборки. JSON properties — camelCase, GUID D lowercase, массивы обязательны/non-null. Transport enums — строки, case-sensitive; numeric/unknown values →400. Domain enums/IDs маппятся явно, Domain assembly не становится зависимостью UI. `SchemaInstanceSelectionMode`: AllExcept/SelectedOnly; `SchemaAssignmentMode`: Inherit/AllExcept/SelectedOnly. Остальные закрытые enum values перечислены в полях ниже.

**SchemaContentSelectionDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaId | Guid | Да | Logical schema identity | Authoritative; immutable identity |
| SchemaVersionId | Guid | Да | Fixed Published structure | Не latest |
| Mode | SchemaInstanceSelectionMode | Да | Saved policy | String AllExcept/SelectedOnly |
| IncludedVersionIds | IReadOnlyList<Guid> | Да | Saved exact pins | Empty для AllExcept; unique logical owners |
| ExcludedInstanceIds | IReadOnlyList<Guid> | Да | Saved stable exclusions | Empty для SelectedOnly; не complement |

**SchemaSelectionIssueDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Code | string | Да | Stable machine code | Коды §5.3; не локализованный Message |
| Severity | SchemaSelectionIssueSeverity | Да | Error/Warning/Info | Closed string enum |
| Scope | SchemaSelectionIssueScope | Да | Save либо Release | Не смешивать SaveBlocker и ReleaseBlocker |
| SchemaId | Guid? | Условно | Affected schema | Null для whole-request issue |
| EntityInstanceId | Guid? | Условно | Affected instance | Заполняется для item issue |
| Path | string? | Нет | JSON Pointer candidate field | RFC6901 escaping; для global issue null |
| Message | string | Да | Читаемая причина | Не machine contract; UI может локализовать Code |

**SchemaSelectionSummaryDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| TotalCount | long | Да | Число logical identities полного scope | Включая retained missing refs; не page count |
| SelectedCount | long | Да | Включённые policy identities с chosen version | Включает BlockedCount; не обещает release readiness |
| ExcludedCount | long | Да | Не выбранные policy identities | SelectedOnly complement вычислен, не сохранён |
| NoPublishedForTargetCount | long | Да | Нет Published history в exact target | Может пересекаться с ExcludedCount |
| BlockedCount | long | Да | Selected identities с Release/Error | Подмножество SelectedCount; counts не суммировать как partition |

**SchemaSelectionGroupDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaId | Guid | Да | Logical group key | Unique в preview |
| SchemaVersionId | Guid? | Условно | Exact own/inherited target | Own selector всегда non-null; legacy mixed display может null |
| SchemaVersionNumber | int? | Условно | Published number структуры | Null при unavailable/mixed target |
| SchemaKey | string? | Нет | Display key | Current logical label, fallback frozen target metadata, иначе null |
| SchemaName | string? | Нет | Display name | Та же fallback policy; не identity |
| OwnMode | SchemaInstanceSelectionMode? | Условно | Собственная policy | Null в Inherit |
| Origin | SchemaSelectionGroupOrigin | Да | Inherited/Own/LegacyLocal/Mixed | Read-only provenance |
| IsInheritedSchemaExcluded | bool | Да | Inherited schema exclusion | False для Own selector |
| Summary | SchemaSelectionSummaryDto | Да | Whole-scope counts | До paging/search; read failure не zero |
| Issues | IReadOnlyList<SchemaSelectionIssueDto> | Да | Group diagnostics | Empty допустим |

**SchemaSelectionPreviewDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| BasisHash | string | Да | Fingerprint candidate + observed basis | 64 lowercase hex; не ETag и не разрешение Create |
| Groups | IReadOnlyList<SchemaSelectionGroupDto> | Да | Полный список schema groups | Ordinal SchemaId; не страницы экземпляров |
| Issues | IReadOnlyList<SchemaSelectionIssueDto> | Да | Whole-candidate diagnostics | Server authority |

**SchemaSelectionTreePageQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaId | Guid | Да | Запрашиваемая группа | Должна входить в candidate/inherited context |
| ExpectedBasisHash | string | Да | Basis ранее полученного preview | Mismatch →409; не подписанный security token |
| PageNumber | int | Нет | One-based page | Default 1; >=1 |
| PageSize | int? | Нет | Размер страницы | Null → DefaultPageSize; 1..MaxPageSize |
| Search | string? | Нет | Поиск Name/Key | Trim; case-insensitive literal substring; escape SQL wildcard chars |
| State | SchemaSelectionTreeState | Нет | All/Selected/Excluded/Blocked/NoPublishedForTarget | Default All; filter после resolution |

**SchemaSelectionTreeItemDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntityInstanceId | Guid | Да | Stable child identity | Не tree row index |
| Key | string? | Нет | Display key | Null при retained missing source |
| Name | string? | Нет | Display name | Null при unavailable metadata |
| SchemaVersionId | Guid? | Условно | Chosen version target | Own scope совпадает с fixed target |
| ChosenVersionId | Guid? | Условно | Exact выбранная/разрешённая версия | SelectedOnly pin либо AllExcept winner; null без Published |
| ChosenVersionNumber | int? | Условно | Номер chosen version | Не internal Version |
| LatestPublishedVersionId | Guid? | Условно | Max Published по target | Display candidate; не заменяет SelectedOnly pin |
| LatestPublishedVersionNumber | int? | Условно | Published number max candidate | Может отличаться от chosen pin |
| HasPublishedForTarget | bool | Да | Наличие Published history | Retired winner всё ещё имеет history |
| Disposition | SchemaSelectionDisposition | Да | Selected/Excluded/NoPublishedForTarget | Отдельно от eligibility |
| Origin | SchemaSelectionItemOrigin | Да | Product/VariantSchemaSelection/VariantAddition/VariantOverride/None | Не SDK field |
| IsEligible | bool | Да | Current new-use eligibility chosen result | False не вызывает skip/fallback |
| Issues | IReadOnlyList<SchemaSelectionIssueDto> | Да | Item reasons | Excluded item не effective blocker |

**SchemaSelectionTreePageDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| BasisHash | string | Да | Проверенный current preview basis | Равен ExpectedBasisHash при success |
| Page | OffsetPagedResultDto<SchemaSelectionTreeItemDto> | Да | Existing LeanSoft.Pagination.Offset result | totalCount/items по общей library; фильтр до paging; count ограничен MaxResolutionRows int |
| PageNumber | int | Да | Применённая страница | One-based |
| PageSize | int | Да | Применённый размер | Default раскрыт явно |
| HasNextPage | bool | Да | Есть следующие rows | Вычисляет сервер |

**ProductSchemaTreeQueryRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Candidate | UpdateProductContentAssignmentRequest | Да | Текущий generated whole candidate | Saved либо dirty; read-only use |
| Query | SchemaSelectionTreePageQuery | Да | Scope/page/filter/basis | Не читает только client-loaded children |

**SchemaSelectionConversionTarget**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Mode | SchemaAssignmentMode | Да | Inherit/AllExcept/SelectedOnly | Inherit только Variant |
| SchemaVersionId | Guid? | Условно | Желаемый exact target | Required для Own; null для Inherit |
| ExcludedInstanceIds | IReadOnlyList<Guid> | Да | Явные exclusions target AllExcept | Empty для иных modes; default не complement SelectedOnly |

**ProductSchemaConversionRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Candidate | UpdateProductContentAssignmentRequest | Да | Исходный whole candidate | Не mutation |
| SchemaId | Guid | Да | Изменяемая schema group | Остальные группы сохраняются |
| Target | SchemaSelectionConversionTarget | Да | Желаемый режим/target | При смене target чужие pins не переносятся молча |
| ExpectedBasisHash | string | Да | Основание conversion preview | Mismatch →409 preview-stale |

**SchemaSelectionConversionDiffDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AddedInstanceIds | IReadOnlyList<Guid> | Да | Новые selected identities | Полный scope, не page |
| RemovedInstanceIds | IReadOnlyList<Guid> | Да | Убранные selected identities | Не physical delete |
| ChangedVersionInstanceIds | IReadOnlyList<Guid> | Да | Смена exact выбранной версии | Видна до Confirm |
| RemovedLegacyRulePaths | IReadOnlyList<string> | Да | Competing rules, удаляемые candidate | JSON Pointers; Variant preview |
| ChangesFutureMembership | bool | Да | Меняется динамика появления новых instances | Например AllExcept → SelectedOnly |
| CanApply | bool | Да | Нет Save/Error в предлагаемом candidate | Release blockers показаны отдельно |

**ProductSchemaConversionDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Candidate | UpdateProductContentAssignmentRequest | Да | Предлагаемый generated request | Не сохранён на сервере |
| Preview | SchemaSelectionPreviewDto | Да | Полный результат conversion | Basis для дальнейшей локальной работы |
| Diff | SchemaSelectionConversionDiffDto | Да | Что пользователь подтверждает | Confirm только при CanApply |

**PublishingSelectionOptions**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| MaxSelectors | int | Да | Bound schema rules на assignment | Positive; Host ValidateOnStart |
| MaxExplicitInstanceIds | int | Да | Суммарные pins/exclusions/rules IDs | Positive; не silently truncate |
| MaxResolutionRows | int | Да | Bound authoritative scope resolution | Positive; проверить до materialization |
| MaxReviewBytes | long | Да | Bound serialized receipt evidence | Positive; no partial receipt |
| DefaultPageSize | int | Да | Tree default | 1..MaxPageSize |
| MaxPageSize | int | Да | Tree hard bound | Positive |
| ReviewLifetime | TimeSpan | Да | Lifetime технического review | Positive; ExpiresAt = CreatedAt + value |
| ReviewCleanupInterval | TimeSpan | Да | Период cleanup expired receipts | Positive; Host background service |
| ReviewCleanupBatchSize | int | Да | Bound технического delete batch | Positive; не business mutation |

Tree query использует full candidate и весь authoritative scope. Counts в Summary считаются до Search/State; TotalCount страницы — после фильтра. Key сортируется ordinal (PostgreSQL COLLATE "C"), null labels после известных; tie-break — lowercase Guid D. Scope resolution не читает instance JSON values/asset bytes. NoPublished rows и excluded rows не исчезают из дерева; absent metadata не снимает сохранённую identity. Ошибка обязательного read → classified error, не пустой успех. Эти DTO не являются wrapper для одной уже загруженной страницы.

Новые query types: `GetProductSchemaSelectionPreviewQuery` (ProductId, Candidate), `GetProductSchemaInstancesQuery` (ProductId, Candidate, Query), `PreviewProductSchemaConversionQuery` (ProductId, Request). Первое поле каждого — Guid route owner; Candidate/Query/Request имеют определённые выше типы. Query handlers расположены в Persistence и не сохраняют rule/receipt. Existing Update command принимает SchemaSelections, а не плоский array всех AllExcept results.

**GetProductSchemaSelectionPreviewQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Guid | Да | Route owner | Current access/context |
| Candidate | UpdateProductContentAssignmentRequest | Да | Whole generated candidate | Без mutation |

**GetProductSchemaInstancesQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Guid | Да | Route owner | Current access/context |
| Candidate | UpdateProductContentAssignmentRequest | Да | Whole candidate | Не только schema page |
| Query | SchemaSelectionTreePageQuery | Да | Scope/basis/page | §3.6 |

**PreviewProductSchemaConversionQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Guid | Да | Route owner | Current access/context |
| Request | ProductSchemaConversionRequest | Да | Full conversion request | Read-only |

**PublishedSelectionReferenceDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaId | Guid | Да | Logical schema | Authoritative |
| SchemaVersionId | Guid | Да | Exact structure | Published snapshot target |
| EntityInstanceId | Guid | Да | Logical instance | Stable ID |
| EntityInstanceVersionId | Guid | Да | Exact frozen version | No latest at read |
| VersionNumber | int | Да | Published number | Positive |
| ContentProfile | string | Да | Frozen content profile | Не exporter profile |
| ContentHash | string | Да | Frozen content digest | 64 lowercase hex |

Query pagination адаптирует PageNumber/PageSize к существующему offset paging pipeline с checked arithmetic; overflow/invalid bounds →400. Result Page использует уже применяемый LeanSoft.Pagination.Offset.OffsetPagedResultDto<T>, новый общий paginator не вводится. HasNextPage вычисляется из server result. Options bound MaxResolutionRows не превышает int.MaxValue.

## 4. Application

### 4.1. Handlers / use cases

Mutation handlers соблюдают порядок §7.2: shape/auth preparation → own ReadCommitted transaction → отдельный global gate statement → fresh tracked own roots + independent primary public facts → own token/basis/domain checks → own state/intents commit. Предварительные reads/CPU results не являются authority после ожидания gate.

`UpdateProductContentAssignmentHandler` и internal setup handler находятся в Publishing.Application. Query handler `GetProductContentAssignmentQueryHandler` — Publishing.EntityFramework.Persistence. Api не получает repository и не проверяет предметное родство версий.

Update handler загружает own root/items/token, проверяет ExpectedVersion и Product eligibility/F15 facts. Requested exact IDs, уже сохранённые в этом root, сопоставляются с trusted persisted references; unavailable enrichment не стирает IDs. Для остальных selected IDs batch ContentModeling resolution подтверждает authoritative IDs/current new-use. Handler строит candidate по EntityInstanceId, выявляет duplicates и классифицирует added/replaced/unchanged/removed; inactive/archived Product допускает только semantic Remove проблемной reference/eligible Replace проблемной reference без расширения. Успех вызывает `ReplaceItems`, фиксирует root Version/own changes в Publishing transaction и возвращает DTO + committed token. Нет автоматического retry пользовательского Save, source-catalog mutation, release или Delivery effect. Алгоритм DF02 раскрывает точку commit и races.

Internal setup идемпотентно обеспечивает один пустой assignment для существующего Product, отдельно от CatalogAccess commit. Duplicate delivery/setup race не очищает уже заполненный root. Конкретный recoverable trigger и поведение до завершения setup — Q20/DF03. Setup F07 не создаёт заодно policy, active-state и другие roots соседних фич без их отдельного контракта.

Query читает сохранённый working set и current display/availability facts через public boundaries. F15/source lifecycle blocker отображается отдельно, не превращает GET в mutation или пустой набор. Наличие Product и отсутствие assignment при задержке setup не дают право synthesize DTO+ETag, не соответствующие сохранённому owner.

### 4.2. Validators

[S18](../../../catalog/docs/standards/dotnet/fluentvalidation.md): `UpdateProductContentAssignmentRequestValidator` композирует ProductId/VersionId field validators, required collection и unique exact-ID rules; `UpdateProductContentAssignmentCommandValidator` использует те же pure constraints и internal expected-version profile. `ProductContentAssignmentConstraints.Items` — source ограничений, если предел набора будет принят; initial local Version grammar берётся общей convention.

Collection не null, может быть empty; каждый version ID non-empty. Duplicate exact ID — structural request issue; две разные версии одного logical instance обнаруживаются после authoritative resolution и отклоняются как domain composition conflict. Не молча Distinct incoming payload. FV не читает чужую БД, не меняет refs/Latest, не обращается к broker. Async existence/availability/blocker/guard facts проверяет handler.

### 4.3. Mappers

| Mapping | Ответственность | Правило |
| --- | --- | --- |
| Route/body + If-Match → command | Api | ProductId только route; expected int из strong tag; exact version IDs без metadata и body Revision |
| ContentModeling resolved facts → reference | Application | Все четыре IDs и owner/target подтверждены authoritative source; no Key matching |
| Saved root + captured display facts → result DTO | Application/read mapper | Представление ровно successful candidate; metadata/token одной mutation; не второй GetWorkspace после commit |
| Own persisted state → query DTO + ETag carrier | Persistence | Consistent root/items/token; unavailable ref показана с ID/issue по Q15, не удалена |

Mapping не строит release canonical JSON, не выполняет полевого merge и не пересчитывает hash выбранной Published version. Недоступность post-commit read enrichment не должна превращать successful save в скрытый повтор use case; необходимую response metadata собирают до commit/из сохранённого результата. Если ответ потерян после commit, это unknown outcome, а не rollback.

### 4.4. Services / algorithms

`AssignmentReferenceResolver` выполняет batch exact resolution и возвращает facts по каждому input ID. `AssignmentChangeClassifier` сопоставляет candidate/current по EntityInstanceId и exact version ID: new logical position, replacement, unchanged, removed. Unchanged означает совпадение всех сохранённых exact reference IDs; display reorder не new use. Новая позиция/замена проходят полную текущую eligibility, unchanged unavailable остаётся с ReleaseBlockers. Removed items не требуют автоматически восстановления их source versions ради удаления из working set; Remove проблемной reference и eligible Replace проблемной reference разрешены для inactive/archived owner без Add/expansion; reference-release writes используют MVP gate §7.2; representation retention facts остаётся Q20. OPEN F15 block не обходится maintenance.

`AssignmentWriteGuards` читает и согласует ContentModeling new-use facts, catalog eligibility и F15 blockers. Plain read checks недостаточно: параллельный Archive/Retire/Withdrawal/delete/move после проверки не должен обходить согласованный invariant. MVP gate §7.2 сериализует все invalidating writers; boundary facts carrier остаётся Q15; не подменять его прямым `AdvanceRevision` чужих roots или общей SQL transaction всех BC.

ETag относится к assignment целиком. Только изменение release/policy/activation не должно ложно делать assignment stale, если его набор не менялся. F09 использует assignment If-Match при создании release и получает один consistent set/version. Ни новое content Publish, ни Product Name change не повышают assignment Version автоматически.

### 4.5. Orchestration массовой подготовки

Application.Abstractions владеет нейтральными selection/plan/result моделями выше и public ports; Domain не получает batch HTTP DTO. Preflight read handlers находятся в соответствующих Persistence read adapters и композируют exact snapshots через public boundaries. Mutating coordinator располагается в Application; он вызывает F03/F04 public use cases, затем F07/F08 и F09/F10, не репозитории/DbContext другого BC. Standalone ContentModeling freeze не зависит от Publishing coordinator.

Алгоритм: exact inputs/tokens → batch resolution всех dependencies и strict diagnostics → reviewed plan → explicit confirm → повторная basis/token/lifecycle проверка → schema stage F03 → instance stage F04 → полный поэлементный результат → только при полном успехе Publish-stage assignment/release/revision. Первая конфликтующая стадия закрывает последующие stage gates; успешно зафиксированные snapshots сохраняются, нет unpublish/компенсации изменением immutable history. Подтверждённый schema Publish меняет ожидаемый Draft dependency на собственный exact Published result; это единственное запланированное изменение basis, не разрешение принять параллельное редактирование.

Каждая операция имеет собственный UoW/guards/commit и требует собственный текущий token; cross-BC транзакция и публикация по старому preflight запрещены. Lost response выясняется по exact state/operation result до продолжения. Api связывает новый transport с command/query после решения Q38 и публикует OpenAPI для NSwag; endpoint/method/job storage/operation IDs сейчас `Уточняется`. EF/Persistence сохраняет только owner changes существующих операций; новый durable workflow store не подразумевается до выбора execution protocol Q38. Host связывает public ports/coordinator и validators; новые DI names не выдаются за готовый код.

Приёмка orchestration: real owner tokens/schema-first ordering, concurrent edit/base/lifecycle invalidation, per-item partial commit и unknown outcome, отсутствие assignment/release/activation после failed freeze stage, explicit exact source selection, no Publish-on-Save/no latest. Компонентные mocks не доказывают меж-BC recovery.

### 4.6. Сохранение schema rules и разрешение состава

Application handler получает whole SchemaSelections с own If-Match, нормализует только согласованные wire forms, валидирует режим и несовместимые списки через reusable validators и batch-разрешает exact schema targets/pins/exclusion identities. Затем проверяет Catalog/F15 guards, retained versus new-use и атомарно вызывает ReplaceSelections в own Publishing UoW. AllExcept expansion не сохраняется как скрытый список pins; Save не создаёт release. Для Archived maintenance нельзя расширить policy с SelectedOnly до AllExcept под предлогом равенства сегодняшнего count: будущая семантика тоже расширяется.

Application.Abstractions определяет нейтральные rule/input/read-resolution contracts; Domain владеет SchemaContentSelection и pure invariants. EntityFramework сохраняет owned schema rules с unique owner+SchemaId, fixed SchemaVersionId/mode и дочерними exact inclusions/stable exclusions. Logical names не являются утверждёнными DDL: mappings/index/legacy conversion — Q39. Сохранённый selector, its exact target и pins участвуют в reference/retention checks; удаление working rule не снимает historical retention.

EntityFramework.Persistence размещает query handlers: читает saved rules/own token согласованно и батчами получает ContentModeling-owned projections. ContentModeling adapter для AllExcept группирует Published history по EntityInstanceId в exact SchemaVersionId и выбирает maximum VersionNumber; exclusions применяются до effective dependency validation. Draft-only owners показываются отдельно, недопустимый winner — blocker, не silent fallback/skip. UI tree раскрывает children серверными страницами; parent counts/eligibility/basis нельзя вычислять по одной загруженной странице. Для SelectedOnly authoritative exact pins не обновляются этим resolver.

Host регистрирует SchemaSelectionResolver, ContentModelingSchemaSelectionQueries, SchemaSelectionEvidenceWriter, validators §4.7 и query handlers §3.6 по существующим owning modules; options bind Publishing:Selections + ValidateOnStart. API maps generated whole-rule models, не берёт чужой Domain. Live resolution может измениться без Save assignment: own ETag относится к rule, publication basis — к наблюдаемому universe. F09 обязан защитить эту границу до commit, включая phantom новых owners/versions. Unit проверяет policy lists/scope/uniqueness и отсутствие implicit conversion; real PostgreSQL/HTTP checks — scope resolution, no fallback, whole-set concurrency и сохранение правил; bUnit сам по себе этого не доказывает. Имена новых тестов — MethodUnderTest_Scenario_ExpectedBehavior.

### 4.7. ContentModeling resolution и проверка контракта

`SchemaSelectionResolver` в Publishing Application координирует public ContentModeling publishing queries, но не получает его DbContext/Domain roots. `ContentModelingSchemaSelectionQueries` в ContentModeling.EntityFramework.Persistence выполняет batch projections по заданным exact targets, owners и pins. Specifications ограничивают SchemaVersion/SchemaId/IDs; SQL projection выбирает winner без загрузки values graphs. Числовые guardrails проверяются до полной materialization, cancellation проходит до DB reads.

Важно: выбирать maximum нужно по **истории публикации**, а не предварительно отфильтрованному eligible Status. В текущем source modern Published number хранится в `published_version_number`; retirement/archive не стирает факт уже выполненной публикации. Для modern rows source query имеет вид DISTINCT ON(entity_instance_id) ... WHERE schema_version_id=@target AND published_version_number IS NOT NULL ORDER BY entity_instance_id,published_version_number DESC. Затем валидируются lifecycle/integrity/profile/hash и current guards. WHERE status='Published', !Archived либо media.IsEligible до maximum запрещены: они могли бы молча вернуть старую version. Legacy/неоднозначная publication metadata в том же scope выявляется отдельным read и даёт source/integrity blocker, а не исчезает из universe или угадывается как Draft.

Нужен ContentModeling-owned read index `ix_instance_versions_schema_published_winner` на `content_modeling.entity_instance_versions(schema_version_id,entity_instance_id,published_version_number DESC)`, partial WHERE published_version_number IS NOT NULL, INCLUDE(id,status,hash,canonical_profile_id,published_at,version). Его migration/model принадлежит ContentModeling, не Publishing EF. Existing owner index(schema_id,key) применяется для tree universe; поиск escaped literal substring реализуется общим read-search adapter. План/representative fixtures должны подтвердить bounded scans/отсутствие N+1. Новые statuses/legacy formats не добавляют fallback веток без profile decision.

Exact pins SelectedOnly читаются отдельно по requested version IDs и не заменяются winner projection. Excluded AllExcept IDs сохраняются как rule intent; их item/media guards не входят в final effective dependency closure. Schema target и current catalog/parent facts всё равно проверяются. Source failure не подменяется пустым scope. F09 строит Release evidence под общим Q20 gate; standalone preview uses наблюдённый read basis и не становится commit permit.

`PublishingSchemaSelectionJsonContract.Configure` в Publishing.Api добавляет converter только для определённых здесь enum types. Converter принимает только JSON string с точным enum spelling, отвергает numbers/unknown/case mismatch и не меняет JSON других BC. OpenAPI описывает string enum/required arrays/nullable fields/error codes; NSwag regenerates клиент обычным GenerateApiClient после фактического API change. FluentValidation validators: SchemaContentSelectionInputValidator, SchemaSelectionTreePageQueryValidator, ProductSchemaConversionRequestValidator; Domain SchemaContentSelectionConstraints повторно обеспечивает uniqueness/mode lists/nonempty IDs. Foreign ownership/eligibility не проверяются сетевыми вызовами из property validator — их делает application resolver. Host registration оформляется в existing Api/Application/EF modules, новых packages нет.

Тесты wire contract обязаны отправлять raw JSON с numeric/unknown/wrong-case enum, null/missing required arrays и duplicate IDs; generated-client round trip не заменяет эти проверки. Golden fixtures profile cm-publishing-selection-basis/1 хранят literal expected bytes/digest и перестановки входных arrays. Real PostgreSQL tests проверяют max modern publication при Retired/Archived winner, unsupported legacy metadata, no fallback, Key/ID paging, counts до фильтра, stale page и materialization bounds. Test names — MethodUnderTest_Scenario_ExpectedBehavior, AAA и собственные IDs/cleanup.

## 5. Api

Schema-first reads/writes используют модели §2/§3. Существующий flat Items/AssignmentItemInput не становится полноценным selector API через UI grouping. Wire/enum/tree paging определены §3.6/§5.3, storage/migration — §6 и F09; [технический Q39](../questions/Q39-schema-content-selection.md). OpenAPI и NSwag реализуются совместно.

### 5.1. Endpoints

| Method | Route | Request | Response | Access |
| --- | --- | --- | --- | --- |
| GET | `/api/v1/products/{productId}/content-assignment` | Route ProductId: Guid | `ApiResponse<ProductContentAssignmentDto>` + own strong ETag, 200 | MVP Specialist read policy; exact permission/isolation Q15/Q10 |
| PUT | `/api/v1/products/{productId}/content-assignment` | Route ProductId, required If-Match, UpdateProductContentAssignmentRequest | `ApiResponse<ProductContentAssignmentDto>` + resulting own ETag, 200 | MVP Specialist assignment write policy; Q15/Q10, F15/new-use guards |

Target routes приняты прежней F07. HTTP создающий assignment/source Product, per-item mutations и publish-on-save не вводятся. Internal setup DF03 не является внешним API. Picker DF04/DF05 использует F04 list endpoints; F06 exact Product предоставляет read-only header workspace. F07 response не включает все releases/policy/history/activation как current PublicationWorkspaceDto.

### 5.2. ETag / If-Match

[S16](../standards/S16-rest.md): exact GET возвращает strong numeric ETag assignment из внутреннего int Version. PUT существующего ресурса требует If-Match; Api parses его в command.ExpectedVersion. Local Version не входит DTO/OpenAPI/body/query. 428 при отсутствии, 412 при stale tag, 409 при domain/catalog/lifecycle conflict. Malformed/weak/wildcard/multiple tags и error priority — общий profile Q15; не принимать всё как «достаточный любой заголовок».

Successful state-changing PUT атомарно увеличивает own Version и возвращает DTO + новый ETag без hidden result GET. Токены Product catalog, EntityInstanceVersion picker и ProductPublicationState непригодны для этого Save. Реальная optimistic race после первоначальной проверки классифицируется после rollback/disposal через fresh own-root state: own expected mismatch → 412; external eligibility/blocker race не выдается за stale assignment. MVP gate §7.2; no-op/metadata/error profile Q15/Q20.

412 не разрешает автоматически повторить whole-set PUT с новым token и overwrite чужого набора. Lost response после commit: клиент может явно получить exact assignment и сравнить с intent, сохранив свои edits; равенство состава не доказывает авторство попытки. Idempotency/correlation для дополнительного доказательства не выбраны Q15.

### 5.3. OpenAPI и ошибки

ProducesResponseType описывает 200 envelope и ETag headers, 400 ValidationProblemDetails, 404 missing Product/assignment/reference, 409 composition/lifecycle/F15 blocker, 412 и 428 ProblemDetails; auth 401/403 по MVP profile. Missing exact item ID — error привязанный к reference; нельзя отдавать partial success Save. Error machine codes/path/details и setup-pending mapping находятся Q15/Q20, а не копируются из текущего shared ArgumentException.

Отдельные stable codes для own stale, unavailable ref и catalog conflict позволяют UI различать reload, замену выбора и переход F15. GET existing unavailable references не становится empty или автоматически 409; authoritative readonly diagnostics сохраняют выбранные IDs. Header access/CORS exposed headers и generated response carrier согласуются Q15; NSwag generation после проверки реального backend OpenAPI, без ручного generated-кода.

### 5.3. Schema selection HTTP и ошибки

Product prefix `/api/v1/products/{productId}/content-assignment`; Variant prefix `/api/v1/products/{productId}/variants/{variantId}/content-assignment`. Full scope/applied parent/access guards сохраняются. Content-Type application/json, camelCase, Cache-Control: no-store на dynamic preview/review/tree. GET/PUT assignment сохраняют own strong ETag; его нельзя использовать как validator dynamic preview. Success envelope — существующий ApiResponse<T>; ошибки — ProblemDetails/ValidationProblemDetails S16 с existing validation key profile. JSON Pointer относится к SchemaSelectionIssueDto.Path; общий ModelState contract других endpoints не меняется.

| Method | Path suffix | Body | Success |
| --- | --- | --- | --- |
| GET | пустой | Нет | 200 saved owner DTO + own ETag; scoped preview при доступном resolution |
| PUT | пустой | Product Update request либо Variant Update request | 200 saved owner DTO + resulting own ETag; If-Match required |
| POST | /preview | Тот же whole candidate request, без Save | 200 ApiResponse<SchemaSelectionPreviewDto> |
| POST | /schema-instances/query | ProductSchemaTreeQueryRequest либо VariantSchemaTreeQueryRequest | 200 ApiResponse<SchemaSelectionTreePageDto> |
| POST | /conversion-preview | ProductSchemaConversionRequest либо VariantSchemaConversionRequest | 200 соответствующий conversion DTO |
| POST | /release-review | F09 review request + assignment If-Match | 200 F09 review DTO; техническая receipt только при CanCreate=true |
| POST | /release | F09 Create request + source assignment If-Match | 201 release DTO, own release ETag и Location exact release |

Preview/tree/conversion не обязаны иметь mutation If-Match; current saved generation входит в BasisHash и concurrent Save делает старые tree pages stale. Variant candidate содержит собственный SchemaSelections плюс legacy groups; generated Product DTO нельзя использовать для Variant body. Endpoints принадлежат существующим source assignment Api adapters; они dispatch query/command и не содержат алгоритм resolver.

| Code | HTTP | Условие / результат |
| --- | --- | --- |
| validation.failed | 400 | Existing S16 shape/unknown enum/mode-list combination/duplicate IDs/schema groups; стандартные field validation keys |
| schema-selection.scope-mismatch | 409 | Pin/exclusion/target принадлежит другой schema/structure |
| schema-selection.legacy-rules-conflict | 409 | Own selector конкурирует с addition/override/inherited rule той же schema |
| schema-selection.preview-stale | 409 | ExpectedBasisHash не current; discard cached child pages, перечитать preview |
| schema-selection.resolution-limit-exceeded | 409 | Full authoritative scope превышает configured bound; no partial result |
| schema-selection.reference-unavailable | 409 | Недопустимое новое использование exact target/pin; unchanged retention обрабатывается отдельно |
| schema-selection.source-invalid | 409 | Published history/profile/integrity неоднозначны или повреждены; не silent skip/fallback |
| release-review.invalid-token | 400 | Token не canonical base64url 32 bytes |
| release-review.expired | 409 | Review отсутствует/истёк; fresh review required, confirmed replay проверяется раньше |
| release-review.context-mismatch | 409 | Actor/owner/validation context не совпадает с receipt |
| release-review.stale | 409 | Fresh protected rule/publication/guard basis изменился |
| release-review.legacy-source | 409 | Новый F10 Create пытается использовать legacy release format; подготовить новый release |

413 request.too-large — существующий HTTP body bound; 401/403 — current access, 404 — missing scoped resource, 412/428 — own If-Match, 503 — required dependency read/gate failure. Preview может вернуть 200 с issue Error для структурно корректного, но не готового к release candidate; это не успешный Create. MaxReviewBytes проверяется до receipt insert; при превышении используется schema-selection.resolution-limit-exceeded и нет half receipt. Никакой новый код не превращает operational failure в empty valid set.

## 6. EntityFramework

### 6.1. Schema selection storage

`SchemaContentSelectionConfiguration` — owned mapping: `OwnsMany` SchemaSelections, reference IDs через существующий HasGuidId, mode через string conversion, collections mapped явно. Domain Exclusions использует owned VO `SchemaInstanceExclusion(EntityInstanceId)`; wire ExcludedInstanceIds остаётся GUID array. Physical row contracts ниже описывают columns, включая EF shadow scope, а не новые публичные DTO.

**SchemaSelectionRow**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| OwnerId | Guid / uuid | Да | Physical owner FK | Product: product_content_assignment_id; Variant: product_variant_content_assignment_id |
| SchemaId | Guid / uuid | Да | Logical rule identity | Non-empty; PK вместе с owner |
| SchemaVersionId | Guid / uuid | Да | Exact target | Non-empty; no foreign BC FK |
| Mode | string / text | Да | Stored enum | CHECK IN ('AllExcept','SelectedOnly') |

**SchemaSelectionIncludeRow**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| OwnerId | Guid / uuid | Да | Owner scope | FK в selector |
| SchemaId | Guid / uuid | Да | Logical schema scope | FK в selector |
| SchemaVersionId | Guid / uuid | Да | Authoritative exact target | Composite scope FK в selector |
| Mode | string / text | Да | Parent mode evidence | CHECK = 'SelectedOnly' |
| EntityInstanceId | Guid / uuid | Да | Logical pin identity | Non-empty; unique на owner |
| EntityInstanceVersionId | Guid / uuid | Да | Exact pin | Non-empty; unique на owner |

**SchemaSelectionExcludeRow**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| OwnerId | Guid / uuid | Да | Owner scope | FK в selector |
| SchemaId | Guid / uuid | Да | Logical schema scope | FK в selector |
| SchemaVersionId | Guid / uuid | Да | Parent target evidence | Composite scope FK |
| Mode | string / text | Да | Parent mode evidence | CHECK = 'AllExcept' |
| EntityInstanceId | Guid / uuid | Да | Excluded stable ID | Non-empty; не version ID |

Physical tables: Product — `publishing.product_content_assignment_schema_selections`, `product_content_assignment_schema_includes`, `product_content_assignment_schema_excludes`; Variant — те же suffixes с prefix `product_variant_content_assignment_`. Owner FK соответствует owning root. Selector PK(owner_fk,schema_id), alternate UNIQUE(owner_fk,schema_id,schema_version_id,mode); include PK(owner_fk,schema_id,entity_instance_id), UNIQUE(owner_fk,entity_instance_id), UNIQUE(owner_fk,entity_instance_version_id); exclude PK(owner_fk,schema_id,entity_instance_id). Children FK(owner_fk,schema_id) → selector ON DELETE CASCADE. Дополнительный composite scope FK(owner_fk,schema_id,schema_version_id,mode) → selector alternate key DEFERRABLE INITIALLY DEFERRED добавляется явным migration SQL: на commit он не допускает child другого target/mode. EF ownership остаётся natural owner+schema; SQL constraint не создаёт navigation в ContentModeling.

Indexes: selector(schema_version_id,owner_fk), include(entity_instance_version_id,owner_fk), include(entity_instance_id,owner_fk), exclude(entity_instance_id,owner_fk). Все UUID columns имеют non-empty CHECK; backend подтверждает реальную принадлежность через public resolver, SQL не делает cross-BC FK. Surrogate row IDs/индивидуальные ETags child не вводятся. Constraint names используют короткие prefixes pca/pvca + sel/inc/exc + pk/fk/uq/ck, фиксируются migration/model одинаково; фактические определения проверяются PostgreSQL integration, не только in-memory mapping.

Whole-set replacement выполняется в одной owner transaction: root conditional write защищает Version, EF reconcile учитывает natural keys, children удаляются до вставки несовместимого mode; deferred scope FK проверяется при commit. При unchanged selector не delete/reinsert ради выдачи нового row identity. При смене target не допускается промежуточный partial commit. Database error/412 откатывает все root/child изменения. Tests обязаны проверить AllExcept↔SelectedOnly, same-ID pin replacement, empty↔nonempty, rollback и HasPendingModelChanges=false; не предполагать, что immutable collection replacement сам решает EF identity conflicts.

### 6.1. DbContext ownership

`PublishingDbContext`, schema `publishing`, владеет ProductContentAssignment/items. F09 releases и F10/F11 roots находятся в том же BC, но Save F07 их не меняет. Cross-BC FK и чтения/записи CatalogAccess/ContentModeling DbContext отсутствуют в target; facts доступны через ports.

Существующий [PublishingDbContext](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/PublishingDbContext.cs) — реальный mapping boundary. Наличие всех groups в одном context не означает общий concurrency owner или разрешение менять все groups при Save. Queries AsNoTracking; commands tracked.

### 6.2. Configurations

| Entity | Table | Key/index | Concurrency | Mapping |
| --- | --- | --- | --- | --- |
| ProductContentAssignment | `publishing.product_content_assignments` | ProductId PK target; один root на Product | Own `version: int` EF concurrency token | Stable Guid ID ValueGeneratedNever; optional updated_at по audit profile |
| PublishedInstanceVersionReference | `publishing.product_content_assignment_items` | `(product_id, entity_instance_id)` unique membership/PK | Защищён root.Version | owner FK только внутри Publishing; required instance_version_id/schema_id/schema_version_id Guid; no cross-BC FK |

Current mapping [ProductContentAssignmentConfiguration](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/ProductContentAssignmentConfiguration.cs) имеет surrogate assignment Id и unique ProductId; own Version не задан. [PublishingOwnedMappings](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/PublishingOwnedMappings.cs) хранит row_id, string Keys/hash/order и uniqueness по schema/instance Keys. Target unique stable EntityInstanceId и own Version требуют согласованной migration Q20; сохранение surrogate internal key возможно только как physical implementation с той же однозначной semantic identity, не новый HTTP resource.

### 6.3. Constraints / indexes

One root per Product, unique item EntityInstanceId within root. В root/item reference IDs required/non-empty по constraints. Owner child FK и cascade при согласованном удалении root — только локальная целостность; HTTP delete assignment в F07 отсутствует. Никаких FK к source Product, ContentModeling versions или assets.

Root Version guard обязан срабатывать при добавлении/замене/удалении всех child rows, включая Save пустого набора; одним concurrency token чужого state или child auto row_id это не обеспечивается. Замена коллекции не должна приводить к transient duplicate key из-за EF delete/insert order: mapping/change tracking/SQL order проверяются реальным PostgreSQL. Exact check/index names, initial Version, overflow и no-op profile — Q20. No arbitrary MaxCount, required order или Key unique business rule из legacy mapping.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

Host/DI задаёт owner-partitioned UoW participants: в transaction входят только DbContext/own outbox/inbox данного BC. Текущий EfCoreUnitOfWorkParticipant, способный объединять несколько DbContexts в общей transaction, не является target coordination adapter: participant selection разделяется по owner BC, foreign public read adapter создаёт независимый primary scope/connection без ambient enlistment. Guard adapter использует owner connection/transaction; mapping/repository другого BC в command handler не внедряется.

Target `ProductContentAssignmentsRepository` читает root/items по ProductId tracked для mutation и Add internal setup; external source presence определяется public port. `GetProductContentAssignmentQueryHandler` проецирует own set/token, enriches requested IDs через ContentModeling read port и возвращает internal metadata. Не repository на каждый child, не handler в Api/App universal workspace service.

Publishing public reference/dependency adapters предоставляют ContentModeling и F15 необходимые stable-ID facts через accepted boundary. Existing [ContentReferenceGuard](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/ContentReferenceGuard.cs) читает assignments/releases для reference checks и MediaLibrary для assets. Его direct adapter placement/shared provider не подтверждает выбранный target coordination protocol.

### 7.2. Transactions

По [MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) каждый invalidating writer начинает **свою** BC transaction с явно заданным ReadCommitted и отдельным первым SQL statement получает один стабильный глобальный exclusive pg_advisory_xact_lock. Lock выполняется на том же открытом owner connection, на котором будут authoritative reads, row locks, запись и commit; общий ключ одинаков во всех репликах CM. До получения gate нельзя authoritative-read/check/row-lock затронутый basis. Затем тот же свежий owner scope на удерживающем gate connection загружает собственные tracked roots и проверяет expected Version; чужие current facts читает только через public read ports в независимом primary connection/scope, без ambient UoW enlistment, foreign EF, shared SQL transaction, cache или read replica. Публичный read carrier остаётся предметом Q15; механизм сериализации уже выбран.

Все способные инвалидировать проверяемый basis writers используют тот же gate: F07 whole Save/setup, F08 patch Save/setup, ContentModeling/Media lifecycle/reference changes, CatalogAccess applied relation/F15 Resolve, F09 CreateRelease/Retire/Restore, F10 CreateRevision/lifecycle/Withdrawal, F11 Select/Activate/Rollback/Unpublish и promotion capture/per-item/stage/late Approval, F12 policy changes, F15 scoped repair, релевантные F17 decisions и Delivery-owned invalidation commits. Каждый изменяет только own BC state. Own state/token, required own outbox, safe audit intent и inbox marker фиксируются атомарно; commit/rollback освобождает transaction lock. Это общий write gate в одной PostgreSQL database между репликами, а не общая транзакция BC.

CPU-подготовка, immutable projection/canonical hashing и предварительные проверки могут быть снаружи gate, но после acquisition весь влияющий на commit basis заново подтверждается до применения подготовленного результата. Gate нельзя удерживать во время UI/человеческого Approval, VR School HTTP, S3/bytes, broker send/ACK, ожидания Delivery projection либо HTTP stream; эти действия и downstream commits имеют отдельные границы.

Acquisition wait ограничен выбранной конфигурацией и cancellation; timeout/cancel/lock failure — технический отказ, не domain Conflict. Failed/partial public port не превращается в empty compatible facts. Потеря owner connection прерывает операцию целиком; reconnect не продолжает прежний basis. Own expected Version mismatch остаётся 412; independent relevant basis drift — 409 с повторной проверкой/review, без автоматического retry с новым ETag. Потерянный commit ACK означает unknown outcome: inquiry по durable operation identity/выбранному Q15 recovery, а не утверждение rollback или слепой повтор.

Read queries, pickers, preview/review не получают этот global gate и не выдают commit permit. Own DTO и token читаются согласованно из одного owner поколения; composite basis/freshness других roots учитывается отдельно. Последующая mutation получает новый gate и перепроверяет basis.

F07 gate покрывает весь DF02 ReplaceItems и DF03 idempotent setup. Own assignment Version проверяется после gate; new-use/retained classification и CatalogAccess/F15 basis вычисляются по свежим facts. Setup также читает current Product public fact под gate и фиксирует только один own empty root/intent/inbox; его trigger/readiness/recovery остаются Q20.

Publishing local transaction фиксирует whole new assignment items, own Version и audit fields при наличии. Structural/reference/blocker failure до commit → нет частичного replacement. Own stale race → rollback всех child changes; response 412 после правильной classification. External guard failure → rollback и соответствующий 409/операционный error; не считать его safe facts.

ContentModeling/CatalogAccess/MediaLibrary записи не объявляются частью Publishing UoW. MVP gate §7.2 защищает lifecycle/new-use/reference basis; source retention representation/recovery остаётся Q20, отдельная reservation вместо gate не выбирается. Atomicity нескольких DbContexts в текущем App service не переносится как target cross-BC transaction.

GET/metadata enrichment не сохраняет owner. Saved DTO/new token получают из commit result/captured reference facts, не из нового GetWorkspace: поздняя чужая mutation не должна быть приписана текущему Save. Ошибка связи после commit оставляет committed working state; это unknown outcome. F09 чтение source assignment + immutable snapshot в Publishing должно видеть один защищённый set/version, F07 не изменяет уже созданный release.

### 7.3. Migrations

По Q20 production legacy data отсутствуют; допускается согласованное изменение physical identity/layout. В этой аналитике migrations не исполняются. Реализация добавляет assignment own int Version, меняет Key-based unique на stable-ID membership, отделяет root concurrency от ProductPublicationState.Revision, приводит item reference/audit fields и child mapping к contract.

Общий `PublishingOwnedMappings` используется также F08/F09: его нельзя целиком переписать как побочный F07 cleanup. Scope migration делится по владельцам, сохраняет immutable release/revision смысл, проверяет constraints и initial concurrent setup. Новая `.EntityFramework.Persistence` регистрирует repositories/query handlers; текущий [PublishingEntityFrameworkModule](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/PublishingEntityFrameworkModule.cs) регистрирует DbContext, а [ContentManagerApplicationModule](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs) связывает IPublishingService с App service. DI target должен подключать один command/query path, а не две конкурирующие реализации.

## 8. External adapters

### 8.1. Публичные ContentModeling contracts

Адаптер `IContentModelingPublishingQueries` разрешает batch exact Published references и даёт stable ownership/target, display/lifecycle/guard facts. Истина принадлежит ContentModeling. Он не возвращает tracked Domain root или content graph ради строки picker. Required asset/content availability при new-use входит authoritative profile F04/F05/Q07; F07 не загружает/downloads байты, не создаёт parallel MediaLibrary guard и не вычисляет hash заново.

Обратный Publishing reference-use boundary позволяет проверить working/historical dependencies перед удалением/архивированием по правилам F04/F09. Release retention не сводится к текущему assignment: удаление рабочего item не снимает historical refs автоматически. Transport этого boundary не назначается HTTP/broker без решения Q20.

### 8.2. Публичные CatalogAccess contracts

Адаптер `ICatalogPublishingQueries` читает Product existence/source eligibility и F15 block для конкретного scope/action. Missing Product отличается от present inactive/pending/blocking conflict. Source ProductId не меняется Save. Guard с concurrent Variant move/F15 resolution обеспечивается MVP gate §7.2; CatalogAccess DbContext внутрь Publishing не передаётся.

### 8.3. Internal initialization и Host

Internal setup использует выбранный recoverable trigger Q20 после появления Product; handler работает локально в Publishing. Event/inbox/outbox применяются только если реально выбран соответствующий механизм. ProductId обеспечивает повторяемость создания; никакой новой source identity, bootstrap import или unconditional reset существующего набора. Broker queue/event name и raw HTTP callback не изобретаются F07. Server clock для UpdatedAt переиспользует существующий TimeProvider.

## 9. Data flows

### Транспортные потоки

По [S17](../../../catalog/base/docs/standards/data-flows.md), models §2–3. F04 candidate reads перечислены как переиспользуемые внешние interactions, не новые assignment endpoints. Initialization internal, его transport не выбран.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Прочитать working assignment | HTTP API | GET | `/api/v1/products/{productId}/content-assignment` | MVP assignment read Q15/Q10 | ProductId | GetProductContentAssignmentQuery | ApiResponse assignment DTO + own ETag | 200 сохранённого состава | 404 missing owner/root, setup profile Q20; 401/403; operational enrichment failure |
| DF02. Сохранить полный working set | HTTP API | PUT | `/api/v1/products/{productId}/content-assignment` | MVP assignment write Q15/Q10; eligibility/F15 guards | ProductId, exact version IDs, If-Match | UpdateProductContentAssignmentCommand | Result DTO + resulting own ETag | 200 после Publishing commit | 400,404,409,412,428;401/403; dependency/storage failure |
| DF03. Обеспечить empty assignment | Internal setup; механизм Q20 | EnsureProductContentAssignment | Publishing.Application; durable trigger/destination Q20 | Подтверждённый existing Product fact; internal context | ProductId, выбранная trigger identity | EnsureProductContentAssignment | Existing/created root; не public response | Один root после local commit; повтор no-op | Missing source fact, initialization race/recovery, storage failure |
| DF04. Найти logical content | HTTP API, reuse F04 DF01 | GET | `/api/v1/entity-instances` | MVP ContentModeling read Q10/F04 | Search/schema/filter/page | ListEntityInstancesQuery F04 | ApiResponse offset summaries | 200 server page | Validation/access/storage по F04; empty нормален |
| DF05. Выбрать exact Published version | HTTP API, reuse F04 DF04 | GET | `/api/v1/entity-instances/{instanceId}/versions` | То же | InstanceId, Published/state/target/page filters F04 | ListEntityInstanceVersionsQuery F04 | ApiResponse offset version summaries | 200; пользователь выбирает exact ID | Missing/invalid owner/filter/access по F04; unavailable selection не authority для Save |

### DF01. Exact read без изменения working set

| Шаг | Исполнитель | Вход / чтение | Преобразование и проверка | Запись / выход |
| --- | --- | --- | --- | --- |
| 1 | Api | ProductId, trusted read context | Binding/access; нет creation-on-read | Query |
| 2 | Publishing query adapter | Own root/items/Version через ProductId specification | AsNoTracking; missing root не маскировать empty default token; consistency set/token | Saved stable refs + internal own token |
| 3 | Public read adapters | Requested content IDs, Product/F15 read facts | Batch labels/availability; present inactive Product и refs отличаются от missing; read blockers не скрывают состав | Display/issue facts по Q15 |
| 4 | Mapper/Api | Saved set и facts | DTO без Version; own Version → ETag | 200 representation + header; no Save/commit/new release |

Изменения availability между read и Save допустимы как observable stale knowledge; последующий command revalidates. Read не выполняет automatic latest selection/cleanup, не возвращает content из release вместо assignment и не использует workspace state revision как ETag. Если dependency enrichment недоступен, политика degraded labels/error Q15; IDs уже сохранённого состава не теряются.

### DF02. Whole-set Save и конкурентные проверки

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

| Шаг | Исполнитель | Вход / чтение | Преобразование и проверка | Запись / выход |
| --- | --- | --- | --- | --- |
| 1 | Api/FV | Route/body/If-Match | Required strong tag parsing; non-null collection, IDs/duplicates; no body Revision/metadata | Command + expected own Version; 428/400 без writes |
| 2 | Application/repository | Assignment/items/current Version | Проверить owner/root существование и own expected token; stale → 412, не overwrite | Сохранённый набор для delta и concurrency guard |
| 3 | Catalog public port | Product identity/current eligibility/F15 blocks | Оценить конкретную mutation; normal blocker → 409; unavailable port не safe result | Authoritative write facts/guard context |
| 4 | ContentModeling public port | Requested exact IDs; matched persisted references + remaining new IDs | Batch authoritative resolve новых IDs; New/replaced IDs → authoritative exact resolution/current eligibility; unchanged unavailable IDs → stored trusted reference + diagnostics, no latest; read outage не eligibility | Mapping каждого ID к authoritative reference/facts |
| 5 | Application/Domain | Current и candidate по EntityInstanceId | Duplicate logical ID → conflict; classify added/replaced/retained/removed; new-use для added/replaced; удержание unchanged unavailable; inactive/archived owner только Remove/eligible Replace проблемной reference без expansion; capture labels/diagnostics | Validated whole set либо полный отказ |
| 6 | Coordination boundary | Reference-use facts + catalog/content versions/scopes | Gate уже получен до шага 2; свежие primary facts против Archive/Retire/Withdrawal/delete/move/F15 Resolve; recheck independent basis | Защищённое право commit; не прямые foreign root writes |
| 7 | Domain/Persistence | Candidate, own expected Version, audit time | ReplaceItems; conditional root write и child replacement в одной Publishing transaction; проверка root token даже для [] | Commit working set + Version/audit; no release/revision/active pointer |
| 8 | Result mapper/Api | Committed candidate + captured metadata и resulting Version | DTO/ETag той же mutation; no GetWorkspace после commit | 200; потеря ответа оставляет unknown outcome |

Каждая новая ссылка проверяется заново на сервере независимо от picker. Смена Key у instance не превращает reference в новую identity; две версии одного instance нельзя silently collapse. Removed references отсутствуют в новом whole set, но исторические releases/revisions сохраняют свои dependencies. F08 local additions/overrides/exclusions не переписываются: новая live база влияет только на последующий preview/F09 snapshot.

Concurrency before/at commit: own stale → rollback + 412; relevant catalog/content basis drift →409/revalidation; technical port/gate failure отдельно; blind reattempt whole-set Save не выполняется. После failed UoW нельзя классифицировать через stale tracked entities. Unknown commit/result response не означает rollback; client recovery DF01 читает собственный ресурс и сохраняет пользовательский intent отдельно. Read freshness не доказывает авторство Save.

### DF03. Internal idempotent initialization

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. После появления source Product выбранный Q20 mechanism обеспечивает recoverable необходимость setup. F06 apply и Publishing setup имеют независимые commits; успешный CatalogAccess commit ещё не означает готовый assignment.
2. Publishing internal handler проверяет Product existence через public fact/port и читает root по ProductId. Already exists → no reset/no change Version; source Name/lifecycle update не пересоздаёт root.
3. Absent root → Create(ProductId) с empty set, Add и local commit. Unique ProductId защищает concurrent setup: после classified duplicate-create race fresh read existing root, не clear collection. Other constraint error не объявлять duplicate success.
4. Trigger/work acknowledgement после соответствующего durable commit; при crash/retry assignment остаётся один. Конкретные delivery/inbox/event API и wakeup гарантии Q20, не условный publish в GET.
5. Пока setup не готов, read/write response mapping выбирается Q20; не подставлять empty DTO с выдуманным ETag и не создавать дополнительные Publishing roots автоматически.

### DF04. Найти instance для добавления

1. Picker передаёт F04 list query с server search/schema/page и согласованными availability filters; context Product остаётся у родительской F07 формы.
2. F04 query adapter читает logical summary согласно своим specifications/sort/tie-breaker/count rules, не все versions/content graphs.
3. Client показывает instance identity/Name/Key/schema и reasons, сохраняет временный выбранный logical ID. Выбор не добавляет на сервер item и не назначает latest Published version.
4. Переход к DF05; Cancel оставляет whole-set form без добавления. Доступ/filters/metadata определены F04/Q15; дополнительные candidate endpoints не вводятся.

### DF05. Явно выбрать version

1. По chosen InstanceId либо replacement current item клиент запрашивает F04 version list в пределах этого instance, с Published и необходимым target/lifecycle контекстом. Точный availability filter contract F04/Q15; Published status alone не доказывает new-use.
2. F04 возвращает summaries exact IDs/numbers/schema target и доступности. Смена target/schema версии не сворачивается в один latest row; пользователь выбирает конкретную version.
3. Confirm помещает exact version ID в local candidate. Replacement сохраняет logical instance позицию; две его versions в F07 не сосуществуют. Full content inspection/navigation при необходимости переиспользует F04 exact version screen, не создаёт assignment API.
4. Save выполняет DF02 и повторную authoritative resolution всех submitted refs. Сохранение нового Published version в ContentModeling после выбора не меняет request автоматически. Недоступность selected version обнаруживается без silent substitute.

## 10. Ошибки и диагностика

| Отказ / состояние | Поведение | Объяснение для потребителя |
| --- | --- | --- |
| Structural body/ID/duplicate exact ID | 400 ValidationProblemDetails, без writes | Field/item issue; не silently Distinct |
| Missing Product/assignment/requested version | 404 по contract; setup-pending distinction Q20 | Not found не равен empty working set |
| Draft/unavailable new reference, duplicate logical position | 409 предметный отказ | Requested version ID и причина; current assignment не изменён |
| F15/catalog lifecycle blocker | 409 mutation blocker | Scoped reason/conflict link по read contract; GET/history доступны |
| Missing If-Match | 428 | Получить exact own ETag; body Revision не замена |
| Stale own ETag/own race | 412 | Reload/reconcile явно; edits клиента не уничтожаются |
| Dependency/guard transient failure | Operational error; rollback до commit | Нет false «все references допустимы»; retry не автосохранение с новым token |
| Error/cancellation после commit | Unknown outcome | Working set может быть сохранён; явный exact read recovery |
| Existing unchanged reference стала unavailable | Save допускает retention; ReleaseBlockers | Stored exact IDs сохраняются; F09 Create блокируется; no latest |

Логи: ProductId, correlation/operation identity при наличии, requested version IDs, affected logical IDs, own expected/observed token внутри server diagnostics и отказ/commit stage. Не full content JSON/keys/secrets, credentials, activation key или payload пользовательских данных. Machine codes, issue shape, no-op/unknown outcome diagnostics Q15/Q20. Assignment Save не сопровождается сообщением «клиентам опубликовано».

Global gate timeout/cancellation/failed port/owner connection loss — технический abort, не domain409 или empty valid facts. Unknown commit ACK требует inquiry/operation identity по Q15; reconnect continuation/blind retry с новым ETag не допускаются.

## 11. Тестирование

Стратегия target по [S04](../standards/S04-testing.md)/[S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism); здесь application tests не запускались. Existing [AssignmentTests](../../../../contentmanager.api/tests/BoundedContexts/Publishing/ContentManager.Publishing.Tests.Unit/AssignmentTests.cs) проверяют Key-based legacy behavior; [PersistenceTests](../../../../contentmanager.api/tests/BoundedContexts/Publishing/ContentManager.Publishing.Tests.Integration/PersistenceTests.cs) используют PostgresSuite и показывают независимый release snapshot после удаления item. [PublishingArchiveConcurrencyTests](../../../../contentmanager.api/tests/App/ContentManager.App.Tests.Integration/PublishingArchiveConcurrencyTests.cs) содержит barrier для current shared-scope Save против Archive; это useful fixture, не доказательство target public-port/no-cross-BC guards или HTTP ETag.

Unit SUT — Domain methods, validators, mapping/change classification; controlled ContentModeling/Catalog ports/clock doubles допустимы для orchestration. Integration SUT — реальные PostgreSQL/EF/repository/query/UoW/unique/concurrency и selected guard adapters; Testcontainers/существующий PostgresSuite, не in-memory provider. Functional SUT — один CM host, реальный PostgreSQL, HTTP/OpenAPI и JWT/permissions MVP. Мocks ContentModeling/Catalog доказывают local failure handling, но не race protocol реальных writers. Integration нескольких BC одного host не называется E2E; broker/VR School setup delivery требует соответствующего реального контура только после выбора trigger.

Собственные Product/instance/version IDs и данные per test; fixture isolation/cleanup без общего mutable seed. Races — отдельные contexts/connections/scopes и barriers до commit, не sleeps. Проверять actual persisted root/token, child rows и unchanged historical artifacts, а не только returned DTO.

| ID / DF | Уровень / SUT | Исходные данные / сценарий | Ожидаемый результат |
| --- | --- | --- | --- |
| T01 / DF01 | Integration query + Functional | Saved empty/nonempty set, labels/F15 unavailable state | DTO и own ETag одного root; read не пишет/не очищает refs; blocker не empty |
| T02 / DF02 | Unit + Integration Domain/handler | Exact Published A/v2; затем Publish A/v3 или rename Keys | Сохраняется exact v2; identity не меняется; no latest substitution |
| T03 / DF02 | Functional/Integration, batch resolver | Draft/Retired/Withdrawn/Archived new-use refs, missing ID, duplicate exact ID, A/v1+A/v2 | Полный rollback; field/reference codes; stable-ID conflict, not LWW |
| T04 / DF02 | Functional HTTP/OpenAPI | Missing/stale/valid strong If-Match; body legacy Revision/DTO token check |428/412/200+own new header; no Version body/DTO; header grammar profile Q15 |
| T05 / DF02 | PostgreSQL concurrency | Два whole-set Saves одним token; child add/replace/remove-to-empty | One winner, other412; no mixed set/lost update; Version owner реально guard child writes |
| T06 / DF02,F09 | Integration Publishing | Existing immutable release/revision; assignment replace/remove; unrelated policy/activation | History/artifacts unchanged; own ETag независим unrelated state |
| T07 / DF02 | Integration guard protocol | Assignment addition vs real Archive/Retire/Withdrawal/delete/media unavailability | Gate сериализует оба real writers; losing basis re-read запрещает new reference после invalidation; нельзя commit по stale facts |
| T08 / DF02,F15 | Integration scope guards | Product edit vs Variant move/F15 open/Resolve | Scoped mutation block, no foreign DbContext transaction; historical reads не отозваны |
| T09 / DF02 | Integration failure injection | Constraint/guard/DB failure между child replacement и root commit | Original set/token intact; no partial success; fresh classification after disposal |
| T10 / DF02 | Functional result carrier | Successful commit; concurrent later Save; lost response/failed dependency enrichment | Ответ первого Save его DTO/token без hidden result GET; unknown outcome без blind retry |
| T11 / DF03 | PostgreSQL Integration setup | Concurrent duplicate setup; retry после filled set/source metadata update | Один root; existing filled set не reset; actual duplicate vs other constraints distinguished |
| T12 / DF03 | Integration selected delivery boundary | Catalog commit success, worker/setup crash | Recoverable setup без fake read token; отдельные commits наблюдаемы |
| T13 / DF04–05 | Functional F04 + frontend interaction | Multiple targets/Published numbers, empty/search pages, explicit replacement | Exact version chosen; server filter/page; no latest/whole browser catalog; Save revalidates |
| T14 / DF01–02 | Integration + Functional | Saved exact A становится Retired/Withdrawn/Archived; unchanged/reordered A, removed A, replaced A; inactive owner Add vs Replace problematic/nonproblematic reference | Retention/reorder и maintenance Remove/eligible Replace проблемной reference разрешены; Add/expansion, замена непроблемной reference inactive owner и unavailable replacement запрещены; retained A блокирует F09; OPEN F15 блокирует maintenance |
| T15 / all | Architecture + HTTP surface | Project references/DI/routes/model schemas | Commands Application, queries Persistence; public ports; no source CRUD/per-item publish-on-save; DTO+ETag own resource |

| ID / DF | Уровень / SUT | Исходные данные / сценарий | Ожидаемый результат |
| --- | --- | --- | --- |
| G01 / guarded write flows | PostgreSQL Integration, separate owner connections/CM replicas | Pause writer after gate; concurrent relevant writer with same stable key | Second writer waits; first commits own BC only; second loads fresh committed facts, no forbidden stale commit |
| G02 / guarded write flows | PostgreSQL Integration + architecture/DI | Independent public primary reads while owner holds gate; foreign UoW enlistment/shared multi-DbContext participant | Reads see committed facts after acquisition; no foreign EF/transaction participant/cache/replica; own writes+outbox/audit intent/inbox atomic |
| G03 / guarded write flows | Fault integration | Bounded acquisition cancellation/timeout, failed read, owner connection loss, lost commit ACK | Technical abort/unknown outcome; no fake compatible empty facts or reconnect continuation; original own412 and independent basis409 preserved |

## 12. Порядок реализации

1. Реализовать agreed retained/new-use/maintenance границу Q07; реализовать gate §7.2; уточнить internal setup trigger/readiness Q20; определить read/write metadata/error profile Q15, согласовать F04/F06/F15/F09 writers.
2. В Domain оформить Product identity, exact reference value object, whole-set ReplaceItems/unique EntityInstanceId и own constraints/Version. Не переносить Key-based removal или SortOrder как продуктовую identity.
3. В Application.Abstractions определить command/query/result/public ports. В Application реализовать validators, batch resolver, change classifier, guards/setup/update handlers.
4. В EntityFramework/Persistence создать own Version/mapping/specifications/query/repository, transactional child replacement и migration scoped к F07; отделить publication state concurrency.
5. Подключить public ContentModeling/Catalog adapters и durable initialization protocol; проверить real writer races, orphan/reference retention и setup retry. Не сохранять shared direct-DbContext orchestration только ради текущего теста.
6. Реализовать exact GET/PUT, strong own ETag/If-Match, result DTO carrier/error classification/OpenAPI; затем NSwag header support и frontend F07.
7. Выполнить матрицу target tests, включая real PostgreSQL/JWT Functional и MVP gate и выбранную setup-delivery boundary. Static documentation/source audit не являются runtime verification.

## 13. Открытые технические вопросы

[Технический Q39](../questions/Q39-schema-content-selection.md) закрывает selector/tree DTO, enum codec, review carrier, storage/migration и conversion. Соответствующая реализация ещё необходима; общие внешние вопросы Q10/Q15/Q20 этим не закрываются.

[Q38](../../../business/questions/Q38-bulk-version-freeze-and-release-preparation.md) сохраняет открытыми batch transport, preflight basis carrier и execution/continuation/storage mechanics. Согласованные selection/dependency/stage/partial-result требования находятся в §3/§4 и не заменяются текущими одиночными API.

- [Q07 — допустимость рабочих references](../../../business/questions/Q07-exclusions-and-local-content.md#f07-допустимость-рабочих-references): правила retention, new-use и ограниченного maintenance согласованы; точные schema/media/lifecycle carriers остаются в owning contracts, no latest/stable identity сохраняются.
- [Q15 — read/write и reference контракт](../questions/Q15-api-and-source-code.md#f07-read-write-и-reference-контракт): exact display/diagnostic shape/limits/order/audit, errors/access/header profile/NSwag/result carrier и unknown outcome.
- [Q20 — initialization и конкурентные guards](../questions/Q20-persistence-and-migration.md#f07-initialization-и-конкурентные-guards): durable setup/recovery и root-ready response, own Version/child mapping/no-op policy, carrier/read consistency/retention storage без cross-BC transaction; MVP gate уже выбран (§7.2).

Working set, Published exact selection, whole-set Save, собственный concurrency owner и неизменность releases/revisions уже определены. Открытая реализация не возвращает full Publishing в Demo.
