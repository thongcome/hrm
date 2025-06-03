using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class sc_userPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ALL_EMPLOYEE",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DATASOURCE = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PRS_NO = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EMP_INTL = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EMP_SURNME = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EMP_E_NAME = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PRS_SC_D = table.Column<DateOnly>(type: "date", nullable: true),
                    JBT_THAIDESC = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    COMPANY = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DEPT1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DEPT2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DEPT3 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SEX = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EMP_MARITAL = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EMP_BIRTH = table.Column<DateOnly>(type: "date", nullable: true),
                    EMP_ADDR_1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EMP_ADDR_2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EMP_ADDR_3 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EMP_POST = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EMP_TEL = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EMP_EMAIL = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EMP_I_CARD = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EMP_I_EXPIRE = table.Column<DateOnly>(type: "date", nullable: true),
                    EMP_I_ISSUE = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    STATUSEMP = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    DIPCHIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LOAD_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    EMP_NAME = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DIPSHIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ALL_EMPLOYEE", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_application_list",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_application_list", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "approve_level",
                columns: table => new
                {
                    approveid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    approverLevel = table.Column<int>(type: "int", nullable: false),
                    levelcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    comcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    cost_center = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    min_budget = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    max_budget = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    role_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    role_id = table.Column<long>(type: "bigint", nullable: true),
                    engname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApproveLevel", x => x.approveid);
                });

            migrationBuilder.CreateTable(
                name: "approver_budget",
                columns: table => new
                {
                    budgetid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    approvelevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    budget_min = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    noreceipt = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    advance = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    comparesign = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApproverBudget", x => x.budgetid);
                });

            migrationBuilder.CreateTable(
                name: "app_serverInfo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    nameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    appID = table.Column<long>(type: "bigint", nullable: false),
                    appCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ipAddress = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    serverType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    instantName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    serviceName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    userName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    confident_info = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    routeNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_serverInfo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleAspNetUser",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleAspNetUser", x => new { x.RoleId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "asset_fa_ora",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    book_type_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    asset_number = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    asset_desc = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    tag_number = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    serial_number = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    date_placed_in_service = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    units_assigned = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    original_cost = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    COST = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    salvage_value = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    life_in_months = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    asset_year = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    asset_month = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    asset_category = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    asset_category_desc = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    date_effective = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    employee_number = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    full_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    location_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    company_desc = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    costcenter_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    account_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    account_desc = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    product_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    product_desc = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    asset_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    distribution_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    transaction_header_id_in = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    transaction_header_id_out = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_fa_ora", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asset_notice",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    serial = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    assetNo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    comCode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    tag = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    qty = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    empName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    empid = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    getdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    outdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isOwnbyOrg = table.Column<bool>(type: "bit", nullable: true),
                    assetid = table.Column<long>(type: "bigint", nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    createby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    moduserid = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_notice", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asset_owner",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    location = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    costCenter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    category = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    assetNo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    comCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    place = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    tag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    brand = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    model = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    serial = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    year = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    month = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    inServiceDate = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    qty = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    currentCost = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccDP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    nbv = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    empName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_owner", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "button_link",
                columns: table => new
                {
                    btlinkid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    btid = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    workflowid = table.Column<long>(type: "bigint", nullable: true),
                    wlevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    role = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    bcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    wfcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isshow = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ButtonLink", x => x.btlinkid);
                });

            migrationBuilder.CreateTable(
                name: "com_company",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    logo_file = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    logp_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    tax_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    mission = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    slogan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    address_HQ = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    tel = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    capital_register = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    amount_emp = table.Column<int>(type: "int", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    abbr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comp_company", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "com_organization",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    layer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    parent_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isCompany = table.Column<bool>(type: "bit", nullable: false),
                    comp_code_all = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isBranch = table.Column<bool>(type: "bit", nullable: false),
                    tax_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    layer_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    approver_userid = table.Column<long>(type: "bigint", nullable: true),
                    isManPowerCount = table.Column<bool>(type: "bit", nullable: false),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    org_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    abbr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    boss_emp_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    approver_empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    comp_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    companyid = table.Column<long>(type: "bigint", nullable: true),
                    org_level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    asOfDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    orgCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgLayerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    orgLayerNameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    refkeyParent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ref1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    parentID = table.Column<long>(type: "bigint", nullable: true),
                    boss_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approver_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approver_PosName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    node_level = table.Column<int>(type: "int", nullable: true),
                    istop = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "com_organization_his",
                columns: table => new
                {
                    idn = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    layer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    parent_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isCompany = table.Column<bool>(type: "bit", nullable: false),
                    comp_code_all = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isBranch = table.Column<bool>(type: "bit", nullable: false),
                    tax_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    layer_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    approver_userid = table.Column<long>(type: "bigint", nullable: true),
                    isManPowerCount = table.Column<bool>(type: "bit", nullable: false),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    org_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    abbr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    boss_emp_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    approver_empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    comp_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    companyid = table.Column<long>(type: "bigint", nullable: false),
                    org_level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    asOfDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    orgCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgLayerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    orgLayerNameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    refkeyParent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ref1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    parentID = table.Column<long>(type: "bigint", nullable: true),
                    boss_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approver_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approver_PosName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    node_level = table.Column<int>(type: "int", nullable: true),
                    istop = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_com_organization_his", x => x.idn);
                });

            migrationBuilder.CreateTable(
                name: "com_organize_layer",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    layercode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    layer = table.Column<int>(type: "int", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    group_layer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    abbr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organize_layer", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "com_org_layer_group",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    abbr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_com_org_layer_group", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "com_position",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    comp_code_all = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pos_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    min_salary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    max_salary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    costcentercode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_com_position", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ConsentHistory",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    email = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    lastname = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    mobile = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    line = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    card_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    card_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentHistory", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CostCenterProcurement",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    costcenter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    costCenterAll = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    upperCostCenter = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCenterProcurement", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Currency",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currency", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DocTypeMapping",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doctypeid = table.Column<long>(type: "bigint", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    isRequired = table.Column<bool>(type: "bit", nullable: true),
                    Source2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doctypecode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocTypeMapping", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "emp_checkout",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    checkinid = table.Column<long>(type: "bigint", nullable: true),
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    checkout_time = table.Column<DateTime>(type: "datetime", nullable: true),
                    lat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    lon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    place = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    ismain = table.Column<bool>(type: "bit", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isAdjust = table.Column<bool>(type: "bit", nullable: true),
                    checkout_time_stamp = table.Column<DateTime>(type: "datetime", nullable: true),
                    isIn = table.Column<bool>(type: "bit", nullable: true),
                    isOut = table.Column<bool>(type: "bit", nullable: true),
                    macAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    orgName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    files = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    path = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emp_checkout", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<long>(type: "bigint", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TitleTh = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TitleEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NameTh = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CardID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CardType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EmployeeID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Companycode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Company = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CostCenter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    MFName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SuperviserID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SuperViserName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    MEMail = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    empLevel = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    extensionNo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OrgNameEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrgNameTh = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PosCode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CostCenterName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    sex = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    hiringDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    positionEN = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    positionTH = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    employeegroup = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    OrgCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    departmentName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    departmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    comp_code_all = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    image_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isApprove = table.Column<bool>(type: "bit", nullable: false),
                    ApproveBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ApproveDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    last_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    mobile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    marital = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    lastNameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isRegister = table.Column<bool>(type: "bit", nullable: false),
                    registerDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    asOfDate = table.Column<DateOnly>(type: "date", nullable: true),
                    job_grade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    head_code = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    resign_by = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    resign_date = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMPLOYEE", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Employee_data",
                columns: table => new
                {
                    Emp_key = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Surname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Emp_no = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Emp_card = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Emp_Dept = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Passwd = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Head_Code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Vacation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sick = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    business = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee_data", x => x.Emp_key);
                });

            migrationBuilder.CreateTable(
                name: "employeetype",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createby = table.Column<int>(type: "int", nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    engname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ismanpower = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employeetype", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "emp_overtime_request",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    requestDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    starttime = table.Column<DateTime>(type: "datetime", nullable: false),
                    endtime = table.Column<DateTime>(type: "datetime", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    workhour = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    workminute = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ot_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    approveby = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    approvedate = table.Column<DateTime>(type: "datetime", nullable: true),
                    real_starttime = table.Column<DateTime>(type: "datetime", nullable: true),
                    real_endtime = table.Column<DateTime>(type: "datetime", nullable: true),
                    objective = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgname = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    com_code_all = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emp_overtime_request", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "error_log_file",
                columns: table => new
                {
                    logid = table.Column<long>(type: "bigint", nullable: false),
                    logdate = table.Column<DateOnly>(type: "date", nullable: true),
                    logfile = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    logpath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogFile", x => x.logid);
                });

            migrationBuilder.CreateTable(
                name: "error_messege",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    messege_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    messege_Th = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    message_En = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    lang = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorMessege", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "HeadCode_V1",
                columns: table => new
                {
                    PRS_DEPT = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DPG_THAIDESC = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DEPT_THAIDESC = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "his_doc_bak",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    doc_type = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    doc_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doc_date = table.Column<DateOnly>(type: "date", nullable: true),
                    doc_expire_date = table.Column<DateOnly>(type: "date", nullable: true),
                    filename = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    file_path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    secureLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    table_ref = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    code_ref = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    createon = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_his_doc", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Holiday",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    hol_month = table.Column<int>(type: "int", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    hol_day = table.Column<int>(type: "int", nullable: true),
                    year = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isWeekEnd = table.Column<bool>(type: "bit", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOLIDAY", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "HRBASEPAYROLLFIXED",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EMP_NO = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SEQ_NO = table.Column<int>(type: "int", precision: 3, nullable: false),
                    SALITEM_CODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    ITEM_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRBASEPAYROLLFIXED", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HREMPLOYEE",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyid = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    EMP_NO = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    PRENAME_CODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    EMP_NAME = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EMP_SURNAME = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EMP_ENAME = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EMP_ESURNAME = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EMP_NICKNAME = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EMPTYPE_CODE = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    DEPTGRP_CODE = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    POS_CODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    SEX = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    WEIGHT = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    HEIGHT = table.Column<decimal>(type: "decimal(3,2)", precision: 3, nullable: true),
                    NATION = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RELIGION = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ADN_NO = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ADN_TAMBOL = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ADN_AMPHUR = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ADN_PROVINCE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ADN_POSTCODE = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ADN_TEL = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ADN_EMAIL = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ADR_NO = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ADR_TAMBOL = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ADR_AMPHUR = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ADR_PROVINCE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ADR_POSTCODE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ID_CARD = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    BIRTH_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    WORK_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    RESIGN_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    CONTAIN_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    TERM_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    SALARY_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    DAILY_WAGE = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    SALEXP_CODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    SALEXP_BANK = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    SALEXP_BRANCH = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    SALEXP_ACCID = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    TAX_CALCODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    TAX_BFAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    SS_STATUS = table.Column<decimal>(type: "decimal(2,2)", precision: 2, nullable: true),
                    SS_APPFIRSTSTS = table.Column<decimal>(type: "decimal(2,2)", precision: 2, nullable: true),
                    SS_BFAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    SS_APPDATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    SS_RATE = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SS_HOSPITAL = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PROVF_STATUS = table.Column<decimal>(type: "decimal(2,2)", precision: 2, nullable: true),
                    PROVF_CORPRATE = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    PROVF_EMPRATE = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    PROVF_APPDATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    PROVF_RESIGNDATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    PROVF_BFAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    REF_MEMBNO = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    EMP_STATUS = table.Column<decimal>(type: "decimal(2,2)", precision: 2, nullable: true),
                    BLOODTYPE_CODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    EMPLEVEL_CODE = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    PROFESSIONAL_AMT = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    COMPENSATION_AMT1 = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    COMPENSATION_AMT2 = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    EMER_NAME = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EMER_SURNAME = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CONCERN_CODE = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    EMER_PHONE = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    EMER_ADDRESS = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LEVEL_CODE = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    WORKTIME_CODE = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    LEAVE_BF = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    OVERLEAVE_FLAG = table.Column<int>(type: "int", nullable: false),
                    EMP_SPECIAL = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UPDATE_BYENTRYID = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UPDATE_BYENTRYIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DEPT_FIRST = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    SCAN_NO = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    LAVEL_ACTIVE_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    CONTRACT_NO = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    CONTRACT_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    HEALTH_CHECK1 = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    HEALTH_CHECK2 = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    VACCINE_AMT = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    MATE_CODE = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    HR_TYPE = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    PROVFCORP_BFAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    PROVF_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    PROVFCORP_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    ASSIST_BF = table.Column<decimal>(type: "decimal(15,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HREMPLOYEE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HREXTENUATETAX",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyid = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    EMP_NO = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    PAY_TRAVEL = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PAY_CHILD = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PAY_PARENT = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PAY_INTADDR = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PAY_INSOTH = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PAY_INSRETRY = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PAY_DISABLED = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PAY_INSPARENT = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PAY_MATE = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PAY_LTF = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PAY_RMF = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PAY_DONATEOTH = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PAY_DONATEEDU = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HREXTENUATETAX", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HRPAYACCUM",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyid = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    YEAR = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    EMP_NO = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    SALARYBASE = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    OTHERINCOME = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    INCOMEPREDICTAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    INCOMEYEARAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    TAXYEARAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    INCOMEFORWARDAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    STARTMONTH = table.Column<decimal>(type: "decimal(2,0)", nullable: true),
                    ENDMONTH = table.Column<decimal>(type: "decimal(2,0)", nullable: true),
                    WORKMONTH = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    LASTMONTHCAL = table.Column<decimal>(type: "decimal(2,0)", nullable: false),
                    LASTCALPERIODCODE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MODBY = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    MODDATE = table.Column<DateTime>(type: "DATE", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRPAYACCUM", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HRPAYROLL",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyid = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    PAYROLLSLIP_NO = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    EMP_NO = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    PAYROLL_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    PAYROLL_PERIOD = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    SALARYBASE_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    SALARYOTH_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    SALARYSUBT_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    SALARYNET_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    PAYROLL_STATUS = table.Column<decimal>(type: "decimal(2,0)", nullable: true),
                    EXPENSE_CODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    EXPENSE_BANK = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    EXPENSE_BRANCH = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    EXPENSE_ACCID = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    POST_STATUS = table.Column<int>(type: "int", nullable: true),
                    MEMBER_NO = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    PERCEN_SECURITY = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    PERCENMG_SECURITY = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    SOFMG_SECURITY = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    POST_SOF_STATUS = table.Column<decimal>(type: "decimal(1,0)", nullable: false),
                    POST_STA_STATUS = table.Column<decimal>(type: "decimal(1,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRPAYROLL", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HRPAYROLLDET",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyid = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    PAYROLLSLIP_NO = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    SEQ_NO = table.Column<int>(type: "int", nullable: false),
                    EMP_NO = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    PAYROLL_PERIOD = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    SALITEM_CODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ITEM_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRPAYROLLDET", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HRUCFSALARYITEM",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyid = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    SALITEM_CODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    SALITEM_DESC = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SIGN_FLAG = table.Column<int>(type: "int", precision: 2, nullable: true),
                    MANUAL_FLAG = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "HRUCFSECURITY",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyid = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    SECURITY_CODE = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    SECURITY_DESC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PERCEN_SECURITY = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    SECURITY_MONEY = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    OVER_SECURITY_MONEY = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PERCENMG_SECURITY = table.Column<decimal>(type: "decimal(3,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRUCFSECURITY", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HRUCFTAXRATE",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyid = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    CODE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NAME = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NAMEEN = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    MINRATE = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MAXRATE = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PERCENTRATE = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TAXLEVELRATE = table.Column<int>(type: "int", nullable: false),
                    TAXACCUMLEVELRATE = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BASELEVELTAX = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BASELEVELINCOME = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ISACTIVE = table.Column<int>(type: "int", nullable: false),
                    YEAR = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    STEP = table.Column<int>(type: "int", nullable: false),
                    MODIFIED_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    MODIFIED_BY = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRUCFTAXRATE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HRW_OT",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyid = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    EMP_NO = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    SEQ_NO = table.Column<int>(type: "int", precision: 10, nullable: false),
                    REMARK = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DATE_WORK = table.Column<DateTime>(type: "DATE", nullable: true),
                    WORK_IN = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    WORK_OUT = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    WORKOT_IN = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    WORKOT_OUT = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    OT_P = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    APV_OT_STATUS = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    OT_CODE = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    OT_DOCNO = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OT_DOCNO_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    OT_AMT = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MONEY_HOUR = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    MONEY_MINUTE = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    OT_P_MINUTE = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    DEPTGRP_CODE = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    WORKDEFAULT_IN = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    WORKDEFAULT_OUT = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    POST_STATUS = table.Column<int>(type: "int", nullable: true),
                    DATE_WORK_TO = table.Column<DateTime>(type: "DATE", nullable: true),
                    SIGNATURE1 = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    SIGNATURE2 = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    SIGNATURE3 = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    SALARY_CAL_SEQ = table.Column<decimal>(type: "decimal(4,2)", precision: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRW_OT", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "J_DEPT_GROUP_V2",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DEPT_KEY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DEPT_CODE = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DEPT_THAIDESC = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DEPT_ENGDESC = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DEPT_LEVEL = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DEPT_PARENT = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DEPT_PARENT_KEY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MOD_DATE = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    MOD_BY = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    REMARK = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEPT_GROUP_V2", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "J_HR_CHECK_DEPT",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EMP_KEY = table.Column<long>(type: "bigint", nullable: true),
                    PRS_NO = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PRS_E_CARD = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EMP_NAME = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EMP_SURNME = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PRS_TITLE = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PRS_DEPT = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PRI_STATUS = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PRS_GRADE_EX = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MOD_DATE = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    MOD_BY = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    REMARK = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HR_CHECK_DEPT", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Job_Comment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    mobby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doc1 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    doc2 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    createby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job_Comment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "JobInJob",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mainjobid = table.Column<long>(type: "bigint", nullable: false),
                    refjobid = table.Column<long>(type: "bigint", nullable: false),
                    wlevel_start = table.Column<int>(type: "int", nullable: false),
                    isApprove = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobInJob", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_loa",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: false),
                    wlevel = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    jobrefid = table.Column<long>(type: "bigint", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    workflowid = table.Column<long>(type: "bigint", nullable: true),
                    loaid = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_loa", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_status",
                columns: table => new
                {
                    jobstatusid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    jobstatuscode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    businessstatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    bizName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobStatus", x => x.jobstatusid);
                });

            migrationBuilder.CreateTable(
                name: "job_subworkflow_master",
                columns: table => new
                {
                    jobsubworkflowid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: false),
                    workflowid = table.Column<long>(type: "bigint", nullable: true),
                    wlevel = table.Column<int>(type: "int", nullable: false),
                    isupperrole = table.Column<bool>(type: "bit", nullable: false),
                    isupperuser = table.Column<bool>(type: "bit", nullable: false),
                    iscondition = table.Column<bool>(type: "bit", nullable: false),
                    isorcondition = table.Column<bool>(type: "bit", nullable: false),
                    isandcondition = table.Column<bool>(type: "bit", nullable: false),
                    andpercent = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    istop = table.Column<bool>(type: "bit", nullable: false),
                    remark = table.Column<string>(type: "text", nullable: true),
                    iscustomUser = table.Column<bool>(type: "bit", nullable: true),
                    iscustomRole = table.Column<bool>(type: "bit", nullable: true),
                    empLevel = table.Column<int>(type: "int", nullable: true),
                    isshow = table.Column<bool>(type: "bit", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    jobseq = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSubworkflowmaster", x => x.jobsubworkflowid);
                });

            migrationBuilder.CreateTable(
                name: "KPTEMPRECEIVE",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyid = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    KPSLIP_NO = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    MEMCOOP_ID = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    MEMBER_NO = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    RECV_PERIOD = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    REFMEMCOOP_ID = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    REF_MEMBNO = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    MEMBTYPE_CODE = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    DEPARTMENT_CODE = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    MEMBGROUP_CODE = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    PROCESS_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    RECEIPT_NO = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    RECEIPT_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    OPERATE_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    SHARESTKBF_VALUE = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    SHARESTK_VALUE = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    INTEREST_ACCUM = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    KEEPING_STATUS = table.Column<int>(type: "int", precision: 2, nullable: true),
                    RECEIVE_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    MONEY_TEXT = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LAST_SEQ_NO = table.Column<int>(type: "int", precision: 5, nullable: true),
                    ENTRY_ID = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPTEMPRECEIVE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "KPTEMPRECEIVEDET",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyid = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    KPSLIP_NO = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    SEQ_NO = table.Column<int>(type: "int", precision: 5, nullable: false),
                    MEMCOOP_ID = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    MEMBER_NO = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    RECV_PERIOD = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    REFMEMCOOP_ID = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    REF_MEMBNO = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    KEEPITEMTYPE_CODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    SHRLONTYPE_CODE = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    ITEM_SEQNO = table.Column<int>(type: "int", precision: 5, nullable: true),
                    LOANCONTRACT_NO = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PERIOD = table.Column<int>(type: "int", precision: 5, nullable: true),
                    PRINCIPAL_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    INTEREST_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    INTARREAR_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    ITEM_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    ITEM_BALANCE = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    PRINCIPAL_BALANCE = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    CALINTFROM_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    CALINTTO_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    PRINCIPAL_PERIOD = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    INTEREST_PERIOD = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFPRINBALANCE_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFPERIOD = table.Column<int>(type: "int", precision: 5, nullable: true),
                    BFLOANPAYMENT_TYPE = table.Column<int>(type: "int", precision: 2, nullable: true),
                    BFPERIOD_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFMAXPERIOD_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFPAYMENT_STATUS = table.Column<int>(type: "int", precision: 2, nullable: true),
                    BFLASTCALINT_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    BFLASTPAY_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    BFINTEREST_ARREAR = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFINTMONTH_ARREAR = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFINTYEAR_ARREAR = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFPRINCIPAL_ARREAR = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFINTEREST_RETURN = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFCONTRACT_STATUS = table.Column<int>(type: "int", precision: 2, nullable: true),
                    BFCONTLAW_STATUS = table.Column<int>(type: "int", precision: 2, nullable: true),
                    KEEPITEM_STATUS = table.Column<int>(type: "int", precision: 2, nullable: true),
                    POSTING_STATUS = table.Column<int>(type: "int", precision: 2, nullable: true),
                    POSTING_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    CANCEL_ID = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    CANCEL_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    BFADJUST_PRNAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFADJUST_INTAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFADJUST_ITEMAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFREAL_INTPAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    INTERESTRECAL_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    KEPPAYMENT_TYPE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    MISSPAY_FLAG = table.Column<int>(type: "int", precision: 2, nullable: true),
                    OVERPAY_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    OVERPAY_PRNAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    OVERPAY_INTAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    RETOVERPAY_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    RETOVERPAY_PRNAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    RETOVERPAY_INTAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    ADJUST_ITEMAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    ADJUST_PRNAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    ADJUST_INTAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    AFKEPPAY_ITEMAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    AFKEPPAY_PRNAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    AFKEPPAY_INTAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    AFKEPPAY_INTRETAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFKPPOST_ITEMAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFKPPOST_PRNAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BFKPPOST_INTAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    KPRECAL_ITEMAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    KPRECAL_PRNAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    KPRECAL_INTAMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    CASE_POST = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DIINT_PERIOD = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    RECALINTAFKP_AMT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    ADJUST_ID = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ADJUST_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    REALITEM_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    REALPRINCIPAL_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    REALINTEREST_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    CCLKEPPRN_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    CCLKEPINT_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    CCLKEPITEM_PAYMENT = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    AFKEPPAY_REFSLIP = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    AFKEPPAY_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    KEPEXENSE_CODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    KEPEXENSE_ACCID = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    TRANTOVC_DATE = table.Column<DateTime>(type: "DATE", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPTEMPRECEIVEDET", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "loa",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    min = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    max = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    loaTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    levelcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOA", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loa_type",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOAType", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "log_system_log",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(18,0)", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    activity = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ipaddress = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    hostname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    accesstime = table.Column<DateTime>(type: "datetime", nullable: false),
                    actstatus = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "ntext", nullable: true),
                    oldvalue = table.Column<string>(type: "ntext", nullable: true),
                    newvalue = table.Column<string>(type: "ntext", nullable: true),
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    moddate = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHANGELOG", x => x.id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateTable(
                name: "mas_address_type",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mas_AddressType", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mas_bidding_status",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mas_bidding_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mas_country",
                columns: table => new
                {
                    countryid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    countryName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    countrycode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.countryid);
                });

            migrationBuilder.CreateTable(
                name: "mas_doc_type",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    group_ref = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    subMode1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orderTh = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DPDocType", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mas_EmailTemplate",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prefix = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    suffix = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    noParam = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mas_EmailTempate", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mas_reason",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    workflowid = table.Column<long>(type: "bigint", nullable: true),
                    wlevel = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mas_reason", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mas_service",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    service_code = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    service_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    slevel = table.Column<int>(type: "int", nullable: true),
                    service_parent_code = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    service_name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    service_code_all = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "text", nullable: true),
                    Pid = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mas_service", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mas_status",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    group_ref = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mas_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mas_title",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    title_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mas_title", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mas_unit_type",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    nameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mas_unit_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mas_WarranteeType",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarrantyTypeTh = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    WarrantyTypeEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyType", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pc_BiddingStatus",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    modate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_BiddingStatus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pc_rfq",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rfq_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    rfqdate = table.Column<DateOnly>(type: "date", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    total = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    total_net = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    createby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    vat = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    isVat = table.Column<bool>(type: "bit", nullable: true),
                    vat_amount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: true),
                    quotation_ref = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vendor_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vendor_taxid = table.Column<byte[]>(type: "varbinary(50)", maxLength: 50, nullable: true),
                    vendor_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    rfqName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: true),
                    isApprove = table.Column<bool>(type: "bit", nullable: true),
                    file1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    file2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    file3 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    deadline_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    requestor = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    requestorOrg = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    requestorCostCenter = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    start_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    requestorOrgName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    requestorName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    showplusday = table.Column<int>(type: "int", nullable: true),
                    isBidClosed = table.Column<bool>(type: "bit", nullable: true),
                    isBidding = table.Column<bool>(type: "bit", nullable: true),
                    pridAsTe = table.Column<long>(type: "bigint", nullable: true),
                    isTE = table.Column<bool>(type: "bit", nullable: true),
                    fileTe = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    pathTe = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApproveDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    statusName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    pr_no = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    condition = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    remark2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewerBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc1path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc2path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc3 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc3path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc4 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc4path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc5 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc5path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_rfq", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pc_RFQCondition",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    nameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ismandatory = table.Column<bool>(type: "bit", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    orderTh = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RFQCondition", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pc_rfq_doc",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    doc_type = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    doc_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doc_date = table.Column<DateOnly>(type: "date", nullable: true),
                    doc_expire_date = table.Column<DateOnly>(type: "date", nullable: true),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    secureLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    isMandatory = table.Column<bool>(type: "bit", nullable: true),
                    remark = table.Column<bool>(type: "bit", nullable: true),
                    doc_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_rfq_doc", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pc_RFQServiceSelect",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rfqID = table.Column<long>(type: "bigint", nullable: false),
                    rfqNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    serviceid = table.Column<long>(type: "bigint", nullable: false),
                    servicecode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    serviceLevel = table.Column<int>(type: "int", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_RFQSeviceSelect", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pc_rfq_status",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_rfq_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pc_te",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TEName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: true),
                    workflowid = table.Column<long>(type: "bigint", nullable: true),
                    wleve = table.Column<int>(type: "int", nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    crateby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    prNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    prItem = table.Column<int>(type: "int", nullable: true),
                    approveBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PcTe", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pc_vd_Clarify",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rfqid = table.Column<long>(type: "bigint", nullable: false),
                    vendorid = table.Column<long>(type: "bigint", nullable: false),
                    text1 = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    file1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    path = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    filerResp = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    pathResp = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    modbyVdID = table.Column<long>(type: "bigint", nullable: true),
                    modbyVdName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddateVD = table.Column<DateTime>(type: "datetime", nullable: true),
                    useridVD = table.Column<long>(type: "bigint", nullable: true),
                    subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    isReadOnly = table.Column<bool>(type: "bit", nullable: true),
                    isApprove = table.Column<bool>(type: "bit", nullable: true),
                    approveBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    text2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    approvedate = table.Column<DateTime>(type: "datetime", nullable: true),
                    isVendorState = table.Column<bool>(type: "bit", nullable: true),
                    vdText1 = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    isprocurementState = table.Column<bool>(type: "bit", nullable: true),
                    vdText2 = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    file2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    file3 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    path2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    path3 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    statusName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    modbyReqID = table.Column<long>(type: "bigint", nullable: true),
                    modbyReqName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddateReq = table.Column<DateTime>(type: "datetime", nullable: true),
                    useridReqCreate = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_vd_Clarify", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pc_vd_RFQCondition",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    nameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ismandatory = table.Column<bool>(type: "bit", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    orderTh = table.Column<int>(type: "int", nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: true),
                    rfqid = table.Column<long>(type: "bigint", nullable: true),
                    quotationNo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    VendorCode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    text1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    text2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "pc_vd_te",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vendorid = table.Column<long>(type: "bigint", nullable: false),
                    vendorcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    rfqid = table.Column<long>(type: "bigint", nullable: false),
                    techcriteria_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    te_comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    condition = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isPass = table.Column<bool>(type: "bit", nullable: true),
                    isAgree = table.Column<bool>(type: "bit", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    approveBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approveDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    userid = table.Column<long>(type: "bigint", nullable: true),
                    orderTh = table.Column<int>(type: "int", nullable: true),
                    isSentMember = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_vd_te", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pc_vd_te_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vendorid = table.Column<long>(type: "bigint", nullable: true),
                    vendorcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    rfqid = table.Column<long>(type: "bigint", nullable: false),
                    techcriteria_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pr_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    pr_item = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    no1 = table.Column<int>(type: "int", nullable: true),
                    no2 = table.Column<int>(type: "int", nullable: true),
                    no3 = table.Column<int>(type: "int", nullable: true),
                    criteria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doc1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    doc2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    doc3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    te_comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isPassCondition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    point = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    point_evaluate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    condition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    approveBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approveDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    pc_te_id = table.Column<long>(type: "bigint", nullable: true),
                    prid = table.Column<long>(type: "bigint", nullable: true),
                    topicNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    teitemid = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_EvaluationVD", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pc_vendor_quotation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    quotation_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    rfq_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PRNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    rfqdate = table.Column<DateOnly>(type: "date", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    total = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    total_net = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    createby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    vat = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    isVat = table.Column<bool>(type: "bit", nullable: true),
                    vat_amount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    vendor_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vendor_taxid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vendor_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    vendor_alias_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: true),
                    rfqid = table.Column<long>(type: "bigint", nullable: true),
                    isBidwon = table.Column<bool>(type: "bit", nullable: true),
                    addr = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    logo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    isRequisitionerState = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CURRENCY_KEY = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isPriceApprove = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    PriceApproveBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    negoCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_vendor_quotation", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pc_vendor_quotation_Item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    quotation_no_ref = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    rfq_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    rfq_itemno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PRNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    pr_itemno = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    net_amount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    descript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    mat_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    unit_type = table.Column<long>(type: "bigint", nullable: true),
                    unit_amount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    remark_special = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nego_price = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    nego_amount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    prid = table.Column<long>(type: "bigint", nullable: true),
                    rfqid = table.Column<long>(type: "bigint", nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: true),
                    qty = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    uom = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    quotationID = table.Column<long>(type: "bigint", nullable: true),
                    ApproverPrice = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isApproverTech = table.Column<bool>(type: "bit", nullable: false),
                    isApprovePrice = table.Column<bool>(type: "bit", nullable: false),
                    isPriceAction = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    DateApproverTech = table.Column<DateTime>(type: "datetime", nullable: true),
                    DateApproverPrice = table.Column<DateTime>(type: "datetime", nullable: true),
                    ApproverUserID = table.Column<long>(type: "bigint", nullable: true),
                    ApproverUserIDPrice = table.Column<long>(type: "bigint", nullable: true),
                    ApproverUserPrice = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NextApproverUserPrice = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    requestor_empid = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NextApprover = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isRequisitionerState = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    isTechAction = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    ApproverTech = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isApproverPrice = table.Column<bool>(type: "bit", nullable: false),
                    CURRENCY_KEY = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isPriceRequisitionState = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    budget = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_vendor_quotation_Item", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pdpa_compliance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    complianceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ref1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ref2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ref3 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ref4 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    descript1 = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    descript2 = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    descript3 = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    law_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pdpa_compliance", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pdpa_consent",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    consent_masterid = table.Column<long>(type: "bigint", nullable: false),
                    consent_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    company_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    company_name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    channel = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    email = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    mobile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    social1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    social2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    social3 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    social4 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    objective_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    objectiveID = table.Column<long>(type: "bigint", nullable: true),
                    cust_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    getdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    outdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    app_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    app_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    version_no = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    isEmail = table.Column<bool>(type: "bit", nullable: true),
                    isSms = table.Column<bool>(type: "bit", nullable: true),
                    isMobile = table.Column<bool>(type: "bit", nullable: true),
                    isSocial = table.Column<bool>(type: "bit", nullable: true),
                    isMail = table.Column<bool>(type: "bit", nullable: true),
                    isOther = table.Column<bool>(type: "bit", nullable: true),
                    telephone = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    other = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    img1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    img2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    img3 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    consent_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    unconsent_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    unconsent_channel = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    unconsent_remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    unconsent_reasoncode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    consent_detail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    consent_subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pdpa_consent", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pdpa_consent_master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code_master = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    subject_en = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    file_name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    file_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    file_name2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    file_path2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    domain_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    consent_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    create_by = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    start_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    end_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    version_no = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    new_versionid = table.Column<long>(type: "bigint", nullable: true),
                    isApprove = table.Column<bool>(type: "bit", nullable: true),
                    approve_by = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approve_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    company_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isPrivacy = table.Column<bool>(type: "bit", nullable: true),
                    isEmail = table.Column<bool>(type: "bit", nullable: true),
                    isSms = table.Column<bool>(type: "bit", nullable: true),
                    isMobile = table.Column<bool>(type: "bit", nullable: true),
                    isSocial = table.Column<bool>(type: "bit", nullable: true),
                    isMail = table.Column<bool>(type: "bit", nullable: true),
                    isOther = table.Column<bool>(type: "bit", nullable: true),
                    app_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    app_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    app_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    app_qrcode = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pdpa_consent_master", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pdpa_datamart",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    sLevel = table.Column<int>(type: "int", nullable: true),
                    sLevelCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    uri_api = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_con_datamart", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pdpa_filePrivacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    file_name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    file_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    domain_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    consent_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    start_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    end_date = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_con_filePrivacy", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pdpa_log_convertEndDec",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subject = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    str_source = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    str_target = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    col_source = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    col_target = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    tab_target = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    tab_source = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    converttime = table.Column<DateTime>(type: "datetime", nullable: true),
                    sys_name = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    uri_calling = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_convertEndDec", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pdpa_objective",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    consent_master_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    consent_masterid = table.Column<long>(type: "bigint", nullable: true),
                    version_no_master = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    createby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    isApprove = table.Column<bool>(type: "bit", nullable: true),
                    approveby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approvedate = table.Column<DateTime>(type: "datetime", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pdpa_objective", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "po_inv",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    org_id = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    po_date = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    po_number = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    po_description = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    vendor_code = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    vendor_name = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    authorization_status = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    line_num = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    item_description = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    quantity = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    unit_price = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    line_amount = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    user_name = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    user_desc = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    approved_flag = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    unit_meas_lookup_code = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    currency_code = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    rate_type = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    rate_date = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    rate = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    cancel_flag = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    company_code = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    company_desc = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    costcenter_code = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    costcenter_desc = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    account_code = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    account_desc = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    product_code = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    product_desc = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    invoice_id = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    invoice_number = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    invoice_description = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    invoice_line_number = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    invoice_line_desc = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_po_inv", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pos_position",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pos_code = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    code = table.Column<int>(type: "int", nullable: true),
                    pos_exec_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    sec_code = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    abbname = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    dopaname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    expiredate = table.Column<DateTime>(type: "datetime", nullable: true),
                    min_c_level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    max_c_level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    salary_level = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    isvacancy = table.Column<string>(type: "nchar(1)", fixedLength: true, maxLength: 1, nullable: true),
                    status = table.Column<string>(type: "nchar(1)", fixedLength: true, maxLength: 1, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    min_graduate = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    normal_salary = table.Column<float>(type: "real", nullable: true),
                    paymentrunid = table.Column<int>(type: "int", nullable: true),
                    lperiodid = table.Column<int>(type: "int", nullable: true),
                    paymentno = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    ptp_code = table.Column<int>(type: "int", nullable: true),
                    employeetype = table.Column<int>(type: "int", nullable: true),
                    posid = table.Column<int>(type: "int", nullable: false),
                    ismanpower = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    max_salary = table.Column<float>(type: "real", nullable: true),
                    jobsummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    personid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    engname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    engabbname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    min_salary = table.Column<float>(type: "real", nullable: true),
                    real_salary = table.Column<float>(type: "real", nullable: true),
                    is_boss = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    worklineid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__positionlist__4D1564AE", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pos_position_level",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    pol_note = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    update_by = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    update_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    engname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    plevel = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pos_position_level", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PosRoleAssociate",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    roleid = table.Column<long>(type: "bigint", nullable: false),
                    posid = table.Column<long>(type: "bigint", nullable: false),
                    rolecode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    poscode = table.Column<byte[]>(type: "varbinary(250)", maxLength: 250, nullable: false),
                    isactive = table.Column<byte>(type: "tinyint", nullable: true),
                    remark = table.Column<byte[]>(type: "varbinary(250)", maxLength: 250, nullable: true),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true),
                    enddate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosRoleAssociate", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prefix_runnig",
                columns: table => new
                {
                    gencodeid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    lastrun = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    runing = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    year = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    lastcode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreFixRunnig", x => x.gencodeid);
                });

            migrationBuilder.CreateTable(
                name: "pr_po",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    org_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    req_number = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    req_description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    vendor_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    vendor_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    creation_date = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    type_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    req_line_num = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    item_description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    uom_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    unit_price = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    quantity = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    line_amount = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    cancel_flag = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    requestor = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    user_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    user_description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    authorization_status = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approved_date = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    company_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    company_desc = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    costcenter_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    costcenter_desc = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    product_code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    product_desc = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    po_number = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    requisition_header_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    requisition_line_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    distribution_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    po_header_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    po_line_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    po_distribution_id = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pr_po", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PRRequisitionConfirm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: true),
                    rfqid = table.Column<long>(type: "bigint", nullable: false),
                    prid = table.Column<long>(type: "bigint", nullable: true),
                    PrNo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    pritem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pritemNo = table.Column<int>(type: "int", nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: false),
                    vendorcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    modate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isApprove = table.Column<bool>(type: "bit", nullable: true),
                    approvedate = table.Column<DateTime>(type: "datetime", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ref1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    rfqitemid = table.Column<long>(type: "bigint", nullable: true),
                    approveby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approveUserid = table.Column<long>(type: "bigint", nullable: true),
                    ref2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequisitionerName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ApproverName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isSendMember = table.Column<bool>(type: "bit", nullable: true),
                    SendMemberDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    isAllowReApprove = table.Column<bool>(type: "bit", nullable: true),
                    status = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    statusName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isMandatory = table.Column<bool>(type: "bit", nullable: true),
                    prServiceid = table.Column<long>(type: "bigint", nullable: true),
                    quoteID = table.Column<long>(type: "bigint", nullable: true),
                    quoteItemID = table.Column<long>(type: "bigint", nullable: true),
                    RequisitionerEmpID = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "pr_service_type",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isTE = table.Column<bool>(type: "bit", nullable: false),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pr_service_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sc_menugroup",
                columns: table => new
                {
                    menugroupid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    menugroupname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    langcode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, defaultValueSql: "(NULL)"),
                    menucode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    menugrouplevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "(NULL)"),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sc_menugroup_menugroupid", x => x.menugroupid);
                });

            migrationBuilder.CreateTable(
                name: "sc_menu_program",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    menuid = table.Column<long>(type: "bigint", nullable: false),
                    menucode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    programid = table.Column<long>(type: "bigint", nullable: false),
                    programcode = table.Column<byte[]>(type: "varbinary(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SC_MenuProgram", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sc_program",
                columns: table => new
                {
                    progid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    progname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    templatename = table.Column<string>(type: "text", nullable: true),
                    filename = table.Column<string>(type: "text", nullable: true),
                    progcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    progmastercode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    remark = table.Column<string>(type: "text", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(NULL)"),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    isactive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "(NULL)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SC_PROGRAM", x => x.progid);
                });

            migrationBuilder.CreateTable(
                name: "sc_program_group",
                columns: table => new
                {
                    programgroupid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    langcode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, defaultValueSql: "(NULL)"),
                    proggroupcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    proglevel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, defaultValueSql: "(NULL)"),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sc_programgroup_programgroupid", x => x.programgroupid);
                });

            migrationBuilder.CreateTable(
                name: "stoa",
                columns: table => new
                {
                    stoaid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    comcode = table.Column<string>(type: "nvarchar(125)", maxLength: 125, nullable: true),
                    expenseType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    glAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    specialType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    dateFrom = table.Column<DateTime>(type: "datetime", nullable: true),
                    dateTo = table.Column<DateTime>(type: "datetime", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STOA", x => x.stoaid);
                });

            migrationBuilder.CreateTable(
                name: "task_activity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    checkinid = table.Column<long>(type: "bigint", nullable: true),
                    userid = table.Column<long>(type: "bigint", nullable: true),
                    objectiveid = table.Column<long>(type: "bigint", nullable: true),
                    keyresultid = table.Column<long>(type: "bigint", nullable: true),
                    subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OKRType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    qty = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    expectdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    finisheddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    unit_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    total = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    detail1 = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    detail2 = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    detail3 = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    assignee_EmpID = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    assignby_EmpID = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    files = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    path = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    costcenter = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    total_get = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    total_expect = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    assignee_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    assigner_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_activity", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    descript = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    descript_en = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    create_by = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    pushdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    org_code_create = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    empid_create = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    empid_project_owner = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_Master", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_checkin",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    userid = table.Column<long>(type: "bigint", nullable: true),
                    emp_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    checkin_time = table.Column<DateTime>(type: "datetime", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    latitude = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    longitude = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    comp_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    taskid = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_checkin", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "toa",
                columns: table => new
                {
                    toaid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    comcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    a = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    approvelevel = table.Column<int>(type: "int", nullable: true),
                    ApprovelevelText = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LineExco = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approverEmpid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NameTh = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    delegateEmplD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Company = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Position = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SectTh = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SectEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DeptTh = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DeptEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    StartDateDG = table.Column<DateOnly>(type: "date", nullable: true),
                    EnddateDG = table.Column<DateOnly>(type: "date", nullable: true),
                    wlevel = table.Column<int>(type: "int", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TOA", x => x.toaid);
                });

            migrationBuilder.CreateTable(
                name: "upload_center",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    datasource = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    refId = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ref2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    files = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    path = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    userid = table.Column<long>(type: "bigint", nullable: true),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    table_ref = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    taskid = table.Column<long>(type: "bigint", nullable: true),
                    checkinid = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_upload_center", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vd_certificate",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    standCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    issueBy = table.Column<byte[]>(type: "varbinary(250)", maxLength: 250, nullable: true),
                    issueYear = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    expireYear = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    fileRef = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    filePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: false),
                    vendorcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vd_certificate", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vd_contact",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    mobile = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    line = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    social = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    ismainContact = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: true),
                    taxid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ext = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    position = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    titleid = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vd_contact", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vd_financial",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vendorid = table.Column<long>(type: "bigint", nullable: false),
                    vendorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    year = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    income = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    profit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    loss = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    workingCapitalRatio = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    filename = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    filepath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vd_financial", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vd_portfolio",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    startdate = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    enddate = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    customerName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    fileName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    filePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: false),
                    vendorcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    isWithUs = table.Column<bool>(type: "bit", nullable: false),
                    refcode1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    refcode2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    year = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vd_portfolio", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vd_service",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    service1_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    service1_id = table.Column<long>(type: "bigint", nullable: true),
                    service1_level = table.Column<int>(type: "int", nullable: true),
                    service1_name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    service1_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    service2_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    service2_id = table.Column<long>(type: "bigint", nullable: true),
                    service2_level = table.Column<int>(type: "int", nullable: true),
                    service3_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    service3_id = table.Column<long>(type: "bigint", nullable: true),
                    service3_level = table.Column<int>(type: "int", nullable: true),
                    service4_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    service4_id = table.Column<long>(type: "bigint", nullable: true),
                    service4_level = table.Column<int>(type: "int", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    isApproved = table.Column<bool>(type: "bit", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    service_code_all = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approveby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    file1 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    file2 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    file3 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    vendor_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    taxid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: true),
                    service2_name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    service2_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    service3_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    service4_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    service4_name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    service3_name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    file1_path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    file2_path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    file3_path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vd_service", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vd_signed",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    signedName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    signedPosition = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    signedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: true),
                    userid = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vd_signed", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_button_master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    class_style = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    actiontypecode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    value = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    idname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    wlevelType = table.Column<int>(type: "int", nullable: true),
                    controller = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    action = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    btnType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    btnTag = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    orderth = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_button_master", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_checklist",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    link_url = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    refcode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    workflowmasterid = table.Column<long>(type: "bigint", nullable: false),
                    subworkflowmasterid = table.Column<long>(type: "bigint", nullable: false),
                    isPass = table.Column<bool>(type: "bit", nullable: true),
                    wlevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_CheckList", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_condition",
                columns: table => new
                {
                    subworkflowid = table.Column<long>(type: "bigint", nullable: false),
                    wfcondiionid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approveLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    con_type = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    wlevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    conditionLower = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    con_value_lower = table.Column<decimal>(type: "decimal(20,4)", nullable: true),
                    con_value_upper = table.Column<decimal>(type: "decimal(20,4)", nullable: true),
                    conditionUpper = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isforcecheck = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    reftable = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    refid = table.Column<long>(type: "bigint", nullable: true),
                    value_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    eff_subworkflowid = table.Column<long>(type: "bigint", nullable: true),
                    eff_wlevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    workflowid = table.Column<long>(type: "bigint", nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    types = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_Condition_1", x => x.subworkflowid);
                });

            migrationBuilder.CreateTable(
                name: "wf_customer_approver",
                columns: table => new
                {
                    subworkflowid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id = table.Column<long>(type: "bigint", nullable: false),
                    modate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<long>(type: "bigint", nullable: true),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true),
                    enddate = table.Column<DateOnly>(type: "date", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    workflowid = table.Column<long>(type: "bigint", nullable: true),
                    wlevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    emplevel = table.Column<int>(type: "int", nullable: true),
                    poscode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_CustomApprover", x => x.subworkflowid);
                });

            migrationBuilder.CreateTable(
                name: "wf_emailTemplate",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    workflowid = table.Column<long>(type: "bigint", nullable: true),
                    wlevelTarget = table.Column<int>(type: "int", nullable: true),
                    wlevelFrom = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    param = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    files = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    file_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    paramNo = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_emailTemplate", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_employee",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    empid = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    titleid = table.Column<int>(type: "int", nullable: true),
                    picname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    firstname_th = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    midname_th = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    lastname_th = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    nickname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    cardid = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    sexid = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    birthdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    nation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    firstname_en = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    midname_en = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    lastname_en = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    start_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    end_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    employeetype = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgcodefull = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgname_th = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    orgname_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    wlevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_employee", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_loa",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    loaid = table.Column<long>(type: "bigint", nullable: false),
                    wfid = table.Column<long>(type: "bigint", nullable: false),
                    levelcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    min = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    max = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    nowWorkflowid = table.Column<long>(type: "bigint", nullable: false),
                    nextWorkflowId = table.Column<long>(type: "bigint", nullable: false),
                    nowLevel = table.Column<int>(type: "int", nullable: false),
                    nextLevel = table.Column<int>(type: "int", nullable: false),
                    descript = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    descriptEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    subjectEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_LOA", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_loa_user",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    loaid = table.Column<long>(type: "bigint", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<long>(type: "bigint", nullable: true),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true),
                    enddate = table.Column<DateOnly>(type: "date", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    workflowid = table.Column<long>(type: "bigint", nullable: false),
                    wlevel = table.Column<int>(type: "int", nullable: false),
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    poscode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    costcenter = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    emplevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgrole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "wf_mas_reason",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_mas_reason", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_organize",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    orgcodefull = table.Column<int>(type: "int", nullable: true),
                    uppercode = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    abbname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    upperorgname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    abbupperorgname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    eng_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    eng_abbname = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    eng_upperorgname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    eng_abbupperorgname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    financialOrgcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ismanpower = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    istop = table.Column<string>(type: "nchar(1)", fixedLength: true, maxLength: 1, nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bossid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    acting_bossid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_Organize", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_org_type",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    orgcodefull = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    istoplevel = table.Column<bool>(type: "bit", nullable: false),
                    abbname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    lang = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    levelno = table.Column<int>(type: "int", nullable: true),
                    codetype = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    upperorg = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_OrgType", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_sub_workflow_master",
                columns: table => new
                {
                    subworkflowid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    workflowid = table.Column<long>(type: "bigint", nullable: false),
                    wlevel = table.Column<int>(type: "int", nullable: false),
                    isAdhocUser = table.Column<bool>(type: "bit", nullable: false),
                    iscustomApprover = table.Column<bool>(type: "bit", nullable: false),
                    isupperrole = table.Column<bool>(type: "bit", nullable: false),
                    isupperuser = table.Column<bool>(type: "bit", nullable: false),
                    iscustomRole = table.Column<bool>(type: "bit", nullable: false),
                    iscustomUser = table.Column<bool>(type: "bit", nullable: false),
                    iscondition = table.Column<bool>(type: "bit", nullable: false),
                    isorcondition = table.Column<bool>(type: "bit", nullable: false),
                    isandcondition = table.Column<bool>(type: "bit", nullable: false),
                    andpercent = table.Column<decimal>(type: "decimal(6,2)", nullable: true, defaultValueSql: "(NULL)"),
                    forwardstatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    standstatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    backwardstatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    approvedstatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    declinestatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    istop = table.Column<bool>(type: "bit", nullable: false),
                    remark = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<long>(type: "bigint", nullable: true, defaultValueSql: "(NULL)"),
                    isReturnSender = table.Column<bool>(type: "bit", nullable: false),
                    empLevel = table.Column<int>(type: "int", nullable: true),
                    isshow = table.Column<bool>(type: "bit", nullable: false),
                    isLOA = table.Column<bool>(type: "bit", nullable: false),
                    loacode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    wfcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isAutoApproveAllow = table.Column<bool>(type: "bit", nullable: false),
                    isNeedBudgetApproval = table.Column<bool>(type: "bit", nullable: false),
                    isPool = table.Column<bool>(type: "bit", nullable: false),
                    backwardlevel = table.Column<int>(type: "int", nullable: true),
                    sitinstatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    aa_id = table.Column<int>(type: "int", nullable: true),
                    aa_level = table.Column<int>(type: "int", nullable: true),
                    controller = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    action = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    displayName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    userid1 = table.Column<long>(type: "bigint", nullable: true),
                    userid2 = table.Column<long>(type: "bigint", nullable: true),
                    userid3 = table.Column<long>(type: "bigint", nullable: true),
                    subjectBiz = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    describeBiz = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    describe = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    actionEdit = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    subject_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    subjectBiz_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    describeBiz_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    describe_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isApproverSameOrg = table.Column<bool>(type: "bit", nullable: false),
                    isApproverSameCostCenter = table.Column<bool>(type: "bit", nullable: false),
                    isManualButton = table.Column<bool>(type: "bit", nullable: false),
                    ApproveController = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ApproveAction = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__wf_subwo__0D16B5CDB3240F76", x => x.subworkflowid);
                });

            migrationBuilder.CreateTable(
                name: "wf_workflow",
                columns: table => new
                {
                    workflowid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    wname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    wstatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    wstartdate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(NULL)"),
                    wenddate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(NULL)"),
                    wexpireday = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, defaultValueSql: "(NULL)"),
                    c_detail = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    c_create = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    c_edit = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    c_list = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    isshow = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "(NULL)"),
                    isactive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "(NULL)"),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    tablename = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    lifetimeday = table.Column<decimal>(type: "decimal(10,2)", nullable: true, defaultValueSql: "(NULL)"),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "(NULL)"),
                    columnref = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    tableref = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    create_mothod = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    edit_mothod = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    view_mothod = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    list_mothod = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    workflowcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    icon = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<byte[]>(type: "varbinary(250)", maxLength: 250, nullable: true),
                    wgroup = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    abbname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    expecttime = table.Column<int>(type: "int", nullable: true),
                    expecttimeUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    expectinBiz = table.Column<bool>(type: "bit", nullable: true),
                    param = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRejectToReq = table.Column<bool>(type: "bit", nullable: true),
                    controller = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    action = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    wname_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__wf_workf__D00EA011B75E9C00", x => x.workflowid);
                });

            migrationBuilder.CreateTable(
                name: "wf_workflow_in_workflow",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    workflowid = table.Column<long>(type: "bigint", nullable: false),
                    wlevel = table.Column<int>(type: "int", nullable: false),
                    isNeedTrue = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_workflow_in_workflow", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sc_role",
                columns: table => new
                {
                    roleid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    abbr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, defaultValueSql: "(NULL)"),
                    rolelevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    upperrole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    rolecode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    isactive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "(NULL)"),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    isHeader = table.Column<bool>(type: "bit", nullable: false),
                    ref1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ref2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__sc_role__CD994BF2188A25C3", x => x.roleid);
                    table.ForeignKey(
                        name: "FK_sc_role_com_company_company_id",
                        column: x => x.company_id,
                        principalTable: "com_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sc_user",
                columns: table => new
                {
                    userid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    firstname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false, defaultValueSql: "(NULL)"),
                    lastname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false, defaultValueSql: "(NULL)"),
                    loginname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false, defaultValueSql: "(NULL)"),
                    password = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    phone = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    mobilephone = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    isforcechanged = table.Column<bool>(type: "bit", nullable: false),
                    isdisable = table.Column<bool>(type: "bit", nullable: false),
                    iscancel = table.Column<bool>(type: "bit", nullable: false),
                    pwdexpdate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(NULL)"),
                    lasttimelogin = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(NULL)"),
                    invalidpwcount = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    lastinvalidpwd = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(NULL)"),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    upperuserid = table.Column<long>(type: "bigint", nullable: true, defaultValueSql: "(NULL)"),
                    orgname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(NULL)"),
                    enddate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(NULL)"),
                    remindpwd = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    social = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    langcode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, defaultValueSql: "(NULL)"),
                    sex_sexid = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    title_titleid = table.Column<long>(type: "bigint", nullable: true, defaultValueSql: "(NULL)"),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    orgid = table.Column<long>(type: "bigint", nullable: true, defaultValueSql: "(NULL)"),
                    isroot = table.Column<bool>(type: "bit", nullable: false),
                    email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    isEmployee = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    poscode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pos_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActivate = table.Column<bool>(type: "bit", nullable: false),
                    vendorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isVendor = table.Column<bool>(type: "bit", nullable: false),
                    isAccept = table.Column<bool>(type: "bit", nullable: false),
                    vendorid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    supervisor = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    costcenter = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    verifycode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    registerdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    isHeader = table.Column<bool>(type: "bit", nullable: false),
                    ref1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ref2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    salt = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Companycode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sc_user_userid", x => x.userid);
                    table.ForeignKey(
                        name: "FK_sc_user_com_company_company_id",
                        column: x => x.company_id,
                        principalTable: "com_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CT_Contract",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    po_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    constract_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vendor_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vendor_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    tax_id = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    requester = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    requestercode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    requesterOrg = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    requester_email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expired_date = table.Column<DateOnly>(type: "date", nullable: true),
                    isNeedExtends = table.Column<bool>(type: "bit", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectAmount = table.Column<decimal>(type: "money", nullable: true),
                    ProjectUtilization = table.Column<decimal>(type: "money", nullable: true),
                    ProjectRemaining = table.Column<decimal>(type: "money", nullable: true),
                    ProjectCurencyID = table.Column<long>(type: "bigint", nullable: true),
                    isWarrantyRequired = table.Column<bool>(type: "bit", nullable: true),
                    WarrantyType = table.Column<long>(type: "bigint", nullable: true),
                    WarrantyStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    WarrantyEnddate = table.Column<DateOnly>(type: "date", nullable: true),
                    isBankGuarantee = table.Column<bool>(type: "bit", nullable: true),
                    BankGuaranteeAmount = table.Column<decimal>(type: "money", nullable: true),
                    BankGuaranteeEffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BankGuaranteeExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BankGuaranteeRemark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BankGuaranteeCurrencyID = table.Column<long>(type: "bigint", nullable: true),
                    isInsurancePolicyRequired = table.Column<bool>(type: "bit", nullable: true),
                    InsurancePolicyType = table.Column<long>(type: "bigint", nullable: true),
                    InsurancePolicyAmountCoverage = table.Column<decimal>(type: "money", nullable: true),
                    InsurancePolicyEffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InsurancePolicyExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InsurancePolicyRemark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InsurancePolicyCurrencyID = table.Column<long>(type: "bigint", nullable: true),
                    isContractDocumentStatusComplete = table.Column<bool>(type: "bit", nullable: true),
                    DateNotice = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RemainPeriod = table.Column<TimeOnly>(type: "time", nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CT_Contract", x => x.id);
                    table.ForeignKey(
                        name: "FK_CT_Contract_BankGuarantee",
                        column: x => x.BankGuaranteeCurrencyID,
                        principalTable: "Currency",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_CT_Contract_Currency_insurrance",
                        column: x => x.InsurancePolicyCurrencyID,
                        principalTable: "Currency",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_CT_Contract_Project",
                        column: x => x.ProjectCurencyID,
                        principalTable: "Currency",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vd_general_info",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    taxid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    vendorcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    first_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    mid_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    last_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    first_name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    midname_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    last_name_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    title = table.Column<long>(type: "bigint", nullable: true),
                    title_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    email2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    username = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    code = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    islocal = table.Column<bool>(type: "bit", nullable: true),
                    countrycode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    website = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    mobile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    mobile2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    register_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: true),
                    enddate = table.Column<DateOnly>(type: "date", nullable: true),
                    remark_status = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: true),
                    areatype = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    branchcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isApprove = table.Column<bool>(type: "bit", nullable: true),
                    ApproveDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ApproveBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isLast = table.Column<bool>(type: "bit", nullable: true),
                    birthdate = table.Column<DateOnly>(type: "date", nullable: true),
                    capitalRegister = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    capitalCurrencyid = table.Column<long>(type: "bigint", nullable: true),
                    signedName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    signedPosition = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    signedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    fax = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    extPhone = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    extFax = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isAcceptCondition1 = table.Column<bool>(type: "bit", nullable: true),
                    isAcceptCondition2 = table.Column<bool>(type: "bit", nullable: true),
                    isAccept1Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    isAccept2Date = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vd_Vendor", x => x.id);
                    table.ForeignKey(
                        name: "FK_vd_general_info_vd_general_info",
                        column: x => x.capitalCurrencyid,
                        principalTable: "Currency",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "mas_province",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    countryID = table.Column<long>(type: "bigint", nullable: false),
                    countryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    nameEN = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    nameTH = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    stateCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mas_province", x => x.id);
                    table.ForeignKey(
                        name: "FK_mas_province_mas_country",
                        column: x => x.countryID,
                        principalTable: "mas_country",
                        principalColumn: "countryid");
                });

            migrationBuilder.CreateTable(
                name: "doc_center",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    refno = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isRequired = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    mas_doc_type_id = table.Column<long>(type: "bigint", nullable: true),
                    doctypecode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    refid = table.Column<long>(type: "bigint", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    isInclude = table.Column<bool>(type: "bit", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    files = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    subMode1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isDefaultRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doc_center", x => x.id);
                    table.ForeignKey(
                        name: "FK_doc_center_mas_doc_type",
                        column: x => x.mas_doc_type_id,
                        principalTable: "mas_doc_type",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "DocCheckList",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    masdocid = table.Column<long>(type: "bigint", nullable: false),
                    isMandatory = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    mode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    refcode1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    refcode2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true),
                    expiredate = table.Column<DateOnly>(type: "date", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    secretLevel = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    fileref = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    subMode1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orderTH = table.Column<int>(type: "int", nullable: false, defaultValue: 99)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocCheckList", x => x.id);
                    table.ForeignKey(
                        name: "FK_DocCheckList_mas_doc_type",
                        column: x => x.masdocid,
                        principalTable: "mas_doc_type",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "his_doc",
                columns: table => new
                {
                    his_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id = table.Column<long>(type: "bigint", nullable: false),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: false),
                    wlevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    createby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    secretlevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    path = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    filename = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    filetype = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    servername = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    order_th = table.Column<int>(type: "int", nullable: true),
                    role_allow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isAllAccess = table.Column<bool>(type: "bit", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    approveby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doctype_id = table.Column<long>(type: "bigint", nullable: true),
                    job_type = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    sourceName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sourceID = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sourceNameSub1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sourceNameSub2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sourceNameSub3 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sourceID2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_his_doc_1", x => x.his_id);
                    table.ForeignKey(
                        name: "FK_his_doc_mas_doc_type",
                        column: x => x.doctype_id,
                        principalTable: "mas_doc_type",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pc_vendor_RFQDocRequest",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rfqno = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isRequired = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doctypeid = table.Column<long>(type: "bigint", nullable: true),
                    doctypecode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    rfqid = table.Column<long>(type: "bigint", nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    isInclude = table.Column<bool>(type: "bit", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    files = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    subMode1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    createby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isAproved = table.Column<bool>(type: "bit", nullable: true),
                    ApproveBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ApproveDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_vendor_RFQDocRequest", x => x.id);
                    table.ForeignKey(
                        name: "FK_pc_vendor_RFQDocRequest_mas_doc_type",
                        column: x => x.doctypeid,
                        principalTable: "mas_doc_type",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "util_document",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: true),
                    wlevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    createby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    secretlevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    path = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    filename = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    filetype = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    servername = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    order_th = table.Column<int>(type: "int", nullable: true),
                    role_allow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isAllAccess = table.Column<bool>(type: "bit", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    approveby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doctype_id = table.Column<long>(type: "bigint", nullable: true),
                    job_type = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    userid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    sourceName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sourceID = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sourceNameSub1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sourceNameSub2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sourceNameSub3 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sourceID2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jb_document", x => x.id);
                    table.ForeignKey(
                        name: "FK_jb_document_DPDocType",
                        column: x => x.doctype_id,
                        principalTable: "mas_doc_type",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pc_RFQDocRequest",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rfqno = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isRequired = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doctypeid = table.Column<long>(type: "bigint", nullable: true),
                    doctypecode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    rfqid = table.Column<long>(type: "bigint", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    isInclude = table.Column<bool>(type: "bit", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    files = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    subMode1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isDefaultRequired = table.Column<bool>(type: "bit", nullable: false),
                    orderTh = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_RFQDocRequest", x => x.id);
                    table.ForeignKey(
                        name: "FK_pc_RFQDocRequest_pc_RFQDocRequest",
                        column: x => x.doctypeid,
                        principalTable: "mas_doc_type",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_pc_RFQDocRequest_pc_rfq",
                        column: x => x.rfqid,
                        principalTable: "pc_rfq",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pc_rfqItem",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rfq_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    rfq_itemno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PRNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    pr_itemno = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    net_amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    descript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    mat_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    unit_type = table.Column<long>(type: "bigint", nullable: true),
                    unit_amount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    remark_special = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ticketNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    qty = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    rfqid = table.Column<long>(type: "bigint", nullable: true),
                    prid = table.Column<long>(type: "bigint", nullable: true),
                    uom = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isSelect = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CURRENCY_KEY = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    VALUE_PRICE = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0.00m),
                    PRICE_UNIT = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0.00m),
                    TOTAL_VALUE = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0.00m),
                    requestor_empid = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_rfqItem", x => x.id);
                    table.ForeignKey(
                        name: "FK_pc_rfqItem_pc_rfq",
                        column: x => x.rfqid,
                        principalTable: "pc_rfq",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pc_te_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    techcriteria_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pr_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    pr_item = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    no1 = table.Column<int>(type: "int", nullable: true),
                    no2 = table.Column<int>(type: "int", nullable: true),
                    no3 = table.Column<int>(type: "int", nullable: true),
                    criteria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doc1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    doc2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    doc3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    te_comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isPassCondition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    point = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    point_evaluate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    prNo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PrTicketNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrItemNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    condition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    approveBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approveDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    pc_te_id = table.Column<long>(type: "bigint", nullable: true),
                    topicNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_TECriteria", x => x.id);
                    table.ForeignKey(
                        name: "FK_pc_te_item_PcTe",
                        column: x => x.pc_te_id,
                        principalTable: "pc_te",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pc_pr",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PRNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    total = table.Column<decimal>(type: "money", nullable: true),
                    total_net = table.Column<decimal>(type: "money", nullable: true),
                    createby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    vat = table.Column<decimal>(type: "money", nullable: true),
                    isVat = table.Column<bool>(type: "bit", nullable: true),
                    vat_amount = table.Column<decimal>(type: "money", nullable: true),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    costcenter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    prCreateDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    prReleaseDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    prStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    prSAPstatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pr_service_type_id = table.Column<long>(type: "bigint", nullable: true),
                    requestor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    requestor_empid = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    fileTe = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    approveTicketDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    isApproved = table.Column<bool>(type: "bit", nullable: true),
                    createid = table.Column<long>(type: "bigint", nullable: true),
                    requstorid = table.Column<long>(type: "bigint", nullable: true),
                    SAPRequisitioner = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    userid = table.Column<long>(type: "bigint", nullable: true),
                    approvedby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doc1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc1path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc2path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc3 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc3path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc4 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc4path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc5 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    doc5path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_pr", x => x.id);
                    table.ForeignKey(
                        name: "FK_pc_pr_service_type",
                        column: x => x.pr_service_type_id,
                        principalTable: "pr_service_type",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sc_menu",
                columns: table => new
                {
                    menuid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    menuname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    menulevel = table.Column<int>(type: "int", nullable: false),
                    isfinal = table.Column<bool>(type: "bit", nullable: false),
                    menuorder = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    menucode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "(NULL)"),
                    langcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValueSql: "(NULL)"),
                    isshow = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "(NULL)"),
                    tooltip = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(NULL)"),
                    enddate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(NULL)"),
                    programid = table.Column<long>(type: "bigint", nullable: true, defaultValueSql: "(NULL)"),
                    icon = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    small_icon = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    url = table.Column<string>(type: "text", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    uppermenucode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    menugroupid = table.Column<long>(type: "bigint", nullable: false),
                    isactive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "(NULL)"),
                    method_action = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    menuname_en = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__sc_menu__3B5F7D5C109083C8", x => x.menuid);
                    table.ForeignKey(
                        name: "FK_SC_Menu_sc_menugroup",
                        column: x => x.menugroupid,
                        principalTable: "sc_menugroup",
                        principalColumn: "menugroupid");
                });

            migrationBuilder.CreateTable(
                name: "task_assign",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    assign_empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    assign_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    assignee_empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    assignee_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    assign_time = table.Column<DateTime>(type: "datetime", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    place = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    approve_by = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approve_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    lat = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    lon = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    description2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    description3 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    taskid = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_assign", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_assign_Task_Master",
                        column: x => x.taskid,
                        principalTable: "task_master",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "wf_button",
                columns: table => new
                {
                    wfbuttonid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    btname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    bt_type = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    bcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isshow = table.Column<bool>(type: "bit", nullable: true),
                    showwhenstatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    class_style = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    button_masterid = table.Column<long>(type: "bigint", nullable: false),
                    istop = table.Column<bool>(type: "bit", nullable: true),
                    isStart = table.Column<bool>(type: "bit", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    workflowid = table.Column<long>(type: "bigint", nullable: true),
                    wlevel = table.Column<int>(type: "int", nullable: true),
                    subworkflowid = table.Column<long>(type: "bigint", nullable: true),
                    isAndCondition = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_Button", x => x.wfbuttonid);
                    table.ForeignKey(
                        name: "FK_wf_button_wf_button_master",
                        column: x => x.button_masterid,
                        principalTable: "wf_button_master",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "wf_budget",
                columns: table => new
                {
                    wfcondiionid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subworkflowid = table.Column<long>(type: "bigint", nullable: true),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approveLevel = table.Column<int>(type: "int", nullable: false),
                    con_type = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    wlevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    conditionLower = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    con_value_lower = table.Column<decimal>(type: "decimal(20,4)", nullable: true),
                    con_value_upper = table.Column<decimal>(type: "decimal(20,4)", nullable: true),
                    conditionUpper = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isforcecheck = table.Column<bool>(type: "bit", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    reftable = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    refid = table.Column<long>(type: "bigint", nullable: true),
                    value_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    eff_subworkflowid = table.Column<long>(type: "bigint", nullable: true),
                    eff_wlevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    workflowid = table.Column<long>(type: "bigint", nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    types = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_CONDITION", x => x.wfcondiionid);
                    table.ForeignKey(
                        name: "FK_WF_CONDI_REFERENCE_WF_SUBWO",
                        column: x => x.subworkflowid,
                        principalTable: "wf_sub_workflow_master",
                        principalColumn: "subworkflowid");
                });

            migrationBuilder.CreateTable(
                name: "wf_custom_role",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subworkflowid = table.Column<long>(type: "bigint", nullable: true),
                    modate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(NULL)"),
                    modby = table.Column<long>(type: "bigint", nullable: true, defaultValueSql: "(NULL)"),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(NULL)"),
                    enddate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(NULL)"),
                    isactive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "(NULL)"),
                    workflowid = table.Column<long>(type: "bigint", nullable: false),
                    wlevel = table.Column<int>(type: "int", nullable: false),
                    roleid = table.Column<long>(type: "bigint", nullable: false),
                    rolecode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    emplevel = table.Column<int>(type: "int", nullable: true),
                    poscode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    loaid = table.Column<long>(type: "bigint", nullable: true),
                    isHeader = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_CUSTOMROLE", x => x.id);
                    table.ForeignKey(
                        name: "FK_WF_CUSTO_REFERENCE_WF_SUBWO",
                        column: x => x.subworkflowid,
                        principalTable: "wf_sub_workflow_master",
                        principalColumn: "subworkflowid");
                });

            migrationBuilder.CreateTable(
                name: "wf_decision_status",
                columns: table => new
                {
                    workflowstatusid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StepType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ButtonName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ButtonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ControlWF = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    methodWF = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    statuscode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    bizstatuscode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Workflowid = table.Column<long>(type: "bigint", nullable: true),
                    subworkflowid = table.Column<long>(type: "bigint", nullable: true),
                    wlevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moveType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDecisionStatus", x => x.workflowstatusid);
                    table.ForeignKey(
                        name: "FK_WF_DecisionStatus_ToSWF",
                        column: x => x.subworkflowid,
                        principalTable: "wf_sub_workflow_master",
                        principalColumn: "subworkflowid");
                });

            migrationBuilder.CreateTable(
                name: "job_master",
                columns: table => new
                {
                    jobmasterid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    jobmastername = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    subject = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    workflowid = table.Column<long>(type: "bigint", nullable: false),
                    maxlevel = table.Column<int>(type: "int", nullable: true),
                    createdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    expirydate = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    workflowcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    jobrefid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    reftable = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    refid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    lastLevel = table.Column<int>(type: "int", nullable: true),
                    ReqEmplD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    reqno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    bizstatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    reqdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    reqdept = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    reqforUserid = table.Column<long>(type: "bigint", nullable: true),
                    reqamont = table.Column<decimal>(type: "decimal(20,4)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    reqName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    reqForName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    wname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    barcodeID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    or_and = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    percent_and = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    createby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    createuserid = table.Column<long>(type: "bigint", nullable: true),
                    lastReqID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    createusername = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    reqOrg = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isJobClosed = table.Column<bool>(type: "bit", nullable: true),
                    reasonClosed = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approvedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    approvedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    approvedUserID = table.Column<long>(type: "bigint", nullable: true),
                    jobseq = table.Column<int>(type: "int", nullable: true),
                    jobmastername_en = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    costcenter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    loaid = table.Column<long>(type: "bigint", nullable: true),
                    reqForNameEN = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JOBMASTER", x => x.jobmasterid)
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_job_master_wf_workflow",
                        column: x => x.workflowid,
                        principalTable: "wf_workflow",
                        principalColumn: "workflowid");
                });

            migrationBuilder.CreateTable(
                name: "sc_role_program",
                columns: table => new
                {
                    roleprogid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    roleid = table.Column<long>(type: "bigint", nullable: true),
                    progid = table.Column<long>(type: "bigint", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLE_PROGRAM", x => x.roleprogid);
                    table.ForeignKey(
                        name: "FK_ROLE_PRO_REFERENCE_SC_PROGR",
                        column: x => x.progid,
                        principalTable: "sc_program",
                        principalColumn: "progid");
                    table.ForeignKey(
                        name: "FK_ROLE_PRO_REFERENCE_SC_ROLE",
                        column: x => x.roleid,
                        principalTable: "sc_role",
                        principalColumn: "roleid");
                });

            migrationBuilder.CreateTable(
                name: "emp_checkin",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    checkin_time = table.Column<DateTime>(type: "datetime", nullable: true),
                    lat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    lon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    place = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    ismain = table.Column<bool>(type: "bit", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isAdjust = table.Column<bool>(type: "bit", nullable: true),
                    checkin_time_stamp = table.Column<DateTime>(type: "datetime", nullable: true),
                    isIn = table.Column<bool>(type: "bit", nullable: true),
                    isOut = table.Column<bool>(type: "bit", nullable: true),
                    macAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    orgName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    files = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    path = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    checkouttime = table.Column<DateTime>(type: "datetime", nullable: true),
                    checkoutid = table.Column<long>(type: "bigint", nullable: true),
                    workhour = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    workminute = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    checkinasDate = table.Column<DateOnly>(type: "date", nullable: true),
                    name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emp_checkin", x => x.id);
                    table.ForeignKey(
                        name: "FK_emp_checkin_sc_user",
                        column: x => x.userid,
                        principalTable: "sc_user",
                        principalColumn: "userid");
                });

            migrationBuilder.CreateTable(
                name: "sc_user_role",
                columns: table => new
                {
                    user_roleID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    roleid = table.Column<long>(type: "bigint", nullable: false),
                    modate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(NULL)"),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)"),
                    isactive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "(NULL)"),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(NULL)"),
                    enddate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(NULL)"),
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__sc_userr__BE814DEE1C50ACD6", x => x.user_roleID);
                    table.ForeignKey(
                        name: "FK_SC_USER_ROLE_SC_Role",
                        column: x => x.roleid,
                        principalTable: "sc_role",
                        principalColumn: "roleid");
                    table.ForeignKey(
                        name: "FK_SC_USER_ROLE_SC_USER",
                        column: x => x.userid,
                        principalTable: "sc_user",
                        principalColumn: "userid");
                });

            migrationBuilder.CreateTable(
                name: "wf_custom_user",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subworkflowid = table.Column<long>(type: "bigint", nullable: false),
                    workflowid = table.Column<long>(type: "bigint", nullable: false),
                    wlevel = table.Column<int>(type: "int", nullable: false),
                    userid = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "(NULL)"),
                    modate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(NULL)"),
                    modby = table.Column<long>(type: "bigint", nullable: true, defaultValueSql: "(NULL)"),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    emplevel = table.Column<int>(type: "int", nullable: true),
                    loaid = table.Column<long>(type: "bigint", nullable: true),
                    isCoMember = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_Customuser_1", x => x.id);
                    table.ForeignKey(
                        name: "FK_WF_CUSTO_USR_REFERENCE_WF_SUBWO",
                        column: x => x.subworkflowid,
                        principalTable: "wf_sub_workflow_master",
                        principalColumn: "subworkflowid");
                    table.ForeignKey(
                        name: "FK_wf_custom_user_sc_user",
                        column: x => x.userid,
                        principalTable: "sc_user",
                        principalColumn: "userid");
                });

            migrationBuilder.CreateTable(
                name: "pc_RFQServiceVendor",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vendorid = table.Column<long>(type: "bigint", nullable: false),
                    vendorcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    rfqID = table.Column<long>(type: "bigint", nullable: false),
                    rfqNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    serviceid = table.Column<long>(type: "bigint", nullable: true),
                    servicecode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    emaildate = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: true),
                    link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    isAttachQuotation = table.Column<bool>(type: "bit", nullable: true),
                    isAttachTechDoc = table.Column<bool>(type: "bit", nullable: true),
                    email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    submitDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    userid = table.Column<long>(type: "bigint", nullable: true),
                    jobstatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    workflowid = table.Column<long>(type: "bigint", nullable: true),
                    wlevel = table.Column<int>(type: "int", nullable: true),
                    submitBiddingDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    submitBiddingBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isVendor = table.Column<bool>(type: "bit", nullable: true),
                    NickName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isBidWin = table.Column<bool>(type: "bit", nullable: true),
                    isDocComplete = table.Column<bool>(type: "bit", nullable: true),
                    ApproveBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ApproveDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    isTEComplete = table.Column<bool>(type: "bit", nullable: true),
                    teid = table.Column<long>(type: "bigint", nullable: true),
                    remark_add = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isTePass = table.Column<bool>(type: "bit", nullable: true),
                    ApproveTEBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ApproveTEDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    TeCondition = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    totalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    vendorComment = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    isBidAllow = table.Column<bool>(type: "bit", nullable: true),
                    PriceApproveBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isPriceApprove = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    isRequstClarify = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    negoCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    isRequisitionState = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_RFQServiceVendor", x => x.id);
                    table.ForeignKey(
                        name: "FK_pc_RFQServiceVendor_pc_rfq",
                        column: x => x.rfqID,
                        principalTable: "pc_rfq",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_pc_RFQServiceVendor_vd_general_info",
                        column: x => x.vendorid,
                        principalTable: "vd_general_info",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vd_doc",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    doc_type = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doc_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    doc_date = table.Column<DateOnly>(type: "date", nullable: true),
                    doc_expire_date = table.Column<DateOnly>(type: "date", nullable: true),
                    filename = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    file_path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    secureLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    isMandatory = table.Column<bool>(type: "bit", nullable: false),
                    taxid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: false),
                    doctypeid = table.Column<long>(type: "bigint", nullable: false),
                    reftype = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vd_doc", x => x.id);
                    table.ForeignKey(
                        name: "FK_vd_doc_mas_doc_type",
                        column: x => x.doctypeid,
                        principalTable: "mas_doc_type",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_vd_doc_vd_general_info",
                        column: x => x.vendorid,
                        principalTable: "vd_general_info",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vd_address",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    address_type = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    address_no = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    building = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sub_district = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    district = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    province = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    countrycode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    zipcode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: true),
                    vendorid = table.Column<long>(type: "bigint", nullable: true),
                    taxid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    address_type_id = table.Column<long>(type: "bigint", nullable: true),
                    countryid = table.Column<long>(type: "bigint", nullable: true),
                    provinceid = table.Column<long>(type: "bigint", nullable: true),
                    road = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vd_address", x => x.id);
                    table.ForeignKey(
                        name: "FK_vd_address_mas_address_type",
                        column: x => x.address_type_id,
                        principalTable: "mas_address_type",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_vd_address_mas_country",
                        column: x => x.countryid,
                        principalTable: "mas_country",
                        principalColumn: "countryid");
                    table.ForeignKey(
                        name: "FK_vd_address_mas_province",
                        column: x => x.provinceid,
                        principalTable: "mas_province",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pc_pr_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PRNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    itemno = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    net_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    descript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    mat_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    mas_unit_type_id = table.Column<long>(type: "bigint", nullable: true),
                    unit_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    currencyID = table.Column<long>(type: "bigint", nullable: true),
                    SAPRequistiner = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    trackingNo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    file1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    file2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    file1_path = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    file2_path = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isAvailable = table.Column<bool>(type: "bit", nullable: true),
                    pc_pr_id = table.Column<long>(type: "bigint", nullable: true),
                    SAPstatus = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DOC_TYPE = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DOC_CATEGORY = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PLANT = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PR_GROUP = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SHORT_TEXT = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ITEM_CATEGORY = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    UOM = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    REQUEST_DATE = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RELEASE_DATE = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    VALUE_PRICE = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PRICE_UNIT = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CURRENCY_KEY = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RELEASE_STATUS = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TOTAL_VALUE = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    COST_CENTER = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ITEM_TXT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MATERIAL_PO_TXT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    rfqid = table.Column<long>(type: "bigint", nullable: true),
                    rfq_itemID = table.Column<long>(type: "bigint", nullable: true),
                    isSelect = table.Column<bool>(type: "bit", nullable: false),
                    requestor_empid = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pc_PRItem", x => x.id);
                    table.ForeignKey(
                        name: "FK_pc_pr_item_pc_pr",
                        column: x => x.pc_pr_id,
                        principalTable: "pc_pr",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sc_role_menu",
                columns: table => new
                {
                    rolemenuid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    menuid = table.Column<long>(type: "bigint", nullable: false),
                    roleid = table.Column<long>(type: "bigint", nullable: false),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true),
                    enddate = table.Column<DateOnly>(type: "date", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "(NULL)"),
                    remark = table.Column<string>(type: "text", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValueSql: "(NULL)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__sc_rolem__6171F3F863B63A9E", x => x.rolemenuid);
                    table.ForeignKey(
                        name: "FK_SC_ROLE_MENU_SC_Menu",
                        column: x => x.menuid,
                        principalTable: "sc_menu",
                        principalColumn: "menuid");
                    table.ForeignKey(
                        name: "FK_SC_ROLE_MENU_SC_Role",
                        column: x => x.roleid,
                        principalTable: "sc_role",
                        principalColumn: "roleid");
                });

            migrationBuilder.CreateTable(
                name: "job_user_list",
                columns: table => new
                {
                    jobapproverid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: false),
                    reqno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    workflowid = table.Column<long>(type: "bigint", nullable: false),
                    wlevel = table.Column<int>(type: "int", nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    companycode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orgName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    reftable = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    subjobid = table.Column<long>(type: "bigint", nullable: true),
                    emplevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isSelected = table.Column<bool>(type: "bit", nullable: true),
                    approvedate = table.Column<DateTime>(type: "datetime", nullable: true),
                    recievedate = table.Column<DateTime>(type: "datetime", nullable: true),
                    istakejob = table.Column<bool>(type: "bit", nullable: true),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    jobstatus = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    subworkflowmasterid = table.Column<long>(type: "bigint", nullable: true),
                    isOr = table.Column<bool>(type: "bit", nullable: true),
                    isAnd = table.Column<bool>(type: "bit", nullable: true),
                    sendDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    isAutoApprove = table.Column<bool>(type: "bit", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    expectdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    point = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    jobtookdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    assignby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    assign_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    username = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    rolecode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    roleid = table.Column<long>(type: "bigint", nullable: true),
                    budget = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    budget_refer = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    userid = table.Column<long>(type: "bigint", nullable: true),
                    isLast = table.Column<bool>(type: "bit", nullable: true),
                    jobStatusName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    sendEmailDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    userIDSent = table.Column<long>(type: "bigint", nullable: true),
                    mas_reason_id = table.Column<long>(type: "bigint", nullable: true),
                    jobseq = table.Column<int>(type: "int", nullable: true),
                    andPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    isHeader = table.Column<bool>(type: "bit", nullable: true),
                    ref1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ref2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApprover", x => x.jobapproverid);
                    table.ForeignKey(
                        name: "FK_job_user_list_job_master",
                        column: x => x.jobmasterid,
                        principalTable: "job_master",
                        principalColumn: "jobmasterid");
                    table.ForeignKey(
                        name: "FK_job_user_list_mas_reason1",
                        column: x => x.mas_reason_id,
                        principalTable: "mas_reason",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_job_user_list_sc_user",
                        column: x => x.userid,
                        principalTable: "sc_user",
                        principalColumn: "userid");
                    table.ForeignKey(
                        name: "FK_job_user_list_wf_sub_workflow_master",
                        column: x => x.subworkflowmasterid,
                        principalTable: "wf_sub_workflow_master",
                        principalColumn: "subworkflowid");
                });

            migrationBuilder.CreateTable(
                name: "wf_adhoc_user",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subworkflowid = table.Column<long>(type: "bigint", nullable: false),
                    workflowid = table.Column<long>(type: "bigint", nullable: false),
                    wlevel = table.Column<int>(type: "int", nullable: false),
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    modate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<long>(type: "bigint", nullable: true),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    emplevel = table.Column<int>(type: "int", nullable: true),
                    jobmasterid = table.Column<long>(type: "bigint", nullable: true),
                    orderTh = table.Column<int>(type: "int", nullable: true),
                    mode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    prid = table.Column<long>(type: "bigint", nullable: true),
                    rfqid = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_adhoc_user", x => x.id);
                    table.ForeignKey(
                        name: "FK_wf_adhoc_user_job_master",
                        column: x => x.jobmasterid,
                        principalTable: "job_master",
                        principalColumn: "jobmasterid");
                    table.ForeignKey(
                        name: "FK_wf_adhoc_user_sc_user",
                        column: x => x.userid,
                        principalTable: "sc_user",
                        principalColumn: "userid");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "([NormalizedName] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "([NormalizedUserName] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_CT_Contract_BankGuaranteeCurrencyID",
                table: "CT_Contract",
                column: "BankGuaranteeCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_CT_Contract_InsurancePolicyCurrencyID",
                table: "CT_Contract",
                column: "InsurancePolicyCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_CT_Contract_ProjectCurencyID",
                table: "CT_Contract",
                column: "ProjectCurencyID");

            migrationBuilder.CreateIndex(
                name: "IX_doc_center_mas_doc_type_id",
                table: "doc_center",
                column: "mas_doc_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_DocCheckList_masdocid",
                table: "DocCheckList",
                column: "masdocid");

            migrationBuilder.CreateIndex(
                name: "IX_emp_checkin_userid",
                table: "emp_checkin",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "companycode",
                table: "employee",
                column: "Companycode");

            migrationBuilder.CreateIndex(
                name: "employeeID",
                table: "employee",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "employeeID_CompanyCode",
                table: "employee",
                columns: new[] { "EmployeeID", "Companycode" });

            migrationBuilder.CreateIndex(
                name: "IX_his_doc_doctype_id",
                table: "his_doc",
                column: "doctype_id");

            migrationBuilder.CreateIndex(
                name: "HRUCFSALARYITEM_X",
                table: "HRUCFSALARYITEM",
                column: "SALITEM_CODE",
                unique: true,
                filter: "[SALITEM_CODE] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_job_master_workflowid",
                table: "job_master",
                column: "workflowid");

            migrationBuilder.CreateIndex(
                name: "IX_job_status",
                table: "job_status",
                column: "jobstatuscode");

            migrationBuilder.CreateIndex(
                name: "IX_job_user_list",
                table: "job_user_list",
                column: "jobstatus");

            migrationBuilder.CreateIndex(
                name: "IX_job_user_list_jobmasterid",
                table: "job_user_list",
                column: "jobmasterid");

            migrationBuilder.CreateIndex(
                name: "IX_job_user_list_mas_reason_id",
                table: "job_user_list",
                column: "mas_reason_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_user_list_subworkflowmasterid",
                table: "job_user_list",
                column: "subworkflowmasterid");

            migrationBuilder.CreateIndex(
                name: "IX_job_user_list_userid",
                table: "job_user_list",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_mas_province_countryID",
                table: "mas_province",
                column: "countryID");

            migrationBuilder.CreateIndex(
                name: "IX_pc_pr",
                table: "pc_pr",
                column: "PRNo");

            migrationBuilder.CreateIndex(
                name: "IX_pc_pr_pr_service_type_id",
                table: "pc_pr",
                column: "pr_service_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_pc_pr_item",
                table: "pc_pr_item",
                column: "PRNo");

            migrationBuilder.CreateIndex(
                name: "IX_pc_pr_item_pc_pr_id",
                table: "pc_pr_item",
                column: "pc_pr_id");

            migrationBuilder.CreateIndex(
                name: "IX_pc_RFQDocRequest_doctypeid",
                table: "pc_RFQDocRequest",
                column: "doctypeid");

            migrationBuilder.CreateIndex(
                name: "IX_pc_RFQDocRequest_rfqid",
                table: "pc_RFQDocRequest",
                column: "rfqid");

            migrationBuilder.CreateIndex(
                name: "IX_pc_rfqItem_rfqid",
                table: "pc_rfqItem",
                column: "rfqid");

            migrationBuilder.CreateIndex(
                name: "IX_pc_RFQServiceVendor_rfqID",
                table: "pc_RFQServiceVendor",
                column: "rfqID");

            migrationBuilder.CreateIndex(
                name: "IX_pc_RFQServiceVendor_vendorid",
                table: "pc_RFQServiceVendor",
                column: "vendorid");

            migrationBuilder.CreateIndex(
                name: "IX_pc_te_item_pc_te_id",
                table: "pc_te_item",
                column: "pc_te_id");

            migrationBuilder.CreateIndex(
                name: "IX_pc_vendor_RFQDocRequest_doctypeid",
                table: "pc_vendor_RFQDocRequest",
                column: "doctypeid");

            migrationBuilder.CreateIndex(
                name: "IX_sc_menu_menugroupid",
                table: "sc_menu",
                column: "menugroupid");

            migrationBuilder.CreateIndex(
                name: "sc_menugroup$programgroupid_UNIQUE",
                table: "sc_menugroup",
                column: "menugroupid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "sc_programgroup$programgroupid_UNIQUE",
                table: "sc_program_group",
                column: "programgroupid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sc_role_company_id",
                table: "sc_role",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_sc_role_menu_menuid",
                table: "sc_role_menu",
                column: "menuid");

            migrationBuilder.CreateIndex(
                name: "IX_sc_role_menu_roleid",
                table: "sc_role_menu",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "IX_sc_role_program_progid",
                table: "sc_role_program",
                column: "progid");

            migrationBuilder.CreateIndex(
                name: "IX_sc_role_program_roleid",
                table: "sc_role_program",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "IX_sc_user",
                table: "sc_user",
                columns: new[] { "userid", "password", "isEmployee" });

            migrationBuilder.CreateIndex(
                name: "IX_sc_user_company_id",
                table: "sc_user",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_sc_user_role_roleid",
                table: "sc_user_role",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "IX_sc_user_role_userid",
                table: "sc_user_role",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_task_assign_taskid",
                table: "task_assign",
                column: "taskid");

            migrationBuilder.CreateIndex(
                name: "IX_util_document_doctype_id",
                table: "util_document",
                column: "doctype_id");

            migrationBuilder.CreateIndex(
                name: "IX_vd_address_address_type_id",
                table: "vd_address",
                column: "address_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_vd_address_countryid",
                table: "vd_address",
                column: "countryid");

            migrationBuilder.CreateIndex(
                name: "IX_vd_address_provinceid",
                table: "vd_address",
                column: "provinceid");

            migrationBuilder.CreateIndex(
                name: "IX_vd_doc_doctypeid",
                table: "vd_doc",
                column: "doctypeid");

            migrationBuilder.CreateIndex(
                name: "IX_vd_doc_vendorid",
                table: "vd_doc",
                column: "vendorid");

            migrationBuilder.CreateIndex(
                name: "IX_vd_general_info_capitalCurrencyid",
                table: "vd_general_info",
                column: "capitalCurrencyid");

            migrationBuilder.CreateIndex(
                name: "IX_wf_adhoc_user_jobmasterid",
                table: "wf_adhoc_user",
                column: "jobmasterid");

            migrationBuilder.CreateIndex(
                name: "IX_wf_adhoc_user_userid",
                table: "wf_adhoc_user",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_wf_budget_subworkflowid",
                table: "wf_budget",
                column: "subworkflowid");

            migrationBuilder.CreateIndex(
                name: "IX_wf_button_button_masterid",
                table: "wf_button",
                column: "button_masterid");

            migrationBuilder.CreateIndex(
                name: "IX_wf_custom_role_subworkflowid",
                table: "wf_custom_role",
                column: "subworkflowid");

            migrationBuilder.CreateIndex(
                name: "IX_wf_custom_user_subworkflowid",
                table: "wf_custom_user",
                column: "subworkflowid");

            migrationBuilder.CreateIndex(
                name: "IX_wf_custom_user_userid",
                table: "wf_custom_user",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_wf_decision_status_subworkflowid",
                table: "wf_decision_status",
                column: "subworkflowid");

            migrationBuilder.CreateIndex(
                name: "IX_wf_sub_workflow_master",
                table: "wf_sub_workflow_master",
                columns: new[] { "wlevel", "workflowid" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ALL_EMPLOYEE");

            migrationBuilder.DropTable(
                name: "app_application_list");

            migrationBuilder.DropTable(
                name: "approve_level");

            migrationBuilder.DropTable(
                name: "approver_budget");

            migrationBuilder.DropTable(
                name: "app_serverInfo");

            migrationBuilder.DropTable(
                name: "AspNetRoleAspNetUser");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "asset_fa_ora");

            migrationBuilder.DropTable(
                name: "asset_notice");

            migrationBuilder.DropTable(
                name: "asset_owner");

            migrationBuilder.DropTable(
                name: "button_link");

            migrationBuilder.DropTable(
                name: "com_organization");

            migrationBuilder.DropTable(
                name: "com_organization_his");

            migrationBuilder.DropTable(
                name: "com_organize_layer");

            migrationBuilder.DropTable(
                name: "com_org_layer_group");

            migrationBuilder.DropTable(
                name: "com_position");

            migrationBuilder.DropTable(
                name: "ConsentHistory");

            migrationBuilder.DropTable(
                name: "CostCenterProcurement");

            migrationBuilder.DropTable(
                name: "CT_Contract");

            migrationBuilder.DropTable(
                name: "doc_center");

            migrationBuilder.DropTable(
                name: "DocCheckList");

            migrationBuilder.DropTable(
                name: "DocTypeMapping");

            migrationBuilder.DropTable(
                name: "emp_checkin");

            migrationBuilder.DropTable(
                name: "emp_checkout");

            migrationBuilder.DropTable(
                name: "employee");

            migrationBuilder.DropTable(
                name: "Employee_data");

            migrationBuilder.DropTable(
                name: "employeetype");

            migrationBuilder.DropTable(
                name: "emp_overtime_request");

            migrationBuilder.DropTable(
                name: "error_log_file");

            migrationBuilder.DropTable(
                name: "error_messege");

            migrationBuilder.DropTable(
                name: "HeadCode_V1");

            migrationBuilder.DropTable(
                name: "his_doc");

            migrationBuilder.DropTable(
                name: "his_doc_bak");

            migrationBuilder.DropTable(
                name: "Holiday");

            migrationBuilder.DropTable(
                name: "HRBASEPAYROLLFIXED");

            migrationBuilder.DropTable(
                name: "HREMPLOYEE");

            migrationBuilder.DropTable(
                name: "HREXTENUATETAX");

            migrationBuilder.DropTable(
                name: "HRPAYACCUM");

            migrationBuilder.DropTable(
                name: "HRPAYROLL");

            migrationBuilder.DropTable(
                name: "HRPAYROLLDET");

            migrationBuilder.DropTable(
                name: "HRUCFSALARYITEM");

            migrationBuilder.DropTable(
                name: "HRUCFSECURITY");

            migrationBuilder.DropTable(
                name: "HRUCFTAXRATE");

            migrationBuilder.DropTable(
                name: "HRW_OT");

            migrationBuilder.DropTable(
                name: "J_DEPT_GROUP_V2");

            migrationBuilder.DropTable(
                name: "J_HR_CHECK_DEPT");

            migrationBuilder.DropTable(
                name: "Job_Comment");

            migrationBuilder.DropTable(
                name: "JobInJob");

            migrationBuilder.DropTable(
                name: "job_loa");

            migrationBuilder.DropTable(
                name: "job_status");

            migrationBuilder.DropTable(
                name: "job_subworkflow_master");

            migrationBuilder.DropTable(
                name: "job_user_list");

            migrationBuilder.DropTable(
                name: "KPTEMPRECEIVE");

            migrationBuilder.DropTable(
                name: "KPTEMPRECEIVEDET");

            migrationBuilder.DropTable(
                name: "loa");

            migrationBuilder.DropTable(
                name: "loa_type");

            migrationBuilder.DropTable(
                name: "log_system_log");

            migrationBuilder.DropTable(
                name: "mas_bidding_status");

            migrationBuilder.DropTable(
                name: "mas_EmailTemplate");

            migrationBuilder.DropTable(
                name: "mas_service");

            migrationBuilder.DropTable(
                name: "mas_status");

            migrationBuilder.DropTable(
                name: "mas_title");

            migrationBuilder.DropTable(
                name: "mas_unit_type");

            migrationBuilder.DropTable(
                name: "mas_WarranteeType");

            migrationBuilder.DropTable(
                name: "pc_BiddingStatus");

            migrationBuilder.DropTable(
                name: "pc_pr_item");

            migrationBuilder.DropTable(
                name: "pc_RFQCondition");

            migrationBuilder.DropTable(
                name: "pc_rfq_doc");

            migrationBuilder.DropTable(
                name: "pc_RFQDocRequest");

            migrationBuilder.DropTable(
                name: "pc_rfqItem");

            migrationBuilder.DropTable(
                name: "pc_RFQServiceSelect");

            migrationBuilder.DropTable(
                name: "pc_RFQServiceVendor");

            migrationBuilder.DropTable(
                name: "pc_rfq_status");

            migrationBuilder.DropTable(
                name: "pc_te_item");

            migrationBuilder.DropTable(
                name: "pc_vd_Clarify");

            migrationBuilder.DropTable(
                name: "pc_vd_RFQCondition");

            migrationBuilder.DropTable(
                name: "pc_vd_te");

            migrationBuilder.DropTable(
                name: "pc_vd_te_item");

            migrationBuilder.DropTable(
                name: "pc_vendor_quotation");

            migrationBuilder.DropTable(
                name: "pc_vendor_quotation_Item");

            migrationBuilder.DropTable(
                name: "pc_vendor_RFQDocRequest");

            migrationBuilder.DropTable(
                name: "pdpa_compliance");

            migrationBuilder.DropTable(
                name: "pdpa_consent");

            migrationBuilder.DropTable(
                name: "pdpa_consent_master");

            migrationBuilder.DropTable(
                name: "pdpa_datamart");

            migrationBuilder.DropTable(
                name: "pdpa_filePrivacy");

            migrationBuilder.DropTable(
                name: "pdpa_log_convertEndDec");

            migrationBuilder.DropTable(
                name: "pdpa_objective");

            migrationBuilder.DropTable(
                name: "po_inv");

            migrationBuilder.DropTable(
                name: "pos_position");

            migrationBuilder.DropTable(
                name: "pos_position_level");

            migrationBuilder.DropTable(
                name: "PosRoleAssociate");

            migrationBuilder.DropTable(
                name: "prefix_runnig");

            migrationBuilder.DropTable(
                name: "pr_po");

            migrationBuilder.DropTable(
                name: "PRRequisitionConfirm");

            migrationBuilder.DropTable(
                name: "sc_menu_program");

            migrationBuilder.DropTable(
                name: "sc_program_group");

            migrationBuilder.DropTable(
                name: "sc_role_menu");

            migrationBuilder.DropTable(
                name: "sc_role_program");

            migrationBuilder.DropTable(
                name: "sc_user_role");

            migrationBuilder.DropTable(
                name: "stoa");

            migrationBuilder.DropTable(
                name: "task_activity");

            migrationBuilder.DropTable(
                name: "task_assign");

            migrationBuilder.DropTable(
                name: "time_checkin");

            migrationBuilder.DropTable(
                name: "toa");

            migrationBuilder.DropTable(
                name: "upload_center");

            migrationBuilder.DropTable(
                name: "util_document");

            migrationBuilder.DropTable(
                name: "vd_address");

            migrationBuilder.DropTable(
                name: "vd_certificate");

            migrationBuilder.DropTable(
                name: "vd_contact");

            migrationBuilder.DropTable(
                name: "vd_doc");

            migrationBuilder.DropTable(
                name: "vd_financial");

            migrationBuilder.DropTable(
                name: "vd_portfolio");

            migrationBuilder.DropTable(
                name: "vd_service");

            migrationBuilder.DropTable(
                name: "vd_signed");

            migrationBuilder.DropTable(
                name: "wf_adhoc_user");

            migrationBuilder.DropTable(
                name: "wf_budget");

            migrationBuilder.DropTable(
                name: "wf_button");

            migrationBuilder.DropTable(
                name: "wf_checklist");

            migrationBuilder.DropTable(
                name: "wf_condition");

            migrationBuilder.DropTable(
                name: "wf_customer_approver");

            migrationBuilder.DropTable(
                name: "wf_custom_role");

            migrationBuilder.DropTable(
                name: "wf_custom_user");

            migrationBuilder.DropTable(
                name: "wf_decision_status");

            migrationBuilder.DropTable(
                name: "wf_emailTemplate");

            migrationBuilder.DropTable(
                name: "wf_employee");

            migrationBuilder.DropTable(
                name: "wf_loa");

            migrationBuilder.DropTable(
                name: "wf_loa_user");

            migrationBuilder.DropTable(
                name: "wf_mas_reason");

            migrationBuilder.DropTable(
                name: "wf_organize");

            migrationBuilder.DropTable(
                name: "wf_org_type");

            migrationBuilder.DropTable(
                name: "wf_workflow_in_workflow");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "mas_reason");

            migrationBuilder.DropTable(
                name: "pc_pr");

            migrationBuilder.DropTable(
                name: "pc_rfq");

            migrationBuilder.DropTable(
                name: "pc_te");

            migrationBuilder.DropTable(
                name: "sc_menu");

            migrationBuilder.DropTable(
                name: "sc_program");

            migrationBuilder.DropTable(
                name: "sc_role");

            migrationBuilder.DropTable(
                name: "task_master");

            migrationBuilder.DropTable(
                name: "mas_address_type");

            migrationBuilder.DropTable(
                name: "mas_province");

            migrationBuilder.DropTable(
                name: "mas_doc_type");

            migrationBuilder.DropTable(
                name: "vd_general_info");

            migrationBuilder.DropTable(
                name: "job_master");

            migrationBuilder.DropTable(
                name: "wf_button_master");

            migrationBuilder.DropTable(
                name: "sc_user");

            migrationBuilder.DropTable(
                name: "wf_sub_workflow_master");

            migrationBuilder.DropTable(
                name: "pr_service_type");

            migrationBuilder.DropTable(
                name: "sc_menugroup");

            migrationBuilder.DropTable(
                name: "mas_country");

            migrationBuilder.DropTable(
                name: "Currency");

            migrationBuilder.DropTable(
                name: "wf_workflow");

            migrationBuilder.DropTable(
                name: "com_company");
        }
    }
}
