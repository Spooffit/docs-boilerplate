# F14. Совместимость клиента: бэкенд, принятый минимум

[Бизнесовая спецификация](../../../business/features/F14-client-compatibility.md) · [M05](../modules/M05-delivery.md) · [Фронтенд](../../frontend/features/F14-client-compatibility.md) · [Q14](../../../business/questions/Q14-client-compatibility.md).

## 1. Область реализации

F14 закрепляет границу существующего версионирования: клиент заявляет конкретные поддерживаемые структуры, а сервер сохраняет точные ссылки на версии схем и данных. Изменение контента специалистом не добавляет клиенту код отображения или обработки. Если такой код требуется, его меняет разработчик. Генерация моделей F21/F22 не доказывает готовность клиентского поведения.

В Demo минимум реализуется внутри F02–F04 и F13: полный выбор точных SchemaId + SchemaVersionId, Published данные выбранных структур, неизменяемый план и атомарное применение каждой группы на клиенте. В MVP сохраняются целая entitlement-selected ревизия, pinned session и самостоятельные проверки права F12/F13. Заявленная поддержка не расширяет лицензионный состав и не разрешает смешивать snapshots.

Расширенная архитектура отложена. В этом проходе не проектируются реестр возможностей/сборок, анализ breaking changes, диапазоны допустимых версий, серверные преобразования, отдельные compatibility endpoints, агрегаты или фоновые задания. Итог при неподдерживаемом составе MVP и возможность выбора другого целого snapshot остаются в Q14. Документ раскрывает обязанности каждого слоя в принятом минимуме; разделы без новой реализации отмечены явно.

Границы текущего checkout от 09.10.2026:

| Источник | Факт реализации | Граница вывода |
| --- | --- | --- |
| [RuntimeSynchronizationReader.cs](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/EntityInstances/QueryHandlers/RuntimeSynchronizationReader.cs) | CaptureAsync проверяет exact Published пару, фильтрует SchemaVersionId до сравнения PublishedVersionNumber; ReadContentAsync читает pinned canonical bytes | Допускает несколько версий одной схемы; не использует глобальный LatestPublishedVersionId или профиль сборки |
| [SynchronizationPlanBuilder.cs](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Application/Synchronization/SynchronizationPlanBuilder.cs), [SynchronizationCoordinator.cs](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Application/Synchronization/SynchronizationCoordinator.cs) | Полный selection/inventory, отдельные exact группы, immutable план и independent group receipts | Receipt сервера не доказывает атомарный durable commit клиента F19 |
| [F14CompatibilityHttpTests.cs](../../../../contentmanager.api/tests/App/ContentManager.App.Tests.Integration/F14CompatibilityHttpTests.cs) | Public authoring → sync с реальным PostgreSQL: новое поле не меняет старую структуру/данные/план; две структуры одного экземпляра доставляются отдельно; Draft и чужие/неизвестные exact пары исключены | Проверяется серверный минимум; SDK/Unity apply и обработка новых полей принадлежат F19/F20 |
| [ClientContentProfile.cs](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Domain/ClientContentProfile.cs) | Create проверяет непустые Key/ClientBuild и хотя бы один contract; Supports сравнивает SchemaKey и точный SchemaVersionId | Нет анализа содержимого полей или доказательства поддержки клиентским кодом |
| [ClientContentProfileConfiguration.cs](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.EntityFramework/ClientContentProfileConfiguration.cs), [DeliveryDbContext.cs](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.EntityFramework/DeliveryDbContext.cs) | DbSet и owned contracts; unique Key профиля, unique SchemaKey внутри профиля | Последнее ограничение не выражает Demo-выбор нескольких версий одной схемы |
| [InternalDeliveryService.cs](../../../../contentmanager.api/src/App/ContentManager.Application/Delivery/InternalDeliveryService.cs) | StartSessionAsync принимает ResolvedLicenseContext и variantId, читает лицензии/историю/ревизию; профиль клиента не передаётся | Найденный runtime путь не доказывает сквозную проверку ClientContentProfile |
| Поиск ClientContentProfile и вызовов .Supports в src, без bin/obj/Migrations | Упоминания профиля найдены в Domain, EF configuration и DbContext; вызовов .Supports не найдено | Не подтверждены runtime подключение, импорт профиля и публичный контракт |
| [ClientProfileAndSyncSessionTests.cs](../../../../contentmanager.api/tests/BoundedContexts/Delivery/ContentManager.Delivery.Tests.Unit/ClientProfileAndSyncSessionTests.cs), [PersistenceTests.cs](../../../../contentmanager.api/tests/BoundedContexts/Delivery/ContentManager.Delivery.Tests.Integration/PersistenceTests.cs) | Есть исходники тестов exact matching, round trip и uniqueness профилей | Здесь тесты не запускались; наличие теста не означает готовую внешнюю функцию |

## 2. Domain

### 2.1. Aggregates, entities и value objects

Новая доменная модель F14 не вводится. Полные целевые модели определены у владельцев, а текущий профиль описан отдельно от них.

| Тип | Роль | Владелец / граница |
| --- | --- | --- |
| EntitySchema / EntitySchemaVersion | Существующие целевые roots; полное определение [F02](F02-create-schema-version.md)/[F03](F03-publish-schema-version.md) | ContentModeling; изменение другой версии не переписывает Published структуру |
| EntityInstance / EntityInstanceVersion | Существующие целевые roots; полное определение [F04](F04-entity-instances.md) | ContentModeling; новое представление создаётся явно, не заменяет старую структуру |
| DeliveryPublicationSnapshot / DeliverySession | Целевые модели MVP; полное определение [F13](F13-content-delivery.md#21-aggregates-entities-и-value-objects) | Delivery; совместимость не меняет entitlement, membership или pinned основу |
| ClientContentProfile | Текущий Aggregate Root, не требование нового реестра | Delivery; Create-only публичный предметный интерфейс, update/lifecycle методы не определены |
| ClientSchemaContract | Текущая owned модель | Принадлежит профилю; бизнесовый номер не является concurrency token |
| SchemaVersionNumber | Текущий value object номера, source evidence | Delivery owned contract; не новый target registry или DTO |
| ClientContentProfileId | Текущая typed identity | Identity профиля; не identity схемы/версии и не разрешение на контент |

**ClientContentProfile**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `ClientContentProfileId` | Да | Текущая identity профиля | Create-only; новый ID через New; не новый transport контракт |
| Key | `string` | Да | Текущий ключ профиля | Create проверяет не whitespace, Trim; текущий EF required MaxLength(128)/unique — source fact, не согласованный лимит |
| ClientBuild | `string` | Да | Текущая строка сборки | Create проверяет не whitespace, Trim; текущий EF required MaxLength(128) — source fact, не доказательство поддержки |
| SchemaContracts | `IReadOnlyCollection<ClientSchemaContract>` | Да | Текущая owned коллекция | Create contracts.Count > 0, копия коллекции; current unique SchemaKey не применяется к Demo multiple-version selection |

**ClientSchemaContract**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaKey | `EntitySchemaKey` | Да | Текущий ключ exact match | Owned profile; фабрика присваивает без дополнительных проверок |
| SchemaVersionId | `EntitySchemaVersionId` | Да | Точная структура | Supports сравнивает обе величины; не latest/диапазон |
| VersionNumber | `SchemaVersionNumber` | Да | Бизнесовый номер с Major/Minor | Не concurrency token; фабрика не проверяет соответствие номера ID |

**ClientContentProfileId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Текущая typed identity | New создаёт Guid v7; не identity схемы/версии и не право на контент |

**SchemaVersionNumber — текущий value object**

Поля подтверждены [SchemaVersionNumber.cs](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Domain/References/SchemaVersionNumber.cs); это описание текущего номера, не новая целевая модель F14.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Major | `int` | Да | Major component текущего бизнесового номера | Не concurrency token; owned VersionNumber текущего ClientSchemaContract |
| Minor | `int` | Да | Minor component текущего бизнесового номера | Не concurrency token; не доказательство поддержки клиентским кодом |

Mutable owners F02–F04/F13 используют собственный внутренний int Version по своим спецификациям. F14 не добавляет общий Version совместимости и не публикует его в DTO. Для текущего Create-only профиля не проектируется новый update-контракт.

### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| ClientContentProfile, текущий код | Create | key:string, clientBuild:string, schemaContracts:IReadOnlyCollection<ClientSchemaContract> | Key/ClientBuild не whitespace, contracts.Count > 0 | Новый ID, Trim строк, копия коллекции; не регистрация внешней сборки |
| ClientContentProfile, текущий код | Supports | schemaKey:EntitySchemaKey, schemaVersionId:EntitySchemaVersionId | Переданы значения | bool: существует contract с обеими точными величинами; состояние не меняется |
| ClientSchemaContract, текущий код | Create | schemaKey, schemaVersionId, versionNumber | Фабрика присваивает переданные значения | Owned contract; дополнительных проверок соответствия номера ID фабрика не выполняет |
| ContentModeling roots | Создание/Save/Publish конкретной версии | Параметры и методы F02–F04 | Инварианты соответствующего владельца | Новая/сохранённая версия, без автоматического обновления клиентской сборки |

Не вводится метод ConvertToSupportedVersion. Перенос значений при явном авторском Copy в F04 не является преобразованием Delivery payload для старого клиента.

### 2.3. Инварианты и constraints

1. Identity точной версии определяется ID, а не Name, строкой сборки или сравнением Major/Minor. Бизнесовый номер служит отображению; большая величина не означает поддержку старым клиентом.
2. Опубликованные структура и данные сохраняют свою точную основу. Новые поля проходят F02–F04; старое Published содержимое не переписывается ради клиента.
3. Demo selection обязателен и полон, может содержать несколько SchemaVersionId одного SchemaId. Отсутствующий/непрочитанный выбор не трактуется как допустимый пустой набор. Явный empty/deselection и достоверное снятие доступности обрабатывает F13.
4. Выбор поддерживаемой структуры и право получать контент — независимые условия. MVP не фильтрует entitlement-selected snapshot по элементам, не объединяет права и не выбирает fallback без решения Q14.
5. Нет молчаливого игнорирования полей, преобразования payload или обещания поддержки только на основании успешной JSON-валидации/генерации C#.

Ограничения схем и экземпляров принадлежат named constraints F02–F04 по [S21](../standards/S21-aggregate-constraints.md). Текущие EF MaxLength(128) профиля — факт кода, а не новый согласованный лимит. Изменение/замена профиля и его ограничения остаются Q14; дублировать его uniqueness в Demo selection нельзя.

### 2.4. Domain events

F14 не создаёт новых domain/integration events. События публикации, materialization и withdrawal принадлежат F03/F04/F10–F13. Наличие нового SchemaVersionId не означает событие «клиент теперь поддерживает эту версию».

## 3. Application.Abstractions

### 3.1. Commands

Новые команды F14 не вводятся. StartSynchronizationCommand принадлежит F13: SDK-generated SynchronizationId, полный Selection и InstalledInventory, результат — точный план/контекст. Авторские команды Copy/Save/Publish определены F02–F04 и сохраняют их own-resource concurrency. Совместимость не подменяет их параметров и результатов.

### 3.2. Queries

CheckSynchronizationFreshnessQuery и чтение exact schema/instance payload принадлежат F13. Query ответа «поддерживает ли эта сборка весь snapshot» пока не согласован; не вводится новый GetCompatibilityQuery с вымышленным результатом.

### 3.3. DTO и parameter models

| Модель | Точный состав в пределах F14 | Источник / назначение |
| --- | --- | --- |
| ExactSchemaSelection | Полное определение [F13](F13-content-delivery.md#21-aggregates-entities-и-value-objects) | Application input F13; полная обязательная коллекция, не диапазон версий |
| Exact payload reference | Полное определение у [F04](F04-entity-instances.md) / [F13](F13-content-delivery.md#33-dto-и-parameter-models) | Владельцы определяют полный контракт/типы ID; delivery читает именно указанные версии и сохраняет соответствие logical SchemaId/InstanceId |
| StartSynchronizationRequest / SynchronizationPlanDto | Поля, nullable/required и реализованный Demo wire profile целиком в F13/Q33 | Повторная независимая копия DTO F14 не создаётся |
| MVP session/snapshot contracts | Полный состав, entitlement и credential profile в F13/Q16 | Никакого нового поля supported=true без согласованной семантики |

ClientContentProfile — текущая доменная модель, а не утверждённый request DTO импорта. Форма декларации поддержки для MVP остаётся Q14. Технический concurrency Version не входит в DTO.

### 3.4. Ports

Новые ports F14 не нужны. Публичные exact read boundaries ContentModeling, plan cache Demo и Delivery snapshot reads MVP переиспользуются по F13. Генерация JSON Schema/C# относится к F21/F22. Порт сервиса автоматического анализа/конвертации совместимости не проектируется. Host/DI подключает владельцев этих механизмов; отдельный F14 service registration не требуется.

### 3.5. Specifications / read contracts

Demo использует exact schema/version принадлежность, Published и доступность по F13, затем выбирает максимальный Published бизнесовый номер экземпляра только внутри выбранного target. Query-условия владельцев выражаются атомарными Ardalis predicates по [S19](../standards/S19-specifications.md); чистый Supports не требует repository/specification. MVP читает materialized pinned snapshot F13, а не составляет ответ из чужих EF contexts. Отдельная фильтрация по строке сборки, Key или latest здесь не добавляется.

## 4. Application

### 4.1. Handlers / use cases

Новых handlers нет. Ответственность состоит в сохранении accepted guards внутри существующих сценариев:

| Сценарий | Чтения / проверки | Домен и запись | Граница / результат |
| --- | --- | --- | --- |
| Publish структуры/данных | Saved exact version, own token, schema/content validity по F03/F04 | Методы и транзакция владельца; immutable Published state | Commit своего BC; не запись профиля клиентской сборки |
| Demo start/preflight | Полный selection, фактический inventory, exact Published projection | Build desired set/diff F13; start фиксирует immutable cached plan, preflight читает | Нет business SQL transaction F14; exact result/group errors |
| MVP session/read | Право F12, effective materialized revision, pinned membership и hard invalidators F13 | Создание/чтение session по F13 | Обнаружение неподдерживаемого состава не разрешает иной snapshot; ветвь решения Q14 |

Command handlers остаются в Application, queries — в доступе к данным (EntityFramework.Persistence либо cache adapter соответствующего query), согласно [S20](../standards/S20-cqrs.md). After-commit side effects принадлежат своим фичам; F14 не добавляет скрытый broker или distributed transaction.

### 4.2. Validators

Переиспользуются semantic validators владельцев: exact SchemaId/SchemaVersionId pair и полный selection/inventory F13; recursive schema/values F02–F04. По [S18](../standards/S18-fluent-validation.md) их композиция не копирует доменные лимиты. Существование/доступность версий и право — изменяемые внешние факты; их проверяет use case/read boundary в нужный момент, FluentValidation не устраняет гонки. Валидатор новых возможностей клиента отсутствует до Q14.

### 4.3. Mappers

Api map сохраняет точные ID в application input; Persistence map возвращает точную Published projection; ответ и SDK read model сохраняют SchemaVersionId без подстановки latest. DTO mapping и canonical payload описаны F03/F04/F13. Mapper не переименовывает, не удаляет и не дополняет поля для подгонки к клиентскому классу.

### 4.4. Services / algorithms

Demo algorithm реализован RuntimeSynchronizationReader и SynchronizationPlanBuilder F13: сначала exact target и полнота фактов, затем доступные Published версии экземпляров по этому target, затем целый план группы. SynchronizationCoordinator закрепляет план и читает exact bytes без конвертации. Текущий ClientContentProfile не является обязательным серверным реестром Demo. Для MVP exact matching отдельных contract само по себе не определяет допустимость всей ревизии и реакцию на mismatch; полный алгоритм остаётся отложен Q14.

## 5. Api

### 5.1. Endpoints

Отдельных endpoints F14 нет. Publish/write использует API F02–F04, runtime selection/plan/data — реализованный Demo API F13 (`/api/v1/content-synchronizations`). Импорт/CRUD профилей, capability-check и conversion endpoints не назначаются. Protected MVP Delivery не подключает реестр совместимости и сохраняет отдельную границу F12/F13.

### 5.2. ETag / If-Match

Применяется [S16](../standards/S16-rest.md) у mutable resource F02–F04/F13: own strong numeric ETag, expected int Version внутри команды, обязательный If-Match там, где это установлено владельцем; 428 missing и 412 stale. SchemaVersionId, business VersionNumber и ContentHash не становятся таким token. Нового F14 ETag/If-Match нет; start/resume plan использует execution ownership F13, а не авторский token.

### 5.3. OpenAPI и ошибки

Состав exact requests и required selection определяется F13/Q15/Q33. Для MVP mismatch не назначены HTTP status, machine code и payload; Q14/Q15. Отсутствие результата проверки не кодируется успешным supported=true или пустым manifest. Access Demo/MVP остаётся у владельцев; отдельное compatibility permission не вводится.

## 6. EntityFramework

### 6.1. DbContext ownership

ContentModelingDbContext хранит схемы/данные; DeliveryDbContext — собственные snapshots/session MVP. F14 не добавляет DbContext и foreign navigation. Текущий DeliveryDbContext содержит ClientContentProfiles; это существующий storage, а не принятие нового обязательного реестра.

### 6.2. Configurations

Текущий ClientContentProfileConfiguration мапит root в delivery.client_content_profiles: id Guid, key/client_build required с MaxLength(128). SchemaContracts — owned collection delivery.client_schema_contracts с FK client_content_profile_id, техническим row_id:int, composite PK owner + row_id, required schema_key/schema_version_id и owned VersionNumber с integer version_major/version_minor. Field access использует _schemaContracts. Поля row_id/owner FK являются persistence details; новые transport IDs из них не создаются.

Целевые configurations exact SchemaVersion/InstanceVersion, Published JSON/hash, snapshot items и session определены F02–F04/F13. F14 не добавляет таблицу «совместимые поля» или metadata произвольных возможностей.

### 6.3. Constraints / indexes

Текущая configuration задаёт unique profile Key и unique (profile owner, SchemaKey). Domain Create не проверяет дубли SchemaKey до SQL; наличие такого ограничения не доказывает ранний application guard. Нельзя перенести unique SchemaKey в обязательный Demo selection: несколько exact версий одной схемы допустимы. При будущем решении об использовании профиля нужно согласовать его identity/cardinality Q14, затем отдельно определить migration. Published immutability и own-resource constraints сохраняются по F02–F04/F13.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

Нет нового repository F14 или каждого ClientSchemaContract. Exact projections и bounded reads принадлежат F03/F04/F13. Demo query adapter возвращает различимый результат: достоверный состав/достоверная недоступность/ошибка чтения. MVP snapshot adapter сохраняет целую pinned ревизию; отсутствие контрактов клиента нельзя исправить скрытым чтением latest ContentModeling. В текущем поиске src самостоятельный runtime адаптер чтения ClientContentProfile не найден.

### 7.2. Transactions

Publish коммитит только свой BC по F03/F04; фиксация MVP session — Delivery по F13. Декларация поддержки не обновляется автоматически в той же транзакции. Demo cached plan не создаёт F14 SQL transaction; клиентский atomic commit всей schema/version группы принадлежит F19. Сбой получения группы не превращается в запись о её успешном удалении.

### 7.3. Migrations

Новой миграции F14 сейчас нет. Существующие profile таблицы описаны как source evidence; их удаление/перестройка не выполняется в рамках аналитики. Если профиль станет частью согласованного runtime контракта, сначала ответить Q14 и определить multiple-version semantics, затем изменить mapping/constraints и проверить PostgreSQL. Не менять базу только ради заполнения шаблона.

## 8. External adapters

F14 не добавляет HTTP интеграцию, S3 операции или RabbitMQ consumers. Asset transport F05/F13, VR School entitlement/key validation MVP F12/F13 и генерация F21/F22 сохраняют своих владельцев. Demo не требует этих MVP зависимостей ради объявления поддерживаемой структуры. Новые capability catalogs, conversion service, package scanner и adapter импорта профиля отложены.

## 9. Data flows

Это локальные обязанности F14 в уже описанных потоках соседних фич, а не новые HTTP операции. Формат соответствует [S17](../standards/S17-data-flows.md). Полные внешние контракты, ошибки и модели остаются у владельцев.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Публикация exact структуры/данных | Внутренний этап use case F03/F04 | Publish | Application → ContentModeling Domain/Persistence | Demo/MVP по F03/F04 | Saved exact version и own expected token | Publish владельца | Exact Published ID/structure/content | Commit своего BC, прежние Published данные сохранены | Validation, stale token, недоступная основа по F03/F04 |
| DF02. Demo exact selection | Внутренний этап F13 | Start / freshness | Delivery Application → exact ContentModeling read boundary | Demo без key/license | Full selection и actual inventory | RuntimeSynchronizationReader / SynchronizationPlanBuilder / SynchronizationCoordinator F13 | План exact групп либо freshness result | Состав вычислен по выбранным версиям без conversion | Invalid/incomplete input, group error, unavailable exact source |
| DF03. MVP exact snapshot boundary | Внутренний этап F13 | Session creation / read | Delivery → entitlement selector / pinned snapshot | Право и credential по F12/F13 | Право, целая revision; декларация поддержки пока Q14 | Use cases F13; compatibility branch уточняется | Целый pinned состав либо отказ по согласованному контракту | Нет item-level смешения; критерий полной поддержки Q14 | Право/доступность по F13; unsupported result Q14/Q15 |

### DF01. Публикация exact структуры/данных

1. Api F03/F04 связывает exact ID и If-Match с командой; Application загружает saved state и проверяет own concurrency. Бизнесовый номер схемы не заменяет token.
2. Handler проверяет структуру и значения по той SchemaVersionId, на которую ссылается публикуемая версия. Невалидное содержимое не получает статус Published.
3. Domain владельца фиксирует immutable Published основу/hash/номер по правилам F03/F04. SaveChanges и необходимые commit side effects выполняются в его границе; прежние Published версии не переписываются.
4. Ответ возвращает exact опубликованную версию. Profile/build capabilities не меняются. Разработчик отдельно добавляет клиентскую обработку новых полей, если она нужна; серверный Publish не объявляет эту работу выполненной.

### DF02. Demo exact selection

1. F13 принимает обязательный полный exact selection и actual installed inventory. Проверяет форму/дубли/принадлежность версии схеме; разные SchemaVersionId одной SchemaId не считаются дублем.
2. Read boundary получает Published/available exact projections. Ошибка/неполнота чтения отличается от достоверного withdrawal и явного deselection; только разрешённые F13 отрицательные факты обосновывают Remove.
3. RuntimeSynchronizationReader.CaptureAsync выбирает данные только по выбранному target, затем SynchronizationPlanBuilder сравнивает exact представления с inventory. Более новый target не подставляется; значения не преобразуются в старый формат.
4. Start закрепляет immutable plan в cache F13; preflight возвращает текущую актуальность без создания plan. Нового business state F14 и after-commit event нет.
5. SDK получает exact payload по плану и активирует данные/removals/inventory целиком для одной schema/version группы по F19. Ошибка группы сохраняет её прежнее committed состояние. Разные группы могут завершиться независимо; это принятая Demo semantics, не правило фильтрации MVP snapshot.

### DF03. MVP exact snapshot boundary

1. F12/F13 подтверждают лицензионное право и выбирают одну целую effective ревизию; snapshot/session materialized точные ссылки не заменяются live latest.
2. Источник декларации поддержки, момент проверки и критерий пригодности всего состава остаются Q14. В этом проходе не назначается автоматическое условие «каждый элемент Supports» как готовый публичный алгоритм.
3. При обнаруженном mismatch нельзя молча отбросить элементы, преобразовать payload, расширить entitlement или сменить pinned revision. Возврат отказа и возможность выбора другой целой ревизии требуют решения Q14; неизвестная ветвь не получает вымышленный HTTP success.
4. При выдаче F13 сохраняет session membership/status/expiry/hard invalidators и exact reads. Новое состояние/запись именно F14 отсутствует. После ответа Q14 её ветвь встраивается в владельца операции до заявления сквозной готовности.

## 10. Ошибки и диагностика

| Ситуация | Принятое поведение | Владелец детализации |
| --- | --- | --- |
| Новое поле не имеет клиентской обработки | Новая структура не обещает клиентскую поддержку | F14/Q14, разработчик приложения |
| Неверная exact пара, Draft вместо runtime Published | Не подставлять latest и не конвертировать данные | F13/Q33 |
| Ошибка чтения/неполнота группы | Отличать от достоверной недоступности; сохранять прежние данные ошибочной группы | F13/F19 |
| Несколько версий одной схемы в Demo | Разрешённый полный selection; текущий profile unique SchemaKey не применяется как правило | F13, Q14 о будущем профиле |
| Неподдерживаемый состав MVP | Право не расширяется, pinned snapshot не урезается; точный результат уточняется | Q14/Q15 |
| Сбой/конфликт авторской публикации | Правила владельца, без изменения опубликованной истории или поддержки сборки | F03/F04 |

Диагностика существующих операций связывает correlation/plan/session ID и exact schema/version/revision IDs; не логирует ключ активации, credential или полный payload. Нельзя выводить «совместим» только из успешной генерации/валидации либо поля ClientBuild. Новые буквальные error codes, telemetry events и retention сроки F14 не назначаются.

## 11. Тестирование

Стратегия по [S04](../standards/S04-testing.md) и [S14](../standards/S14-test-environments.md) проверяет минимальные инварианты в SUT владельцев, без отдельного тестового приложения F14. Открытый Q14 не получает скрытый expected fallback.

| ID | Уровень / SUT | Сценарий / собственные данные | Ожидаемый результат |
| --- | --- | --- | --- |
| T01 / DF01 | Domain Unit F03/F04 | Старая Published схема/данные, новый target и публикация нового поля | Старый exact payload/hash остаётся; новая версия не изменяет клиентскую декларацию |
| T02 / DF02 | Unit SynchronizationPlanBuilder F13 + HTTP Functional F14 | Одна схема с двумя exact Published targets и версиями одного экземпляра | Отдельные группы; max Published выбирается внутри каждого target; Draft/чужой target не попадает |
| T03 / DF02 | Unit + Functional F13 | Required selection отсутствует, explicit empty, read failure и достоверное снятие доступности | Missing/error не равен empty; удаления только по правилам F13; ошибочная группа сохранена |
| T04 / DF01–02 | PostgreSQL Integration exact read/write owners | Published v1, новая v2, повторное чтение v1, constraints exact принадлежности | Точные ссылки/immutable данные и persistence ограничения сохранены |
| T05 / DF01 | HTTP Functional F03/F04 | Реальные saved versions, own ETag, missing/stale If-Match | Поведение 428/412 по контракту владельца; stale Publish не переписывает данные |
| T06 / DF03 | Unit + Functional F12/F13 | Право на одну целую ревизию, независимо меняющийся клиентский набор | Нет union/fallback/item filtering; результат mismatch проверять после решения Q14 |
| T07, source evidence | Текущий ClientContentProfile Unit / PostgreSQL Integration | Exact Supports, другой SchemaVersionId, profile/contract uniqueness | Проверить существующее поведение отдельно от целевого Demo multiple-version selection |

Для Unit использовать собственные exact IDs и чистые projections; doubles допустимы для read failure/clock, не для доказательства SQL constraints. Integration использует реальный PostgreSQL/Testcontainers с schema/content/profile данными конкретного теста и cleanup через существующий fixture; InMemory не заменяет PostgreSQL. Functional поднимает API и реальный PostgreSQL; необходимые внешние зависимости MVP могут подменяться только с явной границей. Несколько BC одного API остаются Functional. Реальный CM → DotNet/Unity путь относится к F19/F20 E2E и требует действительного клиентского контура, а не только mock Supports.

Проверен серверный минимум: `F14CompatibilityHttpTests` — 2/2; полный App integration suite — 60/60 на реальном PostgreSQL/Testcontainers. Доставка сравнивается с точными сохранёнными canonical bytes и ContentHash; freshness не схлопывает версии одной схемы. Дополнительный серверный механизм F14 не требуется: guards находятся у F02–F04/F13. Проверка выполняется под Windows пользователем, временная инфраструктура освобождается fixture. SDK/Unity и атомарное локальное применение не проверены этим suite; это отдельный контур F19/F20.

## 12. Порядок реализации

1. В F02–F04 сохранить exact version links, immutable Published историю и проверки структуры/значений; F14 не создаёт второй owner.
2. В F13/F19 реализовать полный Demo selection, exact plan и атомарные schema/version группы, включая несколько targets одной схемы и различение empty/error/withdrawal.
3. В F21/F22 связать generated модели с выбранными точными структурами; клиентскую логику отображения/обработки проверить отдельно.
4. В MVP сохранить целую entitlement-selected ревизию и pinned session F12/F13. До реализации расширенной проверки решить Q14 и её внешнюю форму Q15; не делать реестр текущего профиля prerequisite Demo/MVP.
5. Если будет принято использование ClientContentProfile, сверить его cardinality/identity с этим решением, затем проектировать storage/adapter/API и тесты. Этот шаг отложен и не является текущим расширением.

## 13. Открытые технические вопросы

| Вопрос | Что блокирует |
| --- | --- |
| [Q14](../../../business/questions/Q14-client-compatibility.md#минимальная-техническая-граница-f14) | Источник/доказательство клиентской поддержки, точный unsupported result, другой целый snapshot и будущее использование текущего профиля |
| [Q15](../questions/Q15-api-and-source-code.md) | Финальный внешний wire/status/error profile; наличие модели не доказывает endpoint |
| [Q33](../questions/Q33-sync-resume-transport.md) | Владелец принятого Demo selection/plan protocol, execution ownership и ошибок; внешние транспортные проверки и клиентский durable apply остаются у F13/F19 |

Подробная архитектура совместимости отложена по бизнесовой F14. Наличие этих вопросов не превращает её в условие запуска Demo или основных функций MVP.
