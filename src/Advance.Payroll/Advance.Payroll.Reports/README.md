# Advance.Payroll.Reports

Deliberately near-empty in Phase 0 (per the task brief: "don't over-engineer this one").

The e-Filing/bank-format/GL/certificate generation work that would eventually live here
already exists today — inside **Advance.Payroll.Engine**, ported as-is from HRM:

- `EFilingExportService.cs` — ภ.ง.ด.1 / ภ.ง.ด.1ก text files (RD Prep format), สปส.1-10
  (SSO e-Service format). Pure formatting half already split out to
  `Advance.Payroll.Core.EFilingFormats`.
- `BankFileExportService.cs` + `Advance.Payroll.Core.BankFileTemplate` — company-configurable
  bank transfer file (CSV or any bank's fixed-width spec via `Pay_BankFileFormat`).
- `GLExportService.cs` — general-ledger export batch.
- `Por1DataService.cs` / `Por1PdfService.cs` — ภ.ง.ด.1 report PDF.
- `WithholdingCertificateDataService.cs` / `WithholdingCertificatePdfService.cs` — 50 ทวิ.
- `SalaryCertificateDataService.cs` / `SalaryCertificatePdfService.cs` — เอกสารรับรองเงินเดือน.
- `PayslipGenerationService.cs` / `PayslipPdfService.cs` — payslip PDF + password.

**Why not split now**: these files are still small enough (150-300 lines each) and share
enough plumbing (PayrollDbContext, PrivateFileStorage, the Pay_*ExportBatch entities) that
carving them into a separate project today would mean re-wiring DI and file-storage access
across a project boundary for no immediate benefit — nothing outside Payroll needs to call
report generation without also having the Engine loaded. The natural split point is if/when
Advance.Payroll.Web (Phase 3, the SaaS host) wants report generation to run as a separate
scaled-out worker from the calculation engine; revisit then.

See `EXTRACTION-PLAN.md` at the repo root of this extraction for the full picture.
