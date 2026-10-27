# Q12. Медиа и хранение


## Принято


Demo использует полноценную MediaLibrary F05. Поддерживаются `AssetKind.Image`, `AssetKind.Audio`, `AssetKind.Video`; тип выбирается специалистом при самостоятельном upload и задаётся definition поля `AssetReference` при работе из экземпляра.


Допустимые форматы централизованно задаёт backend media policy и динамически отдаёт frontend. Схемы не содержат собственные списки extensions. Минимальная Demo policy:


| Kind | Extensions |
| --- | --- |
| Image | `.jpg`, `.jpeg`, `.png` |
| Audio | `.mp3`, `.ogg`, `.wav` |
| Video | `.mp4` |


Backend проверяет выбранный kind, extension/MIME и фактическую file signature/container. Frontend показывает server policy в `accept`, tooltip/help и picker, но клиентская проверка не является authority. Per-kind maximum size является обязательной environment configuration и также отдаётся frontend; скрытый unlimited default запрещён.


Нельзя подменять bytes старого `AssetId`. Любая замена/перезагрузка создаёт новый `MediaAssetId`, после чего пользователь явно выбирает его в Draft. Один и тот же asset можно переиспользовать в нескольких сущностях.


## Хранение и retention


Любой доказанный Draft/Published/history/orphan reference защищает bytes от физического удаления. Automatic garbage collection в Demo/MVP не вводится. Отсутствие references не запускает таймер удаления автоматически: physical delete остаётся явной операцией через F05 и повторно проверяет dependencies.


`PendingUpload`/`Failed` можно увидеть через diagnostic status filter и удалить вручную по согласованному cleanup flow. Продуктовый срок автоматического удаления failed/pending не задаётся.


MVP Archive скрывает asset из обычной library/picker и запрещает **новые** references, но не удаляет bytes и не переписывает historical content. Restore снова разрешает новое использование.


## Historical read


Published/history/frozen consumer получает прежние bytes по прежнему AssetId, пока artifact существует и не применён отдельный terminal access restriction. Обычный Unpublish/Retire другого объекта не является разрешением удалить asset.


Существующие authoring references не очищаются автоматически при Archive. Точная UX-policy редактирования уже сохранённого Draft после Archive может уточняться вместе с MVP Archive workflow, но она не может разрешить physical delete сохранённых bytes либо silently заменить ссылку.


## Связи


[F05](../features/F05-media-assets.md) · [F13](../features/F13-content-delivery.md) · [Q09](Q09-editing-and-deletion.md) · [Q21](../../technical/backend/questions/Q21-media-storage.md) · [Q34](Q34-schema-field-semantics.md)