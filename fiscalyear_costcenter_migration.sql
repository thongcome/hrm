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

