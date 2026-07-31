BEGIN TRANSACTION;
ALTER TABLE [HREMPLOYEE] ADD [OrganizationId] bigint NULL;

ALTER TABLE [HREMPLOYEE] ADD [orgcode] nvarchar(50) NULL;

ALTER TABLE [HREMPLOYEE] ADD [orgcodefull] nvarchar(100) NULL;

ALTER TABLE [com_organization] ADD [orgcodefull] nvarchar(100) NULL;
GO

;WITH OrgTree AS (
    SELECT id, code, parent_code,
           CAST(RIGHT('00' + CAST(ROW_NUMBER() OVER (PARTITION BY parent_code ORDER BY id) AS varchar(2)), 2) AS varchar(100)) AS orgcodefull
    FROM com_organization
    WHERE istop = 1
    UNION ALL
    SELECT c.id, c.code, c.parent_code,
           CAST(t.orgcodefull + RIGHT('00' + CAST(ROW_NUMBER() OVER (PARTITION BY c.parent_code ORDER BY c.id) AS varchar(2)), 2) AS varchar(100))
    FROM com_organization c
    JOIN OrgTree t ON c.parent_code = t.code
)
UPDATE o SET o.orgcodefull = t.orgcodefull
FROM com_organization o
JOIN OrgTree t ON o.id = t.id;


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260731163212_AddOrgHierarchyLinkage', N'10.0.10');

COMMIT;
GO

