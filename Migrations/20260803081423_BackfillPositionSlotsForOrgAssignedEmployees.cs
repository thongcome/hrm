using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class BackfillPositionSlotsForOrgAssignedEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill for the 2 employees (HREMPLOYEE.ID=1 org=6, ID=4
            // org=2, both companyid='001') whose org linkage was previously
            // set only via the now-removed direct picker on
            // PayrollEmployeeAdmin.razor. Pos_PositionSlot is now the single
            // source of truth (see Services/Shared/EmployeePositionSync.cs)
            // — without this row, /employee would show them as unlinked.
            // Values re-verified live against the DB immediately before
            // writing this migration (not reused from memory). Name/
            // PosExecTypeId left null — no reliable source to backfill a
            // title from; Remark flags these for HR follow-up. Id omitted
            // (Pos_PositionSlot.Id is a real IDENTITY column, confirmed via
            // sys.columns.is_identity) so SQL Server auto-assigns it.
            migrationBuilder.InsertData(
                table: "Pos_PositionSlot",
                columns: new[] { "CompanyId", "OrganizationId", "HremployeeId", "IsActive", "IsManpower", "IsBoss", "CreateDate", "Remark" },
                values: new object[,]
                {
                    { "001", 6L, 1L, true, true, false, new DateTime(2026, 8, 3), "Backfilled from legacy Hremployee.OrganizationId picker (data-integrity fix) — title/PosExecTypeId not migrated, no reliable source; HR to fill in via /pos/position-slots" },
                    { "001", 2L, 4L, true, true, false, new DateTime(2026, 8, 3), "Backfilled from legacy Hremployee.OrganizationId picker (data-integrity fix) — title/PosExecTypeId not migrated, no reliable source; HR to fill in via /pos/position-slots" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deletes by matching HremployeeId — safer than assuming
            // specific Ids since Id was left to auto-assign on Up.
            migrationBuilder.Sql("DELETE FROM [Pos_PositionSlot] WHERE [HremployeeId] IN (1, 4) AND [Remark] LIKE 'Backfilled from legacy%';");
        }
    }
}
