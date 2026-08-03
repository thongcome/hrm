BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803024107_SeedPosAdminMenu'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
        SET IDENTITY_INSERT [sc_menu] ON;
    EXEC(N'INSERT INTO [sc_menu] ([menuid], [menuname], [menuname_en], [menulevel], [isfinal], [menuorder], [menucode], [isshow], [url], [menugroupid], [isactive])
    VALUES (CAST(24 AS bigint), N''จัดการตำแหน่ง'', N''Position Management'', 1, CAST(1 AS bit), 34, N''POS_ADMIN'', CAST(1 AS bit), ''/pos/exec-types'', CAST(1 AS bigint), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
        SET IDENTITY_INSERT [sc_menu] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803024107_SeedPosAdminMenu'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
        SET IDENTITY_INSERT [sc_role_menu] ON;
    EXEC(N'INSERT INTO [sc_role_menu] ([rolemenuid], [menuid], [roleid], [isactive])
    VALUES (CAST(22 AS bigint), CAST(24 AS bigint), CAST(9 AS bigint), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
        SET IDENTITY_INSERT [sc_role_menu] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803024107_SeedPosAdminMenu'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803024107_SeedPosAdminMenu', N'10.0.10');
END;

COMMIT;
GO

