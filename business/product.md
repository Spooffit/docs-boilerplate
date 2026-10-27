# Назначение Content Manager

## Исходная проблема и цель

Динамические сведения, фотографии и данные предметных объектов не должны требовать пересборки Unity при каждом изменении содержимого. Content Manager позволяет специалисту самостоятельно управлять контентом: создавать, добавлять, редактировать, удалять, связывать и публиковать его через административные функции.

Цель не ограничена несколькими заранее перечисленными операциями. Конкретные сценарии и правила целостности раскрываются в фичах. Самостоятельность специалиста не означает физическое удаление истории или изменение опубликованной версии в обход принятых правил.

Программист нужен, когда новые поля требуют ещё не реализованного вывода или обработки в клиенте. На текущем этапе эта граница решается существующим версионированием схем и данных и выбором поддерживаемой клиентом структуры; расширенная система совместимости отложена в [F14](features/F14-client-compatibility.md).

## Сотрудники и проверка контента в MVP

Административным CM пользуются только сотрудники собственной организации. IdentityAccess CM отдельная от VR School/runtime Delivery; сквозной вход не нужен. Админ provision-ит сотрудников, назначает fixed system roles и деактивирует доступ; первый админ создаётся manual operator bootstrap без default password/autostart.

Роли Specialist/Administrator фиксированы, комбинируются без implicit inheritance. Все специалисты имеют одинаковые возможности и работают со всем контентом; админ управляет доступом и проверяет exact PublicationRevision по F17. Server-fixed permission catalog и fresh coherent snapshot per HTTP request ограничивают действия вместе с resource/domain rules; AllowedActions advisory.

MVP immutable persisted [Activity History](features/F18-staff-activity-history.md) отражает own committed business results, не failed commands/security logs: common journal Spec/Admin, staff-access Admin-only, eventual append/manual refresh без auto-purge. StaffSession имеет absolute default 8 часов без sliding/refresh; role change применяется в next request, checked in-flight может finish. Logout current не закрывает другие sessions, deactivate/reset invalidates previous, reactivate требует new Login. Form recovery обязательно в memory той же open Blazor Standalone tab при internal same-user reauth без reload: actual input/File objects и original ETag/base/context сохраняются; new GET не overwrites baseline/autosaves. Different actor/manual Logout clears snapshot; close/reload/browser restart не guaranteed.

UI/API independently hosted на HTTPS-поддоменах одного базового домена: different origins/same site, host-only API cookie с exact-origin credentialed CORS и CSRF; shared reverse proxy не выбран. Полные принятые правила/engineering границы в [F16](features/F16-staff-access.md)/[Q24](../technical/frontend/questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы).

Действующие спецификации: [F16](features/F16-staff-access.md), [F17](features/F17-content-review.md), [F18](features/F18-staff-activity-history.md), [M06](modules/M06-staff-workflow.md). Уточнения: [Q10](questions/Q10-permissions-and-isolation.md), [Q24](../technical/frontend/questions/Q24-admin-sign-in.md), [Q28](questions/Q28-content-review-scope.md).

## Клиенты

Доставка выполняется по HTTP. Получателем может быть Unity, веб-, desktop- или мобильное приложение; API не ограничивается конкретной ролью приложения. Фактический текущий получатель является Unity-проектом.

В интеграционном сценарии Digital Space Launcher синхронизирует структурированные сущности через Sdk.Client.DotNet и публикует согласованный snapshot в общем SQLite; Unity-приложение читает этот snapshot и само скачивает media при необходимости через защищённый CM API. ProjectStorage по-прежнему распространяет сборки приложений; VR School владеет каталогом и лицензионными facts. Первый целевой runtime — Unity 2022.3.62f3, Windows x64/IL2CPP; другие платформы отдельно не обещаны.

Специалист выбирает `DeliveryMode` в определении поля AssetReference (или recursive item leaf): OnDemand по умолчанию, неблокирующий Preload, RequiredBeforeLaunch для обязательной готовности присутствующих ссылок данного snapshot. Launcher выполняет только selective preload; оба процесса используют общий SDK file cache. Policy не принадлежит MediaAsset и не расширяет frozen runtime descriptor. Entity sync и media readiness раздельны, ошибка optional media не отменяет установленный entity snapshot. Самостоятельное Demo остаётся без Launcher/auth/preload зависимостей. Принятые границы раскрыты [F01](features/F01-create-entity-schema.md#режим-доставки-assetreference-после-demo), [F13](features/F13-content-delivery.md#launcher-и-selective-preload-после-demo) и [F19](features/F19-unity-client.md#launcher-и-общий-media-cache-после-demo); инженерные recommendations/manifest/profile/re-admission/offline решения **Уточняется [Q37](../technical/frontend/questions/Q37-launcher-asset-delivery.md)**.

## Существующий пользовательский путь

1. Организация приобретает конкретное приложение, например «Московский тир».
2. Скачивает лаунчер на свой стенд и вводит номер своей лицензии.
3. Лаунчер получает из Project Storage сведения о лицензии, проектах и файлах приложений.
4. По этим сведениям лаунчер разрешает скачивание только приобретённых приложений. Организация скачивает и запускает доступные приложения.
5. Когда клиент запрашивает динамический контент, CM отдельно оценивает полученные лицензионные факты и выдаёт либо не выдаёт соответствующую публикацию.

CM не принимает решение о запуске и скачивании файлов приложения. Эти функции остаются в лаунчере и существующем контуре Project Storage. HTTP-выдача динамического содержимого является отдельной обязанностью Delivery.

В MVP подтверждение CM-content period в VR School требует заранее полученного immutable CM PolicyAgreement; policy резервируется при его commit, а Agreement само по себе не даёт grant. Эта принятая зависимость CM не распространяется на application license/Project Storage. Подробные правила: [F12](features/F12-content-entitlement-policy.md).

## Системы и источники данных

| Система | Ответственность |
| --- | --- |
| VR School UI / Host | Единственное место создания и редактирования Product/ProductVariant; управление лицензиями; публикация событий в шину |
| Project Storage | Получение событий своего действующего контура, сведения о лицензиях и старых Project/Application, выдача файлов приложений лаунчеру |
| Лаунчер | Ввод номера лицензии, определение доступных скачиваний/запусков по сведениям Project Storage |
| Content Manager | Получение событий нового каталога и лицензий; подготовка и публикация контента; разрешение его HTTP-выдачи |

CM не редактирует каталог в VR School через собственный UI/API, не читает таблицы монолита напрямую и не требует посредничества Project Storage для получения своих событий. Точные контракты и восстановление событий уточняются в [Q23](../technical/backend/questions/Q23-source-synchronization.md).

## Продукты и варианты

| Product | ProductVariant |
| --- | --- |
| Тир | Московский тир; Нижегородский тир |
| Музей | Музей собак; Музей бабочек |

ProductVariant является конкретным проектом, а не выпуском клиентской сборки. В минимальном каталоге требуются название продукта, название варианта и связь между ними; технические идентификаторы необходимы для устойчивых ссылок, но дополнительные описания, обложки и маркетинговые поля не согласованы.

Один ProductVariant используется разными организациями и лицензиями. Создание новой лицензии не создаёт копию варианта. Локальные отличия состава задаются только вариантом, не организацией, лицензией или устройством. Выбор разных разрешённых исторических ревизий по лицензиям не является персональным переопределением состава.

В CM отображаются все записи нового каталога VR School, события которых CM получил и обработал. Отдельная функция «подключить проект к CM» не требуется. Запись с ожидающим конфликтом остаётся видимой с объяснением ограничения, но не выдаётся за полностью применённое новое состояние.

## Переход со старой модели

Новый каталог вводится в VR School как параллельная функциональность, предназначенная позднее заменить Project/Application. Сейчас он планируется прежде всего для CM; Project Storage продолжает работать со старой моделью.

Работники постепенно переносят сведения вручную. Обязательное автоматическое сопоставление старых и новых объектов и одномоментная миграция не требуются. Не считать ID старого Project автоматически ID нового ProductVariant. Отдельно отличать это от переименования существующего типа ProductVersion внутри CM без смены его идентичности. Ответ закреплён в [Q11](questions/Q11-catalog-ownership.md).

## Изменение связей и завершение использования

Пришедшее из шины изменение связи, конфликтующее с состоянием CM, не применяется незаметно. Для MVP требуется ручное разрешение: интерфейс показывает конфликт, затронутые продукты/варианты и обязательность действий, а зависимые операции блокируются. Изменение каталога выполняется в источнике; разрешение последствий в CM не создаёт второго владельца каталога. [F15](features/F15-catalog-conflicts.md).

Точная матрица конфликтов и граница блокировки, включая выдачу уже опубликованного контента, пока [уточняются](questions/Q25-catalog-conflicts.md).

Требуются скрытие из выбора для новых назначений, архивирование и сохранение сопровождения существующих пользователей. Архивирование не равно удалению данных, отзыву лицензии или уничтожению публикаций. Владелец соответствующего действия и точные переходы состояний ещё [уточняются](questions/Q27-catalog-archiving.md).

## Публикация и права

Схемы, версии экземпляров и медиа управляются независимо от каталога получателей. Publishing хранит назначения и релизы, материализует ProductVariantPublicationRevision и фиксирует её активацию. Delivery выбирает готовую разрешённую ревизию, а не объединяет текущие источники при запросе.

Revision допускает общий и/или local Published source, но всегда имеет непустой итог. Publishing выбирает Variant ActiveRevisionId; Product ActiveReleaseId задаёт только current общую базу. Activation pointer + append-only history атомарны; rollback/unpublish сохраняют прежние revisions. Automatic для конкретной операции обрабатывает Variants независимо. Delivery current committed state отделён от historical visibility: MVP сохраняет conservative VisibilityConfirmedAt после primary observation already committed exact transition, не physical commit time. Freeze исключает commit-before/confirmation-after cutoff. Подробности в F09–F13 и [Q17](../technical/backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery).

При FreezeAtExpiration окончание UpdatesUntil прекращает получение нового контента, сохраняя согласованное право на historical whole revision. Это не разрешение неизвестному ключу, отозванной лицензии или клиенту без grant. [F12](features/F12-content-entitlement-policy.md) задаёт PolicyAgreement/CurrentPeriod, Renewal/Correction, completeness и selected session pin. [F13](features/F13-content-delivery.md) описывает separate runtime bearer, absolute TTL, stable creation replay и bounded response leases по принятому [Q16](../technical/backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases); staff Identity и anonymous Demo отделены.

## Этапы и критерий успешности

Текущая цель: максимально простое Demo только по схемам, экземплярам, их версиям, HTTP-доставке и синхронизации клиента. **Без входа и авторизации, без шины и VR School, без проверки админом, лицензирования и полного продуктового Publishing.** Заглушки допустимы. Это заменяет прежнее предложение демонстрировать весь интеграционный контур.

Для Demo проверяется короткий путь: создать структуру и данные, получить их в клиенте, изменить через новую версию и синхронизировать результат. Объём полей, медиа и выбора выдаваемого состава ещё уточняется в [Q26](questions/Q26-demo-mvp-scope.md).

Для MVP сохраняется более широкий целевой результат, описанный выше: управление контентом сотрудниками с проверкой админом, внешние лицензионные факты и выдача согласованной публикации. Demo не доказывает готовность этих исключённых возможностей. После обратной связи уточняется окончательный MVP. Состав этапов находится в [milestones/README.md](../milestones/README.md).

Полноценная совместимость, преобразование схем на лету, Git-подобное распространение изменений, точные API и эксплуатационные параметры не определяются этим обзором. Подробности находятся в [фичах](../features.md), [модулях](../modules.md) и [вопросах](../questions.md).
