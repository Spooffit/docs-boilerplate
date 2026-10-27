# Production

[Каталог milestones](../README.md) · [Предыдущий milestone: MVP](../mvp/mvp.md).

## Цель и граница согласования

Подготовить результат MVP к рабочему использованию. Production — отдельный третий milestone. Функциональная база наследуется из MVP; точный дополнительный объём и критерии эксплуатационной готовности ещё не утверждены. Этот файл не назначает новые продуктовые фичи, инфраструктуру, сроки или числовые требования по предположению.

## Фичи функциональной базы

Подробная глубина каждой фичи находится в [составе MVP](../mvp/mvp.md#входящие-фичи) и F-документах.

| Фичи | Что сохраняется в Production |
| --- | --- |
| [F01–F05](../../features.md) | Схемы/версии/экземпляры и media lifecycle, real immutable bytes и CM-owned gateway |
| [F06–F11](../../features.md) | Каталог, общий/variant состав, releases/revisions и полный publication lifecycle |
| [F12–F13](../../features.md) | Лицензионные права и exact protected Delivery |
| [F14](../../business/features/F14-client-compatibility.md) | Принятый минимум exact versions; расширение пока отложено |
| [F15–F18](../../features.md) | Каталожные конфликты, сотрудники/доступ, moderation и история действий |
| [F19](../../business/features/F19-unity-client.md) | Многослойный SDK и client integration через публичные contracts |
| [F21–F22](../../features.md) | Registry и reusable code generation, включая уже принятый MVP CLI |

[F20](../../business/features/F20-unity-demo-scene.md) — отдельный sample/presentation consumer, не обязательная часть runtime клиента. [F23](../../business/features/F23-unity-addressables.md) — optional add-on без выбранного milestone; сам переход к Production не делает его обязательным.

## Направления дополнительного workflow

| Связанные фичи | Направление | Что ещё уточняется |
| --- | --- | --- |
| F19/F21/F22 | Декларация exact dependencies клиентского проекта, tooling получает JSON Schema и использует тот же Core | Final имя/формат декларации (рабочее content-manager.json), источник/обновление selections, состав automation; Q29/Q31 |
| F21/F22 | Переиспользование generator другими developer consumers | Конкретные Editor/source-generator/build/CI consumers и объём этого milestone; обязательный CLI уже относится к MVP |
| F19 | Воспроизводимая поставка SDK и поддержанные target environments | Actual package/registry/Unity compatibility matrix и release/install checks Q32 |
| F05/F13 | Рабочие gateway/transport/storage configuration и качество эксплуатации | Provider/signing/forwarding/cache/limits/retention/framing/proxy policy и измеряемые критерии Q21/Q22/Q33 |

Это направления детализации, а не утверждённый список дополнительных обязательных функций Production. Обязательный MVP CLI F22 и уже требуемые проверки Demo/MVP, включая real byte/sync chain и package spike, не переносятся до Production. Source-примеры versions/routes/TTLs и generic production рекомендации не выбирают конкретную конфигурацию. Дополнительные SDK языков, optional Addressables/remote content, CDN, offline-first или expanded compatibility не включаются автоматически.

## Уточнение состава и приёмки

### Предложенное усиление доставки больших файлов

Источник [Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md) рекомендует для Production hardening испытания 5–10 ГБ, resource bounds/quotas, long-running re-admission, crash/recovery/races, observability, multipart inbound upload и согласование retention/revocation/offline. Это предложение декомпозиции; точный объём/числа не утверждены. Необходимая корректность basic cache/resume/integrity/required launch gate не откладывается автоматически до Production. Изменение source target на Unity 2022.3.62f3 Windows x64/IL2CPP также не доказывает поддержку Player/native SQLite/Addressables.

Перед фиксацией окончательного объёма надо определить дополнительный developer workflow и измеряемые критерии готовности. Решения сначала вносятся в owning F/Q, затем отражаются здесь. До этого описание базы MVP не является обещанием завершённого Production release.

[Q26 scope](../../business/questions/Q26-demo-mvp-scope.md) · [Q21 media](../../technical/backend/questions/Q21-media-storage.md) · [Q22 quality](../../business/questions/Q22-quality-targets.md) · [Q29 SDK](../../technical/frontend/questions/Q29-unity-client-package.md) · [Q31 codegen](../../technical/backend/questions/Q31-schema-code-generation.md) · [Q32 distribution](../../technical/frontend/questions/Q32-sdk-package-distribution.md) · [Q33 transport](../../technical/backend/questions/Q33-sync-resume-transport.md).
