# M02. MediaLibrary: интерфейс


## Назначение и целостный результат


Demo содержит самостоятельный каталог ресурсов `/assets`, upload/detail/preview, reusable `AssetReference` picker и guarded delete. Пользователь работает с CM asset descriptor и stable AssetId, а не с MinIO bucket/path.


## Demo navigation и каталог


Раздел «Ресурсы» предоставляет:


- server-side search и pagination;
- filters `AssetKind`, format, upload date, Used/Unused, EntitySchema и diagnostic status;
- metadata/detail;
- image preview, audio/video player при поддерживаемом browser format, иначе download fallback;
- upload;
- usage/delete-impact и допустимый hard delete.


Demo AssetKind: `Image`, `Audio`, `Video`.


## Upload policy


Frontend загружает поддерживаемую policy с backend: kinds, allowed extensions/MIME и configured max size. Она используется для tooltip/help, input `accept` и ранней UX-validation. Frontend не создаёт собственную authority-policy и не считает client MIME доказательством формата.


При generic upload специалист выбирает AssetKind. При upload из EntityInstance kind уже зафиксирован definition поля, поэтому сменить его нельзя.


## AssetReference picker


Schema `AssetReference` содержит exact required AssetKind. Picker показывает только compatible `Available` assets. Пользователь может:


1. выбрать существующий asset;
2. загрузить новый compatible файл;
3. после успешного upload явно подтвердить выбор.


Новый asset сразу появляется в общем каталоге и может переиспользоваться другими compatible полями.


В поле сохраняется stable AssetId; UI может держать рядом descriptor (`Id`, kind, filename, format/MIME, size, hash/status, UploadedAt, ContentUrl) для отображения. Descriptor не заменяет серверную validation. При Publish сервер фиксирует immutable AssetReference descriptor в Published snapshot/hash без ContentUrl; Delivery читает bytes отдельно по AssetId. CM eligibility query до YARP/cache/304/Range допускает Available-only Demo.


## Identity и bytes


Один server GUID является AssetId и object identity. Public UI не получает отдельный random ObjectKey, bucket или provider credentials. Canonical byte route:


```text
/storage/assets/{assetId}
```


Browser и Unity используют один CM-owned принцип доступа; SDK лишь скрывает его за API.


## Replace, history и delete


«Заменить файл» создаёт новый AssetId. Старые bytes под существующим AssetId никогда не перезаписываются. Переключение Draft на новый asset выполняется отдельно.


Hard delete разрешён только при полном отсутствии Draft/Published/history/orphan references; UI сначала показывает usage-impact. Cascade отсутствует.


MVP Archive скрывает asset из обычного picker и запрещает новое использование, сохраняя historical bytes; Restore возвращает его в выбор.


## Cache/read UX


Frontend пользуется HTTP validators/cache headers сервера. Conditional read и browser/client cache не смешиваются с metadata ETag (`If-Match`) mutable resource. Range нужен прежде всего audio/video; unsupported preview не является upload failure.


## Карта фич


| ID | Результат |
| --- | --- |
| [F05](../features/F05-media-assets.md) | Library/detail/upload/picker/delete и CM-owned gateway preview/download |


## Связи


[M01](M01-content-authoring.md) · [M05](M05-delivery.md) · [F19](../features/F19-unity-client.md) · [Q12](../../../business/questions/Q12-media-and-retention.md) · [Q21](../../backend/questions/Q21-media-storage.md) · [Q29](../questions/Q29-unity-client-package.md).
