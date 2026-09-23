using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // Comp_ProficiencyLevel gets the same effective-dated master-data shape as
    // Pay_MinimumWage/Att_OtPolicy/etc (EffectiveFrom/EffectiveTo/IsActive) — a real gap found
    // against SHRM/CIPD competency-management standards (23 ก.ย. 2569): BARS anchor descriptions
    // had no versioning, so revising what "level 3" means silently rewrote history. See
    // Model/Comp_ProficiencyLevel.cs and Services/Shared/ProficiencyLevelResolver.cs.
    public partial class CompetencyLevelEffectiveDating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Comp_ProficiencyLevel', 'EffectiveFrom') IS NULL
    ALTER TABLE Comp_ProficiencyLevel ADD EffectiveFrom date NULL;
IF COL_LENGTH('Comp_ProficiencyLevel', 'EffectiveTo') IS NULL
    ALTER TABLE Comp_ProficiencyLevel ADD EffectiveTo date NULL;
IF COL_LENGTH('Comp_ProficiencyLevel', 'IsActive') IS NULL
    ALTER TABLE Comp_ProficiencyLevel ADD IsActive bit NULL;
");
            // existing rows: every row already in the table has been "in force" since before
            // this feature existed — backfill to a fixed epoch well before any real evaluation
            // data, not today's date, so nothing looks like it was just created
            migrationBuilder.Sql(@"
EXEC(N'
UPDATE Comp_ProficiencyLevel SET EffectiveFrom = ''2020-01-01'' WHERE EffectiveFrom IS NULL;
UPDATE Comp_ProficiencyLevel SET IsActive = 1 WHERE IsActive IS NULL;
ALTER TABLE Comp_ProficiencyLevel ALTER COLUMN EffectiveFrom date NOT NULL;
ALTER TABLE Comp_ProficiencyLevel ALTER COLUMN IsActive bit NOT NULL;
');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Comp_ProficiencyLevel_CompetencyId_Level_IsActive' AND object_id = OBJECT_ID('Comp_ProficiencyLevel'))
    CREATE INDEX IX_Comp_ProficiencyLevel_CompetencyId_Level_IsActive ON Comp_ProficiencyLevel (CompetencyId, Level, IsActive);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Comp_ProficiencyLevel_CompetencyId_Level_IsActive' AND object_id = OBJECT_ID('Comp_ProficiencyLevel'))
    DROP INDEX IX_Comp_ProficiencyLevel_CompetencyId_Level_IsActive ON Comp_ProficiencyLevel;
IF COL_LENGTH('Comp_ProficiencyLevel', 'EffectiveFrom') IS NOT NULL ALTER TABLE Comp_ProficiencyLevel DROP COLUMN EffectiveFrom;
IF COL_LENGTH('Comp_ProficiencyLevel', 'EffectiveTo') IS NOT NULL ALTER TABLE Comp_ProficiencyLevel DROP COLUMN EffectiveTo;
IF COL_LENGTH('Comp_ProficiencyLevel', 'IsActive') IS NOT NULL ALTER TABLE Comp_ProficiencyLevel DROP COLUMN IsActive;
");
        }
    }
}
