using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddBankAccountModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Com_Bank",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Com_Bank", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hrd_BankAccount",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    BankId = table.Column<long>(type: "bigint", nullable: true),
                    BankBranch = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccountTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BankAccountNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BankAccountName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hrd_BankAccount", x => x.Id);
                });

            // Fixed real-world reference list (standard Thai bank codes) — not
            // company-configurable, so seeded directly rather than left empty
            // like the company-specific policy tables elsewhere in this app.
            migrationBuilder.InsertData(
                table: "Com_Bank",
                columns: new[] { "Code", "Name", "NameEn", "SortOrder", "IsActive" },
                values: new object[,]
                {
                    { "002", "ธนาคารกรุงเทพ", "Bangkok Bank", 1, true },
                    { "004", "ธนาคารกสิกรไทย", "Kasikornbank", 2, true },
                    { "006", "ธนาคารกรุงไทย", "Krungthai Bank", 3, true },
                    { "011", "ธนาคารทหารไทยธนชาต", "TMBThanachart Bank", 4, true },
                    { "014", "ธนาคารไทยพาณิชย์", "Siam Commercial Bank", 5, true },
                    { "025", "ธนาคารกรุงศรีอยุธยา", "Bank of Ayudhya (Krungsri)", 6, true },
                    { "069", "ธนาคารเกียรตินาคินภัทร", "Kiatnakin Phatra Bank", 7, true },
                    { "022", "ธนาคารซีไอเอ็มบีไทย", "CIMB Thai Bank", 8, true },
                    { "067", "ธนาคารทิสโก้", "TISCO Bank", 9, true },
                    { "024", "ธนาคารยูโอบี", "United Overseas Bank (Thai)", 10, true },
                    { "073", "ธนาคารแลนด์ แอนด์ เฮ้าส์", "Land and Houses Bank", 11, true },
                    { "070", "ธนาคารไอซีบีซี (ไทย)", "ICBC (Thai)", 12, true },
                    { "030", "ธนาคารออมสิน", "Government Savings Bank", 13, true },
                    { "033", "ธนาคารอาคารสงเคราะห์", "Government Housing Bank", 14, true },
                    { "034", "ธนาคารเพื่อการเกษตรและสหกรณ์การเกษตร (ธ.ก.ส.)", "Bank for Agriculture and Agricultural Cooperatives", 15, true },
                    { "066", "ธนาคารอิสลามแห่งประเทศไทย", "Islamic Bank of Thailand", 16, true },
                    { "071", "ธนาคารไทยเครดิต เพื่อรายย่อย", "Thai Credit Retail Bank", 17, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Com_Bank");

            migrationBuilder.DropTable(
                name: "Hrd_BankAccount");
        }
    }
}
