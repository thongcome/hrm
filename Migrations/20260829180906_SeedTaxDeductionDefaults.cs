using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedTaxDeductionDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Current Thai PIT standard-deduction figures for EffectiveYear 2026
            // (ค.ศ., matching Pay_TaxBracket's own convention): personal
            // allowance 60,000/year, expense deduction 50% of income capped
            // at 100,000/year — HR can add a new row for a later year via
            // /pay/admin/tax-deduction-config, PayrollCalculationService.cs
            // falls back to these same figures if a year's row is missing.
            //
            // Live DB checked immediately before writing this (2026-08-29):
            // both Pay_TaxDeductionSetting and Pay_TaxDeductionType were
            // empty (brand-new tables from the prior migration) — starting
            // at Id 1 for both.
            migrationBuilder.InsertData(
                table: "Pay_TaxDeductionSetting",
                columns: new[] { "Id", "Code", "EffectiveYear", "PersonalAllowancePerYear", "ExpenseDeductionRate", "ExpenseDeductionCap", "IsActive" },
                values: new object[] { 1, "TAXDED-2026", 2026, 60000m, 0.50m, 100000m, true });

            // Starter examples of the OPTIONAL deduction-type catalog — HR
            // edits/extends these (and adds new years) at
            // /pay/admin/tax-deduction-config; these are not the only
            // categories, just a seeded starting point covering the
            // categories the client specifically asked about.
            migrationBuilder.InsertData(
                table: "Pay_TaxDeductionType",
                columns: new[] { "Id", "Code", "EffectiveYear", "NameTh", "NameEn", "MaxAmountPerYear", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 1, "LIFE_INSURANCE", 2026, "เบี้ยประกันชีวิต", "Life Insurance Premium", 100000m, true, 1 },
                    { 2, "RMF_SSF", 2026, "กองทุน RMF/SSF", "RMF/SSF Fund", 500000m, true, 2 },
                    { 3, "DONATION", 2026, "เงินบริจาค", "Donation", 100000m, true, 3 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Pay_TaxDeductionType", keyColumn: "Id", keyValues: new object[] { 1 });
            migrationBuilder.DeleteData(table: "Pay_TaxDeductionType", keyColumn: "Id", keyValues: new object[] { 2 });
            migrationBuilder.DeleteData(table: "Pay_TaxDeductionType", keyColumn: "Id", keyValues: new object[] { 3 });
            migrationBuilder.DeleteData(table: "Pay_TaxDeductionSetting", keyColumn: "Id", keyValues: new object[] { 1 });
        }
    }
}
