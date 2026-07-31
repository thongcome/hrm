BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731011410_AddEmpCodeFormat'
)
BEGIN
    ALTER TABLE [Pay_PayslipSettings] ADD [EmpCodeDigits] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731011410_AddEmpCodeFormat'
)
BEGIN
    ALTER TABLE [Pay_PayslipSettings] ADD [EmpCodePrefix] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731011410_AddEmpCodeFormat'
)
BEGIN
    EXEC(N'UPDATE [Pay_PayslipSettings] SET [EmpCodeDigits] = 3, [EmpCodePrefix] = NULL
    WHERE [Id] = CAST(1 AS bigint);
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731011410_AddEmpCodeFormat'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731011410_AddEmpCodeFormat', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731014903_AddEmpCodeNextNumber'
)
BEGIN
    ALTER TABLE [Pay_PayslipSettings] ADD [EmpCodeNextNumber] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731014903_AddEmpCodeNextNumber'
)
BEGIN
    EXEC(N'UPDATE [Pay_PayslipSettings] SET [EmpCodeNextNumber] = NULL
    WHERE [Id] = CAST(1 AS bigint);
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731014903_AddEmpCodeNextNumber'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731014903_AddEmpCodeNextNumber', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731022808_AddFiscalYearAndCostCenter'
)
BEGIN
    ALTER TABLE [Pay_PayslipSettings] ADD [FiscalYearStartMonth] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731022808_AddFiscalYearAndCostCenter'
)
BEGIN
    ALTER TABLE [Pay_PayrollEmployee] ADD [CostCenterCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731022808_AddFiscalYearAndCostCenter'
)
BEGIN
    ALTER TABLE [HREMPLOYEE] ADD [CostCenterCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731022808_AddFiscalYearAndCostCenter'
)
BEGIN
    ALTER TABLE [com_organization] ADD [CostCenterCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731022808_AddFiscalYearAndCostCenter'
)
BEGIN
    EXEC(N'UPDATE [Pay_PayslipSettings] SET [FiscalYearStartMonth] = 1
    WHERE [Id] = CAST(1 AS bigint);
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731022808_AddFiscalYearAndCostCenter'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731022808_AddFiscalYearAndCostCenter', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731030502_SeedEssAccessMenu'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
        SET IDENTITY_INSERT [sc_menu] ON;
    EXEC(N'INSERT INTO [sc_menu] ([menuid], [menuname], [menuname_en], [menulevel], [isfinal], [menuorder], [menucode], [isshow], [url], [menugroupid], [isactive])
    VALUES (CAST(16 AS bigint), N''พื้นที่พนักงาน (ESS)'', N''Employee Self-Service'', 1, CAST(1 AS bit), 20, N''ESS_ACCESS'', CAST(1 AS bit), ''/ess'', CAST(1 AS bigint), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
        SET IDENTITY_INSERT [sc_menu] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731030502_SeedEssAccessMenu'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
        SET IDENTITY_INSERT [sc_role_menu] ON;
    EXEC(N'INSERT INTO [sc_role_menu] ([rolemenuid], [menuid], [roleid], [isactive])
    VALUES (CAST(14 AS bigint), CAST(16 AS bigint), CAST(10 AS bigint), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
        SET IDENTITY_INSERT [sc_role_menu] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731030502_SeedEssAccessMenu'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731030502_SeedEssAccessMenu', N'10.0.10');
END;

COMMIT;
GO

