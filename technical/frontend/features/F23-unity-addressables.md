# F23. Unity Addressables add-on: клиентская спецификация

[Бизнесовая спецификация](../../../business/features/F23-unity-addressables.md) · [SDK F19](F19-unity-client.md) · [Серверная граница M05](../../backend/modules/M05-delivery.md#f23-граница-optional-addressables) · [M05](../modules/M05-delivery.md) · [Q29](../questions/Q29-unity-client-package.md#f23-mapping-snapshot-и-ownership-операций).

## 1. Результат и границы

В локальном Demo Unity-приложение разрешает AssetId CM через immutable mapping в native Addressables key и получает управляемый handle. Typed entity и raw-file custom providers остаются после Demo. Первый target — Unity 2022.3.62f3, Windows x64, IL2CPP; Local dependency profile/Editor evidence выбран в §10.2; actual target Player compatibility остаётся Q32. Основной SDK и обычный raw image/file flow F19 работают без add-on. По решению владельца 11.10.2026 отдельная F23 включена в Demo; реализуется согласованный локальный объём Addressables отдельно от F19. Add-on поставляется отдельным UPM, зависящим от com.leansoft.contentmanager.unity; обратной зависимости нет. Remote content/build pipeline и Launcher/shared-cache этим решением не добавляются.

Основание — [архив SDK](../../../archive/unity-client-sdk-architecture.md), §§12–24, 25–28, 38–39, 42, 46–52; переименование Core → DotNet принято в [дополнении](../../../archive/sdk-contracts-architecture-update.md). Архивные C# signatures, JSON и package versions иллюстрируют архитектуру, а не утверждённый опубликованный API. Блоки local Demo реализованы типами §5.1; CM custom providers и remote расширения остаются целевым дизайном.

AssetId не равен Addressables key. Add-on имеет три пути: custom entity provider читает типизированный объект из pinned SQLite snapshot; custom raw-asset provider использует общий F19 download/cache manager; обычный Unity-built key продолжает native catalog/AssetBundle pipeline. Mapping/catalog/bundles и dependency metadata относятся к последнему пути и не обязательны для raw CM-файла или entity. Произвольный prefab-файл не становится Unity-контентом без Editor build. Синхронизация F19 завершается на данных + AssetReference; отдельная загрузка ресурса не меняет её результат. Key grammar, public types и custom-provider contract уточняются [Q37](../questions/Q37-launcher-asset-delivery.md); `cm:entity`/`cm:asset` в источнике — примеры, не frozen API.

### 1.1. Локальный объём Demo

Владелец выбрал первый локальный выпуск: отдельный **com.leansoft.contentmanager.unity.addressables** с immutable mapping **Guid AssetId → Unity key**, typed Load, explicit Preload и управляемыми handles. Namespace/assembly — **ContentManager.Sdk.Client.Unity.Addressables**. Main-thread dispatcher переиспользуется из F19; base SDK не зависит от add-on.

Mapping создаётся приложением/content pipeline как **AddressableMappingSnapshot(contentVersion, IEnumerable<AddressableAssetInfo>)**. Snapshot копирует входы и не обновляется. ContentVersion — непрозрачный nonempty build context; это не CM SchemaVersionId и не доказательство готовности catalog. Local catalog/bundles должны соответствовать snapshot; их подготовка принадлежит потребителю. Публичный JSON manifest/remote codec не вводится в первый выпуск.

Remote catalog/mapping update, Launcher/shared cache, CM custom entity/raw-file providers, scenes и отдельный instance API отложены. Raw CM загрузка F19 остаётся самостоятельной.

## 2. Страницы, экраны и навигация

Не применяется к CM admin UI: F23 не вводит страницу, маршрут, форму, диалог или web API client. Здесь описывается программная интеграция Unity-получателя. Не создаются SC-ID или фиктивный экран для заполнения шаблона. Отдельная [сцена F20](F20-unity-demo-scene.md) остаётся потребителем базового SDK с raw image flow, без обязательного Addressables-показа.

Точка входа — прикладной вызов Load для pinned entity/AssetId либо явный selected Preload. Native Unity-built ветка использует mappings/catalog; CM provider ветка — общий F19 reader/manager. Приложение выбирает момент, тип и область использования; add-on отвечает за mapping/Addressables operation/handle. При выходе, смене контекста или уничтожении потребителя требуется согласованный cleanup; конкретный lifecycle API остаётся Q29. Поздний результат не должен применяться к другому контексту. Отмена ожидания не доказывает физическую отмену загрузки.

## 3. Карта FSD-блоков

### 3.1. Применимость уровней

FSD, Blazor, MudBlazor и NSwag из [обзора frontend](../README.md) относятся к CM web UI и не назначают архитектуру Unity-пакетов. Слои `pages`, `widgets`, `features`, `entities`, `shared` здесь не требуют новых срезов, компонентов или stores. Unity resource не является браузерной entity и не наследует server Domain/EF. Нужна карта SDK/content обязанностей, а не механическая FSD-структура.

### 3.2. Состав именно этой фичи

| Имя блока | Слой / срез / сегмент | Ответственность | Использование в этом изменении | Основное описание |
| --- | --- | --- | --- | --- |
| B01. Потребитель | Unity-приложение; вне CM FSD | Выбрать pinned entity/AssetId/тип/набор, использовать и освободить результат | Публичная интеграция без обязательного UI | §3.3, F19 |
| B02. AddressablesClient | `Sdk.Client.Unity.Addressables`; логическая runtime единица | Load, явный Preload и передача/cleanup ownership | Целевой add-on, не изменение DotNet sync | §3.3, §7 |
| B03. Mapping provider | Add-on/content слой | AssetId → платформенная ссылка через immutable snapshot; future manifest | Целевой provider; local/remote объём Q29 | §3.3, §6 |
| B04. Asset handle | Публичная ресурсная граница add-on | Value и явный Release/Dispose-контракт | Целевой результат Load; concrete API Q29 | §3.3, §5 |
| B05. Unity Addressables runtime | Внешний Unity package | Загрузка по key/catalog, собственные operations/resources | Переиспользование выбранной версии после проверки | §3.3, Q32 |
| B06. Unity content pipeline | Платформенная сборка/поставка | Mapping/catalog/bundles/dependencies согласованной сборки | Источник готового Unity-контента; не CM server build | §3.3, §7.3 |
| B07. CM locator/providers | Optional Unity.Addressables adapter | Entity из scoped typed reader; raw asset из shared manager → Unity resource/file lease | Последующая интеграция, §5.2 | F19 §6.8, Q37 |
| F19 DotNet/Unity | Базовый SDK | Данные/AssetId, transport/config/lifecycle, raw helpers и последующий shared file cache | Переиспользование без обратной зависимости на add-on | [F19](F19-unity-client.md) |

### 3.3. Ответственность и lifecycle блоков

| Блок | Входы и выходы | Владелец собственного состояния | Зависимости и место использования | Жизненный цикл |
| --- | --- | --- | --- | --- |
| B01 | Scoped entity/AssetId, тип, набор и cancellation context → вызов операции и использование Value | Прикладной выбор/контекст; handles после принятой передачи ownership | Публичная F19, B02/B04/B07; не внутренности cache | Запуск выбирает приложение; поздний результат не применяется к новому контексту; cleanup Q29/Q37 |
| B02 | AssetId/тип либо набор → handle или отдельный ресурсный исход | Operations до передачи handle/cleanup; Preload result Q29 | B03/B05 и Unity lifecycle/main-thread adapter F19 | Не теряет operation ownership при fail/cancel; shutdown/shared operations Q29 |
| B03 | Manifest + AssetId → key/optional version либо отказ | Локальные mappings и версия; snapshot/cache Q29 | B06; remote источник/получение Q29 | Local поставляется с build/content; remote проверяется; update/in-flight Q29 |
| B04 | Загруженный ресурс/ownership → Value и освобождение | Владелец соответствующей загрузки после передачи | B05; выдаётся B02, используется B01 | Release публично выражен; repeated release и prefab instance Q29 |
| B05 | Key/catalog/тип → операция/ресурс либо ошибка | Runtime handles/reference counting и поддержанные caches | Add-on; выбранная версия Q29/Q32 | Реальные cancel/release/main-thread semantics проверяются runtime |
| B06 | Unity source content → mapping/catalog/bundles/dependencies | Опубликованный платформенный набор/contentVersion | Editor/build/размещение; потребители B03/B05 | Не управляется CM sync; transition/fallback Q29 |
| B07 | Scoped entity/asset key + expected T → typed object либо managed resource/file handle | Addressables operation owner и consumer lease; snapshot зафиксирован до load | F19 typed reader/общий manager/Unity dispatcher, B05 custom provider registration | Release освобождает resource/lease; persistent bytes не удаляются немедленно; exact provider API Q37 |

Имена обозначают логические компоненты, не требование физических классов или singleton. Namespace, каталоги и init/dispose API local Demo назначены в §5.1; future provider names остаются иллюстративными. Общие обязанности F19 не дублируются в add-on.

## 4. Логическая компоновка

Страницы нет; дерево показывает SDK/content flow, а не UI-вёрстку. Новый путь дополняет native Unity-built ветку:

```text
Addressables.LoadAssetAsync<T> [consumer, illustrative public usage]
  CM locator/providers B07 [captured StoreScope + SnapshotId]
    entity → F19 typed reader → scoped SQLite → GeneratedType
    raw asset → F19 common download/cache manager
      verified file lease либо join/download/verify/publish одной job
      Image/Audio → Unity main-thread conversion → managed resource
      большой Video → verified local file lease → VideoPlayer.url
    Unity-built key → B03/B05/B06 native pipeline [ниже]
```

```text
Unity-приложение B01 [выбор и прикладной контекст]
  F19 DotNet/Unity [данные + AssetId; отдельно raw helpers]
  Optional F23 AddressablesClient B02 [operations до передачи ownership]
    Mapping provider B03 [AssetId → key, manifest context]
      Mapping Manifest ← Unity content pipeline B06
    Unity Addressables runtime B05 [catalog → bundles → resource]
      catalog/bundles/dependencies ← Unity content pipeline B06
    Asset handle B04 → B01 [Value + явное освобождение]
```

| Связь частей | Какие сведения передаются | Кто координирует | Что меняется у получателя |
| --- | --- | --- | --- |
| F19 → B01 | Данные + AssetId | Приложение | Может выбрать Load/Preload; metadata не загружает тяжёлые ресурсы |
| B01 → B02 | AssetId/тип/набор и cancellation context | Приложение запускает, B02 ведёт | Создана отдельная ресурсная операция |
| B02 → B03 → B05 | Mapping и catalog/content context | B02; snapshot Q29 | Загружается правильный ресурс либо отдельная ошибка |
| B02 → B04 → B01 | Готовый ресурс с ownership | B02 до передачи, B01 после принятия | Value используется и явно освобождается |
| B06 → B03/B05 | Версия mappings и соответствующий content | Pipeline/update механизм | Подготовлен новый набор; switch/fallback Q29 |

DotNet не раскрывает B01 S3 bucket/object keys. Remote bundles/CDN — отдельный платформенный flow. Приложение выбирает представление объекта; обязательная CM панель загрузки не задаётся.

## 5. Зависимости и публичные границы

Направление: `Sdk.Client.Unity.Addressables` → `Sdk.Client.Unity`, `Sdk.Client.DotNet`, `com.unity.addressables`. DotNet не знает Unity/mapping, основной Unity package не требует Addressables, SDK не зависит от сцены F20. Отдельные UPM/asmdef boundaries локализуют add-on; runtime/Editor зависимости разделяются. Actual IDs, сборки, source/DLL, CI и installation/version matrix — [Q32](../questions/Q32-sdk-package-distribution.md#f23-матрица-поставки-add-on).

| Граница | Подтверждённая ответственность | Незакрытая конкретная форма |
| --- | --- | --- |
| Mapping provider | AssetId → платформенная ссылка; missing mapping отдельно | `IAddressableMappingProvider.TryResolve` из §16.3 — иллюстрация; final signatures/async/snapshot Q29 |
| Load | AssetId + ожидаемый тип + CancellationToken; управляемый lifetime | `LoadAsync<T>` и Task/handle — иллюстрация; constraints/result/errors Q29 |
| Preload | Явный набор; metadata его не запускает | `PreloadAsync` из §20 не определяет result/partial ownership; Q29 |
| Handle | Value и явное освобождение соответствующей загрузки | `IAssetHandle<out T> : IDisposable` из §19 — пример; release/transfer/repeated release Q29 |
| Remote update | Согласование mappings/catalog/content | Update API/source/switch/fallback/in-flight Q29; CM endpoint не принят |

Обычный `Task<T>` без выраженного освобождения не покрывает ownership. Handle загруженного prefab и созданный prefab instance — разные объекты владения. Архивный `using` + `Object.Instantiate` не определяет безопасный момент Release, owner instance и способ создания/уничтожения. Не назначать `InstantiateAsync`, `ReleaseInstance`, `Destroy` либо удержание handle молча: contract и реальные проверки Q29.

Отмена waiter и физическая отмена Addressables operation различаются. B02 сохраняет владельца операции до передачи либо cleanup. Для local Demo каждый Load владеет независимым claim, а runtime может разделять underlying operation. Raw CM files join единственную job F19 manager; отмена одного waiter не удаляет job остальных. F19 entity-sync gate не переносится автоматически на resource lifecycle.

### 5.1. Конкретные interfaces/adapters optional пакета

| Часть | Реальный тип / API | Ответственность |
| --- | --- | --- |
| Mapping | AddressableAssetInfo; AddressableMappingSnapshot.TryResolve(Guid, out AddressableAssetInfo) | Guid → точный key; immutable snapshot; нет AssetId-as-key fallback |
| Coordinator | AddressablesClient(snapshot, IUnityMainThreadDispatcher) | Один content context, независимый native claim на вызов, безопасный handoff/cleanup |
| Runtime bridge | Internal NativeAddressableLoad<T> | Реальные LoadAssetAsync<T>/Completed/Release, только main thread |
| Ownership | AddressableAssetHandle<T>.Value; Dispose/DisposeAsync | Ready resource; idempotent release, Value после dispose запрещён |
| Preload | AddressablesClient.PreloadAsync<T>; AddressablePreloadResult<T> | Явный набор; per-item outcomes; удержание успешных handles даже при Partial |

LoadAsync<T>(Guid assetId, CancellationToken), где T : UnityEngine.Object, возвращает Task<AddressableLoadResult<T>>. Outcomes Loaded/Cancelled/Failed. Result содержит AssetId, ContentVersion, Handle либо Error; Error содержит Stage (Mapping/NativeLoad/Lifecycle) и Code (mapping_not_found/native_load_failed/client_disposed). Нет ready handle при отказе/отмене. Native exception/key/URL payload не копируется в результат; собственное логирование Unity ResourceManager остаётся у Unity/приложения.

Каждый Load получает собственный Addressables claim; runtime может разделять underlying operation. Отмена одного waiter публикует Cancelled, owner продолжает наблюдать физическое completion и освобождает поздний невыданный ресурс. Нет автоматического retry, каталожного update или дублирования F19 transport/store/cache.

PreloadAsync<T>(IEnumerable<Guid>, CancellationToken) сначала материализует входы, затем de-duplicates AssetId в порядке первого появления и запускает выбранные Load. Результат владеет успешными handles. Empty → Completed; все Loaded → Completed; часть Loaded → Partial; ни одного Loaded и есть Cancelled → Cancelled; иначе Failed. Items сохраняют индивидуальные outcomes. Чтение Items не передаёт ownership; отдельный Load получает отдельный claim. DisposeAsync preload освобождает успешные handles.

DisposeAsync клиента запрещает новые Load, отменяет pending waiters и ждёт native cleanup. Ready handles принадлежат приложению и переживают shutdown. Player loop должен работать до завершения cleanup; Wait/Result на frame не применяется. При ручном Instantiate приложение удерживает prefab load handle до уничтожения всех instances; InstantiateAsync/ReleaseInstance не входят в этот API. Consumer сохраняет свои closed generic load types для stripped Player. Native lifecycle основан на [официальном контракте handles](https://docs.unity3d.com/Packages/com.unity.addressables@1.22/manual/AddressableAssetsAsyncOperationHandle.html).

### 5.2. CM entity и asset providers после Demo

`IResourceLocator` и `IResourceProvider` живут в optional Unity.Addressables, а не DotNet. Entity provider получает pinned scope/snapshot и explicit exact generated binding, читает F19 typed store без собственного F13/Registry запроса; несколько versions одной schema не смешиваются по latest. Asset provider получает scoped AssetId и frozen descriptor, вызывает общий DotNet manager, ждёт existing job или возвращает согласованный NotReady; exact поведение/signatures — Q37. CM admission/Range/cache/retry/deduplication не реализуются второй раз в provider. Scheme/key/alias и class names из источника иллюстративны, разрешение и type matching требуют отдельного codec/fixture решения.

Для изображения результатом может быть Texture2D с main-thread decode/destroy; Audio требует явного поддержанного converter и lifecycle. Большой MP4 возвращается как абстракция `CachedAssetFile`/verified file lease, consumer передаёт локальный путь в VideoPlayer.url и удерживает handle до Stop/окончания playback. `LoadAssetAsync<VideoClip>` для raw MP4 не обещается; файл не загружается целиком в byte[]. Release освобождает operation/resource/consumer pin, а retention/GC решает общий manager. Один waiter cancel не удаляет job, нужную другому consumer; memory resource reference counting не равно filesystem job/lease ownership.

Exact user-facing models/requiredness/API и supported closed generic GeneratedType/Texture2D/file types пока Q37; generic DTO loads, converters, serializers/stripping/AOT roots проверяются в Windows x64 IL2CPP Player 2022.3.62f3. Custom provider declaration или Editor smoke не являются proof. Native AssetBundle/mapping update и prefab instance lifecycle остаются Q29, optional package combination — Q32.


## 6. Данные и владение состоянием

### 6.1. Состояние

Приложение/content pipeline владеет готовностью локального catalog и входным набором mappings. SDK копирует mapping при создании snapshot; client владеет pending operations до handoff либо cleanup. После Loaded владельцем handle становится вызывающий код; PreloadResult владеет своим успешным поднабором. Native reference counting и bundle cache принадлежат Addressables, а CM inventory/SQLite/HTTP — F19.

### 6.2. Модели

| Модель / поле | Тип | Обязательность и смысл |
| --- | --- | --- |
| AddressableAssetInfo.AssetId | Guid | Nonempty CM identity; duplicate AssetId в snapshot запрещён |
| AddressableAssetInfo.Key | string | Nonempty/non-whitespace native key; регистр и пробелы сохраняются; одинаковый key у разных AssetId допустим |
| AddressableMappingSnapshot.ContentVersion | string | Nonempty/non-whitespace opaque content context |
| AddressableMappingSnapshot.Assets | IReadOnlyList<AddressableAssetInfo> | Defensive copy; null entries/collection запрещены; пустой набор допустим |
| AddressableLoadResult<T> | AssetId, ContentVersion, Outcome, Handle, Error | Loaded содержит handle; Cancelled/Failed не содержат ready handle |
| AddressablePreloadResult<T> | Outcome, IReadOnlyList<AddressableLoadResult<T>> Items | Owned successful claims и индивидуальные результаты |
| AddressableAssetHandle<T>.Value | T : UnityEngine.Object | Доступ после Dispose даёт ObjectDisposedException |

Невалидные configuration inputs дают ArgumentException/ArgumentNullException. Missing mapping при Load — typed failure, без backend Registry lookup и latest fallback. Manifest schemaVersion/assets wire encoding/remote limits остаются будущим контрактом Q29; JSON parser/TextAsset provider в текущей поставке отсутствуют.

## 7. Действия и клиентские потоки

| Действие | Начальное состояние и запуск | Исполнитель | Обработка / серверная операция | Результат в интерфейсе |
| --- | --- | --- | --- | --- |
| OP01. Load | Приложение выбрало scoped entity/AssetId/тип/контекст | B07 либо B02 через B03/B05 | §5.2 CM reader/manager; §7.1 native mapping branch | Typed object/managed file/resource handle либо отдельный outcome |
| OP02. Явный Preload | Приложение выбрало нужный/скоро нужный набор | B02 | §7.2; metadata не запускает операцию | Учтённые результаты/ownership по Q29 |
| OP03. Release | Завершено использование переданного handle | B04/согласованный owner | Освобождение соответствующей Addressables загрузки, локально | Владение завершено по контракту |
| OP04. Отмена/уход | Waiter отменён, контекст сменился/потребитель уничтожен | B01/B02 | Guard результата + сохранение operation ownership; cleanup Q29 | Нет чужого late Value и обещания physical cancel |
| OP05. Mapping/content update | Новый платформенный набор; trigger Q29 | B03/B06/B05 | §7.3; URL/update API не принято | Согласованный переход после выбора политики |

### 7.1. Load для native Unity-built контента

1. B01 передаёт AssetId, ожидаемый тип и cancellation context; место/момент вызова выбирает приложение.
2. B02 разрешает ID через B03. Missing mapping — отдельный отказ без fallback на AssetId как key или обращения к backend за Unity key.
3. B02 использует соответствующий catalog/content и запускает B05 по key. Snapshot неизменяем весь lifetime клиента; приложение заранее готовит совместимый local catalog. Concurrent update не входит в Demo.
4. После успеха B02 выдаёт управляемый handle, передавая ownership по согласованному контракту. До успеха готового Value нет.
5. При fail/cancel/late-unused result B02 отвечает за созданную операцию до cleanup либо явной передачи; concrete protocol Q29. Resource outcome отдельно от data sync.

В native Unity-built ветке повторная загрузка не утверждает новую shared-handle policy; raw CM provider использует single-flight job общего F19 manager. Ownership/reference counting нескольких потребителей сначала согласуется, затем проверяется на выбранной версии. Уход одного waiter не назначает физическую отмену общей операции.

### 7.2. Явный Preload и частичный исход

1. B01 выбирает нужный/скоро нужный набор AssetId. Получение metadata/list не инициирует preload всех ресурсов.
2. B02 разрешает mappings и загружает набор по выбранному контракту. Ordering, duplicates, empty set, concurrency, limits/progress — Q29.
3. Каждый созданный operation/resource остаётся у учтённого владельца. Сбой/отмена элемента не теряет успешные элементы и не превращает partial в готовность всего набора.
4. **Уточняется Q29:** partial outcomes с передачей successful handles либо cleanup не переданного набора; lifetime preload references, дальнейший Load/reuse и release. Не выбирать fail-fast/retain/rollback по примеру `await PreloadAsync`.
5. Отмена ожидания учитывается отдельно от завершения остальных operations; physical cancellation проверяется runtime.

### 7.3. Mapping и content update

Local manifest поставляется с клиентским build/content без отдельного manifest network call. Источник предусматривает последующий remote flow: получить manifest из выбранного расположения → проверить формат → сохранить локальное представление → использовать mappings. Это не offline-гарантия для всех bundles и не remote как обязательный первый выпуск.

Unity Editor → Addressables build → catalog/bundles/dependencies → S3/CDN → Unity runtime остаётся платформенным pipeline. Формат manifest и набор mappings версионируются отдельно. URL/source, связь contentVersion/catalog, atomic transition, invalid/failed update fallback, прошлый набор и in-flight/ready handles **уточняются Q29**. До решения нельзя незаметно менять mappings или обещать сохранение/сброс прежних ресурсов. F13/F19 data sync не становится catalog update протоколом.

## 8. Использование API и NSwag

NSwag CM web client не применяется: F23 не вводит web формы/CM routes. Load/Preload/Release — клиентские операции add-on, не серверные DF. Отдельная backend F23 не требуется; CM сохраняет AssetId/HTTP-контракт.

| Потребность / поток | Серверная фича и операция | Входные сведения | Используемый результат | Владелец вызова |
| --- | --- | --- | --- | --- |
| Данные с AssetId до OP01/OP02 | [Backend F13](../../backend/features/F13-content-delivery.md), delivery/sync; оркестрация [F19](F19-unity-client.md#8-использование-api-и-nswag) | F13/F19 context, не Unity key | Данные + универсальные AssetId | F19, не B02 |
| Обычный raw file/image | [F05](../../backend/features/F05-media-assets.md), F13/F19 asset boundary | AssetId; exact HTTP у server owners | Metadata/file/Stream, raw helper | F19; не скрытая F23 dependency |
| OP01/OP02 | Нового F23 CM API нет; raw provider использует F19 CM gateway | Scoped entity binding/AssetId либо native mapping key | Typed entity/resource/file lease | B07/F19 либо B02/B03/B05 |
| OP05 remote | CM endpoint не выбран; platform источник [Q29](../questions/Q29-unity-client-package.md#f23-mapping-snapshot-и-ownership-операций) | Location/version context — Уточняется | Manifest/catalog/bundles | Pipeline/add-on по согласованной схеме |

Не создаются копии delivery DTO/OpenAPI models, permissions, headers, ETag/If-Match или auth механизм. Protected runtime access вне Demo принадлежит F13/F05; private package access Q32 относится к developer/CI installation. Add-on не заменяет server validation. [S06](../standards/S06-api-client.md) применим к отделению transport/ошибок, без автоматического переноса web generation на Unity.

## 9. Состояния, ошибки и доступ

Это resource/API outcomes, а не обязательные экраны. Приложение выбирает представление; concrete exception/result/progress types Q29.

| Ситуация | Что получает потребитель | Доступные действия | Данные / ввод |
| --- | --- | --- | --- |
| Load выполняется | Нет ready Value; progress по контракту | Ожидать/cancel context | AssetId/тип/context сохранены |
| Load успешен | Handle ресурса | Использовать/освободить | Ownership передан; data sync отдельно |
| Missing mapping | Mapping error add-on | Исправить/обновить по принятой политике | Нет случайного key/fake ready |
| Invalid/unsupported manifest | Format/manifest error | Update/fallback Q29 | Не принять неизвестный формат как валидный |
| Key/catalog/type/load failure | Resource error с этапом | Повтор по классификации Q29 | Installed данные F19 не откатываются |
| Failed remote update/download | Отдельный failed outcome | Повтор/fallback Q29 | Новый content не готов; previous snapshot Q29 |
| Partial Preload | Не full success; успешные элементы учтены | Retain/transfer/cleanup по Q29 | Ownership каждого operation/resource сохранён |
| Waiter отменён/потребитель ушёл | Cancel либо late-unused result по контракту | Cleanup/completion у owner | Не применять Value к новому context; physical cancel не обещан |
| Release/instance misuse | Поведение/error Q29 | Принятый lifecycle контракт | Handle и instance не один lifetime |

Mapping/manifest/Addressables failures живут в add-on/Unity слое, не DotNet universal contracts. Диагностика содержит безопасный AssetId, этап/описание; tokens, secret URLs и sensitive payload не логируются. Callback thread/main-thread transitions и release API проверяются выбранной матрицей; Core DTO не превращаются в MonoBehaviour/ScriptableObject.

Кнопки/поля/keyboard/focus [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback)/[S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics) не применяются: UI не вводится. Функциональная доступность интеграции — понятные программные операции/исходы и документация lifecycle. Экран приложения имеет собственного UX-владельца; F23 не выдумывает controls.

## 10. Проверки и приёмка

Architecture/adapter checks additionally prove: mapping interfaces/runtime adapter stay in optional assembly; real LoadAssetAsync/Completed/Release exercised via adapter; main-thread execution/no blocking; reuse F19 dispatcher without HTTP/store duplication; cancelled waiter retains operation cleanup owner; actual supported generic T/AOT preserved. Exact public/load/Preload/update policies Q29 must be decided before their runtime assertions; component names alone not implementation proof.


Матрица ниже включает future проверки по [S08](../standards/S08-testing.md); фактически выполненный local scope отдельно зафиксирован в §10.2. Непринятый ответ Q29 нельзя назначить expected result: сначала контракт, затем проверка.

| Проверяемый блок / сценарий | Исходные условия и воздействие | Наблюдаемый результат | Граница проверки |
| --- | --- | --- | --- |
| B03 parse/resolve | Свои local fixtures: known/missing ID, invalid/unsupported format/versions | Правильный key и раздельные ошибки без AssetId-as-key | Изолированный parser/provider; не runtime загрузка |
| B02 Load | Mapping/runtime doubles: success/failure/late result | Mapping передан правильно, handle после успеха, owner учтён | Unit coordinator; не real reference counting |
| B04 Load/use/Release | Свой built asset, реальные Unity/Addressables выбранной версии | Рабочий ресурс и реальное освобождение по контракту | Unity integration с runtime/handle, не mock call count |
| Repeat/concurrent Load | Два потребителя одного ID, release/cancel одного | Native operation ownership/reference counting — Q29; CM raw file single-flight — F19/Q37 | Actual native runtime и общий manager; один waiter не разрушает нужную другому job |
| Explicit/partial Preload | Выбранный поднабор широкого metadata; fail/cancel после частичного успеха | Только выбранные ресурсы; partial не full success; handles переданы/очищены по контракту | Doubles outcomes + real Unity cleanup; policy Q29 |
| Cancellation/late-unused | Реальная начатая загрузка, cancel waiter/смена context/уничтожение consumer | Late Value не применён чужому context; owner завершает/очищает; physical cancel отдельно | Unity lifecycle/runtime; cancelled Task не доказательство release |
| Handle vs prefab instance | Реальный prefab и instance; разные допустимые окончания scopes | Оба lifetime/release соответствуют конкретному Q29 | Unity integration после решения, не архивный короткий using |
| Snapshot/update | Два собственных mapping/catalog набора, старая Load/ready handle, failed remote update | Без смешения несовместимых наборов; transition/fallback/lifetime по Q29 | Pipeline + real remote/runtime, не только parser unit |
| Error isolation | Successful F19 sync, затем mapping/key/download fail | Installed data сохранены, separate resource error, без false ready | SDK integration; durable guarantees проверяет F19 |
| Package boundaries/install | Clean Unity 2022.3.62f3 сначала без add-on, затем exact dependencies | Основной SDK/raw работает сам; add-on без duplicate DotNet | Static + real install/build Q32; publish отдельно от install |
| Optional E2E | Реальный CM со своим data/asset, public F19, свой Unity build/mapping/catalog/bundles | CM AssetId ведёт к правильному ресурсу и освобождению | CM → SDK → Unity runtime; mocks/local prefab не замена цепочки |

Fixtures принадлежат проверке: свои mappings/AssetId/data/content build; cleanup только их. Test framework/harness, limits/timeouts/platforms не выбираются текстом. Remote E2E и ownership tests выполняются после Q29/Q32; raw F20 E2E остаётся отдельным и не требует F23.

Source evidence 05.10.2026: в `contentmanager.api/src` `.cs/.csproj` вне `bin/obj/Migrations` и `contentmanager.ui/src` `.cs/.csproj/.razor/.json` вне build directories не найдены `Sdk.Client.DotNet`, `Sdk.Client.Unity`, `IAddressableMappingProvider`, `AddressablesClient`, `IAssetHandle`, `com.unity.addressables`. Это поиск в ограниченных checkout paths, не отсутствие SDK во всех репозиториях. Server Guid asset IDs не утверждают готовый SDK AssetId wrapper.

В документационной задаче проверяются headings/ссылки/scope/ownership/diff. Код SDK/Unity не изменяется. Unity Editor/Player, runtime reference counting/cancellation, CM E2E, package publish/install/build и application tests не запускались. Текст не объявляет add-on реализованным.

### 10.1. Проверки новых CM providers

На real packaged Unity 2022.3.62f3 Windows x64 IL2CPP: typed generated DTO из exact pinned SQLite; missing/type/schema-version mismatch без latest fallback; image cache miss/hit через CM и main-thread cleanup; raw MP4 local VideoPlayer.url без file-size-proportional memory; Release после Stop снимает pin, GC не удаляет используемый файл; concurrent launcher preload/app load join одной manager job; cancel одного waiter не губит второго; cache/authorization failures не откатывают entity snapshot. Native bundles/prefab отдельно проходят собственную acceptance. Используются actual SQLite/provider/Addressables package и CM gateway; эти проверки ещё не выполнены.

### 10.2. Фактическая проверка локального Demo

В contentmanager.sdk.client.unity реализован source UPM add-on с .asmdef/.meta/link.xml; .csproj остаётся внешним Rider/host compile проектом. Generic NuGet 1.0.0 не пересобирались. Add-on не содержит SDK/Newtonsoft/native DLL; PackageAssembler поддерживает отдельные addon/embed/pack операции с валидацией зависимости, source-only состава и metadata. Существующие base package операции сохранены.

11.10.2026: **63 xUnit passed**, solution Release build **0 warnings/errors**; пять новых real Addressables PlayMode tests прошли с фактически собранными локальными catalog/AssetBundles. Проверены mapping/key/type failures, concurrent independent claims, waiter cancellation, partial/duplicate/empty preload, worker dispatcher, prefab instance scope, сохранение ready handle после client shutdown и освобождение последнего native claim. Read-only review не выявило важных lifecycle/packaging дефектов.

Профиль тестового проекта: **Unity6000.6.5f1 / Addressables2.11.2**. Зависимость source add-on — Addressables1.22.3 minimum; exact consumer versions фиксируются manifest/Unity lock. Addressables1.22.3 действительно восстановлен, но не компилируется на установленной Unity6000.6.5f1 из-за запрещённого GetInstanceID; его исходники не исправлялись. Это не доказательство его runtime на 2022.3.

Полный PlayMode прогон без CM: **8 passed, 0 failed, 2 skipped**; opt-in F19 real-CM tests требуют своей fixture. Обязательный default CI выбирает остальные PlayMode tests и сохраняет strict Passed/zero-failure/zero-test gate; дополнительно требует все пять Addressables tests без skip. CI готовит local content через отдельный Editor batch; ps1 не добавлялись. Actual pipeline/publication/registry install не выполнялись.

EditMode suite: **4 passed, 0 failed, 0 skipped**. Default PlayMode gate без opt-in CM tests: **8 passed, 0 failed, 0 skipped**.

Target **Unity2022.3.62f3 Windows x64 IL2CPP Player** остаётся открытым; установлен только Unity6000.6.5f1. Editor/host checks не доказывают AOT/stripping и target Player compatibility. Source API/политики локального выпуска реализованы, полная F23 acceptance не объявляется закрытой. Собственные Editor процессы завершаются после тестов; backend/контейнеры этому local native этапу не нужны.

## 11. Последовательность изменений и открытые вопросы

1. Local Demo contract выбран и реализован (§5.1/§10.2). Продолжать target Player acceptance; remote/custom-provider scope остаётся отдельно Q29/Q37.
2. Зафиксировать модели/errors/public boundaries и package/asmdef dependencies без Unity knowledge в DotNet и обязательной зависимости F19/F20.
3. Подготовить Unity pipeline/local mappings, provider/Load с ownership/Release и main-thread lifecycle.
4. Реализовать explicit Preload по принятому partial/cancellation контракту; проверить real resource/concurrent consumers.
5. Подключить remote update только в выбранном объёме после snapshot transition/fallback/in-flight решения.
6. Проверить runtime matrix, clean install Q32 и optional E2E. Документация API/errors/lifetime входит в результат, но не доказывает runtime.

| Вопрос | Какие детали блокирует | Что уже принято |
| --- | --- | --- |
| [Q29. F23: mapping snapshot и ownership операций](../questions/Q29-unity-client-package.md#f23-mapping-snapshot-и-ownership-операций) | Future manifest codec/catalog update/cache и remote policies; local API/ownership выбран в §5.1 | Optional, AssetId ≠ key, pipeline, explicit Preload/release, waiter cancel ≠ physical cancel |
| [Q32. Матрица поставки add-on](../questions/Q32-sdk-package-distribution.md#f23-матрица-поставки-add-on) | Actual Addressables/dependency combination, versioned additional platform/Player/stripping/native/registry proof | Assembly Sdk.Client.Unity.Addressables / UPM com.leansoft.contentmanager.unity.addressables selected; optional separate cycle with exact supported base SDK set, no reverse base dependency; first base Windows x64/IL2CPP |
| [Q15](../../backend/questions/Q15-api-and-source-code.md) / [Q21](../../backend/questions/Q21-media-storage.md) | Asset metadata/file HTTP details у server owners | F23 не добавляет Unity keys/routes; raw F05/F13/F19 отдельно |

Полные вопросы/ответы хранятся у Q-владельцев. Routes/signatures/versions/defaults/limits/update policies остаются «Уточняется» и не восстанавливаются по иллюстративным snippets архива.
