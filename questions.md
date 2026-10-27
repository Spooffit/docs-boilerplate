# Каталог вопросов
Каждый вопрос имеет отдельный файл в `questions/` своего раздела. Здесь хранится только карта: полный текст, известные ограничения и ответ находятся по ссылке. Отсутствие документа в другой колонке означает отсутствие второй копии, а не отсутствие влияния на эту сторону.
## Карта документов
| ID | Бизнес | Бэкенд | Фронтенд | Связанные фичи |
| --- | --- | --- | --- | --- |
| Q01 | [Границы лицензии](business/questions/Q01-license-rights.md) | — | — | F12, F13 |
| Q02 | [Изменение политики](business/questions/Q02-policy-changes.md) | — | — | F12 |
| Q03 | [Продление и несколько лицензий](business/questions/Q03-renewal-and-multiple-licenses.md) | — | — | F12 |
| Q04 | [Отзыв исторической публикации](business/questions/Q04-withdrawn-publications.md) | — | — | F11, F12, F13 |
| Q05 | [Sources, empty/repeated artifacts и captured review](business/questions/Q05-initial-publication.md) | — | — | F09, F10 |
| Q06 | [Automatic promotion: inputs, intent и continuation](business/questions/Q06-automatic-promotion.md) | — | — | F11, F17 |
| Q07 | [Исключения и локальные данные](business/questions/Q07-exclusions-and-local-content.md) | — | — | F08, F10 |
| Q08 | [Ключи и черновики](business/questions/Q08-keys-and-drafts.md) | — | — | F01, F02, F04 |
| Q09 | [Редактирование, lifecycle и effective dependency closure](business/questions/Q09-editing-and-deletion.md) | — | — | F01, F02, F04, F05, F10, F11, F13, F17 |
| Q10 | [Полномочия и изоляция](business/questions/Q10-permissions-and-isolation.md) | — | — | Сквозной вопрос |
| Q11 | [Каталог и legacy](business/questions/Q11-catalog-ownership.md) | — | — | F06 |
| Q12 | [Медиа и хранение](business/questions/Q12-media-and-retention.md) | — | — | F05, F13 |
| Q13 | — | — | [Первый UI](technical/frontend/questions/Q13-ui-scope.md) | Сквозной вопрос |
| Q14 | [Совместимость позже](business/questions/Q14-client-compatibility.md) | — | — | F14 |
| Q15 | — | [Контракты и исходники](technical/backend/questions/Q15-api-and-source-code.md) | — | Сквозной вопрос |
| Q16 | — | [Ключи и сессии](technical/backend/questions/Q16-activation-and-sessions.md) | — | F12, F13 |
| Q17 | — | [Момент доступности](technical/backend/questions/Q17-publication-availability.md) | — | F11, F12 |
| Q18 | — | [Хранение истории](technical/backend/questions/Q18-activation-history.md) | — | F11 |
| Q19 | — | [Хешируемое представление](technical/backend/questions/Q19-content-hashes.md) | — | F03, F04, F10, F13 |
| Q20 | — | [База, owner transactions и MVP consistency guard](technical/backend/questions/Q20-persistence-and-migration.md) | — | F01–F13, F15, F17, F18 |
| Q21 | — | [Хранилище медиа и YARP gateway](technical/backend/questions/Q21-media-storage.md) | — | F05, F13 |
| Q22 | [Эксплуатационные цели](business/questions/Q22-quality-targets.md) | — | — | F05, F13 |
| Q23 | — | [Синхронизация источника](technical/backend/questions/Q23-source-synchronization.md) | — | F06, F12, F13 |
| Q24 | — | — | [Приложения и вход специалиста](technical/frontend/questions/Q24-admin-sign-in.md) | F01, F06, F12 |
| Q25 | [Конфликты каталога, affected scope и repair → Resolve](business/questions/Q25-catalog-conflicts.md) | — | — | F06, F08, F15, F11, F13, F18 |
| Q26 | [Состав Demo и переход к MVP](business/questions/Q26-demo-mvp-scope.md) | — | — | Все модули; milestones/README.md |
| Q27 | [Скрытие и архивирование каталога](business/questions/Q27-catalog-archiving.md) | — | — | F06, F15, F12, F13 |
| Q28 | [Объект проверки и последствия одобрения](business/questions/Q28-content-review-scope.md) | — | — | F17, F03, F04, F09–F11 |
| Q29 | — | — | [Unity-библиотека клиента](technical/frontend/questions/Q29-unity-client-package.md) | F19, F13, F05 |
| Q30 | — | [Контракт синхронизации](technical/backend/questions/Q30-content-synchronization.md) | — | F13, F19, F04, F05 |
| Q31 | — | [Контракты Registry и генерации C#](technical/backend/questions/Q31-schema-code-generation.md) | — | F21, F22, F19, F13 |
| Q32 | — | — | [Поставка SDK и проверка UPM/GitLab](technical/frontend/questions/Q32-sdk-package-distribution.md) | F19, F23, F20 |
| Q33 | — | [Streaming HTTP: public wire v1, execution/control и transport profile](technical/backend/questions/Q33-sync-resume-transport.md) | — | F13, F19, F05 |
| Q34 | [Семантика типов полей и DateTime MVP](business/questions/Q34-schema-field-semantics.md) | — | — | F01, F02, F03, F04, F21, F22 |
| Q35 | [MVP ограничения значений и сравнение List](business/questions/Q35-value-constraints.md) | — | — | F01, F02, F03, F04, F21, F22 |
| Q36 | [EntityInstance: идентичность, версии и lifecycle](business/questions/Q36-entity-instance-lifecycle.md) | — | — | F04, F13, Q09, Q20, Q30 |
| Q37 | — | — | [Launcher, режимы доставки и общий файловый кэш](technical/frontend/questions/Q37-launcher-asset-delivery.md) | F01–F05, F12, F13, F19, F21, F23 |
| Q38 | [Массовая фиксация и подготовка выпуска](business/questions/Q38-bulk-version-freeze-and-release-preparation.md) | — | — | F03, F04, F07–F11, F17 |
| Q39 | [Назначение схем и выбор экземпляров](business/questions/Q39-schema-content-selection.md) | [Контракты, review, хранение и миграция](technical/backend/questions/Q39-schema-content-selection.md) | — | F07–F10 |
