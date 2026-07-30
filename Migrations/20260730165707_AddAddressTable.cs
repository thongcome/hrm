using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "address",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    hremployeeid = table.Column<long>(type: "bigint", nullable: false),
                    address_type_id = table.Column<long>(type: "bigint", nullable: true),
                    no = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    road = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    soi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    buildingname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    village = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    subdistrict = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    districtid = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    provinceid = table.Column<long>(type: "bigint", nullable: true),
                    province = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    postcode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    tel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    mobileno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    officeno = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true),
                    fax = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true),
                    email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    createby = table.Column<long>(type: "bigint", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<long>(type: "bigint", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_address", x => x.id);
                    table.ForeignKey(
                        name: "FK_address_HREMPLOYEE",
                        column: x => x.hremployeeid,
                        principalTable: "HREMPLOYEE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_address_mas_address_type",
                        column: x => x.address_type_id,
                        principalTable: "mas_address_type",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_address_mas_province",
                        column: x => x.provinceid,
                        principalTable: "mas_province",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_address_address_type_id",
                table: "address",
                column: "address_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_address_hremployeeid",
                table: "address",
                column: "hremployeeid");

            migrationBuilder.CreateIndex(
                name: "IX_address_provinceid",
                table: "address",
                column: "provinceid");

            migrationBuilder.CreateIndex(
                name: "IX_address_hremployeeid_address_type_id",
                table: "address",
                columns: new[] { "hremployeeid", "address_type_id" });

            // mas_address_type was completely empty (0 rows) before this —
            // free to define fresh codes with no legacy data to conform to.
            migrationBuilder.InsertData(
                table: "mas_address_type",
                columns: new[] { "id", "name", "name_en", "code", "isActive" },
                values: new object[,]
                {
                    { 1L, "ที่อยู่ตามทะเบียนบ้าน", "Registered Address", "REG", true },
                    { 2L, "ที่อยู่ปัจจุบัน/ติดต่อได้", "Current/Contact Address", "CUR", true },
                });

            // One-time data copy: existing Hremployee.ADR_*/ADN_* flat columns
            // -> normalized address rows. ADR_* (registered) and ADN_*
            // (current/contact) were never documented as which-is-which in
            // code — this mapping is the most defensible reading of the
            // naming convention (ADR = address-registered, ADN = address-now)
            // and was confirmed with the user. Existing flat columns are left
            // in place (not dropped) — the actively-used PayrollEmployeeAdmin.razor
            // never touched them; only the legacy scaffolded HremployeePages/
            // CRUD pages do, and migrating those is a separate task.
            migrationBuilder.Sql(@"
                INSERT INTO [address] (hremployeeid, address_type_id, [no], subdistrict, districtid, province, postcode, createdate, moddate, isactive)
                SELECT [ID], 1, ADR_NO, ADR_TAMBOL, ADR_AMPHUR, ADR_PROVINCE, ADR_POSTCODE, GETDATE(), GETDATE(), 1
                FROM HREMPLOYEE
                WHERE ADR_NO IS NOT NULL;

                INSERT INTO [address] (hremployeeid, address_type_id, [no], subdistrict, districtid, province, postcode, tel, email, createdate, moddate, isactive)
                SELECT [ID], 2, ADN_NO, ADN_TAMBOL, ADN_AMPHUR, ADN_PROVINCE, ADN_POSTCODE, ADN_TEL, ADN_EMAIL, GETDATE(), GETDATE(), 1
                FROM HREMPLOYEE
                WHERE ADN_NO IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "mas_address_type", keyColumn: "id", keyValues: new object[] { 1L, 2L });

            migrationBuilder.DropTable(
                name: "address");
        }
    }
}
