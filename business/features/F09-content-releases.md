# F09. Релизы назначенного контента

## Цель

Зафиксировать mutable Product/Variant assignments как immutable snapshots для исторически воспроизводимых PublicationRevision. Сохранённое рабочее состояние может иметь release blockers; создание release требует отдельной проверки.

## ProductContentRelease

После перехода schema-selection release имеет формат SchemaRulesV1. Старый LegacyExact сохраняется для history/lifecycle и уже materialized revisions, но не является source нового F10 Create: требуется подготовить новый release. Upgrade не изменяет его content/hash и не отзывает действующую revision.

ProductContentRelease фиксирует текущие schema selections ProductContentAssignment и проверенный результат их разрешения: exact SchemaVersion, exact Published EntityInstanceVersion, selection intent, ChangeComment и момент/инициатора создания. После создания content release не меняется. Правило AllExcept не остаётся живым запросом внутри release.

Для AllExcept preparation учитывает новые Published экземпляры и последнюю Published version каждого logical instance именно в выбранной структуре; SelectedOnly сохраняет exact pins. Review показывает дерево схем/экземпляров, включения, исключения, версии и причины невключения. Недоступный включаемый winner — blocker, не повод молча выбрать старую версию или пропустить экземпляр. Create фиксирует именно подтверждённый набор; новый instance/winner либо иное изменение basis требует повторного review. Own assignment ETag сам по себе не доказывает неизменность ContentModeling universe. Правила [Q39](../questions/Q39-schema-content-selection.md) дополняют review ниже.

Явный пустой Product assignment разрешено зафиксировать как настоящий ProductContentRelease. Полностью пустой Variant patch также может стать настоящим ProductVariantContentRelease. Это намеренные snapshots, а не фиктивные artifacts ради заполнения ссылки; итоговая PublicationRevision всё равно подчиняется непустому результату F10.

Retained reference, разрешённая при working Save, не освобождается от текущих release guards. Retired/Withdrawn/Archived/unavailable content или недопустимые необходимые dependencies блокируют новый ProductRelease до замены/удаления. Latest не выбирается автоматически.

## ProductVariantContentRelease

ProductVariantContentRelease фиксирует только local patch ProductVariant: frozen schema selections и их SchemaId replacement-mask, additions, overrides, inherited schema/instance exclusions и ChangeComment. Это не полный итоговый состав Variant. Собственный selector материализуется один раз при F09: последующий F10 использует frozen local references и подавляет inherited состав всей соответствующей logical schema, даже при выборе другого ProductRelease.

Release сохраняет provenance контекста, в котором patch проверяли: exact ProductContentRelease либо явный EmptyBase. Контекст обязателен для review/Create, но provenance не превращает VariantRelease в hard dependency на один ProductRelease. Точная форма технических полей определяется серверной спецификацией.

VariantRelease можно использовать с другим ProductRelease того же Product при повторной полной validation F10. Успешное создание доказывает допустимость patch только в проверенном контексте, а не во всех будущих базах.

## ValidationContext Variant release

Перед semantic review специалист явно выбирает один контекст:

- **Exact ProductContentRelease** — конкретный immutable release соответствующего Product;
- **EmptyBase** — сознательный выбор пустого inherited layer для подготовки Variant-only composition.

Текущий live Product assignment и current active ProductRelease не подставляются как неявная база. Отсутствие ProductRelease не означает EmptyBase автоматически. В authoring live preview и release review используются разные явно обозначенные основания.

Для выбранного контекста выполняется композиция по F08 и применимая validation: ownership, доступность выбранных source releases, intrinsic patch invariants, same-schema/trusted captured target evidence, итоговая uniqueness, текущие необходимые result dependencies и scoped CatalogConflict guards. New-use проверка instances/schema/media относится к final effective closure по F10: inherited A, полностью подавленная exclusion/override в пользу B, не блокирует результат из-за своей текущей недоступности. Общие dependencies, используемые итоговым B, проверяются. Historical retention остаётся шире итоговой closure.

Коллизия в выбранном контексте блокирует Create VariantRelease. Live collision может оставаться в working preview и не блокировать release с другим явно проверенным immutable контекстом, если все его guards выполнены. При EmptyBase ранее сохранённый отсутствующий target остаётся warning, result остаётся local content; это не разрешает создавать новые absent-target overrides в F08.

Ручное поле «Порядок в релизе»/SortOrder не входит в release. ReleaseNumber определяет последовательность создания artifacts, а canonical serialization задаёт только детерминированное представление набора. Бизнесовая последовательность элементов моделируется в самом контенте. Мастер [F07](F07-product-content-assignment.md#массовая-фиксация-и-подготовка-выпуска) может вызвать создание release только после полного успеха подтверждённой schema/instance фиксации и обычных assignment/patch checks; он не обходит review/basis/guards этой фичи.

## Semantic review и создание

1. Специалист сохраняет допустимый working assignment/patch.
2. Для VariantRelease явно выбирает ValidationContext.
3. Система показывает semantic summary/diff относительно previous latest-created release того же owner по ReleaseNumber, включая Retired: Added / Changed version / Removed, а также диагностику и CanCreateRelease. Exact previous ReleaseId фиксируется в review. Для первого release предыдущего artifact нет; фиктивный пустой baseline не создаётся. Field-level JSON diff не обязателен.
4. Специалист проверяет именно сохраняемый patch и выбранный exact контекст и задаёт ChangeComment.
5. Create повторно проверяет текущее основание, lifecycle, необходимые source/result dependencies и composition guards; прежний успешный review не является разрешением обходить новые blockers.
6. При успехе фиксируется immutable snapshot и provenance проверки.

Изменившийся после review patch требует нового review. Переключение ProductRelease/EmptyBase меняет контекст проверки и также требует соответствующего review. Изменение внешнего lifecycle/dependencies после review может заблокировать Create, даже если own assignment ETag и content выбранного release не изменились.

Создание более позднего release другим запросом не меняет уже подтверждённый previous ReleaseId, если собственный source и выбранный validation context остаются допустимыми. Намеренный новый release с тем же content и тем же либо другим ChangeComment разрешён; hash не служит deduplication нового artifact.

Server diagnostic basis учитывает контекст и зависимости отдельно от own assignment ETag. Ошибка чтения нужных данных не означает валидный пустой preview; готовность неизвестна до успешной проверки. Нельзя фиксировать другой patch или контекст под ранее показанным review.

## Диагностика

- **SaveBlockers** отражают intrinsic ошибки/new-use/access/concurrency и запрещённые working mutations; они устраняются в F07/F08.
- **ReleaseBlockers** включают retained unavailable effective references/dependencies, коллизии выбранного контекста и применимые запреты создания release; suppressed provenance оценивается по границе F10.
- **Warnings** включают absent retained target либо Deprecated reference, если текущая policy допускает использование.

«Сохранено», diagnostics и CanCreateRelease — отдельные состояния. Retained unavailable reference можно сохранить, но нельзя опубликовать её через новый release. Warning сам по себе не блокирует Create, если остальные guards выполнены.

Открытый blocking CatalogConflict F15 запрещает создание release authoritative affected scope. Archived owner либо Archived parent Variant запрещает новые releases по F06. Maintenance и corrective repair не дают разрешения Create; repair сохраняет working patch и не материализует release. Fresh lifecycle/scope/dependency checks обязательны; unrelated scope продолжает работу.

## ChangeComment

Release имеет ChangeComment, но отдельный пользовательский Title не нужен. Комментарий объясняет смысл изменений, может содержать номер/ссылку внешней задачи обычным текстом и не меняет identity/content release.

## Lifecycle

Release immutable по content. MVP поддерживает Retire/Restore:

- Retired release нельзя использовать для новой PublicationRevision;
- существующие PublicationRevision сохраняются;
- historical provenance не удаляется;
- Retire обратим, Restore сам по себе ничего не публикует и не активирует.

Current active Product base нельзя Retire до выбора другой базы либо снятия current base. Assignment Save, dependency Retire и изменения live базы не пересчитывают existing releases/revisions.

## Automatic / Manual

ProductRelease может быть отправной точкой manual либо automatic promotion. Mode выбирается для конкретной операции promotion, а не является постоянной настройкой Product.

## Повтор Create

Create ProductRelease/VariantRelease требует ClientOperationId — GUID в теле команды. После проверки доступа сервер сначала ищет подтверждённое выполнение этого ID с исходными input, If-Match, validation/review context, ChangeComment и actor, и только для нового выполнения проверяет текущие предусловия. Same ID и bound input возвращают первоначальную confirmed receipt, даже после изменения assignment/source или Retire результата; replay не создаёт новый release и ничего не Restore. Изменение bound input даёт409. Отказ до commit не связывает ID с подтверждённым исполнением. При потере ответа/commit ACK клиент выясняет исход по тому же ID, не создаёт blind duplicate. Точная форма исполнения относится к [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid).

## Результат

Immutable ProductContentRelease либо patch-only ProductVariantContentRelease с provenance явно проверенного контекста. Создание release не является approval, activation или клиентской публикацией.

## Acceptance

- release review явно раскрывает exact результат schema policies; новый Published instance/winner после review требует повторного подтверждения;
- AllExcept не сохраняется в release как будущий live query: historical count/versions не меняются при новых публикациях;
- VariantRelease сохраняет whole-schema replacement-mask и frozen local membership отдельно от inherited exclusions.

- изменение assignment после release не меняет snapshot; exact versions и ChangeComment сохраняются;
- явный empty Product release и fully empty Variant patch release создаются; намеренный same-content release с новым ID/comment получает новую identity;
- review сохраняет latest-created previous ReleaseId, включая Retired; новый чужой release не подменяет этот baseline;
- same ClientOperationId/input после lost ACK возвращает original receipt, changed input даёт409 и Retired result не восстанавливается;
- retained effective A/v2 после Retire можно оставить при Save, но новый release с этой effective reference блокируется до удаления/допустимой замены;
- suppressed unavailable A не блокирует VariantRelease с допустимым итоговым B; shared необходимая B dependency продолжает проверяться;
- VariantRelease остаётся patch-only и создаётся только с явным exact ProductRelease или EmptyBase; отсутствующий выбор и implicit live/current active/default empty отклоняются;
- patch A→B и выбранная base A+B дают collision: review показывает blocker, Create отклоняется; база только A может дать допустимый B при остальных guards;
- EmptyBase сознательно создаёт Variant-only контекст: retained A→B с отсутствующим A оставляет B и warning, без удаления intent;
- изменения patch либо выбранного контекста после review требуют нового review; dependency lifecycle change при прежнем own ETag заново проверяется Create и может его отклонить;
- failed context/dependency read не становится пустым валидным preview или CanCreateRelease=true;
- provenance exact ProductRelease/EmptyBase сохраняется, но другой ProductRelease того же Product допустим в F10 только после полной новой validation;
- успех в одном контексте не обещает совместимость с другой базой; конфликтную комбинацию F10 не материализует;
- Retired release запрещён для новой revision; historical revision/provenance сохраняются;
- blocking CatalogConflict запрещает affected Create; ограниченное maintenance не даёт conflict bypass и не вводит auth в Demo.

## Связи

[F07](F07-product-content-assignment.md) · [F08](F08-variant-content-assignment.md) · [F10](F10-publication-revisions.md) · [F11](F11-publication-lifecycle.md) · [F15](F15-catalog-conflicts.md) · [Q07](../questions/Q07-exclusions-and-local-content.md) · [Q09](../questions/Q09-editing-and-deletion.md) · [Q27](../questions/Q27-catalog-archiving.md) · [Q25](../questions/Q25-catalog-conflicts.md).
