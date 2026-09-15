using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // ทะเบียนลูกค้า (customer registry) ผูกกับ com_company — mirror จากระบบ vd_*
    // (vendor management) ที่มีอยู่แล้วจริงในระบบพี่น้อง (legacy epms/vms, DB ttmepms,
    // ดู hrm_vd_vendor_tables memory) ตามที่ CEO สั่ง 15 ก.ย. 2569: "เอามาหมด ทุกตาราง...
    // แต่ที่จำเป็นคือ 4 ตารางหลัก". ไม่ทำ com_cust_contact ซ้ำ — Com_CompanyContact
    // มีอยู่แล้วและ wire เข้า CompanyDetail.razor's Contacts tab แล้ว (ดู
    // feedback_reuse_existing_oop_modules memory) เลยไม่ต้องสร้างซ้ำ.
    public partial class AddCustomerRegistryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "com_cust_address",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    AddressType = table.Column<int>(type: "int", nullable: false),
                    AddressNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Building = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Road = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubDistrict = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    District = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_com_cust_address", x => x.Id);
                    table.ForeignKey(
                        name: "FK_com_cust_address_com_company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "com_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "com_cust_financial",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    Income = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Profit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Loss = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    WorkingCapitalRatio = table.Column<decimal>(type: "decimal(9,4)", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_com_cust_financial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_com_cust_financial_com_company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "com_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "com_cust_service",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Level1Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Level1Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Level2Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Level2Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Level3Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Level3Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Level4Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Level4Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_com_cust_service", x => x.Id);
                    table.ForeignKey(
                        name: "FK_com_cust_service_com_company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "com_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "com_cust_doc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    DocType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DocName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DocExpireDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_com_cust_doc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_com_cust_doc_com_company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "com_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "com_cust_certificate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StandCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssueBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IssueYear = table.Column<int>(type: "int", nullable: true),
                    ExpireYear = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_com_cust_certificate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_com_cust_certificate_com_company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "com_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "com_cust_portfolio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_com_cust_portfolio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_com_cust_portfolio_com_company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "com_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "com_cust_signed",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SignedName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SignedPosition = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SignedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    RecordedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_com_cust_signed", x => x.Id);
                    table.ForeignKey(
                        name: "FK_com_cust_signed_com_company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "com_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_com_cust_address_CompanyId", table: "com_cust_address", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_com_cust_financial_CompanyId", table: "com_cust_financial", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_com_cust_service_CompanyId", table: "com_cust_service", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_com_cust_doc_CompanyId", table: "com_cust_doc", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_com_cust_certificate_CompanyId", table: "com_cust_certificate", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_com_cust_portfolio_CompanyId", table: "com_cust_portfolio", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_com_cust_signed_CompanyId", table: "com_cust_signed", column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "com_cust_signed");
            migrationBuilder.DropTable(name: "com_cust_portfolio");
            migrationBuilder.DropTable(name: "com_cust_certificate");
            migrationBuilder.DropTable(name: "com_cust_doc");
            migrationBuilder.DropTable(name: "com_cust_service");
            migrationBuilder.DropTable(name: "com_cust_financial");
            migrationBuilder.DropTable(name: "com_cust_address");
        }
    }
}
