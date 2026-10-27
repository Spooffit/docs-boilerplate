# Q28. Объект проверки и последствия решения админа


## Статус


Блок 22 закрывает основную бизнес-семантику модерации MVP.


В Demo модерации, ролей и авторизации нет.


## Объект обязательной модерации


Основной и обязательный объект проверки в MVP — **конкретная immutable `ProductVariantPublicationRevision`**.


Отдельная обязательная модерация не вводится для:


- `EntitySchemaVersion`;
- `EntityInstanceVersion`;
- `ProductContentRelease`;
- `ProductVariantContentRelease`;
- `ContentEntitlementPolicy`.


Published Schema/Instance versions остаются immutable authoring artifacts и могут использоваться Publishing/tooling. Release является immutable входом composition. Quality gate клиентской публикации применяется к уже materialized итоговой revision Variant.


Один moderation record/iteration относится к одной exact PublicationRevision. В одном approval не объединяются несколько разных revisions. Automatic promotion UI может группировать moderation items по batch, но решения остаются независимыми по Variant/revision.


## Место в lifecycle


Целевой MVP workflow:


```text
Assignments
  ↓
Releases
  ↓
PublicationRevision
  ↓
Moderation
  ↓
Activation
  ↓
Delivery effective
```


Approval означает: **эта exact PublicationRevision разрешена для activation**.


Approval сам по себе не равен activation и не переключает Delivery, кроме отдельно определённого Automatic flow ниже.


При manual promotion после Approval специалист выполняет Activate отдельным действием.


При automatic promotion:


1. для Variant строится и валидируется новая PublicationRevision;
2. revision автоматически отправляется на модерацию;
3. operation Variant получает состояние ожидания модерации;
4. после Approval **именно эта revision автоматически активируется**;
5. после Delivery projection она становится effective.


Каждый Variant automatic batch проходит этот процесс независимо. Approval одного Variant не ждёт решения по другим.


## Что видит админ


Moderation Workspace показывает exact immutable revision:


- materialized final composition;
- EntitySchema / SchemaVersion context;
- итоговые EntityInstanceVersion;
- provenance Product / Variant local / Override;
- summary exclusions;
- ChangeComment revision;
- ChangeComment source releases;
- links/details точных source versions.


Основной comparison для review — semantic diff относительно текущей **Delivery effective revision** Variant:


- Added;
- Changed version;
- Removed.


Field-level JSON diff не является обязательным default-view MVP.


## Iterations и решения


Минимальная смысловая модель moderation iteration различает:


- Pending;
- Withdrawn;
- Approved;
- Rejected/Returned.


Точные enum/type names — техническая деталь.


### Submit


Специалист отправляет exact PublicationRevision. Одновременно допускается только одна active Pending moderation iteration для одной revision.


### Withdraw pending


Специалист может отозвать Pending iteration до финального решения админа. Withdraw не удаляет запись и историю.


Ту же exact withdrawn revision разрешено отправить снова как **новую moderation iteration**, если специалист решил, что её содержимое всё-таки корректно.


### Reject / Return


Админ возвращает revision на доработку с обязательной содержательной причиной.


Revision immutable и не исправляется. После изменений специалист создаёт новые release/revision и отправляет новую revision на moderation.


Rejected revision не превращается обратно в Pending через редактирование или resubmit той же iteration.


### Approve


Approval относится только к exact `RevisionId`.


Approval не переносится на новую revision по hash или совпадению materialized content.


Approved revision может оставаться неактивированной неограниченно долго, пока не возникли отдельные invalidating facts: Withdrawal, конфликт/недопустимость или другие согласованные ограничения.


## Comments


Админ может добавлять комментарии к конкретной moderation iteration без изменения статуса.


Final Reject/Return требует содержательную причину.


Отдельный чат/thread с ответами специалиста в MVP не вводится. Специалист видит комментарии и после исправления создаёт новую moderation iteration.


## Права и concurrency


MVP **не вводит обязательный four-eyes rule**.


Если один сотрудник имеет необходимые permissions специалиста и админа, backend не запрещает self-approval только потому, что `SubmittedBy == DecidedBy`.


При этом history всегда сохраняет SubmittedBy и DecidedBy, поэтому self-approval наблюдаем и правило можно ужесточить позже отдельным бизнес-требованием.


При конкурентном решении двух админов первое successful final decision фиксируется; второе получает concurrency conflict и должно перечитать актуальный status.


## Activation и повторное использование Approval


Activation backend повторно проверяет:


- exact revision Approved;
- revision всё ещё допустима;
- нет Withdrawal/блокирующего состояния.


UI-статус Approved не является доказательством права на activation.


Rollback на historical revision, которая уже была Approved, **не требует повторной moderation**, потому что это та же exact immutable revision.


Rollback на revision, которая никогда не была Approved либо была только Rejected/Withdrawn, запрещён как activation target.


Withdrawal после Approval сохраняет approval history, но делает revision недопустимой для новой activation/Delivery.


## Что не требует F17 moderation


### Unpublish


Ordinary Unpublish не требует moderation. Он снимает current publication и не выпускает новый контент.


### Withdrawal


Explicit Withdrawal revision не проходит обычный F17 approval workflow. Это отдельное destructive/administrative действие со своими permissions и audit.


### ContentEntitlementPolicy


Policy versioning/history и permissions отдельны от content moderation.


### Schema Registry / Code Generation


Published SchemaVersion доступна Registry/codegen независимо от moderation PublicationRevision. Если позже потребуется отдельное approval schema-contracts, это новая бизнес-функция, а не расширение F17 по умолчанию.


## Конфликты и изменение состояния во время moderation


F15 catalog conflict блокирует новую composition/submission, если scope неоднозначен.


Если блокирующий conflict возникает уже после Submit, Pending moderation не может быть Approved до повторной server validation актуальности.


Retire source release/dependency сам по себе не разрушает materialized revision и не обязан отменять moderation, если revision остаётся исторически допустимой.


Если revision/dependency становится Withdrawn либо возникает иной факт, делающий revision недопустимой для распространения, Pending iteration становится недопустимой для Approval. История submission сохраняется.


## История


Предметная moderation history revision хранит все iterations:


- SubmittedBy;
- SubmittedAt;
- comments;
- WithdrawnBy/At при наличии;
- DecidedBy;
- DecidedAt;
- final decision/reason.


F18 Activity History фиксирует общие действия сотрудников, но не заменяет предметную moderation history.


## Применение


[F17](../features/F17-content-review.md) реализует moderation process. [F10](../features/F10-publication-revisions.md) создаёт immutable review target. [F11](../features/F11-publication-lifecycle.md) разрешает activation только Approved revisions и учитывает automatic flow. [Q10](Q10-permissions-and-isolation.md) задаёт permissions. [F18](../features/F18-staff-activity-history.md) хранит общий audit trail.


Модерация является **quality gate клиентской публикации**, а не универсальным approval engine всех сущностей Content Manager.

## F17: technical boundaries

[Backend F17](../../technical/backend/features/F17-content-review.md) / [Frontend F17](../../technical/frontend/features/F17-content-review.md) описывают согласованный exact quality gate. Core iteration/comments имеют own ETag; immutable F10 target и живые lifecycle/F15/Delivery diff reads не включаются в тот же concurrency token.

Known workflow: initial Submit и новая iteration после Withdrawn; correction rejected content создаёт новые releases/revision. Не согласованы appeal/reconsideration неизменённой Rejected и повторный Submit уже Approved artifact. Нельзя автоматически открыть эти cases только из generic Submit handler. Если их добавлять, нужно определить authority старого Approval при поздней иной decision; currently exact Approved evidence используется F11 вместе с current eligibility.

Comments target MVP добавляются Admin пока Pending; editing/deletion и post-final discussion/chat не вводятся. Meaningful Text/RejectReason required, но числовые limits/normalization и approval optional reason пока не определены.

Semantic default comparison: real result EntityInstanceId + exact schema/data version pair относительно current Variant Delivery effective basis; override target A не заменяет result B. Confirmed no-effective допускает empty basis/all Added; unavailable/missing projection не равны отсутствию publication. Approve подтверждает показанный comparison basis отдельно от own moderation If-Match: drift требует refresh/new consent и independent basis conflict, не автоматической смены own ETag. После committed Approval последующее изменение comparison baseline само по себе не отменяет decision; activation проверяет собственный intent/basis/current guards. Metadata/order/provenance-only differences beyond три категории и exact basis wire materialization остаются Q15.

Pending invalidity не auto Reject/Withdraw; history сохраняется, current invalid Approve blocked. Retire source release не blanket invalidation materialized revision; exact eligible target/current restriction semantics и guard profile связаны с Q20/Q23. Approval не activation/Delivery, decision self-approval allowed при обеих permissions, shared staff scope сохраняются.

Automatic preparation теперь атомарно создаёт revision/iteration/exact item binding в Publishing; no PreparedEvent→separateSubmit correctness gap. Смена automatic intent не Withdraw-ит human iteration: допустимый exact target может получить Approval для последующего manual использования. Withdraw Pending iteration отменяет связанный item, но её новый manual Submit создаёт новую iteration без старого automatic binding. Rejected сохраняет прежнюю публикацию. Effective dependency closure по Q09 применяется к target restrictions; historical retention/evidence остаются шире.

ClientOperationId для Submit обозначает один intent и confirmed replay, не разрешение повторной iteration: auth + confirmed execution lookup предшествуют new Submit checks. Initial/new-after-Withdrawn workflow сохраняется; расширение appeal/AlreadyApproved не следует из replay. Модели/stages [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение), contract [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid).
