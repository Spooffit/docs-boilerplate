# M06. Работа сотрудников

## Назначение и этап

Совместная работа внутренних сотрудников: отдельный вход CM, общий контент, проверка админом с одобрением/возвратом, история действий. Это модуль MVP. **В Demo не входит:** демонстрация выполняется без входа, авторизации, ролей и согласования.

На фронтенде оболочка Blazor Standalone координирует confirmed current staff и доступную навигацию. UI/API независимо размещаются на same-site HTTPS subdomains; staff cookie/credentialed CORS/CSRF реализуются общим transport по F16/Q15. Fixed Specialist/Administrator roles независимы и совмещаются; admin назначает роли, server permission catalog/role mapping остаётся read-only. Global permissions и resource AllowedActions дают advisory UX, fresh server checks/domain rules остаются authority.

F16 обязательно восстанавливает actual input/File objects/exact context/original ETag тому же user в памяти открытой SPA-вкладки: внутренний SignIn без reload, fresh exact GET и отдельный explicit Save. Unknown mutation не autoreplay, fresh ETag не blind overwrite baseline. Explicit Logout/другой user очищают snapshot; reload/close/restart исключены, browser persistence/autosave не добавляются. StaffSession absolute8h/no sliding, current Logout не закрывает остальные sessions; reset/deactivate старые sessions не восстановятся при Reactivate. Local tab notices/login-context precondition предотвращают A-input под B-cookie без замены server authorization.

История переиспользуется в выбранном месте показа без дублирования моделей; её навигация остаётся в F18. Admin-issued setup/reset с самостоятельным выбором permanent password и last-ready-admin refusal относятся к F16; bootstrap/recovery выполняет оператор без frontend wizard. [Q24](../questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы) фиксирует protocol, [Q15](../../backend/questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) — оставшиеся exact carriers/routes/representation details.

F18 — самостоятельный read-only /admin/activity: common audit.activity.read у Specialist+Administrator, staff-access records audit.staff-access.read только Administrator. Record visibility применяется сервером до paging/counters/filter options; suggestions берутся из доступной audit history, не admin Identity directory. Selected UTC filters/keyset OccurredAtDESC+IdDESC,50 default/max200 и manual Refresh не дают realtime/whole-feed snapshot/global causal order. Exact list/detail/filter-options contracts и все модели остаются backend F18.

## Карта фич

| ID | Результат |
| --- | --- |
| F16 | [Вход, управление доступом и восстановление незавершённой работы](../features/F16-staff-access.md) |
| F17 | [Проверка контента админом и возврат с замечанием](../features/F17-content-review.md) |
| F18 | [Видимая история действий](../features/F18-staff-activity-history.md) |

## Связи и ограничения

Проверка F17 относится к exact ProductVariantPublicationRevision из F10. При Manual одобрение само не активирует контент; Automatic продолжает связанную публикацию через F11. Владелец страниц и алгоритмы раскрываются один раз в соответствующей F-спецификации.

Durable actual owner outcomes появляются в audit асинхронно: source success не ждёт append, UI не создаёт local final row, не повторяет source mutation и не считает empty feed доказательством отсутствия действий/drained backlog. Whole Save/atomic promotion prepare дают одну запись с exact refs; human Approval/Activation и F15 repair/Resolve — отдельные commits/outcomes. Committed stage не workflow-all-succeeded; as-of actor/target captions/IDs и typed safe details сохраняют смысл без current joins/full content/secrets. Audit read не даёт target/reason permission; missing target не удаляет record. MVP retain all/no purge/userdelete; future archive/export/anonymization отдельно, no legal-forever claim. [Q15 F18](../../backend/questions/Q15-api-and-source-code.md#f18-принятый-activity-contract) · [Q20 F18](../../backend/questions/Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read).

[Общая карта модулей](../../../modules.md) · [Фичи](../../../features.md) · [Вопросы](../../../questions.md) · [Этапы](../../../milestones/README.md).
