# Q07. Исключения, локальный контент и override




## Вопрос и границы

### Schema-first назначения

[Q39](Q39-schema-content-selection.md) определяет собственный выбор схемы Variant: AllExcept либо SelectedOnly по exact Published структуре заменяет весь inherited состав logical SchemaId. Inherited exclusions/additions/overrides ниже продолжают действовать вне схем с own replacement. Исключения собственного AllExcept имеют отдельный scope; schema selector и старые competing rules той же схемы не нормализуются молча. F09 замораживает resolved local references, а F10 применяет captured replacement-mask без live expansion.




Документ определяет композицию общего контента Product и локальных отличий ProductVariant: stable identity наследуемых позиций, additions, overrides и exclusions.




Принято в блоке 11:




- внутренняя identity существующей логической позиции основана на `EntityInstanceId`, а не на mutable `SchemaKey` или `EntityInstanceKey`;
- Variant может не только выбрать другую версию того же EntityInstance, но и явно заменить унаследованную позицию другим логическим EntityInstance;
- Variant может иметь собственные local items, которых нет в Product;
- schema-wide и instance-level exclusions действуют на inherited layer;
- local layer применяется после exclusions и имеет более высокий приоритет;
- assignments остаются редактируемыми, releases/revisions — snapshot-артефактами.




## Stable identity и override




### Identity унаследованной позиции




Для matching существующей позиции authority — `EntityInstanceId`.




`SchemaKey` и `EntityInstanceKey` остаются полезными человекочитаемыми/runtime/tooling identifiers, но не являются merge-authority. Rename строковых keys не меняет внутреннюю identity позиции.




### Override другим EntityInstance




MVP должен позволять специалисту явно указать, какую унаследованную позицию он переопределяет, и выбрать для неё опубликованную версию другого EntityInstance.




Следовательно, один `EntityInstanceId` выбранного local item недостаточен для описания операции A → B. Override-intent обязан отдельно ссылаться на target inherited `EntityInstanceId`. Точное имя transport/domain поля (например, `OverridesEntityInstanceId` / `TargetEntityInstanceId`) остаётся TBD и не считается утверждённым этим документом.




Семантика:




- target отсутствует → local addition;
- target указан → explicit override унаследованной позиции;
- выбранный `EntityInstanceVersionId` может принадлежать другому EntityInstance;
- одна inherited position не может иметь два local overrides в одном assignment/release;
- конфликт не разрешается last-write-wins.




Если выбранный override использует другую `SchemaVersionId`, заменяется целая опубликованная версия вместе с её schema-version reference. Полевого merge нет.




## Exclusions




### Schema-wide exclusion




Exclusion по `SchemaId` удаляет из inherited layer весь контент этой схемы, включая элементы, которые появятся в Product позднее.




После schema-wide exclusion Variant может добавить local items той же схемы. Exclusion не является hard-ban для local layer.




### Instance-level exclusion




Exclusion по `EntityInstanceId` означает «не наследовать эту позицию из Product».




После такого exclusion Variant может локально добавить/override контент. Exclusion не запрещает local layer использовать тот же logical instance либо другой instance для нужного результата.




Authoritative references:




- schema exclusion → `SchemaId`;
- instance exclusion → `EntityInstanceId`.




String keys могут присутствовать в read models/snapshots для UI и диагностики, но не участвуют в matching.




## Local overrides и изменения Product




Удаление local override возвращает inherited item, если для этой позиции/схемы нет действующего exclusion.




Если Product меняет версию A/v2 → A/v3, существующий Variant override A→B либо A/v5 сохраняется до явного изменения специалистом.




Если Product полностью удаляет A, ранее настроенный local item не исчезает автоматически. Он остаётся local content Variant. Система не должна молча уничтожать локальное решение из-за изменения базы.




Variant может иметь local additions независимо от наличия совпадающего item в Product.




## Ограничения итогового состава




В одной итоговой ProductVariant revision одна logical position выбирает одну конкретную `EntityInstanceVersion`. Несколько версий одной позиции одновременно не создаются автоматически из-за наличия нескольких SchemaVersionId.




Если композиция приводит к неоднозначному результату или нескольким local overrides одной позиции, assignment/release считается невалидным. Last-write-wins не используется.




Отдельные правила коллизии, когда override A→B одновременно сталкивается с независимой inherited/local позицией B, уточняются следующим блоком; до ответа система не должна молча дедуплицировать такие элементы.




## Связь Product и ProductVariant assignments




`ProductVariantContentAssignment` явно относится к живому `ProductContentAssignment` своего Product и хранит только local Items + Exclusions/override intents.




Эта связь нужна authoring UI для preview и проверки наследования. Она не превращает mutable assignment в исторический источник опубликованной revision.




Publishing разделён:




1. `ProductContentRelease` snapshot-ит конкретное состояние ProductContentAssignment.
2. `ProductVariantContentRelease` snapshot-ит локальные отличия Variant.
3. `PublicationRevision` строится из явно выбранных immutable releases.
4. Изменение живого ProductContentAssignment после snapshot не меняет существующие releases/revisions.




Если модель допускает только один рабочий ProductContentAssignment на Product, физическая форма FK может быть упрощена реализацией, но semantic base relation должна оставаться однозначной.




## Алгоритм композиции




Базовое направление:




1. взять `Base ProductRelease.Items`;
2. применить schema exclusions по `SchemaId`;
3. применить instance exclusions по `EntityInstanceId`;
4. применить explicit local overrides к указанным inherited targets;
5. добавить local additions;
6. проверить uniqueness/conflicts;
7. сохранить immutable итоговый `PublicationRevision`.




Exclusions управляют inherited layer и не удаляют local Items. Local layer применяется после exclusions.




## Затронутые фичи




[F07](../features/F07-product-content-assignment.md) · [F08](../features/F08-variant-content-assignment.md) · [F09](../features/F09-content-releases.md) · [F10](../features/F10-publication-revisions.md)


## Решения блока 12: override target, collisions и immutable revision


### Результат override


Override `A → B` является редакторским намерением заменить унаследованную позицию A опубликованной версией B. В materialized `PublicationRevision` A отсутствует, а B присутствует под собственными `EntityInstanceId` и `EntityInstanceVersionId`. Runtime не получает B под identity A; target A может сохраняться только как provenance/audit metadata.


Отдельная пользовательская сущность `ContentSlot` не вводится. Для authoring достаточно explicit relation между выбранным local item и target inherited `EntityInstanceId`. Точное имя свойства остаётся техническим TBD.


### Граница схемы и коллизии


Explicit override разрешён только между EntityInstance одной логической `EntitySchema`. Cross-schema замена выражается exclusion + local addition.


Композиция считается конфликтной, если result B уже существует как самостоятельный inherited item, local addition либо result другого override. Один B не может заменять одновременно A и C. Один итоговый `EntityInstanceId` встречается в revision не более одного раза. Automatic deduplication и last-write-wins запрещены.


### Исчезновение и возврат target


Если A исчез из Product после настройки override, выбранный B и relation к A сохраняются. Assignment не становится невалидным только из-за отсутствия target. Если A позже возвращается, сохранённый override снова подавляет inherited A автоматически.


Новая Published версия B не выбирается автоматически: сохраняется явно выбранный `EntityInstanceVersionId`.


### Override и exclusions


Schema/instance exclusion и explicit override могут сосуществовать. Exclusion действует на inherited layer, затем применяется local layer. UI может показывать избыточность правил, но backend не меняет их смысл молча.


### VariantRelease и ProductRelease


`ProductVariantContentRelease` является immutable snapshot локального patch-набора Variant: additions, override intents и exclusions. Он не pin-ится как hard dependency к одному `ProductContentReleaseId`.


Один VariantRelease можно использовать при построении revision с другим ProductRelease того же Product. Каждая попытка полностью перевалидируется; несовместимая комбинация revision не создаёт.


Допускается optional provenance о ProductRelease, против которого patch готовили или проверяли, например `PreparedAgainstProductContentReleaseId`. Это review/audit metadata, не identity и не обязательная hard dependency. Точное поле — технический TBD.


### Materialized PublicationRevision


`PublicationRevision` хранит ссылки на конкретные Product/Variant releases и materialized immutable итоговый список выбранных `EntityInstanceVersionId` с необходимой provenance. Историческая revision читается из сохранённого результата, а не через повторный merge текущим алгоритмом.


Изменение алгоритма композиции применяется только к новым revisions. Старые revisions автоматически не пересчитываются.


## Статус Q07


Бизнес-семантика Q07 закрыта. Открыты только технические детали contracts/persistence/UI presentation; они не меняют принятый смысл.
## F07: допустимость рабочих references

Закрытые правила stable identity/override/exclusions выше сохраняются. Этот подраздел относится к допускаемым операциям Product working assignment, которые не были детализированы прежними ответами; он не вводит новую модель композиции. Связано с [business F07](../features/F07-product-content-assignment.md), [backend F07](../../technical/backend/features/F07-product-content-assignment.md), [frontend F07](../../technical/frontend/features/F07-product-content-assignment.md), [Q27](Q27-catalog-archiving.md), [Q36](Q36-entity-instance-lifecycle.md), [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f07-read-write-и-reference-контракт), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f07-initialization-и-конкурентные-guards).

Подтверждено: новый item/замена ссылается на явно выбранную допустимую Published EntityInstanceVersion; нельзя добавить Draft/Retired/Withdrawn либо unavailable content logical owner. Exact version не заменяется автоматически latest. Existing releases/revisions/history не меняются от Save/Remove рабочего item. Новая версия schema/content, rename Keys и изменения живой Product базы не переписывают stable reference identity.

**Ответ: принято.** Save других изменений разрешён, если ранее выбранная exact reference осталась неизменной, даже когда instance/schema archived, version retired/withdrawn или media dependency unavailable. Reference сохраняется с release-blocking diagnostic; no latest substitution/automatic cleanup. Сохранённая работа не обязана быть release-ready.

Новая позиция/замена exact version — new use и требует current eligibility. Нельзя заново выбрать недоступный result. Display reorder не новое использование. Удаление проблемной reference и замена на eligible result разрешены; inactive/archived owner допускает ограниченное maintenance удаления/такой замены, без additions/расширения состава. Это не permission bypass: scoped Open CatalogConflict продолжает блокировать ordinary affected writes, corrective workflow остаётся [Q25](Q25-catalog-conflicts.md).

Раздельная диагностика: SaveBlockers — intrinsic/new-use/access/concurrency; ReleaseBlockers — retained unavailable refs/selected-context collisions/dependencies; Warnings — допустимые состояния вроде retained absent target/deprecated content. UI показывает saved state, адресуемые причины, доступное исправление и release eligibility отдельно. Derived diagnostic basis/context не own assignment ETag; ошибка resolver/base read не успешный empty preview.

Exact reason codes/metadata/nullability/transport/source eligibility mapping — engineering [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f07-read-write-и-reference-контракт)/Q27; конкурентный reference-use/retention guard — [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f07-initialization-и-конкурентные-guards). Не вводятся каскадный cleanup/history mutation или F15 exemption.

## F08: сохранение patch и проверка live-базы

Связано с [business F08](../features/F08-variant-content-assignment.md), [backend F08](../../technical/backend/features/F08-variant-content-assignment.md), [frontend F08](../../technical/frontend/features/F08-variant-content-assignment.md), [F07 reference policy](#f07-допустимость-рабочих-references), [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f08-patch-preview-и-parent-route), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f08-patch-concurrency-и-preview-consistency).

Closed semantics выше не переоткрываются: override A→B одной logical Schema, B сохраняет собственную identity, target исчезает/возвращается без удаления intent; exact result version не auto-upgrade. Exclusions inherited-only, rules/local layer могут сосуществовать, uniqueness/no LWW/no silent dedup сохраняются. Коллизия блокирует release/revision, ordinary live base update не переписывает patch и не является автоматически catalog conflict.

**Ответ: принято.** Structurally valid patch сохраняется, если collision возникла только из current live Product base; diagnostic блокирует release в соответствующем выбранном context/revision, не Save работы. Intrinsic duplicate targets/results, duplicate result local+override, cross-schema override, invalid identities/untrusted target context блокируют Save.

NEW override выбирает target только из current inherited positions. Для B без такого target используется local addition. RETAINED override переживает отсутствие A в live base: B остаётся local result, при возвращении A правило снова применяется. Captured TargetSchemaId берётся из trusted server context, не client authority; отсутствие в live base не то же, что реально отсутствующая identity.

Все targets относятся к ORIGINAL inherited layer; результаты добавляются один раз, без cascade/array-order effect. A→B и B→C дают B,C при соблюдении остальных guards. A→B и B→A допустимы при final uniqueness/same-schema/eligibility и остальных правилах; отдельный запрет cycles не вводится. Сначала suppression исходных targets/exclusions, затем единичные results/local additions и final uniqueness; generated result не target input следующего override.

Retained-reference/new-use/limited inactive maintenance и три diagnostic категории применяются как F07. Open F15 всё ещё блокирует affected ordinary writes; отдельный corrective working patch repair теперь выбран Q25. Concrete target-evidence/basis/DTO/reasons — Q15, coherent read и short guards — Q20. Нормализованный Active/Archived и derived parent eligibility выбраны Q27; source mapping остаётся Q23.

Для F08 maintenance классифицируется по semantic effect на content positions, не количеству rule records. Inherited-only exclusion проблемной существующей позиции — removal; override этой позиции на eligible result — replacement при current inherited target. Добавление таких rule records не является разрешением additions/expansion новых content positions. Не связанные с removal/replacement новые rules при inactive/archived owner не допускаются; current basis/new-use/intrinsic guards и F15 blocker сохраняются.

## F09: live collision и release validation

Связано с [business F09](../features/F09-content-releases.md), [backend F09](../../technical/backend/features/F09-content-releases.md), [frontend F09](../../technical/frontend/features/F09-content-releases.md), [F08 Save question](#f08-сохранение-patch-и-проверка-live-базы), [F10](../features/F10-publication-revisions.md), [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f09-release-contracts-и-semantic-review), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f09-snapshot-creation-и-lifecycle-races).

Подтверждено: VariantRelease immutable patch-only, explicit A→B сохраняет B identity и captured target schema; optional PreparedAgainst provenance не hard dependency. Новый release повторно проверяет new-use exact content; [Q09](Q09-editing-and-deletion.md#entityinstanceversion) блокирует новый Release с Retired instance version. Existing history не пересчитывается/удаляется. Target отсутствует только в live base — не повод удалить intent/result.

**Ответ: принято.** Create VariantRelease требует EXPLICIT validation context: exact immutable ProductRelease либо EmptyBase для variant-only. Нет implicit current ActiveRelease/live assignment/fallback empty. Отсутствие выбора invalid; EmptyBase — осознанный режим. Patch-only artifact не превращается в итоговый состав.

Semantic review показывает composition выбранного context. Collision блокирует Create в этом context. Create повторно проверяет тот же saved patch/own source precondition и exact review context/basis, current eligibility необходимых source/result dependencies. Changed patch требует нового review; read failure не empty valid base. Retained unavailable result блокирует новый release, хотя ordinary Save такой работы допустим.

Live F08 collision относится только к captured live basis и не является blanket запретом открыть F09 review/select context. Другой exact immutable ProductRelease или explicit EmptyBase может дать допустимую composition. Confirm Create определяется выбранным review context и остальными current guards; live collision не переносится как безусловный global blocker.

Context сохраняется как provenance/evidence проверки, не hard dependency на ProductRelease. Release можно использовать F10 с другим ProductRelease того же Product только после полной validation новой комбинации. Успех F09 означает valid patch в зафиксированном review context, не совместимость со всеми будущими базами. No-context != EmptyBase; optional legacy PreparedAgainst alone не подтверждает выбор/context completeness.

Logical context union, review/basis transport и diagnostics SaveBlockers/ReleaseBlockers/Warnings материализуются в Q15/F09; own ETag не composite freshness, short source/context guards — Q20. F10 effective-vs-excluded dependency policy из Q09 остаётся отдельным вопросом и этим блоком не решается.
