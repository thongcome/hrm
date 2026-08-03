BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803050414_AddProvidentFundPolicy'
)
BEGIN
    ALTER TABLE [Pay_ProvidentFundElection] ADD [ElectedByUserId] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803050414_AddProvidentFundPolicy'
)
BEGIN
    ALTER TABLE [Pay_ProvidentFundElection] ADD [ElectedDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803050414_AddProvidentFundPolicy'
)
BEGIN
    ALTER TABLE [Pay_ProvidentFundElection] ADD [InvestmentPolicyId] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803050414_AddProvidentFundPolicy'
)
BEGIN
    CREATE TABLE [Pay_ProvidentFundInvestmentPolicy] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] nvarchar(6) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [RiskDescription] nvarchar(500) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Pay_ProvidentFundInvestmentPolicy] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803050414_AddProvidentFundPolicy'
)
BEGIN
    CREATE TABLE [Pay_ProvidentFundPolicy] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] nvarchar(6) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [MinEmployeeRate] decimal(5,2) NOT NULL,
        [MaxEmployeeRate] decimal(5,2) NOT NULL,
        [MinCompanyRate] decimal(5,2) NOT NULL,
        [MaxCompanyRate] decimal(5,2) NOT NULL,
        [RateChangeLimitPerYear] int NULL,
        [IsEnabled] bit NOT NULL,
        [Note] nvarchar(1000) NULL,
        CONSTRAINT [PK_Pay_ProvidentFundPolicy] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803050414_AddProvidentFundPolicy'
)
BEGIN
    CREATE TABLE [Pay_ProvidentFundVestingTier] (
        [Id] bigint NOT NULL IDENTITY,
        [PolicyId] bigint NOT NULL,
        [MinYearsOfService] int NOT NULL,
        [MaxYearsOfService] int NULL,
        [VestingPercent] decimal(5,2) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Pay_ProvidentFundVestingTier] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pay_ProvidentFundVestingTier_Pay_ProvidentFundPolicy_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [Pay_ProvidentFundPolicy] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803050414_AddProvidentFundPolicy'
)
BEGIN
    CREATE INDEX [IX_Pay_ProvidentFundElection_InvestmentPolicyId] ON [Pay_ProvidentFundElection] ([InvestmentPolicyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803050414_AddProvidentFundPolicy'
)
BEGIN
    CREATE INDEX [IX_Pay_ProvidentFundVestingTier_PolicyId] ON [Pay_ProvidentFundVestingTier] ([PolicyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803050414_AddProvidentFundPolicy'
)
BEGIN
    ALTER TABLE [Pay_ProvidentFundElection] ADD CONSTRAINT [FK_Pay_ProvidentFundElection_Pay_ProvidentFundInvestmentPolicy_InvestmentPolicyId] FOREIGN KEY ([InvestmentPolicyId]) REFERENCES [Pay_ProvidentFundInvestmentPolicy] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803050414_AddProvidentFundPolicy'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803050414_AddProvidentFundPolicy', N'10.0.10');
END;

COMMIT;
GO

