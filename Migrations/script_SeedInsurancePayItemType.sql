BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802021329_SeedInsurancePayItemType'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'DefaultSignFlag', N'GLAccountCode', N'IsActive', N'IsSystemReserved', N'NameEn', N'NameTh', N'SortOrder') AND [object_id] = OBJECT_ID(N'[Pay_PayItemType]'))
        SET IDENTITY_INSERT [Pay_PayItemType] ON;
    EXEC(N'INSERT INTO [Pay_PayItemType] ([Id], [Category], [Code], [DefaultSignFlag], [GLAccountCode], [IsActive], [IsSystemReserved], [NameEn], [NameTh], [SortOrder])
    VALUES (12, 1, N''INSURANCE'', -1, N''2240-INS-PAYABLE'', CAST(1 AS bit), CAST(1 AS bit), N''Group Insurance Deduction'', N''หักเบี้ยประกันกลุ่ม'', 12)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'DefaultSignFlag', N'GLAccountCode', N'IsActive', N'IsSystemReserved', N'NameEn', N'NameTh', N'SortOrder') AND [object_id] = OBJECT_ID(N'[Pay_PayItemType]'))
        SET IDENTITY_INSERT [Pay_PayItemType] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802021329_SeedInsurancePayItemType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802021329_SeedInsurancePayItemType', N'10.0.10');
END;

COMMIT;
GO

