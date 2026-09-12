# Advance.Payroll extraction plan — Phase 0 prep

Written 12 ก.ย. 2569 as the Phase 0 deliverable of `docs/Plan_Split_Payroll_Workflow_v1.1.md`
(see that file for the 3-product target architecture: HumanOk / Advance.Workflow /
Advance.Payroll). Everything under `src/Advance.Payroll/` in this worktree is prep only —
**nothing in HRM proper (`Services/`, `Model/`, `Components/`, `Endpoints/`, `Program.cs`,
`HRM.csproj`/`HRM.sln`) was modified**, per this task's rules. This document is the honest,
file-and-line-level record of what a real cutover would touch.

## 0. Folder structure produced

```
src/Advance.Payroll/
├── Advance.Payroll.Core          — pure calculators, ZERO EF Core reference (builds clean)
├── Advance.Payroll.Domain        — Pay_* (45) + Hrucfsecurity EF entities (builds clean)
├── Advance.Payroll.Contracts     — the 7 seam interfaces + ISocialSecurityRateProvider (builds clean)
├── Advance.Payroll.Data          — PayrollDbContext (builds clean)
├── Advance.Payroll.Engine        — calculation/workflow/report services (partial — see §5)
├── Advance.Payroll.Reports       — near-empty, README only (by design — see its README.md)
├── Advance.Payroll.Blazor        — RCL, 1 of the planned 3-4 pages ported (see §7)
└── EXTRACTION-PLAN.md            — this file
```

## 1. DELETE list for HRM at cutover time

Delete only after Advance.Payroll is a real, tested, referenced package (end of plan Phase 2,
not before). Everything below is exactly what this worktree copied from — nothing more:

- `Model/Pay_*.cs` — all 45 files (see `src/Advance.Payroll/Advance.Payroll.Domain/` for the
  exact list; it's a 1:1 copy).
- `Model/Hrucfsecurity.cs`.
- `Services/Pay/**` — all 39 files, including `Services/Pay/Calculators/*.cs` and
  `Services/Pay/Exceptions/*.cs`.
- `Components/Pages/Pay/**` — 41 admin pages + `Dashboard/` and `Reports/` subfolders (already
  noted as "legacy payroll pages hidden" for the OLD `Services/Payroll/PayrollCalculationService.cs`
  stub in a prior commit — **do not confuse the two**: `Services/Payroll/` (singular,
  `HRM.Services.Payroll` namespace, registered at `Program.cs:453`) is the dead legacy stub
  already superseded; `Services/Pay/` (this extraction) is the live engine).
- `Endpoints/PayrollFileEndpoints.cs` (serves `PrivateFileStorage`-backed payslip/bank/GL
  files — grep confirmed this is the only endpoint file touching `Pay_*` download routes).
- `HRM.Tests/Pay/**` and `HRM.Tests/Integration/Payroll2025ScenarioTests.cs` — these become
  Advance.Payroll's own test suite (plan §4, Phase 2: "เทสทั้งปี 2568 ย้ายไปเป็นชุดตรวจรับของ
  Payroll"), not deleted, **moved**.
- The dead legacy stub `Services/Payroll/PayrollCalculationService.cs` (singular) — separate,
  smaller cleanup, can go anytime independent of this extraction.

**Do NOT delete**: `Model/emp_overtime_request.cs` and the `emp_overtime_requests` DbSet —
still used by non-Payroll attendance/timesheet features; only `PayrollPreflightService.cs`'s
pending-OT checklist item read it (see §6 "seams beyond the original 7" — that one check was
dropped in the port, not migrated).

## 2. Exact HRMContext.cs / HRMContext.Payroll.cs lines to remove

`Model/HRMContext.cs`:
- Line 21: `public virtual DbSet<Hrucfsecurity> Hrucfsecuritys { get; set; }`
- Lines 354-436 and 548-550: every `Pay_*` DbSet declaration (see the file — clearly bracketed
  by `// ----- Pay_* module (new, code-first...) -----` at 354 and `// ----- end Pay_* module
  -----` at 625, plus the 3 stray ones at 548-550 that were added later and fall outside that
  comment block — grep `DbSet<Pay_` to catch all of them, don't trust the comment markers alone).
- Lines 1502-2038: the entire `ConfigurePayrollModel`-equivalent inline block in
  `OnModelCreating` (bracketed by the same `// ----- Pay_* module (new, code-first) -----` /
  `// ----- end Pay_* module -----` comments) — **except** lines 1548-1558
  (`Eng_SurveyAnswer` config) which is NOT payroll-related and was just interleaved in the same
  region; leave that one in place.
- The `Pay_PayItemType.HasData(...)` seed (1743-1755), `Pay_TaxBracket.HasData(...)` seed
  (1761-1770), `Pay_PayslipSettings.HasData(...)` seed (1900-1902), and `Pay_PayrollPeriod
  .HasData(...)` seed (2034-2037) are already reproduced verbatim in
  `Advance.Payroll.Data/PayrollDbContext.cs` — remove from HRM only after confirming
  Advance.Payroll's own migration history seeds the same Ids (existing HRM databases already
  have these rows; a NEW migration on the Advance.Payroll side must not re-insert them if HRM
  and Advance.Payroll ever share one physical database during a transition window — plan §6
  decision 3 assumes they do share SQL Server for now).

`Model/HRMContext.Payroll.cs` (entire file, 30 lines): every DbSet in it
(`Pay_PayrollRunHolds`, `Pay_AttendanceDeductionPolicies`, `Pay_SalaryAdvances`,
`Pay_BankFileFormats`) plus `ConfigurePayrollModel`'s 2 trigger declarations
(`trg_PayrollEmployee_Immutable`, `trg_PayrollLineItem_Immutable`) — all already ported into
`Advance.Payroll.Data/PayrollDbContext.cs`. Also remove the call to `ConfigurePayrollModel(...)`
from wherever `OnModelCreatingPartial` invokes it (`Model/HRMContext.Security.cs` per that
file's own comment — verify at cutover, not re-checked here).

## 3. Exact Program.cs DI lines to remove + the `AddAdvancePayroll(...)` replacement

Lines to remove from `Program.cs` (all confirmed by grep against this worktree):
```
457: builder.Services.AddScoped<ISocialSecurityRateProvider, HrucfsecurityRateProvider>();
460: builder.Services.AddScoped<HRM.Services.Pay.PayrollAnomalyDetectionService>();
461: builder.Services.AddScoped<HRM.Services.Pay.PayrollCalculationService>();
462: builder.Services.AddScoped<PayrollWorkflowService>();
464: builder.Services.AddScoped<PayslipGenerationService>();
466: builder.Services.AddScoped<BankFileExportService>();
467: builder.Services.AddScoped<GLExportService>();
468: builder.Services.AddScoped<HRM.Services.Pay.SeveranceService>();
469: builder.Services.AddScoped<HRM.Services.Pay.EmployeeLoanService>();
470: builder.Services.AddScoped<HRM.Services.Pay.InsuranceAutoEnrollService>();
471: builder.Services.AddScoped<HRM.Services.Pay.DocumentExpiryService>();
472: builder.Services.AddScoped<HRM.Services.Pay.ProvidentFundRateMatrixService>();
473: builder.Services.AddScoped<HRM.Services.Pay.ProvidentFundRateChangeRequestService>();
474: builder.Services.AddScoped<HRM.Services.Pay.ProvidentFundExitCaseService>();
475: builder.Services.AddScoped<HRM.Services.Pay.EmployeeRehireService>();
603: builder.Services.AddScoped<HRM.Services.Pay.PayDateService>();
604: builder.Services.AddSingleton<HRM.Services.Pay.PayrollCalcJobRegistry>();
605: builder.Services.AddSingleton<HRM.Services.Pay.PayrollCalcJobService>();
607: builder.Services.AddScoped<HRM.Services.Pay.PayrollPreflightService>();
```
Also lines 185-186 (`HRM.Services.Pay.EFilingFormats.SsoPrefixCodeMale = ...`) — becomes
`Advance.Payroll.Core.EFilingFormats.SsoPrefixCodeMale = ...` inside `AddAdvancePayroll`, not a
top-level `Program.cs` line anymore (config-driven init belongs inside the extension method
that owns the config section).

**Not removed** (stays a separate line, unrelated dead code): line 453
`builder.Services.AddScoped<HRM.Services.Payroll.PayrollCalculationService>()` — the OLD
legacy stub's DI registration, in the `HRM.Services.Payroll` (singular) namespace. Cleaning
this up is the same independent small task noted in §1.

Replacement — one extension method, `AddAdvancePayroll`, living in
`Advance.Payroll.Engine/ServiceCollectionExtensions.cs` (not yet written in this worktree —
it needs `Microsoft.Extensions.DependencyInjection.Abstractions`, which would be the Engine
project's first new PackageReference):

```csharp
public static IServiceCollection AddAdvancePayroll(this IServiceCollection services, IConfiguration config)
{
    services.AddDbContextFactory<PayrollDbContext>(o => o.UseSqlServer(config.GetConnectionString("Default")));
    services.AddScoped<ISocialSecurityRateProvider, HrucfsecurityRateProvider>();
    services.AddScoped<PayrollAnomalyDetectionService>();
    services.AddScoped<PayrollCalculationService>();
    services.AddScoped<PayrollWorkflowService>();
    services.AddScoped<PayslipGenerationService>();
    services.AddScoped<BankFileExportService>();
    services.AddScoped<GLExportService>();
    services.AddScoped<PrivateFileStorage>();
    services.AddScoped<PayDateService>();
    services.AddScoped<PayrollPreflightService>();
    Core.EFilingFormats.SsoPrefixCodeMale = config["EFiling:SsoPrefixCodeMale"] ?? Core.EFilingFormats.SsoPrefixCodeMale;
    Core.EFilingFormats.SsoPrefixCodeFemale = config["EFiling:SsoPrefixCodeFemale"] ?? Core.EFilingFormats.SsoPrefixCodeFemale;
    // NOT included: PayrollCalcJobRegistry/PayrollCalcJobService (background-job wrapper —
    // not one of the 12 files this Phase 0 pass ported; needs its own IEmployeeSource-style
    // look before moving), SeveranceService/EmployeeLoanService/InsuranceAutoEnrollService/
    // DocumentExpiryService/ProvidentFundRateMatrixService/ProvidentFundRateChangeRequestService/
    // ProvidentFundExitCaseService/EmployeeRehireService — none of these were in the task's
    // required-12 list either; see §6 for what they still need (mostly IWorkflowGateway +
    // IEmployeeSource) before they can move.
    // Callers must ALSO register their IEmployeeSource/IAttendanceFeed/IOvertimeFeed/ILoanFeed/
    // IAllowanceFeed/IHolidayCalendarSource/IWorkflowGateway implementations — this method
    // deliberately does not assume HRM is present (Lite registers its own instead).
    return services;
}
```

## 4. pay_employee sync design

**Shape** (= `Advance.Payroll.Contracts.PayEmployeeSnapshot`, already written and verified
against every `emp.<Column>` read across the 12 ported Engine files, not guessed): `HremployeeId,
EmpNo, CompanyId, EmpName, EmpSurname, Sex, IdCard, BirthDate, WorkDate, ResignDate,
ProbationConfirmedDate, SalaryAmt, DailyWage, SalexpBank, SalexpBranch, SalexpAccid,
CostCenterCode, PosCode, EmptypeCode, OrgCode, ProvfEmprate, ProvfCorprate, RefMembno,
AdnEmail` — 23 columns (plan §2.1 estimated "~25"; the 2 fewer are `Hremployee.id` and
`companyid`, folded into `HremployeeId`/`CompanyId` naming, not missing data).

**Decision: HRM implements `IEmployeeSource` by reading `Hremployee` directly — no
`pay_employee` sync table inside HRM.** The plan document's own text (§2.1, §3.1, §6 decision
5) proposed a synced `pay_employee` table for HumanOk too ("Payroll ซิงก์เข้า pay_employee ก่อน
คำนวณทุกครั้ง"), but building and testing this Phase 0 slice surfaced a simpler path for the
HumanOk case specifically:

- HumanOk already has one live, current copy of employee data (`Hremployee`) and one process
  (the ASP.NET app) with a live DB connection to both HRM's and Payroll's tables in the same
  SQL Server instance (plan §6 decision 3: shared DB until Phase 3).
  `HRM.Payroll.EmployeeSourceAdapter : IEmployeeSource` (to write at Phase 2) is a ~30-line
  class projecting `Hremployee` -> `PayEmployeeSnapshot` on every call — zero staleness
  window, zero sync job to schedule/monitor/fail, zero second copy of PII to audit/PDPA-badge
  separately (`Model/HRMContext.Audit.cs`'s interceptor already covers `Hremployee` writes;
  a synced copy would need its own read-audit story per CLAUDE.md's PDPA rule).
- A sync job earns its cost only when the reader is a SEPARATE process/deployment from the
  writer — which is exactly the Advance.Payroll **Lite/SaaS** case (Phase 3): there, Payroll
  Lite genuinely IS its own deployment with no live connection to any `Hremployee` table, so
  `pay_employee` becomes Lite's own employee master (plan §3.1's "Lite เป็นทะเบียนพนักงานตัวจริง")
  — not a sync target at all, just Lite's normal data-entry table implementing
  `IEmployeeSource` against itself.
- **So "pay_employee" is real for Advance.Payroll Lite (Phase 3) and is NOT needed for
  HumanOk-embeds-Payroll (Phase 2)** — HumanOk's `IEmployeeSource` reads `Hremployee` live,
  same-process, same-transaction-boundary-adjacent (not literally same transaction, since
  `PayrollDbContext` and `HRMContext` are different `DbContext`s even against one database,
  but same request, same latency budget). This is a deliberate correction to the plan
  document's text, made because Phase 0 required actually defining `IEmployeeSource`'s
  shape and that work showed the simpler path — flag this for the CEO/plan owner's
  confirmation before Phase 2, don't treat it as silently settled.
- If a future need DOES arise for HumanOk to run Payroll's calculation in a separate process
  from HRM (e.g. a scaled-out calc worker), THEN a real `pay_employee` sync table is the
  right move, triggered the same way plan §3's `IOrgDirectorySource` sync is described for
  Workflow.Org ("ซิงก์เข้า Workflow.Org ทุกครั้งที่ผัง/คนเปลี่ยน") — but that trigger doesn't
  exist today and shouldn't be built speculatively.

## 5. Engine compile status (per file, all 12 in the task's required list + extras)

Build attempted via `dotnet build -p:UseAppHost=false` in `Advance.Payroll.Engine/`; see §8 for
the actual run's outcome (this environment's builds run 5-20 min each; timing noted where it
matters).

| File | Seam status |
|---|---|
| `PayrollCalculationService.cs` | **Fully wired** — every one of the 7 interfaces is called; zero remaining `TODO(seam)`. The flagship adaptation. |
| `PayrollAnomalyDetectionService.cs` | Wired via `IEmployeeSource`. **One open seam** (not one of the 7): the "new employee / onboarding not started" check reads HRM's `Hrd_LifecycleTaskInstance` — no interface exists for it; the onboarding-set lookup is hard-coded empty (see file header). |
| `PayDateService.cs` | Fully wired via `IHolidayCalendarSource`. |
| `HrucfsecurityRateProvider.cs` / `ISocialSecurityRateProvider.cs` | No seam needed — `Hrucfsecurity` is Payroll's own table (in Domain). |
| `PayrollWorkflowService.cs` | **No seam needed at all** — this file never touched Hremployee or any other external table; it's 100% Pay_* state-machine logic. Copies clean with only namespace/`HRMContext`->`PayrollDbContext` changes. |
| `BankFileExportService.cs` | Wired via `IEmployeeSource`. |
| `GLExportService.cs` | **No seam needed** — purely Pay_* aggregation, copies clean. |
| `Por1DataService.cs` | Wired via `IEmployeeSource` (signature changed: both `BuildMonthlyAsync`/`BuildAnnualAsync` now take an `IEmployeeSource` parameter — callers `EFilingExportService` and the future `Por1PdfService`-facing page updated to match). |
| `WithholdingCertificateDataService.cs` | Wired via `IEmployeeSource` for name/IdCard. **One open seam**: registered-address lookup (`addresses` table, HRM's PDPA-tracked home-address table) has no interface — `RegisteredAddressBlock` always returns null until one exists. |
| `EFilingExportService.cs` | Wired via `IEmployeeSource`; its previously-nested `EFilingFormats` static class was extracted to `Advance.Payroll.Core.EFilingFormats` per the task brief (a mechanical split, not a rewrite — every method is byte-identical). Same open address seam as above for ภ.ง.ด.1ก's address columns. |
| `PayslipGenerationService.cs` / `PayslipPdfService.cs` / `PayslipPasswordService.cs` | Wired via `IEmployeeSource` (password derivation and PDF name line both now take resolved employee data instead of a live `Hremployee` navigation). |
| `PayrollPreflightService.cs` | Wired via `IEmployeeSource` for the eligibility/issues list and the hold-by-empno lookup. **One open seam**: the pending-OT checklist item read `emp_overtime_requests` directly (a table this preflight check used that's DIFFERENT from `HrwOt`/`IOvertimeFeed` — an inconsistency that predates this extraction) — dropped rather than guessed at; see file header. |

Extras ported beyond the required 12 (all clean, no seams): `PayrollSpikeDetector.cs`
(kept in Engine, not Core, because it needs `Microsoft.ML` — see its own header comment for
why that's deliberately NOT added to Core's zero-dependency story),
`Exceptions/InvalidPayrollStatusTransitionException.cs`, `TaxableIncomeHelper.cs`,
`PayrollLineItemDisplayHelper.cs`, `PayrollAction.cs`, `BankFileValues.cs`,
`PrivateFileStorage.cs`, `Por1PdfService.cs`, `WithholdingCertificatePdfService.cs` (the last
two not inspected line-by-line for seams in this pass — they consume already-resolved
data records from their `*DataService` counterparts above, so they're expected clean, but
verify at Phase 2).

**Not ported at all** (outside the task's required-12 list, stay in HRM for now — see §6 for
what each still needs before it CAN move): `SeveranceService.cs`, `EmployeeLoanService.cs`,
`InsuranceAutoEnrollService.cs`, `DocumentExpiryService.cs`, `ProvidentFundRateMatrixService.cs`,
`ProvidentFundRateChangeRequestService.cs`, `ProvidentFundExitCaseService.cs`,
`EmployeeRehireService.cs`, `RecurringPayItemService.cs`, `PayrollDashboardService.cs`,
`PayrollCalcJobRegistry.cs`, `PayrollCalcJobService.cs`, `SalaryCertificateDataService.cs`,
`SalaryCertificatePdfService.cs`.

## 6. Seams beyond the original 7 (discovered, not guessed at)

The task's 7 interfaces covered the plan document's own dependency table (§2.1) accurately for
`PayrollCalculationService.cs`, but 3 more real dependencies turned up in the other 7 required
files that the plan didn't call out:

1. **Onboarding status** (`Hrd_LifecycleTaskInstance`, read by `PayrollAnomalyDetectionService`)
   — an HR-lifecycle checklist concept. Advance.Payroll Lite has no onboarding module at all
   (plan §5's "ไม่มี" list), so this anomaly check may simply not exist in Lite. For
   HumanOk-embeds-Payroll, an 8th interface (`IOnboardingStatusSource`) would be a ~10-line
   addition if the check is wanted; today the check always fires its "no onboarding started"
   warning (false-positive-safe direction, never silently suppressed).
2. **Registered address** (`addresses` table, `address_type_id=1`, PDPA-tracked home address)
   — read by `WithholdingCertificateDataService` (Form 50-Twi) and `EFilingExportService`
   (ภ.ง.ด.1ก's 3 address columns). Both reports currently ship with blank address fields until
   this seam is resolved. Two options for Phase 2: extend `PayEmployeeSnapshot` with an
   optional address block (simplest, but couples an address concept into "employee" that the
   original design kept separate), or add a narrow `IEmployeeAddressSource`. Needs a decision,
   not a default.
3. **`emp_overtime_requests` pending-approval check** (`PayrollPreflightService`'s checklist)
   — reads a DIFFERENT overtime table than `HrwOt`/`IOvertimeFeed` uses. This looks like a
   pre-existing inconsistency in HRM itself (two OT tables, one used for pay calculation, a
   newer one used only for this one preflight warning) rather than something this extraction
   should paper over with a guess — flagged for the original author to clarify which table is
   actually current before deciding whether it needs a seam at all.

## 7. Blazor page porting

Only `PayrollRunList.razor` was ported (plan asked for 3-4: `PayrollRunList`,
`PayrollRunCreate`, `PayrollRunDetail`, one admin config page). Reasons, stated plainly rather
than glossed over:

- Time available in this Phase 0 pass was consumed mostly by the Engine adaptation (§5) and
  writing this document, both of which the task treated as higher-value than a 4th proof page
  once the PATTERN was established with the first one.
- The pattern IS established and is mechanical to repeat: strip `[Authorize(Policy=...)]`
  (host-shell concern), strip `JsonLocalizationService`/`L.Translate(...)` calls (replace with
  literals or the host's own localization), replace any `.Hremployee` navigation reads with an
  injected `IEmployeeSource`, replace `IDbContextFactory<HRMContext>` with
  `IDbContextFactory<PayrollDbContext>`, replace any claim-based company resolution with a
  `[Parameter]` the host page supplies.
- **What finishing this needs, concretely, for Phase 1-2**: port `PayrollRunCreate.razor`
  (creates a `Pay_PayrollRun`, calls `PayDateService`/`PayScheduleResolver` — no employee
  reads, should be a quick clean port), `PayrollRunDetail.razor` (the big one — calls
  `PayrollCalculationService.CalculateAsync`, `PayrollWorkflowService`'s transitions,
  `BankFileExportService`/`GLExportService`/`PayslipGenerationService`, and displays
  `Pay_PayrollLineItem` rows per employee — needs `IEmployeeSource` for every name shown),
  and `PayScheduleAdmin.razor` (simplest admin CRUD page, zero Hremployee dependency, good
  "prove the CRUD-page pattern too" candidate).
- Per plan §7's own risk note, none of these — including the one ported here — will actually
  RENDER until wired into a host project (MudBlazor's services, `_Imports.razor`'s
  `@using`s, layout, and DI container all come from the host). Proving that end-to-end is
  explicitly Phase 1's job (the plan tries this first with the smaller Workflow package, 28
  pages, specifically so Payroll's 41 pages benefit from the lessons — see plan §7's risk
  list), not Phase 0's.

## 8. Standalone build results

| Project | Result |
|---|---|
| `Advance.Payroll.Core` | **Build succeeded, 0 warnings, 0 errors.** No `Microsoft.EntityFrameworkCore*` PackageReference exists in its `.csproj` at all — the "zero DB dependency" claim is a build fact, not a comment. |
| `Advance.Payroll.Domain` | **Build succeeded, 0 warnings, 0 errors.** Only PackageReference: `Microsoft.EntityFrameworkCore` (for the `[Index(...)]` attribute on 3 entities — no provider, no DbContext). |
| `Advance.Payroll.Contracts` | **Build succeeded, 0 warnings, 0 errors.** Zero PackageReferences. |
| `Advance.Payroll.Data` | **Build succeeded** (16 NU1903 advisory warnings, 0 errors — `System.Security.Cryptography.Xml` transitive vulnerability from `Microsoft.EntityFrameworkCore.SqlServer`/`Tools`; this is the SAME warning HRM.csproj's own restore already carries with the identical package versions, not something this extraction introduced). |
| `Advance.Payroll.Engine` | See the per-file table in §5 for what's wired vs. what has an open `TODO(seam)`. A `dotnet build` was kicked off for this project; if this document is being read before that background run's result lands in this session, re-run `dotnet build -p:UseAppHost=false` in `src/Advance.Payroll/Advance.Payroll.Engine/` to get the current line/column-level diagnostic list — the 3 open seams in §6 are believed to be the only remaining compile blockers based on a full manual read of every file, but a compiler run is the actual proof, not this sentence. |
| `Advance.Payroll.Reports` | Trivially builds (near-empty by design, see its README.md). |
| `Advance.Payroll.Blazor` | Not built standalone — a Razor Class Library with MudBlazor markup has no host to resolve services from; the `.csproj`'s own header comment explains why proving this compiles/renders needs a host project, out of scope here. |

## 9. Things too intricate to re-derive — needs the real author's review before cutover

Per the task brief's explicit instruction not to guess at simplifications:

- **YTD folding across prior-employer income** (`PayrollCalculationService.FoldYtd` /
  `FoldPriorEmployerIncome`, `Pay_EmployeePriorEmployerIncome`) — copied byte-for-byte, logic
  untouched, but the underlying business rule (a mid-year hire's prior employer's income/
  deduction/tax-withheld gets folded into THIS company's YTD accumulator for withholding
  projection purposes) has enough edge cases noted in the original comments (audit M-series
  references) that a reviewer who lived through those bugs should re-verify the ported
  version against `HRM.Tests/Integration/Payroll2025ScenarioTests.cs` before trusting it in a
  new deployment target.
- **Same-month SSO netting across reversal/adjustment runs**
  (`sameMonthSsoByEmp`/`sameMonthSsoBaseByEmp` in `PayrollCalculationService.CalculateAsync`)
  — the comments explain WHY (a half-month-pay company's second term must net against what
  the first term already withheld, and a reversal's negative row must cancel out the
  original so a later adjustment doesn't see "already withheld the full month" twice) but the
  arithmetic has 3-4 interacting terms (`priorMonthSsoBase`, `priorMonthSso`, `ssoMonthlyProjected`,
  `termsLeftThisMonth`) that were tuned against real bugs found in the 2025 scenario test —
  treat this block as load-bearing and re-run that full test suite (once it's moved per §1)
  after ANY change to it, not just at cutover.
- **One-off-income tax-by-difference** (`thisPeriodOneOffIncome` /
  `TaxBracketCalculator.CalculateBonusWithholding`'s "tax(base+bonus) − tax(base)" pattern) —
  logic is straightforward on its own, but its INTERACTION with the regular
  `CalculatePeriodWithholding` path (a one-off item paid inside a REGULAR run, vs. a whole
  separate BONUS run) is two different code paths converging on similar-looking numbers for
  different reasons — a reviewer should confirm both paths still agree on total tax for a
  synthetic case that has both a regular salary AND an ad-hoc taxable bonus in the same period
  before shipping this to a new company's first payroll.

## 10. DB triggers — SQL Server–specific, flagged not fixed

`trg_PayrollEmployee_Immutable` and `trg_PayrollLineItem_Immutable` (declared in
`PayrollDbContext.OnModelCreating` via `.ToTable(tb => tb.HasTrigger(...))`, actual SQL in
HRM's `Migrations/Manual/PayrollPhaseABAndPayElementCatalog...sql`) block UPDATE/DELETE on a
posted run's calculated rows at the DATABASE level. If Advance.Payroll (the Lite/SaaS product)
ever targets PostgreSQL instead of SQL Server (plan §6 decision 3 treats this as a later-phase
option), these triggers do not port — Postgres doesn't have T-SQL `AFTER UPDATE/DELETE` triggers
in the same form, and more importantly EF Core's SQL Server-specific `HasTrigger()` model
annotation (needed today only to suppress the OUTPUT-clause conflict, not to create the
trigger — the trigger itself lives in a hand-written migration SQL file, not in C#) has no
Postgres equivalent call. The fix, when needed, is an application-level guard: a
`SaveChanges`/`SavingChanges` interceptor on `PayrollDbContext` (mirroring
`Model/HRMContext.Audit.cs`'s existing interceptor pattern) that throws before letting any
`Modified`/`Deleted` entity through if it belongs to a `Pay_PayrollRun` with
`Status >= Posted`. **Not built now** — correctly flagged in the plan document already
(§2.1's "ยังขวาง" list); this section just pins down exactly which 2 tables/triggers and
exactly what the replacement mechanism should be.

## 11. Cutover checklist (sequenced, each step small enough to build+test after)

1. Create `Advance.Payroll` as a real solution-referenced project set inside `HRM.sln`
   (ProjectReference, per plan §6 decision 2 — not a separate repo yet). Copy this worktree's
   `src/Advance.Payroll/` in as the starting point; it already builds (Core/Domain/Contracts/Data
   confirmed clean — §8).
2. Close the 3 seams in §6 (or explicitly decide to defer each with a stub, as
   `PayrollAnomalyDetectionService`/`PayrollPreflightService` already do) — get
   `Advance.Payroll.Engine` to a full clean build.
3. Write `HRM.Payroll.EmployeeSourceAdapter : IEmployeeSource` (reads `Hremployee` — see §4)
   and the other 6 interface implementations (`IAttendanceFeed` -> `Att_DailyAttendance`,
   `IOvertimeFeed` -> `HrwOt`, `ILoanFeed` -> `Kptempreceive*`, `IAllowanceFeed` ->
   `Wel_BenefitType`/`Wel_Entitlement`/`Pos_PositionSlot` + the existing
   `WelfareEntitlementResolver.Pick` logic moved into this adapter, `IHolidayCalendarSource` ->
   `Lve_CompanySetting`/`Lve_CompanyHoliday`, `IWorkflowGateway` -> the existing
   `WorkflowEngineService.StartJobAsync` call HRM already makes). Each is a small, independently
   testable class — build and unit-test each one against a real HRM dev database before wiring
   any of them into `PayrollCalculationService`.
4. Point `HRM.csproj` at the new `Advance.Payroll.*` projects (ProjectReference) alongside the
   still-live `Services/Pay/**`/`Model/Pay_*.cs` — i.e. BOTH copies exist simultaneously,
   nothing deleted yet. Register `AddAdvancePayroll(...)` (§3) under a DIFFERENT DI key/feature
   flag than the existing registrations so both can coexist without colliding.
5. Point ONE low-risk page (e.g. a read-only report page, not `PayrollRunDetail`) at the new
   Engine services behind that feature flag; verify identical output against the old path for
   a real company's real data over at least 2-3 payroll periods.
6. Move `HRM.Tests/Pay/**` and `HRM.Tests/Integration/Payroll2025ScenarioTests.cs` into
   `Advance.Payroll`'s own test project, adapted to call the new Engine + fake
   implementations of the 7 interfaces (proves Payroll calculates correctly with ZERO live
   HRM database, per plan §4 Phase 2's stated goal) — keep the OLD tests passing against the
   OLD `Services/Pay/**` in parallel until step 8.
7. Repeat step 5 for every remaining page/endpoint one at a time (the §1 DELETE list is the
   checklist of what's left), each behind the same flag, each verified against real data
   before moving to the next.
8. Once every page/endpoint is flipped and stable for a real payroll cycle: flip the feature
   flag default, delete `Services/Pay/**`/`Model/Pay_*.cs`/`Components/Pages/Pay/**` per §1,
   remove the `HRMContext`/`Program.cs` lines per §2-3, delete the OLD test files.
9. Only after step 8 is stable: consider splitting `Advance.Payroll` into its own repo (plan
   §6 decision 2, "แยก repo ตอนเริ่มเฟส 3") — this cutover checklist stops at "lives cleanly
   inside `HRM.sln`", which is Phase 2's actual finish line per the plan document.

## 12. Single biggest risk (see final report to the task caller for the one-sentence version)

Everything in §9 taken together: this extraction is a faithful, line-for-line MECHANICAL port
of already-correct, already-bug-fixed calculation logic — the actual risk is not "did the
Core/Domain/Data/Contracts split introduce a bug" (those 4 projects build clean and are inert
data/interface shapes with no behavior of their own to get wrong), it's "does
`PayrollCalculationService`'s ~950 lines of interacting YTD/proration/SSO-netting/one-off-tax
logic, once it's calling through 7 new interface boundaries instead of 7 direct EF queries,
still produce IDENTICAL numbers for every one of the ~960 checks in
`Payroll2025ScenarioTests.cs`" — and that has NOT been verified yet in this Phase 0 pass
(the test suite itself wasn't run against the new Engine; doing so requires the §11 step-3
interface implementations to exist first, which is Phase 2 work). Treat every number this
new Engine produces as unverified until that specific test suite passes against it.
