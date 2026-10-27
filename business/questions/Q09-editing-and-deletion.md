# Q09. Редактирование и удаление

## Вопрос и известные границы

Что можно удалять: черновики, логические схемы/экземпляры, медиа? Нужен архив вместо удаления? Как редактируется существующий черновик?

Уже определено: Опубликованные данные и зависимости исторической выдачи нельзя незаметно переписать или разрушить.

**Ответ:** Demo minimum Save, локальная clean форма, withdrawal recovery и подтверждение изменившегося delete-impact определены ниже; lifecycle Delete/Archive/Retire/Withdrawal раскрыт в блоке 24. Полная retention/GC policy остаётся последующей технической детализацией.

## Затронутые фичи

[F01](../features/F01-create-entity-schema.md) · [F02](../features/F02-create-schema-version.md) · [F04](../features/F04-entity-instances.md) · [F05](../features/F05-media-assets.md)

## Как применить ответ

Уточнить связанные спецификации в их собственных разделах. Не считать заглушку принятым решением.

## Блок 7. Сохранение черновика и подготовка данных

Это продолжение блока «Схемы: ключи, черновики и нумерация». Вопросы 1–6 и их ответы хранятся в [Q08](Q08-keys-and-drafts.md); ниже принятые ответы 7–8 того же блока. Правила полного удаления, архивирования и снятия с выдачи будут обсуждаться отдельно.

### 7. Насколько незавершённую схему разрешаем сохранять в Draft?

Можно ли сохранить схему с названием, но ещё без полей, или поле Enum без законченного списка вариантов, чтобы вернуться к работе позже? Либо каждое серверное сохранение должно принимать только полностью корректную структуру, а промежуточный ввод остаётся в форме до исправления?

Нужно различить проверки сохранения и публикации. Публикация по-прежнему требует корректного определения; поддержка незавершённого Draft не означает, что API должен принимать неизвестные типы, чужие идентификаторы или повреждённые данные. Точные проверки отдельных типов закреплены Q34.

**Ответ:** Draft схемы разрешено сохранять на сервере, если заданы как минимум название схемы, ключ и хотя бы одно поле. Блок 9 уточнил type-level minimum: каждое уже добавленное field definition должно быть структурно валидно (Object с child, List с recursive item-definition, Enum/Flag минимум с одной option, Color с единым RGBA contract). Publish остаётся строже за счёт cross-version/monotonic checks. Неполнота данных экземпляра не означает разрешение half-configured schema field.

### 8. Можно ли готовить экземпляры по ещё не опубликованной версии схемы?

Специалист добавляет фотографию в новый черновик структуры «Карточка экспоната». Может ли он уже начать заполнять черновики данных по этой структуре или сначала обязан опубликовать версию схемы?

Речь только о подготовке: черновая структура и черновые данные не выдаются клиенту. При разрешённой подготовке потребуется затем определить, что происходит с этими черновиками данных при изменении или удалении исходного Draft схемы; автоматическую миграцию и каскадное удаление заранее не утверждаем.

**Ответ:** Да. Экземпляры можно готовить по ещё не опубликованной Draft-версии схемы, но такие версии экземпляров сами обязаны оставаться Draft и не участвуют в Delivery. Блок 10 уточнил последствия: изменение Draft-схемы сохраняет dependent Draft data/orphan values и требует повторной валидации; явное удаление Draft-схемы может каскадно удалить зависимые Draft instance versions только через отдельное подтверждённое destructive действие.

## Применение принятых ответов блока 7

Разнести ответ 7 в F01/F02 и связанные серверные проверки и состояния редактора; ответ 8 в F02/F03/F04 и сценарии выбора версии схемы. Не переносить факт ограничения текущего домена в бизнес-правило без ответа. Публикационная неизменяемость, отсутствие Draft в Delivery и исключение административного согласования из Demo остаются принятыми.


## Уточнение блока 10: destructive delete, cascade и Archive

Для Demo не вводятся soft-delete/Archive/Restore. Разрешённое delete-действие физически удаляет данные из БД; это сознательное упрощение Demo и не переносится в MVP как политика хранения истории.

Удаление Draft EntitySchemaVersion с зависимыми Draft EntityInstanceVersion разрешается как **явный cascade delete**. UI до вызова показывает количество затрагиваемых Draft instance data, блокирует подтверждение на 5 секунд и явно сообщает необратимость. Backend не полагается только на UI: перед commit повторно определяет exact допустимый dependency set и сравнивает с подтверждённым набором. Изменение состава, даже при прежнем count, отклоняет delete; UI показывает новый impact и повторяет 5-second confirmation. При совпадении выполняется согласованное/атомарное удаление; Published dependency не должна попадать в этот cascade.

Для MVP Archive schema/instance означает как минимум скрытие из обычного authoring UI и снятие с новой клиентской выдачи при сохранении исторических publication references. Restore/Unarchive, физическая очистка и полная dependency matrix остаются последующей детализацией.

Снятие с выдачи **не равно delete**: в Demo отдельно существуют delivery-withdraw scopes на schema, schema version и logical instance; их эффект на sync определён в Q36/F13/Q30.

## Блок 24. Delete / Archive / Retire / Withdrawal

### Общая семантика

Четыре операции окончательно разделены:

- **Delete** — физически удалить данные, которые ещё не стали исторической частью опубликованного контента.
- **Archive** — убрать логический authoring object из обычной работы, сохранив данные и историю.
- **Retire** — запретить immutable Published artifact использовать в новых публикациях, сохранив historical use.
- **Withdrawal** — запретить дальнейшую серверную выдачу уже опубликованного результата.

Archive/Retire сами по себе не прекращают historical Delivery. Для прекращения распространения используется Withdrawal.

Restore после Archive/Retire не создаёт Release/Revision и ничего не активирует автоматически.

### EntitySchema

В Demo hard-delete logical EntitySchema допустим только если у неё никогда не было Published SchemaVersion.

Logical schema Delete дополнительно блокируется наличием любого logical EntityInstance owner, даже пустого. Пользователь сначала выполняет отдельное явное Delete logical instance по F04 с его eligibility/consent. Автоматический cascade cleanup logical owners не выполняется. После этого допустимый schema Delete удаляет её Draft SchemaVersions; UI показывает актуальный impact и подтверждает exact set, backend повторно проверяет весь набор перед commit. Изменение состава/состояния, даже при прежнем count, отклоняет delete и требует нового подтверждения.

Если у Schema когда-либо была Published SchemaVersion, physical delete запрещён.

В MVP используется Archive:
- Schema исчезает из обычного authoring UI;
- нельзя создавать/редактировать новые Draft и новые EntityInstance;
- нельзя использовать её для новой публикационной работы;
- все Draft/Published/history данные сохраняются;
- existing PublicationRevision/Delivery history не ломаются.

Archive Schema не меняет lifecycle каждого дочернего EntityInstance. Они становятся effectively unavailable из-за parent. После Restore Schema отдельно archived Instance остаются archived.

### EntitySchemaVersion

Draft SchemaVersion можно hard-delete, включая последний Draft. Logical EntitySchema сохраняет identity/Key/Name и может иметь пустую историю; parent не удаляется автоматически. Для dependent Draft EntityInstanceVersion сохраняется явный cascade delete с exact count/composition, 5-second confirmation и server-side повторной проверкой dependency set. Удаляются только dependent Draft versions; logical EntityInstance owners сохраняются, включая ставших пустыми. Published dependencies никогда не входят в cascade.

Published SchemaVersion физически не удаляется.

В MVP Published SchemaVersion поддерживает **Retire**:
- остаётся доступной по exact historical ID/version;
- Registry/codegen historical lookup остаётся воспроизводим;
- её нельзя выбрать для нового EntityInstance Draft;
- новые EntityInstanceVersion нельзя публиковать на неё;
- existing releases/revisions остаются допустимыми.

Существующий Draft instance на Retired SchemaVersion не удаляется, но его Publish блокируется.

Retire SchemaVersion обратим, пока нет отдельного Withdrawal/запрета. Restore/EnableForNewUse меняет lifecycle permission, но не Published content.

### EntityInstance

Hard-delete logical EntityInstance допустим только если у него нет Published EntityInstanceVersion и исторических publication dependencies. Тогда удаляются Draft versions вместе с logical instance.

Если Published history существует, physical delete запрещён.

MVP Archive EntityInstance:
- скрывает его из обычного списка;
- запрещает новые Draft/Publish;
- исключает из picker новых assignments;
- сохраняет versions/releases/revisions;
- не withdrawal-ит historical Delivery.

Restore возвращает object в authoring/publishing lifecycle без автоматической публикации.

### EntityInstanceVersion

Draft EntityInstanceVersion можно hard-delete с `ExpectedVersion`, включая последний Draft. Logical instance сохраняет identity/Key/Name и пустой список versions; parent не удаляется автоматически.

Published EntityInstanceVersion физически не удаляется.

В MVP Published EntityInstanceVersion поддерживает **Retire**:
- нельзя выбрать её в новый Product/Variant assignment;
- mutable assignment, уже ссылающийся на retired version, остаётся читаемым, но новый Release из него блокируется до замены/удаления dependency;
- existing releases/revisions остаются валидны.

Retire обратим и не является Withdrawal.

### Asset / MediaLibrary

В Demo Asset hard-delete допустим только при отсутствии references. Любой Draft reference блокирует delete с dependency count; cascade удаления ссылок не выполняется.

Asset, используемый Published/history content, физически не удаляется.

В MVP Asset поддерживает Archive:
- скрыт из обычной Resource Library/picker;
- нельзя создавать новые references;
- historical Published/Revision delivery продолжает получать bytes;
- object storage bytes физически сохраняются.

Restore снова разрешает новые references. Автоматический object-storage GC в MVP не вводится.

### Product / ProductVariant

CM не имеет локальных Delete/Archive/Restore commands для Product/ProductVariant. Lifecycle принадлежит VR School.

Source Archived/Inactive/Deleted-like state сохраняется в projection; identity физически не удаляется. Source inactive state может блокировать новую CM authoring/publishing работу, но historical revisions не уничтожаются. Возврат Active приходит новым source snapshot.

### Assignments

ProductContentAssignment и ProductVariantContentAssignment являются mutable working state, а не архивируемой историей. Items/rules можно удалять обычными mutation с `ExpectedVersion`. Историческую фиксацию дают Releases, отдельный event-store assignment history не вводится.

По [Q07](Q07-exclusions-and-local-content.md#f07-допустимость-рабочих-references) unchanged exact unavailable reference может остаться при Save других изменений с ReleaseBlocker; новая позиция/замена требует current eligibility. Inactive/archived owner допускает limited removal/eligible replacement без добавления/расширения; Open CatalogConflict остаётся обычным mutation blocker. Release требует строгой повторной new-use/context validation, не объявляется готовым только от успешного Save.

### ProductRelease / VariantRelease

Release immutable и физически не удаляется после создания.

Release поддерживает **Retire**:
- нельзя использовать его для создания новой PublicationRevision;
- existing PublicationRevision сохраняется;
- historical provenance читается.

Current active Product base нельзя Retire, пока сначала не выбран другой active base либо Product не переведён в состояние без current base.

VariantRelease отдельного active pointer не имеет и может быть Retired, если этим не ломается уже выполняющаяся atomic operation.

### PublicationRevision

PublicationRevision никогда не Delete.

Поддерживаются разные состояния:
- **Retired** — нельзя снова Activate/Rollback, но historical/frozen delivery ещё допустим;
- **Withdrawn** — дальнейшая серверная выдача запрещена, включая Freeze; Activate/Rollback запрещены.

Active Publishing revision нельзя Retire до переключения на другую revision либо Unpublish.

Withdrawal current active revision автоматически создаёт согласованный transition active state → `null` без fallback на предыдущую revision. Delivery после propagation также получает effective `null`.

### Moderation, Policy, conflicts

Moderation iteration физически не удаляется. Pending → Withdrawn сохраняет историю; Approved/Rejected decision immutable.

Committed ContentEntitlementPolicyVersion не удаляется; новая policy version заменяет current выбор, старая остаётся для historical entitlement.

Resolved catalog conflict не удаляется: он исчезает из unresolved queue и остаётся доступным как history/audit.

### Archive UI

Общий раздел «Архив» содержит logical objects: Schemas, Instances, Assets.

Retired SchemaVersion/EntityInstanceVersion/Release/PublicationRevision остаются внутри workspace своего owner с badge `Retired`; они не переносятся в общий Archive.

Прямая ссылка на archived logical object открывает read-only workspace. Restore — contextual action этого workspace.

Все mutable Delete/Archive/Restore/Retire/Withdrawal operations используют общий optimistic concurrency contract `ExpectedVersion`.

### Parent archive и effective state

Различаются собственный `Archived` и derived state `Unavailable because parent archived`. Parent Archive не массово переписывает child lifecycle.

### Physical retention

Любой object/record/byte, необходимый для воспроизведения Published Release, PublicationRevision либо frozen entitlement, физически не удаляется.

Physical cleanup допустим только при доказанном отсутствии всех historical references и отдельной retention/GC policy.

## Уточнения F02: создание и удаление Draft

При детализации [backend F02](../../technical/backend/features/F02-create-schema-version.md) и [frontend F02](../../technical/frontend/features/F02-create-schema-version.md) были выявлены три продуктовых профиля; принятые Demo ответы приведены ниже. Принятые multiple Draft, immutable Published, stable field identity и Draft-only atomic cascade не пересматриваются.

| Вопрос | Основание | Варианты / требуемое решение |
| --- | --- | --- |
| Когда clean Draft становится сохранённым resource? | Минимум сохранённого Draft требует валидную структуру Q34 | **Ответ:** clean форма остаётся локальной до первого валидного Save, который создаёт серверный Draft. Copy валидной существующей версии создаёт Draft сразу. Persisted empty Draft не вводится. |
| Можно ли копировать Withdrawn Published source? | Historical read сохраняется; Copy не равен Publish/Delivery | **Ответ Demo:** historical read, Copy и Save recovery work разрешены для снятого source/target с предупреждением. Publish к снятому target и runtime delivery запрещены. Retired относится к MVP и сохраняет отдельные new-use ограничения блока 24; Demo ответ не задаёт новую Retired policy. |
| Что делать, если зависимости изменились после delete-impact preview? | Root ETag и совпавший count не доказывают неизменность состава | **Ответ:** отклонить delete и потребовать подтверждение нового exact dependency set. Изменённая dependency тоже требует нового подтверждения. Для Draft schema cascade повторяется прежняя 5-second задержка; Published history не входит в cascade. |
| Можно ли удалить Enum/FlagEnum option при Save? | Option Key задаёт identity; Published значения/history должны оставаться воспроизводимыми | **Ответ:** key, который никогда не был в Published history этой схемы, разрешено удалить при valid непустом option set. Любой ранее Published key сохраняется, вместо удаления допускается Deprecated=true. Key immutable, Name mutable, Deprecated monotonic; правило действует уже на Save и внутри recursive List.Item. |

Wire consent/freshness и transaction guard относятся к [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f02-детализация-целевого-контракта)/[Q20](../../technical/backend/questions/Q20-persistence-and-migration.md). Server не держит transaction во время UI countdown. Для Demo техническое согласие материализовано как opaque server ConsentToken + explicit Cascade в DELETE body; impact показывает полный exact состав. Backend перечитывает current facts под own schema transaction gate и отклоняет изменившийся состав/состояние, даже при прежних counts. Transport/SQL детали принадлежат Q15/F02, бизнесовые правила выше ими не подменяются.

## F10: зависимости исключённого слоя

Связано с [business F10](../features/F10-publication-revisions.md), [backend F10](../../technical/backend/features/F10-publication-revisions.md), [frontend F10](../../technical/frontend/features/F10-publication-revisions.md), [Q07](Q07-exclusions-and-local-content.md), [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f10-revision-contracts-и-preview), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f10-composition-capture-и-withdrawal).

Подтверждено: Retired source release не выбран для новой revision; final effective exact content/schema/media dependencies должны быть допустимы для нового использования. Available release не отменяет эту проверку. Existing materialized history не recompute; Retire dependency не стирает historical references, Withdrawal отдельно запрещает распространение. Target отсутствующий только в selected base допустим по Q07; trusted captured schema сохраняется.

**Ответ:** F10 current new-use проверяет final effective instance/schema versions и действительно используемую ими media closure. Недоступный A, полностью подавленный и отсутствующий в результате B, сам по себе не блокирует новую revision. Shared schema/media, реально входящие в B closure, продолжают проверяться. Выбранные source releases должны быть Available и правильного owner; captured override/exclusion evidence проверяется на intrinsic validity/достоверность, а не как новое использование подавленного target.

Та же effective closure определяет применимые текущие ограничения saved revision в F17 Approve, F11 Activate/Rollback и F13 distribution. Эти проверки не recompute historical composition и не превращают Retire source release в blanket ban уже materialized revision. Actual restriction semantics принадлежат owning lifecycle/media/runtime contract; новый Asset Withdrawal state не вводится.

Retention closure шире: immutable source/evidence/history references сохраняются даже при exclusion, недоступность runtime позиции не разрешает physical delete исторических dependencies. Preview/Create и дальнейшие проверки используют одну сохранённую effective closure. Exact batch/DTO/profile/mappings остаются Q15/Q20/Q19; бизнесовая граница закрыта.
