# Словарь

| Понятие | Смысл |
| --- | --- |
| Product | Абстрактный продукт: Тир, Музей. |
| ProductVariant | Конкретный проект: Московский тир, Музей собак. Общий для разных организаций и лицензий; локальные отличия задаются вариантом. Новое имя вместо ProductVersion внутри CM, не автоматическое сопоставление legacy Project. |
| VR School | Владелец нового каталога Product/ProductVariant и лицензионных фактов; изменения передаются CM через шину. |
| Project Storage | Существующий сервис старого каталога Project/Application и файлов приложений для лаунчера; не Delivery динамического контента. |
| Получатель контента | HTTP-клиент CM; сейчас Unity-проект, включая возможный лаунчер. Роль приложения не задаёт особую продуктовую модель. |
| Конфликт каталога | F15 incompatibility newest source с mutable/in-flight CM dependencies. Manual repair/Resolve либо compatible System SourceChangedApply закрывают same Open episode; не ingestion fault/Withdrawal. |
| Catalog V2 snapshot | Full source Product/Variant state с immutable IDs и positive int per-entity SourceVersion; source-normalized names/Description, gaps allowed, duplicate names не identity. Models только backend F06. |
| Catalog durable acceptance | Atomic newest full candidate + watermark + recoverable work до отдельного apply. Accepted v22 запрещает apply v21 до processing v22; broker ACK не readiness. |
| CatalogIngestionFault | Durable technical unreadiness trusted known entity/higher malformed/unsupported source: scoped new-write/stale-apply block, не F15/global/revoke. Unknown entity не fake blocker; unchanged unsupported-message replay отдельна от changed-payload correction. |
| Catalog reconciliation | Protected operational VR School republish selected/all current state incl parents. Scheduled/errors не applied/readiness; absent ID не delete, no bootstrap/all-history replay. |
| Архивирование каталога | Завершение использования для новых назначений при сохранении согласованного сопровождения и истории; не отзыв лицензии. Точные состояния уточняются. |
| Demo | Первый этап проверки требований и гипотез с заказчиком, до окончательной фиксации объёма MVP. |
| MVP | Следующий после Demo этап с учётом подтверждённых требований и необходимых правок. |
| Сборка клиента | Выпуск приложения с конкретной реализованной логикой; не ProductVariant. |
| EntitySchema | Логический тип контента, например «Карточка экспоната». |
| EntitySchemaVersion | Конкретная структура полей этого типа. |
| DateTime TemporalSemantics | Immutable смысл DateTime definition: historical Naive без offset либо MVP Offset — конкретный UTC instant с известным offset. Смена semantics требует нового FieldId; new MVP fields — Offset. [F01](features/F01-create-entity-schema.md). |
| Offset DateTime | Authoring сохраняет original whole-minute offset и 100ns precision; runtime нового profile — UTC с семью fractional digits и Z. Equality/bounds/order — по UTC instant; offset не timezone/DST и не secondary key. [F04](features/F04-entity-instances.md). |
| EntityInstance | Логический объект, например карточка «Хаски». |
| EntityInstanceVersion | Конкретное состояние объекта по определённой версии схемы. |
| ID | Идентичность объекта/версии; не отображаемое название. |
| Key | Смысловой ключ, отдельный от ID. В Demo специалист вводит `camelCase`; field key уникален во всём дереве конкретной версии схемы. Для MVP целевой формат — `kebab-case` с предложением по русскому названию. Ключ схемы допускается менять после публикации; глобальная уникальность schema key и последствия для старого lookup/контрактов ещё уточняются. |
| ProductContentAssignment | Редактируемые назначения схем продукта с политикой выбора экземпляров; release фиксирует exact результат. |
| Назначение схемы | Exact Published SchemaVersion и AllExcept либо SelectedOnly; в UI «Все», «Все, кроме» или «Только выбранные». |
| Schema replacement Variant | Собственный выбор схемы, заменяющий всю inherited группу её logical SchemaId; не local addition поверх неё. |
| ProductVariantContentAssignment | Локальные дополнения, замены и исключения конкретного варианта. |
| ContentAssignmentItem | Ссылка на конкретную опубликованную версию с идентичностями и метаданными. |
| ContentExclusion | Исключение наследуемого элемента либо схемы. |
| Release | Снимок назначения; после публикации его состав не переписывается. |
| ProductVariantPublicationRevision | Готовый объединённый состав варианта из конкретных исходных релизов. |
| Материализация | Сохранение полного итогового списка ссылок; не копирование всех JSON/файлов на пользователя. |
| PublicationState | Текущий выбор активного артефакта; не вся история. |
| ActiveReleaseId | Выбранный общий release Product для current базы Publishing; не активирует клиентский состав Variant сам по себе. |
| ActiveRevisionId | Authoritative nullable указатель ProductVariantPublicationState на итоговую revision; null означает unpublish. |
| Активация | Publishing pointer + append-only history transition. Current committed Delivery state и historical visibility confirmation учитываются отдельно; Activated не означает полной Delivery readiness. |
| Activation Sequence | Серверный monotonic порядок history внутри ProductVariant, в том числе при одинаковых timestamps. |
| Effective момент | Для MVP historical availability определяется conservative VisibilityConfirmedAt после primary observation already committed exact full Delivery transition; не alleged physical commit timestamp. [Q17](../technical/backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery). |
| VisibilityConfirmedAt | Durable immutable UTC время confirmation primary observation; Freeze требует строго < UpdatesUntil. Missing required time/history → Indeterminate, recovery не backdates. |
| LastProcessedPublishingSequence | Позиция ordered Delivery catch-up; отдельно от effective availability history/time. Terminal Withdrawn может быть processed NotEffective без availability row. |
| Terminal revision ban | Authoritative permanent запрет exact Withdrawn revision, независимый от active pointer/materialized snapshot; late replay/materialization не снимают его. |
| PublishedAt | Дата публикации самого артефакта; не всегда фактическая доступность варианта. |
| Rollback | Новое переключение на прежний состав с сохранением истории промежуточного периода. |
| Unpublish | Переключение Variant pointer в null с новой null activation record; не пустая revision и не удаление истории. |
| ContentEntitlementPolicy | Политика права на контент; настраивается в Publishing, применяется в Delivery. |
| PolicyAgreement | Immutable Publishing reservation policy при commit для exact LicenseId + VariantId + SourcePeriodId и original planned parameters/applied parent provenance. Не даёт grant; не переоценивается при confirmation/first-use. [F12](features/F12-content-entitlement-policy.md). |
| SourcePeriodId | Source-owned identity planned/confirmed периода; Renewal и изменение unconfirmed plan требуют новой identity. Variant scope Agreement/binding сохраняется отдельно. |
| GrantPeriodId | Delivery-owned identity confirmed period binding с exact Agreement, frozen policy и source/version provenance. |
| CurrentPeriod | Explicit source выбор периода LicenseId + VariantId. Его смена при Renewal делает old-period session непригодной и требует new admission без old Freeze fallback; не является revoke. |
| IdentityAccess | Отдельный CM BC учётных записей сотрудников на ASP.NET Core Identity; отдельно от VR School/runtime grants. Infrastructure ContentManagerUser — единственная модель account без duplicate Domain StaffUser. |
| Specialist / Administrator | Fixed system staff roles: общий контент, отдельные authoring/admin обязанности; комбинируются без implicit inheritance. Catalog/initial RolePermission server-fixed. |
| PermissionSnapshot | Один fresh coherent staff permission result на HTTP request без cross-request cache. Role change действует next read; checked in-flight может finish. |
| AllowedActions | Advisory capability результата permissions + resource/domain rules для UX; не command permit/credential. |
| StaffSession | Own Guid staff user/session time/absolute expiry с terminal revoke, default 8 часов без sliding/refresh/RememberMe. Logout current отдельно от остальных; deactivate/reset invalidates previous. |
| CredentialReady | Пользователь завершил setup permanent password; last-admin guard сохраняет IsActive + Administrator + CredentialReady при concurrent Identity writes. |
| Form recovery snapshot | Memory actual input/File objects + original actor/exact owner/object/version/ETag/base/pending context в open SPA tab. Same-user internal reauth без baseline overwrite/autosave; logout/different actor clears, reload/close не guaranteed. |
| Expected login context | Mutation precondition против stale-actor вкладки, связанная с текущим staff login; не credential и не замена auth/CSRF. Exact carrier Q15. |
| Activity History | Immutable persisted MVP журнал independent committed business results на /admin/activity; не eventstore/undo/securitylogs или замена F11/F17 histories. Eventual append/manual refresh, no-op/replay не new row. |
| Audit Committed outcome | Own durable result, включая typed partial/Failed business workflow state; не обещает activation всех targets и не включает failed command без commit. |
| Audit actor / initiator | Staff/System/Integration/Operator executor из trusted context; persisted staff initiator automatic operation отдельно, без fake CM staff для внешнего/operator. |
| Audit visibility | Common content audit.activity.read для Spec/Admin; staff-access audit.staff-access.read только Admin. No personal ownership filter/target-read permission inference, unknown profile не safe default. |
| Audit as-of label | Optional safe captured actor/target caption и exact typed ID; rename/deactivation/deletion не rewrite/cascade history, Draft link открывает current exact identity. |
| SourceOccurredAt / AuditRecordedAt | UTC source result/append observations для журнала; не physical commit timestamp/causal clock. |
| Renewal | Новый SourcePeriodId + Agreement для продления/дополнительной купленной длительности/unlimited upgrade. |
| Correction периода | Явно типизированное исправление того же confirmed period с reason/history, сохраняющее Agreement/agreed policy; current commercial facts и Freeze cutoff могут измениться. |
| IndeterminateDataNotReady | Required authoritative license/period/Agreement facts неизвестны/неполны; отдельно от complete no-grant или confirmed revoke Denied. Любая required Unknown в caller multi-license set делает selection Indeterminate. |
| FreezeAtExpiration | После окончания срока сохраняется разрешённый исторический состав, но не новый. |
| DenyAfterExpiration | MVP policy запрета новой выдачи после UpdatesUntil; agreed policy периода не заменяется поздней current policy. |
| ContinueWithLatest | Возможная стратегия продолжения обновлений; не выбранный общий default. |
| Grant | Лицензионное право на конкретный вариант; родство продуктов его не расширяет. |
| SyncSession | Runtime Delivery pin selected credential/license/Variant/period/Agreement/whole revision с absolute TTL. Не staff session; новая session требует нового plan без silent rebinding. |
| CmDeliverySession | Отдельный CM runtime bearer по HTTPS Authorization: 32 CSPRNG bytes Base64url, Session хранит SHA-256 verifier. Staff/admin token не заменяет его, Demo anonymous. |
| CreationRequestId | Stable SDK-persisted до Create ID, bound original normalized Variant + verified credential/license IDs без raw-key/hash/volatile epochs. Replay возвращает original Session/token/result после fresh auth/pinned check, без TTL extension. |
| Replay capsule | Temporary encrypted ASP.NET Data Protection token-recovery exception к hash-only Session storage; shared persistent encrypted-at-rest keyring. Outage retry same ID, known expired/irrecoverable → new admission. |
| Credential security epoch | Source version credential security facts с same-mutation outbox; HTTP и broker proof одной epoch, stale facts не overwrites уже известную newer epoch. |
| Response lease | Bounded разрешение конкретного response до Session expiry/next finite policy boundary включая Freeze. Independent monotonic deadline stops blocked renewal; late callback не revive canceled response. Не physical latency SLA. |
| Манифест | Опись версий и ресурсов, которые должен иметь клиент. Не сами данные и не самостоятельная лицензия. |
| Hash | Контрольное значение согласованного представления данных; не доказательство права доступа. Asset binary hash и staff/runtime credential verifier не canonical content hash. |
| Canonical content digest | SHA-256 exact canonical UTF-8 bytes,64 lowercase hex без prefix; сравнивается вместе с profile. Не identity artifact/Approval/license/access proof. |
| Canonical profile | Версионированное правило typed input/bytes: cm-schema-content/1, cm-instance-content/1, cm-publication-manifest/1, cm-target-group/1, cm-installed-inventory/1, cm-sync-plan/1. Immutable bytes/profile/digest хранятся вместе; новое byte-changing правило — новый profile, old snapshots не rehash-ятся. Registry cm-json-schema/1 и C# cm-csharp/1 самостоятельны. |
| Runtime manifest / target group integrity | Manifest фиксирует runtime membership/aliases/content references без source provenance; group — full resulting exact-schema target после upserts+removals, не только downloaded delta. Unknown profile/mismatch блокирует group apply. |
| MediaAsset | Учётный ресурс с immutable bytes; исторические публикации требуют стабильного содержимого. Не owner глобального DeliveryMode. |
| AssetReference runtime descriptor | Frozen пять полей: AssetId, AssetKind, ContentType, SizeBytes, SHA-256 bytes. URL строится из CM configuration; DeliveryMode не шестое runtime поле. |
| AssetDeliveryMode / DeliveryMode | Policy AssetReference leaf точной SchemaVersion, в том числе recursive ItemDefinition: OnDemand default, Preload, RequiredBeforeLaunch. Не property MediaAsset или instance value; [F01](features/F01-create-entity-schema.md#режим-доставки-assetreference-после-demo). |
| AssetDeliveryManifest | Рекомендованная immutable разрешённая опись реально присутствующих media ссылок exact revision/snapshot и derived effective mode. Exact API/profile ещё Q37; не entitlement и не F13 entity stream. |
| RequiredBeforeLaunch | Gate запуска конкретного snapshot до verified bytes всех присутствующих обязательных references; не IsRequired и не запрет запуска из-за пустого optional поля. |
| Shared SDK media cache | Общий Launcher/app file cache; persistent index/jobs в SQLite, большие binary bytes отдельными файлами вне папки build. После Demo по [F19](features/F19-unity-client.md#launcher-и-общий-media-cache-после-demo). |
| Verified media Ready | Завершённый файл с проверенными full size/SHA-256; Downloading/.part или успешный entity sync не Ready. |
| BC | Граница модели и ответственности внутри Content Manager, не обязательный отдельный сервис. |

Tags и Pack являются устаревшим решением и не используются для текущей связи каталога с контентом. Использованные схемы определяются из назначений либо зафиксированной ревизии, смотря какой состав рассматривается. Полные основания: [решения](adr/ADR01-content-organization.md).

## Контракты и SDK

| Понятие | Смысл |
| --- | --- |
| `Sdk.Client.DotNet` | Общий runtime SDK для .NET/C#-клиентов; новое имя вместо `Sdk.Client.Core`. Не универсальная обязательная библиотека для любых языков. |
| OpenAPI | Формальный контракт обычных HTTP endpoints, requests/responses, status codes и предусмотренной аутентификации. |
| JSON Schema | Языконезависимое описание динамических схем сущностей. |
| Schema API / Schema Registry | Официальная точка получения машинно-читаемых контрактов выбранных схем и версий; обязанность F21. |
| Языковой SDK | Конкретная реализация потребления общих контрактов для своего языка/runtime; не источник контракта вместо сервиса. |

Основание: [дополнение архитектуры SDK](../archive/sdk-contracts-architecture-update.md). Возможные другие SDK и targets генерации не обозначают уже реализованные или включённые в Demo продукты.

| Draft version number | У Draft схемы нет бизнес-номера опубликованной версии; до публикации он идентифицируется `SchemaVersionId`. Номер назначает система при F03. |
