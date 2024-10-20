using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderDevelop.Migrations
{
    /// <inheritdoc />
    public partial class newMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    FirstName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
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
                name: "sc_user",
                columns: table => new
                {
                    userid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    firstname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    lastname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    loginname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    password = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    mobilephone = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isforcechanged = table.Column<bool>(type: "bit", nullable: false),
                    IsCoachRequest = table.Column<bool>(type: "bit", nullable: false),
                    IsCoach = table.Column<bool>(type: "bit", nullable: false),
                    iscancel = table.Column<bool>(type: "bit", nullable: false),
                    pwdexpdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    lasttimelogin = table.Column<DateTime>(type: "datetime", nullable: true),
                    invalidpwcount = table.Column<int>(type: "int", nullable: true),
                    lastinvalidpwd = table.Column<DateTime>(type: "datetime", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    upperuserid = table.Column<int>(type: "int", nullable: true),
                    orgname = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    orgcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    startdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    enddate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remindpwd = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    social = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    moddate = table.Column<DateTime>(type: "datetime", nullable: true),
                    modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    empid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    poscode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pos_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    isActivate = table.Column<bool>(type: "bit", nullable: false),
                    score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    supervisor = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    costcenter = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    verifycode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    registerdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ref1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ref2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sc_user", x => x.userid);
                });

            migrationBuilder.CreateTable(
                name: "TaskMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    DefaultDay = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TodoTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Startdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LessonMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Lesson1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Lesson2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    coachId = table.Column<int>(type: "int", nullable: true),
                    wolId = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ThankFully = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreateBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WOLs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    theme = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WOLs", x => x.id);
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
                name: "SubTasks",
                columns: table => new
                {
                    SubId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LessonMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Lesson1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Lesson2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThankFully = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubTasks", x => x.SubId);
                    table.ForeignKey(
                        name: "FK_SubTasks_TodoTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "TodoTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Startdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DayofGoal = table.Column<double>(type: "float", nullable: true),
                    progress = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    wol = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Lesson1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Lesson2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ThankFully = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    coachId = table.Column<int>(type: "int", nullable: true),
                    facililatorID = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    WolId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreateBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<int>(type: "int", nullable: true),
                    OwnerID = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    email = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalTasks_WOLs_WolId",
                        column: x => x.WolId,
                        principalTable: "WOLs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Activity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Orders = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Lesson1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Lesson2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    progress = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThankFully = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    Modby = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreateBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TaskId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activity_GoalTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "GoalTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activity_TaskId",
                table: "Activity",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

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
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GoalTasks_WolId",
                table: "GoalTasks",
                column: "WolId");

            migrationBuilder.CreateIndex(
                name: "IX_SubTasks_TaskId",
                table: "SubTasks",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activity");

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
                name: "sc_user");

            migrationBuilder.DropTable(
                name: "SubTasks");

            migrationBuilder.DropTable(
                name: "TaskMasters");

            migrationBuilder.DropTable(
                name: "GoalTasks");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "TodoTasks");

            migrationBuilder.DropTable(
                name: "WOLs");
        }
    }
}
