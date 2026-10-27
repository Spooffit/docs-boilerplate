# Архитектура интерфейсов

## Аудитории

Специалист работает с ContentModeling, медиа и Publishing через административные операции. Клиент-получатель обращается к Delivery за готовым разрешённым составом. Общий backend не превращает их в одну модель полномочий.

## Demo и рабочий интерфейс MVP

В [Demo](../../milestones/demo/demo.md) нет страницы входа, авторизации, ролей, пользователей или проверки контента админом. Интерфейс сразу открывает базовую работу со схемами/экземплярами и их версиями; внешний каталог, лицензии и сложная публикация не являются обязательной навигацией. Детали простого клиентского показа уточняются Q26.

В MVP работают внутренние сотрудники через IdentityAccess/Core Identity, без входа через VR School. [F16](features/F16-staff-access.md) описывает fixed-role RBAC, staff Session/login/logout/setup и управление доступом админом. Global current-user permissions и resource AllowedActions дают UX, backend свежий request snapshot/owner rules — authority. Все специалисты видят общий контент; moderation относится к админу и [F17](features/F17-content-review.md).
Blazor Standalone UI и API — separate independently hosted applications на HTTPS-поддоменах одного base domain: different origins/same site. Exact UI-origin credentialed CORS, client credentials include, host-only API cookie/SameSiteLax и antiforgery обязательны; общий /api reverse proxy не требуется. Protocol/defaults/physical config — [Q24](questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы).
Mandatory same-user reauth recovery сохраняет actual input/File/exact context/original baseline в памяти живой SPA-вкладки без full reload. Explicit Logout/другой user очищают snapshot; reload/закрытие/browser restart не гарантированы. Fresh resource ETag не заменяет original, restore не autosave/unknown command replay; mutations проверяют expected login context против trusted server Session.

Видимая история действий сотрудников требуется по [F18](features/F18-staff-activity-history.md); общий журнал или история в карточке ещё не выбраны. Размещение компонентов и их состояние описываются без вёрстки. Имя блока в спецификации не означает, что он уже существует в архиве.

## Представление данных

В форме известна выбранная схема и конкретная версия структуры. Название не заменяет ID. Черновик, опубликованная версия, назначение, релиз и активная ревизия показывают разные последствия действия.

Источник правила в бизнесовой фиче, формат серверной операции в её серверной спецификации. Для веб-UI выбрана генерация C# API-клиента через NSwag; действующие конфигурация и OpenAPI ещё требуют исходников. Не восстанавливать их по старой ошибке скрипта.

## Неуточнённые решения интерфейса

Стек веб-UI: .NET 10, Blazor Standalone, MudBlazor, FSD и NSwag по [обзору](README.md). IdentityAccess/cookie/session/authorization и same-site independent origins выбраны Q24; Demo anonymous. Concrete wire/provider/deployment adapters, filters/pagination/layout composition уточняются [Q13](questions/Q13-ui-scope.md)/[Q15](../backend/questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities). Наличие source-компонента не доказывает выбранный target.

Смысловой путь: список схем → выбранная схема → её структуры и экземпляры → конкретная версия данных. Размещение в страницах/панелях/tabs пока не выбрано.

## Описание состава и компоновки

Фронтенд-фичи описываются по [шаблону](../../templates/frontend-feature.md): страницы/экраны, необходимые FSD-блоки, их публичные входы и результаты, владение состоянием и совместная работа. Компоновка фиксируется логически, без реализации вёрстки. Этот формат не утверждает конкретные страницы, widgets или маршруты за отдельную фичу.

## Каталог и согласование внешних изменений

Product и ProductVariant создаются/редактируются только в VR School. CM показывает обработанный событийный каталог и не предоставляет даже проксирующий редактор исходных записей. [M03](modules/M03-catalog.md) связывает просмотр/выбор [F06](features/F06-product-catalog.md) и ручное разрешение [F15](features/F15-catalog-conflicts.md).

Один вариант используется организациями с разными лицензиями; UI не создаёт персональные копии или переопределения. Предупреждение о конфликте сохраняется до подтверждённого сервером разрешения; точные блокировки и поведение старой выдачи определяются Q25. Скрытие/архивирование каталога не равно удалению истории, конкретные действия пока Q27.

## Receiver

Блок 13 для MVP: current общая база Product и active revision Variant различаются. UI показывает no-op, append-only rollback/unpublish, per-Variant automatic result и незавершённую readiness Delivery при async. Получатель не использует Product ActiveRelease как прямую поставку. Подробный состав lifecycle blocks и их состояние находится в F11; конкретные routes Q13. Demo эти экраны не требует.

В последующей интеграции launcher получает build из ProjectStorage, выполняет F13 entity sync через DotNet и публикует согласованный SQLite snapshot. Player читает pinned StoreScope/SnapshotId и сам обращается к CM за отсутствующими медиа; launcher выполняет только выборочную предзагрузку. Оба используют один SDK download/cache manager: SQLite хранит metadata/jobs/index, filesystem — verified bytes и owned partial files вне заменяемого build. Entity readiness, required-media readiness и optional preload различны; работающий Player не переключается автоматически на новый snapshot. Новая активация не меняет выбранную сессию. Bootstrap/re-admission, offline/revocation/старый fallback snapshot и точный manifest contract — [Q37](questions/Q37-launcher-asset-delivery.md). UI специалиста CM не подменяет launcher.

DeliveryMode принадлежит опубликованному AssetReference leaf/recursive ItemDefinition: OnDemand default, Preload без launch block, RequiredBeforeLaunch до всех реально присутствующих обязательных файлов Ready. Inspector F01/F02 редактирует Draft, F03 показывает Published readonly; MediaAsset и Variant не получают глобальный mode/override. Предложенный manifest является scoped производной exact definitions × final effective content; strongest-mode aggregation одного AssetId внутри revision требует подтверждения Q37. Подробнее [F13](features/F13-content-delivery.md#41-launcher-readiness-и-pinned-player-после-demo), [F19](features/F19-unity-client.md#68-общий-файловый-cache-и-launcherapp-интеграция-после-demo).

Текущая цель: [Demo](../../milestones/demo/demo.md) для проверки требований; после обратной связи следует MVP. Подробная совместимость остаётся отложенной, на старте используется существующее версионирование.

[Карта функциональных спецификаций](../../features.md).

## Клиентская экосистема: контракты и реализации

По [дополнению от 30.09.2026](../../archive/sdk-contracts-architecture-update.md) `Sdk.Client.Core` переименован в `Sdk.Client.DotNet`. Он является общим runtime SDK только для .NET/C#-экосистемы. Unity и потенциальный Godot C# используют платформенные адаптеры над ним; это не общая библиотека для TypeScript, C++, GDScript или Rust.

Общая схема: **Backend → OpenAPI / JSON Schema / Schema API → независимые языковые SDK**. DotNet определяет neutral interfaces и общую orchestration/protocol/integrity реализацию; HttpClientTransport (Http), UnityWebRequestTransport/DownloadHandlerScript (Unity), SqliteClientStateStore (Store.Sqlite) реализуют dependency ports. Optional Unity.Addressables -> Unity/DotNet, no reverse dependency. Interfaces/components/stream/store/composition — [F19](features/F19-unity-client.md#53-интерфейсы-dotnet-и-конкретные-реализаторы); actual SDK repo/DLL-UPM/native/pinned Player capability — [Q32](questions/Q32-sdk-package-distribution.md#согласованная-поставка-и-release-policy-sdk). Ordinary NET Http не обязательный Unity implementation.

OpenAPI описывает обычные HTTP-операции и их модели; JSON Schema — динамические структуры; Schema API / [F21](../../business/features/F21-schema-registry.md) — официальное получение выбранных определений. Клиентские модели F22 генерируются из JSON Schema, не из классов DotNet или примеров экземпляров.

[F19](features/F19-unity-client.md) сохраняет один высокоуровневый вызов синхронизации и согласованное восстановление; [F20](features/F20-unity-demo-scene.md) остаётся самостоятельным Demo с direct sync/raw image. Launcher/preload/licensed sessions/persistent media cache и optional Addressables не становятся его скрытыми зависимостями. Первая целевая комбинация — Unity 2022.3.62f3, Windows x64, IL2CPP; exact provider/Addressables package/settings и runtime proof ещё Q32/Q37. Optional F23 custom entity/asset providers оборачивают те же F19 typed reader/cache ports; native Unity-built catalog/bundles остаются отдельной веткой.
