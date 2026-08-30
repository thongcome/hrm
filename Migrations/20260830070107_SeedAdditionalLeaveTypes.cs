using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdditionalLeaveTypes : Migration
    {
        // Live DB checked immediately before writing this (2026-08-30):
        // MAX(Id) on Lve_LeaveType was 10 (9 original seed rows + "SiteVisit"
        // added live through the admin UI while verifying that feature) —
        // starting these 4 new rows at 11. None are Labor Protection Act
        // mandated (IsStatutory=false) — all are common Thai company
        // benefits, same category as the existing Ordination row, added at
        // the user's request. ApplicableCountryCode left null (unlike
        // Ordination's "TH") since marriage/bereavement/paternity/Hajj leave
        // aren't Thailand-specific customs — a company anywhere could offer
        // them; per-company opt-in via Lve_LeavePolicy is what actually
        // gates visibility.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Lve_LeaveType",
                columns: new[] { "Id", "Code", "NameTh", "NameEn", "IsStatutory", "LawReference", "StatutoryMinDaysPerYear", "ApplicableGender", "ApplicableCountryCode", "RequiresMedicalCert", "AllowHalfDay", "SortOrder", "IsActive" },
                values: new object[,]
                {
                    { 11, "Marriage", "ลากิจสมรส", "Marriage Leave", false, null, null, null, null, false, true, 10, true },
                    { 12, "Bereavement", "ลางานศพ/ลากิจฌาปนกิจ", "Bereavement Leave", false, null, null, null, null, false, true, 11, true },
                    { 13, "Paternity", "ลาไปช่วยภรรยาคลอดบุตร", "Paternity Leave", false, null, null, "M", null, false, false, 12, true },
                    { 14, "Hajj", "ลาประกอบพิธีฮัจย์", "Hajj Pilgrimage Leave", false, null, null, null, null, false, false, 13, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Lve_LeaveType", keyColumn: "Id", keyValue: 11);
            migrationBuilder.DeleteData(table: "Lve_LeaveType", keyColumn: "Id", keyValue: 12);
            migrationBuilder.DeleteData(table: "Lve_LeaveType", keyColumn: "Id", keyValue: 13);
            migrationBuilder.DeleteData(table: "Lve_LeaveType", keyColumn: "Id", keyValue: 14);
        }
    }
}
