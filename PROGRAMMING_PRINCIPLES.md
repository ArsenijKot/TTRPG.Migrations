# Демонстрація дотримання принципів програмування 

## SRP --- Single Responsibility Principle

**Ідея:** кожен клас має одну відповідальність.

**Фрагмент коду:**

``` csharp
public class MemberRepository : IMemberRepository
{
    public async Task<Member?> GetByIdAsync(int id, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
    }
}
```
**Демонстрація в коді**
- `MemberRepository` — відповідає тільки за доступ до даних членів (CRUD + специфічні запити).  
- `MigrationRunner` — окремий клас, що відповідає лише за застосування SQL-міграцій.   
- `AsyncOperationsDemo` — окремий клас для демонстрацій асинхронних операцій / відміни.

**Посилання на код: https://github.com/ArsenijKot/TTRPG.Migrations/blob/5ebca11fb70c4e4eb792150254c1dabbf2761101/TTRPG.Migrations/MemberRepository.cs#L83** .

------------------------------------------------------------------------

## OCP --- Open/Closed Principle

**Ідея**: модулі відкриті для розширення, закриті для модифікації.

**Демонстрація**
- `MigrationRunner` читає усі файли `*.sql` з директорії й застосовує їх у порядку — щоб додати нову міграцію, не змінюють код, а просто додають файл.

**Фрагмент коду:**

``` csharp
var files = Directory.GetFiles(migrationsPath, "*.sql")
                     .OrderBy(f => f);
```

**Посилання на код: https://github.com/ArsenijKot/TTRPG.Migrations/blob/5ebca11fb70c4e4eb792150254c1dabbf2761101/TTRPG.Migrations/MigrationRunner.cs#L22**
------------------------------------------------------------------------

## LSP --- Liskov Substitution Principle

**Ідея**: підкласи/реалізації можна підмінити без порушення поведінки.

**Демонстрація (архітектурна, через інтерфейси)**
- Репозиторій реалізує інтерфейс `IMemberRepository` (використовується тип-інтерфейс для абстракції), тому реалізацію можна замінити без зміни клієнтського коду.

**Фрагмент коду:**

``` csharp
public class MemberRepository : IMemberRepository
```
**Посилання на код: https://github.com/ArsenijKot/TTRPG.Migrations/blob/5ebca11fb70c4e4eb792150254c1dabbf2761101/TTRPG.Migrations/MemberRepository.cs#L8**
------------------------------------------------------------------------

## ISP --- Interface Segregation Principle

**Ідея**: замість одного великого інтерфейсу — кілька маленьких специфічних.

**Демонстрація**
- Проєкт використовує маленький інтерфейс `IMemberRepository`, це дозволяє споживачам залежати тільки від потрібних методів.
- 
**Фрагмент коду:**

``` csharp
public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(int id, CancellationToken ct);
}
```
**Посилання на код: https://github.com/ArsenijKot/TTRPG.Migrations/blob/5ebca11fb70c4e4eb792150254c1dabbf2761101/TTRPG.Migrations/Interface1.cs#L1**
------------------------------------------------------------------------

## DIP --- Dependency Inversion Principle

**Ідея**: залежності повинні бути від абстракцій, не від конкретних реалізацій.

**Демонстрація**
- `Program.Main` отримує `connectionString` з `ConfigurationBuilder`, а класи (`MemberRepository`, `MigrationRunner`, `ConnectionPoolingTester`, `AsyncOperationsDemo`)
- приймають рядок підключення через конструктор — це приклад інверсії залежностей на рівні конфігурації/ініціалізації (вживаний простий варіант DI).

**Фрагмент коду:**

``` csharp
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
```
**Посилання на код: https://github.com/ArsenijKot/TTRPG.Migrations/blob/5ebca11fb70c4e4eb792150254c1dabbf2761101/TTRPG.Migrations/Program.cs#L8**
------------------------------------------------------------------------

## DRY

**Ідея**: уникати дублювання логіки та даних.

**Демонстрація**
- В `MemberRepository` повторювані операції читання рядків із `reader` централізовано оформлені однаковим патерном
- (`reader.GetInt32(0)`, `reader.GetString(1)`), що робить код однорідним та легким для підтримки.

**Фрагмент коду:**

``` csharp
reader.GetInt32(0);
reader.GetString(1);
```
**Посилання на код: https://github.com/ArsenijKot/TTRPG.Migrations/blob/5ebca11fb70c4e4eb792150254c1dabbf2761101/TTRPG.Migrations/MemberRepository.cs#L72**
------------------------------------------------------------------------

## KISS

**Ідея**: прості рішення краще складних.

**Демонстрація**
- `ConnectionPoolingTester.RunTestAsync` робить проста та прозора робота: відкриває з'єднання, виконує `SELECT 1` в циклі, заміряє час — без надміру складних абстракцій.

**Фрагмент коду:**

``` csharp
await connection.OpenAsync();
await command.ExecuteScalarAsync();
```
**Посилання на код: https://github.com/ArsenijKot/TTRPG.Migrations/blob/5ebca11fb70c4e4eb792150254c1dabbf2761101/TTRPG.Migrations/ConnectionPoolingTester.cs#L78**
------------------------------------------------------------------------

## YAGNI

**Ідея**: не додавайте функціональність до поки вона не знадобиться.

**Демонстрація**
- Базова реалізація `MigrationRunner` виконує саме ту мінімальну логіку, яка потрібна (перевірка хешів і виконання батчів) —
- без складних механізмів планування або доп. метаданих. Це приклад прагнення не ускладнювати систему без потреби.
**Фрагмент коду:**

``` csharp
// only minimal migration logic implemented
```
**Посилання на код: https://github.com/ArsenijKot/TTRPG.Migrations/blob/5ebca11fb70c4e4eb792150254c1dabbf2761101/TTRPG.Migrations/MigrationRunner.cs#L5**
------------------------------------------------------------------------

## Async & Cancellation

**Ідея**: використовувати `async/await` та `CancellationToken` для відміни довгих операцій.

**Демонстрація**
- `AsyncOperationsDemo` і методи `RunWithTimeoutAsync`, `RunWithManualCancellationAsync`, `RunParallelAsync` демонструють:
- створення `CancellationTokenSource`, передавання токена у асинхронні методи, перехоплення `OperationCanceledException`. 
- Методи репозиторію приймають `CancellationToken` і передають його в `OpenAsync`, `ExecuteReaderAsync` тощо.

**Фрагмент коду:**

``` csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
await ExecuteLongQueryAsync(cts.Token);
```
**Посилання на код: https://github.com/ArsenijKot/TTRPG.Migrations/blob/5ebca11fb70c4e4eb792150254c1dabbf2761101/TTRPG.Migrations/AsyncOperationsDemo.cs#L19**
------------------------------------------------------------------------

## Підсумок

Файл містить стислий опис принципів SOLID, DRY, KISS, YAGNI, а також
приклади асинхронності.
