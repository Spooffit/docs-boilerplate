# M02. MediaLibrary


## Назначение и целостный результат


MediaLibrary хранит и переиспользует типизированные binary-ресурсы независимо от EntityInstance. Специалист загружает ресурс один раз, получает стабильный `AssetId`, видит его в общем каталоге и может использовать тот же asset в нескольких совместимых полях экземпляров.


Demo уже включает полноценный каталог ресурсов, а не только upload рядом с формой экземпляра.


## Asset kinds Demo


Поддерживаются три обязательных `AssetKind`:


- `Image`;
- `Audio`;
- `Video`.


`AssetReference` в схеме фиксирует требуемый `AssetKind`. Допустимые расширения/MIME и максимальный размер определяются централизованной backend policy, а не каждой схемой отдельно. Frontend получает эту policy от CM и использует её для подсказок, фильтра выбора файла и предварительной проверки UX; сервер остаётся authority.


## Каталог Demo


Специалист может:


- загрузить asset с выбранным kind;
- искать и постранично просматривать каталог;
- фильтровать по kind, формату, дате, использованию, EntitySchema и диагностическому status;
- просматривать metadata и поддерживаемый preview/player;
- выбрать существующий совместимый asset из EntityInstance;
- загрузить новый asset прямо из picker, после чего он становится общим reusable asset;
- увидеть usage/delete-impact и удалить только действительно неиспользуемый asset.


«Перезагрузить/заменить файл» никогда не переписывает bytes существующего `AssetId`: создаётся новый asset, который затем явно выбирается в Draft. Published/history ссылки продолжают указывать на прежние bytes.


## Identity и хранение


CM генерирует один `MediaAssetId : Guid`. Этот же GUID является identity объекта в final media namespace object storage; отдельного независимого случайного `ObjectKey` нет. Filename, bucket, provider endpoint и credentials не являются identity и не попадают в content.


CM хранит подтверждённые metadata в своей БД и дублирует immutable transport metadata в объекте MinIO. БД остаётся authority каталога/lifecycle.


## Режим доставки и клиентский кэш после Demo

`DeliveryMode` принадлежит AssetReference leaf точной схемы по [F01](../features/F01-create-entity-schema.md#режим-доставки-assetreference-после-demo), не карточке MediaAsset. Один asset может использоваться с разными mode; политика разрешается для конкретного snapshot в Delivery. MediaLibrary сохраняет stable bytes identity, metadata и CM/YARP gateway; S3/MinIO закрыт.

Launcher синхронизирует сущности и выполняет выборочный preload; Unity app сама скачивает нужные media по защищённому CM API. Оба используют SDK download/cache coordinator по [F19](../features/F19-unity-client.md#launcher-и-общий-media-cache-после-demo). Persistent jobs/index находятся в SQLite, большие bytes — во внешнем файловом кэше. Это интеграционный этап после Demo; возможность upload 5–10 ГБ, inbound multipart и лимиты отдельно **Уточняется [Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md)**, не выводится из наличия Range download.

## История и удаление


Любая доказанная Draft/Published/history/orphan ссылка защищает bytes от physical delete. Cascade очистки `AssetReference` отсутствует. Automatic GC в Demo/MVP не вводится.


MVP Archive запрещает новое использование и скрывает asset из обычного picker, но сохраняет historical bytes; Restore снова разрешает новое использование.


## Карта фич


| ID | Спецификация этого раздела |
| --- | --- |
| F05 | [MediaLibrary и AssetReference](../features/F05-media-assets.md) |


## Связи


[Карта модулей по разделам](../../modules.md) · [Полная карта документов фич](../../features.md) · [Q12](../questions/Q12-media-and-retention.md) · [Q21 backend storage/gateway](../../technical/backend/questions/Q21-media-storage.md).
