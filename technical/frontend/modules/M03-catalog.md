# M03. Каталог продуктов: фронтенд

## Назначение

MVP read-only каталог и eligible recipient selection: applied state, newest accepted version/candidate, current assessment/Issues freshness, fault/readiness и source lifecycle показываются раздельно. Source V2 Name Product1–120/Variant1–200, optionalDescription≤500 и Active/Archived принадлежат VR School, сохраняются без CM edit. Hidden — presentation, Pending/Conflict/IngestionFault — отдельные processing/technical состояния, не lifecycle.

## Карта фич

| ID | Роль |
| --- | --- |
| [F06](../features/F06-product-catalog.md) | Applied продукты/варианты и stable связь, source lifecycle/processing/reasons, eligible selectors и exact/history context |
| [F15](../features/F15-catalog-conflicts.md) | Persistent warning, applied/newest/impact, corrective Variant patch preview/save и отдельный Resolve с history |

## Компоновка и переходы

Обзор передаёт immutable ID в exact workspace или сценарий назначения Publishing. Обычный selector показывает только eligible candidates; exact/history workspace сохраняет ineligible объект с причиной и разрешёнными действиями. Applied Name/parent не подменяются newest pending snapshot; canonical Variant route следует applied relation. Целевые routes/navigation определены Q13, отставание текущих исходников не делает их неопределёнными.

Durable receiver acceptance/newest watermark не worker apply/readiness: accepted22 excludes21 even before assessment. Один newest candidate, AppliedState wholly old/absent until full apply. Missing parent durable/noTTL/fake Product, Archived parent separate. Higher trusted identifiable malformed/unsupported source input creates scoped IngestionFault/unreadiness, не F15/manual task/empty result; unknown identity не guessed entityblock, historical Delivery unaffected alone.

При Open ordinary affected writes blocked; F15 warning сохраняет ID/dirty input. New target updates same episode, old Issues marked stale until assessment newest; missing-parent target keeps Open+waiting. Compatible source correction даёт atomic full apply+Resolved System SourceChangedApply/history, без user Resolve/publish/autoresume/history rewrite. Manual corrective repair остаётся отдельным commit: только allowed patch corrections/three previews, не apply/close. Subsequent explicit Resolve снова checks fresh newest/parent/dependencies; stale rejection retains repair. Canonical parent changes only on confirmed full applied result любого выбранного source/manual path.

Archived parent ограничивает новые операции own Active Variant без перезаписи его lifecycle. Archive оставляет read/history, unchanged references, removal и eligible replacement без расширения; не допускает новые releases/revisions, обычную activation/rollback/automatic promotion. Open F15 не обходится обычным maintenance Save. Safety actions сохраняются по правам; Archive/Conflict не выполняют Unpublish/Withdrawal/revoke и не запрещают historical materialized Delivery по ранее подтверждённому entitlement, включая новые sessions. Restore приходит source Active snapshot и повторяет own/parent/other guards без autoactivation/unwithdraw/grant.

Выбранный MVP writer protocol [Q20](../../backend/questions/Q20-persistence-and-migration.md) защищает fresh facts до каждого отдельного repair/Resolve/source commit; preview остаётся read observation. Durably accepted newer candidate уже запрещает stale apply, даже пока newest impact/parent processing ожидает; confirmed repair сохраняется. Own412, foreign/basis409 и technical coordination/read failure показаны отдельно. Потерянное подтверждение требует unknown-outcome recovery Q15 без blind replay/new token; technical failure не создаёт CatalogConflict reason или пустой valid impact.

Same EventId retry/equal-version newEventId same-state no-op/conflicting quarantine — server semantics, не UI actions. Internal pending resolver может продолжить без нового external message. Reconciliation only protected operational VR School selected IDs/all source inclparents: scheduled/error report !=CMready, no CM sync/bootstrap/button/publicHTTProute/permissionliteral. [Q23 V2](../../backend/questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2) owns protocol; configurable technical work/receipt retention не удаляют current/pending/conflict history и не создают global readiness generation/security epoch/license changes.

Pages/экраны, FSD-блоки, состояния и проверки раскрыты в F06/F15; модуль не копирует модели и алгоритмы. Wire DTO/repair carrier/type/recovery остаются Q15/Q20, UI их не выдумывает. Demo single-process AssetId guard сохраняет отдельный scope без зависимости от MVP каталога.

## Ограничения

Не строить копии вариантов для организаций, не делать каталог экраном скачивания Unity-приложений, не подменять backend guards disabled-кнопкой. Нет Ignore/alternate parent/local catalog edit. Repair/Resolve не публикуют контент и не переписывают saved releases/revisions. Специалист действует по конкретным правам и F18 audit; отдельный source fix выполняет уполномоченный VR School user. Demo не зависит от каталога/VR School/conflict/auth.

## Связи

[Q10](../../../business/questions/Q10-permissions-and-isolation.md) · [Q13](../questions/Q13-ui-scope.md) · [Q24](../questions/Q24-admin-sign-in.md) · [Q25](../../../business/questions/Q25-catalog-conflicts.md) · [Q27](../../../business/questions/Q27-catalog-archiving.md) · [Q15](../../backend/questions/Q15-api-and-source-code.md) · [Q20](../../backend/questions/Q20-persistence-and-migration.md) · [F18](../features/F18-staff-activity-history.md).

[Модули](../../../modules.md) · [Фичи](../../../features.md) · [Demo/MVP/Production](../../../milestones/README.md).
