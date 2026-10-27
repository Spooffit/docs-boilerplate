# Q26. Состав Demo и переход к MVP




## Принятые границы




Demo проверяет основные функции CM: схемы, экземпляры, их версии, медиа, HTTP-доставку и синхронизацию. Нужны также библиотека-клиент для Unity и небольшая тестовая сцена, которую команда CM делает самостоятельно для демонстрации команде Unity-разработчиков.




В Demo нет входа, авторизации, Identity, ролей, проверки администратором, лицензирования, шины, синхронизации с VR School/Project Storage и полного продуктового Publishing. Заглушки окружающих систем допустимы; они не заменяют проверяемую цепочку создания, хранения, получения и изменения контента. Git-подобные механизмы не включаются.




MVP следует после Demo и уточнения требований. Карта этапов: [milestones/README.md](../../milestones/README.md). Отложенные возможности не возвращаются как скрытые зависимости демонстрации.




## Ответы на блок 3




### 1. Типы полей




Нужны все типы, определённые в переданном CM. Перечень сверён в FieldDataType: String, Text, Integer, Float, Decimal, Boolean, DateTime, Date, TimeSpan, Enum, FlagEnum, Color, Object, List, AssetReference. Точный перечень и источник закреплены в [F01](../features/F01-create-entity-schema.md).




Наличие enum не является доказательством реализации всей обработки типов. Принятое требование: каждый тип проходит создание структуры, заполнение данных, сохранение и доставку. Новые отсутствующие типы из примеров ассистента не добавляются.




### 2. Медиа




Demo включает полноценную MediaLibrary F05. Поддерживаются `Image`, `Audio`, `Video`; backend централизованно задаёт допустимые extensions/MIME/max-size policy, frontend получает её динамически. AssetReference schema definition фиксирует kind, а экземпляр либо выбирает compatible reusable asset из каталога, либо загружает новый.


Каталог ресурсов поддерживает upload/list/detail/preview, server search/pagination и filters kind/format/date/Used/EntitySchema/status, reuse и guarded hard-delete. Реальная цепочка `upload → AssetReference → Published instance → CM-owned YARP URL → bytes → Unity` обязательна как минимум для демонстрируемого изображения; support Audio/Video проверяется F05 integration/browser/client tests. Предустановленная ссылка вместо цепочки недостаточна. [F05](../features/F05-media-assets.md) · [Q12](Q12-media-and-retention.md) · [Q21](../../technical/backend/questions/Q21-media-storage.md).




### 3. Редактирование и удаление




Нужны все перечисленные операции: создание; редактирование и удаление полей черновой схемы; изменение данных черновика экземпляра; удаление ненужных черновиков; удаление/снятие экземпляра с выдачи с отражением результата на клиенте. Это не разрешение физически стирать всю опубликованную историю.




Операции входят в Demo независимо от того, какие уже реализованы. Точные правила зависимостей, удаления опубликованных объектов и поведения предыдущей версии остаются в [Q09](Q09-editing-and-deletion.md), но вопрос «включать ли редактирование/удаление» закрыт.




### 4. Жизненный цикл




Сохраняется пайплайн «черновик → опубликовать» для версий схем и экземпляров. Незавершённые данные не выдаются как опубликованные; дальнейшие изменения создают новую версию, а не переписывают опубликованную. Согласование администратором и продуктовые релизы для Demo не нужны.




### 5. Выбор содержимого клиентом




Клиент задаёт схемы и конкретные интересующие версии схем. Разрешён выбор нескольких схем и нескольких версий одной схемы одновременно; ограничение «одна версия схемы на клиента» не вводится.




Для каждой пары «схема + версия схемы» CM находит экземпляры с данными по этой структуре и выбирает у каждого последнюю опубликованную доступную версию, созданную именно по указанной версии схемы. Сначала ограничивается версия структуры, затем выбирается последняя версия экземпляра внутри неё. Глобально последняя версия экземпляра по другой структуре не подставляется.




Логический экземпляр может иметь версии по разным структурам. Поэтому его исходная дата/структура создания не заменяет проверку SchemaVersionId каждой версии данных. Правило и примеры раскрывает [F13](../features/F13-content-delivery.md).




### 6. Синхронизация




Направление предложено пользователем: клиент передаёт, какие данные и версии у него реально есть и какие схемы/версии он хочет получать; CM вычисляет различия и возвращает, что добавить, обновить и удалить с указанием идентичностей.




Не заменять этот сценарий только повторной полной загрузкой или перекладыванием всех сравнений на Unity. Не требовать payload всех имеющихся объектов: необходимый состав метаданных и форма ответа проектируются в серверной F13. Передавать только сведения клиента, влияющие на выдачу; полный произвольный профиль устройства не требуется.




В блоке 5 уточнены последовательная публикация, полный клиентский реестр, удаление исключённых структур, раздельные план/данные и атомарность по точной версии схемы. Точные DTO и восстановление — [Q30](../../technical/backend/questions/Q30-content-synchronization.md)/Q33. Уточнения алгоритма ассистента помечаются как проектное предложение, а не дополнительные ответы пользователя.




### 7. Хранение и запуск синхронизации




CM использует постоянную БД. Перезапуск сервиса не очищает данные и не создаёт заново пустой каталог; in-memory-сброс для Demo не разрешён. Вопрос об этом закрыт и повторно не задаётся.




Когда проверять обновления — при запуске приложения, по кнопке или иначе — решает сам клиент и его разработчик. Сервер CM не назначает этот жизненный цикл. Способ локального хранения данных библиотекой Unity обсуждается отдельно и не подразумевается ответом о БД сервера.




### 8. Unity-пакет и сцена




В Demo обязательна устанавливаемая [библиотека-клиент F19](../features/F19-unity-client.md) для Unity поверх общего HTTP API. Её разрабатывает команда CM с учётом специфики Unity; Unity-команда должна иметь возможность добавить библиотеку в проект и использовать её.




Команда CM также делает небольшую тестовую [сцену F20](../features/F20-unity-demo-scene.md) для Unity-разработчиков. Библиотека и сцена — отдельные фичи; public F19 единственная клиентская граница. Каталог экспонатов, экран/controls/uGUI/Inspector configuration согласованы в [разделе F20](#f20-экран-и-границы-показа). Встраивание в существующий продукт/лаунчер/VR-проект и несколько платформ не требуются.




## Дальнейшие вопросы без расширения Demo




[Q29](../../technical/frontend/questions/Q29-unity-client-package.md): упаковка и публичные обязанности Unity-библиотеки, версии Unity, данные, ресурсы и локальное состояние. [Q30](../../technical/backend/questions/Q30-content-synchronization.md): детали HTTP-синхронизации. Конкретные правила всех полей уточняются в блоке схем; удаление — Q09. Нового обсуждения входа, шины или лицензий для Demo не требуется.




## Применение




Ответы применяются в F01–F05, F13, F19, F20, картах M01/M02/M05 и milestones/README.md. Сложная совместимость остаётся отложенной. Авторизация сотрудников, одобрение и история их действий относятся к MVP, не к Demo.




## Дополнение блока 4: уточнённая библиотека и codegen




Приняты ответы Q29: Unity 2022.3.62f3, расширяемые DotNet/transport/Unity слои, высокоуровневый sync с локальным применением, progress/cancellation, default retries с настройкой потребителем. Installed inventory и выбранные схемы/версии хранит клиент. Их physical storage остаётся отдельной деталью; вопрос серверной БД не открывается заново.




По новому [codegen-документу](../../archive/entity-schema-codegen-architecture.md) в Demo включены [F21 Registry](../features/F21-schema-registry.md) и [F22 C# generation](../features/F22-csharp-model-generation.md): пользователь выбирает схемы/версии в UI, API-wrapper получает JSON Schema, reusable generator создаёт `.g.cs`, сервер возвращает ZIP. Разработчик добавляет модели в Unity и использует с F19; F20 остаётся самостоятельной сценой.




Это генерация типов, не автоматическое построение UI и не новая система runtime-совместимости. CLI/Editor/build/manifest-driven codegen — дальнейшие этапы. F23 включена в Demo решением владельца 11.10.2026 как отдельная фича; основной SDK F19 работает без Addressables. Поставка SDK через GitLab Registry требует spike Q32, но это не вход/лицензирование CM.




Прежний заголовок «без расширения Demo» описывал момент до принятия приложенной архитектуры; новое включение F21/F22 является явным решением пользователя. Остальные исключения Demo сохранены. [Этапы](../../milestones/README.md) содержат обновлённую карту.




## Дополнение блока 5: конкретная синхронизация Demo




В Demo входят: полный явно заданный реестр схем/версий клиента; отдельное получение плана и точных данных; удаление локальных представлений исключённой структуры; неделимое применение всех экземпляров одной schema/version; частичный успех между целыми группами; независимая загрузка assets после sync данных.




Порядок назначения Published numbers схем/экземпляров строго последовательный. Concurrent Publish instance Draft по разным target SchemaVersion кратко сериализует sequence allocation; при актуальных relevant baselines обе операции успешны с уникальными номерами, реальный target baseline conflict не retry-ится автоматически. Новую версию можно подготовить на основе предыдущей или с чистого листа. Это не возвращает Git-подобную систему.




Нужна возобновляемая sync с synchronizationId, генерируемым внутри DotNet, контрольным состоянием и 409 конкурентного активного дубля. Тем самым у Demo появляется техническое состояние незавершённой операции — оно не равно лицензированной SyncSession и не вводит полный Publishing/шину/Identity.




Действующее решение заменяет SSE на компактный Start JSON, один resumable streaming HTTP response данных и отдельные group commit requests; SDK считает progress локально. HTTP/3 предпочтителен с обязательным HTTP/2 fallback; фактическая streaming/framing совместимость Unity 2022.3.62f3/сервер/прокси ещё [Q33](../../technical/backend/questions/Q33-sync-resume-transport.md); направление не считается реализованным и не отложено молча. Независимая семантика восстановления и атомарности не должна зависеть от одной удачной сетевой сессии.




Production-направление `content-manager.json` не добавляет автоматическую manifest/codegen-интеграцию в Demo. Ранее `content-schemas.json` обозначал тот же замысел, не второй обязательный реестр. В Demo выбранные пары всё равно должны быть явно представлены в настройках/коде клиента.








## Уточнение блока 10: lifecycle экземпляров Demo




Demo дополнительно проверяет stable immutable `EntityInstanceKey` (unique внутри EntitySchema) и mutable authoring Name; несколько независимых Draft одного instance; отсутствие business number у Draft; одну published sequence на instance независимо от SchemaVersionId; создание нового Draft с чистого листа или на основе другого Draft.




Изменение Draft schema не уничтожает заполненные orphan values: они остаются для ручного исправления и блокируют Publish. Удаление Draft schema может каскадно удалить dependent Draft instance data только после modal с количеством и подтверждённым exact dependency set и 5-second disabled confirm. Изменение состава, даже с прежним count, отклоняет delete и требует нового impact/подтверждения с повторением задержки.




Direct Delivery Demo поддерживает withdrawal whole schema, exact schema version и logical instance; следующий sync возвращает Remove. Historical read, Copy и Save recovery work снятого source/target разрешены с предупреждением; Publish к снятому target/runtime запрещены. Возврат в выдачу — отдельное явное действие с проверкой допустимых Published representations; Publish Withdrawal не очищает. Archive/soft-delete/Restore не входят в Demo: разрешённый Delete физический. MVP добавляет Archive как скрытие из authoring UI + withdrawal из новой выдачи при сохранении истории.




## Уточнение блока 18: UI и codegen boundary




Demo admin UI имеет Home, Schemas, Instances, Resources и Code Generation. Home сохраняется даже если на Demo почти пустой.




Основной UX Schemas/Instances строится вокруг workspace с versions/editor, а не вложенных одноколоночных таблиц.




Demo Code Generation ограничен выбором точных SchemaVersion и скачиванием ZIP с generated `.g.cs`. CLI и полноценный non-UI codegen workflow обязательны уже для MVP, а не для Demo.




MVP добавляет Catalog/Publishing workspaces, Moderation, Archive и Administration. В обычных authoring lists/workspaces архивные сущности не показываются; для них используется отдельный navigation block «Архив».


## F20: актуальный эксперимент Launcher + application (11.10.2026)

По явному новому заданию пользователя предыдущий standalone samples/UnityDemo заменён существующим DigitalSpaceLauncher: Launcher в корне и независимый DemoApplication в том же Git, два Windows EXE. Пользователь разрешил git init экспериментальной копии. Имена SDK/UPM сохраняются; Generic packages используются опубликованными.

Launcher напрямую удаляет login/license/legacy catalog/update/ProjectStorage startup behavior. Он синхронизирует staging entities через SDK, затем передаёт несекретный bootstrap с реальными exact group hash/profile proofs и освобождает SDK/SQLite перед child. Child не синхронизируется: только public typed SDK reads и отдельный Image load. Никакой fabricated whole-product snapshot/ProductVariant/license grant.

Общий root — LocalAppData/DigitalSpace/ContentManagerDemo/staging, override абсолютный; persistentDataPath двух Player не предполагается одинаковым. Конфигурация — несекретный JSON/--cm-config, две реальные exact версии/F22 models; --cm-bootstrap получает child. SDK controls сохраняют immutable handles; local image load не превращается в собственный download/cache framework.

Addressables addon1.0 поддерживает native mapping, не CM custom provider. Shared persistent assets/offline cache/large-video resume/DeliveryMode preload требуют общего SDK capability; они сейчас blockers для соответствующих частей сценария. Серверная защита staging не меняется.

**Состояние:** независимые исходники/scene/build/UI и basic real native SQLite checks реализованы/проверены на Unity6/Mono. Staging URL/IDs/models пользователь ещё не подготовил;2022/IL2CPP toolchain отсутствует. Полная F20 приёмка не завершена. Подробные реальные результаты — в [клиентской F20](../../technical/frontend/features/F20-unity-demo-scene.md). Предыдущие пункты standalone-сцены ниже сохраняют историю согласования fixture/UI, но placement/sync owner/config/store-path заменены этим уточнением.


## F20: экран и границы показа

**Ответ:** пользователь согласовал все 18 пунктов. [Business F20](../features/F20-unity-demo-scene.md) задаёт fixture и результат, [клиентская F20](../../technical/frontend/features/F20-unity-demo-scene.md) — экран, координацию и локальное состояние. Это нормативный дизайн, не доказательство готовой сцены, Player/native compatibility или пройденной сети.

### Сцена и управление

1. Тема — небольшой каталог экспонатов; один экран со списком, подробностями выбранного элемента и изображением. Сцена самостоятельна поверх public F19; серверные обязанности F01–F05/F13/F21/F22.
2. UI — uGUI + TextMeshPro; изображение — RawImage, принимающий Texture без создания дополнительного Sprite. SDK не зависит от uGUI.
3. Первый показ — Windows x64/IL2CPP Unity 2022.3.62f3, мышь и клавиатура. VR-ввод/интеграция в продукт — отдельная работа.
4. Пять частей: контекст CM/exact versions; список, разделённый по версиям; подробности; image/result; sync controls/progress/result. Строка содержит Key, runtime title, номер версии экземпляра и схемы. Exact representations одной logical entity отображаются независимо.
5. Controls: «Синхронизировать», «Остановить», «Отбросить незавершённую попытку», «Повторить загрузку изображения». Доступность по SDK state; Stop/Discard — handle конкретной попытки. Кнопки очистки базы нет.
6. На старте сначала сохранённые committed данные, без auto-sync. Без сети после restart данные читаемы; ошибка image отдельна.

### Конфигурация и связь с SDK

7. ScriptableObject в Inspector задаёт CM address и несекретные параметры клиентского контекста. Во время запуска конфигурация фиксирована; runtime address/registry editor не добавляется.
8. Explicit code registration generated types; exact SchemaVersionId из generated metadata. Полный selection — две подготовленные версии одной схемы. Несовпадение fixture/models/config — ошибка подготовки; latest не выбирается.
9. SQLite adapter через F19; файл в выделенной папке Application.persistentDataPath, разделение по клиентскому контексту, path override для проверок. Сцена не читает таблицы/не пишет inventory. Ready native/AOT support требует фактической проверки.

### Набор данных

10. Основная схема `demoExhibit`; согласованные Key/type/значения перечислены один раз в [наборе данных F20](../features/F20-unity-demo-scene.md#набор-демонстрационных-данных). Все 15 FieldDataType, nested Object/List<Object> и дополнительный List<List<…>>; keys уникальны во всём дереве по Q34.
11. Две Published версии: во второй добавлено поле и существующее deprecated; продолжающиеся поля сохраняют identities. Опубликованные экземпляры есть для обеих exact групп.
12. Минимальные случаи: обычный заполненный экспонат с реальной Image; optional absence/false/zero/empty collections; Unicode/numeric edges/nested lists. Дополнительные реальные Audio/Video references для моделей/serialization, без обязательного playback в F20.
13. Подготовка через публичные операции CM; после Publish модели через F22 в проект сцены. Инструкция фиксирует реальные server-generated exact IDs. При пересоздании fixture с новыми IDs модели и конфигурация обновляются вместе; seed SQL/встроенные фиктивные значения не заменяют путь.

### Показ и результаты

14. Явное typed field mapping; скаляры текстом, Object/List структурированными строками; Text без исполнения HTML. Generic JSON Schema UI renderer не вводится. Видимые поля не заменяют conformance tests.
15. Image load только для выбранного элемента; смена освобождает previous owned handle. Image failure отдельно от sync; image retry без нового sync.
16. После sync перечитать committed store; сохранять выбор по exact representation identity, при Remove очищать подробности/image. Partial показывает применённые группы и прежнее состояние остальных. Data/freshness/resource outcomes отдельно; «Данные синхронизированы; актуальность проверить не удалось» допустимо.

### Сценарий и приёмка

17. Initial sync двух versions → real data/image → Draft no effect → Publish Update → new Published Add → Withdrawal Remove → restart сохранённых данных → Stop/continue и отдельно Discard.
18. Lost ACK/crash recovery/другие сбои проверяет управляемый стенд, без имитации ответов в обычной сцене. Приёмка — real Windows x64 IL2CPP Player, packaged SDK/generated models/SQLite, CM + PostgreSQL + YARP/S3; mocks не заменяют цепочку.

Поставка уже выбрана [Q32](../../technical/frontend/questions/Q32-sdk-package-distribution.md): отдельный `samples/UnityDemo` Unity project внутри SDK repo, public packages без копирования SDK source; SDK не зависит от сцены. F20 сохраняет raw image flow; отдельная Addressables F23 включена в Demo. Автоматическая генерация UI не нужна. Login/licenses/broker/VR School/полное Publishing не возвращаются в Demo.

Data apply, post-success freshness и image load независимы. Partial не full success, Unknown не отсутствие изменений. Image error не rollback successful commit, но полноценная приёмка требует реального изображения CM. Placeholder/Inspector runtime данные не заменяют HTTP-цепочку.

Scene state ограничен configuration/presentation context, выбором exact representation, pending/progress/result и image binding с generation guard. Internal class/prefab names выбираются при реализации; они не создают новый public SDK/wire contract, ETag или второй inventory. Controls функционально доступны с клавиатуры, сообщения различимы текстом, без CSS/pixel-perfect требований.

**Осталось закрепить и проверить:** Unity 2022.3.62f3, stripping/build settings/native/dependency locks, конкретное backend окружение, численные limits после sizing, реальные Player/native/storage/transport результаты по Q29/Q32/Q33. Тема/поля/controls/UI/configuration/поставка больше не открытые бизнесовые развилки.
