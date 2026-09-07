using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddVerticalMaxLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Self-terminating vertical climb (CEO, 2026-09-07): "climb up
            // from the requester's own org, at most N levels — whichever hop
            // actually resolves closes the job." See WorkflowEngineService
            // .AssignVerticalChainHopAsync / ResolveVerticalChainHopAsync.
            migrationBuilder.AddColumn<int>(
                name: "verticalMaxLevel",
                table: "wf_sub_workflow_master",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "verticalMaxLevel",
                table: "job_subworkflow_master",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "verticalMaxLevel",
                table: "wf_sub_workflow_master");

            migrationBuilder.DropColumn(
                name: "verticalMaxLevel",
                table: "job_subworkflow_master");
        }
    }
}
