## Лабораторна 02: сервіс заявок та угод (ADO.NET + Dapper)

Сервіс `Services/Deals` працює з БД `EstateHub_Transactions` (Проєкт №1 з Лаб. 01) і складається з шарів
`Deals.Api` → `Deals.Bll` → `Deals.Dal` (+ `Deals.Domain`).

| Шар | Що містить |
|---|---|
| Deals.Domain | моделі (Application, Viewing, Contract, Realtor, ApplicationRealtor), доменні винятки, інтерфейси репозиторіїв, `IGenericRepository<T>`, `IUnitOfWork` |
| Deals.Dal | `ApplicationRepository` (чистий ADO.NET), `ViewingRepository`/`ContractRepository`/`RealtorRepository` (Dapper, база `BaseDapperRepository<T>`), `UnitOfWork` з одним з'єднанням і транзакцією |
| Deals.Bll | сервіси, DTO (DataAnnotations), профілі AutoMapper, `Caching/CachedRealtorService` |
| Deals.Api | тонкі async-контролери, Swagger, `GlobalExceptionHandler` (ProblemDetails) |

### Запуск через Aspire
1. Встановіть .NET SDK та Docker Desktop.
2. `dotnet run --project Platform.AppHost`
3. У Aspire Dashboard (посилання в консолі) відкрийте ресурс `deals-api` → `/swagger`.
4. Виконайте `db/p1/schema.sql` (сам створює БД `EstateHub_Transactions`), потім `procedures.sql` і `seed.sql`
   (для контейнера: порт SQL Server видно в Dashboard, логін `sa`, пароль - параметр `sql-password` ресурсу).

### Запуск без Docker (LocalDB)
1. Виконайте `db/p1/schema.sql`, `procedures.sql`, `seed.sql` у `(localdb)\MSSQLLocalDB`.
2. `dotnet run --project Services/Deals/Deals.Api` → `https://localhost:<port>/swagger`.

### Приклади запитів
```http
POST /api/realtors
{"fullName":"Іван Петренко","email":"ivan@estatehub.com","phone":"+380501234567"}

POST /api/applications
{"clientId":"<guid>","propertyId":"<guid>","priceSnapshot":125000,"propertyTitleSnapshot":"2-кімн. квартира, центр"}

POST /api/applications/{id}/realtors          (M:N)
{"realtorId":"<guid>","role":"listing_agent"}

POST /api/applications/{id}/approve           (транзакція: статус + огляд)
{"rowVersion":"<base64 з відповіді>","realtorId":"<guid>","viewingDate":"2027-01-15T10:00:00Z"}

POST /api/applications/{id}/contract          (транзакція: договір + completed)
{"rowVersion":"<base64>","finalPrice":120000}

PATCH /api/applications/{id}/status           (completed -> new = 409 ProblemDetails)
{"status":"new","rowVersion":"<base64>"}

GET /api/applications?page=1&pageSize=10&status=new
```
Помилки повертаються у форматі ProblemDetails: 400 (валідація), 404 (не знайдено), 409 (конфлікт: недопустимий
перехід статусу, застарілий RowVersion, дублікат).
