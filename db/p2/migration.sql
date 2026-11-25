IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Amenities] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Amenities] PRIMARY KEY ([Id])
);

CREATE TABLE [Categories] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);

CREATE TABLE [Properties] (
    [Id] uniqueidentifier NOT NULL,
    [CategoryId] uniqueidentifier NOT NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(4000) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [City] nvarchar(100) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_Properties] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Property_Price] CHECK ([Price] > 0),
    CONSTRAINT [FK_Properties_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PropertyAmenities] (
    [PropertyId] uniqueidentifier NOT NULL,
    [AmenityId] uniqueidentifier NOT NULL,
    [AddedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_PropertyAmenities] PRIMARY KEY ([PropertyId], [AmenityId]),
    CONSTRAINT [FK_PropertyAmenities_Amenities_AmenityId] FOREIGN KEY ([AmenityId]) REFERENCES [Amenities] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PropertyAmenities_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PropertyImages] (
    [Id] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [ImageUrl] nvarchar(2048) NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_PropertyImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PropertyImages_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PropertyLocations] (
    [Id] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Latitude] float NULL,
    [Longitude] float NULL,
    [FullAddress] nvarchar(500) NOT NULL,
    [IsGeocoded] bit NOT NULL,
    CONSTRAINT [PK_PropertyLocations] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_PropertyLocations_Latitude] CHECK ([Latitude] IS NULL OR [Latitude] BETWEEN -90 AND 90),
    CONSTRAINT [CK_PropertyLocations_Longitude] CHECK ([Longitude] IS NULL OR [Longitude] BETWEEN -180 AND 180),
    CONSTRAINT [FK_PropertyLocations_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Amenities]'))
    SET IDENTITY_INSERT [Amenities] ON;
INSERT INTO [Amenities] ([Id], [Name])
VALUES ('5b0c1a62-7e0d-4a54-9c43-0d6c8f1e2a11', N'Ліфт'),
('84f49492-cda4-42b7-a3ed-28a1ba57e9bc', N'Автономне опалення'),
('a7d3e9f4-1b2c-4d58-8e61-3f4a5b6c7d22', N'Балкон'),
('c1fa5853-9dcd-4b8c-ab37-18c7ed6bc402', N'Паркінг'),
('e2f1d0c9-8b7a-4695-a4b3-c2d1e0f9a833', N'Кондиціонер');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Amenities]'))
    SET IDENTITY_INSERT [Amenities] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'Name') AND [object_id] = OBJECT_ID(N'[Categories]'))
    SET IDENTITY_INSERT [Categories] ON;
INSERT INTO [Categories] ([Id], [CreatedAt], [CreatedBy], [Name])
VALUES ('149c4f0b-402a-4bc6-8800-47b2c75a40b8', '2026-01-01T00:00:00.0000000Z', NULL, N'Офіс'),
('2700e123-6447-49f3-8b7c-bbddde9531a2', '2026-01-01T00:00:00.0000000Z', NULL, N'Будинок'),
('d343467f-5b65-4203-b0e6-5c56d7831411', '2026-01-01T00:00:00.0000000Z', NULL, N'Квартира');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'Name') AND [object_id] = OBJECT_ID(N'[Categories]'))
    SET IDENTITY_INSERT [Categories] OFF;

CREATE UNIQUE INDEX [UQ_Amenities_Name] ON [Amenities] ([Name]);

CREATE UNIQUE INDEX [UQ_Categories_Name] ON [Categories] ([Name]);

CREATE INDEX [IX_Properties_CategoryId] ON [Properties] ([CategoryId]);

CREATE INDEX [IX_Properties_City_Price] ON [Properties] ([City], [Price]);

CREATE INDEX [IX_Properties_Status] ON [Properties] ([Status]) WHERE [Status] = 'available';

CREATE INDEX [IX_PropertyAmenities_AmenityId_AddedAt] ON [PropertyAmenities] ([AmenityId], [AddedAt]);

CREATE INDEX [IX_PropertyImages_PropertyId] ON [PropertyImages] ([PropertyId]);

CREATE UNIQUE INDEX [IX_PropertyLocations_PropertyId] ON [PropertyLocations] ([PropertyId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261024085346_InitialCreate', N'10.0.0');

COMMIT;
GO

