using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class FixLeaveTypeIdCascadeCorruption : Migration
    {
        // AddLeaveTypeCatalog's original Up() remapped old LeaveType enum
        // ints (0,1,2,3,4,9) to the new Lve_LeaveType catalog Ids (1,2,3,4,8,9)
        // using a LOOP of single-value UPDATE statements instead of one
        // CASE-based UPDATE. Because the old and new value ranges overlap,
        // each step's WHERE clause could match rows a PRIOR step had just
        // written (e.g. step 1 sets old-0 rows to 1, then step 2's "WHERE
        // LeaveTypeId = 1" catches those same rows again and moves them to
        // 2, and so on) — every row cascaded through the whole chain and
        // landed on 8 (or stayed 9). That bug is fixed in AddLeaveTypeCatalog
        // itself (now a single CASE UPDATE) so it won't recur, but this DB
        // had already applied the broken version, so its data needs a
        // one-time targeted correction.
        //
        // Reconstructed the correct mapping from EntitlementDaysPerYear,
        // which is untouched by the bug and matches the seeded
        // Lve_LeaveType.StatutoryMinDaysPerYear defaults exactly for each
        // type (30=Sick, 3=Personal, 6=Vacation, 98=Maternity) — verified
        // against the live DB before writing this (2026-08-30):
        //   Lve_LeavePolicy Id=1 (30.0 days)              -> LeaveTypeId 1 (Sick)
        //   Lve_LeavePolicy Id=2 (3.0 days)                -> LeaveTypeId 2 (Personal)
        //   Lve_LeavePolicy Id=3 (6.0 days, capped carry)  -> LeaveTypeId 3 (Vacation)
        //   Lve_LeavePolicy Id=4 (98.0 days)               -> LeaveTypeId 4 (Maternity)
        //   Lve_LeavePolicy Id=5,6 already correct (8=Ordination, 9=Other) — untouched
        //   Lve_LeaveRequest: all 5 rows were originally LeaveType=0/Sick (the only
        //   type exercised while testing) -> LeaveTypeId 1
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Lve_LeavePolicy SET LeaveTypeId = 1 WHERE Id = 1;");
            migrationBuilder.Sql("UPDATE Lve_LeavePolicy SET LeaveTypeId = 2 WHERE Id = 2;");
            migrationBuilder.Sql("UPDATE Lve_LeavePolicy SET LeaveTypeId = 3 WHERE Id = 3;");
            migrationBuilder.Sql("UPDATE Lve_LeavePolicy SET LeaveTypeId = 4 WHERE Id = 4;");
            migrationBuilder.Sql("UPDATE Lve_LeaveRequest SET LeaveTypeId = 1 WHERE LeaveTypeId = 8;");
        }

        /// <inheritdoc />
        // Not reversible — the corruption this fixes already destroyed the
        // information needed to tell which rows were originally which old
        // enum value; this migration's Up() reconstructed it from external
        // evidence (EntitlementDaysPerYear), which Down() has no way to redo
        // generically. Down/up cycling this migration is unsupported.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
