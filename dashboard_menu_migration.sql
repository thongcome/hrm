BEGIN TRANSACTION;

-- Only run if migration not already recorded
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260730172348_SeedPayrollDashboardMenu')
BEGIN
    -- Insert menu if table exists and menu not already present
    IF OBJECT_ID(N'dbo.sc_menu', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.sc_menu WHERE menucode = N'PAY_REPORTS')
        BEGIN
            -- Enable IDENTITY_INSERT only when the identity column actually exists
            IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [name] = N'menuid' AND [object_id] = OBJECT_ID(N'dbo.sc_menu'))
                SET IDENTITY_INSERT dbo.sc_menu ON;

            INSERT INTO dbo.sc_menu ([menuid], [menuname], [menuname_en], [menulevel], [isfinal], [menuorder], [menucode], [isshow], [url], [menugroupid], [isactive])
            VALUES (CAST(12 AS bigint), N'แดชบอร์ดเงินเดือน', N'Payroll Dashboard', 1, CAST(1 AS bit), 10, N'PAY_REPORTS', CAST(1 AS bit), N'/pay/dashboard', CAST(1 AS bigint), CAST(1 AS bit));

            IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [name] = N'menuid' AND [object_id] = OBJECT_ID(N'dbo.sc_menu'))
                SET IDENTITY_INSERT dbo.sc_menu OFF;
        END
    END

    -- Insert role-menu mapping if table exists and mapping not already present
    IF OBJECT_ID(N'dbo.sc_role_menu', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.sc_role_menu WHERE menuid = 12 AND roleid = 9)
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [name] = N'rolemenuid' AND [object_id] = OBJECT_ID(N'dbo.sc_role_menu'))
                SET IDENTITY_INSERT dbo.sc_role_menu ON;

            INSERT INTO dbo.sc_role_menu ([rolemenuid], [menuid], [roleid], [isactive])
            VALUES (CAST(11 AS bigint), CAST(12 AS bigint), CAST(9 AS bigint), CAST(1 AS bit));

            IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [name] = N'rolemenuid' AND [object_id] = OBJECT_ID(N'dbo.sc_role_menu'))
                SET IDENTITY_INSERT dbo.sc_role_menu OFF;
        END
    END

    -- Record migration in history
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730172348_SeedPayrollDashboardMenu', N'10.0.10');
END;

COMMIT;
GO

