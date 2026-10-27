# Фронтенд

## Стек и граница раздела

Для веб-интерфейса Content Manager используется следующий стек, подтверждённый владельцем проекта:

| Технология | Назначение |
| --- | --- |
| **.NET 10** | Платформа клиентского приложения; код на C# и Razor. |
| **Blazor Standalone** | Самостоятельное браузерное приложение; компоненты интерфейса выполняются на клиенте. |
| **MudBlazor** | Библиотека компонентов пользовательского интерфейса. |
| **FSD (Feature-Sliced Design)** | Архитектура фронтенда с организацией кода по слоям и функциональным срезам. |
| **NSwag** | Генерация типизированного C# API-клиента по OpenAPI-описанию бэкенда. |

Этот стек относится к UI Content Manager. Существующий `vrschool.ui` использует Angular и остаётся отдельным интерфейсом VR School. Раздел документации также охватывает поведение клиентских приложений-получателей, включая Unity; указанный стек не переносится на них автоматически. Модели входа этих приложений не считаются одинаковыми по умолчанию.

## Архитектура фронтенда

UI Content Manager работает как **Blazor Standalone** и независимо от API размещается на своём HTTPS origin; UI/API subdomains имеют общий base domain (same site). Взаимодействие выполняется через HTTP API с использованием клиента NSwag. Выбор IdentityAccess не требует миграции UI или same-origin proxy. Клиент отвечает за отображение, ввод, навигацию и состояния; серверные правила и проверки доступа не заменяются формой.

Для staff MVP [F16](features/F16-staff-access.md) использует host-only API Secure/HttpOnly/SameSite=Lax cookie, exact UI-origin CORS AllowCredentials и browser credentials include. Unsafe browser requests, включая login/logout/setup/reset, защищаются CSRF; final carriers/routes — [Q15](../backend/questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities). Global permissions текущего user и resource AllowedActions служат UX; backend fresh per-request authorization остаётся authority. Demo anonymous и runtime Delivery credentials независимы от staff login.

Mandatory form recovery после expiry/login поддерживается в памяти той же открытой SPA-вкладки без reload: actual input/File objects/exact context/original baseline сохраняются для того же user, Save выполняется отдельно. Explicit Logout/другой user очищают snapshot. Reload/закрытие/browser restart исключены; localStorage/IndexedDB/autosave не вводятся. Детальная композиция и проверки — [F16](features/F16-staff-access.md).

**FSD** задаёт организацию фронтового кода: функциональные срезы отделяют самостоятельные возможности интерфейса, а слои разграничивают их ответственность и зависимости. MudBlazor используется для построения визуальных компонентов, NSwag обслуживает границу обмена с API. Конкретное размещение срезов, компонентов и сгенерированного клиента определяется структурой исходников, а не деревом папок документации.

Версии пакетов MudBlazor и NSwag, точная конфигурация генерации клиента, имена проектов и каталогов, а также конфигурация сборки и развёртывания **уточняются по исходникам**. Выбор стека не означает, что эти детали уже проверены.

Справочные описания технологий: [Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-10.0#blazor-webassembly), [MudBlazor](https://mudblazor.com/), [Feature-Sliced Design](https://feature-sliced.design/docs/get-started/overview), [NSwag](https://github.com/RicoSuter/NSwag). Источник выбора стека проекта: уточнение владельца проекта.

## Unity и launcher

Целевая первая комбинация — **Unity 2022.3.62f3 / Windows x64 / IL2CPP**. Это выбор платформы, не доказательство совместимости SQLite/transport/Addressables: exact dependencies и actual Player acceptance — [Q32](questions/Q32-sdk-package-distribution.md). Standalone [F20 Demo](features/F20-unity-demo-scene.md) сохраняет direct sync/raw image без обязательного launcher/авторизации/preload/add-on.

После Demo launcher синхронизирует entities в pinned SQLite snapshot, Player читает его и сам скачивает CM media по необходимости. Оба используют общий [F19 download/cache manager](features/F19-unity-client.md#68-общий-файловый-cache-и-launcherapp-интеграция-после-demo); metadata/jobs в SQLite, большие bytes в filesystem. Field-level DeliveryMode задаётся в Draft schema inspector, default OnDemand; Published readonly. Launcher readiness и optional [F23 providers](features/F23-unity-addressables.md#52-cm-entity-и-asset-providers-после-demo) отделены от entity sync. Незакрытые manifest/profile/bootstrap/cache/security/large-upload вопросы имеют owner [Q37](questions/Q37-launcher-asset-delivery.md).

## Структура

| Место | Назначение |
| --- | --- |
| [Архитектура стороны](architecture.md) | Общие границы и взаимодействия без пересказа каждой функции. |
| `features/` | Страницы, экраны, FSD-блоки, их компоновка, взаимодействия, состояния и использование API без реализации вёрстки. |
| `modules/` | Общий результат связанного набора функций и карта их спецификаций. |
| `questions/` | Отдельные вопросы этой стороны; полный текст не дублируется в модуле. |
| `standards/` | Нумерованные технические соглашения, общие для нескольких фич. |
| `adr/` | Зафиксированный выбор и его последствия. |

## Как описывать фронтенд-фичу

Используйте [шаблон frontend-фичи](../../templates/frontend-feature.md): результат пользователя → страницы и экраны → карта блоков → компоновка → состояние и действия → API → проверки. В документе описывается, что собрать и как части взаимодействуют; Razor/HTML-разметка, CSS и подробные визуальные макеты не требуются.

Фичи документации и срезы FSD не обязаны соответствовать один к одному. Переиспользуемый блок имеет одно основное описание, а модуль содержит карту фич без копирования их внутреннего состава.

## Навигация

[Фичи](../../features.md) · [Модули](../../modules.md) · [Вопросы](../../questions.md) · [Стандарты](../../standards.md).

Для работы агента действуют [инструкции стороны](AGENTS.md) и общие правила репозитория. Спецификация этой стороны не переписывает бизнесовое правило, а объясняет собственную обязанность при его выполнении.
