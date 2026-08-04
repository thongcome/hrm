using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class addEmp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration was never actually applied even though many later ones were
            // (found 2026-08-04 while adding an unrelated insurance migration — EF refuses
            // to skip ahead of pending migrations). Rewritten as guarded raw SQL instead of
            // AlterColumn so it doesn't fail if IX_sc_user is already missing (real drift:
            // the index no longer exists in this DB, for reasons unrelated to this migration)
            // — recreates it unconditionally so the DB matches what AddColumn <IX_sc_user>
            // originally intended. sc_user.password itself already matches nvarchar(500) in
            // this DB; this is idempotent either way.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sc_user' AND object_id = OBJECT_ID('sc_user'))
    DROP INDEX [IX_sc_user] ON [sc_user];

DECLARE @constraintName nvarchar(max);
SELECT @constraintName = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[sc_user]') AND [c].[name] = N'password');
IF @constraintName IS NOT NULL EXEC(N'ALTER TABLE [sc_user] DROP CONSTRAINT ' + @constraintName + ';');

ALTER TABLE [sc_user] ALTER COLUMN [password] nvarchar(500) NULL;
ALTER TABLE [sc_user] ADD DEFAULT ((NULL)) FOR [password];

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sc_user' AND object_id = OBJECT_ID('sc_user'))
    CREATE INDEX [IX_sc_user] ON [sc_user] ([userid], [password], [isEmployee]);
");

            migrationBuilder.AlterColumn<string>(
                name: "NameEn",
                table: "employee",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            // HRPayrollPayByRequest already exists in this DB (created some other way before
            // this migration's history row existed) — skip re-creating it, unlike the other
            // two operations above there's no safe idempotent CREATE TABLE IF NOT EXISTS
            // equivalent worth hand-rolling here since existence was already confirmed.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sc_user' AND object_id = OBJECT_ID('sc_user'))
    DROP INDEX [IX_sc_user] ON [sc_user];

DECLARE @constraintName nvarchar(max);
SELECT @constraintName = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[sc_user]') AND [c].[name] = N'password');
IF @constraintName IS NOT NULL EXEC(N'ALTER TABLE [sc_user] DROP CONSTRAINT ' + @constraintName + ';');

ALTER TABLE [sc_user] ALTER COLUMN [password] nvarchar(250) NULL;
ALTER TABLE [sc_user] ADD DEFAULT ((NULL)) FOR [password];

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sc_user' AND object_id = OBJECT_ID('sc_user'))
    CREATE INDEX [IX_sc_user] ON [sc_user] ([userid], [password], [isEmployee]);
");

            migrationBuilder.AlterColumn<string>(
                name: "NameEn",
                table: "employee",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);
        }
    }
}
