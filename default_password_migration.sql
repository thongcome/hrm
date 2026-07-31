BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731111656_AddDefaultPasswordParts'
)
BEGIN
    ALTER TABLE [Pay_PayslipSettings] ADD [DefaultPasswordPart1] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731111656_AddDefaultPasswordParts'
)
BEGIN
    ALTER TABLE [Pay_PayslipSettings] ADD [DefaultPasswordPart2] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731111656_AddDefaultPasswordParts'
)
BEGIN
    EXEC(N'UPDATE [Pay_PayslipSettings] SET [DefaultPasswordPart1] = NULL, [DefaultPasswordPart2] = NULL
    WHERE [Id] = CAST(1 AS bigint);
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731111656_AddDefaultPasswordParts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731111656_AddDefaultPasswordParts', N'10.0.10');
END;

COMMIT;
GO

