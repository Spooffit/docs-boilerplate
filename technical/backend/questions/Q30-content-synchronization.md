# Q30. Контракт серверной синхронизации

## Основание и область

Launcher/media integration [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md) сохраняет entity synchronization contract: Launcher вызывает SDK sync, app читает pinned snapshot. Media jobs/Range/checkpoints и manifest требований отдельно от F13 plan/groups/receipts/TTL; successful entity sync не означает media Ready. Proposed manifest carrier/API не расширяет public wire v1 до отдельного решения. Standalone Demo direct sync сохраняется.

[Canonical Q19](Q19-content-hashes.md#принятый-canonical-profile-v1) выбирает exact six profile IDs/SHA-256/64 lowercase hex и full target/actual inventory/original request-plan inputs. F13 owning typed models задают fixed members; SDK validates profile and exact typed content before complete target/removal checks and atomic installed+inventory+receipt apply. No LF transport framing in nested hashes/no current-profile guessing/new Registry dependency; сохранены group atomicity/resume и Demo one-process limits.

Ответы пользователя на блок 5 уточняют Demo-путь по схемам и их точным версиям. Сохраняются: HTTP API для любых клиентов, серверный выбор версий и вычисление Add/Update/Remove, высокоуровневая операция SDK и клиентское владение выбранными структурами/installed inventory. Полный продуктовый Publishing, лицензии, авторизация и шина не становятся предпосылками Demo.

Ниже отделены принятые ответы от ещё проектируемых механизмов. Модели/алгоритмы описываются в [backend F13](../features/F13-content-delivery.md), клиентское выполнение — [F19](../../frontend/features/F19-unity-client.md). Это не описание уже реализованного протокола.

## Принятые ответы блока 5

| № | Тема | Ответ и граница |
| --- | --- | --- |
| 1 | Создание и порядок публикации | Новую версию схемы или экземпляра можно подготовить на основе предыдущей либо с чистого листа. Публикация строго по порядку версий в рамках соответствующей схемы/экземпляра; поздняя публикация v3 после v4 недопустима. Выдача выбирает последнюю опубликованную доступную версию только после фильтра точной SchemaVersionId |
| 2 | Область синхронизации | Каждый новый sync охватывает весь выбранный клиентом реестр точных пар схем/версий. Отдельной частичной по области синхронизации сейчас нет. Реестр не означает все схемы CM: это набор, с которым работает данное приложение |
| 3 | Отказ от структуры | При исключении версии схемы из полного клиентского выбора её локальные представления удаляются из управляемого набора. Это не удаление серверного контента или произвольных файлов приложения |
| 4 | План и данные | Выбран вариант Б: сначала получить план с точными версиями, затем отдельными запросами получить необходимые данные. SDK скрывает последовательность; изображения не входят в тело плана как обязательные байты |
| 5 | Единица применения | Одна точная пара SchemaId + SchemaVersionId является минимальной атомарной единицей. Все необходимые изменения экземпляров этой пары применяются вместе. Между парами возможен частичный успех; половина нового набора одной пары не становится рабочим состоянием |
| 6 | Assets | Успешная sync данных фиксирует экземпляры и AssetId, не ожидая скачивания изображений. Файлы загружаются отдельным API SDK в выбранный приложением момент. Сбой Assets API является самостоятельным результатом, не отменой уже успешной sync данных |
| 7 | Идентификатор и продолжение | Sdk.Client.DotNet генерирует уникальный synchronizationId и передаёт его в CM; прикладной код не обязан создавать/передавать ID вручную. Конкурирующая активная синхронизация с тем же ID получает 409. Нужно продолжать прерванную операцию по прежнему ID и проверяемому состоянию, либо иметь возможность начать новую при несовпадении контрольных данных |
| 8 | Отмена | Ответ связан с продолжением из п. 7: прерванная работа должна допускать восстановление. Частичная фиксация одной структуры всё равно запрещена. В блоке 6 приняты остановка с resume и окончательный отказ, завершение уже начатого commit; default срока серверного плана 1 час, подробности Q33 |

Строгий порядок не устанавливает глобальную очередь публикаций всех схем и не требует совпадения нумерации схемы и экземпляра. Пропуски номеров после удалённого/отменённого черновика и доступные основания копирования уточняются в Q08/Q09; число одновременных черновиков не выведено из ответа автоматически.

## Клиентский реестр

Сейчас разработчик явно поддерживает известный приложению набор схем и версий в коде/конфигурации. Слова «держать в голове» не означают отсутствия машиночитаемого выбора в выполняемом запросе: SDK должен получить конкретный список.

В последнем ответе целевой конфигурационный файл назван `content-manager.json`; в архивном codegen-документе использовано `content-schemas.json`. Речь о направлении декларативной настройки клиентских зависимостей, а не о двух обязательных файлах. Окончательное имя, формат и интеграция с tooling ещё уточняются; автоматизация относится к production workflow, не требуется для Demo. Это не installed inventory и не серверный Schema Registry F21.

Новый запрос объявляет полный актуальный выбор. Продолжение ранее рассчитанной операции использует её зафиксированный выбор; изменение реестра не подмешивается в старый план. [Public wire contract v1 Q33](Q33-sync-resume-transport.md#public-wire-contract-v1) требует non-null `selection`/`installedInventory` arrays: missing/null и дубликаты дают 400; явно пустые массивы допустимы. Пустой selection удаляет ранее installed managed representations. Некорректный выбор/недоступный authoritative источник отклоняют весь Start, не дают partial successful plan или failure-driven mass removal. Модели и поля принадлежат [F13](../features/F13-content-delivery.md#33-dto-и-parameter-models).

## Пример атомарности

Клиент использует dogV4 и hallV2. Для dogV4 скачано 9 из 10 необходимых изменённых версий, а hallV2 готова полностью. Разрешено применить hallV2 и сообщить частичный результат. Для dogV4 остаётся прежний рабочий набор; девять загруженных записей могут храниться в подготовительном состоянии для продолжения. После получения последней записи весь результат dogV4, включая удаления и installed inventory, фиксируется вместе.

«Получить все инстансы» означает подготовить целый целевой набор данной структуры. Неизменившиеся уже проверенные записи не обязательно скачивать повторно: полный результат складывается из них и дельты. Атомарность не означает одну открытую БД-транзакцию на всё время сетевой передачи или один огромный HTTP-ответ.

## synchronizationId: принятая цель и проектное уточнение

Принято отсутствие ручного управления ID приложением; продолжение сохраняет прежний ID. Новая логическая синхронизация получает новый. ID операции не равен соединению, application cursor, контрольной сумме, ID версии или лицензионному разрешению.

**Принято:** различать запуск, чтение состояния/результата, продолжение и получение данных. Start/Data/Commit/Status и execution/control/Freshness semantics согласованы в [Q33](Q33-sync-resume-transport.md#execution-control-и-transport-profile). Immutable plan/records/hash/cursor не содержат worker generation/lease. Data atomically захватывает generation/lease, expired owner заменяется; stale worker/control command не меняет новое исполнение. 409 относится к действительному конкурентному захвату, stale expected generation или plan/receipt mismatch, а не ко всякому чтению существующей операции. SDK не перенаправляет stale control-команду на fresh generation. Lost control ACK восстанавливается повтором исходной команды, возвращающим original result snapshot; после eviction receipt сохраняется fencing.

Recovery идёт durable context/committed receipts → Status → pending server receipt repeats без local reapply → Data checkpoint. Timeout/409/unknown network outcome не доказательство необходимости нового ID. 410 только для known expired/discarded ID до eviction bounded tombstone; unknown/restart missing — 404, outage не отсутствие плана. Новый ID допустим при подтверждённой недоступности/expiry/discard/invalidation прежнего либо новом явном selection intent; фактический installed inventory authoritative, staging не переиспользуется вслепую.

**Принято:** сохранить неизменный план и отдельно учитывать полный исходный выбор, исходное установленное состояние, устойчиво сохранённые records и уже применённые группы. Собственный commit успешной схемы закономерно меняет inventory — это не повреждение контрольной суммы исходного запроса. При безопасно подтверждённой невозможности продолжения новая операция строится по фактически установленным данным, не очищает их автоматически.

Один ID не запрещает двум различным ID изменять одно локальное хранилище. Принята одна high-level sync на stable store scope между экземплярами SDK: второй вызов отклоняется до нового ID, без очереди/присоединения. Local run generation ограничивает callbacks/parser/control outcomes. Concrete store signatures и actual cross-process ready-store lock/crash guarantees проверяются Q29/Q32; документ не является adapter proof.

Один process-local coordinator на серверную операцию выполняет acquire/renew/revoke/transitions/receipts под короткой atomic guard, HTTP/DB I/O и client wait — вне неё. Generation проверяется перед record/state write/renew/group advance; late finally освобождает лишь свои ресурсы. Monotonic lease не продлевает absolute plan TTL. Receipt endpoint независим от worker lease: Offered закрепляется под current fence до отправки boundary, first receipt принимает лишь next unconfirmed offered group, identical receipt возвращает same ACK. Finite receipt wait ограничивает тишину, timeout прерывает execution и требует recovery, не доказывает invalid plan.

PlanId выделяется при atomic reservation и виден в Status ещё до ready metadata. Stop preparation сохраняет original input; Resume может закончить первую подготовку, если ready plan ещё не существовал, не recapture готовый target. Absolute TTL **1 час по умолчанию** начинается от reservation для preparing/streaming/stopped, не продлевается. Active plans не вытесняются произвольно ради capacity; заполнение делает новый Start недоступным, expiry всё равно останавливает active работу. Stop сохраняет staging/cursor/installed groups; started short atomic commit может закончиться с durable receipt/отложенным ACK. Discard сначала разрешает local commit outcome, при Unknown откладывает cleanup, удаляет лишь unfinished attempt и сохраняет installed/inventory/committed receipts.

## Streaming HTTP / HTTP/3 / QUIC

[Переданное транспортное решение](../../../archive/content-manager-sync-streaming-transport-decision.md) заменяет SSE state/progress + GET порций. Принято: компактный Start JSON metadata → один resumable streaming HTTP data response с последовательными exact SchemaVersion группами → отдельные group commit receipts. SDK вычисляет progress локально по metadata, durable staged records, validation и commit. Assets остаются отдельным API; дополнительных SSE channels нет.

HTTP/3 предпочтителен, при недоступности применяется HTTP/2 с тем же synchronizationId/plan/cursor. Q33 фиксирует NDJSON application/x-ndjson UTF-8/LF, camelCase envelope и шесть kinds, exact contentJson string, normal sequence1..N и terminal invalidation0/no cursor, Base64URL/HMAC cursor, receipt-before-next-group. Changed groups/полный base-target membership, no-change terminal и Start/Data/Commit/Status fields/statuses согласованы; owner tables/literal fixtures — F13. Фактическая Unity/proxy совместимость, codec/adapter conformance и transport settings проверяются отдельно.
## Что осталось уточнить

| Область | Незакрытая часть | Где |
| --- | --- | --- |
| Контракт | Public wire v1 и execution/control/Freshness semantics согласованы; literal common reason registry/API mapping, complete fixtures/OpenAPI и SDK signatures ещё материализуются | Q33; owner models F13/Q15, Q29/Q32 |
| Границы количества данных | NDJSON/variants/count/boundary fields/removals/receipt pacing выбраны; numeric record/buffer limits и real fixtures требуют configuration/verification; нет chunk GET API | Q33, Q22 |
| Идентичность и хеши | Canonical profiles/full membership/normal durable checkpoints выбраны; complete fixtures/shared-codec conformance ещё проверяются | Q19; owner models F13 |
| Продолжение | Absolute reservation TTL default1h/no extension/restart loss/capacity, generation/lease fencing/receipts/control replay приняты; controlled-clock/barrier/crash и actual codec/lifecycle conformance ещё требуются | Q33; owner models F13 |
| Отмена/конкуренция | Stop/Discard semantics и stable store scope single-writer согласованы; concrete SDK/store signatures и cross-process adapter tests ещё требуются | Q29/Q32 и Q33 |
| Транспорт | Separate stage budgets/config validation, Demo no compression/no heartbeat приняты; numeric sizing и реальные incremental Unity/HTTP3-H2/proxy timeout/buffering tests обязательны | Q33 |

Первичные правила удаления серверного контента остаются [Q09](../../../business/questions/Q09-editing-and-deletion.md). Вопрос не вводит личные права через клиентские selections: лицензирование MVP остаётся F12. Минимальная единица Demo не заменяет целую разрешённую публикационную ревизию MVP без отдельного решения.

## Применение

[F13 backend](../features/F13-content-delivery.md) — единственный основной серверный контракт; [F13 frontend](../../frontend/features/F13-content-delivery.md) и [F19](../../frontend/features/F19-unity-client.md) описывают выполнение. Правила создания/публикации применены в F02–F04, разделение assets — F05/Q21; [Q26](../../../business/questions/Q26-demo-mvp-scope.md) и [этапы](../../../milestones/README.md) отражают расширение Demo возобновляемой синхронизацией. Прогресс, отмена, настраиваемые retries и Unity 2022.3.62f3 уже приняты и не задаются заново.

## Ответы блока 6 и новая проверка актуальности

Основной ответ [Q33](Q33-sync-resume-transport.md): устойчивое восстановление после закрытия приложения; автоматическое resume либо новая операция при невозможном resume; остановка/отказ разделены; начатый commit можно закончить; второй вызов одного набора отклоняется до нового ID. Кэш плана на CM локальный, срок настраивается backend, default **1 час**.

Старый план остаётся прежним при новых публикациях. После полного успешного выполнения SDK обращается к отдельной проверке актуальности (preflight); сообщает о новых доступных данных, следующий запуск с новым ID/планом выбирает приложение. Это не тот же default, что автоматическое восстановление НЕУДАЧНОЙ попытки. Протокол чтения актуальности добавлен в backend F13/DF09; ошибка проверки не должна подменяться отсутствием обновлений или откатом installed.

Действующее транспортное решение заменяет прежний ответ блока 6 о SSE: Start JSON metadata, один resumable streaming HTTP response данных и отдельные group commit requests; progress локальный. При недоступном HTTP/3 применяется HTTP/2 с теми же ID/plan/cursor и прикладными гарантиями. Q33/F13 закрывают public wire, execution/control/Freshness semantics. Freshness — отдельный POST по полным selection/actual installedInventory без создания plan/lease: outcome `upToDate`/`changesAvailable`, `checkedAt` UTC и sorted exact `changedGroups`. Authoritative failure даёт error/SDK Unknown, не rollback и не автоматическую следующую sync.

Transport budgets разделены на unary/header/read-idle/record-assembly/server-write-backpressure/preparation/receipt-wait. Read-idle не учитывает ожидаемую local validation/atomic commit/receipt POST паузу; respective budgets/server watchdog продолжают ограничивать её. ResponseHeadersRead ограничивает HttpClient timeout до headers, body имеет отдельные budgets. Первый Demo stream без compression/heartbeat/empty lines. Durations/renew interval/buffers/capacity/tombstone retention задаются startup-validated configuration, renewInterval меньше leaseDuration, numeric sizing выбирается representative/load проверками. Retry bounded/transient/backoff/jitter; corruption/validation/confirmed invalidation не ретраятся как transient и не дают blind fresh ID. Literal registry, SDK signatures, adapter conformance и реальная транспортная совместимость остаются инженерными проверками, а не открытыми semantic развилками.


## Уточнение блока 10: removals ContentModeling

В Demo server desired set теперь имеет явные withdrawal scopes: entire EntitySchema, exact EntitySchemaVersion и logical EntityInstance. Любое representation из installed inventory, которое больше не входит в desired set из-за такого scope либо допустимого hard delete, возвращается как `Remove`.

Scope не меняет unit атомарности: plan всё равно группируется по точной SchemaVersionId, поэтому массовое withdraw schema раскладывается на removals соответствующих schema/version groups.

`EntityInstanceKey` передаётся/хранится как stable per-schema logical key, чтобы installed typed store поддерживал lookup by key. Он не заменяет IDs/version IDs в протоколе синхронизации и не используется как единственный идентификатор installed version.
