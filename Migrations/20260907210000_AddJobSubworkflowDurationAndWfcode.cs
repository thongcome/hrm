using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSubworkflowDurationAndWfcode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Per-level duration tracking + migration-safe workflow code
            // (CEO, 2026-09-07 follow-up). All nullable, all NULL for every
            // existing row — no behavior change until a job actually
            // advances through a level after this deploys.
            migrationBuilder.AddColumn<DateTime>(
                name: "starttime",
                table: "job_subworkflow_master",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "endtime",
                table: "job_subworkflow_master",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "wfcode",
                table: "job_subworkflow_master",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "starttime", table: "job_subworkflow_master");
            migrationBuilder.DropColumn(name: "endtime", table: "job_subworkflow_master");
            migrationBuilder.DropColumn(name: "wfcode", table: "job_subworkflow_master");
        }
    }
}
