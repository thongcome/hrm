BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730170134_AddPayslipCompanyTaxInfo'
)
BEGIN
    ALTER TABLE [Pay_PayslipSettings] ADD [CompanyAddress] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730170134_AddPayslipCompanyTaxInfo'
)
BEGIN
    ALTER TABLE [Pay_PayslipSettings] ADD [CompanyTaxId] nvarchar(13) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730170134_AddPayslipCompanyTaxInfo'
)
BEGIN
    EXEC(N'UPDATE [Pay_PayslipSettings] SET [CompanyAddress] = NULL, [CompanyTaxId] = NULL
    WHERE [Id] = CAST(1 AS bigint);
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730170134_AddPayslipCompanyTaxInfo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730170134_AddPayslipCompanyTaxInfo', N'10.0.10');
END;

COMMIT;
GO

