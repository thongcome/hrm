using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedLeaveTypeIcons : Migration
    {
        // Live DB checked immediately before writing this (2026-08-30): the
        // 14 Lve_LeaveType rows seeded so far (Ids 1-14, see
        // AddLeaveTypeCatalog/SeedAdditionalLeaveTypes) all have IconName
        // NULL — this just backfills a sensible default per Code so the new
        // icon-card picker in LeaveRequestList.razor isn't all fallback
        // icons on day one. Keyed by Id (stable, unique) rather than the
        // sequential-value-overlap pattern that caused a real bug in
        // AddLeaveTypeCatalog's original remap — no such risk here.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'LocalHospital' WHERE Id = 1;");   // Sick
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'Event' WHERE Id = 2;");            // Personal
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'BeachAccess' WHERE Id = 3;");      // Vacation
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'ChildCare' WHERE Id = 4;");        // Maternity
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'LocalHospital' WHERE Id = 5;");    // Sterilization
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'MilitaryTech' WHERE Id = 6;");     // Military
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'School' WHERE Id = 7;");           // Training
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'SelfImprovement' WHERE Id = 8;");  // Ordination
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'HelpOutline' WHERE Id = 9;");      // Other
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'DirectionsCar' WHERE Id = 10;");   // SiteVisit
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'Favorite' WHERE Id = 11;");        // Marriage
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'LocalFlorist' WHERE Id = 12;");    // Bereavement
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'FamilyRestroom' WHERE Id = 13;");  // Paternity
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = 'SelfImprovement' WHERE Id = 14;"); // Hajj
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Lve_LeaveType SET IconName = NULL WHERE Id BETWEEN 1 AND 14;");
        }
    }
}
