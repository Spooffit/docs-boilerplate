# F14. Совместимость клиента: фронтенд, принятый минимум

[Бизнесовая спецификация](../../../business/features/F14-client-compatibility.md) · [Бэкенд](../../backend/features/F14-client-compatibility.md) · [M05](../modules/M05-delivery.md) · [Q14](../../../business/questions/Q14-client-compatibility.md).

## 1. Результат и границы

Пользователь работает с конкретной структурой и видит её точную версию. Публикация новых полей не означает, что клиентское приложение уже умеет их выводить/обрабатывать. Если нужна новая обработка, её добавляет разработчик. Генерация C# F21/F22 даёт модели данных, но не создаёт поведение приложения.

F14 не вводит самостоятельную страницу, реестр сборок/возможностей или мастер преобразования. Принятый минимум реализуется в существующих authoring screens F02–F04 и клиентских сценариях F13/F19/F20. Расширенная совместимость отложена. Точный UX неподдерживаемого состава MVP и допустимость другого целого snapshot остаются Q14. Это целевое описание; наличие текущей server model ClientContentProfile не доказывает готовность интерфейса или runtime проверки.

## 2. Страницы, экраны и навигация

| Контекст | Что содержит экран в пределах F14 | Действия / владелец |
| --- | --- | --- |
| SC01. Workspace версии схемы F02/F03 | Название схемы, выбранная точная версия/статус, её поля; Published показывает сохранённую структуру | Authoring действия F02/F03; переключение exact версии по их навигации |
| SC02. Workspace версии экземпляра F04 | Логический экземпляр, выбранная версия данных, точная SchemaVersion основа, значения; Draft/Published различимы | Copy/Save/Publish F04; target меняется только по принятому сценарию, без скрытой подстановки latest |
| SC03. Сцена Demo F20 / клиентский экран приложения | Контекст выбранных точных структур, текущие committed данные, результат/прогресс sync F13/F19 | F20: Inspector configuration + explicit generated registration, запуск sync кнопкой; прочие приложения выбирают собственное управление |
| SC04. Потребитель MVP F13/F19 | Данные одной pinned ревизии и результат операции SDK | Новый специальный compatibility screen и его маршрут не определены; Q14 |

F14 не добавляет web route. Unity сцена не становится Blazor-страницей; FSD и NSwag web conventions не переносятся автоматически в SDK. Подписи используют Name/business number для человека, точные ID сохраняются в контексте. Не найденная exact версия не заменяется последней. Несколько selected SchemaVersion одной SchemaId в Demo допустимы; интерфейс не вводит ограничение «одна версия» через текущую серверную profile таблицу.

## 3. Карта FSD-блоков

Новых срезов F14 нет. Для web действует существующая декомпозиция F02–F04; для Unity/.NET — обязанности SDK F19 и сцены F20, а не искусственная FSD раскладка.

| Блок / место использования | Назначение, входы | Выход / получатель | Состояние / зависимости |
| --- | --- | --- | --- |
| pages: workspace схемы F02/F03 | Exact schema/version route context и server read model | Выбранная версия в существующий editor/read view | Page selection; авторские features и shared generated API по F02/F03 |
| pages: workspace экземпляра F04 | InstanceId/InstanceVersionId, точная schema основа | Контекст в editor/read view и действия F04 | Page selection, version editor local input; ETag transport state принадлежит своему ресурсу |
| entities: version read models F02–F04 | Generated DTO и exact IDs | Подписи/статус/значения в представление | Не серверные агрегаты; Domain/EF зависимости не добавляются |
| features/widgets/shared существующих workspaces | Показ exact контекста и authoring действия по F02–F04 | Confirmed результат либо локальная ошибка | Повторно используются публичные boundaries владельцев; новый supported flag/store не вводится |
| SDK coordinator и runtime read boundary F19 | Full exact selection, actual inventory, generated exact contracts | Committed данные и результат F13 в F20/приложение | Durable installed/staging и execution state F19; контракт/storage Q29 |
| Представление контекста/результата F20 | Выбор и SDK result от координатора | Видимые данные, прогресс, сообщение об ошибке | Scene-owned UI state; зависимости только на публичный SDK |

Название блока здесь обозначает роль, а не утверждённое новое имя Razor/MonoBehaviour. Полные входы/выходы/жизненный цикл переиспользуемых блоков определены F02–F04/F19/F20. Локальная настройка F14 — сохранить exact version context и не обещать новую поддержку.

## 4. Логическая компоновка

```text
SC01. Workspace схемы (F02/F03)
  Header и выбор exact версии
  Поля этой версии: editor Draft / read view Published
  Authoring actions и результат владельца

SC02. Workspace экземпляра (F04)
  Логическая identity и выбранная версия данных
  Точная SchemaVersion основа
  Значения этой структуры: editor Draft / read view Published
  Copy/Save/Publish и результат владельца

SC03. Сцена F20 / приложение
  Контекст: полный выбор точных SchemaId + SchemaVersionId
  Координатор → публичный sync F19
  Результат операции / прогресс по F13
  Текущий committed набор → typed представление exact данных
```

Workspace передаёт выбранную версию существующему блоку и принимает результат её действия. Связанные rail/read views обновляются по F02–F04, без изменения уже выбранного target ради latest. Координатор сцены передаёт полный выбор SDK; SDK возвращает confirmed результат и доступ к committed данным. Представление не выполняет свой второй diff, выбор версии или конвертацию. Поздний ответ старого контекста не заменяет текущий выбор.

## 5. Зависимости и публичные границы

Web pages используют публичные exports своих features/entities/shared по F02–F04. API consumption через shared NSwag infrastructure, без handwritten копии server DTO или ссылки на Domain/EF. SDK использует runtime контракт F13 и generated schema models F21/F22; NSwag для authoring API не означает готовый Unity runtime transport. Сцена зависит от SDK, SDK не зависит от сцены. Факты лицензии MVP и server-side проверки не вычисляются из UI флага поддержки.

## 6. Данные и владение состоянием

| Данные | Источник истины | Клиентский владелец |
| --- | --- | --- |
| Exact структура/данные и Published статус | Server F02–F04 | Server read model страницы; локальный Draft input отдельно по владельцу |
| Текущий selection Demo | Приложение/конфигурация F20 | Координатор передаёт полный набор F19; несколько targets одной схемы |
| Фактически установленный состав | Atomic committed state F19 | SDK state adapter; staging не считается installed |
| Generated model / код обработки | Developer build F21/F22 и приложение | Новый server target не изменяет уже установленную сборку |
| Pinned revision/session MVP | Delivery F13 | SDK session context; поддержка клиента не расширяет право |
| Current operation/progress/error | Подтверждённый SDK/API результат | Workspace/scene state в своей границе; нет глобального compatibility store |

Dirty/Cancel/Save при переключении authoring context описаны F02/F04; F14 не вводит autosave или новый browser storage. Resume/Stop/Discard и durable state остаются F13/F19. Способ объявления поддержки сборкой для MVP не выбран, Q14.

## 7. Действия и клиентские потоки

| Действие / место запуска | Передача и результат | Связь с бэкендом |
| --- | --- | --- |
| Открыть/переключить exact authoring version SC01/SC02 | Route/selection ID в read модели своего workspace; показать ту же версию или явную ошибку | F02–F04; не новый endpoint F14 |
| Опубликовать новые поля/данные SC01/SC02 | Действие владельца, own If-Match; confirmed Published состояние | Backend F14 DF01, полные потоки F03/F04; клиентская сборка автоматически не обновляется |
| Синхронизировать SC03 | Full exact selection + actual inventory в один вызов SDK; план и применение целых групп | Backend F14 DF02 и F13; алгоритм/ID/resume принадлежит F19 |
| Читать данные SC03 | Только committed representation нужного SchemaVersion, typed lookup по F19 | Exact payload F13, generated модели F21/F22 |
| Получить MVP snapshot SC04 | Session/entitlement/pinned данные F13; unsupported ветвь уточняется | Backend F14 DF03, Q14; нет самостоятельного выбора fallback в UI |

Ошибочная Demo группа сохраняет прежний committed набор по F13, успешные целые группы могут примениться независимо. Это не разрешение урезать целую MVP ревизию. Клиент не удаляет неизвестные поля или меняет target ради успешной десериализации. Новую обработку добавляет разработчик и проверяет на exact данных.

## 8. Использование API и NSwag

F14 не добавляет operation в NSwag и не исправляет generated-код вручную. Server source contracts и ETag/If-Match остаются у F02–F04/F13 по [S06](../standards/S06-api-client.md). Own ETag хранится в generic transport/UI state рядом с generated model; бизнесовый номер версии не превращается в token. Новый server ClientContentProfile не используется как автоматически доступный web endpoint.

Full required selection, exact identity, runtime wire/error profile SDK определяет F13/Q15/Q33. Потребность в декларации поддержки MVP остаётся Q14, без вымышленного request supportedVersions или метода CheckCompatibilityAsync. Codegen F21/F22 не подменяет транспорт NSwag или данные installed inventory.

## 9. Состояния, ошибки и доступ

| Ситуация | Что видит пользователь | Действия / восстановление |
| --- | --- | --- |
| Открыта Published версия | Сохранённая точная структура/значения и статус, по F02–F04 | Читать/выбрать другую версию; новые поля другой версии не появляются сами |
| Draft меняется/публикуется | Local input и результат операции владельца | Dirty/Save/Cancel/conflict recovery F02/F04; не обещать поддержку уже установленной сборкой |
| Exact версия не найдена/недоступна | Явная ошибка соответствующей области | Перечитать/выбрать допустимый контекст; latest не подставляется |
| Demo group download/read error | Ошибка группы, отдельно от успешных групп и freshness | Сохранить прежний committed набор группы; retry/resume F13/F19 |
| Новая структура требует иной обработки | Нет утверждённого автоматического badge поддержки | Изменить код приложения через разработчика; UI warning/detection механизм отложен Q14 |
| MVP неподдерживаемый состав | Точное сообщение/действия уточняются Q14 | Не показывать урезанный snapshot как полный успех и не переключать pinned основу молча |

Demo без авторизации/лицензии; MVP доступ проверяется сервером F12/F13. Функциональная доступность существующих screens по [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback)/[S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics): понятные подписи точных версий/ошибок, клавиатурные действия web и сохранение фокуса по владельцу. Новая модальная форма или обязательный navigation flow F14 не назначаются.

## 10. Проверки и приёмка

Стратегия по [S08](../standards/S08-testing.md); новые тестовые библиотеки не выбираются.

| Проверяемый сценарий | Исходные условия / воздействие | Наблюдаемый результат | Граница |
| --- | --- | --- | --- |
| Exact context SC01/SC02 | Две версии схемы, две основы данных; открыть конкретный ID | Показана именно эта версия; отсутствующая не заменена latest | Workspace composition F02/F04; API double явно |
| Новое поле | Publish новой структуры при старой клиентской сборке | Старый экран читает прежний exact contract; нет обещания нового client behavior | Component/SDK behavior; отдельно реальные данные E2E |
| Multiple targets SC03 | Передать две SchemaVersion одной SchemaId | Полный набор дошёл до SDK; представления различимы по exact context | Координатор с SDK double, без доказательства server persistence |
| Ошибка одной группы | SDK вернул committed успех одной группы и error другой | Показаны confirmed данные/результаты, прежняя группа не выдана за удалённую | Scene result composition F20 |
| Поздний ответ | Выбор сменился до завершения старого запроса | Новый контекст не перезаписан | Client state владельца |
| MVP mismatch | Условия и итог пока Q14 | Expected fallback/сообщение не выбираются тестом молча | После решения — SDK/API путь F13/F19 |

При реальном CM → DotNet/Unity E2E использовать exact Published версии и действительный SDK/scene; mocks транспорта не доказывают такой путь. Серверный минимум проверяется `F14CompatibilityHttpTests` через public authoring/sync API и PostgreSQL: старые Published bytes после нового поля, две версии одной схемы, отсутствие latest fallback. Эти проверки не подтверждают SDK/Unity, атомарное локальное применение или отображение полей сценой F20.

Существующий `ContentManager.Ui.Tests` runner F01–F05/F13 собран и пройден, включая exact Publish/late-response protection, workspace guards и generated streaming transport. Внешний API F14 не изменился, поэтому повторная генерация клиента не требовалась; для будущих изменений применяется существующая `generate-api-client` конфигурация UI. Отдельная страница F14 не добавляется.

## 11. Последовательность изменений и открытые вопросы

1. Переиспользовать exact version context и действия F02–F04; не создавать экран реестра клиентов.
2. Передавать полный выбор F20 в SDK F19 и отображать confirmed exact данные/результат F13. Способ задания выбора остаётся у F20.
3. Согласовать generated exact contracts F21/F22 с кодом приложения; проверить отображение/обработку отдельно от генерации.
4. Расширенную декларацию поддержки, UX mismatch и fallback проектировать только после решения Q14; минимум Demo не ждёт этого решения.

| Вопрос | Какие детали блокирует |
| --- | --- |
| [Q14](../../../business/questions/Q14-client-compatibility.md#минимальная-техническая-граница-f14) | Декларация поддержки и UX неподдерживаемого MVP состава/другого целого snapshot |
| [Q15](../../backend/questions/Q15-api-and-source-code.md), [Q33](../../backend/questions/Q33-sync-resume-transport.md) | Принятый Demo runtime wire/error profile F13 и оставшиеся внешние транспортные проверки; новые DTO F14 не вводятся |
| [Q29](../questions/Q29-unity-client-package.md) | SDK storage/lifecycle, не реестр совместимости |

Расширенная F14 остаётся отложенной; принятый минимум не требует нового frontend framework, stores или массовой переработки соседних фич.
