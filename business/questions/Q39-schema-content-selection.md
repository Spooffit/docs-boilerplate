# Q39. Назначение схем и выбор экземпляров

## Принятое поведение

Основная единица выбора в Product/Variant assignment — EntitySchema с явно выбранной exact Published SchemaVersion. Экземпляры показываются дочерними узлами и используются для точечной настройки. Это назначение контента, а не новый эксклюзивный owner схемы: Product/Variant ownership EntitySchema этим решением не вводится.

| Режим в UI | Смысл | Что будет с новым Published экземпляром |
| --- | --- | --- |
| Все экземпляры | Весь состав выбранной версии структуры | Учитывается при подготовке следующего release |
| Все, кроме выбранных | Тот же состав минус stable EntityInstanceId из списка исключений | Учитывается, если его ID не исключён |
| Только выбранные | Явный набор exact Published EntityInstanceVersion по выбранной структуре | Не добавляется без изменения выбора |

Технически первые два варианта имеют одну политику AllExcept: пустой список исключений означает «Все экземпляры». SelectedOnly хранит включения. Это имена целевых моделей, не существующие API enum/wire values. Не следует хранить перечисление всех исключённых объектов для SelectedOnly или всех включённых для AllExcept: смысл правил отличается при появлении новых экземпляров.

Для AllExcept при подготовке release рассматриваются logical instances, имеющие Published history в выбранной SchemaVersion. Для каждого выбирается максимальный Published VersionNumber именно внутри этого target; Draft не участвует. Затем выполняются обычные eligibility checks. Нельзя молча подставить более старую версию, если максимальная Retired/unavailable, или убрать проблемный включаемый экземпляр из результата. Явно исключённый ID не является effective dependency. Draft-only экземпляры видны в дереве как не входящие в опубликованный состав.

Сохранённый assignment хранит правило; его read preview может меняться при новых публикациях. Release сохраняет проверенный результат как exact immutable references вместе с source selection intent для review/audit. Выбранная SchemaVersion никогда не меняется вслед за новой публикацией структуры. Изменившийся между review и Create состав требует нового review/подтверждения. Existing releases/revisions не пересчитываются; F10 не исполняет AllExcept повторно по live ContentModeling.

SelectedOnly продолжает существующий явный exact-version выбор. Новая версия выбранного экземпляра не заменяет pin автоматически. Пустой SelectedOnly либо AllExcept с пустым результатом сохраняет назначение схемы с нулём выбранных экземпляров, а не удаляет правило. Существующие empty-source/nonempty-final правила F09/F10 сохраняются: само назначение пустой схемы не делает пустую клиентскую revision допустимой.

## ProductVariant и исключения

По умолчанию Variant наследует Product. Его собственная schema selection явно **заменяет весь inherited состав logical SchemaId**, включая экземпляры других SchemaVersion этой схемы. Пример: inherited 20, own SelectedOnly из двух → final два. При композиции с другим exact ProductRelease replacement-mask действует на ту же logical schema, но локальный набор уже заморожен своим VariantRelease.

Режим наследования со schema/instance exclusions остаётся доступен отдельно. Старые inherited exclusions действуют только на inherited layer. Исключения AllExcept относятся к собственному schema selector и не должны попадать в ту же коллекцию transport/domain rules. Удаление собственного selector возвращает наследование, если его не запрещают явно сохранённые inherited exclusions.

Вне schema replacement сохраняются F08 additions/explicit overrides и их stable identity semantics. Новый selector и старые additions/overrides/inherited exclusions одной схемы не должны конкурировать неявно: UI показывает необходимые удаления/перенос при смене режима, сервер отклоняет неоднозначный candidate. Имеющиеся historical patches не конвертируются в schema replacement автоматически: legacy local addition и replacement всей схемы имеют разный смысл.

## Раскрытие в фичах

- [F07 business](../features/F07-product-content-assignment.md), [backend](../../technical/backend/features/F07-product-content-assignment.md), [frontend](../../technical/frontend/features/F07-product-content-assignment.md): schema rules, exact-version scope, сохранение и дерево.
- [F08 business](../features/F08-variant-content-assignment.md), [backend](../../technical/backend/features/F08-variant-content-assignment.md), [frontend](../../technical/frontend/features/F08-variant-content-assignment.md): наследование, schema replacement и локальные исключения.
- [F09 business](../features/F09-content-releases.md), [backend](../../technical/backend/features/F09-content-releases.md), [frontend](../../technical/frontend/features/F09-content-releases.md): captured resolution, review и immutable release.
- [F10 business](../features/F10-publication-revisions.md), [backend](../../technical/backend/features/F10-publication-revisions.md), [frontend](../../technical/frontend/features/F10-publication-revisions.md): композиция frozen результатов без live expansion.

## Ответы по инженерной детализации

| Граница | Требование | Принятое решение |
| --- | --- | --- |
| API | Whole-set selectors, отдельные inherited rules и own preconditions | String enums, конкретные DTO/POST preview/tree/conversion/release-review и error codes — backend F07/F09 |
| Review basis | Связь с selector/ETag, exact target и publication universe | PostgreSQL review receipt + opaque random token, internal evidence hash, configured positive lifetime и явный ExpiresAt; не assignment ETag |
| Concurrency | Fresh resolution защищён до commit | Существующий общий transaction gate Q20, повторная проверка evidence и отдельные phantom/winner concurrency tests |
| Persistence | Правила, pins/exclusions и frozen materialization | Owned relational tables с composite constraints; additive preflight migration; ambiguous working data требует явного решения, historical bytes/hash не меняются |
| Conversion | Режим меняется с видимым diff | Read-only conversion-preview возвращает generated candidate, полный resolved diff и diagnostics; Confirm меняет local request, Save защищён If-Match |

Полная детализация и ссылки на owning модели/DDL/экраны — [технический Q39](../../technical/backend/questions/Q39-schema-content-selection.md). Вопросы этой таблицы закрыты как проектные решения; их реализация и проверки ещё должны быть выполнены.

Решение расширяет целевой Publishing MVP; самостоятельный Demo, SDK wire, автоматическая генерация поведения клиента и расширенная F14 compatibility не меняются. Текущий код с AssignmentItemInput(EntityInstanceVersionId) ещё не реализует schema rules; наличие документа не означает готовые migration/API/UI.
