BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803022042_RemoveDuplicateIsManpowerColumn'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[com_organization]') AND [c].[name] = N'IsManpower');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [com_organization] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [com_organization] DROP COLUMN [IsManpower];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803022042_RemoveDuplicateIsManpowerColumn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803022042_RemoveDuplicateIsManpowerColumn', N'10.0.10');
END;

COMMIT;
GO

