# Q13. Первый UI

## Runtime session и recovery

Принятый [Q16](../../backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases) отделяет CmDeliverySession от staff/admin scheme. SDK сохраняет CreationRequestId и nonsecret intent/provider context до POST. Trusted source identities формирует CM после validation; SDK не сохраняет raw activation key/hash по умолчанию. App provider предоставляет key для нового admission/replay. Повтор с тем же ID возвращает original result без TTL extension, выбора нового candidate или restore. Временный DB/keyring failure требует same-ID retry; proven replay end — нового ID/admission. Staff/raw-key form и token viewer не добавляются.

Pin R1 при active updates сохраняется после R2/ordinary Unpublish. После Freeze cutoff он сравнивается с current eligible confirmed frozen candidate; CurrentPeriod change/correction может требовать нового admission. Required unknown даёт временный DataNotReady без revoke; restore/expanded cutoff не оживляют invalidated credential. Server defaults — TTL 30 минут, replay 5 минут, validation attempt 30 секунд, response lease 5 секунд — могут отображаться как server metadata, без client duration selectors. Проверяется полный explicit credential set; Session фиксирует selected basis/provenance. При одинаковом candidate выбираются smallest LicenseId, затем CredentialId.

Каждый request/Range/resume авторизуется до cache/304/bytes. Независимый lease deadline прекращает transfer при stalled renewal, late callback не возобновляет его. Обрыв после headers означает SDK Unknown до получения typed result; автоматические revoke, удаление installed data и old-plan rebind отсутствуют. До headers приняты категории 401 credential, 403 confirmed rights denial, 409 new basis, 503 unknown/unavailable, 404 outside scope. Exact DTO/machine codes/access задаёт [Q15](../../backend/questions/Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts); SDK provider/storage adapters остаются инженерной детализацией.

## IdentityAccess и hosting сотрудников

Последнее уточнение пользователя: **Blazor Standalone**, отдельно хостящиеся UI и API на HTTPS-поддоменах общего base domain; это different origins/same site, не shared UI-origin proxy. [Q24](Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы) фиксирует API host-only cookie/credentialed exact-origin CORS/CSRF и Core IdentityAccess, fixed-role grants/request-local fresh permissions/advisory AllowedActions. Web stack не назначает Unity/F19 и не вводит staff dependency Demo.

F16 scopes: SignIn/current-user, admin user-role/lifecycle/setup, same-tab reauth recovery. Role catalog readonly/server-fixed, no custom role/permission editor. Staff Session8h/no sliding, setup/reset20min, logout current Session/Reactivate new login. Recovery actual input/File/original owner/object/version/ETag в памяти живой SPA-вкладки без reload; close/reload/restart excluded, other user/explicit Logout clear. Mutation expected-login-context guards browser account switch; exact carrier Q15. Не добавлять localStorage/IndexedDB/autosave/backend form draft ради восстановления.

## Activity History F18

Выбраны `/admin/activity`, common audit.activity.read для Specialist/Administrator и staff-access audit.staff-access.read только Administrator. Source confirmed business facts с Outcome=Committed/typed partial details, optional as-of actor/initiator/target captions, exact IDs и owner reason/target read. Mutable Draft reference открывает текущую exact identity, не historical content copy; author rename/deactivation не меняет прошлые подписи.

Filters actor/kind/action/type+exact ID, UTC `[From,To)`, correlation/operation; default no time bound. Keyset OccurredAt DESC/Id DESC, default50/max200, NextCursor/HasMore без required total count; reader/query reset/current permission on every page. Options authors из visible journal, не admin Identity directory; backend visibility before keyset/options. Manual Refresh first page, no polling/realtime/local final row после чужого action. Empty list не backlog-readiness proof, persisted rows могут прийти позже/out of order. Detail shows source/audit times separately, safe typed metadata и independently authorized F17 reason/target.

[F18](../features/F18-staff-activity-history.md) — composition once; [Q15](../../backend/questions/Q15-api-and-source-code.md#f18-принятый-activity-contract)/[Q20](../../backend/questions/Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read) — chosen read/append and remaining literal contracts. Reload перечитывает server journal; F16 same-tab memory recovery only retains allowed query/selection and clears other-user/logout state. Retained MVP rows no automatic purge/user-delete/export/notifications.

## Delivery: committed state и подтверждённая история

Staff representation различает Publishing active, Delivery committed effective, LastProcessedPublishingSequence и saved visibility confirmation/temporal readiness. Gap, materialization failure, pending confirmation и terminal ban имеют отдельные причины. Совпадение nullable RevisionIds не доказывает full readiness; pending necessary history — DataNotReady, не no-publication/InvalidKey. Current committed state может быть доступен при достаточном current right, но expired Freeze требует собственного complete confirmed history.

Freeze использует persisted VisibilityConfirmedAt < UpdatesUntil: original commit-before-cutoff/confirmation-after-cutoff не входит. Replay возвращает прежний stored timestamp/outcome, recovery без saved evidence не backdates. Null/rollback/terminal intermediate объясняются без older fallback/fake availability. Authoritative DTO/exposure/access/scoped tags ещё [Q15](../../backend/questions/Q15-api-and-source-code.md#delivery-visibility-contracts); новые страницы/forms/polling interval не выбираются. [Q17](../../backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery) / [Q20](../../backend/questions/Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости) — выбранная semantics, session/stream timing — Q16.

## F12: согласование периода и diagnostics

PolicyAgreement выдаётся authenticated VR School backend, без staff/Unity issuance form. Pending оформления CM-content периода показывается в source; его экран/коммерческие действия принадлежат VR School, не CM. Application-license и Project Storage сохраняют независимый поток. CM показывает current configured policy отдельно от immutable agreed policy/provenance и current confirmed commercial facts: AgreementId, license/Variant/source period, cutoff/unlimited, applicable CurrentPeriodId и completeness. Missing Agreement/facts — Indeterminate/DataNotReady, complete no-grant — Denied, confirmed revoke — отдельная причина; valid credential не превращается в InvalidKey.

Renewal — новый период/Agreement; явно классифицированная correction сохраняет promise и обновляет cutoff. После вступившего renewal old-period session basis unusable/new admission, no old same-license Freeze. Unknown full caller verified multi-license basis blocks selection; UI не скрывает его для successful partial result. Exact DTO/error wire остаются [Q15](../../backend/questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary), source workflow — [Q23](../../backend/questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow). Pinned TTL/replay/renew-correction/lease cancellation выбраны [Q16](../../backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases), physical integration/wire ещё проверяются.

## Вопрос и известные границы

Какие фронтенд и экраны уже существуют? Какие поиск, фильтры, пагинация и массовые действия нужны? Где находятся актуальные исходники и макеты?

Уже определено: прежнее обсуждение одноколоночных таблиц не подтверждает конкретный итоговый дизайн. Продуктовые ограничения определяются бизнесовыми F-документами.

**Ответ:** Текущий UI-инвентарь подтверждён snapshot-кодом 01.10.2026. Target workspace/navigation/routes, server-side search/filter/pagination, отсутствие generic bulk actions без отдельного use case, владение состоянием, FSD и direct NSwag models приняты ниже. Открыты только явно невыбранные UI-детали, числовые настройки/test contour и согласование конкретного server wire contract; snapshot не утверждает готовность target.

## Затронутые фичи

[F01](../../../business/features/F01-create-entity-schema.md) · [F02](../../../business/features/F02-create-schema-version.md) · [F03](../../../business/features/F03-publish-schema-version.md) · [F04](../../../business/features/F04-entity-instances.md) · [F05](../../../business/features/F05-media-assets.md) · [F06](../../../business/features/F06-product-catalog.md) · [F07](../../../business/features/F07-product-content-assignment.md) · [F08](../../../business/features/F08-variant-content-assignment.md) · [F09](../../../business/features/F09-content-releases.md) · [F10](../../../business/features/F10-publication-revisions.md) · [F11](../../../business/features/F11-publication-lifecycle.md) · [F12](../../../business/features/F12-content-entitlement-policy.md)

## Как применить ответ

Уточнить связанные спецификации в их собственных разделах. Не считать заглушку принятым решением.

## Current-state snapshot 01.10.2026

Источник: [срез backend/frontend](../../../archive/current-state-code-snapshot-2026-10-01.md). Это **инвентаризация текущего кода, а не утверждение целевого дизайна**.

Во frontend snapshot используется Blazor WebAssembly + MudBlazor. Найдены routes: schemas list/create/details, instances list/create/details, media assets, catalog, publishing product/variant workspaces. Физически существуют проекты App/Common/Entities/Features/Pages/Shareds/Widgets, но `ContentManager.Features` не содержит source-фич, а Pages сами выполняют заметную API orchestration. Поэтому наличие слоёв не подтверждает, что текущая декомпозиция является желаемой.

Для schemas существуют две частично пересекающиеся линии редактора: используемый route-page `EntitySchemaPage + SchemaFieldsEditor` и `EntitySchemaFieldManager/EditEntitySchemaFieldDialog`, где сохранение Draft всё ещё помечено TBD. Текущий route-page использует major/minor buttons, блокирует второй Draft и сортирует версии по Major/Minor. Это current implementation и прямо расходится с согласованной target-моделью блока 7.

Current schema editor позволяет менять key существующего field и required в обе стороны, показывает InheritanceStrategy/ColorFormat и numeric Enum/Flag values; Type существующего field read-only, а действие «Заменить тип» создаёт новую identity. Эти детали используются только как отправная точка рефакторинга; target правила находятся в Q34/F01/F02.

Current instance page выбирает только Published schema versions, блокирует второй Draft, требует timezone в DateTime helper и использует numeric bitmask для FlagEnum. Эти факты **не закрывают** target-вопросы в пользу текущего поведения.

## Решения блока 18: информационная архитектура UI

### Общий принцип

Основная модель административного интерфейса — **workspace**, а не цепочки одноколоночных таблиц и отдельных CRUD-страниц для каждой вложенной сущности.

Workspace объединяет identity объекта, versions/history, текущий editable/read-only state, связанные действия и contextual navigation. Список служит поиском/входом в workspace, а не вторым полноценным редактором.

Текущие routes и компоненты snapshot 01.10.2026 не являются compatibility contract: проект ещё не запущен, поэтому URLs/component composition можно перестроить под target UX.

### Demo navigation

В Demo сохраняется Home, даже если он практически пустой.

Основная навигация:

- Home;
- Схемы;
- Экземпляры;
- Ресурсы;
- Генерация моделей.

В Demo отсутствуют Catalog, Publishing, Licenses, Moderation, Users/Roles, Activity и Archive как MVP lifecycle-area.

### Schemas

Список схем показывает минимум Name, Key, число Draft, последнюю Published version, состояние выдачи/withdrawal и дату изменения. Нужны поиск по Name/Key и server-side pagination. Generic mass selection и bulk actions без отдельного use case не добавляются.

Создание имеет отдельный route, но переиспользует тот же editor/workspace model. Frontend не генерирует ID новых fields.

Schema Workspace содержит:

- header/identity;
- version selector/rail с несколькими Draft и Published versions;
- read-only Published view;
- editor выбранного Draft;
- tree структуры + inspector выбранного field;
- validation/publish blockers;
- действия создания Draft, сохранения, публикации, удаления допустимого Draft и withdrawal.

Published version не переключается в edit-mode. Продолжение работы создаёт новый Draft, при необходимости на основе выбранной версии.

### Instances

Список экземпляров показывает минимум Name, Key, Schema, число Draft, latest Published version и состояние. Нужны поиск Name/Key, filter по Schema и server-side pagination.

Instance Workspace повторяет знакомый паттерн Schema Workspace: identity/header + versions + editor выбранной version.

При создании Draft пользователь выбирает EntitySchema и конкретную SchemaVersion, включая Draft schema version там, где это допускает согласованный Demo lifecycle.

Object/List отображаются рекурсивно подходящими sections/list editors. Orphan authoring values после изменения Draft schema не скрываются: показываются отдельным блоком «данные вне текущей структуры» и блокируют Publish до явного исправления/удаления.

### Resources

В Demo есть отдельная пользовательская страница «Ресурсы»: reusable MediaLibrary catalog с upload, list/grid, detail, preview/player, metadata, server-side search/pagination и фильтрами `AssetKind`, format, upload date, Used/Unused, EntitySchema и diagnostic status. `AssetReference` редактируется через picker, а не ручным вводом GUID.

Финальная граница `MediaLibrary` bounded context используется уже в Demo. Поддерживаются `Image`, `Audio`, `Video`; допустимые extensions/MIME/max-size policy UI получает динамически от backend. Schema field фиксирует required `AssetKind`, поэтому picker и upload из Instance Workspace сразу ограничены совместимыми assets.

### Code Generation

Demo Code Generation — **только UI-выбор точных SchemaVersion и скачивание ZIP**.

Сценарий:

1. выбрать одну или несколько точных versions, включая несколько versions одной Schema;
2. увидеть итоговый selection;
3. запустить backend generation;
4. скачать ZIP с generated `.g.cs` C# classes.

CLI, manifest-driven workflow, полноценная автоматизированная code generation pipeline, Unity Editor/build tooling и аналогичная developer automation относятся к MVP/последующим tooling-сценариям и не усложняют Demo UI.

### MVP navigation

Верхнеуровневая структура:

- Home;
- Контент;
- Каталог и публикация;
- Модерация;
- Архив;
- Администрирование.

Конкретные вложенные пункты могут уточняться, но boundaries фиксированы.

#### Контент

Schemas, Instances, Resources/MediaLibrary, Code Generation/tooling.

#### Каталог и публикация

Products являются основной точкой входа. Product и его ProductVariant не дублируются отдельными глобальными navigation trees.

Product Workspace:
- Обзор;
- Общий контент;
- Релизы;
- Варианты;
- Политика.

Variant Workspace:
- Обзор;
- Локальный контент;
- Публикация;
- История;
- Политика.

Source catalog fields read-only: изменение Product/ProductVariant выполняется в VR School. По [F06](../features/F06-product-catalog.md)/[Q23 V2](../../backend/questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2) Product Name1–120/Variant Name1–200 и optionalDescription≤500 сохраняются из источника без CM edit. UI раздельно показывает applied state/version, newest accepted candidate/version, current assessment/Issues freshness, scoped IngestionFault/readiness и lifecycle. ACK/acceptance не apply: accepted22 excludes21 before worker, old AppliedState wholly retained/absent. Missing parent durable/noTTL/fakeProduct, Archived parent separate; compatible source correction can full-apply+Resolved System SourceChangedApply without publish/autoresume. Operational reconciliation VRS-only, its report не CMready; UI sync/bootstrap/write кнопки не вводятся, diagnostics exact wire Q15.

Variant local composition визуально различает inherited, local addition, override и excluded. Override `A → B` показывает target и result явно.

Release creation, revision preview, activation, rollback, unpublish и history являются contextual sections workspace, а не глобальными CRUD-разделами.

При async Publishing → Delivery UI различает Publishing activation и фактическое Delivery effective state.

Catalog conflicts имеют глобальный список/очередь и contextual banner в затронутом Product/Variant Workspace.

Licenses не входят в основную specialist navigation. При необходимости MVP предоставляет read-only diagnostic/admin representation.

### Модерация

Название UI-раздела F17 — **«Модерация»**.

Один и тот же раздел доступен специалисту и админу, но действия зависят от роли.

Специалист:
- видит переданное на модерацию;
- видит pending/rejected/approved result и комментарии;
- может забрать ещё не рассмотренную отправку назад, если сам обнаружил ошибку;
- после исправления может отправить состояние на модерацию заново.

Админ:
- видит поступившие на модерацию состояния;
- открывает проверяемое immutable состояние;
- может оставлять комментарии;
- может одобрить;
- может отклонить/вернуть с обязательным содержательным пояснением.

Точная единица moderation по-прежнему определяется Q28. UI-концепция единого раздела не выбирает за Q28, что именно является review target.

### Архив

В MVP архив является **отдельным блоком navigation**, а не filter внутри обычных списков.

Обычные lists/workspaces показывают только active/non-archived authoring objects. В «Архиве» находятся отдельные представления архивированных типов, для которых lifecycle поддерживает archive: schemas, instances, resources и другие согласованные сущности.

Конкретный перечень archive tabs определяется lifecycle каждого типа. Source Product/ProductVariant не получают локальный произвольный archive command вопреки VR School ownership.

### Прочее

MVP Home становится dashboard только для действительно actionable вещей: unresolved conflicts, moderation queue/result, failed/blocked promotions и integration/system problems. Vanity analytics не являются обязательными.

Users/Roles и Activity History — отдельные administrative screens.

Mass actions не вводятся по умолчанию. Search/filter/pagination проектируются под реальную задачу конкретного list screen.

## Решения блока 19: Schema/Instance Workspaces

### Schema Workspace

Desktop target: persistent version rail слева, selected version workspace справа. Draft показываются выше Published; Draft сортируются по времени создания descending, Published — по VersionNumber descending.

Каждый Schema Draft имеет:
- короткое **Название** для различения параллельной работы;
- отдельный **Комментарий к изменениям** — свободный authoring text о том, что меняется и зачем.

Комментарий предназначен для reviewer/context/history и может содержать внешнюю ссылку/номер задачи как обычный текст. Специальная Jira-интеграция из этого решения не следует.

Создание Draft:
- с чистого листа;
- на основе выбранной Draft/Published версии.

Workspace использует explicit Save без autosave. При уходе с dirty state показывается guard Save / Discard / Stay. Publish не выполняет скрытый save: сначала Draft должен быть сохранён.

После publish открывается новая read-only Published version. На Published view основное продолжение — создание нового Draft на основе этой версии.

Schema editor: tree + inspector. Top-level field добавляется отдельным действием; child доступен у container node. Основное редактирование field происходит в inspector, не в modal.

Published view показывает обычное read-only представление свойств, а не disabled form-controls.

Deprecated fields:
- остаются видимыми;
- получают badge/de-emphasis;
- в authoring tree/list **всегда сортируются после non-deprecated siblings**;
- внутри deprecated-группы используется обычный детерминированный UI-порядок.

Это только presentation rule. Canonical serialization/hash/Registry/codegen ordering не меняется из-за UI grouping.

Publish blockers показываются summary + field-level errors с переходом к проблемному field.

Delete SchemaDraft с зависимыми instance Draft показывает exact set/count и требует явного confirmation после 5-second disabled interval. Если dependency set изменился, server reject возвращает новый impact; UI сбрасывает consent и повторяет 5 секунд для нового SchemaCascade. Withdrawal остаётся contextual action с видимым badge; read/Copy/Save разрешены, Publish/runtime blocked. Explicit ReturnToDelivery проверяет Published state, Publish withdrawal не снимает.

### Instance Workspace

Instance Workspace использует тот же mental model: identity/header + persistent versions rail + selected version editor/read-only view.

Version item обязательно показывает SchemaVersion context, потому что версии одного EntityInstance могут принадлежать разным SchemaVersion.

Создание EntityInstance в Demo:
- Name;
- **явно вводимый Key**;
- Schema;
- SchemaVersion;
- dynamic editor.

Автопредложение Key из Name переносится в **MVP**. Даже там это только видимое предложение, которое пользователь подтверждает; silent normalization/перезапись contract key запрещены. После создания EntityInstanceKey immutable.

Новый Draft instance можно начать clean либо на основе выбранного Draft/Published instance version. При clean выбирается SchemaVersion. При copying исходная SchemaVersion используется default, но можно выбрать другую совместимую Draft/Published SchemaVersion; совпадающие values переносятся по stable SchemaFieldId, unmatched сохраняются orphan.

Orphan values не мигрируются эвристически. UI показывает warning и отдельную секцию после основной формы; для Demo доступны просмотр old field identity/key/value и явное удаление orphan.

Dynamic form:
- Object → collapsible sections/cards;
- List → recursive editor;
- List<Object> → collapsible item cards;
- List<List> → тот же recursive collection editor;
- Enum → single select по key/display name;
- FlagEnum → multi-select/chips/checkboxes по keys;
- Color → picker + точное #RRGGBBAA;
- AssetReference → required `AssetKind` из schema definition + resource picker с compatible search/preview/upload/select; allowed extensions/limits приходят из backend media policy.

OrderMode.Default сохраняет authored list order и UI позволяет менять порядок list items. Это value-order и не связано с отсутствием business order у SchemaField.

Save/Publish и dirty-state/concurrency behavior совпадают с Schema Workspace. Concurrent server change не запускает auto-merge nested editor state.

EntityInstance withdrawal находится на logical header; explicit ReturnToDelivery требует Published state. Withdrawn read/Copy/Save разрешены, Publish/runtime blocked до возврата. Последний Draft delete оставляет parent с empty history. Different-target Publish коротко сериализуется на сервере: при valid bases оба success с distinct global numbers; same-target stale правило сохраняется.

### Schema ↔ Instances

Schema Workspace содержит contextual section/list экземпляров этой схемы и action создания instance, но глобальные пункты «Схемы» и «Экземпляры» сохраняются.

Instance Workspace содержит navigation обратно к Schema.

### Archive MVP

Обычные active lists не показывают archived objects. Прямая ссылка на archived workspace остаётся валидной и открывает read-only archived view с banner/link в «Архив».

Редактирование archived object невозможно до Restore, если Restore разрешён lifecycle этого типа.

Archive navigation содержит отдельные представления по типам, а не одну общую таблицу.

## Решения блока 20: Product / Variant Publishing Workspace

### Общий принцип

MVP Publishing UI строится вокруг **Product Workspace** и **Variant Workspace**. Assignment, Release, PublicationRevision и Activation не получают отдельные глобальные CRUD-разделы.

UI обязан сохранять смысловую цепочку:

```text
Assignment         = что сейчас хотим
Release            = что зафиксировали
PublicationRevision = какой итоговый состав рассчитан
Activation         = что выбрано Publishing
Delivery effective = что реально доступно клиенту
```

Эти состояния не объединяются одной неопределённой кнопкой Publish.

### Product Workspace

Source Product fields read-only и принадлежат VR School.

Вкладки:

- Обзор;
- Общий контент;
- Релизы;
- Варианты;
- Политика.

Overview показывает source state, current Product base/release, Variants, conflicts, effective policy и последние publishing events без дублирования полных дочерних экранов.

«Общий контент» редактирует live ProductContentAssignment. Items группируются по EntitySchema; показываются Instance Name/Key, точная EntityInstanceVersion, SchemaVersion и metadata. Добавление выбирает логический EntityInstance и затем exact Published EntityInstanceVersion. Latest не подставляется молча.

Assignment имеет explicit Save/dirty state. Save не является release или клиентской публикацией.

ProductRelease создаётся во вкладке «Релизы» из текущего assignment. Перед созданием показывается summary/diff Added / Changed version / Removed относительно прошлого release. Field-level JSON diff не является основным UX.

Release immutable и имеет sequence/identity + ChangeComment без отдельного Title.

Вкладка «Варианты» показывает source status, local patch presence, Publishing active revision, Delivery effective revision, conflict/promotion result и ведёт в отдельный Variant Workspace.

### Variant Workspace

Вкладки:

- Обзор;
- Локальный контент;
- Публикация;
- История;
- Политика.

Overview показывает parent Product, current base, local patch summary, Publishing/Delivery revisions, blockers/conflicts и effective policy.

«Локальный контент» показывает единый composition preview с provenance: Inherited / Local / Override / Excluded.

Inherited item read-only. Действия: override или exclusion.

Override отображается явно как A → B и ограничивается той же EntitySchema. Cross-schema изменение оформляется exclusion + local addition.

Schema/instance exclusions остаются видимыми правилами. Schema exclusion удаляет только inherited layer; local items той же Schema остаются видимыми.

Если override target временно исчез из Product base, rule и local result сохраняются; UI показывает отсутствующий target. При возврате target override снова отображается нормально.

Composition collisions являются blockers; silent dedup отсутствует.

Изменение Product base пересчитывает live preview, но не переписывает local patch. UI показывает информационное сообщение о пересчёте.

### VariantRelease и Revision

VariantRelease создаётся как immutable snapshot local patch и явно подписан как «Локальные отличия, не итоговый состав».

Перед review/Create VariantRelease пользователь явно выбирает exact immutable ProductRelease либо EmptyBase для variant-only; нет implicit live assignment/current active/empty fallback. Review показывает выбранный composition context; changed patch/context invalidates review, Create повторно проверяет тот же saved source/context/current eligibility. Context сохраняется как provenance без hard dependency, F10 другой base полностью revalidates. Нормативные UI state/controls находятся в [F09](../features/F09-content-releases.md), wire/basis carrier — Q15.

F07/F08 отделяют saved working state и release-ready diagnostics: unchanged exact unavailable reference и live-base-only collision допускают Save с ReleaseBlocker. Intrinsic/new-use/access/concurrency ошибки — SaveBlockers; permitted retained absent target/deprecated — Warnings. Inactive maintenance по semantic removal/eligible replacement, Open F15 обычные writes блокирует. Own ETag отдельно derived basis; read failure не empty valid preview. Полное поведение [F07](../features/F07-product-content-assignment.md)/[F08](../features/F08-variant-content-assignment.md)/Q07.

Publication section визуально показывает последовательность:

```text
Local Patch Release
        ↓
Revision Preview
        ↓
Activation / Delivery
```

При создании revision ProductRelease и VariantRelease выбираются явно. Допустимы Product-only, Variant-only и both. Neither запрещён; пустой VariantRelease ради nullable-ссылки не создаётся.

Перед созданием PublicationRevision backend возвращает materialized preview. Итог группируется по Schema и показывает provenance. Excluded items вынесены в collapsible summary.

Invalid composition блокирует создание revision.

PublicationRevision immutable, имеет ChangeComment без отдельного Title и после создания считается «готовой к активации», но ещё не активной.

### Activation / Delivery

Variant Workspace одновременно показывает Publishing active revision и Delivery effective revision.

При async propagation новая Publishing revision не помечается current effective до full Delivery commit. UI раздельно показывает committed effective state и pending/confirmed temporal history; промежуточные причины видны даже при совпадении Publishing/Delivery RevisionIds. Effective commit сам по себе не timestamp Freeze: saved VisibilityConfirmedAt и completeness приходят из authoritative read model.

Rollback может выбрать любую допустимую historical revision Variant. Он создаёт новую history запись и не переписывает старые records.

Unpublish — отдельное action с nullable effective state; confirmation не обещает полного запрета historical/frozen access.

Withdrawal конкретной revision находится в details/history как advanced destructive action и объясняет запрет дальнейшей серверной выдачи, включая Freeze, без automatic replacement.

Activation history сортируется по server Sequence.

### Automatic promotion

Manual/Automatic выбирается для конкретного ProductRelease/promotion, не как постоянный Product switch.

Automatic result отображается в Product Workspace per Variant: Success / Blocked / Failed. Partial success разрешён и видим. Domain conflict требует исправления, после чего возможен server-supported retry.

### Policy

Product Policy показывает configured Product policy, system default и effective result.

Variant Policy показывает inherited Product policy, optional Variant override и effective result. Без override явно показано наследование, скрытая копия не создаётся.

При изменении policy UI объясняет отсутствие обратной силы для уже существующего entitlement period.

Во вкладке Policy показывается компактная предметная version history; общий F18 audit её не заменяет.

### Conflicts и moderation

F15 conflict показывается persistent banner в затронутом workspace. Заблокированные действия disabled с explanation и переходом к resolution, а не скрываются.

По [Q27](../../../business/questions/Q27-catalog-archiving.md) показываются раздельные applied/newest source `Active/Archived` и processing/pending/conflict. Hidden — эффект исключения из нового selection, отдельного toggle Archive/Restore в CM нет. Parent Archive ограничивает новые операции Variant, но не перерисовывает его own lifecycle. Exact workspace сохраняет чтение/limited maintenance и safety actions с обычными permissions; обычные new releases/revisions/activation/rollback/promotion до Restore недоступны. Историческая Delivery и подтверждённое entitlement от Archive сами не отменяются.

F15 corrective repair — отдельный разрешённый специалисту сценарий working Variant patch по [Q25](../../../business/questions/Q25-catalog-conflicts.md#f15-технические-границы-разрешения), не исключение для ordinary Save. Preview разделяет current applied/prospective newest parent/proposed repaired composition. Пользователь сначала сохраняет repair, затем отдельно подтверждает Resolve; обычный workspace остаётся applied до успешного Resolve. При source/patch change старый basis отвергается с refresh/review, committed repair не откатывается автоматически. Missing newest parent показывается как waiting, не успешный move. Resolve не auto-publishes, существующая active revision/history сохраняется. Exact repair API/basis carrier/recovery выбираются Q15; detailed screens/FSD/flows описаны F15.

Q28 всё ещё определяет exact moderation target, поэтому publishing lifecycle не связывается с F17 искусственно. Workspace должен быть готов показать moderation state/action у конкретного immutable state после закрытия Q28.

Schema Draft Title/ChangeComment используются reviewer context, если Q28 выберет SchemaVersion. Аналогичная metadata других объектов не добавляется автоматически без отдельного решения.

## Решения блока 21: routes, navigation и frontend decomposition

### Route identity

Primary route identity строится по immutable ID, а не по Name/Key.

Schema:
- `/schemas`;
- `/schemas/new`;
- `/schemas/{schemaId}`;
- `/schemas/{schemaId}/versions/{schemaVersionId}`;
- `/schemas/{schemaId}/instances`.

Instance:
- `/instances`;
- `/instances/new`;
- `/instances/{instanceId}`;
- `/instances/{instanceId}/versions/{instanceVersionId}`.

Assets:
- `/assets`;
- `/assets/{assetId}`.

Demo code generation:
- `/code-generation`.

Если version ID не указан, Schema/Instance Workspace выбирает newest Draft; если Draft отсутствует — latest Published.

Workspace tab/state, имеющий самостоятельную deep-link ценность, хранится route segment, а не числовым `?tab=...`.

Product:
- `/products`;
- `/products/{productId}`;
- `/products/{productId}/content`;
- `/products/{productId}/releases`;
- `/products/{productId}/variants`;
- `/products/{productId}/policy`.

Variant canonical route включает Product context:
- `/products/{productId}/variants/{variantId}`;
- `.../content`;
- `.../publishing`;
- `.../history`;
- `.../policy`.

Если Variant был перенесён в другой Product, old route с прежним ProductId разрешается по immutable VariantId и redirect-ит на новый canonical Product/Variant URL.

MVP:
- `/conflicts`, `/conflicts/{conflictId}`;
- `/moderation`, `/moderation/{moderationId}`;
- `/archive/schemas`, `/archive/instances`, `/archive/assets`;
- `/admin/users`, `/admin/activity`.

### Navigation

MVP sidebar:

```text
Home








Контент
  Схемы
  Экземпляры
  Ресурсы
  Генерация моделей








Каталог и публикация
  Продукты
  Конфликты








Модерация








Архив
  Схемы
  Экземпляры
  Ресурсы








Администрирование
  Пользователи
  История действий
```

ProductVariant не получает отдельный sidebar item. Licenses не входят в основной specialist workflow; диагностический read view при необходимости относится к Administration.

Home Demo сохраняется минимальной. Home MVP показывает actionable items: moderation, conflicts, blocked/failed promotions и integration/system problems, а не декоративную статистику.

### Lists, query state и breadcrumbs

Search/filter/page/sort list-screen хранятся в query string, чтобы browser navigation и copy-link сохраняли контекст.

Pagination server-side. Search выполняется через backend query с debounce; полный каталог не загружается в browser для локального фильтра.

Breadcrumb обязателен для workspace. Label использует Name, route identity использует immutable ID.

Deep links сохраняются для moderation exact state, activity object/state, release/revision history, archived workspace и withdrawn historical state.

### Page states и notifications

Workspace различает loading, loaded, not-found, forbidden, stale/conflict и transient API failure.

403 и 404 не смешиваются. Failed refresh не обязан стирать уже безопасно отображаемые данные.

Pending action блокирует только соответствующую feature/action, а не всю SPA без необходимости.

Короткий confirmed success показывается snackbar/toast; validation — inline; persistent blocker/conflict — banner; modal применяется для реального confirmation/dangerous action. Success snackbar не заменяет обновление workspace state.

### Client state

Глобальный Redux-подобный store для всего CM не вводится.

Page/workspace владеет route context и загруженной read model. Feature владеет состоянием конкретного действия. Global state ограничивается действительно глобальными сведениями вроде authenticated employee/permissions/navigation shell/notifications.

После mutation workspace принимает согласованный response DTO и ETag exact owner, когда operation их возвращает. Зависимые/вычисляемые projections, rail/list/header context перечитываются по contract вместо ручного патчинга множества локальных копий. Это не требование скрытого exact GET для получения уже возвращённого результата или ETag; успешный operation result сохраняется при ошибке последующего refresh.

### FSD

Target semantics:

- Pages — route/composition orchestration;
- Widgets — крупные workspace sections;
- Features — законченные user actions;
- Entities — предметное UI-представление/read behavior;
- Shared — generic UI/API infrastructure без business orchestration.

Feature не создаётся для каждого GET. Shared не становится местом бизнесовых picker/selector только потому, что они переиспользуются.

### NSwag models

Серверная editable boundary — explicit recursive union; UI использует NSwag generated models без ручной business DTO-копии. Save full snapshot: optional absence очищает значение, collapsed/unrendered values и incompatible/orphans сохраняются до explicit discard. Missing required разрешён при Save/Accept, новый type-invalid input rejected; transient numeric text локален. ItemKey opaque client key каждого List<Object> occurrence, Copy/reorder сохраняют его. Derived incompatibility token bound к separate diagnostic basis, path использует FieldId/ItemKey; own ETag защищает saved owner. Create operation ID durable replay: same payload прежний result, mismatch conflict. Text restricted HTML p/br/lists/bold/italic, server sanitization с returned result; semantic-empty invalid, exact configurable max позже. DateTime wire seven digits Unspecified без Z/offset. Exact methods/numeric limits остаются backend OpenAPI.

**NSwag-generated models используются во всём web UI как основные client models**, включая сложные editable формы.

Не создаются дублирующие `ViewModel`, `EditorModel`, `FormModel` или ручные копии DTO только ради UI-слоя.

Generated classes не редактируются вручную. UI и features работают с ними напрямую.

Чисто UI-state хранится отдельно только там, где это не часть API-model: dirty/loading flags, selected tree node, раскрытие секций, pending action, локальное отображение ошибок и аналогичные presentation concerns.

Если UI требует данных/формы, которых невозможно корректно выразить существующим NSwag contract, сначала исправляется/расширяется server OpenAPI/DTO contract, а не создаётся постоянная параллельная клиентская модель для обхода контракта.

Create/edit contracts на сервере должны быть пригодны для такого использования. Например, create field DTO не содержит ID нового field, а edit DTO при появлении edit API должен содержать обязательную identity согласно уже принятой модели.

### Role / responsive / localization

Sidebar скрывает полностью недоступные role areas, но временно недоступные действия внутри разрешённого workspace предпочтительно остаются видимыми disabled с explanation. Backend authorization остаётся обязательной.

UI desktop-first. Basic narrow/tablet layout не должен ломаться, но полноценное mobile authoring сложных Schema/Instance editors не входит в MVP acceptance.

Основной язык интерфейса — русский. Demo может быть только русским. Тексты не должны хаотично размазываться по компонентам; localization resources используются как техническая основа, даже если language switch пока отсутствует.

## Решения блока 22: moderation UI closure

Q28 закрыт: обязательный moderation target MVP — exact immutable `ProductVariantPublicationRevision`.

Variant Publishing Workspace показывает moderation status/action у конкретной revision между `Revision готова` и Activation. Manual flow требует Approval перед доступностью Activate. Automatic flow показывает `Ожидает модерации`; Approval той же exact revision автоматически продолжает activation без второго клика специалиста.

Global `/moderation` остаётся рабочей очередью обеих ролей. `/moderation/{moderationId}` показывает exact submitted PublicationRevision, materialized composition, provenance, ChangeComment, comments/history и semantic diff относительно текущей Delivery effective revision.

Rejected revision не редактируется в moderation screen. Исправление выполняется в Product/Variant workspace и создаёт новую revision/new iteration. Withdraw pending сохраняет историю; ту же withdrawn revision можно отправить новой iteration повторно.

Rollback UI предлагает только ранее Approved revisions, которые остаются допустимыми. Unpublish, Withdrawal и Policy changes не создают обычные moderation items.

Self-approval UI не запрещается автоматически при наличии обеих permissions; SubmittedBy/DecidedBy видимы в history.

Moderation является quality gate клиентской публикации, а не универсальным approval UI для Schema/Instance/Release/Policy.

## Решения блока 24: Archive / Retire UI

Отдельный navigation block «Архив» содержит только logical archived objects: Schemas, Instances, Assets.

Retired versions/releases/revisions не переносятся в общий Archive. Они остаются в workspace своего owner с явным badge `Retired` и read-only/history context.

Archived logical workspace доступен по deep link в read-only режиме. Restore является contextual action, если lifecycle разрешает restore.

Если child недоступен только из-за archived parent, UI показывает derived state «Недоступно: родитель в архиве», не притворяясь, что child сам Archived.

Delete/Archive/Restore/Retire/Withdrawal actions используют HTTP `ETag` exact mutable resource как transport-state и отправляют его через `If-Match`; `Version`/`ExpectedVersion` в NSwag DTO отсутствуют. После успеха workspace принимает новый ETag из response либо перечитывает server read model по согласованному contract.

Withdrawal current active revision предупреждает, что active/effective state станет `null` без автоматического fallback.
