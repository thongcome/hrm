BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802021117_AddEmployeeInsurance'
)
BEGIN
    ALTER TABLE [Pay_PayrollEmployee] ADD [InsuranceCompanyAmount] decimal(15,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802021117_AddEmployeeInsurance'
)
BEGIN
    ALTER TABLE [Pay_PayrollEmployee] ADD [InsuranceEmployeeAmount] decimal(15,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802021117_AddEmployeeInsurance'
)
BEGIN
    CREATE TABLE [Pay_InsurancePlan] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] nvarchar(6) NOT NULL,
        [PlanCode] nvarchar(20) NOT NULL,
        [PlanName] nvarchar(200) NOT NULL,
        [PlanType] int NOT NULL,
        [Provider] nvarchar(200) NULL,
        [CoverageAmount] decimal(15,2) NULL,
        [DefaultEmployeeAmount] decimal(15,2) NOT NULL,
        [DefaultCompanyAmount] decimal(15,2) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Pay_InsurancePlan] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802021117_AddEmployeeInsurance'
)
BEGIN
    CREATE TABLE [Pay_EmployeeInsuranceEnrollment] (
        [Id] bigint NOT NULL IDENTITY,
        [HremployeeId] bigint NOT NULL,
        [PlanId] bigint NOT NULL,
        [EmployeeAmount] decimal(15,2) NOT NULL,
        [CompanyAmount] decimal(15,2) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [IsActive] bit NOT NULL,
        [EnrolledByUserId] bigint NOT NULL,
        [EnrolledDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Pay_EmployeeInsuranceEnrollment] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pay_EmployeeInsuranceEnrollment_HREMPLOYEE_HremployeeId] FOREIGN KEY ([HremployeeId]) REFERENCES [HREMPLOYEE] ([ID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pay_EmployeeInsuranceEnrollment_Pay_InsurancePlan_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [Pay_InsurancePlan] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802021117_AddEmployeeInsurance'
)
BEGIN
    CREATE INDEX [IX_Pay_EmployeeInsuranceEnrollment_HremployeeId] ON [Pay_EmployeeInsuranceEnrollment] ([HremployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802021117_AddEmployeeInsurance'
)
BEGIN
    CREATE INDEX [IX_Pay_EmployeeInsuranceEnrollment_PlanId] ON [Pay_EmployeeInsuranceEnrollment] ([PlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802021117_AddEmployeeInsurance'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802021117_AddEmployeeInsurance', N'10.0.10');
END;

COMMIT;
GO

