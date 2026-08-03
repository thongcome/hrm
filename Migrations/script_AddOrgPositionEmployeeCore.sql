BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802164012_SeedOrgAdminMenu'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
        SET IDENTITY_INSERT [sc_menu] ON;
    EXEC(N'INSERT INTO [sc_menu] ([menuid], [menuname], [menuname_en], [menulevel], [isfinal], [menuorder], [menucode], [isshow], [url], [menugroupid], [isactive])
    VALUES (CAST(23 AS bigint), N''ผังองค์กร'', N''Organization'', 1, CAST(1 AS bit), 33, N''ORG_ADMIN'', CAST(1 AS bit), ''/org/organizations'', CAST(1 AS bigint), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
        SET IDENTITY_INSERT [sc_menu] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802164012_SeedOrgAdminMenu'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
        SET IDENTITY_INSERT [sc_role_menu] ON;
    EXEC(N'INSERT INTO [sc_role_menu] ([rolemenuid], [menuid], [roleid], [isactive])
    VALUES (CAST(21 AS bigint), CAST(23 AS bigint), CAST(9 AS bigint), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
        SET IDENTITY_INSERT [sc_role_menu] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802164012_SeedOrgAdminMenu'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802164012_SeedOrgAdminMenu', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    ALTER TABLE [com_organization] ADD [IsManpower] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    ALTER TABLE [com_organization] ADD [SectionTypeCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    ALTER TABLE [com_organization] ADD [SubSectionTypeId] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    ALTER TABLE [com_organization] ADD [createby] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    ALTER TABLE [com_organization] ADD [createdate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    CREATE TABLE [Com_SectionType] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] nvarchar(6) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [AbbName] nvarchar(50) NULL,
        [NameEn] nvarchar(200) NULL,
        [AbbNameEn] nvarchar(50) NULL,
        [LevelNo] int NOT NULL,
        [IsTopLevel] bit NOT NULL,
        [UpperSecType] nvarchar(20) NULL,
        [Remark] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreateDate] datetime2 NULL,
        [CreateBy] nvarchar(50) NULL,
        [ModDate] datetime2 NULL,
        [ModBy] nvarchar(50) NULL,
        CONSTRAINT [PK_Com_SectionType] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    CREATE TABLE [Com_SubSectionType] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] nvarchar(6) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Remark] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Com_SubSectionType] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    CREATE TABLE [Hrd_ContactPerson] (
        [Id] bigint NOT NULL IDENTITY,
        [HremployeeId] bigint NOT NULL,
        [Name] nvarchar(250) NULL,
        [Relation] nvarchar(200) NULL,
        [Phone] nvarchar(50) NULL,
        [Address] nvarchar(1000) NULL,
        [Remark] nvarchar(1000) NULL,
        [ModDate] datetime2 NULL,
        [ModBy] nvarchar(50) NULL,
        CONSTRAINT [PK_Hrd_ContactPerson] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    CREATE TABLE [Hrd_Education] (
        [Id] bigint NOT NULL IDENTITY,
        [HremployeeId] bigint NOT NULL,
        [Level] nvarchar(200) NULL,
        [Degree] nvarchar(200) NULL,
        [Major] nvarchar(200) NULL,
        [MajorSubject] nvarchar(200) NULL,
        [Faculty] nvarchar(200) NULL,
        [Institute] nvarchar(300) NULL,
        [Country] nvarchar(100) NULL,
        [EntryDate] date NULL,
        [FinishedDate] date NULL,
        [Gpa] decimal(5,2) NULL,
        [IsHonors] bit NOT NULL,
        [IsHighestDegree] bit NOT NULL,
        [Remark] nvarchar(1000) NULL,
        [ModDate] datetime2 NULL,
        [ModBy] nvarchar(50) NULL,
        CONSTRAINT [PK_Hrd_Education] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    CREATE TABLE [Hrd_Experience] (
        [Id] bigint NOT NULL IDENTITY,
        [HremployeeId] bigint NOT NULL,
        [StartDate] date NULL,
        [EndDate] date NULL,
        [Position] nvarchar(300) NULL,
        [Company] nvarchar(300) NULL,
        [Remark] nvarchar(1000) NULL,
        [ModDate] datetime2 NULL,
        [ModBy] nvarchar(50) NULL,
        CONSTRAINT [PK_Hrd_Experience] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    CREATE TABLE [Hrd_Family] (
        [Id] bigint NOT NULL IDENTITY,
        [HremployeeId] bigint NOT NULL,
        [CardId] nvarchar(13) NULL,
        [FamilyType] nvarchar(100) NULL,
        [PrenameCode] nvarchar(20) NULL,
        [FirstName] nvarchar(250) NULL,
        [LastName] nvarchar(250) NULL,
        [BirthDate] date NULL,
        [Nationality] nvarchar(100) NULL,
        [Religion] nvarchar(100) NULL,
        [IsAlive] bit NOT NULL,
        [DeceasedDate] date NULL,
        [Career] nvarchar(200) NULL,
        [IsChild] bit NOT NULL,
        [ChildStudying] bit NOT NULL,
        [ChildDeductionEligible] bit NOT NULL,
        [EduRefundEligible] bit NOT NULL,
        [Address] nvarchar(1000) NULL,
        [ModDate] datetime2 NULL,
        [ModBy] nvarchar(50) NULL,
        CONSTRAINT [PK_Hrd_Family] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    CREATE TABLE [Hrd_Marriage] (
        [Id] bigint NOT NULL IDENTITY,
        [HremployeeId] bigint NOT NULL,
        [MarriageNo] nvarchar(50) NULL,
        [MarriedAt] nvarchar(200) NULL,
        [RegisterDate] date NULL,
        [BookNo] nvarchar(50) NULL,
        [SpouseName] nvarchar(300) NULL,
        [SpouseAge] int NULL,
        [SpouseCareer] nvarchar(200) NULL,
        [SpouseIncome] decimal(15,2) NULL,
        [ChildCount] int NULL,
        [ChildStudyingCount] int NULL,
        [ChildNotStudyingCount] int NULL,
        [Remark] nvarchar(1000) NULL,
        [ModDate] datetime2 NULL,
        [ModBy] nvarchar(50) NULL,
        CONSTRAINT [PK_Hrd_Marriage] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    CREATE TABLE [Hrd_NameChangeHistory] (
        [Id] bigint NOT NULL IDENTITY,
        [HremployeeId] bigint NOT NULL,
        [OldPrenameCode] nvarchar(20) NULL,
        [OldFirstName] nvarchar(250) NULL,
        [OldLastName] nvarchar(250) NULL,
        [NewPrenameCode] nvarchar(20) NULL,
        [NewFirstName] nvarchar(250) NULL,
        [NewLastName] nvarchar(250) NULL,
        [DocNo] nvarchar(100) NULL,
        [DocDate] date NULL,
        [IssuedBy] nvarchar(300) NULL,
        [RegisterDate] date NULL,
        [Remark] nvarchar(1000) NULL,
        [ModDate] datetime2 NULL,
        [ModBy] nvarchar(50) NULL,
        CONSTRAINT [PK_Hrd_NameChangeHistory] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    CREATE TABLE [Pos_EmployeeType] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] nvarchar(6) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Remark] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Pos_EmployeeType] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    CREATE TABLE [Pos_ExecType] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] nvarchar(6) NOT NULL,
        [EmployeeTypeId] bigint NULL,
        [Code] nvarchar(20) NULL,
        [Name] nvarchar(500) NOT NULL,
        [AbbName] nvarchar(200) NULL,
        [NameEn] nvarchar(500) NULL,
        [AbbNameEn] nvarchar(200) NULL,
        [IsTopLevel] bit NOT NULL,
        [IsBoss] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [StartDate] date NULL,
        [EndDate] date NULL,
        [Remark] nvarchar(1000) NULL,
        [CreateDate] datetime2 NULL,
        [CreateBy] nvarchar(50) NULL,
        CONSTRAINT [PK_Pos_ExecType] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    CREATE TABLE [Pos_PositionSlot] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] nvarchar(6) NOT NULL,
        [PosCode] nvarchar(20) NULL,
        [PosExecTypeId] bigint NULL,
        [OrganizationId] bigint NULL,
        [EmployeeTypeId] bigint NULL,
        [Name] nvarchar(500) NULL,
        [AbbName] nvarchar(200) NULL,
        [StartDate] date NULL,
        [ExpireDate] date NULL,
        [MinGraduate] nvarchar(200) NULL,
        [MinSalary] decimal(15,2) NULL,
        [NormalSalary] decimal(15,2) NULL,
        [MaxSalary] decimal(15,2) NULL,
        [IsManpower] bit NOT NULL,
        [IsBoss] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [HremployeeId] bigint NULL,
        [PaymentNo] nvarchar(50) NULL,
        [Remark] nvarchar(2000) NULL,
        [CreateDate] datetime2 NULL,
        [CreateBy] nvarchar(50) NULL,
        CONSTRAINT [PK_Pos_PositionSlot] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802220443_AddOrgPositionEmployeeCore'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802220443_AddOrgPositionEmployeeCore', N'10.0.10');
END;

COMMIT;
GO

