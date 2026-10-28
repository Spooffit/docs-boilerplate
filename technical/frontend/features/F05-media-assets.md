# F05. Ресурсы и выбор AssetReference: фронтенд


[Бизнесовая спецификация](../../../business/features/F05-media-assets.md) · [Backend F05](../../backend/features/F05-media-assets.md) · [M02](../modules/M02-media-library.md) · [F04](F04-entity-instances.md)


## 1. Результат и границы


Пользователь загружает файл через Content Manager, выбирая `AssetKind`, находит/reuse-ит ресурс в MediaLibrary, изучает metadata/preview и выбирает его для поля `AssetReference` совместимого kind. Stable identity ссылки — `AssetId`; read model возвращает enriched asset descriptor, а не заставляет UI работать с голым Guid. Bucket/provider location не являются значением поля. Новый файл создаёт новый ресурс; bytes прежнего Available AssetId неизменяемы.


Upload и metadata/lifecycle остаются CM API; обычное чтение bytes проходит `browser → CM-owned URL → CM host / YARP → private S3-compatible storage`. Upload идёт через Application и `IObjectStorage`, а binary read — через read-only gateway `GET`/`HEAD`. Браузер не получает provider endpoint, bucket, ObjectKey или storage credentials и не загружает/удаляет объект через gateway. Публичный byte route фиксирован `/storage/assets/{assetId}`. Server object name детерминирован тем же Guid; отдельный random ObjectKey не существует как public/independent identity. FileName не определяет storage path. Это [принятое целевое решение](../../../archive/content-manager-s3-yarp-media-delivery-decision.md), а не утверждение о готовом YARP route.


Demo показывает реальную цепочку `upload в CM → AssetId → AssetReference → Save/Publish экземпляра → HTTP download bytes → отображение в Unity`. Browser preview подтверждает только свою часть. Статическая фотография/placeholder вместо скачанного ресурса не удовлетворяет Demo. Sync content и загрузка media независимы; поздняя ошибка получения файла не отменяет Save/Publish или commit sync. Unity-потребление описывается в [F19](F19-unity-client.md), доставка — в [F13](F13-content-delivery.md).


Demo включает полноценный каталог MediaLibrary: upload, detail, preview/player, binary download через CM, server search/pagination/filtering, reuse existing asset, picker и допустимый hard-delete. Archive/Restore и раздел «Архив» относятся к MVP. Demo не зависит от входа/авторизации, ролей, брокера и полного продуктового Publishing. MVP применяет серверные права соответствующего сценария; скрытая кнопка не заменяет проверку.


Документ описывает target; названия будущих блоков — логические обозначения, не существующие Razor-классы. Текущее состояние исходников отделено в §11. Состав UI согласован в [Q13](../questions/Q13-ui-scope.md); kinds/formats/retention зафиксированы в [Q12](../../../business/questions/Q12-media-and-retention.md), техническая policy/storage/gateway — в [Q21](../../backend/questions/Q21-media-storage.md).


## 2. Страницы, экраны и навигация


| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. Библиотека ресурсов | `/assets`, «Ресурсы» Demo / контент MVP | FileName search, server pagination, filters Kind/format/date/Used/Schema/status, list/grid metadata, UploadAsset | Строка → SC02; upload обновляет list; query context в URL |
| SC02. Ресурс | `/assets/{assetId}`, строка или deep link | Exact AssetMetadata, preview/download, допустимые Delete/Archive/Restore; без редактора bytes | Назад в библиотеку; confirmed delete → список; Archive → read-only detail |
| SC03. Выбор ресурса | Диалог editable поля F04, current AssetId + required AssetKind | AssetPicker: search/page только совместимых Available assets, metadata/preview, upload нового того же kind, выбор одного AssetId | Confirm → выбранный AssetId родителю; Cancel → прежнее значение F04 |
| SC04. Удаление | Delete из SC02 либо строки SC01/SC05 | Exact metadata/ETag, server delete-impact, counts/blockers, подтверждение | Отмена сохраняет ресурс; success обновляет родителя; отказ сохраняет context |
| SC05. Архив ресурсов [MVP] | `/archive/assets`, отдельный navigation block «Архив» | Архивный server list/search/page, detail/Restore; без выбора нового content | Restore обновляет архив; detail → SC02 |


SC03/SC04 — значимые диалоговые экраны, отдельные routes не назначаются. Страница upload не нужна. SC01 не является вторым редактором. Breadcrumb SC02 использует FileName, route — immutable AssetId. Storage location не становится label/ссылкой.


Прямое открытие/refresh SC02 читает exact AssetId. Некорректный/отсутствующий ID не подменяется другим ресурсом. Archived detail MVP сохраняет deep link, показывает read-only/Archive state и запрещает новое использование. Demo byte read разрешает только Available: CM eligibility query выполняется до YARP/cache/304/Range. Архивные/незавершённые/Deleting ресурсы не становятся доступны по одному URL. Права MVP определяет отдельный server context.


SC01/SC05 хранят согласованные search/filter/page/sort в query string по Q13. Search с debounce выполняется сервером, весь каталог в браузер не загружается. Query использует backend-supported Kind/format/date/Used/EntitySchema/status filters; exact parameter naming и paging limits принадлежат Q15. Media kind является обязательным picker constraint, а не client-side догадкой. SC03 владеет своим временным query, не заменяет URL/query F04. Browser back восстанавливает list context, но сохранение File/stream через refresh не обещается.


## 3. Карта FSD-блоков


### 3.1. Границы уровней


Page координирует route/read model/results. Widget нужен для составного picker, используемого F04. Features выполняют upload, выбор и lifecycle-команды. Entities представляет ресурс/операции чтения. Shared содержит универсальные controls/dialog/HTTP. Feature на каждый GET, отдельный widget списка и global media store не требуются.


### 3.2. Состав фичи


| Имя блока | Слой / срез / сегмент | Ответственность | Использование | Основное описание |
| --- | --- | --- | --- | --- |
| Подключение ресурсов | `app`, composition/navigation | Routes/«Ресурсы», архив только MVP | Изменить shell | §2/§11 |
| AssetsPage / AssetDetailPage / ArchivedAssetsPage | `pages`, локальная media composition | SC01/SC02/SC05: route/query/read/action coordination | Изменить current media page; архив MVP | §3.3 |
| AssetPicker | `widgets/asset-picker`, `ui/model` | SC03: поиск/кандидат/upload/Select | Создать, F04 recursive reuse | §3.3/§5 |
| UploadAsset | `features/upload-asset`, `ui/api` | Явная загрузка одного нового File | SC01/SC03 | §3.3/§7 |
| SelectAsset | `features/select-asset`, `ui/model` | Confirm одного пригодного кандидата / Cancel | SC03 → F04 callback | §3.3/§7 |
| DeleteAsset | `features/delete-asset`, `ui/api` | Impact/consent/DELETE с own ETag | SC04 | §3.3/§7 |
| ArchiveAsset / RestoreAsset [MVP] | `features`, media lifecycle | Archive/Restore с own ETag | SC02/SC05 | §3.3/§7 |
| AssetPolicy / AssetMetadata / AssetPreview / reads | `entities/asset`, `ui/api/lib` | Generated backend policy, metadata, состояние, list/exact/content reads | SC01–SC05, read-only F04 | §3.3/§6 |
| File input/dialog/errors/NSwag/headers | `shared`, технические сегменты | Общий ввод/confirmation/transport без business orchestration | Reuse F01/F02/F04 | §5/§8 |


Размещение логическое, новые C# projects/каталоги с этими именами не обязательны. Current ContentManager.Pages/Widgets/Entities/ApiClient/Shareds — исходная структура, не подтверждение готовой декомпозиции.


### 3.3. Интерфейсы и ответственность


| Блок | Назначение и входы | Выходы / получатель | Своё состояние / область жизни | Зависимости, состав и жизненный цикл |
| --- | --- | --- | --- | --- |
| AssetsPage / ArchivedAssetsPage | Route/query, ordinary/archive mode, server list | AssetId navigation, action intents/results | Query/generated rows/loading/error до смены context | Entity list/metadata, upload обычной library, lifecycle; late old query не заменяет current result |
| AssetDetailPage | Route AssetId/exact metadata/own ETag | Inputs actions, принимает confirmed result | Generated DTO/ETag/read state до смены ID | Entity exact/metadata/preview + delete/lifecycle; прекращает прежние reads/очищает старый context |
| AssetPicker | Current AssetId/editability/required AssetKind/callback | Confirmed compatible AssetId или Cancel родителю F04 | Query/page/candidate descriptor/pending/errors до закрытия | Backend policy + filtered metadata/preview, UploadAsset, SelectAsset; kind нельзя переключить вопреки schema definition |
| UploadAsset | File + AssetKind + explicit Upload + backend policy | Pending/error либо server AssetDto/own ETag | File/stream/operation state на один запуск | File input accept/help строится из policy; multipart DF02; backend validates signature; success не Select/Save |
| SelectAsset | Candidate descriptor + required AssetKind + original value | Selected(AssetId) либо Cancel | Проверка/решение текущего выбора | Candidate Available и same-kind; F04 server revalidates; без Save |
| DeleteAsset | Exact AssetId/metadata/own ETag | Confirmed deletion либо blocker/error page | Generated impact/consent/pending/error на SC04 | DF03 из строки, DF05, shared dialog, DF06; preview не authority, close не server rollback |
| ArchiveAsset / RestoreAsset | Exact AssetId/own ETag/разрешённое действие | Confirmed outcome page | Pending/errors одной команды | DF03 из строки, DF07/DF08; page refresh; не изменяют content/bytes/F04 Save |
| AssetMetadata / AssetPreview / reads | Generated DTO/AssetId/read context | Read result/error owner; download outcome без изменения ссылки | Metadata без второй mutable DTO; preview read/resources до смены ID | NSwag reads, binary DF04/shared transport; unsupported preview оставляет metadata/download; освобождает temporary resources |


Shared file input возвращает браузерный File, не загружает Asset сам. Shared dialog получает текст/варианты/pending/disabled reason и возвращает решение; dependency rules принадлежат DeleteAsset. Общая infrastructure переиспользуется из [F01](F01-create-entity-schema.md#5-зависимости-и-публичные-границы)/[F02](F02-create-schema-version.md#5-зависимости-и-публичные-границы), её карточки не копируются.


## 4. Логическая компоновка


```text
SC01. AssetsPage [pages; route/query/list]
  Поиск/пагинация [локальная composition + shared]
  Metadata list/grid [entities/asset]
    Открыть ресурс → SC02
    DeleteAsset → SC04 [условно]
  UploadAsset [features]
    File input / operation state [shared]


SC02. AssetDetailPage [pages; AssetId/exact DTO/own ETag]
  Breadcrumb / AssetMetadata [entities/asset]
  AssetPreview / download [entities/asset; own read state]
  DeleteAsset → SC04 [условно]
  ArchiveAsset / RestoreAsset [features; MVP]


SC03. AssetPicker [widgets; диалог внутри F04]
  Поиск/пагинация [own query]
  Metadata list/grid [entities/asset]
  Кандидат: AssetMetadata / AssetPreview [entities/asset]
  UploadAsset [features]
  SelectAsset: подтвердить одного / отменить [features]


SC04. DeleteAsset [features; диалог exact AssetId]
  AssetMetadata [entities/asset]
  Delete-impact counts/blockers [generated response]
  Подтверждение / отмена / ошибка [shared presentation]


SC05. ArchivedAssetsPage [pages; MVP route/query/list]
  Поиск/пагинация [shared controls]
  Архивные metadata rows [entities/asset]
    Открыть archived detail → SC02
    RestoreAsset [features; exact own ETag]
    DeleteAsset → SC04 [если допускается сервером]
```


| Связь частей | Сведения | Координатор | Изменение у получателя |
| --- | --- | --- | --- |
| Строка SC01/SC05 → SC02 | AssetId/context возврата | Page/navigation | Exact read; list ETag не применяется |
| UploadAsset → SC01 | Confirmed DTO/ETag | AssetsPage | Success/server list refresh; без обхода query/page локальной вставкой |
| UploadAsset → SC03 | Confirmed DTO/ETag | AssetPicker | Новый кандидат, list refresh; поле F04 ещё не меняется |
| Строка → кандидат SC03 | AssetId | AssetPicker | Exact metadata/preview; query change сам по себе не подтверждает выбор |
| SelectAsset → picker → F04 | Confirmed AssetId или Cancel | Widget/родительский editor | Только нужное generated value, dirty=true; Cancel сохраняет прежнее |
| Delete/Archive/Restore → page | Confirmed server outcome | Page | Read refresh/navigation; success сохраняется при ошибке refresh |
| Preview → read context | Ошибка bytes/renderer | Preview owner | Metadata/ссылка сохранены; failure не отменяет Save/Publish |


Список/upload либо detail/preview видны совместно. Picker и delete confirmation открываются отдельно поверх родителя, не заменяют весь Instance Workspace. UI-вложенность не требует ссылки дочернего кода на page и не означает вызов соседней feature.


## 5. Зависимости и публичные границы


Публичная граница AssetPicker: current AssetId/editability/context и `Selected(AssetId)`/`Cancelled`. Это логический интерфейс, не заданная C# signature. F04 адресует конкретное поле generated request, поэтому один результат изменяет одну ссылку. `List<AssetReference>` и вложенные Object/List используют этот picker для каждого значения; multi-select за один вызов не добавляется.


F04 обращается к публичному widget media/entity-представлению выбранного ресурса. F05 не импортирует page F04, её внутренний editor и Save/Publish features. SelectAsset не вызывает UploadAsset: widget принимает upload result и передаёт SelectAsset как кандидата. Callback не вводит зависимость media от content editor.


`AssetReference` definition обязательно содержит `AssetKind`. Demo kinds: Image/Audio/Video. Picker получает kind из F01/F02 definition и запрашивает только совместимые Available assets; пользователь не выбирает kind отдельно внутри поля. Поддерживаемые extensions/max size приходят из backend F05 policy и показываются как hints/tooltips/`accept`. Manual GUID input запрещён.


Entities использует generated metadata/public reads и не является Domain/EF-моделью. Shared transport знает HTTP/binary/headers/cancellation, не business eligibility/delete rules. Браузер не получает storage SDK/credentials/bucket/key. Hidden/disabled UI не заменяет server authorization.


Режим доставки задаётся в [schema inspector F01/F02](F01-create-entity-schema.md#63-режим-доставки-assetreference-после-demo), а не в карточке/Upload/AssetPicker MediaAsset. Один файл используется в разных полях с разными режимами; picker возвращает совместимый AssetId и не редактирует OnDemand/Preload/RequiredBeforeLaunch. Для последующего 5–10 ГБ сценария upload должен показывать отдельные transfer/validation/ready/failure состояния и не обещать chunk-resume по текущей форме: streamed/multipart контракт, серверный максимальный размер и recovery остаются [Q37](../questions/Q37-launcher-asset-delivery.md) / Q21. Большой файл не считывается UI целиком ради preview. Это расширение после самостоятельного Demo, не доказательство поддержанного upload.

## 6. Данные и владение состоянием


### 6.1. Состояние


| Данные / состояние | Источник истины | Клиентский владелец / область жизни | Обновление / сброс |
| --- | --- | --- | --- |
| List metadata | DF01/generated response | SC01/SC05 либо SC03 | Query/route, refresh, confirmed mutation; late old query отвергается |
| Exact metadata | DF03/confirmed DF02 | SC02 либо текущий кандидат | Смена ID/reload/lifecycle result/read refresh |
| Asset own ETag | Header exact read/write | Рядом с соответствующим generated DTO | Не list/hash/content/F04 token; только confirmed server response |
| Search/page/sort/filter | URL SC01/SC05; own UI SC03 | Page/picker | Вход/query; picker очищается по закрытию/context change |
| File до upload | Browser File | UploadAsset, один выбор/запуск | Выбор/завершение/close; persistence через refresh не обещается |
| Uploaded result | DF02 metadata/ETag | Upload composition owner | Только подтверждённый result; failure не создаёт Available локально |
| Исходная ссылка / кандидат | F04 generated value / own UI | Исходное — F04, кандидат — picker | Confirm меняет поле F04; Cancel очищает кандидата |
| Content/preview resources | Binary DF04 | Preview/download read lifetime | Смена ID/close/cancel; temporary browser representation освобождается |
| Impact/consent | DF05/явное согласие | DeleteAsset SC04 | Refresh/context/error/close; preview не гарантирует DELETE |
| Pending/errors | Outcome конкретного запроса | Feature/read owner | Новый запуск/retry; не глобальная блокировка SPA |


Global media store/browser storage/offline/autosave не вводятся. Несохранённая ссылка принадлежит generated request F04, загруженный файл — отдельный server resource. Cancel picker/Discard F04 не выполняют implicit delete/rollback upload. Хранение неиспользованных/незавершённых resources уточняется Q12/Q21; automatic GC в MVP отсутствует.


### 6.2. Состав представления и формы


| Сведение | Источник | Использование | Правило / ограничение |
| --- | --- | --- | --- |
| AssetId | Server identity | Detail/выбор; только ID в content | Не генерируется UI и не заменяется URL/hash |
| FileName | Generated metadata / Browser File до upload | List/detail/кандидат | Display metadata, не storage path; server validation authoritative |
| ContentType/size/hash | Generated metadata после server validation | Metadata/safe renderer/integrity сведения | UI не вычисляет authoritative hash/Available по отправленному File |
| Status/lifecycle/upload/archive time | Target generated contract F05 | Badge/blockers/new-use/Archive | Нет ручной DTO с недостающими полями; current generated их не содержит |
| Browser File | Generic input | Multipart DF02 | Selection не upload; size/MIME policy не выбирается UI |
| AssetReference | Generated value F04 + F05 descriptor | Stable AssetId + immutable descriptor, captured сервером при Publish | Required kind из schema; descriptor/hash immutable Published, URL не serialized; Delivery отдельно читает bytes по AssetId |
| Delete-impact | Generated server response | CanDelete/counts/blockers | Любой reference блокирует hard-delete, cascade нет |
| Technical concurrency | HTTP ETag | Own If-Match | Вне DTO; не Version и не hash bytes |


Точные target DTO/поле/request/envelope shapes закрепляются backend/OpenAPI в [Q15](../../backend/questions/Q15-api-and-source-code.md#f05-детализация-целевого-контракта). Generated models используются напрямую, дублирующие AssetFormModel/EditorModel/ViewModel не создаются. Presentation-state — отдельно. Текущий generated `init` accessor не правится вручную; пригодность моделей для UI решается в server contract/generation infrastructure.


Существующая archived ссылка не исчезает из F04 при read/preview, orphan values также не удаляются. UI не трактует любой Guid внутри unknown/orphan JSON как AssetId: provenance принадлежит F04/server contract. Новые references к archived запрещены; граница повторного Save прежней archived ссылки и её переноса в новый context уточняется Q12/Q21.


## 7. Действия и клиентские потоки


| Действие | Экран / исполнитель | Клиентские шаги | Результат / ошибка |
| --- | --- | --- | --- |
| UI01. Search/page | SC01/SC05 page, SC03 picker | Query → debounce/server DF01 → read pending → generated rows | Empty отличается от loading/error; late query не заменяет current |
| UI02. Открыть ресурс | SC02 page | AssetId → DF03 → DTO + own ETag → actions | Недоступный ресурс не подменяется другим |
| UI03. Upload | SC01/SC03 UploadAsset | File → явный Upload → multipart DF02 → confirmed result | Success после Available response; list/candidate обновляются, Select/Save не вызываются |
| UI04. Preview/download | Read owner | AssetId → CM-owned gateway DF04 → безопасное поддержанное отображение/скачивание | Unsupported preview оставляет metadata/download; ошибка bytes не стирает ссылку; gateway cancellation/lifetime отделены от sync commit |
| UI05. Select | SC03 SelectAsset | Кандидат → exact metadata/eligibility → Confirm | Один AssetId родителю F04; dirty, explicit Save/Publish отдельно |
| UI06. Cancel | SC03 | Cancel/close → Cancel callback | Прежняя ссылка/другой ввод сохранены; успешный upload не удаляется |
| UI07. Delete | SC04 DeleteAsset | Exact DF03/ETag → DF05 → counts/blockers → consent при CanDelete → DF06/If-Match | Любой Draft/Published/history reference блокирует; success обновляет page, cascade отсутствует |
| UI08. Archive [MVP] | SC02/action строки | Exact metadata/own ETag → DF07/If-Match → confirmed result | Обычный picker/list исключает archived, новые ссылки запрещены; bytes/history сохраняются |
| UI09. Restore [MVP] | SC02/SC05 | Exact metadata/own ETag → DF08/If-Match → confirmed result | Server разрешает новое использование; обновляется archive/detail/read model |


Upload error не заменяет прежнее значение F04 и не выдаёт пригодный AssetId. После подтверждённого upload и Cancel picker ресурс можно позже найти в library. Успешная загрузка не выполняет неявный Select. Все отправленные клиентом bytes ещё не означают server Available; потеря ответа после Available commit означает неизвестный outcome, не доказанный Failed asset.


Поздний ответ после закрытия picker/смены поля не должен менять новый editor context. Отмена transport не обещает rollback на сервере. Новый файл не перезаписывает прежний Published asset: он получает новый AssetId и отдельно выбирается в Draft.


Hard-delete показывает server blockers/counts. Consent не разрешает cascade: пользователь может явно изменить/сохранить допустимый Draft F04, затем перечитать impact. Published/history таким способом не очищаются. DELETE повторно проверяет references независимо от preview и metadata ETag; UI не обещает успех после CanDelete при гонке.


412 сохраняет context, предлагает explicit exact reload и новое решение, не повторяет mutation с автоматически подменённым ETag. Network failure после mutation — неизвестный outcome, без гарантированного rollback/успеха. Повтор старого DELETE If-Match после внутреннего перехода `Deleting` не объявляется cleanup-протоколом: own ETag уже мог измениться. Recovery со свежим exact read/operation identity/internal worker уточняется Q21; новая public recovery route не назначается.


## 8. Использование API и NSwag


Ниже target backend F05. Точные generated names/query/envelopes/statuses/headers закрепляются Q15 и OpenAPI. Current client не объявляется уже реализующим target.


| Действие / чтение | Backend поток / HTTP target | Вход | Ответ / transport metadata | Клиентский владелец |
| --- | --- | --- | --- | --- |
| UI01 library/picker/archive | DF01, `GET /api/v1/assets` | Server search/page/согласованный archive context | Generated rows/paging; без own ETag каждой строки | Page/picker |
| UI03 upload | DF02, `POST /api/v1/assets` | Multipart одного File через stream/FileParameter | Generated AssetDto + own HTTP ETag после confirmed upload | UploadAsset → composition |
| UI02/candidate/action from row | DF03, `GET /api/v1/assets/{assetId}` | AssetId | Generated exact metadata + own HTTP ETag | Page/picker/action |
| UI04 content | DF04, CM-owned YARP gateway: `GET`/`HEAD`; canonical /storage/assets/{assetId} | AssetId/read context; server eligibility query перед YARP, Available-only Demo | Binary response/stream и headers; error body может быть non-JSON, не ApiResponse<byte[]> | Preview/download |
| UI07 impact | DF05, `GET /api/v1/assets/{assetId}/delete-impact` | AssetId | Generated CanDelete/counts/blockers | DeleteAsset |
| UI07 delete | DF06, `DELETE /api/v1/assets/{assetId}` | AssetId + обязательный own If-Match | Confirmed outcome по OpenAPI; wire Q15 | DeleteAsset → page |
| UI08 Archive [MVP] | DF07, `POST /api/v1/assets/{assetId}/archive` | AssetId + обязательный own If-Match | Confirmed metadata/outcome; следующий own ETag по contract | ArchiveAsset → page |
| UI09 Restore [MVP] | DF08, `POST /api/v1/assets/{assetId}/restore` | AssetId + обязательный own If-Match | Confirmed metadata/outcome; следующий own ETag по contract | RestoreAsset → page |


Все metadata-changing requests существующего mutable Asset требуют его own If-Match; upload нового Asset не требует. ETag хранится рядом с generated DTO. Token экземпляра/schema/list, hash bytes и content validator не заменяют metadata ETag/internal Version. Storage ETag — HTTP cache validator bytes, а не metadata concurrency token и не автоматически SHA-256. Exact validators/headers остаются Q21/Q15.


После confirmed mutation page обновляет согласованную server read model, принимает новый ETag из response либо читает exact resource по contract. Это не скрытый шаг для объявления mutation успешным. Ошибка refresh после success показывается отдельно; mutation не повторяется. Если headers недоступны через NSwag, меняется общая generation/transport infrastructure, копия business DTO ради ETag не создаётся.


Frontend перед первым upload/picker use получает backend `AssetUploadPolicyDto` и кеширует его только как read configuration на срок страницы/application policy refresh. Authoritative supported kinds/extensions/MIME/max bytes не дублируются константами UI. Если policy read failed, upload disabled с понятной ошибкой; UI не подставляет свой fallback allowlist.


Используется [nswag.json](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json): namespace `ContentManager.ApiClient.Generated`, multiple clients, base interface `IContentManagerApiClient`, custom templates, output `OpenApi/Generated/ContentManagerApiClients.g.cs`. [DI](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs) регистрирует найденные clients scoped. Generated-файл не редактируется вручную. List/impact/lifecycle/binary/header capabilities сначала появляются в backend/OpenAPI и общей infrastructure.


Target получает bytes через CM-owned YARP boundary, без обязательного прямого/presigned S3 URL и без MVC/Application OpenRead handler в обычном read hot path. Current `MediaDownloadDto.Url/ExpiresAt` и presigned MinIO flow не нормативны. Presigned optimization требует отдельного будущего архитектурного решения. Binary transport сохраняет Content-Type и Content-Length, когда provider их возвращает; тип не определяется только extension FileName. Stream/browser resources завершаются и освобождаются по lifetime. Range/206/416, conditional GET/HEAD, validators/cache и cancellation проверяются с реальным gateway/provider/browser; forwarding headers само по себе не доказывает поддержку. Buffering/resume и точный error mapping остаются Q21. Точные проценты progress, resumed/chunk upload и числовые timings не назначаются.


Binary client использует server validators (`ETag` from SHA-256, `Last-Modified`) и Cache-Control; Demo server policy default 24h immutable. Gateway OutputCache остаётся optional/bounded и не является обязательным cache media bodies. Его MaximumBodySize ограничивает попадание response в cache, а не допустимое скачивание файла. Клиент не делает вывод о failure большого файла из cache miss. Настройка gateway/private upstream, signing и access/cache boundary принадлежит Q21; в UI storage configuration не добавляется.


## 9. Состояния, ошибки и доступ


| Состояние / outcome | Представление | Следующий шаг | Сохраняемый context |
| --- | --- | --- | --- |
| Loading/empty | Отдельное ожидание / «ничего не найдено» | Дождаться/изменить query | Route/query, исходное поле F04 |
| File selected/uploading | File и pending этой операции; повторный старт блокирован | Explicit Upload до старта; ожидание/отмена по transport | Остальной editor; числовой progress не выдумывается |
| Confirmed Available | Server metadata/пригодный кандидат | Preview/download/Select/detail | Generated DTO/own ETag |
| PendingUpload/Failed/Deleting | Server state/reason, если раскрыты read contract | Разрешённое read/refresh/recovery Q21 | Нет новой пригодной ссылки/ложного успеха |
| Archived [MVP] | Badge/read-only/archive link | Historical read по contract, Restore | Старые references/history; новые запрещены |
| Unsupported preview | Metadata/доступный download | Скачать поддержанным способом | AssetId; отсутствие renderer не failure upload |
| Invalid upload/400 | Inline/summary у File | Исправить ввод/другой File | Прежнее значение F04 |
| 409 asset.not-available | Причина непригодности candidate/content | Explicit refresh/другой кандидат | Исходная ссылка/metadata |
| 409 asset.in-use | Dependency counts/blocker | Cancel/допустимый Draft edit/повтор impact | Ни ресурс, ни references не удаляются |
| 412 | Banner «ресурс изменился» | Explicit exact reload/новое решение | Без automatic new-token retry |
| 428 | Ошибка client/transport flow | Исправить missing mandatory header | Без пользовательского обхода precondition |
| 403 [MVP]/404 | Denied/not found различимы | Назад/допустимое чтение | Demo login/roles не вводятся; нет подмены ID |
| Storage/5xx/read failure | Ошибка конкретного upload/read | Допустимый read retry; outcome записи сначала выясняется | Нет fake Available/стирания AssetReference |
| Gateway 401/403, 404, 416 либо 5xx | Отдельный отказ доступа, unavailable, invalid Range либо gateway/storage failure | Разрешённый повтор/исправление read по контракту | HTTP status проверяется до renderer; non-JSON body не трактуется как файл или успешный JSON envelope |
| Network/timeout после записи | Результат не подтверждён | Проверить server state по recovery contract | Нет blind POST/DELETE/Archive retry/rollback promise |
| Refresh failure после success | Confirmed result + read error | Retry affected read | Mutation не повторяется |


409 dependency conflict не превращается в 412 optimistic concurrency. Exact server error codes/statuses/limit refusals остаются Q15/Q21. Size/allowed MIME policy frontend не выбирает; подпись upload не наследует current 1 GiB как requirement.


По [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback)/[S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics) исходная ссылка, кандидат, File, отправка и confirmed result различимы. Labels/blockers/pending/disabled reasons понятны без цвета; search/строка/upload/Confirm/Cancel/lifecycle доступны с клавиатуры. Dialog close возвращает focus инициатору; ошибки связаны с операцией. Safe preview profile уточняется Q21: browser renderer не доверяет одному untrusted MIME и не исполняет произвольный upload как UI приложения; unsupported type сохраняет metadata/download.


MVP CM проверяет authorization/entitlement, membership конкретной session/revision при runtime read и lifecycle/consumer context до proxy и любых output-cache/304/Range shortcuts. Архив сохраняет historical bytes; право читать их определяется контекстом CM, не YARP route. Знание AssetId, URL/hash или local preview не даёт права. Веб-библиотека не выбирает license policy и не выдаёт storage credentials. Demo сохраняет отсутствие auth.


## 10. Проверки и приёмка


Это target criteria по [S08](../standards/S08-testing.md), не результаты UI tests. Test library/browser matrix остаются Q13. API doubles проверяют взаимодействие блоков; storage, bytes, references/history/concurrency/access требуют реального backend соответствующего этапа.


| Сценарий | Условия / действие | Наблюдаемый результат | Граница |
| --- | --- | --- | --- |
| Library/query | Search/page/kind/format/date/Used/Schema/status/back/refresh | Correct server query/URL context, filtered rows, no full browser catalog | Page/transport + real query |
| Exact detail | Valid/missing/некорректный ID | Exact read/breadcrumb/context, нет другого ресурса | Page/API |
| Policy + upload success | Policy read → choose kind → valid File → Upload | Hints/accept from server policy; multipart includes kind; server descriptor/ETag; asset reusable in catalog | Feature + real storage |
| Upload validation/error | Wrong ext/MIME/signature/over-limit/storage failure/network after send | Нет fake Available/usable reference, прежнее поле сохранено, нет blind retry | UI + real failure/recovery |
| Existing select | Required kind + search/preview/Confirm | Only compatible Available candidates; selected AssetId dirty F04, без Save/Publish | F05/F04 composition |
| Upload в picker | Success → Confirm либо Cancel | Отдельное подтверждение выбора, Cancel сохраняет прежний ID, upload не rollback | Composition + real upload |
| Recursive form | Object/List/List<AssetReference> | Один picker адресует одно значение, остальные сохранены | F04 editor |
| Generated boundary | List/detail/impact/result/headers | Direct generated, own ETag отдельно, без storage identity в content | Contract/generation/serialization |
| Binary read | Supported/unsupported preview type | CM binary bytes, fallback download, корректный lifetime | Browser/transport + real bytes |
| Gateway contract | Real GET/HEAD, ETag/304, Audio/Video Range206/416, large file/cancel | Правильные bytes/headers/status, bounded streaming и освобождение ресурсов; non-JSON failure не success | Browser + CM YARP + выбранный private provider; forwarding не доказательство |
| Gateway boundary/cache [MVP] | Неавторизованный/foreign/archived context; cache hit/304/Range; файл больше cache body limit | Access/lifecycle проверены до cache/proxy; произвольный object и gateway mutations недоступны; большой файл скачивается без cache | Реальный CM access/gateway/provider; политика и конкретные ограничения Q21 |
| Download failure | Saved/Published content, поздняя media error | Не меняет reference/Save/Publish/sync commit | F04/F13/F19 + real delivery |
| History | Published с A, новый upload B/новый Draft | A возвращает прежние bytes, rewrite A отсутствует | Real content/storage/Unity |
| Delete from row | List без own ETag | Exact GET/own header, impact/consent/DELETE, без F04/hash/list token | UI/real HTTP |
| Delete blocked/race | Draft/Published/history/orphan provenance reference; новый reference после preview | Counts/blocker, no cascade/bytes delete; server повторяет usage check | UI + real references/concurrency |
| Deleting/recovery | Storage/metadata cleanup failure | Нет fake success/old-token automatic retry; согласованный recovery | Real backend; детали Q21 |
| Archive/Restore [MVP] | Historical asset → Archive → Restore | Исключён из ordinary picker, новые ссылки запрещены, старые bytes доступны; Restore разрешает выбор | UI + real lifecycle/access |
| Existing archived/orphan value | Read/reopen/Save прежней ссылки | Нет implicit clear/Guid inference; eligibility по согласованной границе Q12/F04 | Editor + real contract |
| Preconditions | Stale own ETag/missing header | Различимые 412/428, explicit reload, без blind retry | UI/real HTTP |
| Async | Double click/late query/ID change/closed picker | Один pending, нет позднего изменения другого поля/context, read resources освобождены | Component/browser |
| Success + refresh error | Mutation succeeded, следующая read failed | Confirmed result сохранён, повтор только чтения | Page/transport |
| Accessibility/access | Keyboard/errors/focus/MVP denial | Context/focus/disabled reasons, distinct403/404, server authority | Browser; real access отдельно |


Demo E2E проходит real upload/download: AssetId переживает explicit Save/reopen F04, Publish сохраняет ссылку, Unity отображает скачанные bytes. Screenshot metadata, placeholder или Markdown/link-check этого не доказывают. Full Publishing/broker/MVP authorization не становятся зависимостями Demo acceptance.


## 11. Последовательность изменений и открытые вопросы


Сначала backend F05/Q15/Q21 фиксируют list/detail/impact/lifecycle/binary/header contracts, затем обновляются OpenAPI/NSwag и shared infrastructure. После этого выполняется composition library/detail/upload/picker с F04, delete/preconditions и real bytes chain в Unity. Archive/Restore/SC05 добавляются в MVP с historical retention. Эта аналитика не меняет app/generated source.


| Вопрос | Что остаётся «Уточняется» |
| --- | --- |
| [Q15: F05 contract](../../backend/questions/Q15-api-and-source-code.md#f05-детализация-целевого-контракта) | Generated method/DTO/query/envelope names, list filters/sort/fields, errors/statuses/results/headers и NSwag ETag access; authoring/MVP read context |
| [Q21](../../backend/questions/Q21-media-storage.md) | Kinds/formats/policy/MinIO/YARP/object naming/cache/Range приняты; остаются low-level recovery/idempotency/operational tuning детали без изменения public media semantics |
| [Q12](../../../business/questions/Q12-media-and-retention.md) | Demo kinds/formats/retention boundaries закрыты; MVP UX existing-Draft-vs-Archived может уточняться без ослабления history protection |
| [Q22](../../../business/questions/Q22-quality-targets.md) | Нагрузка/размеры, задержки/восстановление; числовые defaults не назначены |
| [Q09](../../../business/questions/Q09-editing-and-deletion.md#asset--medialibrary) | Основание lifecycle: любой reference блокирует delete, cascade нет, Archive сохраняет bytes, automatic GC нет |
| [Q13](../questions/Q13-ui-scope.md) | Routes `/assets`, `/assets/{assetId}`, MVP `/archive/assets`, server search/page и generated direct приняты; testing/browser детали не подтверждаются этим документом |


Подтверждённая исходная реализация UI 04.10.2026:


| Источник | Текущая реализация | Отличие target |
| --- | --- | --- |
| [MediaAssetsPage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/MediaLibrary/MediaAssetsPage.razor), Upload/Find/Download | `/media-library/assets`; upload сразу при File selection, GUID lookup, metadata/presigned link, фиксированный 1 GiB; page оркестрирует API | Library/search/server page/detail, explicit upload/binary CM/picker/delete/own ETag; current limit не requirement |
| [NavMenu.razor](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/Common/NavMenu.razor) | «Файлы» → `/media-library/assets`, рядом Catalog/licenses | Target routes/«Ресурсы» Demo, архив только MVP |
| [Generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs), IMediaAssetClient/DTO | Только MediaAssetsPOSTAsync, MediaAssetsGETAsync(id), DownloadAsync; MediaAssetDto Id/ObjectKey/FileName/ContentType/Length/Hash; MediaDownloadDto Url/ExpiresAt; нет list/status/impact/lifecycle/own header contract | Regenerate target OpenAPI; не ручная модель с недостающими fields, storage location не public identity |
| InstanceValuesEditor.razor из [исторического среза](../../../archive/current-state-code-snapshot-2026-10-01.md), AssetReference/UploadFile | Read-only ID, upload заменяет field.Text, download callback; нет existing picker | Public AssetPicker/отдельный Confirm/generated value, required-kind filtering/recursive reuse |
| [EntityInstancePage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/ContentModeling/EntityInstances/EntityInstancePage.razor), Upload/Download | Upload возвращает AssetId/notice «Сохраните черновик», отдельный Save; presigned download | Независимость upload/Save сохраняется; target picker/bytes, media own ETag отдельно от instance |
| [WorkspacePageBase.cs](../../../../contentmanager.ui/src/ContentManager.Shareds/Components/Shared/Infrastructure/WorkspacePageBase.cs), RunAsync/Dispose | Общий Busy/Error/lifetime token, всякий409 дополняется «данные изменились» | Local pending, distinct business409/412/428/unknown/refresh; lifetime не доказывает весь flow |
| [nswag.json](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json), [DI](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs), [ApiEnvelopeDelegatingHandler.cs](../../../../contentmanager.ui/src/ContentManager.ApiClient/Infrastructure/Http/ApiEnvelopeDelegatingHandler.cs) | Generated/init/scoped infrastructure; non-JSON success пропускается, JSON body заменяется StringContent | Reuse с проверкой binary/header preservation; наличие handler не доказывает stream/ETag contract |


Source evidence исторического среза подтверждает описанные тогда факты, а не реализацию target или real Unity bytes chain.

## 12. Реализованный веб-сценарий Demo, 09.10.2026

Раздел описывает текущие исходники после согласования backend F05/Q15/Q21 и генерации клиента из фактического Windows API через настроенный `.run/generate-api-client.run.xml` → `.tools/generate-api-client.sh`. Исторический срез §11 сохраняется для сравнения. Точные DTO/HTTP-контракты принадлежат [backend F05](../../backend/features/F05-media-assets.md) и [Q15](../../backend/questions/Q15-api-and-source-code.md); клиент использует generated `IAssetClient`, собственный header metadata/ETag захватывается отдельно от DTO.

| Экран / блок | Реализованное поведение и владелец состояния | Связь с результатом |
| --- | --- | --- |
| [MediaAssetsPage](../../../../contentmanager.ui/src/ContentManager.Pages/Components/MediaLibrary/MediaAssetsPage.razor), SC01 | `/assets`, «Ресурсы»; server search с debounce, страницы по 50, Kind/format/date/Used/Schema/status. Query восстанавливается из URL; верхняя дата включает последний tick выбранного локального дня. Usage относится к текущим Draft/Published, неизвестный/incomplete count показан отдельно | Строка открывает exact detail с сохранённым return context; upload/delete обновляют list. Ошибка обновления после подтверждённой команды повторяет только read |
| [AssetDetailPage](../../../../contentmanager.ui/src/ContentManager.Pages/Components/MediaLibrary/AssetDetailPage.razor), SC02 | Exact `/assets/{assetId}`, FileName/metadata/status, собственный ETag, all-reference impact/provenance. Известная версия экземпляра имеет ссылку `/instances/{instanceId}/versions/{versionId}`; orphan/category/path сохраняются | Preview/download и delete используют именно открытый AssetId; отсутствующий/denied ID не подменяется |
| [InstanceAssetPicker](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/ContentModeling/EntityInstances/InstanceAssetPicker.razor), SC03 | Встроенный немодальный экран F04: required kind, server search/page, exact кандидат, preview, upload того же kind, явные Confirm/Cancel | Confirm передаёт пригодный AssetId родителю; Cancel оставляет исходное значение. Upload не выполняет Save/Publish и требует отдельного выбора; свежий exact read проверяет пригодность после upload replay |
| [UploadAsset](../../../../contentmanager.ui/src/ContentManager.Features/MediaLibrary/UploadAsset.razor) / [AssetUploadFlow](../../../../contentmanager.ui/src/ContentManager.Features/MediaLibrary/AssetUploadFlow.cs) | Backend policy задаёт kinds/accept/extensions/MaxBytes; выбор файла отделён от Upload. Flow замораживает File/kind/operation ID/MIME/лимит, блокирует повторный старт и хранит исходную попытку в sessionStorage | При неизвестном результате повторяется исходная операция. После refresh нужен исходный файл с теми же именем/размером, fingerprint проверяет сервер; отклонённый replay не стирает неизвестный исход |
| [DeleteAsset](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/MediaLibrary/DeleteAsset.razor) / [AssetDeleteFlow](../../../../contentmanager.ui/src/ContentManager.Features/MediaLibrary/AssetDeleteFlow.cs), SC04 | Встроенное подтверждение: fresh exact/own ETag, полный delete-impact, blockers/references и явное согласие. Исходные asset ID/operation ID/consent/expected ETag сохраняются в sessionStorage | Обычный replay сохраняет исходный запрос; explicit Deleting recovery получает текущий собственный ETag и сохраняет первоначальные operation/consent. 412 не запускает automatic retry с новым token |
| [AssetPreview](../../../../contentmanager.ui/src/ContentManager.Entities/Components/MediaLibrary/AssetPreview.razor) | Общий entity renderer для library/detail/picker и frozen Published facts F04. HEAD проверяет публичный CM URL/status/type/length до native image/audio/video; ошибка renderer оставляет facts и download fallback | Preview использует `/storage/assets/{id}`; Save download добавляет только CM `?download=true`. Provider URL и Blob buffering не вводятся; byte failure не очищает AssetReference |

Логическая компоновка текущих исходников:

```text
MediaAssetsPage → server list + UploadAsset + DeleteAsset → refresh list
AssetDetailPage → exact metadata + all-reference impact + AssetPreview + DeleteAsset
F04 recursive InstanceValueControl → InstanceAssetPicker → candidate + UploadAsset + AssetPreview
                                                   Confirm → выбранный AssetId в F04
Published InstanceAssetView → frozen facts + AssetPreview → отдельный CM byte read
```

Page владеет route/query/read state; picker — временным кандидатом и поиском; feature flows — одной попыткой команды; AssetPreview — byte-read/renderer lifetime. Upload session key включает стабильный owner instance/version и точный field path с opaque List ItemKey, поэтому два поля одного kind не заменяют попытку друг друга и refresh сохраняет адресацию. Библиотека использует свой отдельный постоянный ключ. File/stream через refresh не сохраняется. Escape/Cancel возвращают focus в исходный context; неизвестная mutation не закрывается как успешно завершённая отмена. Числовой upload progress не выдумывается.

Проверено 09.10.2026: `dotnet build ContentManager.UI.slnx --no-restore` завершился без ошибок; полный `tests/ContentManager.Ui.Tests` runner прошёл F01–F05. F05 включает immutable upload/replay, 400/413 после неизвестного исхода, независимые field session keys, own numeric ETag, usage guard, durable Deleting recovery, generated multipart/DELETE/header/default paging, server filters/inclusive date/URL context, owner links и CM HEAD/audio/download rendering. Эти проверки используют console/HtmlRenderer/HTTP doubles.

Отдельный [F05 browser smoke](../../../../contentmanager.ui/tests/browser/F05-media-smoke.cjs) прошёл в настоящем Chrome против временного Windows API, PostgreSQL и MinIO: загрузка PNG через UI подтвердила собственный ETag/размер/SHA-256; search URL пережил refresh; native image отобразил исходные bytes; cross-origin attachment сохранил FileName и совпал с исходными bytes. Picker Cancel и Confirm не выполняли implicit Save; явный Draft Save закрепил AssetId и блокировал hard-delete. Явное удаление значения и последующий Save сняли blocker, после impact/согласия физическое удаление прошло, публичный byte URL вернул 404. Повторный вход в preview прошёл без CORS/console errors после исправления gateway `Vary: Origin`, проверенного отдельной backend-регрессией. Проверочные процессы остановлены, временный UI API override удалён.

Реальная Unity-цепочка F19, browser audio/video playback/Range, полная keyboard/screen-reader matrix, HTTP/2+HTTPS request streaming и большой файл свыше 1 GiB этими проверками не доказаны. Archive/Restore/SC05 остаются за пределами Demo.
