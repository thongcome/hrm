using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddInsuranceAutoEnrollAndAssociation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoEnrollEnabled",
                table: "Pay_InsurancePlan",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EligibleEmploymentTypes",
                table: "Pay_InsurancePlan",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupPolicyNumber",
                table: "Pay_InsurancePlan",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAge",
                table: "Pay_InsurancePlan",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinAge",
                table: "Pay_InsurancePlan",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinTenureDays",
                table: "Pay_InsurancePlan",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryName",
                table: "Pay_EmployeeInsuranceEnrollment",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryRelationship",
                table: "Pay_EmployeeInsuranceEnrollment",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CardExpiryDate",
                table: "Pay_EmployeeInsuranceEnrollment",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CardIssueDate",
                table: "Pay_EmployeeInsuranceEnrollment",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MemberCertificateNumber",
                table: "Pay_EmployeeInsuranceEnrollment",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsReview",
                table: "Pay_EmployeeInsuranceEnrollment",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "Pay_EmployeeInsuranceEnrollment",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoEnrollEnabled",
                table: "Pay_InsurancePlan");

            migrationBuilder.DropColumn(
                name: "EligibleEmploymentTypes",
                table: "Pay_InsurancePlan");

            migrationBuilder.DropColumn(
                name: "GroupPolicyNumber",
                table: "Pay_InsurancePlan");

            migrationBuilder.DropColumn(
                name: "MaxAge",
                table: "Pay_InsurancePlan");

            migrationBuilder.DropColumn(
                name: "MinAge",
                table: "Pay_InsurancePlan");

            migrationBuilder.DropColumn(
                name: "MinTenureDays",
                table: "Pay_InsurancePlan");

            migrationBuilder.DropColumn(
                name: "BeneficiaryName",
                table: "Pay_EmployeeInsuranceEnrollment");

            migrationBuilder.DropColumn(
                name: "BeneficiaryRelationship",
                table: "Pay_EmployeeInsuranceEnrollment");

            migrationBuilder.DropColumn(
                name: "CardExpiryDate",
                table: "Pay_EmployeeInsuranceEnrollment");

            migrationBuilder.DropColumn(
                name: "CardIssueDate",
                table: "Pay_EmployeeInsuranceEnrollment");

            migrationBuilder.DropColumn(
                name: "MemberCertificateNumber",
                table: "Pay_EmployeeInsuranceEnrollment");

            migrationBuilder.DropColumn(
                name: "NeedsReview",
                table: "Pay_EmployeeInsuranceEnrollment");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "Pay_EmployeeInsuranceEnrollment");
        }
    }
}
