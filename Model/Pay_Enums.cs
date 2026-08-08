namespace HRM.Models;

public enum PayrollRunStatus
{
    Draft = 0,
    Calculated = 1,
    Reviewed = 2,
    Approved = 3,
    Posted = 4,
    Paid = 5,
    Cancelled = 9
}

public enum PayrollRunType
{
    Regular = 0,
    Adjustment = 1,
    Bonus = 2
}

public enum PayItemCategory
{
    Earning = 0,
    Deduction = 1,
    Informational = 2
}

public enum PayLineSourceType
{
    Base = 0,
    Overtime = 1,
    Loan = 2,
    SocialSecurity = 3,
    ProvidentFund = 4,
    Tax = 5,
    Allowance = 6,
    Adjustment = 7,
    Insurance = 8,
    WelfareFund = 9
}

public enum PayAuditEventType
{
    StatusTransition = 0,
    TaxCalculationDetail = 1,
    ManualAdjustment = 2,
    RecalculationTriggered = 3,
    ExclusionChanged = 4,
    Error = 5
}

public enum BankFileExportStatus
{
    Generated = 0,
    Downloaded = 1,
    ConfirmedSent = 2
}

public enum PayAdhocItemStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
    Consumed = 4
}

public enum Pay_EmployeeDocumentType
{
    IdCard = 0,
    Contract = 1,
    EducationCertificate = 2,
    ResignationLetter = 3,
    BankBook = 4,
    WorkPermit = 5,
    Visa = 6,
    DrivingLicense = 7,
    ProfessionalLicense = 8,
    Other = 9
}

public enum Pay_EmployeeLoanStatus
{
    Active = 0,
    PaidOff = 1,
    Cancelled = 2
}

public enum Pay_LoanInstallmentStatus
{
    Pending = 0,
    Consumed = 1,
    Cancelled = 2
}

public enum Pay_InsurancePlanType
{
    Health = 0,
    Life = 1,
    Accident = 2,
    Other = 9
}
