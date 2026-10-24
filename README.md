## Лабораторна 01: проєктування баз даних для мікросервісної архітектури (EstateHub)

### 1. Предметна область

Платформа для пошуку та реалізації нерухомості. Автоматизує процеси оренди та продажу житла і взаємодію трьох типів користувачів: **власників** (розміщують оголошення), **клієнтів** (шукають об'єкти, залишають заявки) і **рієлторів** (супроводжують угоду).

Систему розділено на три ізольовані bounded contexts, кожен — своя база даних, без FOREIGN KEY між базами.

| # | Контекст | База даних | Основні сутності/колекції |
|---|---|---|---|
| 1 | Каталог нерухомості (Property Catalog) | SQL Server + EF Core (Проєкт №2) | Property, Category, Amenity, PropertyImage, PropertyLocation |
| 2 | Заявки та угоди (Applications & Transactions) | SQL Server + ADO.NET & Dapper (Проєкт №1) | Application, Viewing, Contract, Realtor |
| 3 | Комунікація та активність (Activity & Communication) | MongoDB (Проєкт №3) | Favorites, Reviews, Inquiries |

ERD-діаграми: [`docs/erd-project1.png`](docs/erd-project1.png) (Проєкт №1), [`docs/erd-project2.png`](docs/erd-project2.png) (Проєкт №2).

### 2. Політика дублювання даних та узгодженості (Eventual Consistency)

Між базами немає JOIN-ів і FOREIGN KEY, тому частина полів свідомо дублюється.

| Поле | Де зберігається | Джерело (власник) | Політика узгодженості |
|---|---|---|---|
| `Applications.PropertyId`, `PriceSnapshot`, `PropertyTitleSnapshot` | БД Угод (Проєкт №1) | `Property` (БД Каталогу) | Знімок на момент подання заявки — **не оновлюється** (аналог `OrderItem.ProductName/UnitPrice` в eShop) |
| `Favorites/Reviews/Inquiries.propertyId`, `propertyTitle`, `city` | БД Активності (MongoDB, Проєкт №3) | `Property` (БД Каталогу) | Копія для читання — оновлюється подією `PropertyUpdatedEvent` |
| `Properties.Status = 'sold'` | БД Каталогу (Проєкт №2) | Угода (БД Угод) | Оновлюється подією `PropertySoldEvent` з Проєкту №1 |

Жодне з цих полів не має FOREIGN KEY на іншу базу — цілісність посилання перевіряє прикладний код.

### 3. Розгортання з нуля

#### Проєкт №1 — SQL Server + Dapper (`db/p1/`)

```powershell
# 1. Створити базу
sqlcmd -S localhost -Q "CREATE DATABASE EstateHub_Transactions"

# 2. Схема (таблиці, ключі, обмеження, індекси)
sqlcmd -S localhost -d EstateHub_Transactions -i db/p1/schema.sql

# 3. Збережувані процедури
sqlcmd -S localhost -d EstateHub_Transactions -i db/p1/procedures.sql

# 4. Тестові дані (ідемпотентно — повторний запуск не дублює)
sqlcmd -S localhost -d EstateHub_Transactions -i db/p1/seed.sql
```

#### Проєкт №2 — SQL Server + EF Core (`db/p2/`)

```powershell
# Варіант 1: готовий SQL
sqlcmd -S localhost -Q "CREATE DATABASE EstateHub_Catalog"
sqlcmd -S localhost -d EstateHub_Catalog -i db/p2/schema.sql
sqlcmd -S localhost -d EstateHub_Catalog -i db/p2/seed.sql

# Варіант 2: міграціями EF Core (сервіс Catalog з Лаб. 03, див. розділ «Лаб. 03» нижче)
dotnet ef database update --project Services/Catalog/Catalog.Dal --startup-project Services/Catalog/Catalog.Api
```

Історичні міграції Лаб. 01 (проєкт `Lab1_Project2`, у порядку застосування):
1. `InitialCreate_AddPropertyLocationTable` — базова схема + таблиця `PropertyLocations`, довідникові seed-дані (Категорії, Зручності)
2. `AddAddedAtToPropertyAmenities` — власне поле `AddedAt` у проміжній таблиці `PropertyAmenities`

#### Проєкт №3 — MongoDB (`db/p3/`)

```powershell
# 1. Запустити mongod (якщо ще не запущено)
mongod --dbpath D:\Databases\data

# 2. В окремому терміналі — створити колекції з валідацією схеми
mongosh < db/p3/collections.js

# 3. Наповнити тестовими даними (ідемпотентно)
mongosh < db/p3/seed.js
```

### 4. Перевірка після розгортання

```sql
-- Немає FK за межі своєї бази (виконати в кожній SQL-базі)
SELECT * FROM sys.foreign_keys;

-- Перевірка CHECK-обмеження (має впасти з помилкою)
INSERT INTO Properties (..., Price, ...) VALUES (..., -100, ...);

-- Перевірка UNIQUE на Contracts.ApplicationId (друга спроба має впасти)
INSERT INTO Contracts (ApplicationId, ...) VALUES (@existingAppId, ...);
```

```javascript
// MongoDB: документи різної структури в одній колекції
db.Inquiries.find().pretty()   // type: question / viewing_request / offer — різні набори полів

// Перевірка індексів
db.Inquiries.getIndexes()
```

Повторний запуск будь-якого `seed.sql` / `seed.js` **не створює дублікатів** (перевірка `IF NOT EXISTS` / `find-or-create` перед вставкою).

### 5. Структура репозиторію

```
.
├── README.md
├── docs/
│   ├── erd-project1.png
│   ├── erd-project2.png
│   └── 01-swagger.png … (знімки екрана Лаб. 03)
├── db/
│   ├── p1/   schema.sql · procedures.sql · seed.sql
│   ├── p2/   schema.sql · seed.sql · migration.sql (Лаб. 03)
│   └── p3/   collections.js · seed.js
├── Platform.slnx · Platform.AppHost/
└── Services/   Deals (Лаб. 02) · Catalog (Лаб. 03)
```


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

## Лаб. 03 — Catalog (ASP.NET, тришарова архітектура, EF Core Code-First)

Мікросервіс **Catalog** (Проєкт №2, БД `EstateHub_Catalog_EF`): `Services/Catalog/` — `Catalog.Domain`, `Catalog.Dal`, `Catalog.Bll`, `Catalog.Api`, `Catalog.Tests`.

### Запуск
1. Встановіть SQL Server (LocalDB / Express) і .NET 10 SDK.
2. У `Services/Catalog/Catalog.Api/appsettings.json` змініть `ConnectionStrings:CatalogDb` на свій сервер
   (за замовчуванням LocalDB з `AttachDbFileName=C:\Users\Public\EstateHub_Catalog_EF.mdf`).
3. `dotnet run --project Services/Catalog/Catalog.Api`, Swagger: http://localhost:5200/swagger
   (у Development міграції застосовуються і сідінг виконується автоматично; у Production вимкнено).

### Міграції
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project Services/Catalog/Catalog.Dal --startup-project Services/Catalog/Catalog.Api
dotnet ef database update --project Services/Catalog/Catalog.Dal --startup-project Services/Catalog/Catalog.Api
dotnet ef migrations script --project Services/Catalog/Catalog.Dal --startup-project Services/Catalog/Catalog.Api -o db/p2/migration.sql
```
Міграції: `Services/Catalog/Catalog.Dal/Migrations/`. Готовий SQL-скрипт: `db/p2/migration.sql`.

### Ключові рішення
- Зв'язки: 1:1 Property–PropertyLocation, 1:N Category→Property та Property→PropertyImage, M:N Property–Amenity через явну сутність `PropertyAmenity` з полем `AddedAt`.
- Репозиторії: Eager (`GetWithDetailsAsync`, 1 SQL), Explicit (`GetWithPropertiesAsync`, 2 SQL), LINQ через M:N (`GetRecentlyAddedAsync`); Unit of Work з єдиним `SaveChangesAsync`.
- Пагінація, фільтрація й сортування виконуються в SQL (Specification pattern, Ardalis); `pageSize` обмежено 1..100.
- Валідація: DataAnnotations + FluentValidation (явний виклик), відповідь `ValidationProblemDetails`.
- Помилки: глобальний `IExceptionHandler` + ProblemDetails (404, 400, 409, 500).
- Логи: Serilog зі структурованими властивостями `CorrelationId`, `UserId`, `RequestPath`; SQL-запити EF логуються.

### Основні ендпойнти
| Метод | URL | Коди |
|---|---|---|
| GET | `/api/Properties?page=1&pageSize=5&city=Київ&sortBy=price&sortDir=asc` | 200, 400 |
| GET | `/api/Properties/{id}` (eager) | 200, 404 |
| POST | `/api/Properties` | 201 + Location, 400, 404, 409 |
| PUT | `/api/Properties/{id}`, `/price`, `/status` | 204, 400, 404, 409 |
| DELETE | `/api/Properties/{id}` (м'яке видалення) | 204, 404 |
| GET | `/api/Categories/{id}/properties` (explicit) | 200, 404 |
| GET | `/api/Properties/by-amenity?amenity=Паркінг&since=2020-01-01` | 200, 400 |

Знімки екрана: `docs/` (Swagger, пагінація, 400, 409, 404, логи SQL для eager і explicit).
