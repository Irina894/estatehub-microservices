-- ============================================================
-- EstateHub_Transactions — Проєкт №1: збережувані процедури
-- ============================================================
USE EstateHub_Transactions;
GO

-- ------------------------------------------------------------
-- 1. sp_CreateApplication — створення заявки (CRUD: Create)
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_CreateApplication
    @ClientId               UNIQUEIDENTIFIER,
    @PropertyId              UNIQUEIDENTIFIER,
    @PriceSnapshot           DECIMAL(18,2),
    @PropertyTitleSnapshot   NVARCHAR(200),
    @CreatedBy               UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO dbo.Applications (ClientId, PropertyId, PriceSnapshot, PropertyTitleSnapshot, CreatedBy)
    OUTPUT inserted.Id
    VALUES (@ClientId, @PropertyId, @PriceSnapshot, @PropertyTitleSnapshot, @CreatedBy);
END
GO

-- ------------------------------------------------------------
-- 2. sp_AssignRealtorToApplication — бізнес-операція (M:N)
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_AssignRealtorToApplication
    @ApplicationId   UNIQUEIDENTIFIER,
    @RealtorId       UNIQUEIDENTIFIER,
    @Role            NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Applications WHERE Id = @ApplicationId AND IsDeleted = 0)
        THROW 50001, 'Application not found or deleted', 1;

    IF EXISTS (SELECT 1 FROM dbo.ApplicationRealtors WHERE ApplicationId = @ApplicationId AND RealtorId = @RealtorId)
        THROW 50002, 'Realtor already assigned to this application', 1;

    INSERT INTO dbo.ApplicationRealtors (ApplicationId, RealtorId, Role)
    VALUES (@ApplicationId, @RealtorId, @Role);
END
GO

-- ------------------------------------------------------------
-- 3. sp_SoftDeleteApplication — м'яке видалення (CRUD: Delete)
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_SoftDeleteApplication
    @ApplicationId   UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Status NVARCHAR(20) = (SELECT Status FROM dbo.Applications WHERE Id = @ApplicationId);

    IF @Status IS NULL
        THROW 50003, 'Application not found', 1;

    IF @Status = 'completed'
        THROW 50004, 'Cannot delete a completed application', 1;

    UPDATE dbo.Applications
    SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @ApplicationId;
END
GO

-- ------------------------------------------------------------
-- 4. sp_UpdateApplicationStatus — транзакційна зміна статусу
--    (обов'язкова вимога: явна транзакція + відмова на
--    недопустимому переході + оптимістична конкурентність)
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_UpdateApplicationStatus
    @ApplicationId   UNIQUEIDENTIFIER,
    @NewStatus       NVARCHAR(20),
    @RowVersion      ROWVERSION
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @CurrentStatus NVARCHAR(20), @CurrentRowVersion ROWVERSION, @IsDeleted BIT;

    SELECT
        @CurrentStatus = Status,
        @CurrentRowVersion = RowVersion,
        @IsDeleted = IsDeleted
    FROM dbo.Applications WITH (UPDLOCK)
    WHERE Id = @ApplicationId;

    IF @CurrentStatus IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50005, 'Application not found', 1;
    END

    IF @IsDeleted = 1 OR @CurrentStatus = 'completed'
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50006, 'Application is deleted or already completed', 1;
    END

    IF @CurrentRowVersion <> @RowVersion
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50007, 'Concurrency conflict: row was modified by another user', 1;
    END

    -- допустимі переходи
    IF NOT (
        (@CurrentStatus = 'new'      AND @NewStatus = 'approved')  OR
        (@CurrentStatus = 'new'      AND @NewStatus = 'rejected')  OR
        (@CurrentStatus = 'approved' AND @NewStatus = 'completed')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50008, 'Invalid status transition', 1;
    END

    UPDATE dbo.Applications
    SET Status = @NewStatus, UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @ApplicationId;

    COMMIT TRANSACTION;
END
GO

-- ------------------------------------------------------------
-- 5. sp_SignContract — бізнес-операція: approved -> completed
--    + створення Contract в одній транзакції
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_SignContract
    @ApplicationId   UNIQUEIDENTIFIER,
    @FinalPrice      DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(20) = (
        SELECT Status FROM dbo.Applications WITH (UPDLOCK) WHERE Id = @ApplicationId
    );

    IF @Status IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50009, 'Application not found', 1;
    END

    IF @Status <> 'approved'
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50010, 'Contract can be signed only for an approved application', 1;
    END

    INSERT INTO dbo.Contracts (ApplicationId, FinalPrice)
    VALUES (@ApplicationId, @FinalPrice);

    UPDATE dbo.Applications
    SET Status = 'completed', UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @ApplicationId;

    COMMIT TRANSACTION;
END
GO
