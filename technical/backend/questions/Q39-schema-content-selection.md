# Q39. Технический контракт назначения схем

[Бизнесовое решение](../../../business/questions/Q39-schema-content-selection.md). Этот документ закрывает инженерные варианты Q39; нормативные модели, алгоритмы и экраны находятся в F07–F10. Это целевой контракт, а не описание уже работающих endpoints/migrations.

## API и дерево

Выбран whole-set контракт с отдельными schema selectors и inherited rules. Domain enum `SchemaInstanceSelectionMode` имеет AllExcept и SelectedOnly; JSON использует только строки `AllExcept`/`SelectedOnly`, numeric/unknown values отклоняются. Inherit — отсутствие собственного selector Variant, не третье stored Domain значение. Для preview смены режима отдельный transport enum `SchemaAssignmentMode` допускает Inherit, AllExcept, SelectedOnly; Product не принимает Inherit.

Сохранены owning routes F07/F08. Для обоих owner prefixes вводятся POST `preview`, `schema-instances/query`, `conversion-preview`; F09 review — POST `release-review`, Create — существующий POST `release`. Полные routes и поля однократно определены в [backend F07](../features/F07-product-content-assignment.md#36-контракты-schema-selection) и [F09](../features/F09-content-releases.md#36-контракт-проверки-schema-selection). Preview/conversion — read-only относительно бизнесового состояния; release-review создаёт только техническую квитанцию.

Tree query передаёт весь текущий generated candidate и expected preview basis, поэтому работает и для несохранённого выбора. Counts вычисляются для всего scope до paging. Смена basis →409, клиент не склеивает страницы разных состояний. Определены Page/PageSize, search/state filters, stable Key/ID order и отдельные признаки selected/blocked/no-published. Нельзя сериализовать весь complement SelectedOnly или делать N+1 по каждому instance.

## Проверка и срок действия

Выбрана сохранённая PostgreSQL `ReleaseReviewReceipt`, а не подписанный self-contained token. Она хранит проверенные inputs, exact resolution и basis, связана с actor/owner/assignment generation/context/previous baseline. HTTP получает случайный opaque ReviewToken; БД хранит SHA-256 token bytes. Ключи подписи, shared cache и token rotation protocol не нужны. Model/storage/retention определены в F09; это техническая запись, не новый moderation target или бизнесовый aggregate.

Lifetime задаёт обязательная положительная настройка `Publishing:Selections:ReviewLifetime`; ответ содержит ExpiresAt. Нормативное число минут без sizing evidence не назначается. Истечение никогда не разрешает повторный Create без проверки. Отдельные execution receipts ClientOperationId сохраняют прежнюю replay семантику: confirmed result возвращается до проверки истёкшего review. ReviewToken может использоваться для намеренного нового Create с другим ClientOperationId, пока basis/lifetime/actor допустимы; это не deduplication по content.

Preview basis и resolution evidence используют один внутренний профиль `cm-publishing-selection-basis/1` с domain separator Kind. Это не ContentHash/ETag/SDK profile. Точный состав, canonical encoding и comparison определены в F09. Create под общим MVP gate Q20 заново разрешает scopes и сравнивает evidence; receipt не является разрешением обойти current guards.

## Конкуренция

Применяется существующий `ICmConsistencyGate` на том же owner connection/local transaction до fresh reads и row locks. Publish/Retire/Withdraw/Archive и создание включаемых Published owners участвуют в том же protocol. Между authoritative resolution и release commit phantom insert не допускается. Foreign adapter использует fresh primary read без второго gate и без enlistment в Publishing transaction. Captured previous ReleaseId остаётся прежним даже при появлении следующего release; current latest baseline не подставляется.

Release review/technical receipt persistence размещается в Application command handler; component queries остаются в EntityFramework.Persistence. Query handler не превращается в hidden writer. Preview/Tree/Conversion не сохраняют receipt и не меняют assignment Version.

## Хранение и миграция

Выбраны нормализованные owned selector/include/exclude tables и composite owner+SchemaId keys, без surrogate identity дочернего правила. Frozen release metadata отделена от authoritative exact Items; revision хранит конечные refs и provenance. Полные PostgreSQL columns/checks/indexes/mappings определены в [F07](../features/F07-product-content-assignment.md#61-schema-selection-storage), [F08](../features/F08-variant-content-assignment.md#schema-selections-по-физическим-слоям), [F09](../features/F09-content-releases.md#schema-selection-storage-и-переход-формата).

Миграция additive и выполняется после dry-run/preflight. Однозначные Product exact assignments преобразуются в SelectedOnly без смены IDs. Mixed targets одной logical schema и неоднозначные legacy Variant intents не угадываются: preflight выдаёт machine-readable report и останавливает upgrade до явной корректировки working data. Нельзя автоматически выбрать latest, удалить лишние refs или превратить local addition в whole-schema replacement.

Существующие release/revision payloads/hash не переписываются. Legacy release format читается для history; для нового F10 Create требуются новые schema-selection releases, созданные после migration. Existing materialized revisions сохраняют delivery/lifecycle/rollback semantics по F11/F13; это не глобальная Retire старой истории. Новые revision source pickers объясняют необходимость подготовить новый release. Mixed-format composition не включается как скрытая fallback ветка.

## Смена режима

Conversion preview возвращает полный generated candidate, diff и diagnostics. Подтверждение меняет локальный request; только обычный conditional Save пишет assignment. AllExcept→SelectedOnly материализует полный resolved набор по target, включая blocker rows с diagnostics, а не только загруженную страницу. SelectedOnly→AllExcept начинает с явного пустого exclusions, показывает expansion и будущую динамику. Inherit→Own подавляет всю logical schema; Own→Inherit удаляет selector. Competing legacy rules показываются как removals, не уничтожаются молча. Exact flow и bUnit acceptance находятся в frontend F07/F08.

## Guardrails и завершённость решения

`PublishingSelectionOptions` определены в F07: обязательные positive bounds для selectors/explicit IDs/resolution rows/review bytes/page sizes и lifetime/cleanup. Значения задаёт deployment после representative fixtures; отсутствие/некорректность выявляется Host validation. Превышение отклоняет весь результат до записи; partial tree/release не выдаётся как полный. Это операционные параметры, не оставленный выбор алгоритма.

В Q39 не осталось выбора carrier/storage/conversion/migration. Общие незакрытые задачи Q10/Q15/Q20/Q38 за пределами schema-selection delta этим не объявляются решёнными. Тестирование документа не доказывает implementation, обновлённый OpenAPI, migration run или совместимость Player.
