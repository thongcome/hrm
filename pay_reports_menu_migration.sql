BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730152633_SeedPayReportsMenu'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
        SET IDENTITY_INSERT [sc_menu] ON;
    EXEC(N'INSERT INTO [sc_menu] ([menuid], [menuname], [menuname_en], [menulevel], [isfinal], [menuorder], [menucode], [isshow], [url], [menugroupid], [isactive])
    VALUES (CAST(8 AS bigint), N''แนวโน้มต้นทุนแรงงาน'', N''Labor Cost Trend'', 1, CAST(1 AS bit), 6, N''PAY_REPORTS'', CAST(1 AS bit), ''/pay/reports/labor-cost-trend'', CAST(1 AS bigint), CAST(1 AS bit)),
    (CAST(9 AS bigint), N''สัดส่วนรายรับ-รายหักตามประเภท'', N''Pay Item Breakdown'', 1, CAST(1 AS bit), 7, N''PAY_REPORTS'', CAST(1 AS bit), ''/pay/reports/pay-item-breakdown'', CAST(1 AS bigint), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
        SET IDENTITY_INSERT [sc_menu] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730152633_SeedPayReportsMenu'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
        SET IDENTITY_INSERT [sc_role_menu] ON;
    EXEC(N'INSERT INTO [sc_role_menu] ([rolemenuid], [menuid], [roleid], [isactive])
    VALUES (CAST(7 AS bigint), CAST(8 AS bigint), CAST(9 AS bigint), CAST(1 AS bit)),
    (CAST(8 AS bigint), CAST(9 AS bigint), CAST(9 AS bigint), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
        SET IDENTITY_INSERT [sc_role_menu] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730152633_SeedPayReportsMenu'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730152633_SeedPayReportsMenu', N'10.0.10');
END;

COMMIT;
GO

