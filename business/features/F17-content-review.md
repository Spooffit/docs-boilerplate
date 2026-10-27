# F17. Модерация PublicationRevision

## Цель

Сделать обязательный quality gate MVP между созданием exact immutable `ProductVariantPublicationRevision` и её activation.

В Demo модерации/ролей нет.

## Review target

Модерируется только exact `ProductVariantPublicationRevision`.

Отдельная обязательная moderation не применяется к:

- SchemaVersion;
- EntityInstanceVersion;
- Product/Variant releases;
- ContentEntitlementPolicy;
- Unpublish;
- explicit Withdrawal.

## Workflow

```text
Assignments → Releases → PublicationRevision → Moderation → Activation → Delivery effective
```

Approval означает: exact RevisionId разрешена для activation.

Approval сам по себе не равен Delivery availability.

### Manual

После Approval Specialist выполняет Activate отдельным действием.

### Automatic

Automatic promotion создаёт revision и автоматически отправляет её на moderation. Approval **этой exact revision** создаёт продолжение activation конкретного Variant при текущих условиях F11.
Создание revision, automatic Submit, exact revision/iteration binding и AwaitingModeration item подтверждаются вместе в PREPARE F11. Human decision и activation выполняются отдельно. Чужая уже Pending iteration не присваивается операции по hash, source IDs либо равному content.

Approval сохраняет durable exact continuation, но не разрешает перезаписать последующее решение специалиста. Перед продолжением проверяются exact approved RevisionId, current Variant automatic head, captured ProductStateVersion и Variant Sequence, Active Variant/parent F06, affected closure F15 и current ограничения final effective closure F10. A→B→A либо R1→R2→R1 не восстанавливают старую basis. Cancelled/Superseded не возобновляются через Restore/Resolve/event; сохранённая Approved revision может быть отдельно activated вручную. Правила [F11](F11-publication-lifecycle.md) и [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение) уже согласованы; технические signatures/контракты остаются у backend.

Variants одного batch модерируются независимо.

## Moderation iteration

Одна iteration относится к одной exact PublicationRevision.

Смысловые states MVP:

- Pending;
- Withdrawn;
- Approved;
- Rejected/Returned.

Одновременно не более одной Pending iteration на одну revision.

## Specialist actions

Specialist:

- Submit exact revision;
- видит status/comments/history;
- может Withdraw Pending до final decision;
- после correction создаёт новые release/revision и submits новую revision;
- может повторно submit ту же exact withdrawn revision новой iteration, если содержимое всё же признано корректным.

Withdraw не удаляет history.
Withdrawal exact iteration, связанной с automatic item, отменяет именно этот item. Новый manual Submit новой iteration той же revision не reattaches старую operation. Отмена либо supersession automatic item сама по себе не Withdraw-ит human review; Rejected не меняет прежний active Publishing pointer.
Новый Submit affected revision запрещён при blocking Conflict; Archived Variant/parent также ограничивает обычную новую publishing работу. Resolve/Restore не заменяют повторную проверку scope и eligibility перед продолжением moderation/activation.

Rejected immutable revision не «редактируется и переотправляется» как та же revision.

## Повтор Submit

Submit требует ClientOperationId — GUID в теле команды. После проверки доступа confirmed execution lookup по исходному ID и bound inputs/If-Match/context/review/comment/actor предшествует проверке новых предусловий. Same ID/input возвращает первоначальную receipt и ту же iteration, даже если она уже Withdrawn/Approved/Rejected или lifecycle revision изменился; replay не создаёт iteration заново, не Restore и не Activate. Changed bound input →409; отказ до commit не подтверждает binding ID. При lost ACK выясняется исход same operation, без blind duplicate Submit. Точная форма принадлежит [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid).

## Administrator actions

Administrator:

- видит exact materialized revision;
- видит source releases/ChangeComments/provenance;
- видит semantic diff относительно current Delivery effective revision: Added / Changed version / Removed;
- может добавлять comments;
- может Approve;
- может Reject/Return с обязательной содержательной причиной.

Показанная semantic comparison basis содержит exact current Delivery effective revision либо подтверждённый null и необходимые сведения сравнения. Она независима от own moderation ETag. Approve повторно проверяет именно показанное основание; если Delivery effective basis успела измениться, decision не фиксируется, возвращается409 и требуется refresh с новым consent. Unavailable/incomplete Delivery read не означает null или пустой diff. Новый delivery transition после committed Approval не отменяет это Approval; дальнейшая activation всё равно проходит собственные проверки F11.

Отдельный chat/thread специалист↔админ в MVP не требуется.

## Approval rules

Approval:

- exact RevisionId only;
- не переносится на новую revision по hash;
- сохраняет SubmittedBy/DecidedBy/timestamps;
- self-approval допустим, если сотрудник имеет обе permissions; обязательный four-eyes rule не вводится.
- current instance/schema/media ограничения проверяются по final effective closure F10: suppressed A не блокирует действующий B, shared необходимая B dependency проверяется; source provenance остаётся в historical retention.

Rollback на ранее Approved exact revision не требует повторной moderation, если revision всё ещё lifecycle-allowed.

Never-approved/Rejected/Withdrawn-only revision не может Activate/Rollback.

Withdrawal после Approval сохраняет decision history, но делает revision недопустимой для выдачи.

## Concurrent decisions

Два админа могут открыть одну Pending iteration. Первое final decision фиксируется, второе должно получить concurrency conflict и перечитать state.

## Invalidation while Pending

Approve блокируется, если после Submit появились:

- blocking CatalogConflict в authoritative affected closure;
- Archived Variant либо его parent;
- Withdrawal revision либо недопустимость instance/schema/media её final effective closure;
- другой факт, делающий revision недопустимой для activation.

Retire source release сам по себе не разрушает уже materialized revision, если historical use остаётся допустимым.

## History

Предметная moderation history хранит все iterations, actors, comments, withdrawal/final decisions.

F18 Activity History дополняет её общим audit, но не заменяет предметную history.

## Acceptance

- exact immutable revision review;
- Pending withdraw preserved;
- Reject reason mandatory;
- approval exact RevisionId;
- manual approval != activation;
- automatic approval continues только exact current bound item при условиях F11;
- suppressed unavailable A не блокирует Approve итогового B, необходимая B shared dependency остаётся в проверке;
- self-approval allowed only with proper combined permissions;
- race has one final decision;
- old approval cannot override Withdrawal.
- comparison-basis drift при прежнем moderation ETag требует refresh/new consent409, unavailable read не превращается в empty comparison;
- более поздний delivery basis change не отзывает уже confirmed exact Approval;
- same ClientOperationId replay после lost ACK сохраняет initial iteration/receipt, changed bound input409 и Withdrawn iteration не submits заново;
- automatic exact binding атомарен с PREPARE, чужая Pending не принимается по hash;
- Withdraw bound iteration cancels exact item, новый manual Submit его не reattaches; Supersede/Cancel item не Withdraw-ит review и Rejected не меняет pointer.

## Связи

[F10](F10-publication-revisions.md) · [F11](F11-publication-lifecycle.md) · [F16](F16-staff-access.md) · [F18](F18-staff-activity-history.md) · [Q28](../questions/Q28-content-review-scope.md) · [Backend](../../technical/backend/features/F17-content-review.md) · [Frontend](../../technical/frontend/features/F17-content-review.md)
