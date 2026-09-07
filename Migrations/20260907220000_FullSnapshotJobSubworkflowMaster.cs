using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class FullSnapshotJobSubworkflowMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Full-level snapshot (CEO, 2026-09-07 follow-up): stop
            // hand-picking which wf_sub_workflow_master columns get frozen
            // onto job_subworkflow_master at job-start — snapshot everything
            // else too. All nullable, all NULL for every existing row until
            // the next job starts — no behavior change to any in-flight job.
            var columns = new (string Name, string Type)[]
            {
                ("isAdhocUser", "bit"),
                ("iscustomApprover", "bit"),
                ("approvedstatus", "nvarchar(50)"),
                ("declinestatus", "nvarchar(50)"),
                ("isReturnSender", "bit"),
                ("loacode", "nvarchar(50)"),
                ("isAutoApproveAllow", "bit"),
                ("isNeedBudgetApproval", "bit"),
                ("sitinstatus", "nvarchar(50)"),
                ("aa_id", "int"),
                ("aa_level", "int"),
                ("controller", "nvarchar(250)"),
                ("action", "nvarchar(250)"),
                ("displayName", "nvarchar(250)"),
                ("userid1", "bigint"),
                ("userid2", "bigint"),
                ("userid3", "bigint"),
                ("subject", "nvarchar(250)"),
                ("subjectBiz", "nvarchar(250)"),
                ("describeBiz", "nvarchar(500)"),
                ("describe", "nvarchar(500)"),
                ("actionEdit", "nvarchar(250)"),
                ("subject_en", "nvarchar(250)"),
                ("subjectBiz_en", "nvarchar(250)"),
                ("describeBiz_en", "nvarchar(250)"),
                ("describe_en", "nvarchar(250)"),
                ("isApproverSameOrg", "bit"),
                ("isApproverSameCostCenter", "bit"),
                ("isManualButton", "bit"),
                ("ApproveController", "nvarchar(250)"),
                ("ApproveAction", "nvarchar(250)"),
            };

            foreach (var (name, type) in columns)
            {
                migrationBuilder.Sql(
                    $"IF COL_LENGTH('job_subworkflow_master', '{name}') IS NULL " +
                    $"ALTER TABLE job_subworkflow_master ADD [{name}] {type} NULL;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var names = new[]
            {
                "isAdhocUser", "iscustomApprover", "approvedstatus", "declinestatus", "isReturnSender",
                "loacode", "isAutoApproveAllow", "isNeedBudgetApproval", "sitinstatus", "aa_id", "aa_level",
                "controller", "action", "displayName", "userid1", "userid2", "userid3", "subject",
                "subjectBiz", "describeBiz", "describe", "actionEdit", "subject_en", "subjectBiz_en",
                "describeBiz_en", "describe_en", "isApproverSameOrg", "isApproverSameCostCenter",
                "isManualButton", "ApproveController", "ApproveAction",
            };
            foreach (var name in names)
                migrationBuilder.DropColumn(name: name, table: "job_subworkflow_master");
        }
    }
}
