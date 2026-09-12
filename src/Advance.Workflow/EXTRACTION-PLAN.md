# EXTRACTION-PLAN.md — Advance.Workflow, Phase 0

Written 12 ก.ย. 2569, against HRM commit `30a50ea` ("Workflow: retire the old
engine — one engine, one code path") + later. Companion to
`docs/Plan_Split_Payroll_Workflow_v1.1.md` (the CEO-level plan) — this file
is the concrete, file-and-line-level version of that plan's Phase 0/1 for
Advance.Workflow specifically.

**Status: Phase 0 only. Nothing in HRM has been touched.** Everything here
lives under `src/Advance.Workflow/` as a parallel copy, proven to compile
standalone. HRM still runs exactly as it did before this work started.

---

## 0. What was built (folder structure)

```
src/Advance.Workflow/
├── EXTRACTION-PLAN.md                        (this file)
├── Advance.Workflow.Contracts/               net10.0 class library — BUILDS CLEAN
│   ├── IWorkflowEngine.cs                    (+ JobAgeInfo, PoolInboxRow, LevelApproverPlan)
│   ├── IWorkflowDocumentHandler.cs           (+ WorkflowClosedEvent, WorkflowCloseOutcome)
│   ├── IOrgDirectorySource.cs                (+ OrgNode, WorkflowEmployee)
│   ├── IWorkflowUserDirectory.cs             (+ WorkflowUser)
│   └── IWorkflowNotifier.cs
├── Advance.Workflow.Domain/                  net10.0 class library — BUILDS CLEAN
│   └── Entities/  (27 files — full list in section 2)
├── Advance.Workflow.Org/                     net10.0 class library — BUILDS CLEAN
│   ├── Entities/wf_employee.cs, wf_org_type.cs
│   ├── WorkflowOrgDbContext.cs
│   └── README.md
├── Advance.Workflow.Engine/                  net10.0 class library — BUILDS CLEAN
│   ├── WorkflowDbContext.cs
│   ├── WorkflowService.cs                    (ported from Services/Workflow/WorkflowService.cs, 1642 lines)
│   ├── WorkflowEngineService.cs              (ported facade, implements IWorkflowEngine)
│   ├── WorkflowApproverResolver.cs           (ported from Model/wf_sub_workflow_master.Behavior.cs)
│   ├── WorkflowButtonService.cs              (ported, no seams needed)
│   └── WorkflowLockGuard.cs                  (ported, no seams needed)
└── Advance.Workflow.Blazor/                  Razor Class Library — BUILDS CLEAN (1 trivial warning, fixed)
    ├── _Imports.razor
    └── Pages/
        ├── WfMyInbox.razor                   (trimmed port)
        ├── WfMyRequests.razor                (near-verbatim port)
        ├── WorkflowDetail.razor              (heavily trimmed port)
        └── WfEmployeeAdmin.razor             (Workflow.Org admin page, ported off CrudScaffold)
```

**Project reference graph** (no cycles, matches the plan's layering):

```
Domain  <---  Contracts  <---  Engine
Domain  <---  Org
Contracts, Domain, Org, Engine  <---  Blazor
```

Notably **Engine has no reference to Org**. The plan's rule "engine อ่าน
ผู้อนุมัติจาก Workflow.Org เท่านั้น" is satisfied through dependency injection —
`Advance.Workflow.Engine` only knows `IOrgDirectorySource`/
`IWorkflowUserDirectory` (Contracts). Whoever composes the host (HRM today,
`Advance.Workflow.Web` later) decides whether those interfaces are
implemented by reading HRM's `Hremployee`/`com_organization`/`sc_user`
directly, or by an implementation inside `Advance.Workflow.Org` that reads
`wf_employee`/`wf_org_type`. The engine code is identical either way.

---

## 1. Build results

| Project | Result | Notes |
|---|---|---|
| Advance.Workflow.Domain | **Clean** — 0 warnings, 0 errors | |
| Advance.Workflow.Contracts | **Clean** — 0 warnings, 0 errors | |
| Advance.Workflow.Org | **Clean** — 0 warnings, 0 errors | |
| Advance.Workflow.Engine | **Clean** — 0 warnings, 0 errors | The big one — 1642-line WorkflowService.cs + 820-line WorkflowEngineService.cs ported and compiling standalone against WorkflowDbContext + Contracts, no ProjectReference to HRM at all. |
| Advance.Workflow.Blazor | **Clean** — 0 warnings, 0 errors (one `CS0414` unused-field warning found and fixed) | RCL, MudBlazor 8.14.0 + Components.Authorization referenced directly (host doesn't need to supply them, though a real host's own MudBlazor version must match or the RCL should stop pinning one). |

All five verified with `dotnet build -p:UseAppHost=false` run from inside each
project's own folder (never against `HRM.sln`, per the task's ground rules).

---

## 2. Files copied into Advance.Workflow.Domain/Entities (27 files)

Every one of these is a field-for-field, table-name-for-table-name copy from
HRM's `Model/`. Diff against the HRM original to verify nothing was silently
changed in shape — the only edits made were (a) namespace, (b) dropping the
three `sc_user` navigation properties noted in section 4.

```
job_loa.cs                    job_master.cs                 job_status.cs
job_subworkflow_master.cs     job_user_list.cs
wf_adhoc_user.cs              wf_budget.cs                  wf_button.cs
wf_button_master.cs           wf_checklist.cs               wf_condition.cs
wf_custom_role.cs             wf_custom_user.cs             wf_customer_approver.cs
wf_decision_status.cs         wf_emailTemplate.cs           wf_loa.cs
wf_loa_user.cs                wf_mas_reason.cs              wf_organize.cs
wf_role_authority.cs          wf_sub_workflow_master.cs     wf_workflow.cs
wf_workflow_in_workflow.cs
mas_reason.cs                 Wf_ApproverDelegation.cs
```

Two of these are easy to miss and were **missed on the first pass** of this
extraction because they don't match the `wf_*`/`job_*` lowercase-prefix
naming convention the task description (and CLAUDE.md) uses:

- **`mas_reason.cs`** (table `mas_reason`) — not `wf_`-prefixed, but it
  carries `workflowid`/`wlevel` columns and is only ever referenced by
  `job_user_list.mas_reason_id` (the "เหตุผลการพิจารณา" / `/wf/reasons` admin
  page). It is workflow-owned data, just an old naming inconsistency in the
  legacy schema.
- **`Wf_ApproverDelegation.cs`** (table `Wf_ApproverDelegation`, PascalCase)
  — a `wf_*.cs` glob that is case-sensitive (as most shell globs and some
  `find`/`grep` defaults are) skips this file entirely. It is core to the
  engine: `WorkflowService.ApplyDelegationAsync` reads it on every approver
  resolution. **Anyone re-running or extending this extraction should search
  case-insensitively** (`grep -il`) or grep by table content, not by
  filename casing.

Also found the same way: `Wf_WorkflowStateChangeRequest.cs` — this one is
**correctly excluded** from Domain. It's an HRM module table (the "deactivate
a workflow needs approval" feature), not an engine table — see section 6.

`wf_employee.cs` and `wf_org_type.cs` were copied instead into
**`Advance.Workflow.Org/Entities/`** (not Domain) — see that project's own
`README.md` for why.

### Entity edits made during the copy (not present in the HRM originals)

Three navigation properties were dropped because their target (`sc_user`) is
HRM's identity table, outside this project. The scalar FK column is
untouched in every case — only the EF `virtual` navigation property is gone,
replaced with a `TODO(seam)` comment pointing at
`IWorkflowUserDirectory`:

| File | Property dropped | FK column kept |
|---|---|---|
| `wf_adhoc_user.cs` | `public virtual sc_user user` | `userid` (long) |
| `wf_custom_user.cs` | `public virtual sc_user user` | `userid` (long) |
| `job_user_list.cs` | `public virtual sc_user? user` | `userid` (long?) |

**⚠️ Naming collision to watch for during any future search-and-copy of
"Loa" tables**: `Model/HRMContext.cs` also declares `DbSet<Loa> loas` (table
with `PK_LOA`) — this is an unrelated **leave-of-absence** table from the HR
module, not `job_loa`/`wf_loa` (workflow's **L**evel-**O**f-**A**uthority
approval-amount bands). Both abbreviate to "LOA" in comments; they are
different tables in different modules. This extraction did not touch the
leave-module `Loa` table, but a future contributor grepping for "loa"
case-insensitively will find both and must not conflate them.

---

## 3. Exact files in HRM to DELETE once cutover happens

**Not deleted yet — this is the target list for the real cutover, after
Advance.Workflow ships as a referenced package and HRM's own pages/services
are repointed at it (see the sequenced checklist, section 9).**

### 3.1 Services/Workflow/ — delete these 4:

```
D:\GitWorkspace\HRM\Services\Workflow\WorkflowEngineService.cs
D:\GitWorkspace\HRM\Services\Workflow\WorkflowService.cs
D:\GitWorkspace\HRM\Services\Workflow\WorkflowButtonService.cs
D:\GitWorkspace\HRM\Services\Workflow\WorkflowLockGuard.cs
```

**Do NOT delete** (stay in HRM, edited not removed — see section 6/7):

```
D:\GitWorkspace\HRM\Services\Workflow\WorkflowDocumentWriteback.cs   (dispatcher + 17 handlers — edit, don't delete)
D:\GitWorkspace\HRM\Services\Workflow\WorkflowStateChangeSeeder.cs   (HRM module, calls the engine — not part of it)
D:\GitWorkspace\HRM\Services\Workflow\WorkflowStateChangeService.cs  (ditto)
```

### 3.2 Model/ — delete these 24:

```
Model\wf_adhoc_user.cs            Model\wf_budget.cs                Model\wf_button.cs
Model\wf_button_master.cs         Model\wf_checklist.cs             Model\wf_condition.cs
Model\wf_custom_role.cs           Model\wf_custom_user.cs           Model\wf_customer_approver.cs
Model\wf_decision_status.cs       Model\wf_emailTemplate.cs         Model\wf_loa.cs
Model\wf_loa_user.cs              Model\wf_mas_reason.cs            Model\wf_organize.cs
Model\wf_role_authority.cs        Model\wf_sub_workflow_master.cs   Model\wf_sub_workflow_master.Behavior.cs
Model\wf_workflow.cs              Model\wf_workflow_in_workflow.cs  Model\Wf_ApproverDelegation.cs
Model\job_loa.cs                  Model\job_master.cs               Model\job_status.cs
Model\job_subworkflow_master.cs   Model\job_user_list.cs
```

`Model\mas_reason.cs` is **not** in this list — confirm at cutover time
whether anything in HRM besides the workflow engine reads it (a repo-wide
grep at Phase 0 time found none), and if truly workflow-only, delete it too.

**Do NOT delete**: `Model\Wf_WorkflowStateChangeRequest.cs` (HRM module
table, stays), `Model\Loa.cs` (unrelated leave-of-absence table, stays — see
the naming-collision warning above).

`Model\wf_employee.cs` and `Model\wf_org_type.cs`: delete from HRM **only**
once `IOrgDirectorySource`/sync is actually wired and HRM no longer needs its
own copy of these DbSets — see section 5's note on this being the
riskiest/least-defined part of the whole cutover.

### 3.3 Components/Pages/Wf/ — all 28, once ported to Advance.Workflow.Blazor:

```
WfAdhocUserAdmin.razor      WfButtonAdmin.razor         WfCanvasDesigner.razor
WfCanvasDetail.razor        WfCanvasGallery.razor       WfCustomRoleAdmin.razor
WfCustomUserAdmin.razor     WfCycleTime.razor           WfDelegationAdmin.razor
WfEmployeeAdmin.razor       WfHealthCheck.razor         WfJobsAdmin.razor
WfLoaAdmin.razor            WfLoaUserAdmin.razor        WfMyInbox.razor
WfMyInboxDetail.razor       WfMyRequests.razor          WfOrgTypeAdmin.razor
WfPoolInbox.razor           WfReasonAdmin.razor         WfReassignApprover.razor
WfStateChangeRequests.razor WfSubWorkflowMasterAdmin.razor  WfTeamJobs.razor
WfVacantApprovals.razor     WfWorkflowAdmin.razor       WorkflowDesigner.razor
WorkflowDetail.razor
```

**4 of these (`WfMyInbox`, `WfMyRequests`, `WorkflowDetail`, `WfEmployeeAdmin`)
already have a Phase 0 port** in `Advance.Workflow.Blazor/Pages/` — trimmed,
not byte-identical (see section 8). **`WfStateChangeRequests.razor`** should
probably stay in HRM alongside `WorkflowStateChangeService.cs` (section 3.1)
rather than move — it's UI for an HRM module that happens to use the engine,
not engine UI itself; confirm at cutover time.

### 3.4 Components/Shared/ — 5 of 6, once ported:

```
WfActionButtons.razor        Components/Shared/WorkflowActionPanel.razor
Components/Shared/WorkflowHistory.razor
Components/Shared/WorkflowStepper.razor
Components/Shared/WorkflowNotice.razor
```

**Do NOT delete without a design decision first**:
`Components/Shared/WorkflowDocumentSlot.razor` — this is the `<DynamicComponent>`
plug point that embeds the ORIGINATING module's own form inline on the
workflow detail page (leave request, expense claim, etc.). It is engine-
adjacent (reads `wf_sub_workflow_master.controller`/`action`) but its whole
purpose is to host components that belong to HRM modules — a genuinely
ambiguous case flagged in section 8, not resolved here.

---

## 4. Exact edits needed in Model/HRMContext.cs

### 4.1 DbSet declarations to remove (23 lines)

```csharp
public virtual DbSet<job_loa> job_loas { get; set; }                          // line 142
public virtual DbSet<job_master> job_masters { get; set; }                    // line 144
public virtual DbSet<job_status> job_statuses { get; set; }                   // line 146
public virtual DbSet<job_subworkflow_master> job_subworkflow_masters { get; set; }  // line 148
public virtual DbSet<job_user_list> job_user_lists { get; set; }              // line 150
public virtual DbSet<mas_reason> mas_reasons { get; set; }                    // line 172 (confirm no other reader first)
public virtual DbSet<wf_adhoc_user> wf_adhoc_users { get; set; }              // line 309
public virtual DbSet<wf_budget> wf_budgets { get; set; }                      // line 311
public virtual DbSet<wf_button> wf_buttons { get; set; }                      // line 313
public virtual DbSet<wf_button_master> wf_button_masters { get; set; }        // line 315
public virtual DbSet<wf_checklist> wf_checklists { get; set; }                // line 317
public virtual DbSet<wf_condition> wf_conditions { get; set; }                // line 319
public virtual DbSet<wf_custom_role> wf_custom_roles { get; set; }            // line 321
public virtual DbSet<wf_custom_user> wf_custom_users { get; set; }            // line 323
public virtual DbSet<wf_customer_approver> wf_customer_approvers { get; set; } // line 325
public virtual DbSet<wf_decision_status> wf_decision_statuses { get; set; }   // line 327
public virtual DbSet<wf_emailTemplate> wf_emailTemplates { get; set; }        // line 329
public virtual DbSet<wf_employee> wf_employees { get; set; }                  // line 331 — see 5's caveat
public virtual DbSet<wf_loa> wf_loas { get; set; }                            // line 333
public virtual DbSet<wf_loa_user> wf_loa_users { get; set; }                  // line 335
public virtual DbSet<wf_role_authority> wf_role_authorities { get; set; }     // line 337
public virtual DbSet<wf_mas_reason> wf_mas_reasons { get; set; }              // line 339
public virtual DbSet<wf_org_type> wf_org_types { get; set; }                  // line 341 — see 5's caveat
public virtual DbSet<wf_organize> wf_organizes { get; set; }                  // line 343
public virtual DbSet<wf_sub_workflow_master> wf_sub_workflow_masters { get; set; }  // line 345
public virtual DbSet<wf_workflow> wf_workflows { get; set; }                  // line 347
public virtual DbSet<wf_workflow_in_workflow> wf_workflow_in_workflows { get; set; }  // line 352
public virtual DbSet<Wf_ApproverDelegation> Wf_ApproverDelegations { get; set; }  // line 350
```

**Do NOT remove**: `DbSet<Wf_WorkflowStateChangeRequest> Wf_WorkflowStateChangeRequests`
(line 468) and `DbSet<Loa> loas` (line 152, unrelated table — see the naming
warning in section 2).

### 4.2 OnModelCreating configuration blocks to remove

Every `modelBuilder.Entity<T>(entity => { ... })` block for the types in 4.1.
Quoted here exactly since two of them (`job_master`, `job_user_list`) have a
line that must NOT simply be deleted wholesale — the FK to `sc_user` needs to
go, but the rest of the block (FK to `job_master`, `mas_reason`,
`wf_sub_workflow_master`) stays as HRM's own concern if HRM ever re-adds a
local shadow of these tables; in practice the whole block goes since the
DbSet itself is gone:

```csharp
modelBuilder.Entity<job_master>(entity => { ... });                 // lines 865-874
modelBuilder.Entity<job_status>(entity => { ... });                 // lines 876-879
modelBuilder.Entity<job_subworkflow_master>(entity => { ... });     // lines 881-884
modelBuilder.Entity<job_user_list>(entity => { ... });              // lines 886-899
modelBuilder.Entity<wf_adhoc_user>(entity => { ... });              // lines 1359-1366
modelBuilder.Entity<wf_budget>(entity => { ... });                  // lines 1368-1373
modelBuilder.Entity<wf_button>(entity => { ... });                  // lines 1375-1382
modelBuilder.Entity<wf_checklist>(entity => { ... });               // lines 1384-1389
modelBuilder.Entity<wf_condition>(entity => { ... });               // lines 1391-1397
modelBuilder.Entity<wf_custom_role>(entity => { ... });             // lines 1399-1410
modelBuilder.Entity<wf_custom_user>(entity => { ... });             // lines 1412-1427
modelBuilder.Entity<wf_customer_approver>(entity => { ... });       // lines 1429-1432
modelBuilder.Entity<wf_decision_status>(entity => { ... });         // lines 1434-1439
modelBuilder.Entity<wf_employee>(entity => { ... });                // lines 1441-1444 — see 5's caveat
modelBuilder.Entity<wf_loa>(entity => { ... });                     // lines 1446-1449
modelBuilder.Entity<wf_loa_user>(entity => { ... });                // lines 1451-1455
modelBuilder.Entity<wf_org_type>(entity => { ... });                // lines 1457-1462 — see 5's caveat
modelBuilder.Entity<wf_organize>(entity => { ... });                // lines 1464-1469
modelBuilder.Entity<wf_sub_workflow_master>(entity => { ... });     // lines 1471-1477
modelBuilder.Entity<wf_workflow>(entity => { ... });                // lines 1479 onward
```

(Line numbers as of this Phase 0 snapshot — re-verify against HEAD at cutover
time since HRMContext.cs churns with every new module's migration.)

---

## 5. Exact edits needed in Program.cs

### 5.1 Remove (lines 485-490 as of this snapshot):

```csharp
builder.Services.AddScoped<HRM.Services.Workflow.WorkflowEngineService>();
builder.Services.AddScoped<HRM.Services.Workflow.WorkflowService>();
builder.Services.AddScoped<HRM.Services.Workflow.WorkflowStateChangeService>();   // KEEP — HRM module, not engine
builder.Services.AddScoped<HRM.Services.Workflow.WorkflowButtonService>();
// ปิดงานแล้วเขียนผลกลับไปที่เอกสารของโมดูล (reftable/refid) ทันที — CEO, 11 ก.ย. 2569
HRM.Services.Workflow.WorkflowDocumentHandlers.AddWorkflowDocumentHandlers(builder.Services);  // KEEP, edited
```

Only the `WorkflowEngineService`/`WorkflowService`/`WorkflowButtonService`
registrations are removed. `WorkflowStateChangeService` and
`AddWorkflowDocumentHandlers` stay (see section 6/7) but need their `using`
target adjusted since `IWorkflowDocumentHandler` moves to
`Advance.Workflow.Contracts`.

### 5.2 Add: a single `AddAdvanceWorkflow(...)` extension

Design (goes in a new `Extensions/AdvanceWorkflowServiceCollectionExtensions.cs`
in HRM, or directly in `Advance.Workflow.Engine` as a self-registering
extension the host calls — the latter is more consistent with how a real
NuGet package would ship it):

```csharp
namespace Advance.Workflow.Engine;

public static class AdvanceWorkflowServiceCollectionExtensions
{
    public static IServiceCollection AddAdvanceWorkflow(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddPooledDbContextFactory<WorkflowDbContext>(configureDb);
        services.AddScoped<WorkflowService>();
        services.AddScoped<WorkflowEngineService>();
        services.AddScoped<IWorkflowEngine>(sp => sp.GetRequiredService<WorkflowEngineService>());
        services.AddScoped<WorkflowButtonService>();
        // IWorkflowUserDirectory / IOrgDirectorySource / IWorkflowNotifier are
        // deliberately NOT registered here — the host (HRM) must register its
        // own implementations of these three before calling AddAdvanceWorkflow,
        // or startup fails fast (a missing-registration exception on first
        // resolve) rather than silently running with no approvers ever found.
        return services;
    }
}
```

HRM's `Program.cs` would then add, in place of the three removed lines:

```csharp
builder.Services.AddScoped<IWorkflowUserDirectory, HrmWorkflowUserDirectory>();   // new adapter, reads sc_user
builder.Services.AddScoped<IOrgDirectorySource, HrmOrgDirectorySource>();        // new adapter, reads Hremployee/com_organization
builder.Services.AddScoped<IWorkflowNotifier, HrmWorkflowNotifier>();            // new adapter, wraps EmailSender
builder.Services.AddAdvanceWorkflow(opt => opt.UseSqlServer(connectionString));
```

The three `Hrm*` adapter classes are new code this plan does not write (Phase
0 scope is the package side, not the HRM-side adapters) — they are thin
(a handful of methods each, reading tables HRM already has) but are real work
for Phase 1, not zero-cost glue.

---

## 6. Every call site outside Services/Workflow that calls the engine directly

Grepped across `Services/**/*.cs` for `WorkflowEngineService` (type
reference or member call) and `StartJobAsync`/`CreateDraftAsync`/
`DeleteDraftAsync`. **31 files** reference the engine; every one of them
takes `WorkflowEngineService engine` as a constructor/method parameter today
(concrete class, no interface) and calls `engine.StartJobAsync(...)` to kick
off a job, plus reads the `WorkflowEngineService.Status*`/`ClosedBy*` public
`const string` fields for status comparisons.

| File | Lines (StartJobAsync / status consts) | Becomes |
|---|---|---|
| `Services/Att/AttendanceCorrectionService.cs` | 14, 49, 64, 81, 86 | `IWorkflowEngine` call + status consts |
| `Services/Att/TimesheetService.cs` | 11, 104, 130 | ditto |
| `Services/Att/AttendanceAggregationService.cs` | 131 | status const only |
| `Services/Exp/ExpenseClaimService.cs` | 112, 130, 189 | ditto |
| `Services/Ess/UniformRequestService.cs` | 16, 112 | ditto |
| `Services/Engagement/EngagementService.cs` | 16, 93, 125 | ditto |
| `Services/Hr/DisciplinaryActionService.cs` | 66, 78 | ditto |
| `Services/Hr/RewardCaseService.cs` | 66, 78 | ditto |
| `Services/Hr/SeparationRequestService.cs` | 14, 54, 79, 208, 235 | ditto (2 workflows: request + date-change) |
| `Services/Idp/IdpPlanService.cs` | 12, 102, 128 | ditto |
| `Services/Km/KmArticleService.cs` | 11, 13, 73, 101 | ditto |
| `Services/Leave/LeaveAnalyticsService.cs` | 39-40 | status consts only (reporting) |
| `Services/Leave/BlockLeaveComplianceService.cs` | 61 | status const only |
| `Services/Leave/LeaveRequestService.cs` | 15, 24, 159, 203, 250 | ditto |
| `Services/Lms/LmsEnrollmentService.cs` | 12, 60, 84 | ditto |
| `Services/Org/OrgChangeRequestService.cs` | 12, 69, 105 | ditto |
| `Services/Perf/PerfApprovalService.cs` | 15, 35, 60 | ditto |
| `Services/Perf/PerfAssignmentResolverService.cs` | 10 (comment only) | no change needed |
| `Services/Perf/PerfImprovementPlanService.cs` | 14, 130, 156 | ditto |
| `Services/Rec/RecRequisitionService.cs` | 15, 49, 74 | ditto |
| `Services/Rec/RecOfferService.cs` | 14, 48, 78, 107, 167, 192, 218 | ditto (2 workflows: offer + hire) |
| `Services/Succession/SuccessionService.cs` | 15, 188, 275, 299, 364 | ditto |
| `Services/Welfare/WelfareClaimService.cs` | 14, 53, 63 | ditto |
| `Services/Welfare/WelfareWorkflowSeeder.cs` | 7 (comment only) | no change needed |
| `Services/Welfare/WelfareEntitlementResolver.cs` | 14 (comment only) | no change needed |
| `Services/Pay/ProvidentFundExitCaseService.cs` | 21, 23, 62, 80 | ditto |
| `Services/Pay/ProvidentFundRateChangeRequestService.cs` | 20, 23, 137, 155 | ditto |
| `Services/Workflow/WorkflowStateChangeService.cs` | 15, 62, 92 | **stays in HRM** — this module itself sits on top of the engine; only its `engine` dependency type changes from `WorkflowEngineService` to `IWorkflowEngine` |
| `Services/Dev/DemoCompanySeeder.cs` | 34 (comment only) | no change needed |

**⚠️ The single biggest gap this survey found**: every file above that
compares against `WorkflowEngineService.StatusCompleted`/`.StatusPending`/
`.StatusCancelled`/`.ClosedByDecline` etc. is referencing **`public const
string` fields on the concrete class**, not a DI-injected value.
`IWorkflowEngine` (Contracts) does not — and, as a plain interface, largely
*cannot* — re-expose `const` fields the same way. Three options, none
implemented yet, need a decision before cutover:

1. Add a static `Advance.Workflow.Contracts.WorkflowStatus` class with the
   same constant strings, and repoint all ~25 call sites'
   `WorkflowEngineService.StatusXxx` to `WorkflowStatus.Completed` etc.
   (mechanical, but touches ~25 files).
2. Leave these call sites depending on the concrete
   `Advance.Workflow.Engine.WorkflowEngineService` assembly for constants
   only (still works — Engine is a public package HRM references either
   way — but muddies the "modules depend only on the interface" story).
3. Have callers compare against the literal strings ("COMPLETED",
   "CANCELLED", ...) directly, losing the compile-time safety of a shared
   constant.

This plan does not pick one — it is exactly the kind of small-but-pervasive
decision flagged for the real author in section 8.

---

## 7. The 17 document-writeback handlers

**Confirmed**: the plan's claim that these stay in HRM is correct. They
belong to the document-owning modules, not to the engine — the engine only
needs to know `IWorkflowDocumentHandler` (now in
`Advance.Workflow.Contracts`) exists and to raise a `WorkflowClosedEvent`
when a job closes; it has never known what any handler actually does.

`Services/Workflow/WorkflowDocumentWriteback.cs` stays, with two edits:
- `using` the `IWorkflowDocumentHandler`/`WorkflowClosedEvent`/
  `WorkflowCloseOutcome` types from `Advance.Workflow.Contracts` instead of
  defining them locally (they're removed from this file, already ported).
- `WorkflowDocumentWriteback.OutcomeOf` references
  `WorkflowEngineService.ClosedByDecline`/`ClosedByCancel` — repoint to
  wherever those constants land per section 6's open decision.

The 17 `Map(...)` registrations in `WorkflowDocumentHandlers.AddWorkflowDocumentHandlers`
(same file) are unchanged — full list, confirmed against the live source:

```
Att_TimesheetSubmission            -> Services.Att.TimesheetService.SyncStatusFromJobAsync
Att_CorrectionRequest              -> Services.Att.AttendanceCorrectionService.SyncAsync
Idp_Plan                           -> Services.Idp.IdpPlanService.SyncStatusFromJobAsync
Km_Article                         -> Services.Km.KmArticleService.SyncStatusFromJobAsync
Lms_Enrollment                     -> Services.Lms.LmsEnrollmentService.SyncStatusFromJobAsync
Perf_EvaluationInstance            -> Services.Perf.PerfApprovalService.SyncStatusFromJobAsync
Perf_ImprovementPlan               -> Services.Perf.PerfImprovementPlanService.SyncStatusFromJobAsync
Succ_SuccessorNomination           -> Services.Succession.SuccessionService.SyncStatusFromJobAsync
Rec_Offer                          -> Services.Rec.RecOfferService.SyncStatusFromJobAsync
Rec_Requisition                    -> Services.Rec.RecRequisitionService.SyncStatusFromJobAsync
Pay_ProvidentFundExitCase          -> Services.Pay.ProvidentFundExitCaseService.SyncStatusFromJobAsync
Pay_ProvidentFundRateChangeRequest -> Services.Pay.ProvidentFundRateChangeRequestService.SyncStatusFromJobAsync
Wf_WorkflowStateChangeRequest      -> Services.Workflow.WorkflowStateChangeService.ApplyApprovedAsync
Org_OrganizationChangeRequest      -> Services.Org.OrgChangeRequestService.ApplyDueChangesAsync
Hr_SeparationRequest               -> Services.Hr.SeparationRequestService.SyncStatusFromJobAsync (via Hremployee lookup)
Hr_SeparationDateChange            -> Services.Hr.SeparationRequestService.SyncDateChangeStatusFromJobAsync (ditto)
Eng_RedeemRequest                  -> Services.Engagement.EngagementService.SyncRedeemsAsync (via CompanyId lookup)
```

That's the complete count — 17, matching the plan. Note `Wf_WorkflowStateChangeRequest`
is itself a handler target even though its name looks `wf_`-prefixed — it's
an HRM module (workflow-*about*-workflows), not engine data, confirming the
section 3 exclusion.

---

## 8. Menu/route entries — what happens to `/wf/*`

`Services/Security/ScMenuNavCatalog.cs` seeds `sc_menu`/route rows keyed by
URL path (e.g. `/wf/my-inbox`, `/wf/workflows`), independent of which
assembly's `@page` directive actually serves that route. **This means: when
a page's `.razor` file moves from `HRM.csproj` into the
`Advance.Workflow.Blazor` RCL, its `sc_menu` row and menu entry need
**zero** changes** — ASP.NET Core resolves `@page` routes across all loaded
assemblies (the host app + any referenced RCL) identically, so
`/wf/my-inbox` keeps working the moment HRM references the RCL, whether the
`.razor` file physically lives in `HRM.csproj` or in
`Advance.Workflow.Blazor.csproj`.

What DOES need attention at cutover:
- `Services/Security/ProgramRoleService.SeedAsync` (AD.CRUDManage) scans
  `@page` routes to seed `sc_program_role` — this scan is almost certainly
  assembly-reflection-based against the currently loaded app domain, which
  already includes referenced RCLs' routes automatically in ASP.NET Core.
  **Verify this assumption** against the real `ProgramRoleService` source at
  cutover time — if the scan is scoped to `typeof(Program).Assembly` only
  (a plausible simpler implementation), it would silently stop seeding
  rights for the 28 `/wf/*` routes once they move to the RCL, and every
  role would fail-closed against them. This is flagged, not verified, here.
- The menu ROWS themselves (`sc_menu`, `sc_role_menu`) are HRM's own
  database rows (`ScMenuNavCatalog.cs` lives in HRM, not in
  Advance.Workflow) — they stay in HRM regardless of where the `.razor` file
  lives, since menu/rights are a host-app concern, not a package concern
  (matches the plan's "shell owns menu+rights" architecture, section 3.2).

---

## 9. Sequenced cutover checklist

Each step is sized to build+test independently before moving to the next.

1. **Decide the status-constants question** (section 6's flagged gap) —
   blocks nothing else, but every later step that touches a calling module
   needs the answer.
2. **Write the 3 HRM-side adapters**: `HrmWorkflowUserDirectory`
   (`IWorkflowUserDirectory` reading `sc_user`/`sc_user_roles`),
   `HrmOrgDirectorySource` (`IOrgDirectorySource` reading
   `Hremployee`/`com_organization`), `HrmWorkflowNotifier`
   (`IWorkflowNotifier` wrapping `EmailSender`). Unit-test each against a
   throwaway in-memory/SQLite HRM-shaped DB. No HRM production code touched
   yet.
3. **Add `Advance.Workflow.*` as ProjectReferences to `HRM.csproj`**
   (same solution, not yet a separate repo — matches the plan's phase 1
   instruction "อยู่ใน HRM.sln เดิม"). Register `AddAdvanceWorkflow(...)` +
   the 3 adapters in `Program.cs` **alongside** (not instead of) the
   existing `WorkflowEngineService`/`WorkflowService` registrations — both
   engines coexist temporarily, nothing in HRM points at the new one yet.
   Build must stay green.
4. **Point ONE low-traffic module at the new engine** as a canary — e.g.
   `Km_Article` (single-workflow, low volume, already isolated). Change its
   constructor from `WorkflowEngineService` to `IWorkflowEngine`. Run its
   existing tests + a manual smoke test (submit → approve → verify
   write-back). This is the first real proof the seam substitutions
   (org-chain via `IOrgDirectorySource`, users via `IWorkflowUserDirectory`)
   produce the SAME approver a real workflow with real org-chart config
   would have gotten from the old direct `Hremployee`/`com_organization`
   reads.
5. **Run the full 2568 workflow test suite equivalent for Workflow** — HRM
   doesn't have one yet the way Payroll has
   `HRM.Tests/Integration/Payroll2025ScenarioTests.cs`; write one before
   migrating the other 30 modules, covering at minimum: vertical org-chain
   resolution at 1/2/3 hops, LOA amount-band branching, AND-condition
   partial approval, OR-condition first-wins, pool claim/release,
   delegation substitution, reject-bounce-back. This is the safety net the
   payroll split leaned on; workflow needs its own before wide rollout.
6. **Migrate the remaining ~30 call sites** (section 6's table) module by
   module, each as its own small PR/commit, build+smoke-test after each.
7. **Migrate the 28 Blazor pages** to `Advance.Workflow.Blazor`, 3-5 at a
   time, verifying `/wf/*` routes still resolve and `AD.CRUDManage` rights
   still seed correctly (section 8's flagged risk) after each batch.
8. **Decide `WorkflowDocumentSlot.razor`'s fate** (section 3.4) before this
   step blocks the last few pages.
9. **Remove the old `WorkflowEngineService`/`WorkflowService`/
   `WorkflowButtonService`/`WorkflowLockGuard` registrations and files**
   (section 3.1/5.1) — only once step 6 confirms zero remaining callers.
10. **Remove the DbSets/OnModelCreating blocks from HRMContext.cs**
    (section 4) and the entity files from `Model/` (section 3.2) — a real EF
    migration is NOT needed for this (the physical tables are untouched,
    only the C# mapping goes away), but double-check nothing else in HRM
    (a report, an export, an ad-hoc query) still reads these tables through
    `HRMContext` before removing.
11. **Decide Workflow.Org's real activation** (section 5's biggest open
    question — see section 10) — this can happen in parallel with steps
    4-10 since it doesn't block HRM's own engine cutover, only the
    standalone-sale use case.

---

## 10. Explicit "needs the real workflow author's judgment" flags

Per the task's own instruction: these are called out rather than papered
over, because getting them wrong silently breaks approval routing in a way
that's hard to detect (a job routes to the WRONG person, not an obvious
crash).

1. **`wf_org_type` has no approver column.** The live schema
   (`Model/wf_org_type.cs`) is `orgcode, orgcodefull, name, remark,
   istoplevel, abbname, lang, levelno, codetype, upperorg, id` — there is
   nowhere to store "who approves for this org unit," which is the entire
   point of `com_organization.approver_empid` in HRM today. Any standalone
   Advance.Workflow deployment relying on `wf_org_type` for vertical
   approval needs either a new column on this table or a separate
   assignment table, **and** a decision on whether that's a breaking schema
   change to a table the plan explicitly says is "already correct legacy
   schema, don't redesign." This wasn't discovered until actually trying to
   port `ResolveOrgChainAsync` against it in Phase 0 — the CEO's plan
   document, written before this deep a look, doesn't mention it.
2. **The Mix Approval vertical pre-check hop-walker, the self-terminating
   vertical-chain mechanism, and the fixed-count org-chart climb**
   (`VerticalPrecheckMarker`/`VerticalChainMarker`/`EmpLevelClimbMarker` —
   see the giant comment block at the top of HRM's
   `WorkflowEngineService.cs`) are referenced by name in that file's header
   but their actual implementing methods
   (`AssignVerticalChainHopAsync`/`AssignEmpLevelClimbAsync`) are NOT in the
   ~820-line file as it exists today — meaning either they were already
   folded into `WorkflowService.cs`'s `SupervisorAtHopAsync`/hop-counting
   logic (which this Phase 0 port DID carry over, see
   `WorkflowApproverResolver.SupervisorAtHopAsync`), or they're vestigial
   comments describing a mechanism that moved/was removed and the comment
   never got updated. **This needs the real author to confirm which**, since
   Phase 0 ported the hop-counting it could find and cannot verify there
   isn't a second, undiscovered mechanism with the same marker names doing
   something else.
3. **`isApproverSameCostCenter`** was left as a stub returning zero
   approvers (`WorkflowApproverResolver.IsApproverSameCostCenterAsync`) —
   the original queries `com_organization` by `CostCenterCode` directly, and
   `IOrgDirectorySource` doesn't have a "find org by cost center" lookup.
   Low-risk if this flag is rarely used in real configs (not verified either
   way in Phase 0), but silently returns "nobody" instead of erroring, which
   is exactly the "routes to nobody" failure mode this section exists to
   flag.
4. **Role-name display in `GetApproverPlanAsync`** falls back to `"role
   #<id>"` instead of a real name — `IWorkflowUserDirectory` has role
   *membership* lookups but not a role *name* lookup (`sc_role.name`). Purely
   cosmetic (doesn't affect who actually gets approver rights), but worth a
   one-line interface addition before this page ships for real.
5. **`GetRouteAsync`'s Hremployee-id enrichment** (a `/employee/{id}` deep
   link on each approver name in the route view) was dropped entirely
   rather than sort-of-ported — this is arguably a UI/HRM concern that
   leaked into the engine in the original design, and the real author should
   decide whether it belongs back in the engine (needs a "get employee's
   internal id from EmpNo" method on `IOrgDirectorySource`) or should move
   to the Blazor layer's own concern when rendering the route.
6. **`ProgramRoleService`'s route-scanning assembly scope** (section 8) —
   flagged, not verified; could silently break AD.CRUDManage for every
   `/wf/*` route the moment pages move to the RCL.
7. **Audit logging is completely dropped in the Phase 0 port**
   (`WorkflowService.cs`'s own file-header TODO) — every workflow action
   (`Decline`/`Cancel`/`AutoApprove`/`Approve`/`Reject`/`Create`) currently
   writes to Serilog only, not to any persisted audit trail. HRM's
   `IAuditLogger` wrote every one of these to `AuditLog` for the
   พ.ร.บ.คอมพิวเตอร์ ≥90-day retention requirement (see CLAUDE.md's own audit
   section) — **this is a compliance blocker for real deployment**, not a
   nice-to-have, and needs Advance.Platform's audit package (per the split
   plan's level 0-1) built and wired in before any real cutover, standalone
   or embedded.

---

## 11. What Phase 0 deliberately did NOT attempt

- Byte-identical behavior to HRM's live engine (explicitly out of scope per
  the task brief).
- Full port of all 28 Blazor pages (4 representative ones only).
- A real sync job for `IOrgDirectorySource` (the interface exists; nothing
  implements or calls it yet).
- Any change whatsoever to HRM's actual `Services/`, `Model/`,
  `Components/`, `Program.cs`, `.csproj`/`.sln` files — verified via
  `git status` before commit; only new files under `src/Advance.Workflow/`
  and this document exist in the diff.
