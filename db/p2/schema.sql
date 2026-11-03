-- ============================================================
-- EstateHub_Catalog — Проєкт №2 (SQL Server + EF Core Code First)
-- Контекст: Каталог нерухомості (Property Catalog)
--
-- Цей файл — еквівалент SQL, який генерують EF Core-міграції
-- (InitialCreate_AddPropertyLocationTable + AddAddedAtToPropertyAmenities).
-- Якщо розгортаєте через `dotnet ef database update`, цей файл
-- виконувати не потрібно — він для розгортання без .NET-інструментів.
-- ============================================================

IF DB_ID('EstateHub_Catalog') IS NULL
    CREATE DATABASE EstateHub_Catalog;
GO

USE EstateHub_Catalog;
GO

-- ------------------------------------------------------------
-- Categories — 1:N з Properties
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Categories', 'U') IS NULL
CREATE TABLE dbo.Categories (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name        NVARCHAR(100)    NOT NULL,
    CreatedAt   DATETIME2        NOT NULL,
    CreatedBy   UNIQUEIDENTIFIER NULL,
    CONSTRAINT UQ_Categories_Name UNIQUE (Name)
);
GO

-- ------------------------------------------------------------
-- Amenities — M:N з Properties
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Amenities', 'U') IS NULL
CREATE TABLE dbo.Amenities (
    Id     UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name   NVARCHAR(100)    NOT NULL,
    CONSTRAINT UQ_Amenities_Name UNIQUE (Name)
);
GO

-- ------------------------------------------------------------
-- Properties — агрегат-корінь
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Properties', 'U') IS NULL
CREATE TABLE dbo.Properties (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    CategoryId      UNIQUEIDENTIFIER NOT NULL
                      REFERENCES dbo.Categories(Id),
    OwnerId         UNIQUEIDENTIFIER NOT NULL,
    Title           NVARCHAR(200)    NOT NULL,
    Description     NVARCHAR(MAX)    NOT NULL,
    Price           DECIMAL(18,2)    NOT NULL,
    City            NVARCHAR(100)    NOT NULL,
    Status          NVARCHAR(20)     NOT NULL,
    RowVersion      ROWVERSION,
    IsDeleted       BIT              NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       UNIQUEIDENTIFIER NULL,
    UpdatedAt       DATETIME2        NULL,
    UpdatedBy       UNIQUEIDENTIFIER NULL,
    CONSTRAINT CK_Property_Price CHECK (Price > 0)
);
GO

-- ------------------------------------------------------------
-- PropertyImages — 1:N з Properties, CASCADE на видалення
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.PropertyImages', 'U') IS NULL
CREATE TABLE dbo.PropertyImages (
    Id           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PropertyId   UNIQUEIDENTIFIER NOT NULL
                   REFERENCES dbo.Properties(Id) ON DELETE CASCADE,
    ImageUrl     NVARCHAR(MAX)    NOT NULL,
    SortOrder    INT              NOT NULL
);
GO

-- ------------------------------------------------------------
-- PropertyLocations — 1:1 з Properties (RESTRICT, не CASCADE)
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.PropertyLocations', 'U') IS NULL
CREATE TABLE dbo.PropertyLocations (
    Id             UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PropertyId     UNIQUEIDENTIFIER NOT NULL
                     REFERENCES dbo.Properties(Id),
    Latitude       FLOAT            NULL,
    Longitude      FLOAT            NULL,
    FullAddress    NVARCHAR(MAX)    NOT NULL,
    IsGeocoded     BIT              NOT NULL,
    CONSTRAINT UQ_PropertyLocations_PropertyId UNIQUE (PropertyId)   -- гарантує 1:1
);
GO

-- ------------------------------------------------------------
-- PropertyAmenities — junction M:N з власним полем AddedAt
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.PropertyAmenities', 'U') IS NULL
CREATE TABLE dbo.PropertyAmenities (
    PropertyId   UNIQUEIDENTIFIER NOT NULL
                   REFERENCES dbo.Properties(Id) ON DELETE CASCADE,
    AmenityId    UNIQUEIDENTIFIER NOT NULL
                   REFERENCES dbo.Amenities(Id) ON DELETE CASCADE,
    AddedAt      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),      -- власне поле junction-таблиці
    CONSTRAINT PK_PropertyAmenities PRIMARY KEY (PropertyId, AmenityId)
);
GO

-- ------------------------------------------------------------
-- Індекси під конкретні запити
-- ------------------------------------------------------------

-- «пошук за містом і ціною»
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Properties_City_Price')
    CREATE INDEX IX_Properties_City_Price ON dbo.Properties (City, Price);
GO

-- «тільки доступні об'єкти» — фільтрований індекс
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Properties_Status')
    CREATE INDEX IX_Properties_Status ON dbo.Properties (Status) WHERE Status = 'available';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Properties_CategoryId')
    CREATE INDEX IX_Properties_CategoryId ON dbo.Properties (CategoryId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PropertyImages_PropertyId')
    CREATE INDEX IX_PropertyImages_PropertyId ON dbo.PropertyImages (PropertyId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PropertyAmenities_PropertyId')
    CREATE INDEX IX_PropertyAmenities_PropertyId ON dbo.PropertyAmenities (PropertyId);
GO
