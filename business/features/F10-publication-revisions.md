# F10. Итоговая PublicationRevision ProductVariant
## Цель

Новые Preview/Create используют SchemaRulesV1 source releases. LegacyExact остаётся доступен через историю; для нового клиентского состава специалист готовит новый release. Existing materialized revisions и их допустимый rollback/delivery этим не отменяются.

Schema-first назначения F07/F08 доходят до revision через exact releases F09. Сначала из выбранного ProductRelease удаляется inherited состав logical SchemaId, заменяемых VariantRelease; затем композируются frozen local selections и остальные допустимые F08 правила. Собственный SelectedOnly из двух даёт два вместо всей inherited группы. Собственные selector exclusions не превращаются в inherited exclusions.

Revision не исполняет schema selection по текущему ContentModeling: новые экземпляры/версии могут попасть только через новый подготовленный release. Tree review показывает schema/version groups и final exact instances; изменение выбора выполняется в working assignment с новым release, а не правкой immutable revision. Пустое назначение схемы само по себе не отменяет существующее требование непустого final content. [Q39](../questions/Q39-schema-content-selection.md).
Материализовать один immutable итоговый клиентский состав ProductVariant из конкретных immutable Product/Variant releases.
PublicationRevision является review/activation target и историческим source для Delivery.
Revision не содержит пользовательского SortOrder/«Порядка в релизе». Детерминированный порядок manifest задаёт canonical profile, а не форма назначения. Создание в мастере [F07](F07-product-content-assignment.md#массовая-фиксация-и-подготовка-выпуска) использует только явно проверенные exact source releases и не активирует результат: moderation/activation/Delivery readiness остаются самостоятельными шагами.
## Источники
Для revision явно выбираются ProductContentRelease без VariantRelease, ProductVariantContentRelease без ProductRelease либо оба release одновременно.
Оба источника не могут отсутствовать: нужен минимум один exact source release. Явно созданный пустой release F09 допустим как source, но final effective composition должна содержать хотя бы один instance. Пустой фиктивный VariantRelease ради ссылки не создаётся.
## Composition
При наличии обоих sources применяется алгоритм:
1. ProductRelease base;
2. schema exclusions inherited layer;
3. instance exclusions inherited layer;
4. explicit local overrides;
5. local additions;
6. collision validation.
Deep merge JSON отсутствует. Merge authority inherited позиции — EntityInstanceId. Override A→B даёт в результате настоящий B.
## Validation
Перед созданием система проверяет:
- releases существуют и доступны для нового использования;
- принадлежат правильным Product/ProductVariant;
- VariantRelease совместим с выбранным ProductRelease;
- F09 validation context является provenance, не обязательной базой: другая комбинация проходит полную F10 validation; успешный release не обещает совместимость со всеми будущими базами;
- override target/result относятся к одной EntitySchema;
- итоговый EntityInstanceId уникален;
- final effective instances и их exact schema/media dependencies допустимы для нового использования;
- полностью suppressed inherited instance A не проходит current new-use eligibility: её недоступность не блокирует действующий итоговый B; shared schema/media dependency, необходимая B, всё равно проверяется;
- доступность самих выбранных source releases, ownership и trusted captured evidence для правил композиции проверяются всегда; suppression не отменяет этих проверок;
- final composition непуста;
- Variant и его parent Active по F06; Archived запрещает новые revisions даже после maintenance/repair;
- открытые CatalogConflict не блокируют composition.
Preview и Create выполняют одну бизнес-валидацию, но Create повторяет её заново и не доверяет ранее показанному preview.

Та же final effective closure применяется при F17 Approve, F11 Activate/Rollback и F13 проверках ограничений. Historical retention охватывает также provenance и suppressed references: их отсутствие в runtime result не разрешает удалять history или referenced artifacts. Нельзя получать пустой валидный результат из unavailable/incomplete чтения.
F15 blocker применяется только к authoritative affected closure. Historical immutable revision сама по себе не блокирует move; future источники новой revision после move должны принадлежать актуальным owners. Existing revision не переписывается и не теряет historical Delivery только из-за Archive/Conflict.
## Materialized result
PublicationRevision хранит immutable итоговый набор exact EntityInstanceVersion и provenance.
Historical revision не пересчитывается текущим merge-алгоритмом позже. Изменение composition logic влияет только на новые revisions.
## ChangeComment
Revision имеет ChangeComment без отдельного Title. Комментарий описывает смысл итогового состава, полезен при moderation/history/rollback и не меняет content identity.

Намеренное Create с тем же итоговым content и тем же либо другим ChangeComment разрешено и создаёт новую revision identity. ContentHash с profile cm-publication-manifest/1 описывает canonical runtime manifest: exact schema/instance membership/versions, frozen runtime aliases и content profile+digests, а values остаются immutable instance bytes. Source provenance/override targets/exclusions/release IDs/actor/comment/time/RevisionId не manifest bytes и сохраняются отдельно. Equal runtime payload даёт equal profile+digest даже у разных artifacts; hash не deduplicates и не переносит Approval. Bytes/profile/digest immutable, no historical rehash; full technical profile [Q19](../../technical/backend/questions/Q19-content-hashes.md#принятый-canonical-profile-v1).

## Повтор Create
ClientOperationId — обязательный GUID в теле Create. После проверки доступа сервер сначала проверяет confirmed execution с исходными inputs/If-Match/context/review/ChangeComment/actor. Same ID и bound input возвращают первоначальную receipt до проверки новых lifecycle/preconditions; изменившийся source либо Retire результата не создают revision заново и не восстанавливают её. Changed bound input →409; failure до commit не является confirmed binding. Unknown outcome после lost ACK выясняется по исходному ID, без blind нового Create. Точные fields/хранение receipts принадлежат [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid).
## Lifecycle
PublicationRevision после создания immutable по content.
MVP lifecycle:
- Available;
- Retired;
- Withdrawn.
Retired revision нельзя снова Activate/Rollback, но historical/frozen delivery может продолжать использование.
Withdrawn revision запрещена для дальнейшей серверной выдачи, Freeze, Activate и Rollback. Automatic replacement отсутствует.
Active revision перед Retire должна быть сначала заменена либо Unpublish. Withdrawal active revision переводит Publishing current state в null без fallback.
## Moderation
Revision является единственным обязательным F17 moderation target MVP.
Создание revision не означает approval или activation.
## Результат
Immutable materialized ProductVariantPublicationRevision.
## Acceptance

- frozen Variant SelectedOnly из двух заменяет всю inherited schema group выбранного ProductRelease;
- новое live содержимое схемы не расширяет revision, созданную из прежних exact releases;
- policy/source intent объясняется в tree provenance и не превращается в SDK runtime selection rule.
- Product-only, Variant-only и combined revision поддерживаются;
- минимум один exact source и непустой final composition обязательны; пустой source сам по себе не является ошибкой;
- suppressed unavailable A не блокирует B; необходимая B shared dependency проверяется, historical retention не сужается;
- intentional same-content/comment Create создаёт новую identity, replay same ClientOperationId возвращает исходную receipt и changed input отклоняется;
- invalid composition не создаётся;
- preview не является authority Create;
- historical revision не recompute;
- ChangeComment сохраняется;
- Retire и Withdrawal имеют разные последствия;
- revision после создания ещё не доступна клиентам автоматически.
## Связи
[F09](F09-content-releases.md) · [F11](F11-publication-lifecycle.md) · [F17](F17-content-review.md) · [Q07](../questions/Q07-exclusions-and-local-content.md) · [Q28](../questions/Q28-content-review-scope.md).
