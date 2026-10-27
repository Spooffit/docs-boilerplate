# F20. Экспериментальный Launcher и Unity-приложение: клиентская спецификация

[Бизнесовая F20](../../../business/features/F20-unity-demo-scene.md) · [SDK F19](F19-unity-client.md) · [F13](../../backend/features/F13-content-delivery.md) · [Q26](../../../business/questions/Q26-demo-mvp-scope.md#f20-экран-и-границы-показа).

## 1. Результат и границы

Уточнение пользователя 11.10.2026 заменяет прежний standalone samples/UnityDemo: работа выполняется в существующей экспериментальной копии DigitalSpaceLauncher. Launcher остаётся Unity-проектом в корне, DemoApplication — второй независимый проект в той же папке/Git; два самостоятельных Windows EXE. Пользователь отдельно разрешил Git initialization исходной копии без .git. Коммитов/remote/push агент не создаёт.

Цель — Launcher синхронизирует реальные Published entities staging CM через SDK F13 в общий SQLite и запускает отдельный Player. Приложение читает установленный content и отдельно загружает Image через SDK. Бэкенд F20 не добавляется, серверная авторизация не отключается. Неподготовленные URL/IDs/models и отсутствующие SDK capabilities остаются blockers.

Цель приёмки — Unity2022.3.62f3 Windows x64/IL2CPP. Доступный Unity6000.6.5f1/Mono используется только для диагностики. Нормативный target profile и фактически проверенный diagnostic profile разделены; обратная совместимость исходных Unity6/URP17 assets с2022/URP14 ещё не доказана.

## 2. Страницы, экраны и навигация

| Экран | Вход | Состав | Переход |
| --- | --- | --- | --- |
| Launcher | UserInterface.unity, старт без логина | Одна карточка CM Demo Application; Sync/Stop/Discard; stage/count/outcome/freshness/error; Launch и Exit | Launch открывает независимый DemoApplication.exe после проверки prerequisites |
| Каталог экспонатов | ExhibitCatalogue.unity, --cm-bootstrap | Прокручиваемый список Key/exact version; nested typed values; RawImage; отдельный image loading/error; Close | Закрытие освобождает SDK/native resources; новая публикация показывается после нового Launcher sync/launch |

Auto-sync отсутствует. DemoApplication не вызывает F13 sync. UI не содержит лицензирования, организации/админа, удалённого каталога/обновления/скачивания executable.

## 3. Карта клиентских блоков

FSD/Razor не навязываются Unity.

| Блок | Фактическое размещение | Ответственность |
| --- | --- | --- |
| Старт | Assets/Scripts/CanvasManager.cs | Сразу открыть каталог, coordinated shutdown |
| Каталог/карточка | Assets/Prefabs/ApplicationList и ApplicationItem | Одна локальная карточка, реальные SDK controls, повтор после ошибки |
| SDK orchestration | ApplicationLibraryController.cs | Register, Sync/State/Stop/Discard, readiness, confirmed bootstrap, release store before child |
| Process | ApplicationController.cs, Tools.Windows.cs | Distribution-relative executable, корректные args/working directory, один отслеживаемый child |
| Общие consumer contracts | Assets/Scripts/CmDemo | JSON config/bootstrap, общий path, typed SDK bindings; одинаковый код приложения в двух проектах, не копия SDK |
| Child | DemoApplication/Assets/Scripts/DemoCatalogue.cs | Только public SDK reader, проверка proofs, list/detail и owned image |
| Сборка | Assets/Editor/CmDemoBuild.cs каждого проекта, scripts/*.cmd | Реальные UPM archives, bindings из F22, два EXE; без PS1/hash reports |

Legacy Networking/DSLAPI/ProjectStorage file-sync/login/update код и неиспользуемые prefabs удалены после приватной временной резервной копии. Сцена/сохраняемые prefabs сохраняют GUID. Старые persistent callbacks очищены; необходимые ссылки проверяются NUnit/Editor.

## 4. Публичные зависимости

Оба проекта используют GitLab project-level npm/UPM registry `https://gitlab.alldirect.ru/api/v4/projects/127/packages/npm/`, scope `com.leansoft.contentmanager`. Launcher фиксирует com.leansoft.contentmanager.unity **1.0.0-rc.0**; child дополнительно фиксирует com.leansoft.contentmanager.unity.addressables **1.0.0-rc.0**. Exact versions и scoped registry одинаковы в target/diagnostic profiles. Base содержит опубликованные DotNet/Store/Store.Sqlite NuGet1.0.0; generic пакеты не пересобираются, SDK sources/project references в потребители не вставляются. Package-read token хранится только в пользовательском `.upmconfig.toml`, не передаётся runtime CM и не попадает в bootstrap/source/build.

Текущий Addressables addon предоставляет native immutable AssetId→key mappings/handles, а не CM custom provider/media cache. Его установка не подтверждает CM cache flow. Единственный официальный Newtonsoft UPM3.2.2. Native SQLite и версии portable dependencies фиксированы SDK-поставкой; launcher не вводит второй storage stack.

## 5. Конфигурация и подготовка

Несекретный JSON задаёт staging HTTPS base URI, stable application partition, full exact schema/version selections, общий content root и distribution-relative child executable. Example имеет пустые URL/selections; реальные значения не выдумываются. --cm-config задаёт внешний файл Player; Editor может читать example.

Реальные F22 .g.cs добавляются в оба проекта. CmDemoBuild.GenerateModelBindings формирует прямые закрытые Register<T>/GetPageAsync<T> вызовы из compiled ContentSchema roots, без runtime Registry и MakeGenericMethod. Модели/конфиг/fixture должны совпадать; automatic latest отсутствует. Preserverules сохраняют consumer types, реальная AOT/stripping-проверка остаётся обязательной.

Fixture по бизнесовой F20: две Published версии одной схемы, nested fields, реальные CM Image assets; полный matrix15types проверяется профильными conformance checks. GUI не заменяет их.

## 6. Состояние и владение

По умолчанию SDK database — %LOCALAPPDATA%/DigitalSpace/ContentManagerDemo/staging/content.sqlite. Оба процесса используют один явный root, переданный bootstrap. Path override абсолютный и должен находиться вне outputs. persistentDataPath не считается общим каталогом двух приложений. Legacy Cache.db не используется.

Публичный SDK не имеет whole-product snapshot ID. Прикладной bootstrap format1 содержит sourceIdentity/applicationPartition/storeRoot, staging URI и SDK-confirmed schemaId/schemaVersionId/groupHash/groupHashProfile из committed pages. ProductVariant/license grants не создаются. Child сравнивает proof всех групп до появления selectable rows.

Launcher releases SDK/store ownership до Process.Start. Пока отслеживаемый child running, sync и новый launch не допускаются. Это последовательный POC handoff поверх SDK gate, не полноценный shared multi-writer publication. После child exit/bootstrap launch failure файл удаляется; shutdown Launcher не убивает самостоятельный child.

## 7. Действия

Sync/Resume передают full exact selection SDK. Progress stage/count и разные data/freshness outcomes показываются явно. Stop получает current immutable handle из SDK state один раз; ошибка Status observation при доступном handle не мешает попытке локальной остановки SDK. Stale не ретаргетируется автоматически. Discard требует отсутствия текущего sync и очищает только unfinished SDK attempt.

Launch проверяет EXE, SDK state и matching installed groups. Нет фиктивного запуска при отсутствии config/models/data. Ошибка восстанавливает доступность Retry. Новый запуск после публикации снова читает SQLite; пересборка не нужна, пока совместимые exact compiled models не меняются.

## 8. API

Потребители используют публичный UnityContentManagerClient и typed GetPageAsync<T>; HTTP/F13, hashes, recovery, retries, raw assets и SQLite принадлежит SDK. Ручных маршрутов/DTO/second inventory нет. NSwag UI генерация не требуется для этих Unity-потребителей.

Если staging не предоставляет согласованный standalone Demo доступ, интеграция блокируется. Серверные guards/authorisation не изменяются.

## 9. Изображения, ошибки и ограничения

Child ищет actual Image descriptor в typed values/nested collections. SDK LoadTextureAsync отдельно проверяет bytes/size/hash/PNG-JPEG и создаёт Texture. Generation guard не позволяет late response заменить новый выбор; old handles освобождаются.

SDK1.0 не содержит общий persistent cache/download manager и CM Addressables provider: repeat/offline disk-cache, большие video Range/If-Range resume, RequiredBeforeLaunch/selective preload остаются конкретными зависимостями SDK. Launcher не реализует альтернативный parser DeliveryMode или собственный большой download framework. Отсутствие image/cache не маскируется success данных.

## 10. Фактические проверки и приёмка

На Unity6000.6.5f1: 17 Launcher и4 child NUnit checks, включая реальные scene/prefab references, одну карточку, три SDK actions, возврат после неудачного Launch, list/detail scroll components, bootstrap/config/quoting, реальный native SQLite file open/dispose/reopen через SDK. Показано RED→GREEN для obsolete prefab callbacks и failed-launch retry.

Два Windows x64 Mono diagnostic EXE собраны и запущены. Development --cm-smoke проверяет фактические UI objects/отказы без config/bootstrap; оба процесса самостоятельно закрылись после coordinated cleanup. Это не real-data/staging test и не доказательство pixel-perfect render. Root/child public SDK glue и build code одинаковы.

21.10.2026 повторена проверка с реальной установкой обоих UPM1.0.0-rc.0 из private registry в существующие Unity6 consumers. Resolver locks подтверждают source:registry и exact base dependency add-on; фактические locks сохранены только в diagnostic profiles. Launcher17/17 и child4/4 NUnit прошли без fail/skip, включая реальный SQLite open/dispose/reopen. Оба EXE пересобраны; smoke PASS/exit0, launcher cards1/errors0, child показывает обязательный bootstrap отказ/errors0. По ответу владельца CM пока не готов: обращения к CM/fixtures/fake DTO не вводились. Sources возвращены к2022.3.62f3/IL2CPP; target resolver и Player этим не проверены.

Для целевой приёмки ещё нужны настоящий2022.3.62f3/Windows IL2CPP/C++ toolchain, target UPM re-resolve/lock и проверка serialized assets, actual staging API/Demo access, exact IDs/F22 models и real published data/image. Add/Update/Remove, lost ACK/restart/Stop-Resume, typed installed snapshots, Image integrity/error и offline entities ещё не подтверждены этой интеграцией. Offline cached assets ждут общего SDK cache.

## 11. Следующие действия

Source implementation остаётся частичной до реальной цепочки. В DigitalSpaceLauncher README записаны два Hub project paths, scoped registry/user auth, select-profile/build commands, config поля, fixture prep и distribution layout. Устаревший prepare-sdk.cmd заменён штатным registry restore. Агент не запускал CI/push/publication; private-registry install фактически проверен в Unity6 consumers. Приложения/owned Editors после проверок выключаются; чужие процессы не завершаются.
