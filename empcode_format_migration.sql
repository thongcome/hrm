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

