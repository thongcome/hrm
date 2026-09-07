using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSubworkflowIsPool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pool Workflow (CEO, 2026-09-07): job_subworkflow_master needs
            // its own isPool snapshot — wf_sub_workflow_master.isPool already
            // exists but was never copied at job-start time. NOT NULL with a
            // default so existing snapshot rows (from workflows already
            // in-flight before this feature) get a safe false.
            migrationBuilder.AddColumn<bool>(
                name: "isPool",
                table: "job_subworkflow_master",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isPool",
                table: "job_subworkflow_master");
        }
    }
}
