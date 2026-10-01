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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] uniqueidentifier NOT NULL,
        [ActorUserId] uniqueidentifier NULL,
        [JobCardId] uniqueidentifier NULL,
        [Action] nvarchar(100) NOT NULL,
        [Details] nvarchar(1000) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Industry] nvarchar(100) NOT NULL,
        [ContactName] nvarchar(150) NOT NULL,
        [ContactEmail] nvarchar(200) NOT NULL,
        [ContactPhone] nvarchar(50) NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [JobTemplates] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [TimesUsed] int NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_JobTemplates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [Technicians] (
        [Id] uniqueidentifier NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [Phone] nvarchar(50) NOT NULL,
        [Skills] nvarchar(max) NOT NULL,
        [Availability] int NOT NULL,
        [JobsCompleted] int NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Technicians] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [Sites] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Address] nvarchar(300) NOT NULL,
        [SiteType] nvarchar(100) NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Sites] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sites_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [TemplateChecklistItems] (
        [Id] uniqueidentifier NOT NULL,
        [JobTemplateId] uniqueidentifier NOT NULL,
        [Order] int NOT NULL,
        [Text] nvarchar(300) NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        CONSTRAINT [PK_TemplateChecklistItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TemplateChecklistItems_JobTemplates_JobTemplateId] FOREIGN KEY ([JobTemplateId]) REFERENCES [JobTemplates] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [FullName] nvarchar(200) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [Phone] nvarchar(50) NOT NULL,
        [Role] nvarchar(30) NOT NULL,
        [TechnicianId] uniqueidentifier NULL,
        [IsSuspended] bit NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Technicians_TechnicianId] FOREIGN KEY ([TechnicianId]) REFERENCES [Technicians] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [Equipment] (
        [Id] uniqueidentifier NOT NULL,
        [SiteId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [SerialNumber] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NULL,
        [RowVersion] varbinary(max) NOT NULL,
        CONSTRAINT [PK_Equipment] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Equipment_Sites_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [Sites] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [JobCards] (
        [Id] uniqueidentifier NOT NULL,
        [Number] nvarchar(30) NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [SiteId] uniqueidentifier NOT NULL,
        [JobTemplateId] uniqueidentifier NOT NULL,
        [TechnicianId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [Priority] int NOT NULL,
        [ScheduledDate] date NOT NULL,
        [CompletedAtUtc] datetime2 NULL,
        [Notes] nvarchar(2000) NULL,
        [ReviewNotes] nvarchar(2000) NULL,
        [ReopenReason] nvarchar(1000) NULL,
        [CancelReason] nvarchar(1000) NULL,
        [SealedHash] nvarchar(64) NULL,
        [SealedAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_JobCards] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobCards_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JobCards_JobTemplates_JobTemplateId] FOREIGN KEY ([JobTemplateId]) REFERENCES [JobTemplates] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JobCards_Sites_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [Sites] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JobCards_Technicians_TechnicianId] FOREIGN KEY ([TechnicianId]) REFERENCES [Technicians] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [JobCardRevisions] (
        [Id] uniqueidentifier NOT NULL,
        [JobCardId] uniqueidentifier NOT NULL,
        [Number] int NOT NULL,
        [PreviousRevisionId] uniqueidentifier NULL,
        [ContentJson] nvarchar(max) NOT NULL,
        [ContentHash] nvarchar(64) NOT NULL,
        [SubmittedAtUtc] datetime2 NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        CONSTRAINT [PK_JobCardRevisions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobCardRevisions_JobCards_JobCardId] FOREIGN KEY ([JobCardId]) REFERENCES [JobCards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [JobChecklistItems] (
        [Id] uniqueidentifier NOT NULL,
        [JobCardId] uniqueidentifier NOT NULL,
        [Order] int NOT NULL,
        [Text] nvarchar(300) NOT NULL,
        [IsDone] bit NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        CONSTRAINT [PK_JobChecklistItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobChecklistItems_JobCards_JobCardId] FOREIGN KEY ([JobCardId]) REFERENCES [JobCards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [JobEvidence] (
        [Id] uniqueidentifier NOT NULL,
        [JobCardId] uniqueidentifier NOT NULL,
        [Reference] nvarchar(500) NOT NULL,
        [Caption] nvarchar(500) NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        CONSTRAINT [PK_JobEvidence] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobEvidence_JobCards_JobCardId] FOREIGN KEY ([JobCardId]) REFERENCES [JobCards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE TABLE [JobSignatures] (
        [Id] uniqueidentifier NOT NULL,
        [JobCardId] uniqueidentifier NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [SignerName] nvarchar(200) NOT NULL,
        [Reference] nvarchar(500) NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        CONSTRAINT [PK_JobSignatures] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobSignatures_JobCards_JobCardId] FOREIGN KEY ([JobCardId]) REFERENCES [JobCards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_CreatedAtUtc] ON [AuditLogs] ([CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_JobCardId] ON [AuditLogs] ([JobCardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Equipment_SerialNumber] ON [Equipment] ([SerialNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_Equipment_SiteId] ON [Equipment] ([SiteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JobCardRevisions_JobCardId_Number] ON [JobCardRevisions] ([JobCardId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_JobCards_CustomerId] ON [JobCards] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_JobCards_JobTemplateId] ON [JobCards] ([JobTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JobCards_Number] ON [JobCards] ([Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_JobCards_ScheduledDate] ON [JobCards] ([ScheduledDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_JobCards_SiteId] ON [JobCards] ([SiteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_JobCards_Status] ON [JobCards] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_JobCards_TechnicianId] ON [JobCards] ([TechnicianId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_JobChecklistItems_JobCardId] ON [JobChecklistItems] ([JobCardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_JobEvidence_JobCardId] ON [JobEvidence] ([JobCardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_JobSignatures_JobCardId] ON [JobSignatures] ([JobCardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_Sites_CustomerId] ON [Sites] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Technicians_Email] ON [Technicians] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_TemplateChecklistItems_JobTemplateId] ON [TemplateChecklistItems] ([JobTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    CREATE INDEX [IX_Users_TechnicianId] ON [Users] ([TechnicianId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929174009_InitialAzureSql'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260929174009_InitialAzureSql', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930175459_FlaggedJobsAndCorrectiveActions'
)
BEGIN
    ALTER TABLE [JobCards] ADD [FlagReason] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930175459_FlaggedJobsAndCorrectiveActions'
)
BEGIN
    ALTER TABLE [JobCards] ADD [FlaggedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930175459_FlaggedJobsAndCorrectiveActions'
)
BEGIN
    ALTER TABLE [JobCards] ADD [IsFlagged] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930175459_FlaggedJobsAndCorrectiveActions'
)
BEGIN
    CREATE TABLE [CorrectiveActions] (
        [Id] uniqueidentifier NOT NULL,
        [JobCardId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [OpenedAtUtc] datetime2 NOT NULL,
        [OpenedByUserId] uniqueidentifier NULL,
        [ResolvedAtUtc] datetime2 NULL,
        [ResolvedByUserId] uniqueidentifier NULL,
        [ResolutionNotes] nvarchar(2000) NULL,
        [RowVersion] varbinary(max) NOT NULL,
        CONSTRAINT [PK_CorrectiveActions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CorrectiveActions_JobCards_JobCardId] FOREIGN KEY ([JobCardId]) REFERENCES [JobCards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930175459_FlaggedJobsAndCorrectiveActions'
)
BEGIN
    CREATE INDEX [IX_JobCards_IsFlagged] ON [JobCards] ([IsFlagged]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930175459_FlaggedJobsAndCorrectiveActions'
)
BEGIN
    CREATE INDEX [IX_CorrectiveActions_JobCardId_ResolvedAtUtc] ON [CorrectiveActions] ([JobCardId], [ResolvedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930175459_FlaggedJobsAndCorrectiveActions'
)
BEGIN
    CREATE INDEX [IX_CorrectiveActions_OpenedAtUtc] ON [CorrectiveActions] ([OpenedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930175459_FlaggedJobsAndCorrectiveActions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260930175459_FlaggedJobsAndCorrectiveActions', N'10.0.12');
END;

COMMIT;
GO

