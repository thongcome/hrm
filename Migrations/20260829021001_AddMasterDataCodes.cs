using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterDataCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reason_code",
                table: "wf_mas_reason",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "task_code",
                table: "task_master",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NominationCode",
                table: "Succ_SuccessorNominations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Succ_KeyPosition",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stoa_code",
                table: "stoa",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequisitionCode",
                table: "Rec_Requisition",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfferCode",
                table: "Rec_Offer",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostingCode",
                table: "Rec_JobPosting",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidateCode",
                table: "Rec_Candidate",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationCode",
                table: "Rec_Application",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BudgetCode",
                table: "Pos_HeadcountBudget",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Pos_EmployeeType",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Perf_RaterDirectionConfig",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanCode",
                table: "Perf_ImprovementPlan",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoalCode",
                table: "Perf_Goal",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Perf_EvaluationPeriod",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstanceCode",
                table: "Perf_EvaluationInstance",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionCode",
                table: "Perf_CalibrationSession",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "pdpa_objective",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "clarify_no",
                table: "pc_vd_Clarify",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "te_no",
                table: "pc_te",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "doc_code",
                table: "pc_rfq_doc",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Pay_WelfareFundPolicy",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Pay_TaxBracket",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Pay_ProvidentFundRateMatrixRule",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Pay_ProvidentFundRateChangeWindow",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "Pay_ProvidentFundRateChangeRequest",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PolicyCode",
                table: "Pay_ProvidentFundPolicy",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaseNo",
                table: "Pay_ProvidentFundExitCase",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeriodCode",
                table: "Pay_PayrollPeriod",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatchNo",
                table: "Pay_GLExportBatch",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoanNo",
                table: "Pay_EmployeeLoan",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatchNo",
                table: "Pay_BankFileExportBatch",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestCode",
                table: "Pay_AdhocPayItem",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "Org_OrganizationChangeRequest",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "OrgDev_WorkforcePlan",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "OrgDev_LeadershipPlans",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "OrgDev_ChangeInitiative",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Okr_Objective",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "mas_WarranteeType",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "mas_reason",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "Lve_LeaveRequest",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Lve_LeavePolicy",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Lve_CompanyHoliday",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "Lms_TrainingNeed",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Lms_TrainingBudget",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Km_Article",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "info_message",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanNo",
                table: "Idp_Plan",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "Hr_SeparationRequest",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaseNo",
                table: "Hr_RewardCases",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "HRPayrollPayByRequest",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaseNo",
                table: "Hr_Grievances",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaseNo",
                table: "Hr_DisciplinaryCases",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HolCode",
                table: "Holiday",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimNo",
                table: "Exp_ClaimHeader",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Eng_SurveyCampaign",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Eng_QuestionTemplate",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Eng_ActionPlan",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_no",
                table: "emp_overtime_request",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Com_SubSectionType",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionNo",
                table: "Att_TimesheetSubmission",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Att_OtRule",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Att_GeofenceLocation",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "noticeNo",
                table: "asset_notice",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "budgetcode",
                table: "approver_budget",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Backfill a simple sequential code (PREFIX-0001, ordered by Id) for every
            // table above that already has rows, so no existing record is left with a
            // null code — HR can rename these to something meaningful later, but every
            // row gets a stable, unique value now. Tables with 0 rows need no backfill.
            migrationBuilder.Sql(@"
                UPDATE Att_GeofenceLocation SET Code = 'GEO-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Att_OtRule SET Code = 'OTR-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Eng_QuestionTemplate SET Code = 'QT-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Eng_SurveyCampaign SET Code = 'SURV-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Exp_ClaimHeader SET ClaimNo = 'CLM-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE ClaimNo IS NULL;
                UPDATE Att_TimesheetSubmission SET SubmissionNo = 'TS-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE SubmissionNo IS NULL;
                UPDATE Hr_DisciplinaryCases SET CaseNo = 'DISC-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE CaseNo IS NULL;
                UPDATE Hr_Grievances SET CaseNo = 'GRV-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE CaseNo IS NULL;
                UPDATE Hr_RewardCases SET CaseNo = 'RWD-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE CaseNo IS NULL;
                UPDATE Idp_Plan SET PlanNo = 'IDP-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE PlanNo IS NULL;
                UPDATE Km_Article SET Code = 'KM-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Lms_TrainingBudget SET Code = 'TB-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Lms_TrainingNeed SET RequestNo = 'TN-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE RequestNo IS NULL;
                UPDATE Lve_CompanyHoliday SET Code = 'HOL-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Lve_LeavePolicy SET Code = 'LVP-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Lve_LeaveRequest SET RequestNo = 'LV-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE RequestNo IS NULL;
                UPDATE Okr_Objective SET Code = 'OKR-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE OrgDev_ChangeInitiative SET Code = 'CI-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE OrgDev_LeadershipPlans SET Code = 'LDP-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE OrgDev_WorkforcePlan SET Code = 'WFP-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Org_OrganizationChangeRequest SET RequestNo = 'ORG-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE RequestNo IS NULL;
                UPDATE Pay_AdhocPayItem SET RequestCode = 'ADH-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE RequestCode IS NULL;
                UPDATE Pay_BankFileExportBatch SET BatchNo = 'BNK-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE BatchNo IS NULL;
                UPDATE Pay_EmployeeLoan SET LoanNo = 'LOAN-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE LoanNo IS NULL;
                UPDATE Pay_GLExportBatch SET BatchNo = 'GL-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE BatchNo IS NULL;
                UPDATE Pay_PayrollPeriod SET PeriodCode = 'PP-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE PeriodCode IS NULL;
                UPDATE Pay_ProvidentFundExitCase SET CaseNo = 'PFX-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE CaseNo IS NULL;
                UPDATE Pay_ProvidentFundPolicy SET PolicyCode = 'PFP-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE PolicyCode IS NULL;
                UPDATE Pay_ProvidentFundRateChangeRequest SET RequestNo = 'PFR-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE RequestNo IS NULL;
                UPDATE Pay_ProvidentFundRateChangeWindow SET Code = 'PFW-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Pay_ProvidentFundRateMatrixRule SET Code = 'PFM-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Pay_TaxBracket SET Code = 'TAX-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Pay_WelfareFundPolicy SET Code = 'WF-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Perf_EvaluationInstance SET InstanceCode = 'PEI-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE InstanceCode IS NULL;
                UPDATE Perf_EvaluationPeriod SET Code = 'PEP-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Perf_Goal SET GoalCode = 'PG-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE GoalCode IS NULL;
                UPDATE Perf_ImprovementPlan SET PlanCode = 'PIP-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE PlanCode IS NULL;
                UPDATE Perf_RaterDirectionConfig SET Code = 'RDC-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Rec_Application SET ApplicationCode = 'APP-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE ApplicationCode IS NULL;
                UPDATE Rec_Candidate SET CandidateCode = 'CAND-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE CandidateCode IS NULL;
                UPDATE Rec_JobPosting SET PostingCode = 'POST-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE PostingCode IS NULL;
                UPDATE Rec_Offer SET OfferCode = 'OFR-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE OfferCode IS NULL;
                UPDATE Rec_Requisition SET RequisitionCode = 'REQ-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE RequisitionCode IS NULL;
                UPDATE Succ_KeyPosition SET Code = 'KP-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE Code IS NULL;
                UPDATE Succ_SuccessorNominations SET NominationCode = 'NOM-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE NominationCode IS NULL;
                UPDATE info_message SET code = 'MSG-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE code IS NULL;
                UPDATE emp_overtime_request SET request_no = 'OT-' + RIGHT('0000' + CAST(Id AS VARCHAR(10)), 4) WHERE request_no IS NULL;
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reason_code",
                table: "wf_mas_reason");

            migrationBuilder.DropColumn(
                name: "task_code",
                table: "task_master");

            migrationBuilder.DropColumn(
                name: "NominationCode",
                table: "Succ_SuccessorNominations");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Succ_KeyPosition");

            migrationBuilder.DropColumn(
                name: "stoa_code",
                table: "stoa");

            migrationBuilder.DropColumn(
                name: "RequisitionCode",
                table: "Rec_Requisition");

            migrationBuilder.DropColumn(
                name: "OfferCode",
                table: "Rec_Offer");

            migrationBuilder.DropColumn(
                name: "PostingCode",
                table: "Rec_JobPosting");

            migrationBuilder.DropColumn(
                name: "CandidateCode",
                table: "Rec_Candidate");

            migrationBuilder.DropColumn(
                name: "ApplicationCode",
                table: "Rec_Application");

            migrationBuilder.DropColumn(
                name: "BudgetCode",
                table: "Pos_HeadcountBudget");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Pos_EmployeeType");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Perf_RaterDirectionConfig");

            migrationBuilder.DropColumn(
                name: "PlanCode",
                table: "Perf_ImprovementPlan");

            migrationBuilder.DropColumn(
                name: "GoalCode",
                table: "Perf_Goal");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Perf_EvaluationPeriod");

            migrationBuilder.DropColumn(
                name: "InstanceCode",
                table: "Perf_EvaluationInstance");

            migrationBuilder.DropColumn(
                name: "SessionCode",
                table: "Perf_CalibrationSession");

            migrationBuilder.DropColumn(
                name: "code",
                table: "pdpa_objective");

            migrationBuilder.DropColumn(
                name: "clarify_no",
                table: "pc_vd_Clarify");

            migrationBuilder.DropColumn(
                name: "te_no",
                table: "pc_te");

            migrationBuilder.DropColumn(
                name: "doc_code",
                table: "pc_rfq_doc");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Pay_WelfareFundPolicy");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Pay_TaxBracket");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Pay_ProvidentFundRateMatrixRule");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Pay_ProvidentFundRateChangeWindow");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "Pay_ProvidentFundRateChangeRequest");

            migrationBuilder.DropColumn(
                name: "PolicyCode",
                table: "Pay_ProvidentFundPolicy");

            migrationBuilder.DropColumn(
                name: "CaseNo",
                table: "Pay_ProvidentFundExitCase");

            migrationBuilder.DropColumn(
                name: "PeriodCode",
                table: "Pay_PayrollPeriod");

            migrationBuilder.DropColumn(
                name: "BatchNo",
                table: "Pay_GLExportBatch");

            migrationBuilder.DropColumn(
                name: "LoanNo",
                table: "Pay_EmployeeLoan");

            migrationBuilder.DropColumn(
                name: "BatchNo",
                table: "Pay_BankFileExportBatch");

            migrationBuilder.DropColumn(
                name: "RequestCode",
                table: "Pay_AdhocPayItem");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "Org_OrganizationChangeRequest");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "OrgDev_WorkforcePlan");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "OrgDev_LeadershipPlans");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "OrgDev_ChangeInitiative");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Okr_Objective");

            migrationBuilder.DropColumn(
                name: "code",
                table: "mas_WarranteeType");

            migrationBuilder.DropColumn(
                name: "code",
                table: "mas_reason");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "Lve_LeaveRequest");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Lve_LeavePolicy");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Lve_CompanyHoliday");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "Lms_TrainingNeed");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Lms_TrainingBudget");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Km_Article");

            migrationBuilder.DropColumn(
                name: "code",
                table: "info_message");

            migrationBuilder.DropColumn(
                name: "PlanNo",
                table: "Idp_Plan");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "Hr_SeparationRequest");

            migrationBuilder.DropColumn(
                name: "CaseNo",
                table: "Hr_RewardCases");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "HRPayrollPayByRequest");

            migrationBuilder.DropColumn(
                name: "CaseNo",
                table: "Hr_Grievances");

            migrationBuilder.DropColumn(
                name: "CaseNo",
                table: "Hr_DisciplinaryCases");

            migrationBuilder.DropColumn(
                name: "HolCode",
                table: "Holiday");

            migrationBuilder.DropColumn(
                name: "ClaimNo",
                table: "Exp_ClaimHeader");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Eng_SurveyCampaign");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Eng_QuestionTemplate");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Eng_ActionPlan");

            migrationBuilder.DropColumn(
                name: "request_no",
                table: "emp_overtime_request");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Com_SubSectionType");

            migrationBuilder.DropColumn(
                name: "SubmissionNo",
                table: "Att_TimesheetSubmission");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Att_OtRule");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Att_GeofenceLocation");

            migrationBuilder.DropColumn(
                name: "noticeNo",
                table: "asset_notice");

            migrationBuilder.DropColumn(
                name: "budgetcode",
                table: "approver_budget");
        }
    }
}
