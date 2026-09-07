using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPoolClaimLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pool Workflow claim-lock (CEO, 2026-09-07 follow-up): lets a
            // pool candidate claim a job's current pending level so
            // teammates see it's already being worked, enforced (not just a
            // UI hint) in ApproveAsync/RejectAsync/DeclineAsync/SendBackAsync.
            // All nullable, all NULL by default — zero effect on any
            // existing job until someone actually claims one.
            migrationBuilder.AddColumn<long>(
                name: "PoolClaimedByUserId",
                table: "job_master",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PoolClaimedWLevel",
                table: "job_master",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PoolClaimedJobSeq",
                table: "job_master",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PoolClaimedDate",
                table: "job_master",
                type: "datetime",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PoolClaimedByUserId", table: "job_master");
            migrationBuilder.DropColumn(name: "PoolClaimedWLevel", table: "job_master");
            migrationBuilder.DropColumn(name: "PoolClaimedJobSeq", table: "job_master");
            migrationBuilder.DropColumn(name: "PoolClaimedDate", table: "job_master");
        }
    }
}
