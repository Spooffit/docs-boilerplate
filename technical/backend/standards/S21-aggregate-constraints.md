# S21. Ограничения агрегатов: единый источник в Domain

## Область применения

.NET-приложения с агрегатами, доменным слоем, Application и отдельным слоем доступа к данным. Стандарт определяет, где объявлять постоянные ограничения модели и как согласованно применять их в домене, FluentValidation и настройках хранения. Конкретные значения ограничений определяются требованиями к соответствующей модели, а не этим стандартом.

## 1. Обязательная структура

Для каждого AggregateRoot в слое **Domain** создаётся отдельный `public static class <AggregateName>Constraints`, расположенный рядом с агрегатом. Например, `AppealConstraints.cs` для `Appeal`. В новых именах используется `Constraints`, не `Constrains`. Переименование уже существующего публичного типа выполняется отдельно с учётом его потребителей.

Контейнер охватывает ограничения корня и всех принадлежащих ему вложенных сущностей, value objects и элементов коллекций. Вложенные `public static class` повторяют смысловую структуру свойств: `AggregateConstraints.Sender.Name.MiddleName.MaxLength`. Имена соответствуют доменной модели, а не колонкам БД или DTO отдельной операции.

Это **контейнер определений, не Entity, не Value Object и не валидатор**. Он не хранится в БД, не создаётся через DI, не читает конфигурацию и не выполняет запросы. Domain не получает зависимостей на FluentValidation, EF Core или Application ради объявления ограничений. Контрактный пакет с DTO не подключается к Domain только ради копирования атрибутов: ограничения применяются в слоях, которым разрешена доменная зависимость.

Один и тот же предел или шаблон не объявляется независимо в конструкторе, валидаторе и EF-конфигурации. Все потребители используют исходное определение из Domain. Повторное применение проверки на разных границах допустимо и необходимо; расхождение её параметров недопустимо.

## 2. Состав ограничений

| Вид ограничения | Именование | Семантика |
| --- | --- | --- |
| Длина строки | `MinLength`, `MaxLength` | Включительные границы длины присутствующего значения. Единица измерения и нормализация определены явно. |
| Наличие значения или вложенного объекта | `Required` | Допустимо ли отсутствие соответствующего свойства в доменном состоянии. Не универсальная замена всем проверкам заполненности. |
| Формат значения | `Pattern` | Проверенный шаблон; правила применения, регистр и режим сопоставления определяются вместе с ним. |
| Числовой диапазон | `MinValue`, `MaxValue` | Границы с явно указанной включительностью и единицами измерения. |
| Коллекция | `MinCount`, `MaxCount` | Количество элементов. Ограничения элемента располагаются отдельно, например в `Items.Element`. |

Добавляются только действительно определённые ограничения. Отсутствие установленного предела не заменяется произвольным числом, `0` или `int.MaxValue`. Значения длины и количества неотрицательны; при наличии обеих границ минимум не превышает максимум.

Для `int`, `bool`, `string` и других допустимых compile-time значений используются `public const`. Для значений, которые нельзя представить константой, допустимы неизменяемые `static readonly` значения. Публичный изменяемый массив или коллекция не становятся неизменяемыми только от `readonly` ссылки на них.

Сюда не входят условия, зависящие от текущего состояния, прав, внешней БД, даты, тарифа или настроек: уникальность, принадлежность, возможность перехода, ограничения конкретной операции. Они остаются правилами агрегата, политиками и композицией валидаторов. Требование «поле обязательно только при переходе в определённое состояние» нельзя превращать в безусловный `Required = true` для всех состояний.

## 3. Владение и переиспользование

Дерево ограничений заканчивается на границе агрегата. Ссылка на другой AggregateRoot не означает, что его внутренние ограничения нужно скопировать в контейнер первого агрегата.

Если одно и то же доменное значение действительно переиспользуется несколькими агрегатами, его общие ограничения могут определяться один раз рядом с этим типом в Domain. Контейнер каждого агрегата сохраняет понятный путь и ссылается на общее определение, а не копирует литералы. Обязательность **свойства агрегата** при этом может отличаться: допустимость отсутствия ссылки не меняет валидность самого value object.

Одинаковая длина двух разных полей не делает их одним понятием. Не вводится общий `StringMaxLength`, связывающий несвязанные поля только потому, что сегодня их пределы совпали. Также не создаётся зависимость на чужой агрегат исключительно ради получения его константы.

## 4. Обязательность и нормализация

Различаются отсутствие значения, пустая строка, пробельная строка и некорректное непустое значение. `Required = false` разрешает отсутствие, но **не разрешает игнорировать ошибки переданного значения**.

Для каждой строки задаётся политика: проверять как есть либо предварительно нормализовать. Если применяется `Trim`, приведение регистра или преобразование пустого ввода в `null`, все прикладные пути используют одну согласованную нормализацию, а ограничения итогового значения проверяются после неё. Предварительные ограничения размера внешнего запроса могут проверяться раньше. Валидатор не меняет DTO; нормализованная модель подготавливается отдельно либо правило проверяет согласованное представление без мутации. Domain обеспечивает то же правило при прямом вызове фабрики.

Нельзя проверять длину исходной строки в Application, а затем незаметно проверять уже обрезанную строку в Domain. Не допускается и молча обрезать значение до `MaxLength` вместо отказа.

Для строк обычный `string.Length` измеряет число UTF-16 code units, а не количество видимых символов. Если нужны Unicode code points или текстовые элементы, это явно определяется и одинаково реализуется во всех проверках. Физическая длина столбца зависит от провайдера и не считается автоматически идентичной прикладной единице длины. См. [семантику String.Length](https://learn.microsoft.com/en-us/dotnet/api/system.string.length).

Обязательность вложенного объекта задаётся на его собственном узле дерева. Правила его полей выполняются, когда объект присутствует. Отсутствие необязательного родителя не должно вызывать ошибку обязательного поля несуществующего дочернего объекта.

Обязательность доменного состояния не равна обязательности поля каждого request: например, отсутствие свойства в операции частичного изменения может означать «не менять». Это описывается контрактом операции, а не изменением общих ограничений модели.

## 5. Сквозной пример структуры

`Appeal` и все числа ниже являются учебным примером, а не набором обязательных сущностей или лимитов приложения. Пример сокращён до нескольких полей: в реальной модели контейнер охватывает все её определённые ограничения. Поле `ReferenceCode` служит для демонстрации regex, без правил проверки реальных телефонов или регистрационных номеров.

```csharp
public static class AppealConstraints
{
    public static class Text
    {
        public const int MinLength = 3;
        public const int MaxLength = 255;
        public const bool Required = true;
    }

    public static class Sender
    {
        public const bool Required = true;

        public static class Name
        {
            public const bool Required = true;

            public static class FirstName
            {
                public const int MinLength = 3;
                public const int MaxLength = 128;
                public const bool Required = true;
            }

            public static class MiddleName
            {
                public const int MinLength = 1;
                public const int MaxLength = 128;
                public const bool Required = false;
            }

            public static class LastName
            {
                public const int MinLength = 3;
                public const int MaxLength = 128;
                public const bool Required = true;
            }
        }

        public static class Requisites
        {
            public const bool Required = true;

            public static class ReferenceCode
            {
                public const int MinLength = 3;
                public const int MaxLength = 32;
                public const bool Required = true;
                public const string Pattern = @"\A[A-Z0-9]+\z";
            }
        }
    }
}
```

Условная политика примера: строки проверяются как переданы, автоматические `Trim` и изменение регистра не выполняются. Отсутствующее отчество представляется только `null`; переданные `""` и пробельная строка отклоняются. Это иллюстрация выбранной семантики, не требование применять её к любой модели.

## 6. Использование в Application и FluentValidation

По [S18](S18-fluent-validation.md) смысловые проверки выделяются в переиспользуемые компоненты. Их параметры берутся из `Constraints`; для создания и изменения не заводятся независимые копии чисел и regex.

Ниже показана атомарная проверка присутствующего отчества и её подключение во вложенном валидаторе. Остальные поля проверяются аналогично по собственным определениям. Фрагмент не является полным валидатором всего агрегата.

```csharp
using FluentValidation;

public sealed record AppealSenderNameDto(
    string? FirstName,
    string? MiddleName,
    string? LastName);

public static class AppealMiddleNameRules
{
    public static IRuleBuilderOptions<T, string?> ValidAppealMiddleName<T>(
        this IRuleBuilder<T, string?> rule)
    {
        return rule
            .NotEmpty()
            .Length(
                AppealConstraints.Sender.Name.MiddleName.MinLength,
                AppealConstraints.Sender.Name.MiddleName.MaxLength);
    }
}

public sealed class AppealSenderNameValidator
    : AbstractValidator<AppealSenderNameDto>
{
    public AppealSenderNameValidator()
    {
        RuleFor(x => x.MiddleName)
            .Cascade(CascadeMode.Stop)
            .ValidAppealMiddleName()
            .When(x =>
                AppealConstraints.Sender.Name.MiddleName.Required ||
                x.MiddleName is not null);
    }
}
```

Здесь `null` пропускается только потому, что значение необязательно. Пустое и пробельное **переданное** значение не пропускает проверку. Условие `.When(x => !string.IsNullOrWhiteSpace(x.MiddleName))` имело бы другую семантику: оно исключило бы такие значения из проверки. Его нельзя применять вместо явно определённой нормализации в `null`.

Одна `.Length(...)` не запрещает `null`; проверка обязательного значения задаётся отдельно. Родитель подключает `AppealSenderNameValidator` через `SetValidator` и отдельно проверяет наличие `Name` по `AppealConstraints.Sender.Name.Required`. При `null` дочерний `SetValidator` не выполняется. См. [встроенные проверки](https://docs.fluentvalidation.net/en/latest/built-in-validators.html), [условия](https://docs.fluentvalidation.net/en/latest/conditions.html) и [вложенные валидаторы](https://docs.fluentvalidation.net/en/latest/start.html#complex-properties).

## 7. Использование в Domain

Агрегат и его value objects защищают инварианты и при обходе Application. Наличие FluentValidation не разрешает создавать недопустимое состояние через публичную фабрику. Guards и чистые предикаты используют те же определения, без зависимости Domain на валидаторы DTO.

Ниже приведена самостоятельная фабрика value object для кода из примера. `record` обеспечивает семантику значения; существующая базовая реализация ValueObject может использоваться вместо него. Создание `null` вместо необязательного value object решает владеющий агрегат, а не фабрика корректного присутствующего значения.

```csharp
using System;
using System.Text.RegularExpressions;
using CodeConstraints = AppealConstraints.Sender.Requisites.ReferenceCode;

public sealed record AppealReferenceCode
{
    private static readonly Regex Format = new(
        CodeConstraints.Pattern,
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
        TimeSpan.FromMilliseconds(100));

    public string Value { get; }

    private AppealReferenceCode(string value)
    {
        Value = value;
    }

    public static AppealReferenceCode Create(string? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Reference code must not be empty.", nameof(value));
        }

        if (value.Length < CodeConstraints.MinLength ||
            value.Length > CodeConstraints.MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), "Reference code length is outside its limits.");
        }

        if (!Format.IsMatch(value))
        {
            throw new ArgumentException(
                "Reference code has an invalid format.", nameof(value));
        }

        return new AppealReferenceCode(value);
    }

    public override string ToString() => Value;
}
```

Фабрика не превращает `" abc "` в `"ABC"`: в примере такая нормализация не разрешена. Проверка наличия самого свойства `ReferenceCode` в агрегате использует `CodeConstraints.Required`; валидность непустого экземпляра VO от этого флага не зависит.

Код regex использует API .NET 7+; это область применимости примера, не требование выбрать конкретную версию runtime. Число `100` задаёт только пример технического timeout, не ограничение доменной модели и не универсальный норматив. Стиль доменных отказов следует используемому механизму ошибок; исключения примера сами по себе не являются HTTP-контрактом.

### Правила regex

Шаблон хранится один раз; алгоритм его исполнения находится в чистой проверке или валидаторе. При проверке целого значения используются границы целого ввода, например .NET `\A` и `\z`. В примере длина проверяется отдельно, поэтому её числа не продублированы в квантификаторе regex.

Проверка ограничивает размер входа и время выполнения regex. При подходящем шаблоне допустим `NonBacktracking`; его совместимость с используемыми конструкциями проверяется. Timeout не считается успешной валидацией. Произвольный пользовательский текст нельзя использовать как доверенный шаблон.

Регистрозависимость, используемый алфавит и опции должны совпадать у потребителей. Например, `[0-9]` и `\d` не объявляются взаимозаменяемыми без проверки требуемого алфавита. .NET pattern не переносится автоматически в SQL `CHECK`: язык и семантика regex зависят от СУБД. Источники: [границы совпадения](https://learn.microsoft.com/en-us/dotnet/standard/base-types/anchors-in-regular-expressions), [классы символов](https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions), [практики безопасного выполнения](https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-regex).

## 8. Использование в слое доступа к данным

В текущей EF-конфигурации `HasMaxLength` и применимая обязательность получают значения из Domain, а не из локальных literals. Ограничения самого объекта и каждого его поля задаются отдельно.

Ниже фрагмент настройки `Sender.Name` при выбранном отображении через owned types. `builder` имеет тип `EntityTypeBuilder<Appeal>`, а `Name` содержит строковые свойства; настройка остальных свойств опущена. Стандарт не требует заменять другое согласованное отображение value objects на `OwnsOne`.

```csharp
builder.OwnsOne(x => x.Sender, sender =>
{
    sender.OwnsOne(x => x.Name, name =>
    {
        name.Property(x => x.FirstName)
            .HasMaxLength(AppealConstraints.Sender.Name.FirstName.MaxLength)
            .IsRequired(AppealConstraints.Sender.Name.FirstName.Required);

        name.Property(x => x.MiddleName)
            .HasMaxLength(AppealConstraints.Sender.Name.MiddleName.MaxLength)
            .IsRequired(AppealConstraints.Sender.Name.MiddleName.Required);

        name.Property(x => x.LastName)
            .HasMaxLength(AppealConstraints.Sender.Name.LastName.MaxLength)
            .IsRequired(AppealConstraints.Sender.Name.LastName.Required);
    });

    sender.Navigation(x => x.Name)
        .IsRequired(AppealConstraints.Sender.Name.Required);
});

builder.Navigation(x => x.Sender)
    .IsRequired(AppealConstraints.Sender.Required);
```

`HasMaxLength` задаёт metadata для провайдера; EF Core не выполняет эту проверку перед передачей данных провайдеру. `IsRequired` для строкового столбца описывает запрет `NULL`, а не запрет `""` или пробелов. Ни один из этих вызовов не добавляет проверку `MinLength` или `Pattern`. Источники: [свойства EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties), [owned types и обязательность навигаций](https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities).

Не каждое прикладное ограничение обязано иметь идентичный SQL `CHECK`. Необходимые ограничения БД проектируются явно и проверяются на реальном провайдере. При необязательном родителе, table splitting, отдельной таблице или converter учитывается реальная форма хранения: обязательное поле присутствующего дочернего объекта не должно случайно запретить отсутствие самого необязательного объекта.

## 9. Изменение ограничений и миграции

Изменение `Constraints` требует совместной проверки Domain, валидаторов, EF-модели, существующих данных и потребителей. Уменьшение длины, новый regex или запрет `null` могут сделать сохранённые данные недопустимыми. Автоматическое усечение, очистка или перезапись истории не являются допустимой заменой решения о миграции.

Правка константы не меняет уже существующий столбец. При изменении отображения необходима соответствующая миграция и согласованный порядок развёртывания.

**Текущая EF-конфигурация использует Constraints; исторические migrations и сгенерированный ModelSnapshot сохраняют значения своей редакции.** В них нельзя вручную заменять зафиксированные literals ссылками на изменяемые актуальные константы. Иначе повторный запуск старой миграции начнёт означать другую схему, а изменение текущей модели может перестать обнаруживаться сравнением с snapshot. Назначение файлов миграции и snapshot: [документация EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing).

Публичные `const` встраиваются в использующие их сборки при компиляции. После изменения значения требуется пересобрать и согласованно обновить зависимые сборки; замена только Domain DLL недостаточна. Это важно также для независимо версионируемых пакетов. См. [C# const](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/const).

## 10. Проверка соблюдения

| Проверка | Что подтверждает |
| --- | --- |
| Структурная проверка агрегатов | У каждого AggregateRoot есть свой контейнер; дочерние ограничения принадлежат правильному владельцу, Domain не зависит от FluentValidation/EF. |
| Unit-проверки полей | Минимум, максимум и соседние значения; `null`, пустая и пробельная строки; допустимый и неверный формат. |
| Unit-проверки композиции | Присутствие необязательного поля не отключает проверку его значения; обязательный объект проверяется отдельно от `SetValidator`; ошибки сохраняют путь. |
| Согласованность Domain и Application | Одинаковое значение после согласованной нормализации одинаково оценивается по общим ограничениям. Дополнительные правила операции различаются явно. |
| EF metadata и Integration | Текущее отображение соответствует определениям; реальные ограничения и чтение owned/optional значений проверены на используемом провайдере. |
| Изменение ограничений | Проверены старые данные, обновление схемы и воспроизводимость прежних миграций. |

Проверять нужно не только факт ссылки на константу, но и смысл правила. Тесты, рассчитывающие все ожидания из тех же случайно неверных значений, не заменяют проверку согласованных требований.

Не требуется писать генератор валидаторов, reflection-based mapping или универсальный движок ограничений. Явного дерева постоянных определений, небольших проверок и обычной EF-конфигурации достаточно.

[Валидация моделей](S18-fluent-validation.md) · [Валидация и ошибки](../../../catalog/base/docs/standards/data-flows.md#input-trust-and-failure-semantics) · [Хранение](../../../catalog/docs/standards/dotnet/ef-core.md#domain-constraints-and-historical-persistence-metadata) · [Unit tests](../../../catalog/docs/standards/dotnet/testing.md#unit-tests) · [Integration tests](../../../catalog/docs/standards/dotnet/testing.md#integration-tests).
