using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // ลูกค้า PST (CEO, 21 ก.ย. 2569) — แกนเดียวกัน "เวลาทำงานในรอบตัดเวลา → เงิน":
    //   1. รอบตัดเวลา (เช่น 26–25) แยกจากงวดเงินเดือนที่คิดเต็มเดือนปฏิทิน
    //   2. กติกาหักแบบตั้งค่าได้: เหตุ (สาย/ขาด) → เป้าหมาย (เงินเดือน หรือรายได้ประจำ เช่น เบี้ยขยัน) → สูตร (ตัดทั้งก้อน/ต่อนาที/ต่อครั้ง บาทหรือ %)
    //   3. ค่าจ้างรายวันแบบจำนวนวันคงที่ต่อเดือน (22) ตั้งได้ต่อประเภทพนักงาน
    //   4. เวลาเข้า-ออกงานต่อสาขา/หน่วยงาน
    // ทุกอย่างปิด/ว่างเป็นค่าเริ่มต้น — บริษัทที่ไม่ตั้งค่า ผลคำนวณเท่าเดิมทุกบาท
    public partial class AttendanceRulesCutoffAndBranchHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Pay_AttendanceDeductionPolicy', 'AttendanceCutoffDay') IS NULL
    ALTER TABLE Pay_AttendanceDeductionPolicy ADD AttendanceCutoffDay int NULL;
IF COL_LENGTH('Pay_AttendanceDeductionPolicy', 'DailyWageFixedDays') IS NULL
    ALTER TABLE Pay_AttendanceDeductionPolicy ADD DailyWageFixedDays int NULL;
IF COL_LENGTH('Pos_EmployeeType', 'DailyWageMode') IS NULL
    ALTER TABLE Pos_EmployeeType ADD DailyWageMode int NULL;
IF COL_LENGTH('Pos_EmployeeType', 'DailyWageFixedDays') IS NULL
    ALTER TABLE Pos_EmployeeType ADD DailyWageFixedDays int NULL;

IF OBJECT_ID('Pay_AttendanceRule') IS NULL
BEGIN
    CREATE TABLE Pay_AttendanceRule (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pay_AttendanceRule PRIMARY KEY,
        CompanyId nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Trigger] int NOT NULL,
        [Target] int NOT NULL,
        WelBenefitTypeId bigint NULL CONSTRAINT FK_Pay_AttendanceRule_Wel_BenefitType REFERENCES Wel_BenefitType(Id),
        Formula int NOT NULL,
        [Value] decimal(15,4) NOT NULL CONSTRAINT DF_Pay_AttendanceRule_Value DEFAULT 0,
        GraceMinutes int NOT NULL CONSTRAINT DF_Pay_AttendanceRule_Grace DEFAULT 0,
        ThresholdCount int NOT NULL CONSTRAINT DF_Pay_AttendanceRule_Threshold DEFAULT 1,
        SortOrder int NOT NULL CONSTRAINT DF_Pay_AttendanceRule_Sort DEFAULT 0,
        IsActive bit NOT NULL CONSTRAINT DF_Pay_AttendanceRule_Active DEFAULT 1,
        Note nvarchar(500) NULL,
        ModifiedDate datetime2 NOT NULL,
        ModifiedByUserId bigint NULL
    );
    CREATE INDEX IX_Pay_AttendanceRule_CompanyId ON Pay_AttendanceRule (CompanyId, IsActive);
END

IF OBJECT_ID('Att_OrgWorkTime') IS NULL
BEGIN
    CREATE TABLE Att_OrgWorkTime (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Att_OrgWorkTime PRIMARY KEY,
        CompanyId nvarchar(50) NOT NULL,
        OrganizationId bigint NOT NULL,
        WorkStart time NOT NULL,
        WorkEnd time NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Att_OrgWorkTime_Active DEFAULT 1,
        ModifiedDate datetime2 NOT NULL,
        ModifiedByUserId bigint NULL
    );
    CREATE INDEX IX_Att_OrgWorkTime_Company_Org ON Att_OrgWorkTime (CompanyId, OrganizationId);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('Att_OrgWorkTime') IS NOT NULL DROP TABLE Att_OrgWorkTime;
IF OBJECT_ID('Pay_AttendanceRule') IS NOT NULL DROP TABLE Pay_AttendanceRule;
IF COL_LENGTH('Pos_EmployeeType', 'DailyWageFixedDays') IS NOT NULL ALTER TABLE Pos_EmployeeType DROP COLUMN DailyWageFixedDays;
IF COL_LENGTH('Pos_EmployeeType', 'DailyWageMode') IS NOT NULL ALTER TABLE Pos_EmployeeType DROP COLUMN DailyWageMode;
IF COL_LENGTH('Pay_AttendanceDeductionPolicy', 'DailyWageFixedDays') IS NOT NULL ALTER TABLE Pay_AttendanceDeductionPolicy DROP COLUMN DailyWageFixedDays;
IF COL_LENGTH('Pay_AttendanceDeductionPolicy', 'AttendanceCutoffDay') IS NOT NULL ALTER TABLE Pay_AttendanceDeductionPolicy DROP COLUMN AttendanceCutoffDay;");
        }
    }
}

