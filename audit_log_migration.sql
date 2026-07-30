BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730154232_AddAuditLog'
)
BEGIN
    CREATE TABLE [AuditLog] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorUserId] bigint NULL,
        [ActorName] nvarchar(200) NULL,
        [EventDate] datetime2 NOT NULL,
        [Action] int NOT NULL,
        [EntityType] nvarchar(200) NOT NULL,
        [RecordId] nvarchar(100) NULL,
        [OldValuesJson] nvarchar(max) NULL,
        [NewValuesJson] nvarchar(max) NULL,
        [IsSensitiveDataAccess] bit NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [Note] nvarchar(500) NULL,
        CONSTRAINT [PK_AuditLog] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730154232_AddAuditLog'
)
BEGIN
    CREATE INDEX [IX_AuditLog_EntityType_RecordId] ON [AuditLog] ([EntityType], [RecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730154232_AddAuditLog'
)
BEGIN
    CREATE INDEX [IX_AuditLog_EventDate] ON [AuditLog] ([EventDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730154232_AddAuditLog'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
        SET IDENTITY_INSERT [sc_menu] ON;
    EXEC(N'INSERT INTO [sc_menu] ([menuid], [menuname], [menuname_en], [menulevel], [isfinal], [menuorder], [menucode], [isshow], [url], [menugroupid], [isactive])
    VALUES (CAST(10 AS bigint), N''Audit Log ระบบ'', N''System Audit Log'', 1, CAST(1 AS bit), 8, N''SYS_AUDIT_LOG'', CAST(1 AS bit), ''/admin/audit-log'', CAST(1 AS bigint), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
        SET IDENTITY_INSERT [sc_menu] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730154232_AddAuditLog'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
        SET IDENTITY_INSERT [sc_role_menu] ON;
    EXEC(N'INSERT INTO [sc_role_menu] ([rolemenuid], [menuid], [roleid], [isactive])
    VALUES (CAST(9 AS bigint), CAST(10 AS bigint), CAST(9 AS bigint), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
        SET IDENTITY_INSERT [sc_role_menu] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730154232_AddAuditLog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730154232_AddAuditLog', N'10.0.10');
END;

COMMIT;
GO

