using System;
using System.Collections.Generic;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HRMContext : DbContext
{
    public HRMContext()
    {
    }

    public HRMContext(DbContextOptions<HRMContext> options)
        : base(options)
    {
    }


    public virtual DbSet<Hrbasepayrollfixed> Hrbasepayrollfixed { get; set; }
    public virtual DbSet<Hrucfsecurity> Hrucfsecuritys { get; set; }

    public virtual DbSet<HRUcfTaxRate> HRUcfTaxRates { get; set; }

    public virtual DbSet<Kptempreceive> Kptempreceives { get; set; }


    public virtual DbSet<Kptempreceivedet> Kptempreceivedets { get; set; }

    public virtual DbSet<Hrextenuatetax> Hrextenuatetaxs { get; set; }

    public virtual DbSet<HrwOt> HrwOts { get; set; }

    
    public virtual DbSet<HRPayAccum> HRPayAccums { get; set; }
    public virtual DbSet<Hremployee> Hremployee { get; set; }
    public virtual DbSet<Hrpayroll> Hrpayroll { get; set; }
    public virtual DbSet<Hrpayrolldet> Hrpayrolldet { get; set; }

    public virtual DbSet<Hrbasepayrollfixed> hrbasepayrollfixeds { get; set; }

    public virtual DbSet<Hrbasepayrollrequest> Hrbasepayrollrequests { get; set; }


    public virtual DbSet<Hrucfsalaryitem> hrucfsalaryitems { get; set; }

    public virtual DbSet<ALL_EMPLOYEE> ALL_EMPLOYEEs { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<CT_Contract> CT_Contracts { get; set; }

    public virtual DbSet<ConsentHistory> ConsentHistories { get; set; }

    public virtual DbSet<CostCenterProcurement> CostCenterProcurements { get; set; }

    public virtual DbSet<Currency> Currencies { get; set; }

    public virtual DbSet<DocCheckList> DocCheckLists { get; set; }

    public virtual DbSet<DocTypeMapping> DocTypeMappings { get; set; }

    public virtual DbSet<Employee_datum> Employee_data { get; set; }

    public virtual DbSet<HeadCode_V1> HeadCode_V1s { get; set; }

    public virtual DbSet<Holiday> Holidays { get; set; }

    public virtual DbSet<J_DEPT_GROUP_V2> J_DEPT_GROUP_V2s { get; set; }

    public virtual DbSet<J_HR_CHECK_DEPT> J_HR_CHECK_DEPTs { get; set; }

    public virtual DbSet<JobInJob> JobInJobs { get; set; }

    public virtual DbSet<Job_Comment> Job_Comments { get; set; }

    public virtual DbSet<PRRequisitionConfirm> PRRequisitionConfirms { get; set; }

    public virtual DbSet<PosRoleAssociate> PosRoleAssociates { get; set; }

    public virtual DbSet<app_application_list> app_application_lists { get; set; }

    public virtual DbSet<app_serverInfo> app_serverInfos { get; set; }

    public virtual DbSet<approve_level> approve_levels { get; set; }

    public virtual DbSet<approver_budget> approver_budgets { get; set; }

    public virtual DbSet<asset_fa_ora> asset_fa_oras { get; set; }

    public virtual DbSet<asset_notice> asset_notices { get; set; }

    public virtual DbSet<asset_owner> asset_owners { get; set; }

    public virtual DbSet<button_link> button_links { get; set; }

    public virtual DbSet<com_company> com_companies { get; set; }

    public virtual DbSet<com_org_layer_group> com_org_layer_groups { get; set; }

    public virtual DbSet<com_organization> com_organizations { get; set; }

    public virtual DbSet<com_organization_hi> com_organization_his { get; set; }

    public virtual DbSet<com_organize_layer> com_organize_layers { get; set; }

    public virtual DbSet<com_position> com_positions { get; set; }

    public virtual DbSet<doc_center> doc_centers { get; set; }

    public virtual DbSet<emp_checkin> emp_checkins { get; set; }

    public virtual DbSet<emp_checkout> emp_checkouts { get; set; }

    public virtual DbSet<emp_overtime_request> emp_overtime_requests { get; set; }

    public virtual DbSet<Employee> employees { get; set; }

    public virtual DbSet<Employeetype> employeetypes { get; set; }

    public virtual DbSet<error_log_file> error_log_files { get; set; }

    public virtual DbSet<error_messege> error_messeges { get; set; }

    public virtual DbSet<his_doc> his_docs { get; set; }

    public virtual DbSet<his_doc_bak> his_doc_baks { get; set; }

    public virtual DbSet<job_loa> job_loas { get; set; }

    public virtual DbSet<job_master> job_masters { get; set; }

    public virtual DbSet<job_status> job_statuses { get; set; }

    public virtual DbSet<job_subworkflow_master> job_subworkflow_masters { get; set; }

    public virtual DbSet<job_user_list> job_user_lists { get; set; }

    public virtual DbSet<Loa> loas { get; set; }

    public virtual DbSet<loa_type> loa_types { get; set; }

    public virtual DbSet<log_system_log> log_system_logs { get; set; }

    public virtual DbSet<mas_EmailTemplate> mas_EmailTemplates { get; set; }

    public virtual DbSet<mas_WarranteeType> mas_WarranteeTypes { get; set; }

    public virtual DbSet<mas_address_type> mas_address_types { get; set; }

    public virtual DbSet<mas_bidding_status> mas_bidding_statuses { get; set; }

    public virtual DbSet<mas_country> mas_countries { get; set; }

    public virtual DbSet<mas_doc_type> mas_doc_types { get; set; }

    public virtual DbSet<mas_province> mas_provinces { get; set; }

    public virtual DbSet<mas_reason> mas_reasons { get; set; }

    public virtual DbSet<mas_service> mas_services { get; set; }

    public virtual DbSet<mas_status> mas_statuses { get; set; }

    public virtual DbSet<mas_title> mas_titles { get; set; }

    public virtual DbSet<mas_unit_type> mas_unit_types { get; set; }

    public virtual DbSet<pc_BiddingStatus> pc_BiddingStatuses { get; set; }

    public virtual DbSet<pc_RFQCondition> pc_RFQConditions { get; set; }

    public virtual DbSet<pc_RFQDocRequest> pc_RFQDocRequests { get; set; }

    public virtual DbSet<pc_RFQServiceSelect> pc_RFQServiceSelects { get; set; }

    public virtual DbSet<pc_RFQServiceVendor> pc_RFQServiceVendors { get; set; }

    public virtual DbSet<pc_pr> pc_prs { get; set; }

    public virtual DbSet<pc_pr_item> pc_pr_items { get; set; }

    public virtual DbSet<pc_rfq> pc_rfqs { get; set; }

    public virtual DbSet<pc_rfqItem> pc_rfqItems { get; set; }

    public virtual DbSet<pc_rfq_doc> pc_rfq_docs { get; set; }

    public virtual DbSet<pc_rfq_status> pc_rfq_statuses { get; set; }

    public virtual DbSet<pc_te> pc_tes { get; set; }

    public virtual DbSet<pc_te_item> pc_te_items { get; set; }

    public virtual DbSet<pc_vd_Clarify> pc_vd_Clarifies { get; set; }

    public virtual DbSet<pc_vd_RFQCondition> pc_vd_RFQConditions { get; set; }

    public virtual DbSet<pc_vd_te> pc_vd_tes { get; set; }

    public virtual DbSet<pc_vd_te_item> pc_vd_te_items { get; set; }

    public virtual DbSet<pc_vendor_RFQDocRequest> pc_vendor_RFQDocRequests { get; set; }

    public virtual DbSet<pc_vendor_quotation> pc_vendor_quotations { get; set; }

    public virtual DbSet<pc_vendor_quotation_Item> pc_vendor_quotation_Items { get; set; }

    public virtual DbSet<pdpa_compliance> pdpa_compliances { get; set; }

    public virtual DbSet<pdpa_consent> pdpa_consents { get; set; }

    public virtual DbSet<pdpa_consent_master> pdpa_consent_masters { get; set; }

    public virtual DbSet<pdpa_datamart> pdpa_datamarts { get; set; }

    public virtual DbSet<pdpa_filePrivacy> pdpa_filePrivacies { get; set; }

    public virtual DbSet<pdpa_log_convertEndDec> pdpa_log_convertEndDecs { get; set; }

    public virtual DbSet<pdpa_objective> pdpa_objectives { get; set; }

    public virtual DbSet<po_inv> po_invs { get; set; }

    public virtual DbSet<pos_position> pos_positions { get; set; }

    public virtual DbSet<pos_position_level> pos_position_levels { get; set; }

    public virtual DbSet<pr_po> pr_pos { get; set; }

    public virtual DbSet<pr_service_type> pr_service_types { get; set; }

    public virtual DbSet<prefix_runnig> prefix_runnigs { get; set; }

    public virtual DbSet<sc_menu> sc_menus { get; set; }

    public virtual DbSet<sc_menu_program> sc_menu_programs { get; set; }

    public virtual DbSet<sc_menugroup> sc_menugroups { get; set; }

    public virtual DbSet<sc_program> sc_programs { get; set; }

 
    public virtual DbSet<sc_program_group> sc_program_groups { get; set; }

    public virtual DbSet<sc_role> sc_roles { get; set; }

    public virtual DbSet<sc_role_menu> sc_role_menus { get; set; }

    public virtual DbSet<sc_role_program> sc_role_programs { get; set; }

    public virtual DbSet<sc_user> sc_users { get; set; }

    public virtual DbSet<sc_user_role> sc_user_roles { get; set; }

    public virtual DbSet<stoa> stoas { get; set; }

    public virtual DbSet<task_activity> task_activities { get; set; }

    public virtual DbSet<task_assign> task_assigns { get; set; }

    public virtual DbSet<task_master> task_masters { get; set; }

    public virtual DbSet<time_checkin> time_checkins { get; set; }

    public virtual DbSet<toa> toas { get; set; }

    public virtual DbSet<upload_center> upload_centers { get; set; }

    public virtual DbSet<util_document> util_documents { get; set; }

    public virtual DbSet<vd_address> vd_addresses { get; set; }

    public virtual DbSet<vd_certificate> vd_certificates { get; set; }

    public virtual DbSet<vd_contact> vd_contacts { get; set; }

    public virtual DbSet<vd_doc> vd_docs { get; set; }

    public virtual DbSet<vd_financial> vd_financials { get; set; }

    public virtual DbSet<vd_general_info> vd_general_infos { get; set; }

    public virtual DbSet<vd_portfolio> vd_portfolios { get; set; }

    public virtual DbSet<vd_service> vd_services { get; set; }

    public virtual DbSet<vd_signed> vd_signeds { get; set; }

    public virtual DbSet<wf_adhoc_user> wf_adhoc_users { get; set; }

    public virtual DbSet<wf_budget> wf_budgets { get; set; }

    public virtual DbSet<wf_button> wf_buttons { get; set; }

    public virtual DbSet<wf_button_master> wf_button_masters { get; set; }

    public virtual DbSet<wf_checklist> wf_checklists { get; set; }

    public virtual DbSet<wf_condition> wf_conditions { get; set; }

    public virtual DbSet<wf_custom_role> wf_custom_roles { get; set; }

    public virtual DbSet<wf_custom_user> wf_custom_users { get; set; }

    public virtual DbSet<wf_customer_approver> wf_customer_approvers { get; set; }

    public virtual DbSet<wf_decision_status> wf_decision_statuses { get; set; }

    public virtual DbSet<wf_emailTemplate> wf_emailTemplates { get; set; }

    public virtual DbSet<wf_employee> wf_employees { get; set; }

    public virtual DbSet<wf_loa> wf_loas { get; set; }

    public virtual DbSet<wf_loa_user> wf_loa_users { get; set; }

    public virtual DbSet<wf_mas_reason> wf_mas_reasons { get; set; }

    public virtual DbSet<wf_org_type> wf_org_types { get; set; }

    public virtual DbSet<wf_organize> wf_organizes { get; set; }

    public virtual DbSet<wf_sub_workflow_master> wf_sub_workflow_masters { get; set; }

    public virtual DbSet<wf_workflow> wf_workflows { get; set; }

    public virtual DbSet<wf_workflow_in_workflow> wf_workflow_in_workflows { get; set; }

    // ----- Pay_* module (new, code-first — see Docs/Payroll design plan) -----
    public virtual DbSet<Pay_PayrollRun> Pay_PayrollRuns { get; set; }
    public virtual DbSet<Pay_PayrollEmployee> Pay_PayrollEmployees { get; set; }
    public virtual DbSet<Pay_PayrollLineItem> Pay_PayrollLineItems { get; set; }
    public virtual DbSet<Pay_PayItemType> Pay_PayItemTypes { get; set; }
    public virtual DbSet<Pay_TaxBracket> Pay_TaxBrackets { get; set; }
    public virtual DbSet<Pay_ProvidentFundElection> Pay_ProvidentFundElections { get; set; }
    public virtual DbSet<Pay_PayrollAuditLog> Pay_PayrollAuditLogs { get; set; }
    public virtual DbSet<Pay_Payslip> Pay_Payslips { get; set; }
    public virtual DbSet<Pay_BankFileExportBatch> Pay_BankFileExportBatches { get; set; }
    public virtual DbSet<Pay_BankFileExportLine> Pay_BankFileExportLines { get; set; }
    public virtual DbSet<Pay_GLExportBatch> Pay_GLExportBatches { get; set; }
    public virtual DbSet<Pay_GLExportEntry> Pay_GLExportEntries { get; set; }
    public virtual DbSet<Pay_AdhocPayItem> Pay_AdhocPayItems { get; set; }
    public virtual DbSet<Pay_PayslipSettings> Pay_PayslipSettings { get; set; }
    // ----- end Pay_* module -----

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=hrm;User Id=hrm;Password=hrm;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ALL_EMPLOYEE>(entity =>
        {
            entity.Property(e => e.STATUSEMP).IsFixedLength();
        });

        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedName] IS NOT NULL)");
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedUserName] IS NOT NULL)");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<CT_Contract>(entity =>
        {
            entity.HasOne(d => d.BankGuaranteeCurrency).WithMany(p => p.CT_ContractBankGuaranteeCurrencies).HasConstraintName("FK_CT_Contract_BankGuarantee");

            entity.HasOne(d => d.InsurancePolicyCurrency).WithMany(p => p.CT_ContractInsurancePolicyCurrencies).HasConstraintName("FK_CT_Contract_Currency_insurrance");

            entity.HasOne(d => d.ProjectCurency).WithMany(p => p.CT_ContractProjectCurencies).HasConstraintName("FK_CT_Contract_Project");
        });

        modelBuilder.Entity<ConsentHistory>(entity =>
        {
            entity.Property(e => e.id).ValueGeneratedNever();
        });

        modelBuilder.Entity<DocCheckList>(entity =>
        {
            entity.Property(e => e.orderTH).HasDefaultValue(99);

            entity.HasOne(d => d.masdoc).WithMany(p => p.DocCheckLists)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DocCheckList_mas_doc_type");
        });

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_HOLIDAY");
        });

        // ----- ผูกตารางเงินเดือน (Hr*/Kp*) ด้วยรหัสทางธุรกิจ companyid + EmpNo/PayrollslipNo/KpslipNo -----
        // เป็นความสัมพันธ์ระดับ EF model เท่านั้น ไม่มี FK constraint จริงในฐานข้อมูล (ไม่ได้สร้าง migration)
        modelBuilder.Entity<Hremployee>(entity =>
        {
            entity.HasAlternateKey(e => new { e.companyid, e.EmpNo });

            entity.HasMany(d => d.Hrpayrolls)
                .WithOne(p => p.Hremployee)
                .HasForeignKey(p => new { p.companyid, p.EmpNo })
                .HasPrincipalKey(p => new { p.companyid, p.EmpNo })
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasMany(d => d.HrwOts)
                .WithOne(p => p.Hremployee)
                .HasForeignKey(p => new { p.companyid, p.EmpNo })
                .HasPrincipalKey(p => new { p.companyid, p.EmpNo })
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // Hrpayroll = แม่ (หัวรายการเงินเดือนต่อรอบจ่าย), Hrpayrolldet = ลูก (รายการเงินได้/เงินหักแต่ละบรรทัด)
        // ผูกด้วย FK จริง Hrpayrolldet.HrpayrollId -> Hrpayroll.id (คอลัมน์ใหม่ ต้องสร้าง migration เพื่อเพิ่มคอลัมน์นี้ในฐานข้อมูลจริง)
        modelBuilder.Entity<Hrpayrolldet>(entity =>
        {
            entity.HasOne(d => d.Hrpayroll)
                .WithMany(p => p.Hrpayrolldets)
                .HasForeignKey(d => d.HrpayrollId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Kptempreceive>(entity =>
        {
            entity.HasAlternateKey(e => new { e.companyid, e.KpslipNo });

            entity.HasMany(d => d.Kptempreceivedets)
                .WithOne(p => p.Kptempreceive)
                .HasForeignKey(p => new { p.companyid, p.KpslipNo })
                .HasPrincipalKey(p => new { p.companyid, p.KpslipNo })
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
        // ----- จบส่วนผูกตารางเงินเดือน -----

        modelBuilder.Entity<J_DEPT_GROUP_V2>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_DEPT_GROUP_V2");

            entity.Property(e => e.MOD_DATE)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<J_HR_CHECK_DEPT>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_HR_CHECK_DEPT");

            entity.Property(e => e.MOD_DATE)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<PRRequisitionConfirm>(entity =>
        {
            entity.Property(e => e.id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<approve_level>(entity =>
        {
            entity.HasKey(e => e.approveid).HasName("PK_ApproveLevel");
        });

        modelBuilder.Entity<approver_budget>(entity =>
        {
            entity.HasKey(e => e.budgetid).HasName("PK_ApproverBudget");

            entity.Property(e => e.moddate)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<button_link>(entity =>
        {
            entity.HasKey(e => e.btlinkid).HasName("PK_ButtonLink");
        });

        modelBuilder.Entity<com_company>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_comp_company");
        });

        modelBuilder.Entity<com_organization>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_organization");
        });

        modelBuilder.Entity<com_organize_layer>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_organize_layer");
        });

        modelBuilder.Entity<doc_center>(entity =>
        {
            entity.HasOne(d => d.mas_doc_type).WithMany(p => p.doc_centers).HasConstraintName("FK_doc_center_mas_doc_type");
        });

        modelBuilder.Entity<emp_checkin>(entity =>
        {
            entity.HasOne(d => d.user).WithMany(p => p.emp_checkins)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_emp_checkin_sc_user");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_EMPLOYEE");

            // ผูกกับ Hremployee (พนักงานฝั่ง payroll เดิม) ด้วย EmployeeID = EmpNo
            // เป็นความสัมพันธ์ระดับ EF model เท่านั้น ไม่มี FK constraint จริงในฐานข้อมูล (ไม่ได้สร้าง migration)
            entity.HasMany(d => d.Hremployees)
                .WithOne(p => p.Employee)
                .HasForeignKey(p => p.EmpNo)
                .HasPrincipalKey(p => p.EmployeeID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<error_log_file>(entity =>
        {
            entity.HasKey(e => e.logid).HasName("PK_ErrorLogFile");

            entity.Property(e => e.logid).ValueGeneratedNever();
        });

        modelBuilder.Entity<error_messege>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_ErrorMessege");
        });

        modelBuilder.Entity<his_doc>(entity =>
        {
            entity.HasKey(e => e.his_id).HasName("PK_his_doc_1");

            entity.HasOne(d => d.doctype).WithMany(p => p.his_docs).HasConstraintName("FK_his_doc_mas_doc_type");
        });

        modelBuilder.Entity<his_doc_bak>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_his_doc");

            entity.Property(e => e.createon)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<job_master>(entity =>
        {
            entity.HasKey(e => e.jobmasterid)
                .HasName("PK_JOBMASTER")
                .IsClustered(false);

            entity.HasOne(d => d.workflow).WithMany(p => p.job_masters)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_job_master_wf_workflow");
        });

        modelBuilder.Entity<job_status>(entity =>
        {
            entity.HasKey(e => e.jobstatusid).HasName("PK_JobStatus");
        });

        modelBuilder.Entity<job_subworkflow_master>(entity =>
        {
            entity.HasKey(e => e.jobsubworkflowid).HasName("PK_JobSubworkflowmaster");
        });

        modelBuilder.Entity<job_user_list>(entity =>
        {
            entity.HasKey(e => e.jobapproverid).HasName("PK_JobApprover");

            entity.HasOne(d => d.jobmaster).WithMany(p => p.job_user_lists)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_job_user_list_job_master");

            entity.HasOne(d => d.mas_reason).WithMany(p => p.job_user_lists).HasConstraintName("FK_job_user_list_mas_reason1");

            entity.HasOne(d => d.subworkflowmaster).WithMany(p => p.job_user_lists).HasConstraintName("FK_job_user_list_wf_sub_workflow_master");

            entity.HasOne(d => d.user).WithMany(p => p.job_user_lists).HasConstraintName("FK_job_user_list_sc_user");
        });

        modelBuilder.Entity<Loa>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_LOA");

            entity.Property(e => e.id).ValueGeneratedNever();
        });

        modelBuilder.Entity<loa_type>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_LOAType");

            entity.Property(e => e.id).ValueGeneratedNever();
        });

        modelBuilder.Entity<log_system_log>(entity =>
        {
            entity.HasKey(e => e.id)
                .HasName("PK_CHANGELOG")
                .IsClustered(false);

            entity.Property(e => e.moddate)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<mas_EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_mas_EmailTempate");

            entity.Property(e => e.id).ValueGeneratedNever();
        });

        modelBuilder.Entity<mas_WarranteeType>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_WarrantyType");
        });

        modelBuilder.Entity<mas_address_type>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_mas_AddressType");

            entity.Property(e => e.moddate)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<mas_country>(entity =>
        {
            entity.HasKey(e => e.countryid).HasName("PK_Country");
        });

        modelBuilder.Entity<mas_doc_type>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_DPDocType");
        });

        modelBuilder.Entity<mas_province>(entity =>
        {
            entity.HasOne(d => d.country).WithMany(p => p.mas_provinces)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mas_province_mas_country");
        });

        modelBuilder.Entity<pc_RFQCondition>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_RFQCondition");

            entity.Property(e => e.id).ValueGeneratedNever();
        });

        modelBuilder.Entity<pc_RFQDocRequest>(entity =>
        {
            entity.HasOne(d => d.doctype).WithMany(p => p.pc_RFQDocRequests).HasConstraintName("FK_pc_RFQDocRequest_pc_RFQDocRequest");

            entity.HasOne(d => d.rfq).WithMany(p => p.pc_RFQDocRequests).HasConstraintName("FK_pc_RFQDocRequest_pc_rfq");
        });

        modelBuilder.Entity<pc_RFQServiceSelect>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_pc_RFQSeviceSelect");
        });

        modelBuilder.Entity<pc_RFQServiceVendor>(entity =>
        {
            entity.Property(e => e.isPriceApprove).HasDefaultValue(false);
            entity.Property(e => e.isRequstClarify).HasDefaultValue(false);
            entity.Property(e => e.negoCount).HasDefaultValue(0);

            entity.HasOne(d => d.rfq).WithMany(p => p.pc_RFQServiceVendors)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pc_RFQServiceVendor_pc_rfq");

            entity.HasOne(d => d.vendor).WithMany(p => p.pc_RFQServiceVendors)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pc_RFQServiceVendor_vd_general_info");
        });

        modelBuilder.Entity<pc_pr>(entity =>
        {
            entity.HasOne(d => d.pr_service_type).WithMany(p => p.pc_prs).HasConstraintName("FK_pc_pr_service_type");
        });

        modelBuilder.Entity<pc_pr_item>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_pc_PRItem");

            entity.HasOne(d => d.pc_pr).WithMany(p => p.pc_pr_items).HasConstraintName("FK_pc_pr_item_pc_pr");
        });

        modelBuilder.Entity<pc_rfqItem>(entity =>
        {
            entity.Property(e => e.PRICE_UNIT).HasDefaultValue(0.00m);
            entity.Property(e => e.TOTAL_VALUE).HasDefaultValue(0.00m);
            entity.Property(e => e.VALUE_PRICE).HasDefaultValue(0.00m);
            entity.Property(e => e.isSelect).HasDefaultValue(false);

            entity.HasOne(d => d.rfq).WithMany(p => p.pc_rfqItems).HasConstraintName("FK_pc_rfqItem_pc_rfq");
        });

        modelBuilder.Entity<pc_te>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_PcTe");
        });

        modelBuilder.Entity<pc_te_item>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_pc_TECriteria");

            entity.HasOne(d => d.pc_te).WithMany(p => p.pc_te_items).HasConstraintName("FK_pc_te_item_PcTe");
        });

        modelBuilder.Entity<pc_vd_RFQCondition>(entity =>
        {
            entity.Property(e => e.id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<pc_vd_te_item>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_pc_EvaluationVD");
        });

        modelBuilder.Entity<pc_vendor_RFQDocRequest>(entity =>
        {
            entity.HasOne(d => d.doctype).WithMany(p => p.pc_vendor_RFQDocRequests).HasConstraintName("FK_pc_vendor_RFQDocRequest_mas_doc_type");
        });

        modelBuilder.Entity<pc_vendor_quotation>(entity =>
        {
            entity.Property(e => e.isPriceApprove).HasDefaultValue(false);
            entity.Property(e => e.isRequisitionerState).HasDefaultValue(false);
            entity.Property(e => e.negoCount).HasDefaultValue(0);
        });

        modelBuilder.Entity<pc_vendor_quotation_Item>(entity =>
        {
            entity.Property(e => e.budget).HasDefaultValue(0m);
            entity.Property(e => e.isPriceAction).HasDefaultValue(false);
            entity.Property(e => e.isPriceRequisitionState).HasDefaultValue(false);
            entity.Property(e => e.isRequisitionerState).HasDefaultValue(false);
            entity.Property(e => e.isTechAction).HasDefaultValue(false);
        });

        modelBuilder.Entity<pdpa_compliance>(entity =>
        {
            entity.Property(e => e.id).ValueGeneratedNever();
        });

        modelBuilder.Entity<pdpa_datamart>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_con_datamart");
        });

        modelBuilder.Entity<pdpa_filePrivacy>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_con_filePrivacy");

            entity.Property(e => e.end_date).IsFixedLength();
        });

        modelBuilder.Entity<pdpa_log_convertEndDec>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_log_convertEndDec");
        });

        modelBuilder.Entity<po_inv>(entity =>
        {
            entity.Property(e => e.account_code).IsFixedLength();
            entity.Property(e => e.account_desc).IsFixedLength();
            entity.Property(e => e.approved_flag).IsFixedLength();
            entity.Property(e => e.authorization_status).IsFixedLength();
            entity.Property(e => e.cancel_flag).IsFixedLength();
            entity.Property(e => e.company_code).IsFixedLength();
            entity.Property(e => e.company_desc).IsFixedLength();
            entity.Property(e => e.costcenter_code).IsFixedLength();
            entity.Property(e => e.costcenter_desc).IsFixedLength();
            entity.Property(e => e.currency_code).IsFixedLength();
            entity.Property(e => e.invoice_description).IsFixedLength();
            entity.Property(e => e.invoice_id).IsFixedLength();
            entity.Property(e => e.invoice_line_desc).IsFixedLength();
            entity.Property(e => e.invoice_line_number).IsFixedLength();
            entity.Property(e => e.invoice_number).IsFixedLength();
            entity.Property(e => e.item_description).IsFixedLength();
            entity.Property(e => e.line_amount).IsFixedLength();
            entity.Property(e => e.line_num).IsFixedLength();
            entity.Property(e => e.org_id).IsFixedLength();
            entity.Property(e => e.po_date).IsFixedLength();
            entity.Property(e => e.po_description).IsFixedLength();
            entity.Property(e => e.po_number).IsFixedLength();
            entity.Property(e => e.product_code).IsFixedLength();
            entity.Property(e => e.product_desc).IsFixedLength();
            entity.Property(e => e.quantity).IsFixedLength();
            entity.Property(e => e.rate).IsFixedLength();
            entity.Property(e => e.rate_date).IsFixedLength();
            entity.Property(e => e.rate_type).IsFixedLength();
            entity.Property(e => e.unit_meas_lookup_code).IsFixedLength();
            entity.Property(e => e.unit_price).IsFixedLength();
            entity.Property(e => e.user_desc).IsFixedLength();
            entity.Property(e => e.user_name).IsFixedLength();
            entity.Property(e => e.vendor_code).IsFixedLength();
            entity.Property(e => e.vendor_name).IsFixedLength();
        });

        modelBuilder.Entity<pos_position>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__positionlist__4D1564AE");

            entity.Property(e => e.isvacancy).IsFixedLength();
            entity.Property(e => e.status).IsFixedLength();
        });

        modelBuilder.Entity<pr_service_type>(entity =>
        {
            entity.Property(e => e.moddate)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<prefix_runnig>(entity =>
        {
            entity.HasKey(e => e.gencodeid).HasName("PK_PreFixRunnig");
        });

        modelBuilder.Entity<sc_menu>(entity =>
        {
            entity.HasKey(e => e.menuid).HasName("PK__sc_menu__3B5F7D5C109083C8");

            entity.Property(e => e.enddate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.icon).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isshow).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.langcode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.menucode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.menuorder).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.moddate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.programid).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.small_icon).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.startdate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.uppermenucode).HasDefaultValueSql("(NULL)");

            entity.HasOne(d => d.menugroup).WithMany(p => p.sc_menus)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SC_Menu_sc_menugroup");
        });

        modelBuilder.Entity<sc_menu_program>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_SC_MenuProgram");
        });

        modelBuilder.Entity<sc_menugroup>(entity =>
        {
            entity.HasKey(e => e.menugroupid).HasName("PK_sc_menugroup_menugroupid");

            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.langcode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.menucode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.menugrouplevel).HasDefaultValueSql("(NULL)");
        });

        modelBuilder.Entity<sc_program>(entity =>
        {
            entity.HasKey(e => e.progid).HasName("PK_SC_PROGRAM");

            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.moddate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.progmastercode).HasDefaultValueSql("(NULL)");
        });

    

        modelBuilder.Entity<sc_program_group>(entity =>
        {
            entity.HasKey(e => e.programgroupid).HasName("PK_sc_programgroup_programgroupid");

            entity.Property(e => e.langcode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.proggroupcode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.proglevel).HasDefaultValueSql("(NULL)");
        });

        modelBuilder.Entity<sc_role>(entity =>
        {
            entity.HasKey(e => e.roleid).HasName("PK__sc_role__CD994BF2188A25C3");

            entity.Property(e => e.abbr).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.name).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.rolecode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.upperrole).HasDefaultValueSql("(NULL)");
        });

        modelBuilder.Entity<sc_role_menu>(entity =>
        {
            entity.HasKey(e => e.rolemenuid).HasName("PK__sc_rolem__6171F3F863B63A9E");

            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.moddate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.menu).WithMany(p => p.sc_role_menus)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SC_ROLE_MENU_SC_Menu");

            entity.HasOne(d => d.role).WithMany(p => p.sc_role_menus)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SC_ROLE_MENU_SC_Role");
        });

        modelBuilder.Entity<sc_role_program>(entity =>
        {
            entity.HasKey(e => e.roleprogid).HasName("PK_ROLE_PROGRAM");

            entity.HasOne(d => d.prog).WithMany(p => p.sc_role_programs).HasConstraintName("FK_ROLE_PRO_REFERENCE_SC_PROGR");

            entity.HasOne(d => d.role).WithMany(p => p.sc_role_programs).HasConstraintName("FK_ROLE_PRO_REFERENCE_SC_ROLE");
        });

        modelBuilder.Entity<sc_user>(entity =>
        {
            entity.HasKey(e => e.userid).HasName("PK_sc_user_userid");

            entity.Property(e => e.email).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.empid).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.enddate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.firstname).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.invalidpwcount).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.langcode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.lastinvalidpwd).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.lastname).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.lasttimelogin).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.loginname).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.mobilephone).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.moddate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.orgcode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.orgid).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.orgname).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.password).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.phone).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.pwdexpdate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.remindpwd).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.sex_sexid).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.social).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.startdate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.title).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.title_titleid).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.upperuserid).HasDefaultValueSql("(NULL)");
        });

        modelBuilder.Entity<sc_user_role>(entity =>
        {
            entity.HasKey(e => e.user_roleID).HasName("PK__sc_userr__BE814DEE1C50ACD6");

            entity.Property(e => e.enddate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.startdate).HasDefaultValueSql("(NULL)");

            entity.HasOne(d => d.role).WithMany(p => p.sc_user_roles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SC_USER_ROLE_SC_Role");

            entity.HasOne(d => d.user).WithMany(p => p.sc_user_roles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SC_USER_ROLE_SC_USER");
        });

        modelBuilder.Entity<stoa>(entity =>
        {
            entity.HasKey(e => e.stoaid).HasName("PK_STOA");
        });

        modelBuilder.Entity<task_assign>(entity =>
        {
            entity.HasOne(d => d.task).WithMany(p => p.task_assigns).HasConstraintName("FK_task_assign_Task_Master");
        });

        modelBuilder.Entity<task_master>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_Task_Master");
        });

        modelBuilder.Entity<time_checkin>(entity =>
        {
            entity.Property(e => e.id).ValueGeneratedNever();
        });

        modelBuilder.Entity<toa>(entity =>
        {
            entity.HasKey(e => e.toaid).HasName("PK_TOA");
        });

        modelBuilder.Entity<util_document>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_jb_document");

            entity.HasOne(d => d.doctype).WithMany(p => p.util_documents).HasConstraintName("FK_jb_document_DPDocType");
        });

        modelBuilder.Entity<vd_address>(entity =>
        {
            entity.HasOne(d => d.address_typeNavigation).WithMany(p => p.vd_addresses).HasConstraintName("FK_vd_address_mas_address_type");

            entity.HasOne(d => d.country).WithMany(p => p.vd_addresses).HasConstraintName("FK_vd_address_mas_country");

            entity.HasOne(d => d.provinceNavigation).WithMany(p => p.vd_addresses).HasConstraintName("FK_vd_address_mas_province");
        });

        modelBuilder.Entity<vd_doc>(entity =>
        {
            entity.HasOne(d => d.doctype).WithMany(p => p.vd_docs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_vd_doc_mas_doc_type");

            entity.HasOne(d => d.vendor).WithMany(p => p.vd_docs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_vd_doc_vd_general_info");
        });

        modelBuilder.Entity<vd_general_info>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_vd_Vendor");

            entity.HasOne(d => d.capitalCurrency).WithMany(p => p.vd_general_infos).HasConstraintName("FK_vd_general_info_vd_general_info");
        });

        modelBuilder.Entity<wf_adhoc_user>(entity =>
        {
            entity.HasOne(d => d.jobmaster).WithMany(p => p.wf_adhoc_users).HasConstraintName("FK_wf_adhoc_user_job_master");

            entity.HasOne(d => d.user).WithMany(p => p.wf_adhoc_users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_wf_adhoc_user_sc_user");
        });

        modelBuilder.Entity<wf_budget>(entity =>
        {
            entity.HasKey(e => e.wfcondiionid).HasName("PK_WF_CONDITION");

            entity.HasOne(d => d.subworkflow).WithMany(p => p.wf_budgets).HasConstraintName("FK_WF_CONDI_REFERENCE_WF_SUBWO");
        });

        modelBuilder.Entity<wf_button>(entity =>
        {
            entity.HasKey(e => e.wfbuttonid).HasName("PK_WF_Button");

            entity.HasOne(d => d.button_master).WithMany(p => p.wf_buttons)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_wf_button_wf_button_master");
        });

        modelBuilder.Entity<wf_checklist>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_WF_CheckList");

            entity.Property(e => e.id).ValueGeneratedNever();
        });

        modelBuilder.Entity<wf_condition>(entity =>
        {
            entity.HasKey(e => e.subworkflowid).HasName("PK_WF_Condition_1");

            entity.Property(e => e.subworkflowid).ValueGeneratedNever();
            entity.Property(e => e.wfcondiionid).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<wf_custom_role>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_WF_CUSTOMROLE");

            entity.Property(e => e.enddate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.startdate).HasDefaultValueSql("(NULL)");

            entity.HasOne(d => d.subworkflow).WithMany(p => p.wf_custom_roles).HasConstraintName("FK_WF_CUSTO_REFERENCE_WF_SUBWO");
        });

        modelBuilder.Entity<wf_custom_user>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_WF_Customuser_1");

            entity.Property(e => e.modate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.userid).HasDefaultValueSql("(NULL)");

            entity.HasOne(d => d.subworkflow).WithMany(p => p.wf_custom_users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WF_CUSTO_USR_REFERENCE_WF_SUBWO");

            entity.HasOne(d => d.user).WithMany(p => p.wf_custom_users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_wf_custom_user_sc_user");
        });

        modelBuilder.Entity<wf_customer_approver>(entity =>
        {
            entity.HasKey(e => e.subworkflowid).HasName("PK_WF_CustomApprover");
        });

        modelBuilder.Entity<wf_decision_status>(entity =>
        {
            entity.HasKey(e => e.workflowstatusid).HasName("PK_WorkflowDecisionStatus");

            entity.HasOne(d => d.subworkflow).WithMany(p => p.wf_decision_statuses).HasConstraintName("FK_WF_DecisionStatus_ToSWF");
        });

        modelBuilder.Entity<wf_employee>(entity =>
        {
            entity.Property(e => e.id).ValueGeneratedNever();
        });

        modelBuilder.Entity<wf_loa>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_WF_LOA");
        });

        modelBuilder.Entity<wf_loa_user>(entity =>
        {
            entity.Property(e => e.id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<wf_org_type>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_wf_OrgType");

            entity.Property(e => e.id).ValueGeneratedNever();
        });

        modelBuilder.Entity<wf_organize>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_WF_Organize");

            entity.Property(e => e.istop).IsFixedLength();
        });

        modelBuilder.Entity<wf_sub_workflow_master>(entity =>
        {
            entity.HasKey(e => e.subworkflowid).HasName("PK__wf_subwo__0D16B5CDB3240F76");

            entity.Property(e => e.andpercent).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.status).HasDefaultValueSql("(NULL)");
        });

        modelBuilder.Entity<wf_workflow>(entity =>
        {
            entity.HasKey(e => e.workflowid).HasName("PK__wf_workf__D00EA011B75E9C00");

            entity.Property(e => e.c_create).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.c_detail).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.c_edit).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.c_list).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.code).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.columnref).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isshow).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.lifetimeday).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.moddate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.tablename).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.tableref).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.url).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.wenddate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.wexpireday).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.wstartdate).HasDefaultValueSql("(NULL)");
        });

        // ----- Pay_* module (new, code-first) -----
        // Actor stamps (CreatedByUserId, ActorUserId, GeneratedByUserId, etc.) are
        // plain long/long? columns, not EF-configured FKs to sc_user — matches how
        // other "xxx_by" actor columns are stored elsewhere in this legacy schema,
        // and keeps sc_user deletable independent of payroll history.
        modelBuilder.Entity<Pay_PayrollRun>(entity =>
        {
            entity.HasOne(d => d.AdjustmentOfRun)
                .WithMany()
                .HasForeignKey(d => d.AdjustmentOfRunId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_PayrollEmployee>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollRun)
                .WithMany(p => p.Pay_PayrollEmployees)
                .HasForeignKey(d => d.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Hremployee)
                .WithMany(p => p.Pay_PayrollEmployees)
                .HasForeignKey(d => d.HremployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_PayrollLineItem>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollEmployee)
                .WithMany(p => p.Pay_PayrollLineItems)
                .HasForeignKey(d => d.PayrollEmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Pay_PayItemType)
                .WithMany(p => p.Pay_PayrollLineItems)
                .HasForeignKey(d => d.PayItemTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_ProvidentFundElection>(entity =>
        {
            entity.HasOne(d => d.Hremployee)
                .WithMany(p => p.Pay_ProvidentFundElections)
                .HasForeignKey(d => d.HremployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_PayrollAuditLog>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollRun)
                .WithMany(p => p.Pay_PayrollAuditLogs)
                .HasForeignKey(d => d.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // secondary path — Restrict avoids SQL Server's "multiple cascade
            // paths" error (Run already cascades to this table directly above)
            entity.HasOne(d => d.Pay_PayrollEmployee)
                .WithMany(p => p.Pay_PayrollAuditLogs)
                .HasForeignKey(d => d.PayrollEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_Payslip>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollEmployee)
                .WithOne(p => p.Pay_Payslip)
                .HasForeignKey<Pay_Payslip>(d => d.PayrollEmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pay_BankFileExportBatch>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollRun)
                .WithMany(p => p.Pay_BankFileExportBatches)
                .HasForeignKey(d => d.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pay_BankFileExportLine>(entity =>
        {
            entity.HasOne(d => d.Pay_BankFileExportBatch)
                .WithMany(p => p.Pay_BankFileExportLines)
                .HasForeignKey(d => d.BankFileExportBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // secondary path — Restrict avoids "multiple cascade paths" back to Run
            entity.HasOne(d => d.Pay_PayrollEmployee)
                .WithMany(p => p.Pay_BankFileExportLines)
                .HasForeignKey(d => d.PayrollEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_GLExportBatch>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollRun)
                .WithMany(p => p.Pay_GLExportBatches)
                .HasForeignKey(d => d.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pay_GLExportEntry>(entity =>
        {
            entity.HasOne(d => d.Pay_GLExportBatch)
                .WithMany(p => p.Pay_GLExportEntries)
                .HasForeignKey(d => d.GLExportBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Built-in pay item types — system-reserved, used directly by the
        // calculation service (BASE/OT/SSO/PF/TAX/LOAN). ALLOWANCE/ADJUST are
        // not reserved so admins can add more of that kind via the UI later.
        modelBuilder.Entity<Pay_PayItemType>().HasData(
            new Pay_PayItemType { Id = 1, Code = "BASE", NameTh = "เงินเดือนฐาน", NameEn = "Base Salary", Category = PayItemCategory.Earning, DefaultSignFlag = 1, IsSystemReserved = true, IsActive = true, SortOrder = 1 },
            new Pay_PayItemType { Id = 2, Code = "OT", NameTh = "ค่าล่วงเวลา", NameEn = "Overtime", Category = PayItemCategory.Earning, DefaultSignFlag = 1, IsSystemReserved = true, IsActive = true, SortOrder = 2 },
            new Pay_PayItemType { Id = 3, Code = "ALLOWANCE", NameTh = "เบี้ยเลี้ยง/เงินเพิ่มประจำ", NameEn = "Allowance", Category = PayItemCategory.Earning, DefaultSignFlag = 1, IsSystemReserved = false, IsActive = true, SortOrder = 3 },
            new Pay_PayItemType { Id = 4, Code = "SSO", NameTh = "ประกันสังคม", NameEn = "Social Security", Category = PayItemCategory.Deduction, DefaultSignFlag = -1, IsSystemReserved = true, IsActive = true, SortOrder = 4 },
            new Pay_PayItemType { Id = 5, Code = "PF", NameTh = "กองทุนสำรองเลี้ยงชีพ (พนักงาน)", NameEn = "Provident Fund (Employee)", Category = PayItemCategory.Deduction, DefaultSignFlag = -1, IsSystemReserved = true, IsActive = true, SortOrder = 5 },
            new Pay_PayItemType { Id = 6, Code = "TAX", NameTh = "ภาษีหัก ณ ที่จ่าย", NameEn = "Withholding Tax", Category = PayItemCategory.Deduction, DefaultSignFlag = -1, IsSystemReserved = true, IsActive = true, SortOrder = 6 },
            new Pay_PayItemType { Id = 7, Code = "LOAN", NameTh = "หักเงินกู้", NameEn = "Loan Deduction", Category = PayItemCategory.Deduction, DefaultSignFlag = -1, IsSystemReserved = true, IsActive = true, SortOrder = 7 },
            new Pay_PayItemType { Id = 8, Code = "ADJUST", NameTh = "ปรับปรุงพิเศษ", NameEn = "Special Adjustment", Category = PayItemCategory.Informational, DefaultSignFlag = 1, IsSystemReserved = false, IsActive = true, SortOrder = 8 },
            new Pay_PayItemType { Id = 9, Code = "BONUS", NameTh = "โบนัส/ค่าคอมมิชชั่นเฉพาะกิจ", NameEn = "Bonus / Commission (ad-hoc)", Category = PayItemCategory.Earning, DefaultSignFlag = 1, IsSystemReserved = false, IsActive = true, SortOrder = 9 },
            new Pay_PayItemType { Id = 10, Code = "ADHOC_DEDUCT", NameTh = "หักเฉพาะกิจ (เช่น ค่าเสียหาย/ชุดยูนิฟอร์ม)", NameEn = "Ad-hoc Deduction", Category = PayItemCategory.Deduction, DefaultSignFlag = -1, IsSystemReserved = false, IsActive = true, SortOrder = 10 }
        );

        // Standard Thai personal-income-tax brackets, effective year 2026 (ค.ศ.).
        // These are the long-standing official PIT brackets — confirm against
        // the current Revenue Department schedule before relying on them for
        // a real payroll run; adjust via the Pay_TaxBracket admin CRUD page.
        modelBuilder.Entity<Pay_TaxBracket>().HasData(
            new Pay_TaxBracket { Id = 1, EffectiveYear = 2026, Step = 1, MinIncome = 0m, MaxIncome = 150000m, RatePercent = 0m, IsActive = true },
            new Pay_TaxBracket { Id = 2, EffectiveYear = 2026, Step = 2, MinIncome = 150000m, MaxIncome = 300000m, RatePercent = 5m, IsActive = true },
            new Pay_TaxBracket { Id = 3, EffectiveYear = 2026, Step = 3, MinIncome = 300000m, MaxIncome = 500000m, RatePercent = 10m, IsActive = true },
            new Pay_TaxBracket { Id = 4, EffectiveYear = 2026, Step = 4, MinIncome = 500000m, MaxIncome = 750000m, RatePercent = 15m, IsActive = true },
            new Pay_TaxBracket { Id = 5, EffectiveYear = 2026, Step = 5, MinIncome = 750000m, MaxIncome = 1000000m, RatePercent = 20m, IsActive = true },
            new Pay_TaxBracket { Id = 6, EffectiveYear = 2026, Step = 6, MinIncome = 1000000m, MaxIncome = 2000000m, RatePercent = 25m, IsActive = true },
            new Pay_TaxBracket { Id = 7, EffectiveYear = 2026, Step = 7, MinIncome = 2000000m, MaxIncome = 5000000m, RatePercent = 30m, IsActive = true },
            new Pay_TaxBracket { Id = 8, EffectiveYear = 2026, Step = 8, MinIncome = 5000000m, MaxIncome = null, RatePercent = 35m, IsActive = true }
        );

        modelBuilder.Entity<Pay_AdhocPayItem>(entity =>
        {
            entity.HasOne(d => d.Hremployee)
                .WithMany(p => p.Pay_AdhocPayItems)
                .HasForeignKey(d => d.HremployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Pay_PayItemType)
                .WithMany()
                .HasForeignKey(d => d.PayItemTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // no navigation collection back on Pay_PayrollRun — this is a
            // many-to-one lookup only (which run consumed this item), not
            // part of the run's owned object graph
            entity.HasOne(d => d.ConsumedByPayrollRun)
                .WithMany()
                .HasForeignKey(d => d.ConsumedByPayrollRunId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_PayslipSettings>().HasData(
            new Pay_PayslipSettings { Id = 1, CompanyId = "001", PasswordTemplate = "{BirthDateDDMMYYYY}", ModifiedDate = new DateTime(2026, 7, 29) }
        );
        // ----- end Pay_* module -----

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

public DbSet<HRM.Models.HRTaxRate> HRTaxRate { get; set; } = default!;
}
