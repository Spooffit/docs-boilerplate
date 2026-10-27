# M04. Публикация


## Назначение и целостный результат

Назначения и review показывают дерево схем с экземплярами, exact версией структуры, policy и server counts. F07 определяет общий selector UI, F08 добавляет наследование/whole-schema replacement, F09/F10 показывают frozen composition. «Все»/«Все, кроме»/«Только выбранные» не вычисляются по одной загруженной странице. [Q39](../../../business/questions/Q39-schema-content-selection.md) связывает agreed semantics и незакрытые generated contracts.


Панель публикаций соединяет подготовку общего и локального состава, просмотр результата и управление доступностью проекта. Именно на границах этих действий интерфейс должен различать «сохранено», «подготовлено» и «активировано», не повторяя серверные проверки.

F07/F08 показывают Saved отдельно от адресуемых SaveBlockers, ReleaseBlockers, Warnings и CanCreateRelease; у diagnostics есть собственный basis, который не подменяется assignment ETag. Retained unavailable exact references и live-base collision intrinsically valid patch могут сохраниться с диагностикой. Live collision не закрывает F09 context selection: Create проверяется по выбранной release-паре и остальным guards. Ограниченное maintenance Inactive/Archived owner определяется удалением либо eligible заменой проблемного контента, без additions/расширения; CatalogConflict продолжает блокировать обычные writes.

F07–F12/F15/F17 используют выбранный общий MVP writer protocol [Q20](../../backend/questions/Q20-persistence-and-migration.md): server fresh facts защищены до owner commit, каждый BC сохраняет своё состояние отдельно. Workspace/picker/review/preview read не является commit permit, own If-Match не заменяет foreign/basis validation. UI различает own412, предметный foreign/basis409 и technical gate/read failure; сохраняет actual ввод/context/token и не изображает technical failure как новый CatalogConflict либо пустую valid composition. Publishing result, moderation, Delivery effective и audit projection остаются отдельными наблюдаемыми результатами. Captured-input/head/Product-Version/Variant-Sequence precedence принято; runtime security/cutoff Q16/Q17 остаются отдельными вопросами.

Create F09/F10, Automatic Start F11 и manual Submit F17 требуют GUID ClientOperationId в generated body для одного намерения. Feature сохраняет exact original request/If-Match/context/review/Comment и trusted actor identity до известного outcome; shared transport не становится business store. Same ID/input после current authorization возвращает original execution receipt до checks нового исполнения. Bound ID с изменённым input даёт409; изменённое намерение имеет новый явный ID. Unknown outcome не генерирует новый ID/token автоматически; receipt не утверждает current lifecycle/progress. Это [Q15 execution contract](../../backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid), без ручных DTO и новых browser persistence/global stores.

F12 current policy управляет следующим immutable PolicyAgreement: promise фиксируется на Publishing commit до VRS confirmation того же planned period. Delayed event/first use не выбирает today's policy. Сотрудник меняет own policy через прежний editor; он не выдаёт Agreement, не редактирует commercial dates и не подтверждает CM content grant. Разрешённая диагностика показывает frozen AgreementId/policy/provenance отдельно от current confirmed period/cutoff/unlimited/completeness, без новой issuance формы. Source planned→backend quote→same-plan confirm/recovery принадлежит VRS; CM readiness ограничивает новое CM content право, application license/Project Storage flow остаётся отдельным. [F12](../features/F12-content-entitlement-policy.md), [Q15 boundary](../../backend/questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary).

Publishing active, committed Delivery-effective snapshot, target/processed source Sequence и confirmed availability history показываются раздельно. Conservative VisibilityConfirmedAt — once-only primary observation после exact полного Delivery commit, не physical commit/activation time. Current allowed snapshot может обслуживаться при history Pending, а Freeze с necessary pending confirmation/history даёт Indeterminate; matching RevisionId не history-ready proof. Gap/materialization failure, terminal NotEffective outcome и independent exact ban имеют разные причины. UI не invent-ит polling/routes/confirmation timestamps и получает generated metadata от backend F13. [Q17](../../backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery), [Q15 carrier](../../backend/questions/Q15-api-and-source-code.md#delivery-visibility-contracts).


## Карта фич


| ID | Спецификация этого раздела |
| --- | --- |
| F06 | [Продукты и конкретные варианты](../features/F06-product-catalog.md) |
| F07 | [Общий контент продукта](../features/F07-product-content-assignment.md) |
| F08 | [Отличия конкретного варианта](../features/F08-variant-content-assignment.md) |
| F09 | [Релизы назначенного контента](../features/F09-content-releases.md) |
| F10 | [Итоговая ревизия публикации](../features/F10-publication-revisions.md) |
| F11 | [Активация, история, откат и обновление вариантов](../features/F11-publication-lifecycle.md) |
| F12 | [Политика права на контент](../features/F12-content-entitlement-policy.md) |


## Связи


[Карта модулей по разделам](../../../modules.md). [Полная карта документов фич](../../../features.md).


## Вопросы модуля


UI различает optional source releases, разрешённые explicit empty F09 snapshots, непустую prepared revision F10, current базу Product, active revision Variant и Delivery readiness. Intentional repeated Create создаёт новый artifact; replay прежнего intent возвращает original receipt. F09 review хранит exact predecessor latest-created same-owner, включая Retired, без фиктивного первого baseline. F10/F17/F11/runtime restriction используют effective output dependency closure; broader source/provenance/evidence retention не превращает suppressed dependency в blanket blocker.


[Q13](../questions/Q13-ui-scope.md) · [Q24](../questions/Q24-admin-sign-in.md)


## Workspaces Publishing


M04 frontend не строит отдельные глобальные страницы для каждой технической сущности Publishing.


Основные entry points:
- Product Workspace;
- Variant Workspace.


### Product Workspace


Tabs:
- Обзор;
- Общий контент F07;
- Релизы F09;
- Варианты;
- Политика F12.


Product assignment — mutable working set. ProductRelease — immutable snapshot. Automatic promotion показывает captured sources/membership и независимые per-Variant outcomes. Technical recovery сохраняет exact item/result tuple; domain retry — новое confirmed subset намерение/operation. Новый automatic head supersedes прежний unfinished item, actual manual transition снимает head; manual same-active/already-null no-op его сохраняет. Explicit Cancel exact item имеет own precondition, не создаёт publication history и не отменяет replacement head. Эти обязанности и ABA/terminal guards раскрыты [F11](../features/F11-publication-lifecycle.md)/[Q20 workflow](../../backend/questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение).


### Variant Workspace


Tabs:
- Обзор;
- Локальный контент F08;
- Публикация F09/F10/F11;
- История F11;
- Политика F12.


Local content показывает inherited/local/override/excluded composition. Publication tab проводит пользователя по цепочке saved patch → явный validation context F09 (exact immutable ProductRelease либо сознательный EmptyBase/variant-only) → semantic review той же пары и повторная validation при Create → patch-only VariantRelease → materialized revision preview F10 → activation/effective Delivery. Current active/live база не подставляется при создании VariantRelease; failure/missing selection не означает EmptyBase. Context/provenance не закрепляет hard dependency: reuse с другой базой требует полной validation F10.


### Общая терминология UI


- Assignment: рабочий состав;
- Release: зафиксированный snapshot;
- Revision: materialized итог Variant;
- Publishing active: выбранная activation state;
- Delivery effective: фактически доступная клиентам state.


UI не смешивает эти состояния в один «Publish».


Release/Revision ChangeComment показывается как контекст history/moderation. Source catalog fields read-only.


F15 conflict показывает banner и disabled actions с explanation. F17 manual Submit относится к exact immutable target. Automatic prepare сохраняет revision/Pending iteration/item binding одним Publishing commit, без browser Create→Submit loopback; human decision и activation остаются отдельными commits. Admin Approve захватывает shown Delivery-effective comparison basis отдельно от own moderation ETag; basis drift требует refresh/new consent, unavailable не empty. Поздний drift не отзывает committed Approval. Cancel/Supersede auto item сохраняет human moderation history; terminal item не resumes после Resolve/Restore или новой manual iteration. Per-item progress не повышает чужие tokens, operation summary derived; Activated не обещает Delivery effective.


Demo M04 не требует: весь этот workspace относится к MVP.
