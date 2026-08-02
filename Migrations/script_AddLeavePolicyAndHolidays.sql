BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802091442_AddLeavePolicyAndHolidays'
)
BEGIN
    CREATE TABLE [Lve_CompanyHoliday] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] nvarchar(6) NOT NULL,
        [HolidayDate] date NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Lve_CompanyHoliday] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802091442_AddLeavePolicyAndHolidays'
)
BEGIN
    CREATE TABLE [Lve_LeavePolicy] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] nvarchar(6) NOT NULL,
        [LeaveType] int NOT NULL,
        [EntitlementDaysPerYear] decimal(5,1) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Lve_LeavePolicy] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802091442_AddLeavePolicyAndHolidays'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CompanyId', N'LeaveType', N'EntitlementDaysPerYear', N'IsActive') AND [object_id] = OBJECT_ID(N'[Lve_LeavePolicy]'))
        SET IDENTITY_INSERT [Lve_LeavePolicy] ON;
    EXEC(N'INSERT INTO [Lve_LeavePolicy] ([Id], [CompanyId], [LeaveType], [EntitlementDaysPerYear], [IsActive])
    VALUES (CAST(1 AS bigint), N''001'', 0, 30.0, CAST(1 AS bit)),
    (CAST(2 AS bigint), N''001'', 1, 3.0, CAST(1 AS bit)),
    (CAST(3 AS bigint), N''001'', 2, 6.0, CAST(1 AS bit)),
    (CAST(4 AS bigint), N''001'', 3, 98.0, CAST(1 AS bit)),
    (CAST(5 AS bigint), N''001'', 4, 0.0, CAST(1 AS bit)),
    (CAST(6 AS bigint), N''001'', 9, 0.0, CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CompanyId', N'LeaveType', N'EntitlementDaysPerYear', N'IsActive') AND [object_id] = OBJECT_ID(N'[Lve_LeavePolicy]'))
        SET IDENTITY_INSERT [Lve_LeavePolicy] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802091442_AddLeavePolicyAndHolidays'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802091442_AddLeavePolicyAndHolidays', N'10.0.10');
END;

COMMIT;
GO

