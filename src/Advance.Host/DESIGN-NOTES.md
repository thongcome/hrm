# Advance.Host — Design Notes

Phase 0 prep for `Plan_Split_Payroll_Workflow_v1.1.md` section 3.2 ("สิ่งที่ผลิตภัณฑ์เดี่ยวทั้งสองต้องมีเหมือนกัน").
This is a scaffold, not a finished product — see "Effort estimate" at the bottom for what's left.

## 1. TenantId vs CompanyId — the decision

**TenantId sits ABOVE CompanyId. A Tenant can own multiple Companies. CompanyId is unchanged.**

Concretely:

- `Tenant` (this project, `host_tenant` table) = one SaaS subscriber/customer account of Advance.Payroll or
  Advance.Workflow sold standalone.
- Every table that is genuinely new to the standalone products (`pay_employee`, the Workflow.Org tables, etc. —
  see the plan's section 3.1 ownership table) gets an added `TenantId` column, exactly as the plan already says for
  `Pay_*`: *"เพิ่ม TenantId (SaaS) — HumanOk ใส่ค่าเดียว"*.
- The existing string `CompanyId`/`companyid` convention (HRM's CLAUDE.md: "Company scoping is a string, not a
  numeric FK — deliberately") is **not touched, not renamed, not merged into TenantId**. A company inside a tenant's
  subscription still has its own `CompanyId` exactly the way HRM's 81 company-scoped tables already work today.

Why this shape and not "TenantId == CompanyId" or "replace CompanyId with TenantId":

1. **The plan itself already assumes it.** Section 5's scope for Advance.Payroll Lite lists *"บริษัทหลายแห่งในระบบเดียว
   (tenant)"* as one bullet inside a single product scope — i.e. one subscriber (tenant) can run payroll for more
   than one legal entity (company) from one login. If TenantId and CompanyId were the same axis, that bullet would
   be meaningless — you'd need a second Tenant per company, which defeats "ระบบเดียว".
2. **It costs nothing to keep both.** CompanyId columns are already `nvarchar(50)` everywhere (the 8 ก.ย. 2569
   widening) and unindexed in most tables, so adding a sibling `TenantId nvarchar(50)` column is a plain, additive
   `ALTER TABLE` with no migration risk to existing data — the same low-risk shape as the EmpNo widening the CEO
   already approved.
3. **It matches how HumanOk itself will use the host later.** HRM stays single-tenant (see section 4 below), but if
   it ever *did* sit behind Advance.Host, `com_company` already models "one legal entity, one `code`" while
   `com_organization`/`sc_user.company_id` model a slightly different bigint-keyed axis (per HRM's CLAUDE.md
   "Company scoping" section) — HRM already lives with two different scoping ideas coexisting without collapsing
   them into one. Tenant-above-Company is the same kind of coexistence, one level higher.
4. **Rejecting the alternative:** collapsing Tenant into CompanyId would mean a customer with 3 legal entities needs
   3 separate SaaS subscriptions/logins to run one payroll department — that's a worse product than what HumanOk
   already gives HR staff today (one login, `payroll_company` claim scoped per company, same user sees multiple
   companies they're granted). The standalone product should not regress on this.

**Where the line is deliberately fuzzy for now:** a *single-company* on-premise install of Advance.Payroll or
Advance.Workflow (the plan's other stated deployment mode, "ติดตั้งเดี่ยวได้") has exactly one Tenant with exactly
one Company — `AdvanceHostOptions.TenantResolution = Fixed` (`Advance.Host.Web/AdvanceHostOptions.cs`) exists
specifically for this case, so the two axes are present in the schema but invisible in the UI/ops for a customer who
never needed multi-company in the first place.

## 2. How the four projects compose in one standalone deployment

```
                         Advance.Payroll.Web  (or Advance.Workflow.Web)
                         Program.cs
                         ─────────────────────────────────────────────
                         builder.Services
                             .AddAdvanceSecurityCore(...)   // 1st — login/Identity/menu/AD.CRUDManage
                             .AddAdvanceHost(...)            // 2nd — Tenant table + ICurrentTenant + Excel DI
                             .AddPayrollEngine(...)          // 3rd — the product's own Domain/Data/Engine/Blazor

                         var app = builder.Build();
                         app.UseAuthentication();
                         app.UseAuthorization();
                         app.UseAdvanceSecurityCore(...)     // password-policy gate, menu claims, etc.
                         app.UseAdvanceHost(isDev);           // tenant-aware migrations, SecurityCore-present check
                         app.MapRazorComponents<App>()...     // product's own pages, incl. Payroll.Blazor RCL

        ┌───────────────────────────────┬────────────────────────────────┐
        │                                │                                 │
        ▼                                ▼                                 ▼
 Advance.SecurityCore            Advance.Host                    Advance.Payroll /
 (not yet scaffolded as of       (this scaffold)                 Advance.Workflow
 this writing — separate                                         (own worktrees)
 agent, separate worktree)       Advance.Host.Web
                                    ├─ references Advance.Host.Data
                                 Identity, sc_user-style claims      ├─ references Advance.Host.Excel     PayrollDbContext /
                                 bridging, password policy,       Advance.Host.Data                       WorkflowDbContext
                                 menu + AD.CRUDManage rights          └─ references Advance.Host.Domain   mark their own
                                                                   Advance.Host.Excel                     entities
                                                                       └─ references nothing (ClosedXML   ITenantScoped and
                                                                          only)                            apply the same
                                                                   Advance.Host.Domain                    HasQueryFilter
                                                                       └─ references nothing               pattern (see
                                                                                                            HostDbContext.cs
                                                                                                            header comment)
```

Dependency direction (bottom to top, matching `ADVANCE-ARCHITECTURE.md` section 2's "app → ระดับ 3→2→1→0 ได้เสมอ"
rule): `Domain` ← `Data`/`Excel` ← `Web`. `Advance.Payroll`/`Advance.Workflow` reference `Advance.Host` and
`Advance.SecurityCore`; neither of those two ever references back. `Advance.Host` does not (and must not) reference
`Advance.Payroll` or `Advance.Workflow` — it has zero knowledge of either product, same "package doesn't know its
consumers" rule the central architecture doc states for every shared package.

**Call order matters for one reason only:** `AddAdvanceHost`'s `TryAddScoped<IAdvanceSecurityCoreMarker, ...>` is a
no-op fallback (see `SecurityCore/IAdvanceSecurityCoreMarker.cs`) — if a real `AddAdvanceSecurityCore()` is ever
called, it must run first so its registration wins the `TryAdd`.

## 3. Reconciling with Advance.SecurityCore

`D:\GitWorkspace\HRM\src\Advance.SecurityCore\` did not exist on disk when this scaffold was built (checked
directly; the other agent may have since produced it in its own isolated worktree, which this one cannot see per
the task's own instructions). Consequences:

- `Advance.Host.Web` has **no project reference** to Advance.SecurityCore. It defines a local placeholder,
  `IAdvanceSecurityCoreMarker` (`Advance.Host.Web/SecurityCore/IAdvanceSecurityCoreMarker.cs`), purely so
  `UseAdvanceHost` has something to check and warn on instead of silently assuming Identity is wired.
- **This is the single biggest open item in this whole scaffold** (see "Biggest open question" below) — the
  placeholder's shape (`bool IsRegistered`) is a guess. The real reconciliation needs one decision this scaffold
  could not make blind: does `AddAdvanceSecurityCore()` return something `AddAdvanceHost()` consumes (composition),
  or does `AddAdvanceHost()` simply require the caller to have called `AddAdvanceSecurityCore()` first and only
  verify it happened (sequencing)? Once the real SecurityCore extension methods exist, delete the placeholder file
  and pick one of those two shapes — whichever matches how SecurityCore itself was actually built.

## 4. What HRM itself must NOT do

**HRM does not adopt Advance.Host.** Confirmed against the plan's own staged approach (`Plan_Split_Payroll_Workflow_v1.1.md`
section 3, "ระดับ 0–1... Advance.Auth... ยังไม่มี: ระหว่างนี้ HumanOk ใช้ Identity เดิม"):

- HRM keeps `ApplicationDbContext` + `IdentityConstants.ApplicationScheme` exactly as-is (`Program.cs`, lines
  ~146–250 in this worktree) — no `HostDbContext`, no `ICurrentTenant`, no `AddAdvanceHost()` call anywhere in
  `HRM.csproj`'s `Program.cs`.
- HRM keeps its single-company-with-multi-tenant-flavored-string `CompanyId` convention exactly as documented in its
  own CLAUDE.md ("Company scoping is a string, not a numeric FK — deliberately"). It does not grow a `TenantId`
  column anywhere. `com_company` already gives HRM the "more than one company" capability it actually needs (id 3,
  code `ADVD`, per the CLAUDE.md's own verified note) — Advance.Host's Tenant concept solves a different problem
  (a SaaS subscriber account with no relationship to HRM's own login/company data) that HRM does not have.
- If HRM ever *does* sit behind a shared host later (the plan explicitly defers this — "HumanOk ใช้ Identity เดิม"
  is the stated near-term state), that is a separate, deliberate migration decision for a human to make, not
  something this scaffold's existence should be read as nudging toward.

This scaffold exists **only** so `Advance.Payroll.Web`/`Advance.Workflow.Web` — sold and deployed with zero HRM
present — get login+tenant+menu+import scaffolding "for free" per the plan's stated goal, exactly as phase 3/4 of
the plan describes.

## 5. Effort estimate for this piece

What exists after this scaffold: 4 compiling projects, a real (if minimal) tenant data model + query-filter/
interceptor pattern, a real generic Excel importer, and DI/pipeline extension points with one placeholder pending
SecurityCore.

What is NOT done and still needs real engineering time before any product can use this in earnest:

| Remaining work | Rough estimate |
|---|---|
| Real `AddAdvanceSecurityCore`/`UseAdvanceSecurityCore` integration (once that project exists) — replace the placeholder, decide composition vs. sequencing (section 3 above) | 2–3 days |
| Tenant provisioning/admin UI (create tenant, invite first user, deactivate) — today `Tenant` is a bare EF entity with no service or page | 3–5 days |
| `HostDbContext` migrations (this scaffold ships the model only — no `Migrations/` folder, per this task's "don't run migrations on the main app" constraint, but the standalone product will need its own) | 0.5 day (mechanical, once the product's own DB is real) |
| Wiring the global-query-filter reflection pattern documented in `HostDbContext.cs`'s header comment into a real consuming `PayrollDbContext`/`WorkflowDbContext` and proving it with a cross-tenant-leak test | 2–3 days |
| Menu/AD.CRUDManage seeding for the standalone host shell (HRM's `ScProgramRouteSeeder`/`ProgramRoleService` equivalent doesn't exist here — SecurityCore's job, not Host's, but the two need to agree on the contract) | depends entirely on SecurityCore's design, not estimable from this side alone |
| First real "นำเข้า Excel" screen built on `ExcelImporter` (employee import is the obvious first target) to prove the generic helper's shape holds up against a real messy spreadsheet | 2–4 days |

**Total for Advance.Host to go from "compiles" to "a product can actually onboard its first tenant on it": roughly
2–3 weeks**, gated almost entirely on Advance.SecurityCore's real shape landing first — everything else here is
comparatively mechanical.

## 6. Biggest open question to flag to a human

**Does "Tenant" in Advance.Host duplicate or compete with Advance.Center's "Party" concept?**

`D:\GitWorkspace\Advance.Center\README.md` describes itself as the "Commercial & Lifecycle Core for Advance
Digital's products" with a working `Party (Seller/Payer)` master data model, and its spec explicitly covers
entitlement/subscription — which sounds like it already owns "who is our customer" for the whole product line,
Advance.Payroll/Workflow included. This scaffold's `Tenant` entity is deliberately tiny (Id/Code/Name/IsActive/
CreatedDate) specifically to avoid re-modeling identity/commercial data Advance.Center might already own — but
nobody has yet decided **whether `Tenant.Id` in Advance.Host is meant to BE (or 1:1 map to) an Advance.Center
`Party.Id`**, or whether they are intentionally two different, unrelated ids (technical multi-tenancy key vs.
commercial/billing identity) that happen to both exist per customer. Building tenant provisioning UI or entitlement
checks before that's settled risks building the wrong join key twice — this is worth a direct decision from the CEO
or whoever owns both plans before Phase 3 (Advance.Payroll SaaS host) starts for real.

