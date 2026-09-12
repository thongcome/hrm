namespace Advance.Payroll.Contracts;

// Replaces Services/Pay/Calculators/LoanDeductionCalculator.cs's
// GetLoanDeductionsForPeriodByMemberAsync (queries Kptempreceive/Kptempreceivedet, the
// legacy cooperative-loan module) — called from PayrollCalculationService.cs:274/655-661.
// Keyed by the cooperative member number (Hremployee.RefMembno), matching the original.
// This is SEPARATE from Pay_EmployeeLoan/Pay_EmployeeLoanInstallment (the HR-proxy
// company-loan pathway, which is already Payroll's own table — no seam needed there).
// Advance.Payroll Lite has no cooperative module (plan section 2.1: "Lite: ไม่มี ใช้
// Pay_EmployeeLoan ที่มีอยู่แล้วแทน") — its implementation returns an empty feed;
// HR loans still flow entirely through Pay_EmployeeLoan, unaffected by this interface.
public interface ILoanFeed
{
    Task<IReadOnlyDictionary<string, decimal>> GetCooperativeLoanDeductionsByMemberNoAsync(
        string companyId, string recvPeriod, CancellationToken ct = default);
}
