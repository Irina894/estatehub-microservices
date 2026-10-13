# EstateHub — Лабораторна робота №1: Проєктування баз даних для мікросервісної архітектури

## 1. Предметна область

Платформа для пошуку та реалізації нерухомості. Автоматизує процеси оренди та продажу житла і взаємодію трьох типів користувачів: **власників** (розміщують оголошення), **клієнтів** (шукають об'єкти, залишають заявки) і **рієлторів** (супроводжують угоду).

Систему розділено на три ізольовані bounded contexts, кожен — своя база даних, без FOREIGN KEY між базами.

| # | Контекст | База даних | Основні сутності/колекції |
|---|---|---|---|
| 1 | Каталог нерухомості (Property Catalog) | SQL Server + EF Core (Проєкт №2) | Property, Category, Amenity, PropertyImage, PropertyLocation |
| 2 | Заявки та угоди (Applications & Transactions) | SQL Server + ADO.NET & Dapper (Проєкт №1) | Application, Viewing, Contract, Realtor |
| 3 | Комунікація та активність (Activity & Communication) | MongoDB (Проєкт №3) | Favorites, Reviews, Inquiries |

ERD-діаграми: [`docs/erd-project1.md`](docs/erd-project1.md) (Проєкт №1), [`docs/erd-project2.md`](docs/erd-project2.md) (Проєкт №2).

## 2. Політика дублювання даних та узгодженості (Eventual Consistency)

Між базами немає JOIN-ів і FOREIGN KEY, тому частина полів свідомо дублюється.

| Поле | Де зберігається | Джерело (власник) | Політика узгодженості |
|---|---|---|---|
| `Applications.PropertyId`, `PriceSnapshot`, `PropertyTitleSnapshot` | БД Угод (Проєкт №1) | `Property` (БД Каталогу) | Знімок на момент подання заявки — **не оновлюється** (аналог `OrderItem.ProductName/UnitPrice` в eShop) |
| `Favorites/Reviews/Inquiries.propertyId`, `propertyTitle`, `city` | БД Активності (MongoDB, Проєкт №3) | `Property` (БД Каталогу) | Копія для читання — оновлюється подією `PropertyUpdatedEvent` |
| `Properties.Status = 'sold'` | БД Каталогу (Проєкт №2) | Угода (БД Угод) | Оновлюється подією `PropertySoldEvent` з Проєкту №1 |

Жодне з цих полів не має FOREIGN KEY на іншу базу — цілісність посилання перевіряє прикладний код.

## 3. Розгортання з нуля

### Проєкт №1 — SQL Server + Dapper (`db/p1/`)

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

### Проєкт №2 — SQL Server + EF Core (`db/p2/`)

```powershell
cd src/EstateHub.Catalog   # або актуальний шлях до проєкту з DbContext

# Застосувати всі міграції (створює базу EstateHub_Catalog і таблиці)
dotnet ef database update

# Альтернатива без .NET-інструментів — готовий SQL прямо з міграцій:
sqlcmd -S localhost -d EstateHub_Catalog -i db/p2/schema.sql
sqlcmd -S localhost -d EstateHub_Catalog -i db/p2/seed.sql
```

Список міграцій (у порядку застосування):
1. `InitialCreate_AddPropertyLocationTable` — базова схема + таблиця `PropertyLocations`, довідникові seed-дані (Категорії, Зручності)
2. `AddAddedAtToPropertyAmenities` — власне поле `AddedAt` у проміжній таблиці `PropertyAmenities`

### Проєкт №3 — MongoDB (`db/p3/`)

```powershell
# 1. Запустити mongod (якщо ще не запущено)
mongod --dbpath D:\Databases\data

# 2. В окремому терміналі — створити колекції з валідацією схеми
mongosh < db/p3/collections.js

# 3. Наповнити тестовими даними (ідемпотентно)
mongosh < db/p3/seed.js
```

## 4. Перевірка після розгортання

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

## 5. Структура репозиторію

```
.
├── README.md
├── docs/
│   ├── erd-project1.md / erd-project1.png
│   └── erd-project2.md / erd-project2.png
└── db/
    ├── p1/   schema.sql · procedures.sql · seed.sql
    ├── p2/   schema.sql · seed.sql
    └── p3/   collections.js · seed.js
```
