BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801105744_AddWorkflowDesignTable'
)
BEGIN
    ALTER TABLE [wf_loa_user] ADD CONSTRAINT [PK_wf_loa_user] PRIMARY KEY ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801105744_AddWorkflowDesignTable'
)
BEGIN
    CREATE TABLE [WorkflowDesignTable] (
        [Id] bigint NOT NULL IDENTITY,
        [TableName] nvarchar(128) NOT NULL,
        [FieldsJson] nvarchar(max) NOT NULL,
        [CreatedByUserId] bigint NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_WorkflowDesignTable] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801105744_AddWorkflowDesignTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260801105744_AddWorkflowDesignTable', N'10.0.10');
END;

COMMIT;
GO

