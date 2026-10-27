# F23. Необязательная интеграция Unity Addressables

[Модуль M05](../modules/M05-delivery.md) · [Клиентская спецификация](../../technical/frontend/features/F23-unity-addressables.md) · [SDK F19](F19-unity-client.md).

## Цель и границы

Отдельный optional add-on `Sdk.Client.Unity.Addressables` в первом Demo предоставляет локальное разрешение CM AssetId в native Unity Addressables key и управляемую загрузку ресурсов; последующие CM typed entity/media providers добавляются отдельно, не связывая общий backend и `Sdk.Client.DotNet` с этой технологией. Universal AssetId и entity identities остаются CM contracts, платформенный key/provider — обязанность add-on. Исторические основания — [SDK-архитектура](../../archive/unity-client-sdk-architecture.md), разделы 12–24, 38–39, 46–52, и [уточнение имён пакетов](../../archive/sdk-contracts-architecture-update.md); актуальная Launcher/shared-cache модель — [F19](F19-unity-client.md#launcher-и-общий-media-cache-после-demo) и [Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md).

Основной SDK должен работать без add-on. Обычная загрузка изображения из CM не требует Addressables. По решению владельца 11.10.2026 интеграция F23 включена в Demo и реализуется отдельной фичей после текущего этапа F19. Optional означает независимость основного SDK от add-on, а не исключение из milestone. Launcher/shared-cache integration и расширенные providers остаются последующим этапом; scope F19 этим решением не расширяется. Целевая версия Unity — 2022.3.62f3, Windows x64/IL2CPP; source add-on требует Addressables1.22.3 minimum; текущий Editor6 diagnostic profile использует2.11.2. Совместимость целевого2022.3IL2CPPPlayer ещё не подтверждена.

## Сценарий

1. Unity content pipeline создаёт совместимые local catalog/AssetBundles и mapping универсальных Guid AssetId в native keys.
2. Приложение создаёт immutable snapshot и отдельный AddressablesClient с dispatcher F19.
3. Load<T> возвращает отдельный native claim и управляемый handle. Явный Preload загружает только выбранные IDs, возвращает индивидуальные outcomes и удерживает успешный поднабор при Partial.
4. Приложение использует готовый ресурс. Для ручного prefab Instantiate удерживает load handle до уничтожения всех instances.
5. Dispose/DisposeAsync освобождает claim; ready handle переживает shutdown клиента. Отмена waiter не обещает physical cancel, add-on завершает наблюдение и cleanup операции.

CM typed entity/custom raw-file providers, Launcher/shared download cache и selective launcher preload остаются следующим этапом по Q37. Первый local Demo не реализует второй HTTP/cache/F13 механизм, serialized manifest или remote catalog update. Успешный sync F19 не означает resource readiness; F19 raw image/file path не требует add-on.
## Правила

AssetId не равен Addressables key. Backend не публикует Unity key вместо универсальной ссылки. Mapping для native Unity-built resources, catalog, AssetBundle и сборка Unity-контента принадлежат платформенному content pipeline. CM entity/file providers не требуют превращать каждый JSON/image/video в AssetBundle. Серверные модели, чтения и хранение ресурсов остаются у F05/F13; отдельная серверная копия F23 не нужна.

Наличие произвольного файла prefab в хранилище не считается доказательством готового Addressables-контента: предусмотренный источником путь включает Unity build pipeline. CM не получает обязанность самостоятельно собирать bundles из любых загруженных файлов.

Формат manifest и конкретный набор mappings версионируются раздельно. Нужны понятные отказы для отсутствующего mapping, недоступного key/catalog, несовместимого типа и неуспешной загрузки. Local outcomes/ownership выбраны в клиентской спецификации §5.1; remote/custom-provider policies остаются незакрытыми. Сбой add-on не меняет уже установленный набор данных F19; автоматический fallback на raw файл или иной ресурс не согласован.

Владение загруженным prefab и экземпляром, созданным из него, — отдельные обязанности. Короткий пример Load → Instantiate → Dispose не подтверждает корректность lifetime экземпляра. Отмена ожидания также не доказывает остановку фактической загрузки: оставшаяся операция и её ресурсы должны иметь определённого владельца.

## Приёмка и незакрытое

Базовый SDK используется без add-on; включённый add-on разрешает mapping и выдаёт управляемый ресурс; Load/Preload/Release, повторные и конкурентные загрузки проверяются на выбранной версии Addressables. Ошибки и отмена не оставляют потерянных ресурсов и не выдают неполный preload как полный успех.

Точная версия Addressables, keys/provider API, AOT/stripping для generated DTO, VideoPlayer file lifetime и Launcher/shared-cache integration — [Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md). Native catalog/bundle mapping и lifetime — [Q29](../../technical/frontend/questions/Q29-unity-client-package.md#f23-mapping-snapshot-и-ownership-операций); поставка — [Q32](../../technical/frontend/questions/Q32-sdk-package-distribution.md). Приёмка включает CM typed entity, verified image/file, cache hit/miss, cancellation/concurrent requests/release и реальный Unity 2022.3.62f3 Windows x64 IL2CPP Player; Editor-only check не доказывает AOT. Local implementation проверен real packed catalog/bundles в Unity6000.6.5f1; результаты и открытый2022.3IL2CPP gate описаны в клиентской спецификации §10.2.
