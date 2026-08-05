BEGIN TRANSACTION;
ALTER TABLE [Pay_PayslipSettings] ADD [DefaultProbationDays] int NOT NULL DEFAULT 0;

ALTER TABLE [HREMPLOYEE] ADD [PROBATION_CONFIRMED_DATE] DATE NULL;

ALTER TABLE [HREMPLOYEE] ADD [PROBATION_END_DATE] DATE NULL;

CREATE TABLE [Hrd_ExitInterview] (
    [Id] bigint NOT NULL IDENTITY,
    [HremployeeId] bigint NOT NULL,
    [InterviewDate] datetime2 NOT NULL,
    [ConductedByUserId] bigint NOT NULL,
    [ReasonCode] int NOT NULL,
    [ReasonNote] nvarchar(500) NULL,
    [WouldRecommendCompany] bit NULL,
    [Feedback] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedByUserId] bigint NOT NULL,
    CONSTRAINT [PK_Hrd_ExitInterview] PRIMARY KEY ([Id])
);

CREATE TABLE [Hrd_LifecycleTaskInstance] (
    [Id] bigint NOT NULL IDENTITY,
    [HremployeeId] bigint NOT NULL,
    [Direction] int NOT NULL,
    [TemplateId] bigint NULL,
    [Title] nvarchar(250) NOT NULL,
    [Status] int NOT NULL,
    [DueDate] datetime2 NULL,
    [CompletedDate] datetime2 NULL,
    [CompletedByUserId] bigint NULL,
    [Note] nvarchar(1000) NULL,
    [AssetDescription] nvarchar(250) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedByUserId] bigint NOT NULL,
    CONSTRAINT [PK_Hrd_LifecycleTaskInstance] PRIMARY KEY ([Id])
);

CREATE TABLE [Hrd_LifecycleTaskTemplate] (
    [Id] bigint NOT NULL IDENTITY,
    [CompanyId] nvarchar(6) NOT NULL,
    [Direction] int NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Title] nvarchar(250) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [SortOrder] int NOT NULL,
    [DefaultAssigneeRole] nvarchar(100) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Hrd_LifecycleTaskTemplate] PRIMARY KEY ([Id])
);

UPDATE [Pay_PayslipSettings] SET [DefaultProbationDays] = 119
WHERE [Id] = CAST(1 AS bigint);
SELECT @@ROWCOUNT;


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260805172138_AddOnboardingOffboardingLifecycle', N'10.0.10');

COMMIT;
GO

