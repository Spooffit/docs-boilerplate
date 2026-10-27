# F19. Unity-библиотека клиента CM


[Модуль M05](../modules/M05-delivery.md) · [Клиентская спецификация](../../technical/frontend/features/F19-unity-client.md) · [HTTP-доставка F13](F13-content-delivery.md).


## Цель и участники


Команда CM создаёт самостоятельную подключаемую библиотеку для Unity-разработчиков. Взаимодействие должно быть максимально простым и привычным для них: библиотека скрывает последовательность HTTP-вызовов, работу с версиями и выполнение синхронизации, сохраняя возможность расширять и заменять отдельные слои.


Библиотека обязательна для Demo. Целевая версия Unity — **2022.3.62f3**, первая платформа Windows x64 с реальной IL2CPP Player приёмкой. Сцена остаётся самостоятельной [F20](F20-unity-demo-scene.md), использующей публичный API F19. Для использования библиотеки сцена не требуется; разделение фич не определяет число репозиториев.


## Целевая модель SDK


SDK автоматически оркестрирует синхронизацию, использует типизированные модели из схем, отделяет общий .NET-клиент от Unity-платформенного слоя, поддерживает потоковую загрузку assets, прогресс, отмену и настраиваемые повторы.


Связанные архитектурные материалы сохранены в [архитектуре SDK](../../archive/unity-client-sdk-architecture.md) и [описании Schema Registry / Code Generation](../../archive/entity-schema-codegen-architecture.md); нормативные требования находятся в текущих F/Q-документах. Незакрытые технические детали остаются в Q29/Q31/Q32.


## Границы SDK и контрактов


Общий .NET/C# runtime SDK называется **`Sdk.Client.DotNet`**. Unity-библиотека остаётся платформенным слоем над ним; F20 — отдельная демонстрационная сцена.


Общими для любых клиентов являются OpenAPI, JSON Schema и Schema API, а не C#-библиотека. Другие языки могут иметь независимые SDK на этих контрактах без зависимости от DotNet. Перечень будущих языков не добавляет новые реализации в Demo; высокоуровневая синхронизация, клиентское состояние и ранее согласованные гарантии остаются прежними.


## Основной сценарий


1. Разработчик подключает пакет и настраивает CM, используя предусмотренные программные настройки и Unity-конфигурацию.
2. Клиентская сторона хранит выбранные схемы/точные версии и фактически установленные версии экземпляров. Допускаются несколько версий одной схемы.
3. Приложение вызывает высокоуровневую синхронизацию. Оно не обязано самостоятельно выстраивать последовательность запросов плана и данных.
4. Библиотека получает клиентское состояние, запрашивает у CM план Add/Update/Remove, загружает необходимые точные версии и обновляет локальный набор.
5. Приложение получает итог и использует типизированные данные. Успешные целые группы установлены; общий частичный результат явно различим. После полного успеха SDK проверяет актуальность; при изменениях сообщает приложению, которое выбирает следующий запуск с новым планом.
6. Приложение при необходимости запрашивает конкретные assets через SDK. Для обычных изображений Unity-слой предоставляет удобное получение Unity-ресурса.
7. В течение операции доступны прогресс и отмена. При временных сбоях действует поведение повторов по умолчанию, которое разработчик может настроить.


Момент синхронизации выбирает вызывающее приложение. Старт приложения, кнопка и таймер не становятся обязательными правилами библиотеки.


## Уровни ответственности


| Уровень | Результат для потребителя | Граница |
| --- | --- | --- |
| Общий Sdk.Client.DotNet | Neutral client/transport/store/clock/logger interfaces и общая sync/protocol/integrity/typed-read/raw реализация | Platform adapters реализуют interfaces; DotNet не знает Unity/HttpClient/SQLite native |
| Клиентская координация синхронизации | ContentManagerClient/SynchronizationCoordinator выполняют один полный вызов | Portable DotNet implementation: durable intent/recovery/fences/group apply/receipt/freshness; server desired/diff не дублируется |
| Sdk.Client.Unity | UnityWebRequest transport, main-thread dispatcher/logger/config/image helpers | Реализует DotNet ports и делегирует coordinator; no own HTTP diff/state machine |
| Необязательный add-on | Работа с Addressables через собственное сопоставление и жизненный цикл ресурсов | Отдельная [F23](F23-unity-addressables.md), не условие получения обычной картинки |
| Schema tooling | Генерация C#-моделей до runtime | [F21](F21-schema-registry.md) / [F22](F22-csharp-model-generation.md), не скрытая операция каждой синхронизации |


Sdk.Client.Http реализует IHttpTransport через обычный .NET HttpClient; Sdk.Client.Unity реализует тот же интерфейс через UnityWebRequest/DownloadHandlerScript, Sdk.Client.Store.Sqlite реализует IClientStateStore. Детали компонентов, stream ownership, cancellation, bounded buffers/worker scheduling/SQLite tables и composition находятся в [клиентской F19](../../technical/frontend/features/F19-unity-client.md#53-интерфейсы-dotnet-и-конкретные-реализаторы). DotNet содержит общие algorithms вместе с interfaces; зависимости на native/platform implementations обратным ходом не вводятся. Другие языки/движки не добавлены в Demo.


## Данные и локальное состояние


Клиентская сторона хранит полный selection и actual installed state. SynchronizeAsync получает полный exact selection приложения; binding registry описывает supported generated types и не выбирает данные. SDK сохраняет original selection/recovery context и выводит inventory из committed store. Выбран replaceable store operation contract и SQLite как первый готовый adapter после platform spike Q29/Q32. Durable stage+cursor, atomic group+index+inventory+receipt, Unknown commit reconciliation обязательны; in-memory не обеспечивает restart recovery.


Данные экземпляров доступны как строго типизированные C#-модели, созданные по выбранным версиям схем через F21/F22. Такие модели не добавляют автоматически отображение новых полей или новую игровую логику. Generic-представление по ключам как дополнительный обязательный публичный режим отдельно не утверждено.


## Assets


Stable media identity — AssetId GUID. Runtime AssetReference — frozen five-field descriptor F04; CM-owned URL строится из configuration+AssetId и не serialized member. DotNet raw/HEAD/destination/file API и explicit Range выбран F19/Q29; expected descriptor проверяет size/SHA, ID-only не заявляет Published integrity. Unity helper отдельно создаёт owned resource; private storage topology и UnityEngine-типы не появляются в общем descriptor.


Получение метаданных и загрузка тяжёлых ресурсов разделены. Слова «скачать необходимое при синхронизации» не означают автоматически загружать все assets всех экземпляров. Sync данных заканчивается после обновления экземпляров и их AssetReference identities/descriptors; тяжёлые bytes загружаются отдельным API по решению приложения; его ошибка не отменяет успешный commit данных.


Raw Stream/destination/file и Unity resource имеют отдельные outcomes/owners. Opened не скачан/Verified; partial range не full SHA proof. Caller destination сохраняет owner/position, incomplete output не ready. File helper own temp+verify+no-overwrite move. LoadTextureAsync full verified JPEG/PNG -> owned UnityTextureHandle, main-thread create/decode/destroy, cancel before handoff SDK cleanup, after Ready consumer Dispose. Demo no extra SDK cache/dedup/persistent asset resume; download/decode failure не отменяет committed data sync.


## Launcher и общий media cache после Demo

Digital Space Launcher выполняет entity sync через DotNet и устанавливает согласованный snapshot в общем SDK-managed SQLite; Unity app читает закреплённые сущности и сама запрашивает отсутствующие media по защищённому CM HTTP API. Snapshot/bootstrap не является credential; origin CM/YARP остаётся внешней границей, S3/MinIO не раскрывается. Launcher заранее загружает только ссылки с schema mode Preload/RequiredBeforeLaunch, не весь MediaLibrary. Режимы определены [F01](F01-create-entity-schema.md#режим-доставки-assetreference-после-demo), связь с разрешённой ревизией — [F13](F13-content-delivery.md#launcher-и-selective-preload-после-demo).

Общий download/cache coordinator находится в `Sdk.Client.DotNet`; platform transport/file/SQLite adapters реализуют порты. Launcher и Unity работают через один механизм, а optional Addressables не заводит отдельную загрузку, кэш или F13 state machine. SQLite хранит entities/snapshots и persistent media jobs/cache index; 5–10 ГБ bytes хранятся файлами **вне заменяемой папки билда ProjectStorage**, не SQLite BLOB. Exact paths, ACL, native provider, process locks/leases и таблицы — инженерные решения [Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md), не готовый DDL.

Для больших файлов приняты bounded-memory transfer, HTTP Range/If-Range resume, 64-bit counters, persistent checkpoints и full file size/SHA-256 verification. Downloading/Paused/Verifying/Ready различаются: partial output не выдаётся как готовый ресурс. `Ready` означает verified bytes. Media job не привязывается к F13 plan TTL. Persistent recovery сохраняет доброкачественные bytes при обрыве; доступ к дальнейшему GET требует действующего разрешения. File publication и SQLite commit не одна транзакция, поэтому необходима проверка после crash.

Рекомендованный SDK механизм объединяет конкурентных потребителей одной immutable asset identity в single-flight job; отмена одного не уничтожает работу других. Launcher — writer entity snapshots, оба процесса могут инициировать media jobs через контролируемые SDK-порты. File lease защищает используемые bytes от eviction; конкретные locking/fencing/recovery/GC/quotas и fairness **Уточняется Q37**. Shared cache не объединяет entitlement разных Variants. Launch gate учитывает verified RequiredBeforeLaunch ссылки одного закреплённого snapshot; optional failure сохраняет entity commit. Запуск старого snapshot при неготовности нового и offline/revocation не определены.

Runtime session provider должен отделять происхождение credentials от HTTP transport; способ нового admission после default абсолютного TTL 30 минут, в том числе без живого Launcher, **Уточняется Q37**. No sliding/refresh, secret в URL/build и автоматический fallback этим решением не вводятся.

Будущая приёмка интеграции: два отдельных Windows x64 IL2CPP процесса Unity 2022.3.62f3, общие SQLite/files, concurrent same-asset requests, interruptions/process kill, Range 200/206/416 и validator changes, full size/hash failure, session expiry/denial, disk full, required launch gate и optional failure без rollback entities. Literal provider/API names, numeric limits и реальные результаты остаются открытыми. Большой persistent media cache/selective preload относятся к этапу после самостоятельного Demo.

## Подключение и доставка пакета


Выбран отдельный SDK repository: DotNet DLL в UPM и NuGet, Unity source bridge, отдельный Store.Sqlite adapter, базовые пакеты одним immutable release train. Self-hosted GitLab project-level registry — основной путь, release tarball того же build — первый fallback после подтверждённой несовместимости. F20 — отдельный packaged consumer Unity project в SDK repo; small Samples~ examples optional. First acceptance Player — Unity 2022.3.62f3 Windows x64/IL2CPP, other platforms separately proven. В исходном документе отдельно зафиксирована необходимость проверить совместимость **конкретного GitLab и Unity UPM**, включая установку точной версии, discovery, private access и зависимости. Это ещё не подтверждение работающей инфраструктуры. Детализация — Q32 и клиентская F19.


К библиотеке относятся инструкция подключения, программный пример, описание публичного API, ошибок, отмены, повторов и границ поддержки. Визуальный демонстрационный проект — F20. Версии самого SDK не смешиваются с версиями схем или данных.


## Demo и дальнейшее развитие


Demo включает библиотеку, высокоуровневую синхронизацию, выбранное версионирование, изображения, прогресс/отмену и настраиваемые повторы. Вход, Auth, лицензии, шина и VR School, встречающиеся в общей схеме SDK, относятся к дальнейшему применению, а не возвращаются в Demo.


Генерация моделей включена в Demo отдельной фичей F22. Addressables является optional и не блокирует базовый raw-file сценарий. Launcher integration/selective preload и расширенный persistent media cache относятся к последующему этапу; точное распределение инженерной реализации по milestone ещё Q37. Полный offline-first и patching файлов не становятся требованиями Demo; автоматическая загрузка всех assets не является общей политикой SDK.


## Проверяемое окружение Demo

Первый Player: Windows x64/IL2CPP Unity 2022.3.62f3. Build settings/native/package locks и backend окружение фиксируются до spike, совместимость проверяется фактически. Representative fixture и экран F20 согласованы Q26: все 15 типов, две exact SchemaVersion, nested Object/List/List<List> и реальное изображение; initial sync, Add/Update/Remove, offline committed read/restart/resume, lost ACK, Stop/Discard, Withdrawal и post-success freshness. Успех только в Editor не подтверждает Player. Exact параметры и runtime proof остаются engineering work; это не расширяет Demo на несколько платформ или F23.

## Приёмка


| Сценарий | Ожидаемый результат |
| --- | --- |
| Библиотека подключена без F20 и Addressables | Базовые данные и изображения доступны через публичный API |
| Один вызов синхронизации | План, необходимые загрузки и применение выполняются библиотекой, а не вручную повторяются в сцене |
| Две версии одной схемы | Представления не вытесняют друг друга по одному InstanceId |
| Данные читаются в generated model | Модель соответствует выбранной структуре; новый Draft не подменяет опубликованную |
| Операция выполняется | Потребитель получает прогресс и может отменить её |
| Настройки retry изменены | Используется выбранная конфигурация; default не является неизменяемым скрытым поведением |
| Сбой загрузки/применения | Нет ложного успеха и ложного обновления installed inventory |
| Получена сущность со множеством AssetId | Само чтение сущности не требует загрузить каждый файл |
| Приложение не синхронизируется при старте | Это допустимый выбор потребителя |


Проверки являются критериями будущей реализации, а не отчётом о написанной библиотеке.


## Оставшаяся детализация


[Q29](../../technical/frontend/questions/Q29-unity-client-package.md#public-sdk-и-локальный-store-согласованный-контракт) закрывает public SDK/store contract: typed result/progress, handle-bound controls, detached committed reads, scope/source/partition, atomic activation и format. Expected failures сохраняют committed группы; отмена main call = Stop, Discard explicit. Открыты provider/native/platform/lock/durability checks, numeric policy/error registry и фактическая raw resource conformance. F19 frontend — owner models; Q30/Q33 server protocol, Q31 codegen, Q32 реальная поставка/Player. Документация не утверждает реализацию.


## Реестр схем, группы и восстановление


Один вызов синхронизирует весь реестр точных схем/версий данного клиента. Обновлять лишь выбранную для одного вызова часть реестра сейчас не требуется. Реестр в Demo задаётся разработчиком; последняя рабочая форма названия будущего декларативного файла — `content-manager.json` (ранее в codegen-источнике `content-schemas.json`). Автоматизация этого файла относится к production workflow, не является условием Demo; не создавать два независимых источника выбора.


Отказ от версии схемы удаляет принадлежащие ей локальные представления из управляемого набора. Серверный контент и не относящиеся к SDK данные приложения не удаляются.


Получение плана и данных разделено. SDK собирает полный результат каждой точной schema/version в staging, затем атомарно применяет её Add/Update/Remove и обновляет inventory. Незавершённая dogV4 остаётся прежней, даже если hallV2 уже обновлена. Получить только дельту допустимо: неизменённые записи не обязательно скачивать снова.


Sdk.Client.DotNet создаёт уникальный synchronizationId и передаёт его в API; прикладной код не обязан видеть его как необходимый параметр. Продолжение сохраняет ID и проверяет пригодность прежнего контекста/checkpoint. При следующем вызове автоматически продолжается пригодная попытка, включая после закрытия приложения. При установленной невозможности resume автоматически создаётся новая операция от фактического состояния; успешные группы не теряются.


Принят 409 для конкурентного активного дубля того же ID. Разделение Start/Resume/Status уточняет контракт, не задаёт готовые URI. Для одного локального набора второй прикладной вызов теперь отклоняется как «уже выполняется» без второго ID. Продолжение и новый запуск при невозможном resume автоматические; DotNet не содержит обязательного окна.


Действует Start JSON metadata → один resumable streaming HTTP data response → отдельные group commit requests. SDK инкрементально разбирает records последовательных exact групп, durably stages их до application cursor, валидирует целую группу и атомарно фиксирует data/removals/inventory/local receipt. Progress вычисляется локально, отдельного SSE канала нет. HTTP/3 предпочтителен; переход на HTTP/2 сохраняет прежний ID/plan/checkpoint. [Q33](../../technical/backend/questions/Q33-sync-resume-transport.md) хранит эти решения и необходимость фактической проверки транспорта; технологии не объявлены протестированными.


Полноценное offline-first не вводится, однако пригодное продолжение требует хранить состояние операции и подготовленные части. Сохранность после завершения процесса обязательна; технология ещё уточняется. Остановка сохраняет продолжение, окончательный отказ удаляет незавершённое служебное состояние. Начатый commit можно закончить и затем остановиться; половина обновлённой группы запрещена. Правила Q33.


Read-путь: DotNet/Unity → canonical CM-owned `/storage/assets/{assetId}` → Host YARP → private MinIO; upload/metadata/lifecycle отдельно через CM Application API. Optional Addressables сохраняет отдельный платформенный mapping/catalog/bundles pipeline, не требует direct S3 у базового DotNet. Наличие серверного POST assets не означает требования всем runtime-клиентам загружать файлы.


Дополнительная приёмка: полный реестр и удаление исключённой пары; неделимость группы; честный частичный успех; обрыв и продолжение с прежним ID; конфликт активного дубля; изображение запрашивается после успешной sync данных. Это требования к будущим проверкам.


## Проверка актуальности и время жизни плана


На CM план находится в локальном кэше; default срока хранения — 1 час, параметр принадлежит backend. Клиент хранит свой устойчивый контекст восстановления, но не может продлить серверный срок собственными настройками. Если старый план уже недоступен, DotNet автоматически запрашивает новый от фактического inventory, не переписывая прежний ID.


После полного успешного выполнения исходного плана SDK вызывает проверку актуальности (preflight), затем сообщает, совпадает ли установленный состав с актуальным на момент проверки либо доступны изменения. Следующая sync с новым планом выполняется только по выбору приложения. Это не то же самое, что автоматический новый старт при невозможном resume. Ошибка проверки отдельно от успешной установки; точные методы и сообщения в технической F19/F13.


Приёмка дополнения: закрытие приложения и восстановление; cache miss/истечение; остановка и отказ; завершение начатого commit; второй прикладной вызов; post-sync актуально/изменения/неизвестно; отсутствие самопроизвольного повторного запуска после успеха; сохранение гарантий через HTTP/2. Это требования будущих тестов.




## Lookup по EntityInstanceKey


`EntityInstanceKey` сохраняется как stable per-schema runtime/tooling alias. Public typed store должен поддерживать удобный lookup без GUID, концептуально `TryGetByKey<T>(key, out value)`; generic/model context определяет schema, а key уникален внутри неё.


Это не отменяет хранения `EntityInstanceId`/`EntityInstanceVersionId` в installed inventory и sync protocol. Key нужен для developer ergonomics, а не заменяет техническую identity. `EntityInstance.Name` является authoring label и не требуется runtime SDK. Точное имя публичного метода остаётся Q29, пример не фиксирует сигнатуру API.
