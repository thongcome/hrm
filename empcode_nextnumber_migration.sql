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

