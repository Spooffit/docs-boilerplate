# Q36. EntityInstance: идентичность, версии, черновики и снятие с выдачи


## Контекст и уже принятые границы


**Статус:** Demo lifecycle уточнён: key immutable и задаётся явно; Draft/Published source и cross-schema-version Copy разрешены по FieldId; validation/Accept, concurrent Publish разных targets, последний Draft, delete-impact, Withdrawal return и deprecated options определены ниже. Полная retention/GC policy и отдельные MVP lifecycle детали остаются за пределами этих ответов.


Этот вопрос детализирует F04 после закрытия базовой семантики SchemaField в Q34.


Уже принято:


- `EntityInstance` — логический экземпляр контента, а его данные живут в `EntityInstanceVersion`;
- одна логическая сущность может иметь версии данных по разным `SchemaVersionId`;
- Draft экземпляра можно готовить по Draft-версии схемы, но публиковать/доставлять его до публикации структуры нельзя;
- новую версию данных можно готовить на основе предыдущей либо с чистого листа;
- Published version не переписывается;
- Delivery Demo для каждой запрошенной точной версии схемы выбирает последнюю Published версию экземпляра именно в этой группе;
- удаление Draft и прекращение клиентской выдачи — разные действия;
- current code с одним Draft и lifetime-привязкой instance к одной SchemaVersion является current-state evidence, а не target.


## Блок 10. EntityInstance lifecycle


### 1. Что означает `EntityInstance.Key`?


Нужно определить, является ли key смысловой стабильной идентичностью экземпляра для tooling/runtime либо только удобным authoring-полем.


Кто задаёт key: специалист вручную, CM предлагает его из Name или key вообще не нужен пользователю при наличии `EntityInstanceId`?


Если сохраняем key, отдельно требуется определить Demo/MVP формат (`camelCase`/`kebab-case`) и его использование внешними клиентами.


**Ответ:** `EntityInstanceKey` сохраняется как стабильный человекочитаемый key логического экземпляра для runtime/tooling. `EntityInstanceId` остаётся технической identity. Один из целевых SDK-сценариев — lookup по key в контексте конкретной модели/схемы, например `TryGetByKey<Butterfly>("machaon", ...)`, без необходимости тащить GUID в код приложения.


Историческая причина с merge Product/ProductVariant остаётся только частично актуальной: key полезен как логический alias/slot, но после решения о возможности rename `EntitySchema.Key` внутренний Publishing merge нельзя навсегда привязывать только к строковой паре `SchemaKey + EntityInstanceKey`. Точная authority override/matching остаётся Q07/F07/F08. Demo Key вводится явно; в MVP UI может предложить редактируемое значение из Name до создания, как уточнено в блоке 19. Для Demo принят тот же формат, что F01: trim без изменения регистра, `^[a-z][a-zA-Z0-9]*$`, длина 1–50, Ordinal case-sensitive и уникальность внутри логической схемы. Ключ после создания не изменяется; точный carrier описан в Q15.
### 2. В какой области уникален `EntityInstance.Key`?


Кандидаты:


- глобально во всём CM;
- внутри одной `EntitySchema`;
- внутри конкретной `SchemaVersion`;
- уникальность key вообще не является бизнес-инвариантом, identity — только ID.


Например, могут ли одновременно существовать `dog: rex` и `butterfly: rex`?


**Ответ:** `EntityInstanceKey` уникален **внутри одной логической `EntitySchema`**. Поэтому `dog/rex` и `butterfly/rex` допустимы, а два разных `EntityInstance` с key `rex` внутри одной EntitySchema — нет. SchemaVersion не создаёт новую область уникальности.
### 3. Можно ли менять `EntityInstance.Key` после первой публикации?


Если key используется runtime/tooling contract, rename может ломать ссылки и client-side lookup. Если runtime использует только `EntityInstanceId`, key может оставаться authoring metadata.


Нужно определить неизменяемость и последствия rename, не выводить их из текущего кода.


**Ответ:** Key immutable после создания логического `EntityInstance`. Исправление формы до первого commit не является rename сохранённого объекта; после создания другой key означает другой логический экземпляр.
### 4. Что означает `EntityInstance.Name`?


Рабочее предложение: `Name` — authoring label специалиста, как `SchemaField.Name`, и не должен автоматически попадать в runtime entity payload. Если клиенту нужно показываемое название, оно моделируется обычным полем схемы (`name`, `title` и т.п.).


Нужно подтвердить либо определить runtime-ценность Name отдельно.


**Ответ:** `EntityInstance.Name` — mutable authoring label для специалиста. Он не является runtime content field и не должен автоматически попадать в Entity payload клиента. Отображаемое приложением имя моделируется обычным SchemaField (`name`, `title` и т.п.).
### 5. Сколько Draft-версий одного EntityInstance может существовать одновременно?


Current implementation ограничивает экземпляр одним Draft, но target уже допускает версии одного instance по разным SchemaVersionId.


Нужно выбрать:


- один Draft на весь EntityInstance;
- один Draft на каждую SchemaVersionId;
- несколько независимых Draft без такого ограничения, как у EntitySchema.


От этого зависят параллельная работа, UI выбора Draft и concurrency.


**Ответ:** Одновременно разрешено несколько независимых Draft-версий одного `EntityInstance`, без ограничения «один Draft на instance» и без ограничения «один Draft на SchemaVersionId». Каждый Draft имеет собственный `EntityInstanceVersionId`.
### 6. Когда EntityInstanceVersion получает бизнес-номер?


Для EntitySchema принято: Draft имеет только VersionId, номер назначается при publish.


Нужно определить, зеркалится ли это для данных экземпляра:


- Draft `EntityInstanceVersion` также без бизнес-номера до публикации;
- либо номер экземпляра резервируется раньше.


Рабочее предложение для единообразия — номер только при publish.


**Ответ:** Как и у схемы, Draft `EntityInstanceVersion` не имеет бизнес-номера. Следующий номер назначается системой только при успешной публикации конкретного Draft. Удалённый Draft номер не резервирует.
### 7. Как упорядочиваются публикации версий одного EntityInstance при разных SchemaVersionId?


У одного instance могут готовиться данные одновременно по S1 и S2.


Нужно подтвердить, что published sequence **одна на весь EntityInstance**, независимо от SchemaVersionId: whichever Draft публикуется следующим, получает следующий номер. SchemaVersionId остаётся измерением структуры, но не создаёт отдельную ветку нумерации.


Либо нужны независимые sequences по каждой SchemaVersionId.


**Ответ:** Published sequence одна на весь логический `EntityInstance`, независимо от `SchemaVersionId`. Какой Draft фактически опубликован следующим, тот получает следующий номер. `SchemaVersionId` определяет контракт данных, но не отдельную ветку нумерации.
### 8. Какие версии разрешено использовать как основу нового Draft instance?


Уже приняты режимы «на основе предыдущей» и «с чистого листа». Нужно уточнить источник:


- только Published version;
- любой существующий Draft/Published version;
- только версия с тем же SchemaVersionId;
- при смене SchemaVersionId копирование запрещено без отдельной миграции.


Автоматическое преобразование данных между разными структурами этим вопросом не вводится.


**Ответ:** Новую Draft-версию можно создать с чистого листа либо на основе другого Draft/Published того же EntityInstance. Source и target могут иметь разные Draft/Published SchemaVersionId той же EntitySchema; значения сопоставляются только по stable FieldId с сохранением orphan и без автоматической миграции. Полный ответ блока 19 ниже. Для Withdrawn source/target historical read, Copy и Save recovery work разрешены с предупреждением; Publish к Withdrawn target и runtime выдача запрещены.
### 9. Что происходит с сохранённым Draft instance, если его Draft schema изменили?


Так как Draft schema mutable до публикации, сохранённый instance Draft может перестать ей соответствовать.


Рабочее направление:


- данные instance не мигрируются и не удаляются автоматически;
- при следующем чтении/сохранении/publish выполняется повторная validation против актуального snapshot той же SchemaVersionId;
- несовместимые/orphan values сохраняются как незавершённая работа до явного исправления специалистом, но publish блокируется.


Нужно определить, должны ли orphan values вообще сохраняться либо удаляться при изменении формы.


**Ответ:** Принято рабочее направление: schema Draft не мигрирует и не очищает dependent instance Draft автоматически. При чтении/сохранении/publish данные повторно валидируются против актуального содержимого той же SchemaVersionId. Несовместимые/orphan values сохраняются как незавершённая работа, чтобы специалист мог их увидеть и исправить; Publish блокируется до валидного состояния.
### 10. Можно ли удалить Draft schema, если на неё ссылаются Draft instances?


Рабочее предложение: не делать cascade-delete. Удаление Draft schema блокируется, UI показывает зависимые instance drafts; специалист сначала удаляет/переводит их явно.


Альтернатива — явное подтверждение каскадного удаления всей зависимой незавершённой работы.


Published history в любом случае не затрагивается.


**Ответ:** Для Draft schema разрешён **явный cascade delete** зависимой незавершённой работы. Перед выполнением UI показывает modal с точным количеством Draft instance data, которое будет удалено; destructive confirm заблокирован первые **5 секунд** и только затем становится доступен.


Это не неявный database cascade «потому что FK так настроен»: серверная операция должна повторно определить exact допустимый cascade-set и сравнить его с подтверждённым набором. Изменение состава, даже при прежнем count, отклоняет delete; UI показывает новый impact и повторяет 5-second confirmation. Только подтверждённый актуальный набор удаляется согласованно: Draft schema + зависимые Draft instance versions. Published history этим сценарием не затрагивается; Draft instance по Draft schema по принятому инварианту не может быть Published.
### 11. Что именно означает «убрать EntityInstance с клиентской выдачи» в Demo?


F13 Demo без Product Publishing выбирает опубликованные экземпляры напрямую, поэтому нужна явная семантика прекращения выдачи.


Нужно определить область:


- весь логический EntityInstance для всех SchemaVersionId;
- только представление instance по конкретной SchemaVersionId;
- оба действия нужны отдельно.


После изменения следующая sync должна вернуть `Remove` для соответствующих локальных представлений, но исторические Published versions на сервере не уничтожаются.


**Ответ:** В Demo нужны отдельные scopes снятия с прямой F13-выдачи:


- целая `EntitySchema` — убрать из desired state все её представления/экземпляры;
- конкретная `EntitySchemaVersion` — убрать все representation, созданные по этой структуре;
- конкретный логический `EntityInstance` — убрать его представления из выдачи.


Следующая синхронизация возвращает `Remove` для соответствующих локально установленных representation. Это действие не обязано физически удалять объект из БД. Отдельный scope `EntityInstance + SchemaVersionId` пока не вводится без дополнительной необходимости. Новый Withdraw logical instance требует retained Published content; Draft-only/пустой owner не снимается с выдачи. Возврат при Published history выполняется отдельным явным действием с проверкой допустимых Published representations и effective запретов всех scopes. Для ранее возникшего Withdrawn owner без retained Published разрешено recovery: снять только own запрет с concurrency check без Publish Draft/schema/media reads. Новый Publish Withdrawal автоматически не очищает.
### 12. Как соотносятся Archive, hard delete и возврат в работу?


Рабочее направление:


- Draft version можно удалить как незавершённую работу;
- логический EntityInstance, который ни разу не публиковался и нигде не используется, потенциально можно hard-delete;
- после появления Published history физическое удаление логического instance запрещается, используется Archive;
- Archive скрывает объект из обычного выбора/новых назначений, но не разрушает старые publication snapshots;
- возможность Restore/Unarchive требует отдельного явного действия.


Нужно подтвердить это и решить, влияет ли Archive автоматически на прямую Demo-выдачу F13 либо «archive» и «withdraw from delivery» являются разными состояниями.


**Ответ:** Soft-delete, Archive и Restore **не входят в Demo**. Если в Demo выполняется разрешённое delete-действие, это физическое удаление из БД; отдельная архивная запись ради демонстрации не создаётся. Точная dependency/cascade matrix для остальных объектов остаётся Q09.


Для MVP Archive нужен: архивированный schema/instance скрывается из обычного интерфейса специалиста и снимается с новой клиентской выдачи, при этом исторические publication snapshots/refs не должны разрушаться. Restore/Unarchive и окончательные правила hard delete проектируются уже для MVP.
### 13. Можно ли архивировать/скрывать отдельную Published EntityInstanceVersion?


Current code имеет version-level archive operations, но target-смысл ещё не подтверждён.


Варианты:


- Published version immutable и навсегда остаётся доступна как history, а скрывается только логический instance / будущие selections;
- version можно `Retire/Archive` для новых выборов, не разрушая уже закреплённые publication revisions;
- отдельный version-level lifecycle вообще не нужен.


**Ответ:** Отдельный lifecycle `Retire/Archive` для одной Published `EntityInstanceVersion` в Demo не вводится. Задача прекращения новой выдачи решается scopes из пункта 11 на уровне schema/schema-version/instance. Published version остаётся immutable, пока существует; самостоятельный version-level retire state не нужен без отдельного будущего сценария.
## Current-state evidence


По snapshot 01.10.2026 current `EntityInstance` имеет `Revision`, `SchemaId`, `Key`, `Name`, Versions; разрешает один Draft и заранее назначает InstanceVersionNumber. Current code также фиксирует один SchemaVersionId на lifetime instance и требует Published schema при создании/изменении. Эти правила расходятся с уже принятым target и не используются как ответы этого блока.


## Применение


Решения блока 10 применяются в F04 business/backend/frontend, Q09, F13/Q30, Q20, F19 и milestones. `EntityInstanceKey` сохраняется для runtime/tooling lookup; точный Publishing override identity остаётся Q07/F07/F08, поскольку mutable `EntitySchema.Key` нельзя бездумно делать вечной внутренней merge-identity.


## Решение блока 19: source нового EntityInstance Draft


Это решение **заменяет прежнюю открытую часть пункта 8** про Published source и копирование между разными SchemaVersion.


Новый Draft EntityInstanceVersion можно создать:


- с чистого листа;
- на основе другого Draft того же EntityInstance;
- на основе Published version того же EntityInstance.


При копировании можно использовать исходную SchemaVersion либо выбрать другую Draft/Published SchemaVersion той же EntitySchema.


Перенос выполняется только по stable SchemaFieldId. Совпавшие совместимые поля получают исходное значение. Source values, для которых нет соответствующего target field, сохраняются как orphan authoring data. Новые target fields остаются незаполненными.


Сопоставление по Name, Key, визуальной позиции или эвристике не используется. Orphan values не переносятся автоматически в другое поле.


Draft может содержать orphan/incomplete authoring data в пределах Draft validation, но Publish блокируется до полного соответствия target Published SchemaVersion.


Для EntityInstanceKey уточнена только UX-граница: в Demo Key вводится явно; в MVP UI может предложить редактируемое значение на основе Name до создания. После создания Key остаётся immutable.


Отдельные Title/ChangeComment для EntityInstance Draft не вводятся: решение о такой metadata принято только для Schema Draft.

## F04: оставшиеся продуктовые границы

Связано с [F04](../features/F04-entity-instances.md), [backend F04](../../technical/backend/features/F04-entity-instances.md), [Q15 F04](../../technical/backend/questions/Q15-api-and-source-code.md#f04-детализация-целевого-контракта). Этот блок не переоткрывает multiple Draft, immutable target/Key, copy Draft/Published по FieldId, сохранение orphan, глобальную нумерацию и baseline по target. Бизнесовая F04 содержит актуальную lifecycle-модель; прежние ответы про исключённый Demo Retire/Archive не запрещают согласованный MVP lifecycle.

| Вопрос | Принятая граница | Что уточняется |
| --- | --- | --- |
| Одновременный Publish по разным target | Publish другой SchemaVersion не делает baseline stale | **Ответ:** коротко сериализовать выделение общей sequence; при актуальных relevant target baselines и допустимых условиях обе операции успешны с уникальными номерами. Реальный target baseline conflict возвращается без автоматического retry. |
| Draft validation и Accept base | Publish требует полного соответствия; Accept не меняет payload/orphan | **Ответ:** Save допускает missing required, сохранённую несовместимость после schema changes и orphan; новое present type-invalid значение отклоняется адресуемо. Transient numeric invalid ввод остаётся локальным UI. Accept проверяет exact admissible target/current baseline, подтверждённую basis и concurrency без payload mutation; missing required/orphan и сохранённая несовместимость допускаются с diagnostics. Withdrawal target availability не обходится. |
| Lifecycle источника и target | Copy разрешён из Draft/Published того же instance; target той же schema, Draft/Published | **Ответ Demo:** Withdrawn source/target допускает historical read, Copy и Save recovery work с предупреждением. Publish к снятому target/runtime запрещены. MVP Retired/Archived сохраняют отдельные new-use правила Q09/F04 и не приравниваются к Demo Withdrawal. |
| Последний Draft | Draft допускает hard delete | **Ответ:** удалить последний Draft разрешено. Logical instance остаётся с identity/Key/Name и пустым списком versions; parent автоматически не удаляется. |
| Изменившийся delete impact | Logical delete без Published/history; сервер проверяет зависимости атомарно | **Ответ:** новый exact dependency set требует повторного подтверждения; совпадения count недостаточно. Изменившийся impact отклоняет delete. Прежняя 5-second задержка повторяется для Draft schema cascade F02 и не переносится автоматически на instance delete. |
| Withdrawal и возврат в выдачу | Withdrawal исключает representations из следующей direct sync; history остаётся | **Ответ:** новый Withdraw требует retained Published content; Draft-only rejected. Отдельное явное действие «Вернуть в выдачу» при Published history проверяет допустимые Published representations и запреты schema/schema-version/instance scopes. Без retained Published допускается recovery собственного запрета без публикации Draft и без schema/media checks. Publish Withdrawal не очищает. Archive/Retire Restore остаётся отдельным MVP lifecycle. |
| Deprecated options | Enum/FlagEnum используют keys; unknown key invalid | **Ответ:** объявленные deprecated keys валидны для Save/Copy/Publish. UI предупреждает и скрывает их из обычного нового выбора, сохраняя выбранные исторические значения. |

**Ответ:** перечисленные Demo продуктовые границы приняты и применены в F04/F13/Q09/Q34. Точный wire contract, transaction guards и проверка реализации остаются engineering work Q15/Q20; наличие ответов не означает готовность реализации или закрытие всех MVP неизвестных.
