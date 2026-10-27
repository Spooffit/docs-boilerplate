# F22. Генерация C#-моделей: фронтенд

[Бизнесовая спецификация](../../../business/features/F22-csharp-model-generation.md) · [Backend F22](../../backend/features/F22-csharp-model-generation.md) · [Registry F21](../../backend/features/F21-schema-registry.md) · [M07](../modules/M07-schema-tooling.md) · [Q13](../questions/Q13-ui-scope.md) · [Q31](../../backend/questions/Q31-schema-code-generation.md) · [Demo/MVP Q26](../../../business/questions/Q26-demo-mvp-scope.md).

## 1. Результат и границы

Разработчик открывает Code Generation, выбирает точные опубликованные версии одной или нескольких схем, видит итоговый набор и запрашивает ZIP с generated `.g.cs`. Можно одновременно выбрать несколько версий одной схемы. Идентичность выбора — SchemaVersionId; названия, ключи и VersionNumber нужны для понимания контекста. Сервер F22 получает контракты через F21 и вызывает независимый `Schema.CodeGeneration.Core`; браузер не выполняет JSON Schema → C# и не собирает архив.

Demo включает только этот путь выбора/генерации/скачивания, без входа, Identity, ролей и лицензий. Страница не становится редактором namespace, serializer, generator options, manifest или CI; дополнительных targets и runtime UI/game behavior не генерирует. Схемы и экземпляры не изменяются, модель не выводится из instance payload, история генераций не создаётся.

MVP сохраняет UI→ZIP и добавляет отдельный local .NET tool ContentManager.Schema.CodeGeneration.Cli / cm-schema. content-manager.json, locked formal inputs, restore/offline generate/readonly check и owned filesystem recovery согласованы [Q31](../../backend/questions/Q31-schema-code-generation.md#mvp-cli-конфигурация-и-файловый-workflow); модели/algorithms — backend F22. CLI не зависит от Blazor и не добавляет Demo options/form. Actual tool install/TFM/feed Q32; developer auth Q24 и новые MVP profiles остаются отдельно.

Срез реализации 09.10.2026: [CodeGenerationPage](../../../../contentmanager.ui/src/ContentManager.Pages/Components/CodeGeneration/CodeGenerationPage.razor) владеет локальным выбором, чтением и summary; [SchemaVersionSelection](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/CodeGeneration/SchemaVersionSelection.razor) получает controlled selection; [GenerateCSharpModelsFlow](../../../../contentmanager.ui/src/ContentManager.Features/CodeGeneration/GenerateCSharpModelsFlow.cs) владеет одной immutable attempt и подготовленным ZIP. Современные generated read models предоставляют integer VersionNumber, frozen PublishedSchemaName/Key и IsRetired/IsWithdrawn/IsLegacyDefinition. Raw FileResponse сохраняет stream/headers/HTTP response ownership. F22 использует [GenerateCSharpArchiveClient](../../../../contentmanager.ui/src/ContentManager.ApiClient/Infrastructure/CodeGeneration/GenerateCSharpArchiveClient.cs) → actual generated ICodeGenerationClient.CsharpAsync(GenerateCSharpModelsRequest, CancellationToken). DTO содержит только SchemaVersionIds. Клиент регенерирован из фактического Windows backend через existing generate-api-client configuration; generated source вручную не редактировался. Thin adapter регистрируется существующим scoped matching-interface scanner.

Принятый UI limit: `CodeGeneration:MaximumArchiveBytes = 41943040` (40 MiB) в App `wwwroot/appsettings.json`. AddApp читает обязательное положительное Int32 значение до запуска приложения; отсутствующее, нулевое, отрицательное, нечисловое либо переполненное значение останавливает startup, silent default отсутствует. Это предел принимаемых archive bytes в UI, не обещание browser memory usage/throughput и не подмена серверных F22 source/files/archive/time guardrails.

## 2. Страницы, экраны и навигация

| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. CodeGenerationPage | Выбранный Q13 route `/code-generation`, пункт Demo-навигации «Code Generation»; schema/version ID для открытия не обязательны | SchemaVersionSelection, SelectedContractsSummary, GenerateCSharpModels, состояние операции и download; только чтение определений и локальный выбор | Успешный ответ даёт скачивание ZIP в браузере; страница сохраняет текущий контекст для следующего действия |

Pending/error/download — состояния SC01, не новые маршруты/диалоги. Отдельная Registry page и редактор C# source не нужны. Прямое открытие без selection загружает доступные варианты и показывает пустой итоговый выбор; отсутствие обязательного route ID не вызывает поиск latest.

Срок жизни локального selection — экземпляр страницы. URL-параметры для preselection, сохранение после reload/возврата и browser storage не являются частью Q31; при необходимости это отдельная Q13/UI-деталь. Уже выбранная недоступная версия не заменяется latest и не исчезает из summary без понятного результата проверки. Реализован explicit flow Generate → готовый ZIP → отдельная кнопка «Скачать ZIP». Выбор/снятие и второй Generate заблокированы во время active generation/download. Кнопка «Прервать ожидание» и уход со страницы сначала invalidates attempt, затем cancels transport; поздний ответ не меняет новый context и освобождает FileResponse. Отмена best-effort не доказывает остановку уже завершившейся серверной подготовки.

## 3. Карта FSD-блоков

### 3.1. Состав

| Имя блока | Слой / срез / сегмент | Ответственность | Использование в этом изменении | Основное описание |
| --- | --- | --- | --- | --- |
| Code Generation navigation | app / существующая оболочка | Подключить SC01 к согласованной навигации | Изменить меню/router; Demo без F16 guard | Q13, §2 |
| CodeGenerationPage | pages / code-generation | Единственный владелец selection/read metadata и контекста страницы | Реализована композиция SC01 | §3.2 |
| SchemaVersionSelection | widgets / schema-version-selection | Совместно показать каталог схем и exact версии для выбора | Реализован controlled блок multischema/multiversion | §3.3 |
| SelectedContractsSummary | pages / code-generation / ui | Показать полный выбранный набор и проблемы его членов | Локальная часть страницы; отдельный slice не нужен | §3.4 |
| GenerateCSharpModels | features / generate-csharp-models / ui, model, api | Запросить серверную генерацию и передать готовый binary result в download boundary | Реализован GenerateCSharpModelsFlow; controls компонуются страницей | §3.5 |
| Schema / SchemaVersion read presentation | entities / существующие schema и schema-version возможности | Читать и отображать серверные схемы/версии без второй модели | Переиспользовать и при необходимости расширить read boundary F01/F02 | [F01](F01-create-entity-schema.md), [F02](F02-create-schema-version.md), §3.6 |
| Общий API transport и binary download boundary | shared / api, техническое browser interaction | NSwag HTTP, error mapping, доступ к file content/headers и запуск скачивания | Переиспользован FileResponse; добавлены ZipDownloadBuffer/BrowserFileDownload | §3.7, §8 |
| Общие controls/messages | shared / ui | Выбор, действия, loading/error/empty state с функциональной доступностью | Переиспользовать без бизнес-состояния | S05/S07, §9 |

Отдельный global store и второй widget для summary не требуются. Таблица — логическая FSD-карта, а не требование создать новые assemblies или механически переносить TypeScript-структуру на C#.

### 3.2. CodeGenerationPage

| Аспект | Описание |
| --- | --- |
| Назначение | SC01: связать каталог, exact selection, summary, generation и результат |
| Входы | Route `/code-generation`; результаты обычных read API F01/F02; в MVP — подтверждённый доступ через F16 после выбора policy |
| Выходы | Controlled selection в widget/summary; immutable отправленный набор в feature; пользовательский результат/ошибка |
| Собственное состояние | SelectedVersionIds, read metadata, read loading/errors и текущий page context; один владелец на страницу |
| Данные и зависимости | Public schema/version reads, widget, feature, shared messages; без Registry parser/Domain/EF |
| Состав | SchemaVersionSelection, SelectedContractsSummary, GenerateCSharpModels |
| Жизненный цикл | Вход → чтение каталога; обновление read не меняет идентичность selection; уход → invalidate context/dispose requests и временных ресурсов |

### 3.3. SchemaVersionSelection

| Аспект | Описание |
| --- | --- |
| Назначение | Отобразить схемы и exact доступные версии, разрешить выбор нескольких схем и нескольких версий одной схемы |
| Входы | Generated read models, controlled SelectedVersionIds, read loading/errors и доступность по серверной policy от страницы |
| Выходы | Намерение добавить/снять конкретный SchemaVersionId; запрос повторить чтение; результат принимает страница |
| Собственное состояние | Только локальное presentation-состояние раскрытия/фокуса; не отдельный mutable selection и не второй каталог |
| Данные и зависимости | Schema/SchemaVersion presentation и shared controls; page загружает данные и передаёт результат |
| Состав | Контекст схемы, варианты точных версий, read/empty/error presentation |
| Жизненный цикл | Новые read данные обновляют варианты; ключ совпадения — ID, не caption/index; неизвестный/недоступный selected ID остаётся виден в summary |

UI показывает соответствующую exact версии frozen name/key и опубликованный integer VersionNumber, когда agreed read contract их предоставляет; current parent caption отделяется от historical label. Draft не обычный target. Retired/Withdrawn exact Published остаются допустимыми tooling targets; ordinary selection UI может показывать lifecycle badge/filter, но exact ID не заменяется latest: нельзя выводить право из имени статуса, hash, parent pointer или локального caption. Владелец read contract должен дать необходимые availability facts/policy; UI не изобретает boolean по legacy Archived/Published enum.

Текущий widget показывает исторические badge и допускает Retired/Withdrawn Published; legacy/non-Published либо неполные frozen metadata видимы как неподдерживаемые варианты. Page читает схемы и версии страницами по 20 через обычные generated IEntitySchemaClient/IEntitySchemaVersionClient. Summary version read содержит lifecycle/number, но frozen caption/key берутся отдельным exact detail GET. Ошибка чтения не очищает selected IDs; metadata других выбранных owners сохраняются при смене отображаемой схемы. Final source integrity/profile eligibility повторно проверяет серверная generation.

### 3.4. SelectedContractsSummary

| Аспект | Описание |
| --- | --- |
| Назначение | До отправки показать весь выбранный exact набор, включая несколько версий одного owner, и проблемы его членов |
| Входы | SelectedVersionIds и соответствующие generated read metadata от страницы; diagnostics операции с exact ID/path |
| Выходы | Намерение снять определённый ID при доступности действия; страница обновляет selection |
| Собственное состояние | Нет независимой копии выбора/DTO; представление производно от page state |
| Данные и зависимости | Page callbacks, SchemaVersion presentation, shared messages |
| Состав | Контекст каждой версии, количество/пустой набор, диагностические сведения соответствующих членов |
| Жизненный цикл | Перестраивается из текущего controlled selection; pending result показывается для отправленного набора, не переназначается текущему |

### 3.5. GenerateCSharpModels

| Аспект | Описание |
| --- | --- |
| Назначение | Выполнить server generation выбранного exact набора, получить готовый ZIP и инициировать browser download |
| Входы | Immutable копия selected IDs от page на запуске, correlation текущего контекста; согласованный generated request, без ручной FormModel |
| Выходы | Pending/result/error/cancel notification с correlation; binary result в общую download boundary; наблюдаемый статус для страницы |
| Собственное состояние | Одна active local attempt, отправленный snapshot, cancellation/transport state, diagnostics и временный binary resource; не live selection |
| Данные и зависимости | NSwag generation operation после F22/OpenAPI; общие error/file transport capabilities; без прямого F21 HTTP или generator Core |
| Состав | Generate action, pending/result/error presentation, download action после готового результата по согласованному flow |
| Жизненный цикл | Validate selection → запрос → complete ZIP/ошибка; повторное нажатие pending заблокировано; invalidated context не принимает результат; завершение/уход освобождает ресурсы |

Feature не вызывает другую feature для управления selection. Callback сообщает результат странице; request snapshot существует только как принадлежность текущей операции. Пустой выбор блокирует запуск с объяснением; backend validation остаётся обязательной.

### 3.6. Schema / SchemaVersion presentation

Основное описание чтения — F01/F02. Здесь entity получает generated schema/version models и отображает ID, exact version number, caption/key, lifecycle/availability, frozen metadata и, если доступен в agreed read API, opaque source hash. Read data принадлежат странице. Entity выдаёт представление/публичную read возможность, не изменяет version и не выбирает latest. Зависимости — generated NSwag и shared API/UI; использование — widget и summary SC01. Недостающие frozen metadata/availability добавляются в серверный read contract с регенерацией клиента, а не ручной копией DTO.

### 3.7. Общий transport / download

Фактическая основа — [ContentManager.ApiClient](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json), [ConfigureApi](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientWebAssemblyHostBuilderExtensions.cs) и [ApiEnvelopeDelegatingHandler](../../../../contentmanager.ui/src/ContentManager.ApiClient/Infrastructure/Http/ApiEnvelopeDelegatingHandler.cs). Handler пропускает успешный non-JSON response и распознаёт JSON/non-JSON HTTP failures; это не готовый ZIP consumer.

Binary/download capability реализована [ZipDownloadBuffer](../../../../contentmanager.ui/src/ContentManager.Shareds/Downloads/ZipDownloadBuffer.cs) и [BrowserFileDownload](../../../../contentmanager.ui/src/ContentManager.Shareds/Downloads/BrowserFileDownload.cs). Buffer принимает raw FileResponse и полностью читает owned stream в ограниченный временный byte buffer; HTTP200/application/zip, доступный Content-Length, fixed filename, local/EOCD/central-directory boundaries проверяются до Ready. F22 profile имеет comment-free/single-disk ZIP; распаковка файлов, проверка generated C# и пересборка ZIP не выполняются. Filename/filename* принимаются только для `content-manager-csharp-models.zip`; если Content-Disposition не exposed, fallback — только это известное profile имя. FileResponse освобождается и при отказе/отмене/ошибке чтения.

Готовые bytes принадлежат feature до следующей generation/Cancel/disposal; download retry не отправляет второй POST. BrowserFileDownload использует DotNetStreamReference и [download.js](../../../../contentmanager.ui/src/ContentManager.Shareds/wwwroot/download.js): полный ArrayBuffer → Blob application/zip → temporary anchor → object URL release. Module fences отменённый поздний stream result до click и освобождает URL после browser task. Ошибка browser initiation остаётся отдельной от generation failure. Результат означает «скачивание начато», не доказанное сохранение на диск. Shared boundary не хранит selection и не знает C# mappings; media URL DTO не используется как ZIP result.

## 4. Логическая компоновка

```text
SC01. CodeGenerationPage [pages; owner selection/read metadata/context]
  SchemaVersionSelection [widgets; controlled selection]
    Schema presentation [entities]
    Exact SchemaVersion options [entities]
    Выбор / loading / empty / read error [shared/ui]
  SelectedContractsSummary [локальная часть pages]
    Все selected exact IDs и их metadata / availability / diagnostics
  GenerateCSharpModels [features; owner одной attempt]
    Generate action / pending / failure / ready ZIP result
    Общая binary download boundary [shared/api + browser interaction]
  Сообщение о результате и следующие доступные действия
```

Все части относятся к одной странице. Summary остаётся рядом с выбором и результатом; сообщение об ошибке не заменяет каталог и не стирает IDs. Не добавляются modal, preview generated source и Registry editor.

| Связь частей | Какие сведения передаются | Кто координирует | Что меняется у получателя |
| --- | --- | --- | --- |
| Read API → page → selection/summary | Generated models точных версий, metadata/availability | Page | Каталог обновляется; selection сверяется по ID без latest fallback |
| Selection/summary → page | Намерение добавить/снять exact ID | Page | Единый выбор и summary; доступность Generate пересчитывается |
| Page → GenerateCSharpModels | Snapshot exact ID collection и page context на запуске | Page + feature attempt | Pending привязан к отправленному набору |
| Generation API → feature → page | Полный binary ZIP либо diagnostics с correlation | Feature | Result/error только исходной актуальной attempt |
| Feature → shared download | File result/headers, без Domain данных | Feature | Запущено скачивание или technical failure; это не подтверждение записи на диск |

Дерево показывает UI-состав. Оно не означает, что shared transport импортирует feature/page или entity знает родительский widget. Нижние блоки возвращают callback/event через публичную границу; page связывает выбор, read результат и действие.

## 5. Зависимости и публичные границы

Page использует публичные UI/read boundaries entity, controlled interface widget и action/result interface feature. Widget принимает коллекцию/selected IDs и сообщает exact ID намерения; summary производен от page state. Feature получает snapshot и correlation, возвращает подтверждённый результат/diagnostics; не читает внутренний store страницы и не меняет другой feature.

Shared предоставляет универсальный HTTP/file/download механизм. Его контекст — response/headers/lifetime, а не SchemaVersion, Registry или C# profile. Entity использует generated NSwag models напрямую; Browser UI не ссылается на серверные Domain/Application/EF assemblies. Generated contract не редактируется вручную, публичные operation/class names берутся только из реально согласованного OpenAPI и генерации.

Межфункциональная граница F21/F22 находится на сервере: F22 получает formal JSON Schema через внутренний F21 batch. UI только читает selection metadata и вызывает F22. SDK F19 потребляет сгенерированные типы отдельно от admin UI. Stable SchemaFieldId/Key/type задают contract semantics; authoring Name/HelpText не являются member identity. Exact generated root binding к SchemaId/SchemaVersionId/VersionNumber задан Q31; прочие runtime SDK lookup signatures остаются Q29; UI не решает его своим Registry parser или маппингом raw JSON fields.

## 6. Данные и владение состоянием

### 6.1. Владельцы

| Данные / состояние | Источник истины | Клиентский владелец и область жизни | Когда обновляется / сбрасывается |
| --- | --- | --- | --- |
| Schema/version read metadata и availability | Серверные read operations F01/F02, F21 tooling-read semantics | CodeGenerationPage; generated read models на время SC01 | Первое чтение/подтверждённый refresh; старый response не перезаписывает новый context |
| Текущий selected exact набор | Локальное намерение пользователя | Только CodeGenerationPage | Выбор/снятие ID; не заменяется вслед за parent rename/new Published |
| Отправленный набор | Snapshot выбора на момент Generate | GenerateCSharpModels, одна attempt | Фиксируется на запуске; не изменяется с live selection |
| Pending/result/error и correlation | Ответ конкретной attempt/локальное transport событие | GenerateCSharpModels; page принимает уведомление только своего context | Завершение/отмена/invalidation; предыдущий успех не становится успехом новой попытки |
| ZIP и имя файла | Complete F22 binary result, transport headers | Feature/shared download lifetime | Освобождение после завершения использования/ухода; не durable UI cache |

Логический presentation-state страницы, без новой бизнесовой DTO/ViewModel/FormModel:

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SelectedVersionIds | Уточняется — [Q13](../questions/Q13-ui-scope.md) | Да | Единственный live exact выбор страницы | Guid non-empty; duplicate impossible client-side; несколько exact versions одного Schema разрешены; logical type/representation: `HashSet<Guid>`/эквивалент UI membership; final CLR type is implementation detail |
| ReadMetadata | Уточняется — [Q15](../../backend/questions/Q15-api-and-source-code.md) | Да | Загруженные server сведения | Не ручная копия DTO; loading/unloaded отличаются от подтверждённого empty |
| ReadState | Уточняется — [Q13](../questions/Q13-ui-scope.md) | Да | Loading/ready/failure чтения каталога | Не подтверждает availability каждого выбранного ID после изменения сервера; logical type/representation: UI state; final CLR type is implementation detail |
| PageContext | Уточняется — [Q13](../questions/Q13-ui-scope.md) | Да | Принадлежность async чтений/операции экземпляру страницы | Не серверный Version/ETag, не persistent job ID; logical type/representation: UI lifetime/correlation state; final CLR type is implementation detail |

Логический state операции также не дублирует API-request business fields в отдельной FormModel:

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SubmittedSelection | Уточняется — [Q15](../../backend/questions/Q15-api-and-source-code.md) | Условно | Отправленные targets активной attempt | Заполняется при запуске; не latest/options/Domain definitions; local membership предотвращает повторное включение ID; backend независимо отклоняет duplicate IDs |
| AttemptContext | Уточняется — [Q13](../questions/Q13-ui-scope.md) | Условно | Связывает результат с отправленным набором и page context | Не server job ID; поздние ответы invalidated attempt игнорируются ; Не меняется, пока попытка активна; logical type/representation: UI correlation/lifetime token; final CLR type is implementation detail |
| OperationState | Уточняется — [Q13](../questions/Q13-ui-scope.md) | Да | Idle/pending/ready/failure/cancelled, download initiated отдельно | Ready только после complete binary ZIP response; logical type/representation: UI enum/state; final CLR type is implementation detail |
| Diagnostics | Уточняется — [Q15](../../backend/questions/Q15-api-and-source-code.md) | Условно | Ошибка по всему запросу либо конкретному ID/field path | Exact HTTP wire/status/machine codes Q15; не raw full schema/source dump |
| FileResource | Уточняется — [Q15](../../backend/questions/Q15-api-and-source-code.md) | Условно | Content и доступные file headers готового архива | Не ApiResponse<byte[]>; lifetime/disposal принадлежат transport/feature |

### 6.2. Состав представления

| Сведение | Источник | Использование | Правило и неизвестное |
| --- | --- | --- | --- |
| SchemaId / SchemaVersionId | Generated read contract | Группировка owner и identity выбора | Name/key/номер не заменяют ID; разные версии одного owner независимы |
| Frozen schema Name/Key, published VersionNumber | Exact Published metadata F01–F03 | Caption selected версии | Current parent rename не переименовывает historical contract; integer number, без Draft/major-minor fallback |
| Lifecycle / external availability | Server policy/read metadata | Доступность варианта и пояснение недоступного target | Retired exact read известен; Retired/Withdrawn exact tooling read; не фильтр по caption |
| Source hash | Только если agreed read API предоставляет | Дополнительный opaque контекст | Не JSON Schema/ZIP checksum и не право доступа |
| ZIP filename | `Content-Disposition` F22 | Имя скачиваемого файла | `content-manager-csharp-models.zip`; не зависит от текущего Schema Name |
| Generation diagnostics | F22 error contract | Form/result area и член selection по ID/path | UI не вычисляет C# names/type mapping по ошибке; missing details не угадываются |

Generator options в форме отсутствуют. No ETag/If-Match: это чтение immutable contracts и техническая generation command без сохранения ресурса. UI не читает server technical Version и не вызывает скрытый exact GET ради concurrency token после Generate.

## 7. Действия и клиентские потоки

| Действие | Начальное состояние и запуск | Исполнитель | Обработка / серверная операция | Результат в интерфейсе |
| --- | --- | --- | --- | --- |
| UI01. Открыть / повторить чтение | SC01, вход или read failure | Page через entity read API | F01/F02 read, metadata/availability; latest target не подставляется | Каталог либо различимый empty/error; selection сохраняет identity |
| UI02. Выбрать / снять exact версию | SC01, доступный вариант либо summary | Widget/summary → page | Локальное изменение SelectedVersionIds, без mutation API | Итоговый multischema/multiversion набор и доступность Generate |
| UI03. Генерировать C# | SC01, непустой допустимый selection, нет active local attempt | GenerateCSharpModels | Snapshot → generated F22 request → [backend F22 DF01](../../backend/features/F22-csharp-model-generation.md#df01-demo-generation-zip) | Pending; затем complete ready ZIP или error, без partial success |
| UI04. Скачать ZIP | Готовый complete binary result UI03 | Feature через shared boundary | Browser download server-provided archive; отдельный Registry/generator call не нужен | «Скачивание начато» либо download failure; сохранение на диск не доказано |
| UI05. Повторить генерацию | Завершённая/ошибочная attempt, выбран exact набор | Feature + page | Новый явный запрос для видимого набора; сервер снова применяет availability/profile | Новый результат соответствует новой attempt, старый не подменяет его |
| UI06. Отмена / уход | Active attempt или освобождение SC01 | Feature/page | Cancellation token при поддержке согласованного API; invalidate context и release temporary resources | Поздний ответ не меняет новую страницу и не запускает download; не обещано отменить уже завершившуюся серверную работу |

UI03 не загружает JSON Schema в браузер. Server F22 technical command выполняет Registry batch → reusable Core → ZIP; UI ждёт готовый результат, не поток отдельных файлов. Если одному target не хватает source/profile или обнаружена naming/path collision, результат не объявляется complete successful ZIP. UI не создаёт очередь/jobs/history ради ожидания.

Двойное Generate в pending не отправляет второй запрос. Реализованная UI policy блокирует изменение выбора в pending и предоставляет отдельный Cancel; immutable submitted snapshot сохраняется независимо от current page selection после завершения. Response/диагностики подписаны исходным набором; late invalidated result не подменяет новую attempt. Отмена/timeout после отправки не доказывают отсутствие server preparation; незавершённый/прерванный response не считается пригодным ZIP. Явный повтор технической генерации не повторяет publication или mutation, но заново проверяет текущую availability.

## 8. Использование API и NSwag

| Потребность / UI-поток | Серверная фича и операция | Входные сведения | Используемый результат | Владелец вызова |
| --- | --- | --- | --- | --- |
| UI01, каталог и metadata выбора | [F01](../../backend/features/F01-create-entity-schema.md), [F02](../../backend/features/F02-create-schema-version.md), agreed list/exact read; необходимые frozen tooling metadata F21 | Контекст schema/version чтения по agreed API, без instance inference | Generated read models, точные IDs/frozen metadata/availability | Page через публичное entity read API |
| UI03/UI05, server generation | [F22 DF01](../../backend/features/F22-csharp-model-generation.md#df01-demo-generation-zip), technical command; fixed `POST /api/v1/code-generation/csharp` | Только exact SchemaVersionIds; request model после согласования OpenAPI | Raw complete ZIP либо согласованная ошибка | GenerateCSharpModels через NSwag |
| F22 получает Registry contracts | [F21 DF03](../../backend/features/F21-schema-registry.md#df03-internal-batch) | Snapshot exact IDs на сервере | Полный сопоставленный набор formal documents | Backend F22; это не browser HTTP-поток |
| UI04, download | Уже полученный F22 binary result | Content и доступные Content-Disposition/transport headers | Browser download initiated / technical failure | Общая file/download boundary, вызываемая feature |

`/code-generation` — выбранный UI-route Q13. Actual generated ICodeGenerationClient.CsharpAsync отправляет `POST /api/v1/code-generation/csharp` с GenerateCSharpModelsRequest: только exact `SchemaVersionIds`. Accept — application/zip, response — FileResponse с filename `content-manager-csharp-models.zip`; используется ResponseHeadersRead и существующий shared handler. Core/CLI не вызываются браузером.

Реальный клиент генерируется [nswag.json](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json) в [ContentManagerApiClients.g.cs](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs), namespace `ContentManager.ApiClient.Generated`, и подключается существующим ConfigureApi/AddContentManagerApiClients. Request/read/error models используются напрямую после согласования backend OpenAPI; runtime source/model files и generated client в этой аналитике не меняются.

Success transport: raw binary `application/zip` с attachment filename `content-manager-csharp-models.zip`, без JSON `ApiResponse<T>`/base64 wrapper. Готовность архива означает successful preparation и complete bounded receipt для данной attempt. JSON error body либо non-success HTTP status не передаются в browser download как `.zip`. API handler пропускает binary success и сохраняет typed ProblemDetails failure. F22TransportTests проверяет actual generated method и production scanner DI: exact path/IDs-only body/Accept, no If-Match/hidden GET, unread raw stream/headers/owned disposal, typed failure и cancellation.

OpenAPI file response и generated/shared stream/headers/cancellation/disposal boundary реализованы и проверены HTTP fixtures. Content-Disposition/CORS в реальном deployment и фактический browser download требуют отдельной integration проверки; filename profile фиксирован Q31. UI не обходит согласованный transport отдельным HTTP-клиентом. Существующий MediaDownloadDto выдаёт URL metadata и не используется как raw ZIP response.

Feature отображает согласованные server diagnostics по exact version ID/field path, сохраняет selection и передаёт page подтверждённый result. Shared mapping не превращает download failure в generation failure с ложным новым server call. После ZIP нет автоматического GET schema/latest и нет ETag acquisition. Binary content не распаковывается/модифицируется/пересобирается клиентом; F22 archive validation и dependency closure — серверная обязанность.

## 9. Состояния, ошибки и доступ

| Ситуация | Что видит пользователь | Доступные действия | Что происходит с данными / восстановление |
| --- | --- | --- | --- |
| Loading/read refresh | Ожидание каталога/версий, ещё не подтверждённый empty | Действия вне затронутого loading context; repeat после failure | Старые read сведения не выдаются за актуальную policy; selected IDs не заменяются |
| Нет доступных вариантов | Empty каталог отдельно от read failure | Без Generate из пустого набора; повтор чтения, когда уместен | Не создаётся фиктивная latest/Draft версия |
| Selection пуст | Итоговый набор пуст, пояснение выбора | Выбрать exact versions | Generate блокирован локально; сервер тоже валидирует input |
| Выбрано несколько схем/версий | Каждый selected exact target и полный итог | Generate при известных допустимых условиях | Один page owner; два version IDs одного Schema не конфликтуют |
| Pending generation | Ожидание для отправленного набора | Двойное Generate блокировано; Cancel/edit-selection UI policy Q13 | Snapshot и correlation фиксированы; UI не показывает старый результат как новый |
| Invalid/notfound/unavailable target | Ошибка selection/конкретного exact члена, если сервер дал ID | Исправить выбор, repeat read/generation по контексту | IDs и metadata сохраняются для понимания; no latest substitution/quiet removal |
| Unsupported profile/type/ref, naming/path collision | Различимое generation failure с безопасной diagnostic | Исправить доступный контекст либо обратиться к developer; новый запуск после разрешения | Не partial ZIP/omitted field/generic object fallback |
| Server archive preparation failure | ZIP не подготовлен | Явный повтор после завершения failure | Selection сохранён; не создаётся фиктивный файл из error JSON |
| Complete ZIP ready | Сервер подготовил архив выбранной attempt | Browser download по UI04 | Архив принадлежит submitted selection, не новому выбору |
| Download initiated | Скачивание начато | Следующее явное действие | UI не утверждает «сохранено на диск»/«Unity compilation пройдена» |
| Browser download failure | Technical failure запуска/получения файла отдельно от preparation | Повторный download при ещё пригодном result или новый явный Generate | Без автоматического скрытого повторного generation, без ложного сохранения |
| Timeout/network/truncation/cancel | Не получен пригодный complete result; cancellation отличается от server validation | Явный repeat по UI05, уход; Cancel UI только после выбора | Resource освобождается; late response invalidated context игнорируется, без auto download |
| MVP 401/403 | Вход нужен либо действие запрещено по F16 | Согласованный [F16](F16-staff-access.md) flow; без retry loop при 403 | При повторном входе того же сотрудника восстанавливается исходный exact selection; fresh read metadata/availability проверяются отдельно. Generate/download не повторяются автоматически. Другой identity не получает предыдущий context/result; active attempt invalidates |

Demo route не требует current staff/login и не зависит от F16/Identity. В MVP доступ к listing/generation определяется выбранной серверной policy F16/Q15: конкретное MVP permission name определяется F16/Q15. Клиентская блокировка не является authorization; immutable source/hash и historical Registry read не дают право любому текущему сотруднику. Для MVP reauthentication страница удерживает original exact selection в привязанном к тому же staff recovery context по F16; fresh read может выявить недоступный target, но не заменяет сохранённые IDs. F16/Q24 выбирает in-memory recovery в живой SPA-вкладке через internal reauth без reload; reload/close/browser restart исключены, explicit Logout/другой user очищают snapshot. Recovery не добавляет ETag или ручную business DTO и не возобновляет завершённый либо неизвестный download автоматически.

Read error относится к каталогу/версии, generation error — к действию/выбранному члену, download error — к транспортному результату. Не блокировать всю SPA без необходимости. Подписи выбора, версия/owner, error и ожидание различимы; основные выбор/снятие/Generate/download доступны с клавиатуры. Фокус остаётся в текущем SC01 при ошибке и не переводится late response в чужой контекст. Базовые требования [S05](../standards/S05-ui-and-forms.md), [S07](../standards/S07-accessibility.md); CSS/точные компоненты/сертификация доступности здесь не назначаются.

## 10. Проверки и приёмка

По [S08](../standards/S08-testing.md) проверяются action/selection state, компонентный multiselect, page composition, реальный transport и полный путь отдельно. Каждый тест создаёт свой набор; fixtures ниже не production seeds. Названия библиотек/UI runner не выбираются документацией. Незакрытые profile/availability policies не фиксируются случайным expected result.

| Проверяемый блок / сценарий | Исходные условия и воздействие | Наблюдаемый результат | Граница проверки |
| --- | --- | --- | --- |
| T01. Selection widget + summary | Собственные generated fixtures: Schema A versions A1/A2, Schema B version B1; выбрать A1/A2/B1, снять A1 | Один controlled выбор, две версии одного owner разрешены, summary совпадает | Изолированный компонент, read API doubles |
| T02. ID/metadata identity | Одинаковые captions разных IDs, новый Published/latest, rename current parent, exact frozen labels | Выбор/request сохраняют exact IDs; исторический caption из согласованных metadata, no latest/current-name substitution | Unit selection + page composition, fixtures независимо заданы |
| T03. Read/availability | Loading, empty, failure и selected исчезнувший ID; Draft/Retired/Withdrawn fixtures по selected policy | Empty/error различимы, selected ID не тихо теряется; Draft не target; Retired exact read известен, Retired/Withdrawn exact tooling targets | Component + API stub; real server policy отдельно |
| T04. UI03 request | Непустой набор, double click pending, затем explicit repeat | Ровно один active call с выбранными IDs, без Options/Name/latest/Version/If-Match; новая attempt отдельна | Page/feature + controlled generation boundary; не доказывает backend ZIP |
| T05. Binary success/error | Готовый application/zip fixture с заголовками; JSON error/non-JSON HTTP failure/malformed/truncated response | Только complete success передан download; diagnostics отдельно, error body не скачан как ZIP | Real shared handler/generated transport с HTTP fixture; browser download adapter double |
| T06. Cancel/late response | Pending A, уход/invalidation или разрешённая UX смена контекста B, затем late A success/error | A не изменяет B и не запускает чужое download; result подписан отправленным набором; ресурсы освобождены | Async feature/page composition с управляемым completion, без sleep-based test |
| T07. Failed member | Набор A1/A2/B1, server unavailable/unsupported/collision diagnostics одного члена | Нет complete success/partial ZIP, exact ID/path связан с summary, выбор сохранён | Component/API fixture; реальная all-or-error generation отдельно в F22 backend |
| T08. Download result | Complete file fixture, download initiated либо adapter failure | Подготовлен ZIP / начато скачивание / browser failure различимы; не заявлено disk saved; no hidden server retry | Shared browser integration + feature, проверяет observable initiation |
| T09. Demo/MVP access | Demo без identity; MVP own staff/access fixtures выбранной policy, 401/403, повторный вход того же staff, fresh unavailable metadata и вход другого staff | Demo открыт без login. Тот же staff получает original exact selection без latest/fresh selection замены; availability перечитывается отдельно, auto Generate/download replay отсутствует. Другой staff не получает прежний context/result; refusal не успех | Page/F16 composition; real authorization server Functional tests отдельно |
| T10. Accessibility | Свой каталог/selection/error/pending; выполнить основные действия с клавиатуры | Понятные подписи/captions, error не только цветом, фокус/контекст не теряются | Browser UI behavior; не pixel-perfect/CSS тест |
| T11. Real CM UI→API→ZIP | Собственные Published A1/A2/B1 в реальной PostgreSQL fixture; хотя бы одна схема без экземпляров; latest не выбран | UI отправляет exact набор, реальные F21/F22 возвращают скачиваемый ZIP с выбранными `.g.cs` и agreed dependent types, no extra latest | Реальный браузер + CM/API/DB; ZIP проверяет test harness, не product browser parser |
| T12. Generated models + SDK | Архив T11, согласованный Unity 2022.3.62f3 project и реальный F13 payload выбранных структур | Две версии совместно компилируются без collision, typed payload принимается через F19; deprecated property с обязательным [Obsolete] | Contract/runtime acceptance F22/F19; E2E только при реально выполненной межприложенной цепочке |

F22 Core/backend проверяют 15 типов, recursive List/Object, Enum string keys/FlagEnum collection, fixed RGBA, required/deprecated, precision/temporal/AssetId/profile и безопасные paths. UI принимает server result и не дублирует эти algorithms. ZIP fixture/HTTP test не доказывают C# correctness/Unity compatibility; application test с mocks не доказывает реальную Registry persistence. T11 использует существующий PostgreSQL/Testcontainers contour и собственные данные/cleanup, без in-memory substitution.

Проверено 09.10.2026: полный UI build и console runner F01–F05/F13/F21/F22. F22 renderer использует настоящие generated ordinary read clients над изолированным HTTP fixture: A1/A2/B1 через смену owner, frozen labels, Retired/Withdrawn, visible legacy refusal, read retry/unavailable selected ID. Flow tests проверяют immutable pending, Cancel/disposal/late success/error, typed addressed diagnostics, bounded binary receipt/media/length/ZIP boundary failures, filename/UTF-8 carrier и download retry без нового POST. Actual JS module test выполняет Blob/anchor/URL lifecycle и late-stream fencing с DOM/URL boundaries. Дополнительно выполнен T11 на actual Chromium + disposable PostgreSQL/MinIO BrowserHost под Windows-пользователем: exact A1/A2/B1 сохранён через смену owner, generated client получил полный ZIP, отдельная кнопка инициировала native download. Скачанные bytes равны API response; повтор использовал owned bytes без второго POST. Screenshot просмотрен; обнаруженная literal Error binding исправлена, renderer regression и повторный real browser прогон прошли. Backend/Core/реальный netstandard SDK отдельно подтвердили compilation/strict Newtonsoft acceptance, но межприложенная F19/Unity/IL2CPP цепочка T12 не выполнялась. Приложения и временные контейнеры после проверки выключены.

## 11. Последовательность изменений и открытые вопросы

1. Использовать зафиксированные Q31 F21/F22 profiles и target endpoint; закрыть только OpenAPI/error/binary transport details Q15.
2. Обеспечить target read metadata F01–F03 и серверную цепочку F21 batch → Core → ZIP F22, без Registry parser в UI.
3. Обновить OpenAPI/NSwag generation и общую binary/header/error/download boundary; проверить её реальную transport capability, не править generated вручную.
4. Собрать SC01: controlled selection/read owner, widget, summary, одна generation attempt и download/error presentation; подключить chosen route в Demo shell.
5. Проверить isolated behavior, actual transport и реальный UI/API/ZIP путь; compile/runtime acceptance согласовать с F19/F20 отдельно. В MVP добавить обязательный CLI и access policy F16; не расширять Demo options screen.

| Вопрос | Какие детали блокирует |
| --- | --- |
| [Q31. Registry/codegen](../../backend/questions/Q31-schema-code-generation.md) | Demo naming/shared CLR ABI/golden vectors и принятые limits материализованы и проверены; новые MVP profile bytes и exact CLI codecs ещё materialize |
| [Q13. UI scope](../questions/Q13-ui-scope.md) | Непринятые подробности navigation return/preselection/reload и UI interaction; выбранный `/code-generation` и минимальный экран сохраняются |
| [Q15. API/source](../../backend/questions/Q15-api-and-source-code.md) | Actual OpenAPI/read frozen metadata/availability и binary response transport, headers/CORS/deployment error binding; существующий authoring клиент не доказывает новый endpoint |
| [Q29. SDK](../questions/Q29-unity-client-package.md) | Public typed store/lookup signatures и durable runtime storage; generated exact-version binding уже задан Q31 |
| [Q24. Identity](../questions/Q24-admin-sign-in.md#developer-tooling-доступ-cli) | Developer/staff authorization CLI остаётся отдельным механизмом от browser cookie, runtime activation keys и DeliverySession; Demo anonymous |
| [Q32. Поставка](../questions/Q32-sdk-package-distribution.md#mvp-cli-поставка-tooling) | CLI local tool/package identity выбраны; actual TFM/runtime/Core dependency/feed/install tests и Unity consumer matrix остаются; это не Demo page dependency |

Полные ответы остаются у владельцев Q; после выбора обновляются соответствующие разделы F22, а не добавляются новые «уточнения блока» в историю экрана. Не выбранные library/API/property names и числовые limits не восстанавливаются из archive-примеров.
