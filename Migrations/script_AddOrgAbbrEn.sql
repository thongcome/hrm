BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803022546_AddOrgAbbrEn'
)
BEGIN
    ALTER TABLE [com_organization] ADD [abbr_en] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803022546_AddOrgAbbrEn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803022546_AddOrgAbbrEn', N'10.0.10');
END;

COMMIT;
GO

