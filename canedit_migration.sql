BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731141313_AddScRoleMenuCanEdit'
)
BEGIN
    ALTER TABLE [sc_role_menu] ADD [canedit] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731141313_AddScRoleMenuCanEdit'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
        SET IDENTITY_INSERT [sc_menu] ON;
    EXEC(N'INSERT INTO [sc_menu] ([menuid], [menuname], [menuname_en], [menulevel], [isfinal], [menuorder], [menucode], [isshow], [url], [menugroupid], [isactive])
    VALUES (CAST(17 AS bigint), N''จัดการพนักงาน (Workflow demo)'', N''Workflow Employee Admin'', 1, CAST(1 AS bit), 30, N''WF_EMPLOYEE_ADMIN'', CAST(1 AS bit), ''/wf/employees'', CAST(1 AS bigint), CAST(1 AS bit)),
    (CAST(18 AS bigint), N''จัดการประเภทหน่วยงาน (Workflow demo)'', N''Workflow Org Type Admin'', 1, CAST(1 AS bit), 31, N''WF_ORG_TYPE_ADMIN'', CAST(1 AS bit), ''/wf/org-types'', CAST(1 AS bigint), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
        SET IDENTITY_INSERT [sc_menu] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731141313_AddScRoleMenuCanEdit'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive', N'canedit') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
        SET IDENTITY_INSERT [sc_role_menu] ON;
    EXEC(N'INSERT INTO [sc_role_menu] ([rolemenuid], [menuid], [roleid], [isactive], [canedit])
    VALUES (CAST(15 AS bigint), CAST(17 AS bigint), CAST(9 AS bigint), CAST(1 AS bit), CAST(1 AS bit)),
    (CAST(16 AS bigint), CAST(18 AS bigint), CAST(9 AS bigint), CAST(1 AS bit), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive', N'canedit') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
        SET IDENTITY_INSERT [sc_role_menu] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731141313_AddScRoleMenuCanEdit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731141313_AddScRoleMenuCanEdit', N'10.0.10');
END;

COMMIT;
GO

