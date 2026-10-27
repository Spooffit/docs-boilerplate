# F08. Локальные отличия ProductVariant

## Цель

Позволить конкретному ProductVariant определить локальные отличия относительно общего Product content. ProductVariantContentAssignment хранит mutable local patch, а не полную копию Product assignment.

Save рабочего patch отделён от готовности release/revision и не является клиентской публикацией.

## Виды локальных правил и identity

Variant может содержать local addition, explicit override унаследованной позиции, exclusion конкретного inherited EntityInstance и exclusion всех inherited items конкретной EntitySchema.

Matching выполняется по стабильным IDs: позиция — EntityInstanceId, schema exclusion — SchemaId, instance exclusion — EntityInstanceId. Mutable SchemaKey/EntityInstanceKey не являются merge-authority.

## Composition preview

### Наследование и собственный выбор схемы

Variant показывает дерево схем Product и собственных назначений по [F07](F07-product-content-assignment.md#состав-assignment). Для схемы видны режим «Наследовать» либо собственные «Все экземпляры», «Все, кроме выбранных», «Только выбранные» с exact SchemaVersion. Собственный selector заменяет весь inherited состав logical SchemaId: из 20 inherited и двух SelectedOnly получаются ровно два. Это явная schema replacement, не добавление двух позиций поверх двадцати.

При live preview AllExcept разрешается для текущего просмотра; F09 фиксирует его результат в local release. F10 применяет сохранённый replacement-mask к выбранному ProductRelease без чтения новых экземпляров. Удаление selector возвращает наследование с оставшимися явными inherited правилами.

Inherited schema/instance exclusions сохраняются в режиме наследования. Исключения собственного AllExcept имеют отдельный scope и не объединяются с inherited exclusions. Собственный selector и additions/overrides/inherited exclusions той же схемы не могут конкурировать в одном candidate: смена режима требует явно показать заменяемые правила, сервер отклоняет неоднозначность. Старый local addition не становится replacement всей схемы через автоматическую миграцию. [Q39](../questions/Q39-schema-content-selection.md) раскрывает границы.

Алгоритм ниже описывает inherited exclusions/overrides вне собственных schema replacements. Сначала из исходного inherited layer удаляются logical схемы с собственным selector; затем применяются оставшиеся inherited правила и добавляются resolved local schema selections. Итоговая uniqueness/eligibility проверяется для всего результата.

Рабочий preview использует текущий Product assignment для authoring. Release review отдельно выбирает immutable контекст по F09. Алгоритм композиции:

1. Получить исходный inherited layer и его identity/schema evidence.
2. Применить schema exclusions и instance exclusions только к inherited layer.
3. Подавить inherited targets explicit overrides; каждый target относится к исходному inherited layer.
4. Добавить exact results каждого override один раз, затем local additions.
5. Проверить уникальность итоговых EntityInstanceId и остальные guards.

Exclusions не запрещают local layer: после exclusion Variant может явно добавить item той же Schema. Exclusion и override могут сосуществовать; система не нормализует правила молча. Удаление override возвращает inherited item, если он существует и не исключён.

Override results не становятся targets последующих overrides. Каскада и зависимости от порядка массива нет. Для исходных A и B правила `A → B`, `B → C` дают B и C, если итоговый состав и остальные guards допустимы. `A → B`, `B → A` разрешены при уникальном результате и выполнении остальных ограничений; отдельный запрет циклов не вводится.

## Создание и сохранение override

Override `A → B` означает: inherited EntityInstance A заменяется exact Published EntityInstanceVersion B.

- новый override выбирает target только среди позиций текущего Product assignment; schema/instance exclusion этой позиции не лишает её identity исходного inherited target;
- A и B принадлежат одной логической EntitySchema; смена SchemaVersion внутри неё допустима без полевого merge;
- target identity и schema evidence подтверждаются системой; клиент не может произвольно задать доверенный target context;
- результат содержит настоящий EntityInstanceId B, а не B под identity A;
- cross-schema замена оформляется exclusion + local addition;
- новый B без текущего inherited target оформляется local addition, а не новым absent-target override;
- отдельная сущность ContentSlot не вводится.

Если ранее сохранённый target A исчез только из Product assignment, override сохраняется: B остаётся local content, relation к A не удаляется. Это warning, а не само по себе Save/ReleaseBlocker. Если A возвращается, сохранённый override снова подавляет inherited A. Отсутствие в базе не равно invalid/unavailable identity в ContentModeling; необходимый target context должен оставаться доверенным. Новая версия B не выбирается автоматически.

## Save и коллизии

Intrinsic ошибки patch блокируют Save: несколько overrides одного target, повторный result одного EntityInstance, один result одновременно в addition и override, cross-schema override, invalid identities или недоверенный необходимый target context. Порядок правил не исправляет ошибку; last-write-wins и silent dedup запрещены.

Структурно корректный patch можно сохранить, если коллизия возникает только при соединении с текущим Product assignment. Например, override A→B и независимый inherited B дают duplicate result: Save patch разрешён с диагностикой, а release/revision в этом контексте заблокированы. Если B также подавлен своим override или exclusion, проверяется фактический итоговый набор, а не одно совпадение target/result IDs.

Изменение Product assignment пересчитывает preview, но не переписывает local patch и само по себе не создаёт CatalogConflict. Move в другой Product с несовместимыми локальными зависимостями относится к F15.

## Доступность и сопровождение

Для additions, нового override result и замены exact result version применяется полная текущая new-use eligibility: явно выбранная Published version и допустимые owner/dependencies. Draft/Retired/Withdrawn/unavailable reference добавить нельзя.

Неизменную ранее сохранённую exact result reference можно оставить при Save других изменений после Retire/Withdrawal/Archive/unavailability. Она сохраняет IDs и блокирует новый release до удаления или замены допустимой reference. Изменение отображения/порядка не считается новым использованием; новая позиция или замена считаются.

Для Archived Variant либо Variant с Archived parent разрешено ограниченное сопровождение: сохранить неизменные exact refs, удалить inherited/local позиции и заменить проблемные references допустимыми. Допустимость определяется изменением контентного состава, а не количеством записей patch: inherited-only exclusion, убирающий позицию, является удалением; override текущей проблемной inherited позиции допустимым result является заменой. Такие операции могут добавить rule record, но не расширяют набор контентных позиций. Независимые additions, новые позиции и иное расширение состава не разрешены; все new-use, intrinsic и context/scope guards сохраняются. Собственный lifecycle Variant не переписывается вслед за parent; source lifecycle и restore определяет F06.

Открытый блокирующий F15 conflict запрещает affected ordinary writes, включая удаление/замену. Отдельный Publishing corrective repair F15 допускает только исправление working Variant patch для exact newest pending target: удаление конфликтующего override/addition, eligible замену проблемного result и inherited-only exclusions, убирающие blocker, без расширения состава. Это не исключение обычного Save. Preview различает applied/prospective/repaired; ConflictId, newest SourceVersion, captured basis и own patch If-Match обязательны. Source/patch/basis change отклоняет stale repair. До Resolve обычный workspace использует applied parent.

## Диагностика

UI различает сохранённое рабочее состояние, diagnostics и CanCreateRelease. Диагностика делится на:

- **SaveBlockers** — intrinsic patch errors, недопустимое новое использование, запрет операции для owner, доступ/конкуренция и blocking conflict;
- **ReleaseBlockers** — retained unavailable references/dependencies и collisions выбранного контекста;
- **Warnings** — absent retained target или Deprecated content, если текущая policy допускает использование.

Live collision не означает intrinsic повреждение patch и не является универсальным запретом его использования с любой базой. Возможность release оценивается по явно выбранному контексту F09; F10 повторяет полную validation actually selected releases.

Server diagnostic basis включает зависимости отдельно от own assignment ETag. Ошибка чтения базы/metadata не означает пустой валидный preview; последняя успешная диагностика не становится текущей authority после такой ошибки.

## Результат

Сохранён mutable local patch ProductVariant; его структурная корректность, live preview и готовность release в явном контексте видны отдельно.

## Acceptance

- собственный SelectedOnly из двух экземпляров заменяет inherited двадцать той же logical schema, а не добавляет к ним ещё две позиции;
- AllExcept exclusions собственного selector и inherited exclusions показаны и сохраняются раздельно;
- mode switch не удаляет competing legacy rules молча; удаление selector возвращает допустимое наследование.

- inherited/local/override/excluded состояния различимы; переименование keys не меняет matching;
- новый A→B выбирает A из текущей базы и same-schema допустимую exact B; без A новый B оформляется addition;
- сохранённый A→B переживает исчезновение A с warning, оставляет B и снова применяется при возвращении A;
- new absent-target override, cross-schema, duplicate targets/results, addition+override одного B и недоверенный target context блокируют Save;
- patch A→B при live базе A+B можно сохранить; collision блокирует release/revision в этой базе и не удаляет patch;
- исходные A+B с A→B, B→C дают B+C; A→B, B→A дают B+A при уникальности и остальных guards независимо от порядка rules;
- unchanged unavailable B сохраняется при изменении других правил с ReleaseBlocker; новый/заменённый unavailable B отклоняется;
- Archived Variant либо Variant с Archived parent допускает неизменные refs, удаление/замену проблемной reference, в том числе новый exclusion для удаления inherited позиции или override текущей проблемной позиции допустимым result; independent addition/расширение состава запрещены, blocking conflict запрещает ordinary maintenance;
- отдельный F15 repair устраняет blocker без расширения и не закрывает conflict/не применяет pending parent;
- exclusions inherited-only; local addition той же Schema остаётся; no LWW/no silent dedup;
- failed basis read не показывает пустой валидный preview; successful Save не означает готовность release или клиентскую публикацию.

## Связи

[F07](F07-product-content-assignment.md) · [F09](F09-content-releases.md) · [F10](F10-publication-revisions.md) · [F15](F15-catalog-conflicts.md) · [Q07](../questions/Q07-exclusions-and-local-content.md) · [Q27](../questions/Q27-catalog-archiving.md) · [Q25](../questions/Q25-catalog-conflicts.md).
