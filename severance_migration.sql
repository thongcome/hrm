BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730143216_SeedSeverancePayItemType'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'DefaultSignFlag', N'GLAccountCode', N'IsActive', N'IsSystemReserved', N'NameEn', N'NameTh', N'SortOrder') AND [object_id] = OBJECT_ID(N'[Pay_PayItemType]'))
        SET IDENTITY_INSERT [Pay_PayItemType] ON;
    EXEC(N'INSERT INTO [Pay_PayItemType] ([Id], [Category], [Code], [DefaultSignFlag], [GLAccountCode], [IsActive], [IsSystemReserved], [NameEn], [NameTh], [SortOrder])
    VALUES (11, 0, N''SEVERANCE'', 1, N''5040-SEVERANCE'', CAST(1 AS bit), CAST(1 AS bit), N''Statutory Severance Pay'', N''ค่าชดเชยตามกฎหมาย'', 11)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'DefaultSignFlag', N'GLAccountCode', N'IsActive', N'IsSystemReserved', N'NameEn', N'NameTh', N'SortOrder') AND [object_id] = OBJECT_ID(N'[Pay_PayItemType]'))
        SET IDENTITY_INSERT [Pay_PayItemType] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730143216_SeedSeverancePayItemType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730143216_SeedSeverancePayItemType', N'10.0.10');
END;

COMMIT;
GO

