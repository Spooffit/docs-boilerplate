# M07. Контракты схем и генерация моделей: интерфейс


## Пользовательский результат


UI позволяет выбрать точные версии схем и скачать готовые C# модели. Преобразование и ZIP выполняет сервер. Административный интерфейс использует обычные Pages/Widgets/Features/Entities/Shareds без переноса генератора в браузер.


| Фича | Ответственность интерфейса |
| --- | --- |
| [F22. Генерация C#](../features/F22-csharp-model-generation.md) | Выбор schemas/versions, запуск backend generation, состояния и скачивание ZIP |


F21 не получает отдельную frontend-копию ради симметрии: сведения Registry используются в F22 через согласованный серверный сценарий. [F19](../features/F19-unity-client.md) описывает runtime-пакет; [F20](../features/F20-unity-demo-scene.md) отдельно показывает полученные модели/данные в Unity.


Для Demo требуется только UI → выбор точных SchemaVersion → `POST /api/v1/code-generation/csharp` → deterministic `content-manager-csharp-models.zip`. Request IDs-only, empty/duplicate selection invalid, partial ZIP отсутствует. Общая NSwag/shared binary boundary принимает complete ZIP/headers, JSON/non-JSON failure не скачивается как архив; generated code не правится вручную. Semantic profile принят, literal naming/golden vectors/shared CLR implementation и exact engineering freeze остаются Q31. No-auth Demo сохраняется. В MVP добавляется обязательный CLI и полноценный non-UI codegen workflow поверх тех же Registry/Core contracts. Формат project manifest/config и дополнительные build/Editor integrations уточняются отдельно; они не усложняют Demo.

Registry cm-json-schema/1 и C# cm-csharp/1 остаются самостоятельными tooling profiles. [Q19 canonical content v1](../../backend/questions/Q19-content-hashes.md#принятый-canonical-profile-v1) для Published semantics/runtime integrity не заменяет их, не переносит генератор в UI/SDK runtime и не вводит runtime Registry fetch. Shared typed canonical kernel/neutral models определены один раз backend F03; UI не копирует input DTO или canonical serializer.


[Q31](../../backend/questions/Q31-schema-code-generation.md) · [Карта источников](../../../archive/sdk-architecture-source-map.md) · [Этапы](../../../milestones/README.md) · [Каталог модулей](../../../modules.md).
