# Q32. Поставка SDK и проверка Unity UPM / GitLab

## Принятое направление

[SDK-источник](../../../archive/unity-client-sdk-architecture.md), разделы 25–36, 41, 44 и 50: самостоятельные DotNet/Http/Unity/optional Addressables единицы, UPM-упаковка, self-hosted GitLab NPM Package Registry, CI publication и SemVer. Линия клиента по ответу пользователя — Unity **2022.3.62f3**.

Registry не является слоем runtime SDK. Возможная замена способа доставки не меняет API DotNet/Unity/Addressables. Сцена F20 не является runtime dependency библиотеки. Автоматическая публикация generated models в registry **не нужна для codegen Demo**: там достаточно ZIP.

## Согласованная поставка и release policy SDK

**Ответ:** Согласованный packaging/dependency/CI contract действует с актуальным уточнением двух репозиториев и HTTP/1.1 Demo policy ниже. Local generic/UPM staging artifacts созданы, но CI/publication/private registry install и Unity Player/native compatibility не подтверждены. Normative package graph/artifacts/layers — [F19](../features/F19-unity-client.md#111-поставка-и-зависимости-пакетов); consumer scene — [F20](../features/F20-unity-demo-scene.md).

1. **SDK repositories.** Two existing repositories: contentmanager.sdk.client owns DotNet/Http/Store.Sqlite/tests; contentmanager.sdk.client.unity owns Unity source/asmdef/UPM staging and separate packaged acceptance harness. CM backend/codegen independent; F20 scene is outside this slice. Actual GitLab endpoints/access/runners not validated.
2. **SQLite artifact.** Sdk.Client.Store.Sqlite implements replaceable store port; depends on DotNet, no reverse native dependency. Ready Demo adapter after real platform spike.
3. **Graph.** Http -> DotNet; Unity -> DotNet; Store.Sqlite -> DotNet; optional Unity.Addressables -> Unity/DotNet. Neutral interfaces в DotNet; HttpClientTransport в Http, UnityWebRequestTransport/DownloadHandlerScript в Unity, SqliteClientStateStore в Store.Sqlite. Unity no mandatory ordinary NET Http implementation; portable coordinator/codec/recovery едины.
4. **Unity representation.** DotNet precompiled DLL in UPM; Unity facade/platform bridge source+asmdef. Compiled library doesn't establish IL2CPP compatibility by itself.
5. **NET distribution.** DotNet and applicable Http/SQLite adapters NuGet; Unity UPM. Same DotNet build bytes in both wrappers, no independently built implementations.
6. **IDs.** Accepted ContentManager.Sdk.Client.DotNet/Http/Store.Sqlite NuGet; com.leansoft.contentmanager.dotnet/store.sqlite/unity/unity.addressables UPM. Http UPM only if needed, exact ID not guessed. Assembly names Sdk.Client.*; public CLR namespaces/binding references frozen with F22 before emitter/release.
7. **Newtonsoft.** One assembly per process: generic NuGet 13.0.3, Unity official com.unity.nuget.newtonsoft-json 3.2.2. SDK UPM does not bundle duplicate Newtonsoft DLL. Generic actual F22 ZIP/golden/typed-read evidence exists; official Unity package combination still needs actual managed compile/Player.
8. **Target.** DotNet netstandard2.1 baseline; additional transport targets selected for verified NET consumer. No implied HTTP3/SQLite/IL2CPP support.
9. **Native SQLite.** Selected core/provider.e_sqlite3 3.0.5, lib.e_sqlite3 native 3.53.3. Generic Windows x64 adapter validates native version/WAL/FULL, runs native work off-thread, and holds an OS canonical-path lock across sync/recovery. Real-file/process checks passed. Native Unity importer/AOT/Player gate remains open; unsupported initialization fails explicitly, no memory fallback.
10. **Exact dependencies.** Pinned release component set; Unity manifest/lock/Editor version, NET restore graph. No floating latest-compatible dependency as reproducibility.
11. **Assemblies.** Runtime/Editor/Tests/sample boundaries explicit; no DotNet duplicate via UPM/NuGet/Assets. Stable .meta and asmdef GUIDs committed with SDK source.
12. **First Player.** First required Demo Windows x64 / IL2CPP; Editor development only, Player acceptance. Android ARM64/VR/Linux/macOS/WebGL separately proven future matrix, not claimed current support.
13. **Unity patch.** 2022.3.62f3 fixed; Windows x64/IL2CPP first Player. Pin stripping/build settings/dependencies before spike; compatibility still unverified.
14. **AOT.** Generated roots/nested collections/attributes/converters/reflection paths verified. SDK/sample build preservation rules, consumer generated types preserved without editing historical .g.cs; actual stripping/Player proof.
15. **Transport.** UnityWebRequest/DownloadHandlerScript selected; HTTP/1.1 explicitly allowed for Demo 2022.3.62f3. Native Player must prove early headers, copied bounded queue/async stream, overflow/cancellation/quiescent cleanup. Neutral queue/budget and C# 8 syntax checks passed; actual Unity assemblies/Editor/Player not run. H2/H3 support is a separate future verified matrix, not assumed.
16. **Registry.** Intended project-level GitLab NuGet/npm registries for the owning SDK repositories. Exact host/project IDs/endpoints/access/runners are real installation settings still to be confirmed; no CI/publish/private install performed. npm @scope is not Unity UPM scope.
17. **UPM scope.** Registry scope com.leansoft.contentmanager only; other packages not intercepted. Actual host/project context deployment settings.
18. **Credentials.** Publish CI job token where permissions verified; consumer/developer read-only package access. Credentials user/CI configuration only, no source/package/sample; actual Unity auth tested.
19. **Clean install.** Exact registry install/dependencies/private access/used discovery/query required; successful npm publish not UPM proof.
20. **Fallback.** First immutable release tarball from same build if required registry capability fails. Proxy/alternate registry only evidence-backed need, not automatic new infrastructure.
21. **Release train.** DotNet/Http/Unity/SQLite one aligned release version; optional Addressables independent cycle with explicit supported SDK set.
22. **Immutability.** Same ID/version never overwritten different bytes; fix new version. SDK SemVer separate store/protocol/generator/content profiles.
23. **CI.** Validate -> tests -> pack -> clean consumer/Player acceptance -> publish validated build. Retry same artifacts, not rebuild under same release; partial publication not complete release set.
24. **F20.** Separate consumer scene/project belongs to F20; current packaged acceptance harness is not that scene. SDK code/source dependencies remain outside the consumer package graph.

До claim поддержки нужен exact tested release set и documented results. Repo URL/GitLab host/project ID/version, Editor patch, dependency/provider/native/transport targets/version lock и numeric caps получают из выбранного окружения/spike, не source-примеров. Полные public namespaces/shared SDK CLR references фиксируются с Q31/F22 до emitted fixtures; здесь package IDs не подменяют CLR identity. [GitLab npm registry](https://docs.gitlab.com/user/packages/npm_registry/), [Unity scoped registries](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-scoped.html), [Unity Newtonsoft package](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html), [Unity link.xml](https://docs.unity3d.com/2022.3/Documentation/Manual/ManagedCodeStripping.html) объясняют supported mechanisms; actual installation отдельно проверяется.

## Текущие локальные artifacts и незакрытая поставка

Local staging uses ContentManager.Canonicalization NuGet 0.0.0-f19.local.2 and aligned generic SDK/Unity UPM artifacts 0.0.0-f19.local.2. Exact dependencies: Newtonsoft.Json 13.0.3; official Unity Newtonsoft 3.2.2; SQLitePCLRaw core/provider 3.0.5 and native 3.53.3. Same generic DLL bytes are copied into local UPM; source/DLL inspection and local package validation are not registry installation proof. Historical canonical profiles/F22 generated source bytes remain unchanged.

GitLab CI, publish and private registry clean install have not been attempted. Actual repository/project endpoints, minimal read/publish permissions, runners, Unity managed compile/Editor/Player toolchain, stripping/build/importer locks remain open. F19 cannot be declared Done until mandatory Unity acceptance and supported packaging gates complete. Current bounded local and real CM/PostgreSQL/MinIO SDK results are recorded separately in [implementation evidence](../../backend/f19-demo-implementation-plan.md).

## Shared cache и две сборки launcher/app

Первый Editor/Player target зафиксирован: **2022.3.62f3 / Windows x64 / IL2CPP**. Generic serializer/SQLite dependencies выбраны выше; открыты managed stripping/build/importer settings, actual Unity native/serializer/transport proof и optional Addressables package. Не объявлять Linux/macOS/Android/WebGL поддержанными по netstandard2.1 или Editor smoke.

Пакеты сохраняют graph DotNet ports/общие algorithms → platform adapters. Последующая интеграция F19 добавляет download/cache coordinator, SQLite jobs/index и filesystem binary store; add-on custom entity/file providers не содержат вторую реализацию. Примерные public names/types/keys не являются release API. [Q37](Q37-launcher-asset-delivery.md) owns новые contracts/security/cache policy; [F19](../features/F19-unity-client.md#68-общий-файловый-cache-и-launcherapp-интеграция-после-demo) owns implementation responsibilities.

Приёмка дополнительно требует **две настоящие packaged Windows IL2CPP сборки** (launcher + app), один локальный SQLite/filesystem и process/network/crash fault injection: pinned old/new snapshot, join preload/on-demand, 10 ГБ bounded memory/long counters, durable resume, protocol/hash failure, fenced stale worker, rename/commit recovery, playing-video lease vs GC, disk shortage и истёкшая session/new admission. Custom GeneratedType/Image/file providers проверяются с реальным selected Addressables package/AOT/stripping, native bundles отдельно. Этой документацией builds/tests/install не выполнялись; standalone Demo не требует launcher/cache/add-on для acceptance.

## Уточнение логической структуры пакетов

Источник переименовал Sdk.Client.Core в Sdk.Client.DotNet. Последнее согласованное состояние дополнительно выделяет Sdk.Client.Store.Sqlite; graph/artifact IDs единственным normative owner [F19](../features/F19-unity-client.md#111-поставка-и-зависимости-пакетов). Schema.CodeGeneration.Core не переименовывается и не становится runtime dependency.

Http/Unity/Store.Sqlite -> DotNet, optional Addressables -> Unity/DotNet; backward Unity/SQLite dependency у DotNet отсутствует. Selected package IDs и physical assembly names F19; full public CLR namespaces/shared binding refs закрепляются с Q31/F22 до source fixtures. Независимые non-NET SDK используют formal API contracts, не обязаны импортировать эти packages.

## Риск из источника

Документ отмечает необходимость проверить поддерживаемый Unity UPM discovery/query flow и endpoints `/-/v1/search` / `/-/all` на конкретной версии self-hosted GitLab. Это зафиксированный в исходном документе риск, а не независимо подтверждённый текущий дефект вашей инсталляции. Версия GitLab, endpoint и результаты spike не предоставлены.

## SDK/store conformance на выбранной платформе

[Q29 public/store contract](Q29-unity-client-package.md#public-sdk-и-локальный-store-согласованный-контракт) и [F19](../features/F19-unity-client.md#64-store-port-и-sqlite-adapter) фиксируют source/partition scope, explicit bindings, typed async API/results, short atomic store operations, detached reads, group-bound pages и selected generic Windows x64 SQLite adapter. Provider/native versions и реальный canonical-path process-held lock проверены generic tests; before declaring Unity support verify actual native importer/AOT/Player integration. Lock spans whole sync/recovery, SQL transactions short; crash releases process-held ownership, no SQLite transaction lock as sole high-level gate. Blocking provider calls off Unity main thread; Task method not proof of asynchronous I/O.

Shared adapter conformance uses real local files/process barriers for stage/checkpoint/atomic apply/receipt and canonical-path process ownership, typed committed reads/mutation isolation and format/native refusal. Generic Windows x64 native 3.53.3/WAL/FULL is checked; this does not prove Unity native imports, IL2CPP/stripping, migration on unselected formats or arbitrary filesystems. In-memory/network doubles isolate protocol only; unsupported lock/native/durability guarantee refuses initialization.

[Raw/image contract Q29](Q29-unity-client-package.md#raw-assets-и-unity-image-helper-согласованный-контракт) adds required real gateway/Player matrix: identity bytes/response headers/disposal/large streaming/truncated and size-SHA mismatch,206/200/416/If-Range, caller stream partial writes/no retry and own-temp no-overwrite final move, actual verified JPEG/PNG LoadImage under limits, main-thread/queued cancellation/native decode/handoff/Destroy/no CPU-readable copy. Each case owns test files/streams/resources/temp outputs, no real user/production bucket mutation. Doubles cannot prove upstream abort, atomic filesystem behavior/native decoder/GPU lifetime; exact pinned dependencies/numeric caps and observations recorded before support claim.

## Проверка интеграции

| Шаг | Что должно быть подтверждено |
| --- | --- |
| Публикация | CI формирует package и публикует в выбранный registry |
| Exact install | Clean Unity 2022.3.62f3 Windows x64/IL2CPP consumer installs exact accepted SDK release; real Editor patch/stripping/build settings pinned before spike, not yet measured |
| Discovery | Нужные поиск/перечень/query операции UPM работают или явно зафиксировано ограничение |
| Private access | Выбранный минимально достаточный developer/CI access работает без секретов в репозитории |
| Dependencies | DotNet/Unity/serializer и выбранная SQLite native library совместимы в fixed Player/AOT/platform наборе; assemblies не дублируются; add-on optional |
| Update/downgrade | Переход версий и восстановление зафиксированного набора воспроизводимы |
| Lock | Проверены Packages/manifest.json и packages-lock.json в выбранном сценарии |
| Names/endpoints | Accepted NuGet/UPM IDs from F19; SDK project-level GitLab endpoint actual host/project ID/version configured, scoped registry com.leansoft.contentmanager; npm @scope not UPM scope |

Успешный npm publish сам по себе не подтверждает установку/обнаружение в Unity. Тесты не запускались в рамках внесения документации.

## Открытые решения

**Открыты actual environment/results:** GitLab version/host/project endpoints, actual access/runners, Unity 2022.3.62f3 stripping/build/importer locks и managed compile/Editor/Player/native results. Selected generic dependency set, client defaults, two repositories и HTTP/1.1 Demo policy описаны выше/F19; их выбор не означает registry publication или Unity support. Full public CLR exact F22 bindings сохранены. SQLite native adapter support в Unity остаётся условием platform proof.

Source-примеры com.company.*, release1.0.0 и unity6000.0 не назначают реальные versions. Actual tokens/access не выданы документом; CI job token publish/read-only consumer direction выбран, permission/auth capability проверяется на установке.

При недостаточной registry capability первый выбранный fallback — immutable release tarball того же validated build. Новый proxy/registry требует подтверждённой потребности; runtime contracts неизменны. Discovery source-risk не объявляется доказанным defect вашей GitLab.

## MVP CLI: поставка tooling

По [Q31](../../backend/questions/Q31-schema-code-generation.md#mvp-cli-конфигурация-и-файловый-workflow) выбран отдельный local .NET tool package `ContentManager.Schema.CodeGeneration.Cli`, command `cm-schema`. Package version pin — standard `.config/dotnet-tools.json`, restore через .NET local tool workflow. Это не UPM/SDK runtime dependency и не общий SDK base train; profile version отдельно от NuGet SemVer. Config/filesystem модели и use cases принадлежат [backend F22](../../backend/features/F22-csharp-model-generation.md#82-mvp-cli).

**Уточняется engineering:** actual CLI/Core repository/project placement, Core artifact/package binding, target TFM/runtime/dependency versions, feed/publication/access context и supported host OS matrix. Название tool package выбрано, публикация/установка ещё не выполнены. SDK first Unity Windows x64/IL2CPP не является автоматически target/platform matrix консольного tool.

Acceptance: install pinned exact packaged local tool в clean consumer/CI, offline generate/check с committed config/lock/raw inputs и real compiler, real filesystem/subprocess crash/concurrency/ownership/.meta checks. Same Core bytes с backend на same input/profile обязательны. Actual registry/feed/private install и CI permissions проверяются отдельно; tool restore не Registry staff login. Developer authorization владелец [Q24](Q24-admin-sign-in.md#developer-tooling-доступ-cli).

## Применение

[SDK F19](../features/F19-unity-client.md), [Addressables F23](../features/F23-unity-addressables.md), [сцена F20](../features/F20-unity-demo-scene.md). В milestones отдельно указываются обязательность библиотеки и ещё не подтверждённая транспортная работоспособность registry. Протокол runtime CM и лицензирование не определяются этим вопросом.

## F23: матрица поставки add-on

[F23](../features/F23-unity-addressables.md) является отдельной optional package/asmdef boundary. Логическая зависимость: Sdk.Client.Unity.Addressables → Sdk.Client.Unity, Sdk.Client.DotNet и выбранный Unity Addressables package. Базовые DotNet/Http/Unity и сцена F20 не зависят от add-on обратно. Пакет SDK, Unity content release и Mapping Manifest — разные артефакты; установка UPM не подтверждает готовность catalog/bundles.

**Уточняется:** exact Addressables/dependency versions and supported base release combination, actual additional platform matrix/reflection/asmdef/native proof. Base first target Windows x64/IL2CPP and supplied DotNet DLL/shared IDs selected F19; optional add-on ID com.leansoft.contentmanager.unity.addressables, separate version cycle. None of npm publish/Editor build proves add-on/Player/content compatibility.

| Проверка / SUT | Собственные данные и зависимости | Критерий |
| --- | --- | --- |
| Базовый SDK без add-on | Чистый Unity 2022.3.62f3 проект, выбранные DotNet/Http/Unity packages, raw sample F19/F20 | Разрешает зависимости и использует базовый data/raw путь без обязательного Addressables |
| Add-on exact install | Отдельный чистый Unity проект с точными выбранными SDK/Addressables versions | Reproducible resolve/import/compile; dependency graph не дублирует assemblies; version locks соответствуют выбранному набору |
| Реальный local content slice | Собственный manifest AssetId → key, реально построенные test catalog/bundles и несколько разных target kinds | Load/use/release, повторная/конкурентная загрузка, type mismatch, missing mapping/key и отмена выполняют contract Q29. Тест с operation double не заменяет этот runtime check |
| Prefab instance lifetime | Собственный prefab/dependencies, выбранный supported load/instantiate path | Instance живёт корректно в принятой области; dependencies удержаны и освобождены по выбранному контракту. Короткий using sample не является доказательством |
| Explicit Preload | Собственный набор с успешным и ошибочным элементами | Проверены реальный preload mode, per-item/result semantics, cleanup, повторное Load и cancellation; нет false all-ready |
| Player / threads / teardown | Выбранные Player platforms/scripting backends, реальный content, scene/app teardown | Работают type loading/async/thread dispatch/release в поддержанной matrix; Editor success не подтверждает Player/AOT |
| Remote content при включении этапа | Два собственных согласованных набора manifest/catalog/bundles, контролируемая HTTP/CDN test surface, interruption/mismatch | Подтверждена policy переключения из Q29, поведение старых handles, cleanup неуспешного кандидата, failure/retry; не возвращается ложная готовность нового набора |
| Upgrade / downgrade | Предыдущий и новый exact package sets, отдельно их content contexts | Восстанавливается выбранный набор, выявляются unsupported combinations; SemVer пакета не подменяет manifest/content identity |

Fixture владеет своими Unity project, catalog/bundles, remote test files и operation handles; освобождает их по контракту и удаляет только собственные test artifacts. Не требовать общего production bucket/catalog или ручной предварительной загрузки данных. Контроль content lifetime нужен даже при общем реальном cache Addressables; межтестовое случайное reuse не должно маскировать missing download или cleanup.

Имена пакетов, ссылки registry, secrets/access, limits/backoff и release pipeline не выбираются таблицей. Exact signatures, manifest contract, lifetime/cancellation/update policy остаются в [Q29](Q29-unity-client-package.md#f23-mapping-snapshot-и-ownership-операций). Проверки здесь — план верификации, не выполненные Unity/UPM/Player tests.

## Актуальный generic этап — 10.10.2026

По уточнению владельца рабочий generic репозиторий — **contentmanager.sdk.client**; contentmanager.sdk.client.dotnet является старым источником, его не удаляют. В новом репозитории ровно три production csproj/NuGet: ContentManager.Sdk.Client.DotNet, ContentManager.Sdk.Client.Store и ContentManager.Sdk.Client.Store.Sqlite. Store владеет IClientStateStore/immutable metadata/typed errors в namespace ContentManager.Sdk.Client.Store и не зависит от DotNet или SQLite; DotNet и SQLite зависят от Store. F22 value types/attributes/converter остаются в ContentManager.Sdk.Client.DotNet / assembly ContentManager.Sdk.Client.DotNet. DotNet имеет netstandard2.1 и net10.0; HttpClientTransport включён только в net10.0, отдельный Http пакет не выпускается. Store/Sqlite — netstandard2.1.

Порядок: закончить generic source + tests + NuGet → передать владельцу для CI/CD/publication → владелец устанавливает exact NuGet в Unity SDK → только после этого продолжать Unity. Межрепозиторные ProjectReference/linked source из Unity в generic запрещены; Unity использует PackageReference. Уже созданный Unity source остаётся предварительным, не принят и сейчас не изменяется; предыдущие UPM artifacts не подтверждают новый трёхпакетный graph. Наличие/отсутствие Editor не блокирует текущий generic этап.

Владелец перенёс canonicalization source в ContentManager.Sdk.Client.DotNet. Код находится в namespace ContentManager.Sdk.Client.DotNet.Canonicalization; отдельного NuGet ContentManager.Canonicalization и обязательного owning feed больше нет. Алгоритм/profiles/canonical bytes сохранены. Поставка остаётся из трёх SDK пакетов; actual pipeline/private install/publication выполняет владелец. Межрепозиторных source references нет.

### Тесты и CI generic SDK — 10.10.2026

Тесты переведены на xUnit v3 с отдельными Fact/Theory и категориями Unit/Integration; запуск через dotnet test, без рукописных Program.cs runners. Итоговый Release build обоих DotNet targets и Store/Sqlite: 0 warnings/errors; dotnet test: 200 passed, 1 skipped (CM_F19_TEST_API не задан). После переноса canonicalization source restore не требует отдельного Canonicalization пакета; private feed не проверялся. Отдельный package-only xUnit consumer: 1/1 passed после упаковки трёх Release SDK, restore из локального feed с пустым cache; в assets ровно три SDK package dependencies, без ProjectReference. Временные feed/cache/build outputs удалены. CrashHost — отдельный non-test/non-packable helper для реального аварийного завершения процессов SQLite. Windows x64 native сценарии явно пропускаются на неподдерживаемой платформе; CM integration явно пропускается без CM_F19_TEST_API.

CI использует имеющиеся GitLab dotnet components1.15.1: staging → rc tag/package, main → stable tag/package; pack targets — ровно три production csproj. Build/test предшествуют release и pack; deploy получает artifacts своего pack. Actual GitLab pipeline не запускался. Скрипты ps1 и временные отчёты хешей не входят в поставку. Unity ждёт установки NuGet владельцем; для будущих UI проверок выбран bUnit, без JS tests.

## Unity после установки generic NuGet — 10.10.2026

Владелец установил ContentManager.Sdk.Client.DotNet/Store/Store.Sqlite 1.0.0 в contentmanager.sdk.client.unity. Handoff выполнен; прежнее ожидание выше относится к предыдущему этапу. Unity использует exact PackageReference трёх пакетов, без межрепозиторного ProjectReference/linked source и отдельного Canonicalization пакета. Addressables относится к отдельной optional F23; существующий scaffold не входит в реализацию F19.

Production source находится обычными файлами рядом с csproj в src/ContentManager.Sdk.Client.Unity: UnityContentManagerClient, UnityWebRequestTransport, UnityMainThreadDispatcher, BoundedDownloadBody, DecodeObservationBudget, UnityTexture. Manifest/asmdef/linker/plugin metas находятся в Packaging. Build создаёт .upm/com.leansoft.contentmanager.unity из этих исходников и установленных NuGet DLL. .upm/ игнорируется Git; tracked files в нём отсутствуют. Никакие production Compile items не ссылаются на generated UPM output. Guard отклоняет сборку без runtime source и реальных Unity assemblies.

UPM содержит три portable SDK DLL, SQLitePCLRaw core/provider 3.0.5 netstandard2.0 и native e_sqlite3 из lib 3.53.3. Stable metas сохраняются; native importer включает Windows Editor x64/Standalone Win64. Только официальный com.unity.nuget.newtonsoft-json 3.2.2 поставляет Newtonsoft, дополнительной DLL нет. Generated F22 fixture и production serializer не менялись.

Default Release build solution без переопределения UnityManagedPath прошёл с 0 warnings/errors. Directory.Build.props выбирает явно заданный Editor/managed path либо установленный Hub Editor. 30 neutral/package и 3 cancellation xUnit tests прошли. Regression pre-header cancellation возвращает public raw OpenRead/HEAD Cancelled, вместо Failed.

В tests/UnityProject создан настоящий Unity test-проект, который импортирует локальный generated .upm и фиксирует dependency lock. Фактически запущен Unity 6000.6.5f1 Editor в batchmode/PlayMode: **3 passed, 0 failed, 0 skipped**. Проверены native SQLite создание файла и exact missing-key lookup с регистрацией неизменённой F22 all15 модели; реальные UnityWebRequest headers/первый fragment до завершения body; verified PNG → non-readable Texture2D без mipmaps, сохранение application-owned texture после client shutdown и Destroy после освобождения handle. Это Editor/native/GPU execution, не Player/AOT/stripping acceptance. Cache/log/test outputs игнорируются, Editor завершился после тестов.

Обязательная проверка согласованного **Unity 2022.3.62f3 Windows x64 IL2CPP Player** остаётся открытой: соответствующие Editor/Windows IL2CPP module/C++ tools не доступны в проверенной установке. Unity 6 Editor не доказывает совместимость с лаунчером на 2022.3. Нужны реальные Player import/build/run, AOT/stripping, queue overflow, stream/Range failures, SQLite recovery и CM/PostgreSQL/MinIO sync через Unity transport. Consumer сохраняет собственные F22 generated roots/enums/properties и closed generic collections; package link.xml сохраняет SDK assemblies. **F19 целиком не завершена** до обязательной Player-проверки.

В этом Unity этапе backend/контейнеры/Player не запускались; использовался тестовый loopback HTTP. CI/push/publication/private-registry install/коммиты не выполнялись. Scripts ps1 и отчёты package hashes не входят в репозиторий.

## UPM-миграция F19 — 11.10.2026

Уточнение владельца имеет приоритет над MigrationKit: F19 остаётся без Addressables, но отдельная F23 включена в Demo и будет реализована позднее. Основной UPM ID сохраняется **com.leansoft.contentmanager.unity**. Будущий add-on зависит от основного пакета, основной asmdef не зависит от Addressables и один поставляет общие SDK DLL. Generic SDK packages не пересобирались, public contracts/runtime source/generated serialization не изменялись.

Актуальная структура: **src/com.leansoft.contentmanager.unity/Runtime** содержит обычные Unity исходники/asmdef/link.xml с сохранёнными .meta GUID; package.json находится в корне этого UPM. Внутри распространяемого пакета нет csproj/slnx. Build/DependencyRestore восстанавливает три опубликованных NuGet 1.0.0 для netstandard2.1 с committed packages.lock.json; build/PackageAssembler читает project.assets.json/packageFolders, сверяет полный graph ID/version/NuGet hash metadata с lock, проверяет CLR identities, собирает .artifacts/upm и кандидат .tgz. DLL hash reports/dependencies.json/SHA256SUMS/ps1 не создаются. Stable plugin importer templates находятся в build/plugin-metadata; временные DLL/cache/results/.artifacts и generated embedded package исключены Git, source/meta/ProjectSettings/locks остаются учитываемыми файлами.

Корневая slnx теперь содержит только restore/build/test infrastructure. Существующие xUnit tests сохранены; UnityAdapter.Runtime — test-only managed compile bridge реальных Editor modules, не production UPM/DLL. Tests/UnityProject импортирует **generated embedded** пакет, его manifest сохраняет существующий ID, packages-lock.json подтверждает source:embedded. User-owned NuGet.Config не менялся; CI использует отдельный build/NuGet.CI.Config с фактическим alldirect.ru feed и process/env authentication. Прямой private install/feed availability не проверялся; restore выполнен offline из уже установленного cache.

Фактические результаты: Release build 0 warnings/errors, **52 xUnit passed** (49 neutral/package/assembler, 3 adapter); 19 assembler checks включают missing native/managed, incompatible TFM/RID, conflicts, GUID/metadata, credential/build file rejection, stale graph/CLR name, archive reproducibility/content и embedded replacement. Два defect regressions ревью наблюдались RED→GREEN. Unity **6000.6.5f1 Editor: 3 EditMode + 3 PlayMode passed**, реальные native SQLite/HTTP early headers/PNG Texture2D lifecycle/Destroy сохранены. Editor процессы завершены.

Минимальный CI подготовлен: locked restore → assembled UPM → Linux .NET tests + Windows host/Unity EditMode/PlayMode → .tgz для stable/rc tag, совпадающего с package version. Zero-test failure/Editor version/exit/XML/process cleanup проверяются. Runner unity-windows требует PowerShell7/.NET10/licensed Editor из ProjectVersion, UNITY_EDITOR_PATH и Generic GitLab CI_JOB_TOKEN allowlist. Pipeline/push/publication/private registry install не запускались; publish job отсутствует до отдельной проверки registry/UPM handoff. .tgz не объявлен Unity-signed.

Обязательный **2022.3.62f3 Windows x64 IL2CPP Player** и реальный CM/PostgreSQL/MinIO sync через Unity transport ещё не проверены. Unity6 Editor не заменяет эти gates; F19 не объявлена завершённой. Backend/контейнеры/Player/коммиты не запускались в этой миграции.

## Проверка поставленного пакета с реальным CM

11.10.2026 собранный embedded com.leansoft.contentmanager.unity с опубликованными NuGet 1.0.0 прошёл два real-CM PlayMode tests в Unity6000.6.5f1; полный PlayMode suite 5/5 без skip, .NET tests 52/52. Новые проверки используют реальные F22 generated roots, native SQLite и image lifetime; фикстуры/модели/результаты генерируются в игнорируемых каталогах. Условия повторения и cleanup описаны в README Unity-репозитория, фактические границы — в [F19](../features/F19-unity-client.md#проверка-unity-sdk-с-реальным-cm). CI/publication/private registry install и Unity2022.3.62f3 IL2CPP Player этим прогоном не подтверждены. Зависимости и lock-файлы не менялись; общий SDK не пересобирался.

## F23: локальная поставка и проверенный Editor профиль

Source UPM com.leansoft.contentmanager.unity.addressables1.0.0 зависит от base1.0.0 и Addressables1.22.3 minimum; consumer фиксирует exact Addressables в manifest/Unity lock. В поставке add-on нет DLL, csproj/slnx или повторного Newtonsoft. PackageAssembler валидирует source/metadata/dependencies и отдельно assemble/embed/pack; base package независим. Все реальные NUnit tests импортируют assembled embedded add-on. Внешний csproj нужен Rider/host compile и читает реальные Unity Addressables DLL после import; он не распространяется.

Установленный diagnostic Editor6000.6.5f1 проверен с exact Addressables2.11.2. Official1.22.3 восстановлен, но этот Editor выдаёт CS0619 на GetInstanceID внутри стороннего пакета; package source не патчили. Фактические tests/границы — [F23](../features/F23-unity-addressables.md#102-фактическая-проверка-локального-demo). Target2022.3.62f3/IL2CPPPlayer и cold/private-registry install/CI/publication остаются непроверенными. CI собирает оба source packages и real local bundles, требует обязательные native tests без skip; отдельный add-on .tgz доступен локальным pack CLI, без привязки его версии к base release tag и без публикации.

## Публикация UPM из CI — 20.10.2026

Наблюдавшийся успешный `assemble-upm` сохранял только job artifacts: загрузки в Package Registry не было. В исходниках Unity SDK добавлена цепочка tag pipeline `pack-upm` → `publish-upm` для project-level npm endpoint `${CI_API_V4_URL}/projects/${CI_PROJECT_ID}/packages/npm/` с `CI_JOB_TOKEN`. Branch/MR pipeline остаётся проверочным. Публикация ждёт предыдущие стадии, включая существующие security jobs; Unity Editor tests по указанию владельца исключены из CI и сохранены для локального запуска.

Release tag должен совпадать с `package.json` хотя бы одного assembled пакета. Упаковываются и публикуются только совпавшие версии: текущий `v1.0.0` выбирает base и Addressables; отдельная версия add-on не заставляет перевыпускать base. Package IDs сохранены; generic NuGet DLL не пересобираются. Stable использует npm dist-tag `latest`, `-rc.N` — `rc`. Перед любым upload job проверяет все выбранные версии; существующая версия или ошибка проверки кроме `E404` останавливает публикацию. Resource group сериализует эти publication jobs. Это не серверная блокировка внешних publishers: GitLab может принимать duplicate versions, поэтому вне job их одновременно не публикуют. Частичный upload не считается завершённым release set; перед повтором требуется проверить registry.

Локально выполнены **66 .NET tests без ошибок и skip** и упаковка двух реальных `.tgz`; проверены manifest identity/version и отсутствие DLL в add-on. Проверки shell/Node syntax и `npm publish --dry-run` обоих архивов прошли; dry-run использовал localhost registry и не публиковал пакеты. Временные outputs остаются в игнорируемой `.artifacts/`, новых ps1/hash reports нет. GitLab pipeline, publication и private-registry Unity install агентом не запускались. Actual registry permissions/forwarding/UPM discovery и clean consumer install остаются непроверенными; локальный архив не доказывает registry handoff или target IL2CPP acceptance.

## Автоматические release tags Unity SDK — 21.10.2026

По явному ответу владельца выбран автоматический workflow generic SDK вместо ожидания ручного тега. В Unity CI подключены существующие `release-workflow` и `release-version` components 1.15.1 с теми же inputs: staging → rc, main → stable с aggregate_pre_releases. Release jobs стоят после build/test/security без обхода stage ordering. Они создают тег; `package`/`publish` выполняются в отдельном tag pipeline, поэтому в первоначальном branch pipeline эти стадии по-прежнему не появляются. Actual release component expansion, release-triggering commit rules и credentials/project permissions проверяются в GitLab, а не выводятся из локального YAML.

`pack-ci-release` валидирует оба assembled UPM, создаёт временные копии, ставит tag version в оба package.json и exact base dependency add-on, валидирует и архивирует, затем удаляет staging. Inputs, SDK DLL, native bytes, .meta и lock-файлы не меняются; NuGet generic SDK не пересобирается. Это совместная автоматическая поставка двух UPM. Прежний `pack-release` сохраняет строгий выбор manifests с совпадающей версией для независимых ручных кандидатов. Историческое требование совпадения committed package.json с каждым автоматическим тегом из предыдущей секции этим решением заменено.

Новые archive tests наблюдались RED до реализации; полный локальный suite **70 passed, 0 failed/skipped**. Реальный CLI собрал оба `1.0.1-rc.1.tgz`, оба прошли локальный `npm publish --dry-run` с registry localhost, без публикации. Shell syntax проверен; diff проверен и прошёл отдельное read-only ревью. Actual GitLab CI lint с раскрытием private components, pipeline/tag creation, upload и private Unity install не выполнялись. Unity Editor job не возвращён; backend, контейнеры и Unity не запускались.

## Versionize: ошибка ProjectSettings.asset в release job — 21.10.2026

Присланный владельцем log подтверждает фактический запуск `release_version:staging` для commit 40feafd и установку Versionize 2.6.0. Job завершился `ProjectSettings.asset not found` до записи release.env; warning об отсутствии dotenv artifact — следствие основной ошибки. Не следует добавлять фиктивный Unity ProjectSettings в корень UPM-репозитория.

Из локального checkout CI components прочитан точный `1.15.1:templates/release-version/template.yml`: подготовка/auth/full-history и вызов `versionize -i` действительно соответствуют логу. Ошибка воспроизведена локально той же версией Versionize 2.6.0 в dry-run под Windows-владельцем checkout (exit 1). Корневая `.versionize` с `tagOnly: true` отключает сканирование project version files; повторный dry-run прошёл (exit 0), исходники не менялись.

Дополнительная проверка выявила: без tags первый release при `--pre-release rc` всё равно выбирает stable 1.0.0. Для staging переопределён только script release job: если version tags отсутствуют, добавляется `--release-as 1.0.0-rc.0`, далее остаётся штатный prerelease calculation. Компонент сохраняет before_script/auth/rules/GIT_DEPTH/artifacts; main использует прежний стандартный script. Shell syntax и dry-run подготовки args из staging script подтвердили 1.0.0-rc.0; main dry-run подтвердил 1.0.0. Реальные commits/tags не создавались; CHANGELOG.md не появился, SDK DLL/locks/runtime source не менялись.

Полный локальный .NET suite повторён: **70 passed, 0 failed/skipped**. Выполнено read-only ревью. Versionize установлен только как временный public tool в игнорируемую .artifacts, private package install не выполнялся. Новый GitLab pipeline, git push, release/tag creation и registry publication агентом не запускались. Для применения исправления нужен новый pipeline с commit, содержащим конфигурацию; повтор старого job использует прежний checkout.

## F20: фактический private UPM consumer install — 21.10.2026

Владелец сообщил, что оба Unity UPM опубликованы, и разрешил взять package-read credential из CM Host NuGet.Config. Metadata GET подтвердил фактическую версию 1.0.0-rc.0 обоих package IDs в project127 npm registry. В DigitalSpaceLauncher и DemoApplication добавлены exact registry dependencies и scope com.leansoft.contentmanager; те же настройки во всех четырёх target/diagnostic profiles. Credential записан только в пользовательский .upmconfig.toml вне репозитория; auth сервера CM не менялась.

Unity6000.6.5f1 PackageManager действительно установил оба пакета в существующие consumers, active locks имели source:registry/version1.0.0-rc.0 и add-on exact base dependency1.0.0-rc.0. Locks сохранены в diagnostic profiles; полностью очищенный global package cache и Unity2022 resolver не проверялись. NUnit17 Launcher +4 child прошли, включая настоящий native SQLite. Два Windows x64 Mono EXE пересобраны и завершили smoke с exit0/errors0. Визуальный render не заявляется; smoke проверял UI objects и понятные отказы при отсутствующих config/bootstrap.

Пакетный handoff в Unity6 consumer теперь подтверждён фактическим install, а не только npm publish. По ответу владельца CM ещё не готов; real sync/assets/typed fixture/offline и целевой2022.3.62f3 IL2CPP Player остаются открытыми. SDK/UPM source не копировался в приложения; generic DLL не пересобирались. Owned приложения после проверок завершены. CI/push/publication/private data mutations агентом не выполнялись.
