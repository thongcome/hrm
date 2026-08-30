using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveTypeCatalog : Migration
    {
        // Live DB checked immediately before writing this (2026-08-30):
        // Lve_LeaveRequest has 5 rows (all LeaveType=0/Sick), Lve_LeavePolicy
        // has 6 rows (one each of the old enum values 0,1,2,3,4,9) — real
        // test data from exercising the Leave module, not an empty table.
        // Order matters: create+seed the catalog first (Ids 1-9 below), THEN
        // rename the old int LeaveType column to LeaveTypeId (RenameColumn
        // preserves the old int values as-is), THEN remap those old enum
        // ints to the new catalog Ids, and only THEN add the FK — adding the
        // FK before the remap would fail immediately (SQL Server validates
        // FK constraints against existing data, and 0/1/2/3/4/9 don't exist
        // as Lve_LeaveType.Id at that point).
        private static readonly (int OldEnumValue, int NewTypeId)[] EnumToTypeIdMap =
        [
            (0, 1), // Sick
            (1, 2), // Personal
            (2, 3), // Vacation
            (3, 4), // Maternity
            (4, 8), // Ordination
            (9, 9), // Other
        ];

        private static string BuildRemapSql(string table, (int OldEnumValue, int NewTypeId)[] map)
        {
            var sb = new StringBuilder();
            sb.Append($"UPDATE {table} SET LeaveTypeId = CASE LeaveTypeId ");
            foreach (var (oldValue, newTypeId) in map)
                sb.Append($"WHEN {oldValue} THEN {newTypeId} ");
            sb.Append("ELSE LeaveTypeId END;");
            return sb.ToString();
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lve_LeaveType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameTh = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsStatutory = table.Column<bool>(type: "bit", nullable: false),
                    LawReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StatutoryMinDaysPerYear = table.Column<decimal>(type: "decimal(5,1)", nullable: true),
                    ApplicableGender = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    ApplicableCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    RequiresMedicalCert = table.Column<bool>(type: "bit", nullable: false),
                    AllowHalfDay = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lve_LeaveType", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lve_LeaveType_Code",
                table: "Lve_LeaveType",
                column: "Code",
                unique: true);

            migrationBuilder.InsertData(
                table: "Lve_LeaveType",
                columns: new[] { "Id", "Code", "NameTh", "NameEn", "IsStatutory", "LawReference", "StatutoryMinDaysPerYear", "ApplicableGender", "ApplicableCountryCode", "RequiresMedicalCert", "AllowHalfDay", "SortOrder", "IsActive" },
                values: new object[,]
                {
                    { 1, "Sick", "ลาป่วย", "Sick Leave", true, "มาตรา 32", 30m, null, null, true, true, 1, true },
                    { 2, "Personal", "ลากิจธุระจำเป็น", "Personal Leave", true, "มาตรา 34/1", 3m, null, null, false, true, 2, true },
                    { 3, "Vacation", "ลาพักร้อน", "Vacation Leave", true, "มาตรา 30", 6m, null, null, false, true, 3, true },
                    { 4, "Maternity", "ลาคลอดบุตร", "Maternity Leave", true, "มาตรา 41", 98m, "F", null, false, true, 4, true },
                    { 5, "Sterilization", "ลาทำหมัน", "Sterilization Leave", true, "มาตรา 33", null, null, null, true, true, 5, true },
                    { 6, "Military", "ลาเพื่อรับราชการทหาร", "Military Service Leave", true, "มาตรา 35", 60m, null, null, false, true, 6, true },
                    { 7, "Training", "ลาเพื่อฝึกอบรม/พัฒนาความรู้", "Training Leave", true, "มาตรา 36", null, null, null, false, true, 7, true },
                    { 8, "Ordination", "ลาบวช", "Ordination Leave", false, null, null, null, "TH", false, true, 8, true },
                    { 9, "Other", "อื่นๆ", "Other", false, null, null, null, null, false, true, 9, true },
                });

            migrationBuilder.RenameColumn(
                name: "LeaveType",
                table: "Lve_LeaveRequest",
                newName: "LeaveTypeId");

            migrationBuilder.RenameColumn(
                name: "LeaveType",
                table: "Lve_LeavePolicy",
                newName: "LeaveTypeId");

            // A single CASE-based UPDATE, not a loop of per-value UPDATEs —
            // the old enum values (0-4,9) and new catalog Ids (1-9) overlap
            // in range, so sequential single-value UPDATEs would cascade
            // (e.g. 0->1 runs, then the very next step's 1->2 catches those
            // just-updated rows too, and so on). CASE evaluates the OLD
            // value for every row in one pass, so there's no interim state
            // for a later branch to accidentally re-match.
            migrationBuilder.Sql(BuildRemapSql("Lve_LeaveRequest", EnumToTypeIdMap));
            migrationBuilder.Sql(BuildRemapSql("Lve_LeavePolicy", EnumToTypeIdMap));

            migrationBuilder.CreateIndex(
                name: "IX_Lve_LeaveRequest_LeaveTypeId",
                table: "Lve_LeaveRequest",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Lve_LeavePolicy_LeaveTypeId",
                table: "Lve_LeavePolicy",
                column: "LeaveTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lve_LeavePolicy_Lve_LeaveType_LeaveTypeId",
                table: "Lve_LeavePolicy",
                column: "LeaveTypeId",
                principalTable: "Lve_LeaveType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lve_LeaveRequest_Lve_LeaveType_LeaveTypeId",
                table: "Lve_LeaveRequest",
                column: "LeaveTypeId",
                principalTable: "Lve_LeaveType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lve_LeavePolicy_Lve_LeaveType_LeaveTypeId",
                table: "Lve_LeavePolicy");

            migrationBuilder.DropForeignKey(
                name: "FK_Lve_LeaveRequest_Lve_LeaveType_LeaveTypeId",
                table: "Lve_LeaveRequest");

            migrationBuilder.DropIndex(
                name: "IX_Lve_LeaveRequest_LeaveTypeId",
                table: "Lve_LeaveRequest");

            migrationBuilder.DropIndex(
                name: "IX_Lve_LeavePolicy_LeaveTypeId",
                table: "Lve_LeavePolicy");

            var reverseMap = EnumToTypeIdMap.Select(m => (OldEnumValue: m.NewTypeId, NewTypeId: m.OldEnumValue)).ToArray();
            migrationBuilder.Sql(BuildRemapSql("Lve_LeaveRequest", reverseMap));
            migrationBuilder.Sql(BuildRemapSql("Lve_LeavePolicy", reverseMap));

            migrationBuilder.RenameColumn(
                name: "LeaveTypeId",
                table: "Lve_LeaveRequest",
                newName: "LeaveType");

            migrationBuilder.RenameColumn(
                name: "LeaveTypeId",
                table: "Lve_LeavePolicy",
                newName: "LeaveType");

            migrationBuilder.DropTable(
                name: "Lve_LeaveType");
        }
    }
}
