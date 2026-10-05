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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001194051_AddUserIdentityFields'
)
BEGIN
    ALTER TABLE [Users] ADD [FailedLoginAttempts] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001194051_AddUserIdentityFields'
)
BEGIN
    ALTER TABLE [Users] ADD [IsEmailConfirmed] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001194051_AddUserIdentityFields'
)
BEGIN
    ALTER TABLE [Users] ADD [LockedOutUntilUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001194051_AddUserIdentityFields'
)
BEGIN
    ALTER TABLE [Users] ADD [PasswordHash] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001194051_AddUserIdentityFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261001194051_AddUserIdentityFields', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001225759_AddPasswordResetTokens'
)
BEGIN
    CREATE TABLE [PasswordResetTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [TokenHash] nvarchar(100) NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [UsedAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [RowVersion] varbinary(max) NOT NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001225759_AddPasswordResetTokens'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PasswordResetTokens_TokenHash] ON [PasswordResetTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001225759_AddPasswordResetTokens'
)
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001225759_AddPasswordResetTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261001225759_AddPasswordResetTokens', N'10.0.12');
END;

COMMIT;
GO

-- Required baseline data only: one Standard Job Card and its six checklist items.
-- This migration does not insert demo customers, technicians, users or jobs.
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (
        SELECT 1 FROM [__EFMigrationsHistory]
        WHERE [MigrationId] = N'20261003120000_AddStandardJobCardTemplate'
    )
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM [dbo].[JobTemplates] WITH (UPDLOCK, HOLDLOCK)
            WHERE [Name] = N'Standard Job Card' AND [IsDeleted] = 0
        )
        BEGIN
            IF EXISTS (SELECT 1 FROM [dbo].[JobTemplates] WHERE [Id] = '7c59055c-7d5a-4a02-9191-e95729b23bab')
                THROW 51210, 'Reserved Standard Job Card template ID is already in use.', 1;

            INSERT INTO [dbo].[JobTemplates]
                ([Id], [Name], [Category], [Description], [TimesUsed], [RowVersion], [IsDeleted], [DeletedAtUtc])
            VALUES
                ('7c59055c-7d5a-4a02-9191-e95729b23bab', N'Standard Job Card', N'All field work',
                 N'The single standard checklist used for every Verteco job card.',
                 0, CONVERT(varbinary(16), NEWID()), 0, NULL);

            INSERT INTO [dbo].[TemplateChecklistItems]
                ([Id], [JobTemplateId], [Order], [Text], [RowVersion])
            VALUES
                (NEWID(), '7c59055c-7d5a-4a02-9191-e95729b23bab', 0, N'Confirm site access and safety controls', CONVERT(varbinary(16), NEWID())),
                (NEWID(), '7c59055c-7d5a-4a02-9191-e95729b23bab', 1, N'Record equipment and serial numbers', CONVERT(varbinary(16), NEWID())),
                (NEWID(), '7c59055c-7d5a-4a02-9191-e95729b23bab', 2, N'Complete installation, inspection or maintenance work', CONVERT(varbinary(16), NEWID())),
                (NEWID(), '7c59055c-7d5a-4a02-9191-e95729b23bab', 3, N'Record test results and outstanding issues', CONVERT(varbinary(16), NEWID())),
                (NEWID(), '7c59055c-7d5a-4a02-9191-e95729b23bab', 4, N'Capture photographic evidence', CONVERT(varbinary(16), NEWID())),
                (NEWID(), '7c59055c-7d5a-4a02-9191-e95729b23bab', 5, N'Obtain technician and client sign-off', CONVERT(varbinary(16), NEWID()));
        END;

        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20261003120000_AddStandardJobCardTemplate', N'10.0.12');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- Mobile delta pull: changed-job markers for technician sync.
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (
        SELECT 1 FROM [__EFMigrationsHistory]
        WHERE [MigrationId] = N'20261003143502_MobileSyncDeltaPull'
    )
    BEGIN
        CREATE TABLE [MobileSyncChanges] (
            [Id] uniqueidentifier NOT NULL,
            [JobCardId] uniqueidentifier NOT NULL,
            [ChangedAtUtc] datetime2 NOT NULL,
            [RowVersion] varbinary(max) NOT NULL,
            CONSTRAINT [PK_MobileSyncChanges] PRIMARY KEY ([Id])
        );

        CREATE INDEX [IX_MobileSyncChanges_ChangedAtUtc]
            ON [MobileSyncChanges] ([ChangedAtUtc]);
        CREATE INDEX [IX_MobileSyncChanges_JobCardId]
            ON [MobileSyncChanges] ([JobCardId]);
        CREATE INDEX [IX_MobileSyncChanges_JobCardId_ChangedAtUtc]
            ON [MobileSyncChanges] ([JobCardId], [ChangedAtUtc]);

        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20261003143502_MobileSyncDeltaPull', N'10.0.12');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- Persist refresh tokens for mobile sessions.
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (
        SELECT 1 FROM [__EFMigrationsHistory]
        WHERE [MigrationId] = N'20261004132918_AddRefreshTokens'
    )
    BEGIN
        CREATE TABLE [RefreshTokens] (
            [Id] uniqueidentifier NOT NULL,
            [UserId] uniqueidentifier NOT NULL,
            [TokenHash] nvarchar(128) NOT NULL,
            [CreatedAtUtc] datetime2 NOT NULL,
            [ExpiresAtUtc] datetime2 NOT NULL,
            [RevokedAtUtc] datetime2 NULL,
            [ReplacedByTokenHash] nvarchar(128) NULL,
            [RowVersion] varbinary(max) NOT NULL,
            CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_RefreshTokens_Users_UserId]
                FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
        );

        CREATE INDEX [IX_RefreshTokens_ExpiresAtUtc]
            ON [RefreshTokens] ([ExpiresAtUtc]);
        CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash]
            ON [RefreshTokens] ([TokenHash]);
        CREATE INDEX [IX_RefreshTokens_UserId]
            ON [RefreshTokens] ([UserId]);

        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20261004132918_AddRefreshTokens', N'10.0.12');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
