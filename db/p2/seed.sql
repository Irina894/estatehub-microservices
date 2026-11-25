-- ============================================================
-- EstateHub_Catalog — Проєкт №2: довідникові та тестові дані
-- (ідемпотентно — ті самі GUID, що й у EF Core HasData seed)
-- ============================================================
USE EstateHub_Catalog;
GO

-- ------------------------------------------------------------
-- Categories — довідник типів нерухомості
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Id = '149c4f0b-402a-4bc6-8800-47b2c75a40b8')
    INSERT INTO dbo.Categories (Id, Name, CreatedAt, CreatedBy)
    VALUES ('149c4f0b-402a-4bc6-8800-47b2c75a40b8', N'Офіс', '2026-01-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Id = '2700e123-6447-49f3-8b7c-bbddde9531a2')
    INSERT INTO dbo.Categories (Id, Name, CreatedAt, CreatedBy)
    VALUES ('2700e123-6447-49f3-8b7c-bbddde9531a2', N'Будинок', '2026-01-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Id = 'd343467f-5b65-4203-b0e6-5c56d7831411')
    INSERT INTO dbo.Categories (Id, Name, CreatedAt, CreatedBy)
    VALUES ('d343467f-5b65-4203-b0e6-5c56d7831411', N'Квартира', '2026-01-01T00:00:00', NULL);
GO

-- ------------------------------------------------------------
-- Amenities — довідник зручностей
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Amenities WHERE Id = '84f49492-cda4-42b7-a3ed-28a1ba57e9bc')
    INSERT INTO dbo.Amenities (Id, Name)
    VALUES ('84f49492-cda4-42b7-a3ed-28a1ba57e9bc', N'Автономне опалення');

IF NOT EXISTS (SELECT 1 FROM dbo.Amenities WHERE Id = 'c1fa5853-9dcd-4b8c-ab37-18c7ed6bc402')
    INSERT INTO dbo.Amenities (Id, Name)
    VALUES ('c1fa5853-9dcd-4b8c-ab37-18c7ed6bc402', N'Паркінг');
GO

-- ------------------------------------------------------------
-- Properties — один тестовий об'єкт (маркер SEED: у Title,
-- щоб перевірка на дублікат спиралась на природний ключ)
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Properties WHERE Title = N'SEED: Квартира в центрі Києва')
    INSERT INTO dbo.Properties (Id, CategoryId, OwnerId, Title, Description, Price, City, Status, IsDeleted, CreatedAt)
    VALUES (
        NEWID(),
        'd343467f-5b65-4203-b0e6-5c56d7831411',   -- Квартира
        NEWID(),
        N'SEED: Квартира в центрі Києва',
        N'Тестовий запис для перевірки сидування.',
        2500000,
        N'Київ',
        'available',
        0,
        SYSUTCDATETIME()
    );
GO

-- ------------------------------------------------------------
-- PropertyAmenities — прив'язка зручностей до тестового об'єкта
-- ------------------------------------------------------------
DECLARE @SeedPropertyId UNIQUEIDENTIFIER = (
    SELECT TOP 1 Id FROM dbo.Properties WHERE Title = N'SEED: Квартира в центрі Києва'
);

IF NOT EXISTS (
    SELECT 1 FROM dbo.PropertyAmenities
    WHERE PropertyId = @SeedPropertyId AND AmenityId = 'c1fa5853-9dcd-4b8c-ab37-18c7ed6bc402'
)
    INSERT INTO dbo.PropertyAmenities (PropertyId, AmenityId, AddedAt)
    VALUES (@SeedPropertyId, 'c1fa5853-9dcd-4b8c-ab37-18c7ed6bc402', SYSUTCDATETIME());
GO
