# F13. Доставка и синхронизация контента

## Два этапа одной доставки

**Demo:** полный клиентский реестр точных схем/версий, серверный план Add/Update/Remove, отдельная загрузка точных данных и возобновляемая sync. Нет лицензий, авторизации, шины, внешнего каталога и полного Publishing.

**MVP:** отдельный ранее согласованный выбор разрешённой продуктовой ревизии и права. Описание ниже сохранено; атомарность Demo по структуре не разрешает по частям подменять лицензионную ревизию MVP.

## Demo: выбор версий и полный реестр

Клиент определяет и хранит весь нужный ему набор SchemaId + SchemaVersionId. Можно выбрать несколько версий одной схемы; нельзя запросить случайное подмножество текущего реестра как отдельную частичную sync. Это не запрос всех схем CM.

Сначала CM фильтрует опубликованные доступные версии данных по точной структуре, затем выбирает последнюю версию каждого экземпляра по доменному порядку номеров. Для схем и экземпляров публикация строго последовательная; v3 не публикуется после v4. Новые версии допускают подготовку на основе предыдущей или с чистого листа, не меняя опубликованные исходники.

Пример: I2/I5 по S1, I7 по S2. Выбор S1 даёт I5, S2 даёт I7; обе структуры дают два представления одного логического экземпляра. Новый Draft не вытесняет Published.

## Demo: Withdrawal и возврат

Withdrawal целой EntitySchema, exact EntitySchemaVersion либо logical EntityInstance исключает соответствующие representations из direct runtime выдачи; следующая sync возвращает Remove. Published history и exact historical tooling read сохраняются. Copy и Save recovery work снятого source/target разрешены с предупреждением, Publish к снятому target запрещён.

«Вернуть в выдачу» — отдельное явное действие, проверяющее допустимые Published representations и текущие ограничения всех затронутых scopes. Возврат instance не обходит Withdrawal его schema или exact schema version. Publish новой версии сам по себе Withdrawal не очищает. Withdrawal during sync сохраняет правила invalidation/atomicity ниже.

## Demo: план, данные и удаление

Клиент сообщает полный реестр и фактически установленное состояние в управляемой области. Start требует обязательные non-null массивы `selection` и `installedInventory`: явно пустые массивы допустимы, отсутствующие/null дают 400. Дубликаты выбранных пар или installed representations отклоняются, не исправляются автоматически. CM фиксирует план целиком либо возвращает ошибку: некорректный выбор или недоступный authoritative источник не создают частичный успешный план. SDK получает необходимые данные следующим шагом, не требует от приложения вручную выстраивать HTTP-вызовы.

Пара схемы/версии, от которой клиент отказался, удаляется из его локального управляемого набора; явно пустой `selection` удаляет ранее installed managed representations. Удаление не уничтожает историю CM и не очищает произвольные данные/кэш приложения. Ошибка чтения и неполученный реестр не трактуются как намеренный пустой выбор или доказанное отсутствие target.

Идентичность представления — SchemaId + SchemaVersionId + EntityInstanceId. Точный wire-контракт принадлежит [серверной F13](../../technical/backend/features/F13-content-delivery.md#33-dto-и-parameter-models). Несуществующая/недоступная запрошенная структура отклоняет Start целиком, не становится командой удалить её данные как будто целевой набор пуст.

Summary содержит только изменённые группы в стабильном порядке SchemaId, затем SchemaVersionId. Группа исключённой структуры сохраняется в плане с пустым target. Base integrity описывает полный исходный installed состав группы, target integrity — полный результат после Add/Update/Remove, включая неизменившиеся representations. Counts не заменяют проверку membership, exact versions, keys и hashes. Если изменений нет, успешный план имеет `groups:[]`, но Data всё равно выдаёт terminal `operationCompleted`; group receipts в этом случае нет.

## Demo: неделимый результат одной структуры

Минимальная атомарная единица — все экземпляры одной точной пары схемы/версии, например dogV4. Готовые изменения данных, удаления и сведения об установленных версиях применяются вместе. Неполный набор этой группы не становится рабочим.

Другие группы могут завершиться независимо. Если dogV4 недогружена, а hallV2 готова, разрешён результат «hallV2 обновлена, dogV4 не завершена». Загруженные части dogV4 сохраняются отдельно от активного набора для продолжения; конкретное клиентское хранение ещё уточняется. Неизменившиеся данные не обязательно скачивать снова для достижения полного результата.

## Demo: восстановление, кэш и отмена

Sdk.Client.DotNet генерирует и передаёт synchronizationId; Unity-разработчик не управляет им вручную. При следующем вызове после обрыва либо закрытия/аварийного завершения приложения SDK сначала восстанавливает durable context и committed receipts, читает Status, повторяет pending server receipts без local reapply и только затем получает Data от сохранённого checkpoint. Собственные commits не меняют original request fingerprint. Timeout, 409 и неизвестный сетевой результат не доказывают необходимость нового ID. Новый план создаётся от фактического installed inventory только при подтверждённой невозможности продолжения прежнего либо новом явном выборе приложения; staging не переиспользуется вслепую.

Demo CM хранит план в bounded in-memory кэше одного процесса. Настраиваемый разработчиком backend срок — **1 час по умолчанию**, абсолютный от reservation, включая preparation, streaming и stopped, без продления потоком/resume. Активные планы не вытесняются произвольно ради capacity: при заполнении новый Start недоступен, но deadline завершает и активную работу. Это не срок жизни опубликованных данных и не основание очищать установленный набор клиента. Restart CM теряет планы. 410 означает только известный expired/discarded ID, пока сохранён bounded tombstone; unknown ID, в том числе после restart или eviction tombstone, даёт 404. Сетевая недоступность/ошибка Status оставляет состояние неизвестным и не доказывает отсутствие плана.

Конкурирующий активный серверный запуск того же ID получает 409. Повторный прикладной вызов для одного stable store scope отклоняет раньше создания второй операции: «синхронизация уже выполняется», в том числе между экземплярами SDK. Поздние callbacks не изменяют новый запуск. Фактический cross-process lock готового adapter требует проверки Q29/Q32. Чтение статуса и безопасное resume не являются новым запуском.

Остановка прекращает чтение/transport, сохраняет staged records, cursor, installed groups и возможность продолжения. Уже начатую короткую неделимую фиксацию группы допустимо закончить, сохранив receipt; server ACK можно отложить до Resume. Окончательный отказ сначала останавливает исполнение и выясняет результат local atomic commit, затем очищает только незавершённое состояние попытки. При неизвестном результате commit разрушительная очистка откладывается. Installed data/inventory и committed receipts сохраняются; неполная группа не применяется.

Reservation получает PlanId до готовности metadata; Status позволяет восстановить его. Stop во время preparation сохраняет original input, а Resume может завершить первую подготовку, если готового плана ещё не было. Уже готовый immutable target при resume не выбирается заново. Plan/records/hash/cursor отделены от generation/lease исполнения: смена worker не меняет план. Control-команда относится к ожидаемому исполнению; устаревшая команда получает 409 и не перенаправляется SDK на свежую generation. Повтор команды с потерянным ACK возвращает её original result, не текущий Status.

## Demo: актуальность после успешной синхронизации

Продолжение всегда исполняет тот же план из кэша, даже если CM уже опубликовал новые данные. После полного успешного выполнения SDK делает отдельный проверочный запрос Freshness, названный пользователем **preflight**: сопоставляет actual installed inventory с актуальным содержимым для того же полного selection. Проверка не создаёт plan или execution lease.

Если появилось доступное изменение, SDK сообщает приложению. Новая синхронизация с новым ID/планом выполняется по выбору приложения, а не бесконечно автоматически. Проверка актуальности и успешная установка — разные результаты: состояние может быть успешно установлено по старому плану и уже требовать обновления.

Ответ `upToDate` либо `changesAvailable` содержит момент проверки `checkedAt` и изменённые exact schema/version группы `changedGroups` в стабильном порядке. Актуальность подтверждается только на этот момент. Ошибка authoritative comparison оставляет SDK результат Unknown, не отменяет успешную установку и не считается «обновлений нет». Draft, новая невыбранная структура и сбой отдельного Assets API не подменяют этот результат. Wire-контракт и алгоритм принадлежат backend F13.

## Demo: транспорт

Принято [новое транспортное решение](../../archive/content-manager-sync-streaming-transport-decision.md): компактный Start JSON содержит plan metadata; один обычный resumable streaming HTTP response передаёт records данных последовательно по exact SchemaVersion группам; подтверждения group commit отправляются отдельными requests. Assets читаются отдельно через CM-owned YARP gateway F05; upload/metadata/lifecycle остаются CM API. SDK вычисляет progress по metadata, устойчиво сохранённым records, validation и commit.

Data — UTF-8 NDJSON (`application/x-ndjson`), compact JSON record с LF; parser допускает CRLF. Приняты шесть kinds: `groupStarted`, `entityUpsert`, `entityRemove`, `groupCompleted`, `operationCompleted`, `operationInvalidated`. Обычные записи immutable плана имеют стабильную sequence 1..N; группы не перемежаются. Upsert передаёт `contentJson` как JSON string с исходным canonical JSON: после unescape SDK восстанавливает exact UTF-8 bytes и проверяет digest до typed parse. Nested object с повторной сериализацией и base64 не используются.

Полностью полученный и проверенный record сначала устойчиво сохраняется в staging вместе с cursor, затем продвигает application checkpoint. Network fragment или callback не durable checkpoint; malformed/unknown record или неполный хвост не успешные данные. Полнота target и исходный local basis проверяются до atomic local commit data/removals/inventory/durable receipt. Server receipt содержит plan/group/target hash/profile: идентичный повтор возвращает тот же ACK, другое evidence даёт 409. Потерянный ACK сверяется через Status/повтор receipt без повторного локального применения. `planState:completed` означает принятые server receipts, не доказательство установки клиента.

Полный успех требует валидного `operationCompleted`, всех local commits и server ACKs и корректного конца HTTP response. Withdrawal во время передачи invalidates старый plan: terminal `operationInvalidated` имеет зарезервированную sequence 0 и не имеет cursor, не заменяет planned record/checkpoint; неполная affected group не применяется, ранее committed группы сохраняются. Invalidation или truncated transfer исключают полный успех. Предпочтителен HTTP/3; при его недоступности библиотека продолжает через HTTP/2 с прежними прикладными гарантиями, без смены плана только из-за транспорта. Это требование, а не отчёт о проверенной совместимости Unity.

[Q33](../../technical/backend/questions/Q33-sync-resume-transport.md#execution-control-и-transport-profile) закрепляет public wire v1 и execution/control/Freshness semantics; модели, поля и literal fixtures принадлежат backend F13. Долгие network/client waits ограничиваются отдельными stage budgets; ожидаемая пауза во время local validation/commit/receipt не считается read-idle. В первом Demo stream compression отключена, heartbeat/empty lines не добавляются. Numeric settings выбираются по representative/load checks и проверяются при startup. Concrete common reason literals/API mappings, SDK/store signatures и actual adapter/Unity transport conformance ещё требуют материализации; документация не подтверждает их работу.

## Assets и ответственность клиента

Успешная sync данных означает обновление экземпляров и AssetId. Загрузка изображений/других файлов — отдельный Assets API SDK; приложение само выбирает момент вызова. Ошибка изображения не отменяет успешно установленную группу данных.

Внутреннее S3-compatible storage доступно через CM HTTP API, не напрямую из Core. Слой Unity интерпретирует файлы; Addressables является отдельным необязательным дополнением. Способ upload и полномочия не выводятся из факта существования публичного GET-файла.

CM сохраняет данные в постоянной БД. Запуск sync по кнопке/таймеру/при старте выбирает приложение. В Demo обязательны самостоятельная [F19](F19-unity-client.md) и отдельная [F20](F20-unity-demo-scene.md), которая использует библиотеку. Generated модели F22 не заменяют runtime-реестр или installed inventory.

## Launcher и selective preload после Demo

Digital Space Launcher вызывает F13 через Sdk.Client.DotNet и публикует согласованный локальный entity snapshot в общем SQLite. Entity stream содержит значения AssetReference с frozen пятью полями, без binary bytes. Unity app читает закреплённый snapshot и сама получает отсутствующие файлы из защищённого CM `/storage/assets/{assetId}` через тот же SDK download/cache coordinator. ProjectStorage поставляет исполняемые билды; CM поставляет сущности и media. Самостоятельные F19/F20 Demo остаются direct-sync consumers без этой интеграционной зависимости.

`DeliveryMode` задаётся на exact Published schema AssetReference leaf по [F01](F01-create-entity-schema.md#режим-доставки-assetreference-после-demo): default OnDemand, неблокирующий Preload, обязательные verified bytes для RequiredBeforeLaunch. Успешный entity sync сохраняется при ошибке optional media; RequiredBeforeLaunch блокирует запуск **этого snapshot** до готовности всех присутствующих обязательных ссылок. Entity readiness, required-media readiness, preload progress и разрешение запуска показаны раздельно. Уже запущенная программа остаётся привязана к своему snapshot; автоматический fallback на старый или hot reload не установлен.

Инженерная рекомендация — immutable resolved `AssetDeliveryManifest`, привязанный к той же permitted ProductVariant revision и exact schema/instance versions после всей композиции assignments/exclusions/overrides. Resolver рекурсивно сопоставляет definition × actual values, учитывает только присутствующие ссылки, агрегирует одну verified asset identity с самым строгим mode `RequiredBeforeLaunch > Preload > OnDemand` **внутри этого snapshot** и не объединяет права разных Variants. Shared bytes могут переиспользоваться при отдельной проверке scope. Manifest не entitlement и не второй editable owner политики; per-Variant override mode не включён.

Exact manifest API/IDs/profile/hash, build requirements ↔ SchemaVersion mapping и безопасный runtime re-admission ещё [Уточняется Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md). Рекомендованный manifest нельзя вставить в F13 wire v1 без отдельного совместимого решения. Entity plan TTL/receipt/resume не становятся lifetime media-download job; protected file auth/checkpoints F12/F13 сохраняются. Offline/revocation policy из наличия локальных файлов не выводится.

## Приёмка Demo

| Сценарий | Результат |
| --- | --- |
| Несколько точных структур одного экземпляра | Представления не теряются из-за общего InstanceId |
| Полный реестр изменён | Убраны локальные представления исключённых структур |
| План получен | Следующая загрузка получает точные версии, не latest на момент загрузки |
| Start malformed/unavailable authority | Ошибка всего Start, без частичного плана или failure-driven mass removal |
| Явно пустой выбор | Удалены только ранее installed managed representations через removal groups с пустым target |
| Изменений нет | `groups:[]`, terminal `operationCompleted`, ноль receipts; затем отдельный preflight |
| Не получена половина dogV4 | Никакая часть нового рабочего dogV4 не применяется |
| hallV2 готова, dogV4 нет | Допускается частичный результат по целым группам |
| Потеря связи | Есть путь продолжить по прежнему ID и проверенному сохранённому состоянию |
| Конкурирующий старт одного ID | 409, без второй активной операции |
| Второй SDK instance использует тот же stable store scope | Прикладной вызов отклонён до нового ID; stale callback не меняет новый запуск |
| Stop во время загрузки/первой подготовки | Checkpoint/original input сохранены; Resume продолжает прежнюю попытку, готовый target не пересчитывается |
| Discard при неизвестном результате atomic commit | Сначала выяснен outcome, затем очищена unfinished работа; installed/inventory/committed receipts сохранены |
| Кэш заполнен активными планами | Новый Start недоступен; active plan не вытеснен произвольно, absolute deadline не продлён |
| Sync данных успешна, файл не загружен | Данные доступны, отдельный вызов Assets API отвечает за файл |
| Ошибка схемы/неполный ответ | Нет успешной команды массового удаления |
| Приложение закрыли после загрузки части группы | Следующий вызов автоматически восстанавливает пригодный контекст и использует сохранённые порции |
| План доказанно отсутствует/истёк | Новый ID и план от фактически установленного набора, без стирания успешных групп; outage остаётся Unknown |
| Потерян ACK local committed группы | Status/тот же receipt возвращают ACK без повторного local apply; другое evidence даёт 409 |
| Terminal marker без корректного конца response либо без commits/ACKs | Полный успех не объявляется; Status completed не install proof |
| После старого плана уже появились новые данные | Старый план не меняется; проверка сообщает об изменениях, повтор выбирает приложение |
| Проверка актуальности не удалась | Установка не откатывается и актуальность не объявляется подтверждённой |
| HTTP/3 недоступен | Продолжение по HTTP/2 с теми же прикладными гарантиями |

[Q30](../../technical/backend/questions/Q30-content-synchronization.md) хранит принятые ответы, [Q33](../../technical/backend/questions/Q33-sync-resume-transport.md) — принятые правила восстановления/транспорта и их незакрытые технические детали; правила серверного удаления остаются Q09. [Этапы](../../milestones/README.md).

## Полная модель MVP: сессия, права и публикационная ревизия

Клиентский выбор относится к готовой итоговой revision Variant, выбранной через `ProductVariantPublicationState.ActiveRevisionId`, а не к Product ActiveRelease. При async projection Publishing activation ещё не означает клиентскую доступность: Delivery использует собственную readiness/effective history по Q17. Unpublish сохраняет revisions и добавляет null activation; frozen historical access определяется F12/Q04. Demo direct sync этих механизмов не требует.

### Подтверждённая видимость и catch-up

MVP historical availability определяется VisibilityConfirmedAt: durable UTC-временем primary observation exact уже committed полного Delivery transition. Время сохраняется один раз; оно не объявляется actual physical commit timestamp и не назначается до commit. Current committed state может обслуживаться при допустимом entitlement/restrictions до confirmation, но required pending time/history даёт IndeterminateDataNotReady для Freeze. Commit до UpdatesUntil с confirmation после либо ровно на cutoff не входит в historical Freeze.

Bus event — trigger ordered catch-up из public Publishing history с проверкой range completeness. После Sequence12 arrival15 необходимо получить13/14, а не перейти сразу к15. LastProcessedPublishingSequence отделён от current effective history и visibility time. Previous effective revision может обслуживаться во время catch-up, если права и restrictions остаются допустимыми.

Payload staging выполняется вне короткого coordination gate. Собственный Delivery commit атомарно фиксирует complete projection, pointer, history outcome, processed sequence и own inbox для одного real transition. R1/R2/R3 не получают фиктивную последовательную доступность одним commit. Terminal Withdrawn intermediate фиксируется как processed NotEffective без availability row, после чего обработка продолжает следующий transition. Technical artifact/materialization error означает NotReady/stall, не разрешение пропустить sequence.

Replay возвращает original outcome и ранее stored time; changed payload при том же Variant/Sequence — integrity conflict. При crash/ACK loss сначала выясняется durable результат. Если time ещё не была сохранена, recovery заново наблюдает exact committed transition и фиксирует fresh confirmation без backdating. UTC primary clock/stored precision и time order проверяются; обнаруженная ошибка порядка даёт DataNotReady без quiet clamp.

Последнее confirmed state строго до cutoff выбирается целиком, включая null; previous non-null fallback запрещён. Rollback старой revision — новый transition с новым confirmation, поэтому whole candidates сравниваются по effective transition order, а не возрасту artifacts. Complete empty history, confirmed null, pending confirmation и incomplete history имеют разные результаты; required Unknown не превращается в empty/null.

Правила: [Q17](../../technical/backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости). Exact wire/diagnostic contracts: [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#delivery-visibility-contracts).

### Проблема и цель

Передать один согласованный разрешённый состав без смешивания ревизий и лишнего скачивания неизменённых данных.

### Участники и доступ

Клиент-получатель предъявляет полный явно выбранный набор runtime credentials, которые CM проверяет в VR School для разрешённого Variant. CmDeliverySession отделён от staff/admin Identity и используется только для runtime Delivery; staff token его не заменяет. Demo остаётся anonymous и не требует этого протокола. Raw activation keys нужны только при Create/recovery session; приложение предоставляет их через свой provider, SDK не сохраняет raw key по умолчанию.

### Создание runtime session и replay

SDK устойчиво сохраняет CreationRequestId до первого вызова. CM проверяет весь explicit credential set без organization expansion, выбирает allowed whole candidate по F12 и при identical candidate использует минимальный LicenseId, затем credentialId. Session pin включает только selected basis: невыбранный credential/license revoke не отменяет pin, но при отказе selected basis нельзя fallback на иной key/license/revision.

Session bearer содержит 32 CSPRNG bytes в Base64url, передаётся только по HTTPS Authorization. Session хранит SHA-256 verifier, не plaintext token. Для same-request replay принимается единственное временное исключение из hash-only хранения: encrypted replay capsule ASP.NET Data Protection по умолчанию на 5 минут. Persistent shared keyring всех CM replicas защищён encryption at rest; token protection выполняется вне короткого commit gate.

Creation identity связывает original normalized Variant и verified credential/license IDs без raw keys, их hashes и volatile epochs/timestamps. Changed bound input при том же ID даёт409. Same CreationRequestId с fresh authentication и текущей pinned eligibility возвращает original committed Session/token/result, не выбирает новый candidate, не продлевает TTL и не восстанавливает invalidated session. Other actor не получает чужой receipt/token. Own Session и creation receipt атомарно committed до HTTP success; metadata receipt сохраняет исход outcome после capsule expiry, retention определяется engineering без нового числового default.

При DB/keyring outage retry использует тот же ID. Known ReplayExpired либо irrecoverable replay требует нового CreationRequestId/new admission. Новая session не закрывает другие автоматически; правило «одна session на license» не вводится. Lost ACK не считается failed creation и не вызывает blind новый ID.

Credential proof age CM считает monotonic от момента **до** VR School HTTP до final guarded check, включая crypto и gate wait; source ValidatedAt используется только для audit. Stale proof повторно получается вне gate. Source credential security epoch, nonsecret state/deadline фиксируются с outbox в одной source mutation; HTTP proof и broker updates имеют тот же epoch, старые facts не перезаписывают уже известный более новый epoch. Source LicenseWatermark hint требует CM projection не ниже hint; иначе DataNotReady, не guessed Allowed.

| Server-configurable default | Значение | Семантика |
| --- | --- | --- |
| Absolute session TTL | 30 минут | Минимум из creation+TTL, finite Deny UpdatesUntil и explicit credential deadline; не application ExpiresAt |
| Encrypted replay capsule | 5 минут | Original token recovery, не новая session/TTL extension |
| Maximum credential proof age | 30 секунд | CM monotonic interval до HTTP и через final guarded check |
| Maximum response lease | 5 секунд | Ограничен также SessionExpiresAt и ближайшей finite policy boundary, включая Freeze |

Это выбранные configurable defaults, не измеренные SLA. Нет sliding/refresh session: expiry требует нового admission. Existing session requests не вызывают VR School HTTP; обычный upstream outage сам по себе не останавливает их до TTL, если current local/public restrictions и facts остаются достаточными. [Q16](../../technical/backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#сессии-replay-и-response-leases), [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts) и [Q23](../../technical/backend/questions/Q23-source-synchronization.md#credential-validation-и-security-epoch) определяют exact contracts.

### Границы фичи

SyncSession, манифест, контрольные значения, загрузка данных/медиа. Не публикация и не объединение живой базы.

### Предусловия

Выбрана разрешённая whole revision, готовы её зависимости и fresh credential proof для создания/recovery. Абсолютные сроки и response lease приняты выше; конкретные wire fields/providers/error literals остаются технической детализацией.

Для нового MVP admission необходимы complete authoritative license/Variant facts, explicit CurrentPeriod и Delivery binding exact LicenseId + VariantId + SourcePeriodId с PolicyAgreement по F12. Agreement quote либо license projection отдельно не даёт права. Complete no-grant или confirmed revoke дают Denied с различимыми основаниями; missing required Agreement/period/source facts дают IndeterminateDataNotReady.

При явно предъявленном проверенном multi-license set любая required Unknown basis делает весь selection Indeterminate; KnownDenied + Allowed при известных фактах допускает Allowed whole revision. Explicit single-license selection допустим, implicit organization expansion отсутствует. Вступивший в силу Renewal/смена CurrentPeriod делают old-period session непригодной и требуют new admission, без old same-license Freeze fallback и без переименования этого события в revoke.

### Основной сценарий

1. Создать/recover session с durable CreationRequestId, fresh proof и exact selected basis; commit session/receipt до response, pin whole revision.
2. Отдать опись версий и контрольные значения; клиент сравнивает с локальными данными.
3. На каждом response checkpoint проверить session, selected basis/cutoff/restrictions и принадлежность snapshot; получить bounded lease до headers/cache/304/Range/proxy, затем выдавать bytes вне commit gate.
4. Проверить скачанное содержимое и переключить локальный рабочий набор после готовности согласованного результата.

### Альтернативные сценарии

Новая activation/effective R2, ordinary Unpublish, source Archive либо Retire не repin/revoke допустимую active-updates session R1. Частичная загрузка не заменяет рабочий набор смесью; hash не отменяет проверку прав. При expired Freeze pin должен совпасть с current eligible confirmed whole frozen candidate Q17: mismatch →NeedsNewAdmission, required missing history →DataNotReady. Current Deny cutoff применяется после Correction даже раньше прежнего SessionExpiresAt.

Correction сохраняет Agreement/pin: Allowed продолжает original TTL, known ineligible инвалидирует и требует new admission, Unknown временно блокирует без revoke. Renewal/CurrentPeriod change требует new admission. Extension/restoration не продлевают TTL и не resurrect expired/invalidated session. New session требует нового plan; old plan не перепривязывается молча. Installed client data не удаляются автоматически.

### Response checkpoints и leases

Перед каждым новым response короткий owner Delivery checkpoint по общему consistency guard сначала fresh читает primary/public Session, selected credential epoch, license/period/cutoff, pin/membership и authoritative bans. Gate освобождается **до** headers/cache hit/304/Range/proxy/bytes; долгий transfer не держит commit transaction. Неизвестные required facts дают временный blocked outcome, не allow.

Lease не длиннее configurable 5 секунд, Session expiry и ближайшей finite policy boundary, **включая Freeze**. Периодическое renewal заново авторизует response. Независимый monotonic deadline отменяет выдачу, даже если renewal заблокирован в gate/DB; поздний callback не возобновляет canceled response. Достижение boundary может требовать нового HTTP request, даже когда session после переоценки всё ещё Allowed.

Known committed CM revoke запрещает новый checkpoint и отменяет active response при обнаружении либо по текущему deadline. Это не zero-latency/physical 5-second SLA: bytes уже в buffers не отзываются, реальный VR School revoke до поступления facts в CM имеет asynchronous lag. Range/resume проходит fresh authorization; CM bearer не передаётся S3. После headers ошибка означает abort, SDK считает outcome Unknown и выясняет причину, не объявляет автоматически Revoked.

До headers категории отказа:401 bad/expired session,403 known rights denied,409 changed pinned basis/NeedsNewAdmission,503 required facts/upstream Unknown,404 resource вне snapshot. Stable literal machine codes и точная wire representation принадлежат Q15; после headers JSON error не вставляется в bytes.

### Бизнес-правила и ограничения

Манифест является описью, не лицензией. Integrity identity — profile+digest, не bare hex; immutable bytes/hash/profile сохраняются вместе без historical rehash. Schema/instance content, runtime manifest, whole target group, actual installed inventory и original plan имеют разные profiles [Q19 v1](../../technical/backend/questions/Q19-content-hashes.md#принятый-canonical-profile-v1). Full group после upserts/removals проверяется по exact context/membership/versions/Keys/profile+digests до atomic local apply; unknown profile/corruption не применяется/не «исправляется» automatic rehash. Inventory — только installed, не staging/selection; own commit не меняет original plan fingerprint. URLs/tokens/provenance не content identity, Asset binary hash отдельный; hash не заменяет права.

MVP ограничения instance/schema/media применяются к final effective closure выбранной materialized revision по F10. Suppressed inherited A не становится dependency выдаваемого B; общие необходимые B schema/media dependencies проверяются. Это не сужает historical retention/provenance, не меняет точную entitlement/session модель F12/Q16 и не превращает Publishing Activated в Delivery-ready.

Exact terminal revision ban независим от pointer и materialized snapshot. Withdrawal inactive/unmaterialized revision запрещает новую выдачу; late materialization/replay не снимают запрет. New admission и response checkpoints читают authoritative owner restrictions до cache hit/304/Range. Неизвестность temporarily blocks, known selected ban/revoke отменяет lease response по принятому протоколу; restoration не resurrect-ит invalidated token.

Staff diagnostics различают Publishing active, current Delivery effective, processed sequence/gap, materialization, pending visibility confirmation и exact ban. Одинаковые active/effective IDs не означают полной готовности истории и ограничений.

### Результат и дальнейшее использование

Клиент получает данные одной ревизии и необходимые зависимости; другие активации не меняют выбор посреди загрузки.

### Критерии приёмки

| Условие | Действие | Ожидаемый результат |
| --- | --- | --- |
| Сессия выбрала R31 | Активировать R32 | Сессия сохраняет R31. |
| Разрешена R31 | Запросить чужой asset ID | Доступ не обходится. |
| Совпал хеш, права нет | Проверить обновление | Хеш не даёт разрешения. |
| Загрузка оборвалась | Применить | Неполный состав не заменяет рабочий. |
| Grant отсутствует при complete facts | Новая выдача | Denied; отсутствие required Agreement/snapshot отдельно означает IndeterminateDataNotReady. |
| Allowed license и другая required Unknown в полном caller set | Новый multi-license selection | Весь selection Indeterminate, без выбора по частичным фактам. |
| Новый CurrentPeriod уже в силе и его Deny истёк | New admission/old-period session checkpoint | Нет старого Freeze fallback; old-period session требует new admission. |

| ID / семейство | Воздействие | Наблюдаемый результат |
| --- | --- | --- |
| V01 / cutoff | Complete commit до UpdatesUntil, confirmation после | Transition не входит в Freeze, несмотря на earlier commit |
| V02 / equality и precision | VisibilityConfirmedAt ровно на cutoff либо сравнение с UTC stored precision | Equality исключена, нет округления в разрешённую сторону |
| V03 / crash до confirmation | Transition committed, time не stored, recovery после cutoff | Fresh primary observation/time без backdate; старую visibility не синтезировать |
| V04 / lost ACK | Confirmation commit прошёл, ответ потерян | Lookup/replay original persisted time/outcome, без нового момента |
| V05 / два consumers и integrity | Concurrent duplicate Variant/Sequence либо changed payload same identity | Один original outcome; changed payload integrity conflict, history/time не дублируются |
| V06 / clock order | Primary UTC/stored time нарушает допустимый порядок | DataNotReady, без silent clamp/backdating |
| V07 / gap recovery | После12 получен15, полный source range13/14 существует | Обработаны13/14 до15, отдельно по transitions |
| V08 / incomplete range | Required13/14 отсутствуют или read unavailable | Catch-up stalled/Indeterminate; previous effective можно выдать только при действующих правах/restrictions |
| V09 / atomic visibility | R1/R2/R3 подготовлены, отказ одного owner commit | Один real transition на commit; нет torn projection/pointer/inbox или нескольких fake availability rows |
| V10 / terminal intermediate | Next transition на permanently Withdrawn revision | Processed NotEffective без availability row, далее catch-up продолжается |
| V11 / technical artifact failure | Next revision не удаётся материализовать | NotReady/stall, sequence не пропускается как будто terminal ban |
| V12 / null history | Последний confirmed transition до cutoff null | Нет frozen revision и older non-null fallback; late Unpublish не переписывает Freeze при неизменном cutoff |
| V13 / rollback и выбор | Старая artifact revision снова activated | Новый transition/time участвует по effective order; whole revision и stable license provenance, не artifact age |
| V14 / independent ban и protected reads | Withdraw inactive/unmaterialized, late replay, warm cache/304/Range | Authoritative ban до checkpoint; active response canceled по bounded lease protocol, без zero-latency обещания |
| V15 / completeness | Empty history/null/pending time/incomplete range либо required Unknown license | Outcomes различимы; Unknown full caller set Indeterminate, не synthetic empty/Denied |
| V16 / Correction и Agreement | Corrected UpdatesUntil при stored visibility и immutable PolicyAgreement | Cutoff пересчитан, Agreement и stored transition time прежние; new CurrentPeriod/no-fallback и pinned-session границы сохранены |

| ID / session family | Воздействие | Наблюдаемый результат |
| --- | --- | --- |
| S01 / ACK loss и actor | Create commit успешен, ответ потерян; same ID retry, changed bound input либо иной actor | Original Session/token/result после fresh auth/pinned check; changed input409, чужой receipt не раскрывается |
| S02 / две replicas/key rotation | Retry через другую CM replica, protection key rotation | Shared persistent encrypted keyring восстанавливает capsule в replay window; verifier hash-only |
| S03 / outage и replay expiry | DB/keyring недоступны либо known ReplayExpired/irrecoverable | Outage retry same ID; known recovery limit → new ID/admission, без закрытия остальных sessions |
| S04 / proof age | VRS HTTP/crypto/gate wait превышают 30 секунд или source ValidatedAt выглядит свежим | CM monotonic proof age проверяется через final gate; stale proof redo вне gate, source timestamp не продлевает freshness |
| S05 / epoch/watermark | Старый HTTP proof после более нового broker epoch; CM projection ниже hint | Старое не overwrites newer; required lag/Unknown → DataNotReady |
| S06 / gate expiry и boundaries | Session/credential/Deny deadline наступает во время wait; Freeze boundary в response | Equality Session/credential/Deny не разрешает выдачу; Freeze lease заканчивается на boundary, новый HTTP заново проверяет допустимость того же pin |
| S07 / active pin | Session R1, effective R2/ordinary Unpublish/Archive/Retire/upstream outage | Selected R1 allowed до original TTL при current restrictions, без VRS HTTP на existing request |
| S08 / expired Freeze | Pinned R1 отличается от current confirmed frozen candidate или history unavailable | NeedsNewAdmission без repin; Unknown → temporary DataNotReady |
| S09 / Renewal/Correction | CurrentPeriod сменился либо genuine correction/extension | Renewal invalidates old-period session; correction rechecks pin/Agreement; нет TTL extension/resurrection |
| S10 / scoped revoke | Unselected credential revoked; selected credential/license/grant/Revision withdrawn | Unselected session survives; selected scope denies/invalidate, нет иной basis fallback |
| S11 / cache/Range/proxy | Warm cache/304/Range/resume; запрос чужого snapshot resource | Checkpoint до cache/proxy; outside snapshot404; CM token не upstream S3 credential |
| S12 / blocked renewal | Renewal висит в gate/DB, приходит поздний success callback | Independent monotonic deadline cancels response; late callback не revive |
| S13 / response error и plan | Abort после headers либо new admission после истечения | SDK Unknown, не false Revoked; новая session/new plan, без silent old-plan binding; installed data retained |
| S14 / credential/token boundary | Raw key provider, staff token, default Demo | Raw key только Create/recovery без SDK default persist; runtime bearer отдельно; Demo anonymous |

### Открытые вопросы

[Q01](../questions/Q01-license-rights.md) · [Q04](../questions/Q04-withdrawn-publications.md) · [Q10](../questions/Q10-permissions-and-isolation.md) · [Q12](../questions/Q12-media-and-retention.md) · [Q15](../../technical/backend/questions/Q15-api-and-source-code.md) · [Q16](../../technical/backend/questions/Q16-activation-and-sessions.md) · [Q19](../../technical/backend/questions/Q19-content-hashes.md) · [Q21](../../technical/backend/questions/Q21-media-storage.md) · [Q22](../questions/Q22-quality-targets.md) · [Q23](../../technical/backend/questions/Q23-source-synchronization.md)

### Техническое описание

[Серверная спецификация](../../technical/backend/features/F13-content-delivery.md) · [Спецификация интерфейса](../../technical/frontend/features/F13-content-delivery.md)

## SDK и формальные контракты

Клиентская сторона обязана хранить выбранные схемы/версии и installed inventory. Unity F19 выполняет полный высокоуровневый sync: получает серверный план, загружает необходимые данные и обновляет локальный набор; приложение использует результат. Серверный алгоритм выбора/сравнения остаётся в F13 и не переносится в SDK.

Entity payload используется с моделями, генерируемыми через [F21 Registry](F21-schema-registry.md) и [F22](F22-csharp-model-generation.md) до runtime. Это не требует генерации при каждой синхронизации. AssetId является универсальным GUID, а интерпретация файла принадлежит платформе. Metadata fetch не запускает обязательную загрузку всех assets; конкретные ресурсы загружаются/предзагружаются явно по потребности.

SDK обеспечивает progress/cancellation и configurable defaults retries. Параметры retry уточняются Q29/Q30, остановка/commit и восстановление определены в Q33; наличие повторов не меняет смысл удаления, права и границы выбранного набора. Эти дополнения не отменяют полную лицензионную модель MVP.


## Key lookup и scopes Remove в Demo

Direct Demo desired state учитывает explicit withdrawal не только отдельного instance, но и EntitySchema / EntitySchemaVersion. Если scope withdrawn либо объект физически удалён допустимой Demo-операцией, следующая sync формирует `Remove` для всех фактически установленных representation, попавших под этот scope.

`EntityInstanceKey` является stable immutable key внутри EntitySchema и входит в данные/metadata, достаточные SDK для typed lookup по key. `EntityInstance.Name` является authoring label и не требуется runtime payload.

Снятие с выдачи не уничтожает Published history само по себе. Demo Archive/Restore отсутствуют; MVP Archive schema/instance будет дополнительно означать снятие с новой выдачи при сохранении исторических publication references.
