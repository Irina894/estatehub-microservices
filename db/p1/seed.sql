-- ============================================================
-- EstateHub_Transactions — Проєкт №1: тестові дані (ідемпотентно)
-- Повторний запуск НЕ створює дублікатів (перевірка IF NOT EXISTS
-- по природному ключу перед кожною вставкою).
-- ============================================================
USE EstateHub_Transactions;
GO

-- ------------------------------------------------------------
-- Realtors — 3 тестові рієлтори (для демонстрації M:N)
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Realtors WHERE Email = 'olena.realtor@estatehub.test')
    INSERT INTO dbo.Realtors (FullName, Email, Phone, IsActive)
    VALUES ('Олена Коваль', 'olena.realtor@estatehub.test', '+380501112233', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Realtors WHERE Email = 'andriy.realtor@estatehub.test')
    INSERT INTO dbo.Realtors (FullName, Email, Phone, IsActive)
    VALUES ('Андрій Шевченко', 'andriy.realtor@estatehub.test', '+380502223344', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Realtors WHERE Email = 'maria.realtor@estatehub.test')
    INSERT INTO dbo.Realtors (FullName, Email, Phone, IsActive)
    VALUES ('Марія Бойко', 'maria.realtor@estatehub.test', '+380503334455', 1);
GO

-- ------------------------------------------------------------
-- Applications — тестова заявка (прив'язка через PropertyTitleSnapshot
-- як природний ключ-маркер seed-запису, щоб не плодити дублікати)
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Applications WHERE PropertyTitleSnapshot = 'SEED: Квартира на Хрещатику')
    INSERT INTO dbo.Applications (ClientId, PropertyId, PriceSnapshot, PropertyTitleSnapshot, Status)
    VALUES (NEWID(), NEWID(), 2500000, 'SEED: Квартира на Хрещатику', 'new');
GO

-- ------------------------------------------------------------
-- ApplicationRealtors — призначення двох рієлторів на seed-заявку
-- (демонстрація M:N: listing_agent + buyer_agent на одній заявці)
-- ------------------------------------------------------------
DECLARE @SeedAppId UNIQUEIDENTIFIER = (
    SELECT TOP 1 Id FROM dbo.Applications WHERE PropertyTitleSnapshot = 'SEED: Квартира на Хрещатику'
);
DECLARE @Realtor1Id UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Realtors WHERE Email = 'olena.realtor@estatehub.test');
DECLARE @Realtor2Id UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Realtors WHERE Email = 'andriy.realtor@estatehub.test');

IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationRealtors WHERE ApplicationId = @SeedAppId AND RealtorId = @Realtor1Id)
    INSERT INTO dbo.ApplicationRealtors (ApplicationId, RealtorId, Role)
    VALUES (@SeedAppId, @Realtor1Id, 'listing_agent');

IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationRealtors WHERE ApplicationId = @SeedAppId AND RealtorId = @Realtor2Id)
    INSERT INTO dbo.ApplicationRealtors (ApplicationId, RealtorId, Role)
    VALUES (@SeedAppId, @Realtor2Id, 'buyer_agent');
GO
