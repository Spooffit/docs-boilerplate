# Целевая структура backend solution

## 1. Общий принцип

VRSchool.ContentManager остаётся одним deployable .NET 10 приложением, но код разделён по bounded contexts и техническим модулям.

Цель структуры:

- Domain/Application одного BC не зависит от persistence/transport;
- BC не читают чужие DbContext/repositories;
- cross-BC synchronous read идёт через public contract/port;
- cross-BC side effect идёт через integration event/outbox;
- compile-time зависимости не образуют циклы;
- физическое разделение на сервисы в будущем возможно без переписывания Domain.

Отдельный Kubernetes/service deployment на каждый BC сейчас не требуется.

## 2. Composition root

Единый host/composition project:

```text
ContentManager.Host
```

Если в актуальном solution host уже называется иначе, сохраняется существующее имя; важна ответственность, а не переименование ради документации.

Host:

- собирает DI всех BC;
- конфигурирует ASP.NET Core pipeline;
- регистрирует DbContext каждого BC;
- подключает MassTransit/RabbitMQ;
- подключает S3-compatible IObjectStorage adapter для upload/delete/verification/recovery;
- регистрирует CM-owned read-only YARP media gateway/route/cluster/transforms и выбранный upstream access/signing; protected access gate до optional output cache/proxy — F05/F13/Q21;
- подключает VR School HTTP client;
- применяет auth/authorization MVP;
- поднимает health/metrics/tracing.

Host не содержит domain/application business logic.

## 3. Шаблон проектов bounded context

Для полноценного BC целевая структура:

```text
ContentManager.<BC>.Domain
ContentManager.<BC>.Application.Abstractions
ContentManager.<BC>.Application
ContentManager.<BC>.EntityFramework
ContentManager.<BC>.EntityFramework.Persistence
ContentManager.<BC>.Infrastructure
ContentManager.<BC>.Integration
ContentManager.<BC>.Api
```

Не каждый BC обязан иметь непустой проект каждого типа. Если adapter/transport отсутствует, пустой проект не создаётся ради симметрии.

### Domain

Содержит:

- aggregate roots;
- entities;
- value objects;
- domain enums;
- domain services, если правило не принадлежит одному aggregate;
- domain errors;
- domain events, имеющие смысл внутри BC.

Не содержит:

- EF attributes/configuration;
- HTTP DTO;
- MassTransit consumer;
- S3/HTTP client;
- repository implementation.

### Application.Abstractions

Содержит:

- commands/queries;
- request/response/application DTO;
- public read contracts;
- consumer-owned outbound ports;
- repository interfaces, если они являются application port;
- result/error contracts уровня use case.

Wire DTO не содержит EF technical `Version`. HTTP concurrency находится в ETag/If-Match.

### Application

Содержит:

- command/query handlers;
- validators;
- mappers;
- orchestration use case;
- algorithms/application services;
- calls outbound ports.

Application не зависит от Api/EntityFramework/Infrastructure/Integration.

### EntityFramework

Содержит:

- DbContext BC;
- entity/value converters;
- IEntityTypeConfiguration;
- migrations/migration assembly;
- database schema naming;
- EF concurrency mapping internal `int Version`.

### EntityFramework.Persistence

Содержит:

- repository implementations;
- Specification/query adapters;
- optimized read projections;
- persistence-specific batch operations;
- DI registration persistence layer.

Зависит от own Domain/Application.Abstractions/EntityFramework, но не от чужого persistence.

### Infrastructure

Outbound adapters:

- external HTTP;
- object storage;
- consumer-owned cross-BC port adapters;
- crypto/token generation;
- process-local plan/cache adapters;
- other non-EF infrastructure.

Infrastructure может ссылаться на **Application.Abstractions другого BC**, если реализует consumer-owned port. Core Domain/Application проектов cross-BC reference не получают.

### Integration

Inbound/outbound durable messaging adapters:

- MassTransit consumers;
- mapping versioned integration contracts -> commands;
- outbox/inbox registration specific to BC;
- message topology endpoints from project standard.

Consumer не содержит business logic: после validation/mapping отправляет command/query в Application.

### Api

Содержит:

- MVC controllers;
- model binding;
- OpenAPI metadata;
- authorization attributes;
- ETag/If-Match HTTP adaptation;
- HTTP result mapping.

Api зависит от Application.Abstractions/shared API infrastructure. Handler/domain/persistence logic в controller запрещена.

## 4. ContentModeling

```text
ContentManager.ContentModeling.Domain
ContentManager.ContentModeling.Application.Abstractions
ContentManager.ContentModeling.Application
ContentManager.ContentModeling.EntityFramework
ContentManager.ContentModeling.EntityFramework.Persistence
ContentManager.ContentModeling.Infrastructure        // cross-BC Asset checks/read adapters if needed
ContentManager.ContentModeling.Api
```

Основные roots:

- EntitySchema;
- EntitySchemaVersion;
- EntityInstance;
- EntityInstanceVersion.

PostgreSQL schema:

```text
content_modeling
```

F01–F04 являются нормативной моделью.

## 5. MediaLibrary

```text
ContentManager.MediaLibrary.Domain
ContentManager.MediaLibrary.Application.Abstractions
ContentManager.MediaLibrary.Application
ContentManager.MediaLibrary.EntityFramework
ContentManager.MediaLibrary.EntityFramework.Persistence
ContentManager.MediaLibrary.Infrastructure
ContentManager.MediaLibrary.Api
```

Infrastructure содержит S3-compatible `IObjectStorage` adapter для business writes/recovery. Обычный binary read доставляет Host YARP напрямую от private storage после applicable access gate; metadata queries не byte pump. Не создавать отдельный AWS/MinIO/YARP domain layer; concrete provider/config/prefix/cache profile — Q21/F05.

PostgreSQL schema:

```text
media_library
```

Основной root: MediaAsset.

Demo может сразу использовать этот target BC: временно переносить F05 в ContentModeling теперь нецелесообразно, поскольку финальный owner уже определён.

## 6. CatalogAccess

```text
ContentManager.CatalogAccess.Domain
ContentManager.CatalogAccess.Application.Abstractions
ContentManager.CatalogAccess.Application
ContentManager.CatalogAccess.EntityFramework
ContentManager.CatalogAccess.EntityFramework.Persistence
ContentManager.CatalogAccess.Infrastructure
ContentManager.CatalogAccess.Integration
ContentManager.CatalogAccess.Api
```

PostgreSQL schema:

```text
catalog_access
```

Domain/projections:

- Product;
- ProductVariant;
- License/grants projection;
- CatalogConflict.

Integration принимает VR School state-carrying snapshots.

[F06](features/F06-product-catalog.md) определяет Product/Variant V2 wire/normalized snapshots, CatalogSourceState/CatalogEventReceipt/CatalogWorkItem/CatalogIngestionFault once; F15 reuses them. Integration/RabbitMq admits trusted V2 and maps normalized command; own Application receiver atomically accepts newest candidate/receipt/work before ACK, worker separately assesses/applies under the same MVP guard. Persistence owns claim/recovery/wakeup/receipt cleanup; queries return accepted vs applied/readiness/current assessment and scoped technical fault via public ports, not foreign SQL. Single active payload owner receive-state→unresolved Variant→Open F15.PendingState; stale work fenced, accepted newer/fault boundary prohibits old apply. Protected reconciliation command belongs VRS producer composition, not CM UI/API. [Q23](questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2)/[Q20](questions/Q20-persistence-and-migration.md#catalog-v2-receive-work-и-fault-boundary) retain exact codec/DDL/transport registration work; License/security epoch boundaries separate.

Infrastructure реализует outbound impact ports, например проверку Publishing dependencies при Variant move, не создавая compile-cycle core projects.

## 7. Publishing

```text
ContentManager.Publishing.Domain
ContentManager.Publishing.Application.Abstractions
ContentManager.Publishing.Application
ContentManager.Publishing.EntityFramework
ContentManager.Publishing.EntityFramework.Persistence
ContentManager.Publishing.Infrastructure
ContentManager.Publishing.Integration
ContentManager.Publishing.Api
```

PostgreSQL schema:

```text
publishing
```

Основные roots/state:

- ProductContentAssignment;
- ProductVariantContentAssignment;
- ProductContentRelease;
- ProductVariantContentRelease;
- ProductVariantPublicationRevision;
- ProductPublicationState;
- ProductVariantPublicationState;
- ProductReleasePromotionOperation;
- Product/Variant entitlement policy state;
- PolicyAgreement — immutable planned-period promise с original RequestId/input replay, без выдачи entitlement;
- ModerationIteration.

Infrastructure реализует outbound ports в CatalogAccess/ContentModeling/Identity context.

Integration отправляет Delivery transitions и принимает durable cross-boundary events, где это требуется.

## 8. Delivery

```text
ContentManager.Delivery.Domain
ContentManager.Delivery.Application.Abstractions
ContentManager.Delivery.Application
ContentManager.Delivery.EntityFramework
ContentManager.Delivery.EntityFramework.Persistence
ContentManager.Delivery.Infrastructure
ContentManager.Delivery.Integration
ContentManager.Delivery.Api
```

PostgreSQL schema:

```text
delivery
```

Domain:

- ProductVariantDeliveryState;
- DeliveryPublicationSnapshot;
- DeliverySession;
- EntitlementPolicyBinding;
- DeliveryActivationRecord.

Binding хранит exact Publishing AgreementId/policy provenance; current confirmed period facts и CurrentPeriodId принадлежат CatalogAccess, без foreign FK/общей transaction. Модели и ownership описаны в [F12](features/F12-content-entitlement-policy.md). Delivery владеет также source processing outcomes, actual effective history, pending/confirmed visibility evidence, independent exact revision restrictions и history completeness read по [F13](features/F13-content-delivery.md); global mutation guard и primary observation adapter не создают foreign UoW.

Infrastructure:

- VR School activation-key HTTP adapter;
- opaque session-token generation/hash;
- runtime CmDeliverySession scheme, credential security-state/verifier and short encrypted creation replay with shared persistent DataProtection keyring;
- primary/coherent authorization checkpoint + monotonic response lease coordinator/cancellation, no long SQL transaction while streaming; models/layers [F13](features/F13-content-delivery.md), pinned policy [F12](features/F12-content-entitlement-policy.md);
- cross-BC Publishing/ContentModeling/CatalogAccess/MediaLibrary adapters;
- Demo process-local SynchronizationPlan cache adapter.

Integration:

- Publishing transition/withdrawal consumers;
- license/grant revoke consumers;
- policy projection consumers when applicable.

## 9. IdentityAccess bounded context и общая authorization

F16 принадлежит separate IdentityAccess BC по [Q24](../frontend/questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы) и [source](../../archive/identity-access-architecture.md). M06 — workflow/module документации, Publishing moderation/Audit ownership не переносятся в IdentityAccess.

```text
ContentManager.IdentityAccess.Domain
ContentManager.IdentityAccess.Application.Abstractions
ContentManager.IdentityAccess.Application
ContentManager.IdentityAccess.EntityFramework
ContentManager.IdentityAccess.EntityFramework.Persistence
ContentManager.IdentityAccess.Api

ContentManager.Authorization.Abstractions
ContentManager.Authorization.AspNetCore
```

IdentityAccess.Domain минимален для реальных правил; не создаётся parallel Domain.User/mapper/repository поверх provider user ради формальности. Infrastructure ContentManagerUser/Role<Guid>, private provider password/stamps, RolePermission/StaffSession/setup coordination — owner identity schema/IdentityAccessDbContext. EF содержит DbContext/configurations/migrations; EF.Persistence — query/repository/provider enlistment adapters. Physical tree source illustrative, target CQRS placement сохраняется.

Authorization.Abstractions независим от IdentityAccess/ASP.NET: ICurrentUser/immutable PermissionSnapshot/definitions/checker/CQRS requirement metadata. Authorization.AspNetCore реализует policy/handler/endpoint/current-user adaptation; command types не импортируют эту assembly. Owning Application.Abstractions публикует собственные providers/permissions; Host собирает registry и backend pipeline. Fixed two system roles/server grants, direct user grants/custom-role editor отсутствуют MVP.

Business BC используют только общие authorization contracts и external actor Guid без cross-BC FK/DbContext. Resource capability resolver у owner вычисляет AllowedActions, Domain не содержит user permissions. Snapshot once per request from fresh coherent primary state, no interrequest cache. Staff Session/cookie и Delivery runtime scheme/purpose изолированы.

Host регистрирует Core Identity, own provider/session/setup stores/guarded transactions, cookie/auth pipeline+deny-by-default CQRS, options defaults8h/setup20min/password15–128/lockout5failed15min, exact credentialed CORS/antiforgery и persistent protected compatible keyring. Blazor Standalone UI независимо хостится на HTTPS-поддомене того же base domain: cross-origin same-site, no mandatory UI proxy. Browser/client CORS+credentials/header integration не подтверждена одним DI call; final wire/deployment/provider API Q15/Q20.

## 10. Audit technical module

F18 — отдельный append-only read-model contour:

```text
ContentManager.Audit.Application.Abstractions
ContentManager.Audit.Application
ContentManager.Audit.EntityFramework
ContentManager.Audit.EntityFramework.Persistence
ContentManager.Audit.Integration
ContentManager.Audit.Api
```

PostgreSQL schema:

```text
audit
```

Audit не является event store BC и не участвует в их business transaction.

[F18](features/F18-staff-activity-history.md) определяет versioned intent/event/record, actor/initiator/target refs, typed safe metadata/ActionProfileDefinition, immutable read/query/cursor contracts once. Owning Application captures safe intent в своём commit; Audit internal Append handler Application+EF.Persistence принимает trusted producer с unique EventId/own inbox, не foreign UoW. Actor Guid внешнее значение без Identity/target FK; normalized source meaning сохраняется достаточно lossless для duplicate comparison, проекции не объявляют byte identity.
Audit own registry/profile задаёт action/read-class allowlist; source caller не выбирает Public/StaffAccess visibility произвольно. Read/query adapters дают current-user-filtered keyset activity/exact/filter-options, default50/max200, fresh per-request snapshot, dictionary authors только из visible history. Source/model/DTO secrets не проходят generic JSON dump. Host включает producers/dispatcher/consumer/read policies audit.activity.read и audit.staff-access.read, без UI writers/auto purge/notifications/Demo dependency. Concrete cursor/wire/mappings/transport — [Q15](questions/Q15-api-and-source-code.md#f18-принятый-activity-contract)/[Q20](questions/Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read).

## 11. Schema tooling

Schema Registry/code generation не становятся bounded context/source of truth.

```text
ContentManager.SchemaRegistry.Application.Abstractions
ContentManager.SchemaRegistry.Application
ContentManager.SchemaRegistry.Api
ContentManager.Schema.CodeGeneration.Core
ContentManager.Schema.CodeGeneration.Cli       // MVP
```

Registry читает Published schema contract через ContentModeling public port.

`Schema.CodeGeneration.Core` stateless и не зависит от API/EF.

Demo UI ZIP и MVP CLI используют один Core algorithm.

## 12. SDK / consumer artifacts

Отдельно от server solution либо в том же repository tree без server references:

```text
ContentManager.Sdk.Client.DotNet      // netstandard2.1
ContentManager.Sdk.Client.Unity       // Unity 2022.3.62f3 / UPM surface
```

SDK зависит от generated HTTP contracts/client-neutral abstractions, но не от server Domain/EF assemblies.

## 13. Shared

Shared допускается только для технических primitives:

- CQRS abstractions/LeanSoft integration;
- API response/result helpers;
- ETag parsing/formatting;
- clock/id/hash primitives;
- EF conventions;
- MassTransit infrastructure helpers;
- generic serialization/stream helpers.

Shared запрещён как место для:

- EntitySchema/Product/License/Asset business model;
- business constraints;
- cross-BC repository;
- универсального DTO, объединяющего разные bounded contexts.

Перед созданием собственного Shared primitive проверяются LeanSoft libraries и существующие project standards.

## 14. Compile-time dependency rules

### Core

```text
Domain
  -> shared technical primitives only

Application.Abstractions
  -> own Domain (если нужны strongly typed domain contracts)
  -> shared abstractions

Application
  -> own Domain
  -> own Application.Abstractions
```

Core project одного BC **не ссылается на core project другого BC**.

### Adapters

```text
EntityFramework.Persistence
  -> own Domain / Application.Abstractions / EntityFramework

Infrastructure
  -> own Application.Abstractions
  -> public Application.Abstractions других BC при реализации adapter

Integration
  -> own Application.Abstractions
  -> versioned integration contract packages

Api
  -> own Application.Abstractions
  -> shared API/CQRS abstractions
```

### Host

Host — единственный обычный project, которому разрешены broad references на concrete adapters разных BC ради composition/DI.

## 15. Почему cross-BC adapters обязательны

Прямая схема:

```text
Publishing.Application -> CatalogAccess.Application.Abstractions
CatalogAccess.Application -> Publishing.Application.Abstractions
```

создаёт compile-time cycle.

Поэтому consumer-owned ports находятся в own `Application.Abstractions`, а adapter живёт в consumer `Infrastructure`:

```text
Publishing.Application
  -> ICatalogPublishingQueries (Publishing.Application.Abstractions)

Publishing.Infrastructure
  -> Publishing.Application.Abstractions
  -> CatalogAccess.Application.Abstractions
```

И зеркально для CatalogAccess impact checks.

Так synchronous in-process call остаётся дешёвым, но core BC не зависят друг от друга.

## 16. Database ownership

Одна PostgreSQL database, отдельные schemas:

```text
content_modeling
media_library
catalog_access
publishing
delivery
identity
audit
```

Правила:

- отдельный DbContext/migrations на owner;
- migration history отделена по DbContext/schema;
- cross-schema physical FK запрещены;
- cross-BC SQL join из application/repository запрещён;
- один application DB principal допустим MVP;
- production data compatibility текущей pre-launch модели не требуется.

## 17. Transactions

Одна transaction может менять несколько aggregate roots только:

- внутри одного BC;
- когда application use case явно требует атомарности.

Примеры:

- ContentModeling Publish: version root + logical root;
- Publishing activation: state pointer + history + outbox;
- active revision Withdrawal: revision lifecycle + publication state/history + outbox.

Transaction через DbContext разных BC запрещается architecture rule, даже если текущая infrastructure технически это позволяет.

MVP guarded mutation использует собственную Read Committed transaction и общий transaction-level PostgreSQL advisory gate на её physical connection. Acquire первым, затем fresh domain/public dependency reads, deterministic own row locks и own commit/rollback. Domain не зависит от SQL lock types; shared техническая abstraction/owner PostgreSQL adapter и Host registration описаны в [Q20](questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard).

Foreign public read adapter создаёт independent primary read scope/connection, не enlisted caller transaction. Current common connection registry/multi-context participant должен быть разделён по owner UoW; использование public port само по себе не доказывает этого разделения. Background source acceptance/watermark, retries/setup и invalidating mutations участвуют в том же gate. No long I/O/user/async projection wait внутри local transaction; audit/outbox intent local, последующий dispatch/consumer independent.

## 18. Optimistic concurrency

Mutable persistence owner содержит internal `int Version`.

HTTP DTO Version не содержит.

Exact mutable HTTP representation возвращает strong `ETag`; mutation существующего resource требует `If-Match`.

- missing If-Match -> 428;
- stale If-Match -> 412;
- domain conflict -> 409.

Internal event consumer использует Version/CAS там, где конкурентное применение возможно, без HTTP headers.

## 19. Integration contracts

External/durable integration contracts versioned.

Конкретная package/versioning convention пока не определяется в этом документе: пользователь переносит уже используемый стандарт других проектов.

Domain entities и EF models не публикуются как broker contracts.

VR School .NET 7 является out-of-support legacy source; изменения producer/outbox/contracts должны учитывать deployment risk и не ломать существующий Project Storage consumer.

## 20. Architecture tests

Canonicalization — shared pure .NET library boundary по [Q19](questions/Q19-content-hashes.md#принятый-canonical-profile-v1), не business BC и не SDK→server dependency. CanonicalProfileId/CanonicalDigest/CanonicalRepresentation и typed normalized inputs используются owning Application adapters и SDK; библиотека не зависит от Domain/EF/ASP.NET/default serializer contracts. Физическое package/project name и runtime compatibility materialize без нового удалённого serialization service; profile dispatch fixed/unknown rejected. Checks include same literal fixtures on backend/SDK/Unity, no historical byte rehash, no guessed numeric types/runtime Registry fetch.

Обязательные проверки project graph:

- Domain не зависит от Application/Api/EF/Infrastructure;
- Application не зависит от Api/EF/Infrastructure;
- Api не зависит от EntityFramework.Persistence;
- core projects разных BC не ссылаются друг на друга;
- foreign DbContext/repository namespace запрещён;
- cross-BC transaction coordinator запрещён;
- guarded mutation использует owner transaction connection; foreign query DbContext не enlisted, no cached/replica authority;
- все invalidating command/consumer/setup paths участвуют в одном MVP gate до commit, gate берётся до row locks;
- Shared не содержит business entities;
- SDK не ссылается на server Domain/EF.

Конкретная test library выбирается по существующему test stack; правило важнее пакета.

## 21. Связанные спецификации

[Архитектура](architecture.md) · [Backend feature template](../../templates/backend-feature.md) · [S23 csproj](standards/S23-csproj.md) · [S16 REST](standards/S16-rest.md) · [M01 ContentModeling](modules/M01-content-authoring.md) · [M02 MediaLibrary](modules/M02-media-library.md) · [M03 CatalogAccess](modules/M03-catalog.md) · [M04 Publishing](modules/M04-publishing.md) · [M05 Delivery](modules/M05-delivery.md) · [M06 staff workflow](modules/M06-staff-workflow.md) · [M07 tooling](modules/M07-schema-tooling.md)
