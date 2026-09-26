using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // Personal-income-tax deductions by method (ม.47 ประมวลรัษฎากร), configured per tax year:
    // FixedCap / PerPerson (spouse, child, parent…) / PercentOfIncome (RMF, SSF, donation), plus shared
    // group caps (RETIREMENT 500,000 with PVD; LIFE_HEALTH 100,000). Seeds default rows for 2025 and
    // 2026 only where (Code, EffectiveYear) is missing — HR-edited rows are never overwritten, and the
    // amounts must be checked against the Revenue Department's rules for each year at
    // /pay/admin/tax-deduction-config. Election gets PersonCount and DeclaredByEmployee (ล.ย.01).
    public partial class TaxDeductionMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Pay_TaxDeductionType', 'CalcMethod') IS NULL
    ALTER TABLE Pay_TaxDeductionType ADD CalcMethod int NOT NULL CONSTRAINT DF_Pay_TaxDeductionType_CalcMethod DEFAULT 0;
IF COL_LENGTH('Pay_TaxDeductionType', 'AmountPerPerson') IS NULL
    ALTER TABLE Pay_TaxDeductionType ADD AmountPerPerson decimal(15,2) NULL;
IF COL_LENGTH('Pay_TaxDeductionType', 'MaxPersons') IS NULL
    ALTER TABLE Pay_TaxDeductionType ADD MaxPersons int NULL;
IF COL_LENGTH('Pay_TaxDeductionType', 'PercentCap') IS NULL
    ALTER TABLE Pay_TaxDeductionType ADD PercentCap decimal(5,2) NULL;
IF COL_LENGTH('Pay_TaxDeductionType', 'PercentBase') IS NULL
    ALTER TABLE Pay_TaxDeductionType ADD PercentBase int NULL;
IF COL_LENGTH('Pay_TaxDeductionType', 'CapGroup') IS NULL
    ALTER TABLE Pay_TaxDeductionType ADD CapGroup nvarchar(50) NULL;
IF COL_LENGTH('Pay_TaxDeductionType', 'GroupCapPerYear') IS NULL
    ALTER TABLE Pay_TaxDeductionType ADD GroupCapPerYear decimal(15,2) NULL;
IF COL_LENGTH('Pay_EmployeeTaxDeductionElection', 'PersonCount') IS NULL
    ALTER TABLE Pay_EmployeeTaxDeductionElection ADD PersonCount int NULL;
IF COL_LENGTH('Pay_EmployeeTaxDeductionElection', 'DeclaredByEmployee') IS NULL
    ALTER TABLE Pay_EmployeeTaxDeductionElection ADD DeclaredByEmployee bit NOT NULL CONSTRAINT DF_Pay_EmployeeTaxDeductionElection_DeclaredByEmployee DEFAULT 0;
");

            // Existing rows: give them the method/group the law gives them, without touching the baht
            // amounts HR may have edited.
            migrationBuilder.Sql(@"
EXEC(N'
UPDATE Pay_TaxDeductionType SET CapGroup = ''LIFE_HEALTH'', GroupCapPerYear = 100000 WHERE Code = ''LIFE_INSURANCE'' AND CapGroup IS NULL;
UPDATE Pay_TaxDeductionType SET CalcMethod = 2, PercentCap = 30, PercentBase = 0, CapGroup = ''RETIREMENT'', GroupCapPerYear = 500000
    WHERE Code = ''RMF_SSF'' AND CalcMethod = 0 AND CapGroup IS NULL;
UPDATE Pay_TaxDeductionType SET CalcMethod = 2, PercentCap = 10, PercentBase = 1
    WHERE Code = ''DONATION'' AND CalcMethod = 0 AND PercentCap IS NULL;
');
");

            // Defaults for 2025 and 2026 — insert only what is missing for that year.
            migrationBuilder.Sql(@"
EXEC(N'
DECLARE @d TABLE (Code nvarchar(30), NameTh nvarchar(200), NameEn nvarchar(200), Method int, MaxAmt decimal(15,2),
                  PerPerson decimal(15,2) NULL, MaxPersons int NULL, Pct decimal(5,2) NULL, PctBase int NULL,
                  CapGroup nvarchar(50) NULL, GroupCap decimal(15,2) NULL, SortOrder int);
INSERT INTO @d VALUES
 (N''SPOUSE'', N''คู่สมรส (ไม่มีเงินได้)'', N''Spouse without income'', 1, 0, 60000, 1, NULL, NULL, NULL, NULL, 10),
 (N''CHILD'', N''บุตร (คนละ 30,000)'', N''Child'', 1, 0, 30000, NULL, NULL, NULL, NULL, NULL, 20),
 (N''CHILD_2ND_2018'', N''บุตรคนที่ 2 ขึ้นไปที่เกิดตั้งแต่ปี 2561 (คนละ 60,000)'', N''2nd+ child born 2018 or later'', 1, 0, 60000, NULL, NULL, NULL, NULL, NULL, 30),
 (N''PARENT'', N''บิดามารดาของตนและคู่สมรส (อายุ 60 ขึ้นไป เงินได้ไม่เกิน 30,000/ปี)'', N''Parents aged 60+'', 1, 0, 30000, 4, NULL, NULL, NULL, NULL, 40),
 (N''DISABLED_DEPENDENT'', N''อุปการะเลี้ยงดูคนพิการ/ทุพพลภาพ (คนละ 60,000)'', N''Disabled dependent'', 1, 0, 60000, NULL, NULL, NULL, NULL, NULL, 50),
 (N''PRENATAL'', N''ค่าฝากครรภ์และคลอดบุตร'', N''Prenatal and childbirth'', 0, 60000, NULL, NULL, NULL, NULL, NULL, NULL, 60),
 (N''LIFE_INSURANCE'', N''เบี้ยประกันชีวิต'', N''Life insurance premium'', 0, 100000, NULL, NULL, NULL, NULL, N''LIFE_HEALTH'', 100000, 70),
 (N''HEALTH_INSURANCE'', N''เบี้ยประกันสุขภาพตนเอง'', N''Own health insurance'', 0, 25000, NULL, NULL, NULL, NULL, N''LIFE_HEALTH'', 100000, 80),
 (N''PARENT_HEALTH_INSURANCE'', N''เบี้ยประกันสุขภาพบิดามารดา'', N''Parents health insurance'', 0, 15000, NULL, NULL, NULL, NULL, NULL, NULL, 90),
 (N''PENSION_INSURANCE'', N''เบี้ยประกันชีวิตแบบบำนาญ (15% ของเงินได้)'', N''Pension life insurance'', 2, 200000, NULL, NULL, 15, 0, N''RETIREMENT'', 500000, 100),
 (N''RMF'', N''กองทุนรวมเพื่อการเลี้ยงชีพ RMF (30% ของเงินได้)'', N''RMF'', 2, 500000, NULL, NULL, 30, 0, N''RETIREMENT'', 500000, 110),
 (N''SSF'', N''กองทุนรวมเพื่อการออม SSF (30% ของเงินได้)'', N''SSF'', 2, 200000, NULL, NULL, 30, 0, N''RETIREMENT'', 500000, 120),
 (N''THAI_ESG'', N''กองทุนรวมไทยเพื่อความยั่งยืน ThaiESG (30% ของเงินได้)'', N''Thai ESG fund'', 2, 300000, NULL, NULL, 30, 0, NULL, NULL, 130),
 (N''HOME_LOAN_INTEREST'', N''ดอกเบี้ยเงินกู้ยืมเพื่อซื้อที่อยู่อาศัย'', N''Home loan interest'', 0, 100000, NULL, NULL, NULL, NULL, NULL, NULL, 140),
 (N''DONATION'', N''เงินบริจาคทั่วไป (ไม่เกิน 10% ของเงินได้หลังหักค่าใช้จ่ายและลดหย่อน)'', N''General donation'', 2, 0, NULL, NULL, 10, 1, NULL, NULL, 150);

DECLARE @y TABLE (Yr int);
INSERT INTO @y VALUES (2025), (2026);

INSERT INTO Pay_TaxDeductionType (Code, EffectiveYear, NameTh, NameEn, MaxAmountPerYear, CalcMethod, AmountPerPerson, MaxPersons,
                                  PercentCap, PercentBase, CapGroup, GroupCapPerYear, IsActive, SortOrder)
SELECT d.Code, y.Yr, d.NameTh, d.NameEn, d.MaxAmt, d.Method, d.PerPerson, d.MaxPersons, d.Pct, d.PctBase, d.CapGroup, d.GroupCap, 1, d.SortOrder
FROM @d d CROSS JOIN @y y
WHERE NOT EXISTS (SELECT 1 FROM Pay_TaxDeductionType t WHERE t.Code = d.Code AND t.EffectiveYear = y.Yr)
  -- a year still carrying the old combined RMF_SSF row keeps it (already in the RETIREMENT group) instead of getting a duplicate RMF + SSF
  AND NOT (d.Code IN (N''RMF'', N''SSF'') AND EXISTS (SELECT 1 FROM Pay_TaxDeductionType t WHERE t.Code = N''RMF_SSF'' AND t.EffectiveYear = y.Yr));
');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM Pay_TaxDeductionType
 WHERE Code IN ('SPOUSE','CHILD','CHILD_2ND_2018','PARENT','DISABLED_DEPENDENT','PRENATAL','HEALTH_INSURANCE',
                'PARENT_HEALTH_INSURANCE','PENSION_INSURANCE','RMF','SSF','THAI_ESG','HOME_LOAN_INTEREST')
   AND NOT EXISTS (SELECT 1 FROM Pay_EmployeeTaxDeductionElection e WHERE e.DeductionTypeId = Pay_TaxDeductionType.Id);
IF COL_LENGTH('Pay_EmployeeTaxDeductionElection', 'DeclaredByEmployee') IS NOT NULL
BEGIN
    ALTER TABLE Pay_EmployeeTaxDeductionElection DROP CONSTRAINT DF_Pay_EmployeeTaxDeductionElection_DeclaredByEmployee;
    ALTER TABLE Pay_EmployeeTaxDeductionElection DROP COLUMN DeclaredByEmployee;
END
IF COL_LENGTH('Pay_EmployeeTaxDeductionElection', 'PersonCount') IS NOT NULL ALTER TABLE Pay_EmployeeTaxDeductionElection DROP COLUMN PersonCount;
IF COL_LENGTH('Pay_TaxDeductionType', 'CalcMethod') IS NOT NULL
BEGIN
    ALTER TABLE Pay_TaxDeductionType DROP CONSTRAINT DF_Pay_TaxDeductionType_CalcMethod;
    ALTER TABLE Pay_TaxDeductionType DROP COLUMN CalcMethod;
END
IF COL_LENGTH('Pay_TaxDeductionType', 'AmountPerPerson') IS NOT NULL ALTER TABLE Pay_TaxDeductionType DROP COLUMN AmountPerPerson;
IF COL_LENGTH('Pay_TaxDeductionType', 'MaxPersons') IS NOT NULL ALTER TABLE Pay_TaxDeductionType DROP COLUMN MaxPersons;
IF COL_LENGTH('Pay_TaxDeductionType', 'PercentCap') IS NOT NULL ALTER TABLE Pay_TaxDeductionType DROP COLUMN PercentCap;
IF COL_LENGTH('Pay_TaxDeductionType', 'PercentBase') IS NOT NULL ALTER TABLE Pay_TaxDeductionType DROP COLUMN PercentBase;
IF COL_LENGTH('Pay_TaxDeductionType', 'CapGroup') IS NOT NULL ALTER TABLE Pay_TaxDeductionType DROP COLUMN CapGroup;
IF COL_LENGTH('Pay_TaxDeductionType', 'GroupCapPerYear') IS NOT NULL ALTER TABLE Pay_TaxDeductionType DROP COLUMN GroupCapPerYear;
");
        }
    }
}
