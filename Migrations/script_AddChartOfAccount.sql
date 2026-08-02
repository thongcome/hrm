BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802163022_AddChartOfAccount'
)
BEGIN
    CREATE TABLE [Com_ChartOfAccount] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] nvarchar(6) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [NameTh] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NULL,
        [AccountType] int NOT NULL,
        [IsCostCenter] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Com_ChartOfAccount] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802163022_AddChartOfAccount'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802163022_AddChartOfAccount', N'10.0.10');
END;

COMMIT;
GO

