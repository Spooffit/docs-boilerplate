# S08. Проверки интерфейса и клиентского поведения

## Область применения

Тестирование пользовательских интерфейсов и клиентских приложений. Для компонентных тестов Blazor **обязательно используется bUnit**. Правила применимы к самостоятельно размещённому Blazor UI, включая приложения с отдельно размещённым HTTP API. Они не требуют определённой модели хостинга и не переносят bUnit на другие клиентские технологии.

bUnit — библиотека тестирования Razor-компонентов, а не замена общему test framework и не браузер. Она используется вместе с принятым в solution xUnit, NUnit либо MSTest; выбор bUnit сам по себе не требует сменить runner. Версия пакета должна быть совместима с целевым .NET/Blazor и зафиксирована принятой конфигурацией зависимостей.

## Предмет проверки

Тест проверяет наблюдаемое поведение: действие пользователя, отправленные по публичному интерфейсу сведения, отображённый результат и допустимый следующий шаг. Внутренние классы не подменяют пользовательский результат.

## Общая модель и уровни проверки

Применяются [S04](../../backend/standards/S04-testing.md), [изоляция и lifecycle S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism) и [test engineering S15](../../../catalog/docs/standards/dotnet/testing.md#shared-testing-infrastructure-and-safe-migration). Расположение этих документов в backend не ограничивает применимость общих правил: SUT, независимость данных, bounded ожидания, cleanup и честный отчёт обязательны также для фронтенда. Серверная БД/брокер не становятся зависимостями каждого клиентского теста.

| Проверяемая граница | Подход | Что доказывает |
| --- | --- | --- |
| Чистая C#-логика: validation, selectors, преобразования, переходы состояния | Unit без bUnit, браузера и сети | Результат для заданных входов и граничных случаев |
| Razor-компонент либо ограниченная композиция компонентов | bUnit; реальные компоненты внутри SUT, doubles внешних сервисов | Rendering, parameters, пользовательские события, callbacks и наблюдаемые состояния |
| HTTP adapter / generated client / serialization | Отдельная проверка API-границы; controlled HttpMessageHandler для unit, реальный тестовый HTTP server для integration | Запрос/ответ, headers, ошибки и cancellation; не весь backend workflow |
| Browser-dependent поведение самостоятельного UI | Реальный браузер; рекомендуется Playwright for .NET | JS/DOM, navigation, focus/keyboard, загрузка приложения и browser network behavior |
| Полный сценарий UI → API → необходимые реальные зависимости | E2E по выбранной общей классификации тестирования | Совместная работа приложений; mocks API не заменяют эту проверку |
| Зависимости слоёв и срезов | Architecture tests | Соблюдение FSD и публичных границ без запуска браузера |

Тест с несколькими дочерними компонентами и fake API остаётся компонентной проверкой. Он не объявляется серверным Integration или полным E2E только из-за размера страницы. Browser test с подменённым API доказывает клиентское поведение; для E2E нужен реальный согласованный контур.

## Организация тестов при FSD

Тесты группируются по владельцу проверяемого поведения: слой → slice → компонент/операция. В отдельном .NET test project структура папок/namespace отражает соответствующие production slices. Для Shared/App, где FSD не требует бизнесовых slices, используется группировка по сегменту или компоненту. Тестовые зависимости не добавляются в production assembly ради размещения теста рядом с `.razor`.

| Слой | Основная проверка |
| --- | --- |
| Shared | Чистые helpers и общие UI primitives, параметры, disabled/validation/callback behavior; общие HTTP adapters отдельно |
| Entities | Клиентское состояние и отображение сущности; выбор/derived values, без копирования серверных domain invariants |
| Features | Пользовательская операция: ввод → validation → вызов API → success/error; request payload и сохранение пользовательского контекста |
| Widgets | Взаимодействие реальных features/entities внутри блока; selection и обновление связанных представлений |
| Pages | Route parameters и композиция страницы, передача выбранного контекста, observable navigation; детальная логика нижних slices проверяется у владельца |
| App | DI, providers, routing и startup composition; browser bootstrap проверяется реальным запуском UI |

Один и тот же набор edge cases не копируется на все уровни: у нижнего компонента проверяется его контракт, у композиции — их взаимодействие. Внутренний unit-тест может проверять локальную реализацию своего slice. Межсрезовые и композиционные проверки используют публичные границы, не открывают private members через reflection и не расширяют public API только ради теста.

Architecture checks проверяют зависимости вниз по FSD, отсутствие циклов/запрещённых same-layer cross-slice зависимостей и обходов public API. Допустимые explicit cross-reference исключения задаются существующей архитектурой, а не изобретаются тестами. Shared не зависит от Features/Widgets/Pages; общее testing boilerplate не превращается в production Shared и не хранит бизнесовые fixtures всех slices. Для C#/Razor проверяется фактический namespace/type/project reference graph; JavaScript barrel files и линтеры не переносятся механически.

Это применение правил FSD к .NET-тестовым проектам, принятое настоящим стандартом; официальные документы FSD не задают отдельного готового стандарта bUnit/Blazor.

## Правила компонентного теста bUnit

Каждый тест создаёт свой bUnit context, явно задаёт parameters/cascading values и регистрирует только нужные сервисы. Context и созданные disposable resources освобождаются после теста, включая отказ. Mutable context, DI scope и UI-state не разделяются между параллельными тестами.

Используется Arrange → Act → Assert: подготовить входы/зависимости, выполнить одно смысловое действие, проверить результат. Несколько assertions допустимы для одного поведения; независимые сценарии разделяются. Ожидаемый результат не вычисляется тем же production helper, который проверяется.

Компонент рендерится и получает события через bUnit: click/change/submit либо согласованный публичный callback. Прямой вызов private handler не заменяет путь пользователя. Проверяются значимые текст/атрибуты/состояния формы, EventCallback и сведения на внешней границе. Тесты не завязываются на число render-вызовов, внутренние поля или случайные generated CSS classes.

При сравнении разметки применяется semantic comparison (`MarkupMatches`) для нужного фрагмента, а не буквальное совпадение всей HTML-строки. Большие snapshots внутренней разметки сторонней UI-библиотеки не являются основной проверкой поведения. Child stub допустим для изоляции родителя, но не доказывает работу настоящей композиции; необходимое взаимодействие проверяется с реальными дочерними компонентами.

JS interop в bUnit настраивается явно. Предпочтителен strict mode с проверкой требуемых identifiers/arguments; глобальный loose mode не используется для скрытия незаданных вызовов. Если сторонний UI-компонент требует root providers, portals или JS services, тестовый harness добавляет его документированное окружение. Fake IJSRuntime доказывает вызов, но не исполнение JavaScript или browser DOM.

## Асинхронность и API doubles

Асинхронный render ожидается через bUnit `WaitForAssertion`/`WaitForState` либо их async equivalents выбранной версии. Ожидание всегда ограничено timeout; assertion не содержит повторный click/submit или другую mutation, которая могла бы выполниться несколько раз. `Task.Delay`, Sleep и случайное увеличение timeout не являются способом синхронизации.

Controlled TaskCompletionSource/fake response управляет моментом success/error/cancellation. Проверяются loading → результат, disabled во время запроса, повторное нажатие, late response после смены selection, уход со страницы/disposal и сохранение пользовательского ввода при отказе. Детали последовательности определяются контрактом компонента, а не искусственной задержкой сети.

Для внешнего сервиса допустим fake уже существующего порта либо controlled HTTP handler. Не создаётся production interface только ради mocks. В компонентной проверке сохраняются реальные generated DTO по [S06](S06-api-client.md); generated client не редактируется и параллельная ручная копия DTO не вводится. Если SUT — сам HTTP adapter, он не заменяется fake, а проверяется на request/response boundary.

Проверка вызова сервиса дополняет отображённый результат. Mock не имитирует отдельную копию серверной бизнес-логики и не доказывает authorization, persistence либо совместимость с реальным API.

## Проверки в браузере для Standalone UI

bUnit не исполняет JavaScript и не подтверждает browser layout/focus, реальные cookie/CORS, загрузку runtime/static assets или совместимость с отдельно размещённым API. Такие зависимости проверяются в browser suite. Самостоятельный UI и API запускаются/адресуются как отдельные приложения; подмена API-origin единым test host не доказывает корректность их раздельного размещения.

При соответствующем контракте проверяются первоначальная загрузка/deep link/reload, routes и navigation, JS-heavy controls, focus/keyboard, upload/download и работа API-origin. Для credentialed API отдельно проверяются настоящий browser credentials/CORS/cookie/CSRF flow; UI permission state не считается серверной авторизацией. Не каждый компонент обязан иметь browser test: туда выносятся границы, которые bUnit доказать не может, и основные пользовательские цепочки.

В Playwright предпочтительны role/accessible name/label либо стабильный test-id вместо привязки к DOM-индексу/CSS структуре. Используются auto-waiting и retrying assertions, не фиксированные sleeps. На тест выделяются собственный browser context, данные и cleanup; неожиданные JS errors/unhandled errors отражаются в результате. При failure сохраняется диагностика сценария, например trace/screenshot, без credentials и чувствительных данных. Наличие role/label selector само по себе не подтверждает всю доступность по [S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics).

## Общие сценарии

| Область | Проверка |
| --- | --- |
| Ввод | Идентичность и поля запроса соответствуют операции; форма не присваивает себе серверные обязанности. |
| Состояния | Несохранённые данные, сохранённый результат и его применение различимы. |
| Ошибка ввода, доступа или сети | Нет ложного успеха; сохраняется понятный контекст; неизвестный исход не вызывает безусловный повтор записи. |
| Неизменяемое состояние | Интерфейс не предлагает незаметную перезапись состояния, которое должно оставаться неизменным. |
| Массовая операция | Видны результаты отдельных элементов, частичный сбой не скрыт общим успехом. |
| Доступ | Клиентское обновление, кэш и контрольная сумма не создают отсутствующее право. |
| Синхронизация | Частичный результат не объявляется завершённым; согласованный рабочий набор не смешивается с новым незавершённым набором. |

## Изоляция

Изолированную клиентскую логику можно проверять с test doubles. Проверка законченного сценария через несколько deployable units относится к E2E и не заменяется цепочкой mocks внутренних вызовов.

Тест создаёт собственные данные, не зависит от другого теста и ожидает наблюдаемое условие, а не произвольную задержку. Clock/culture и значимые runtime options задаются явно, когда от них зависит результат. Cleanup затрагивает только собственные ресурсы; подписки, handlers и незавершённые операции не протекают в следующий тест.

Общие builders/context setup/wait helpers выделяются только по фактическому повторному использованию. Slice-specific fixtures остаются у своего slice. Component/Unit/Architecture запускаются без Docker, реального API и ручной подготовки аккаунтов/БД; browser/E2E harness сам подготавливает необходимое окружение. Отсутствие инфраструктуры не превращает обязательную проверку в Skipped.

## Именование

Для всех клиентских тестов обязателен формат `MethodUnderTest_Scenario_ExpectedBehavior` по [общему правилу именования](../../backend/standards/S04-testing.md#именование-тестов). При проверке интерфейса первая часть обозначает проверяемую операцию, вторая — условия, третья — наблюдаемый результат. Например: `SubmitForm_InvalidInput_ShowsValidationMessage`.

Примеры: `SubmitForm_ValidInput_InvokesSaveCallback`, `LoadData_RequestFails_ShowsErrorMessage`, `Navigate_UnknownRoute_ShowsNotFound`. Названия на английском, три части в PascalCase через `_`; параметризованные cases дополняют имя. Имена из примеров документации библиотек не отменяют это обязательное соглашение. Это рекомендация Microsoft, принятая здесь как правило для всех категорий, а не требование использовать MSTest.

## Проверка соблюдения

| Область | Требование |
| --- | --- |
| Инструмент | Blazor component tests используют bUnit; JS/DOM-dependent checks не выдаются за выполненные браузером |
| Граница | Указаны SUT, реальные части композиции и doubles; API integration/E2E не подменены компонентным render |
| FSD | Видно владение slice; production references/public APIs сохраняют архитектурные ограничения |
| Поведение | Действие проверяется через observable result, а не private fields/render count/случайную разметку |
| Асинхронность | Controlled completion и bounded assertion/state waits, без sleeps и mutations внутри retrying assertion |
| Независимость | Fresh context/data; disposal/cleanup; самостоятельный и совместный запуск без зависимости от порядка |
| Именование | Обязательный `MethodUnderTest_Scenario_ExpectedBehavior` |
| Запуск | .NET-наборы выполняются через dotnet test локально/CI; browser dependencies установлены и окружение подготовлено harness |

При изменении поведения запускаются соответствующие component/unit tests и проверка композиции; при изменении browser/API boundary — соответствующий browser/integration/E2E сценарий. Build и coverage percentage не заменяют эти проверки. Повторный прогон не превращает flaky failure в доказательство корректности: причина нестабильности должна быть установлена.

## Подтверждение

Критерий приёмки и наличие теста не являются результатом его запуска. Поддержка нового поведения не объявляется реализованной без проверки соответствующего клиентского пути.

[Общая модель тестирования](../../backend/standards/S04-testing.md) · [E2E tests, migrated .NET baseline](../../../catalog/docs/standards/dotnet/testing.md) · [Данные и ожидания](../../../catalog/docs/standards/dotnet/testing.md#shared-testing-infrastructure-and-safe-migration).

## Основания и официальные источники

Соглашение Blazor + FSD выше составлено из независимых рекомендаций и правил архитектуры. bUnit обязателен по этому стандарту; Playwright — рекомендуемый browser runner. Источники не объявляют одну готовую архитектуру тестов Standalone Blazor/FSD:

- [Microsoft: тестирование Razor-компонентов Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/test?view=aspnetcore-10.0) — component/browser границы; bUnit является сторонней библиотекой, не продуктом Microsoft.
- [Microsoft: unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices) — naming, Arrange/Act/Assert и изоляция.
- [bUnit: writing tests](https://bunit.dev/docs/getting-started/writing-tests.html) — render, inputs, DI и context.
- [bUnit: asynchronous assertions](https://bunit.dev/docs/verification/async-assertion.html) — bounded ожидание render результата.
- [bUnit: JSInterop](https://bunit.dev/docs/test-doubles/emulating-ijsruntime.html) — explicit strict doubles, без выполнения JS.
- [FSD: layers](https://feature-sliced.design/docs/reference/layers) и [public API](https://feature-sliced.design/docs/reference/public-api) — ownership и направление зависимостей; .NET размещение тестов является адаптацией настоящего стандарта.
- [Playwright .NET: locators](https://playwright.dev/dotnet/docs/locators) и [assertions](https://playwright.dev/dotnet/docs/test-assertions) — устойчивое взаимодействие и ожидание состояния в браузере.
