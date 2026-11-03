-- ============================================================
-- EstateHub_Transactions — Проєкт №1 (SQL Server + ADO.NET & Dapper)
-- Контекст: Заявки та угоди (Applications & Transactions)
-- ============================================================

IF DB_ID('EstateHub_Transactions') IS NULL
    CREATE DATABASE EstateHub_Transactions;
GO

USE EstateHub_Transactions;
GO

-- ------------------------------------------------------------
-- Realtors — довідник рієлторів (M:N з Applications)
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Realtors', 'U') IS NULL
CREATE TABLE dbo.Realtors (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    FullName    NVARCHAR(150)    NOT NULL,
    Email       NVARCHAR(100)    NOT NULL,
    Phone       NVARCHAR(20)     NOT NULL,
    IsActive    BIT              NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   UNIQUEIDENTIFIER NULL,
    CONSTRAINT UQ_Realtors_Email UNIQUE (Email)
);
GO

-- ------------------------------------------------------------
-- Applications — агрегат-корінь (reference на Каталог, без FK)
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Applications', 'U') IS NULL
CREATE TABLE dbo.Applications (
    Id                      UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    ClientId                UNIQUEIDENTIFIER NOT NULL,
    PropertyId              UNIQUEIDENTIFIER NOT NULL,              -- reference на БД Каталогу, без FK
    PriceSnapshot           DECIMAL(18,2)     NOT NULL,
    PropertyTitleSnapshot   NVARCHAR(200)     NOT NULL,
    Status                  NVARCHAR(20)      NOT NULL DEFAULT 'new',
    RowVersion              ROWVERSION,
    IsDeleted               BIT               NOT NULL DEFAULT 0,
    CreatedAt               DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy               UNIQUEIDENTIFIER  NULL,
    UpdatedAt                DATETIME2        NULL,
    UpdatedBy                UNIQUEIDENTIFIER NULL,
    CONSTRAINT CK_Applications_PriceSnapshot CHECK (PriceSnapshot > 0),
    CONSTRAINT CK_Applications_Status CHECK (Status IN ('new', 'approved', 'rejected', 'completed'))
);
GO

-- ------------------------------------------------------------
-- Viewings — 1:N з Applications, N:1 з Realtors
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Viewings', 'U') IS NULL
CREATE TABLE dbo.Viewings (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    ApplicationId   UNIQUEIDENTIFIER NOT NULL
                      REFERENCES dbo.Applications(Id),
    RealtorId       UNIQUEIDENTIFIER NOT NULL
                      REFERENCES dbo.Realtors(Id),
    [Date]          DATETIME2        NOT NULL,
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'scheduled',
    CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       UNIQUEIDENTIFIER NULL
);
GO

-- ------------------------------------------------------------
-- Contracts — 1:1 з Applications (UNIQUE ApplicationId)
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Contracts', 'U') IS NULL
CREATE TABLE dbo.Contracts (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    ApplicationId   UNIQUEIDENTIFIER NOT NULL
                      REFERENCES dbo.Applications(Id),
    FinalPrice      DECIMAL(18,2)    NOT NULL,
    SignedAt        DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       UNIQUEIDENTIFIER NULL,
    CONSTRAINT UQ_Contracts_ApplicationId UNIQUE (ApplicationId),   -- гарантує 1:1
    CONSTRAINT CK_Contracts_FinalPrice CHECK (FinalPrice > 0)
);
GO

-- ------------------------------------------------------------
-- ApplicationRealtors — junction M:N з власними полями
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.ApplicationRealtors', 'U') IS NULL
CREATE TABLE dbo.ApplicationRealtors (
    ApplicationId   UNIQUEIDENTIFIER NOT NULL
                      REFERENCES dbo.Applications(Id),
    RealtorId       UNIQUEIDENTIFIER NOT NULL
                      REFERENCES dbo.Realtors(Id),
    Role            NVARCHAR(20)     NOT NULL,          -- listing_agent | buyer_agent
    AssignedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ApplicationRealtors PRIMARY KEY (ApplicationId, RealtorId),
    CONSTRAINT CK_ApplicationRealtors_Role CHECK (Role IN ('listing_agent', 'buyer_agent'))
);
GO

-- ------------------------------------------------------------
-- Індекси під конкретні запити
-- ------------------------------------------------------------

-- «заявки клієнта з певним статусом»
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Applications_ClientId_Status')
    CREATE INDEX IX_Applications_ClientId_Status ON dbo.Applications (ClientId, Status);
GO

-- «тільки живі (не видалені) заявки» — фільтрований індекс
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Applications_Active')
    CREATE INDEX IX_Applications_Active ON dbo.Applications (ClientId) WHERE IsDeleted = 0;
GO

-- «перегляди рієлтора на конкретну дату»
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Viewings_RealtorId_Date')
    CREATE INDEX IX_Viewings_RealtorId_Date ON dbo.Viewings (RealtorId, [Date]);
GO

-- складений PK ApplicationRealtors вже є індексом під запит «усі рієлтори заявки» /
-- «усі заявки рієлтора» зворотного напрямку — додатковий індекс по RealtorId:
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApplicationRealtors_RealtorId')
    CREATE INDEX IX_ApplicationRealtors_RealtorId ON dbo.ApplicationRealtors (RealtorId);
GO
