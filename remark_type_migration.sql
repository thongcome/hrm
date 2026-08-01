BEGIN TRANSACTION;
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[wf_workflow]') AND [c].[name] = N'remark');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [wf_workflow] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [wf_workflow] ALTER COLUMN [remark] nvarchar(500) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260801011127_ChangeWfWorkflowRemarkToText', N'10.0.10');

COMMIT;
GO

