using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowStateChangeRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Wf_WorkflowStateChangeRequest",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TargetWorkflowId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotWorkflowCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SnapshotWorkflowName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SetActive = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false),
                    AppliedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_WorkflowStateChangeRequest", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_WorkflowStateChangeRequest_TargetWorkflowId",
                table: "Wf_WorkflowStateChangeRequest",
                column: "TargetWorkflowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Wf_WorkflowStateChangeRequest");
        }
    }
}
