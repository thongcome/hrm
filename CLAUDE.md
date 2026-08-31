# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Build (always use `-p:UseAppHost=false` — a running `HRM.exe` from a previous `dotnet run` otherwise locks the output and build fails with `MSB3027`/`MSB3021`; kill it first with `Get-Process -Name HRM | Stop-Process -Force` if that happens):
```
dotnet build -p:UseAppHost=false
```

Run the dev server (also available via `.claude/launch.json` config `hrm-dev`, port 5052):
```
dotnet run --launch-profile http
```

EF Core migrations — this project has no seed data beyond what's already applied, and every migration that seeds `sc_menu`/`sc_role_menu`/`wf_*`/similar tables queries `MAX(id)` from the live database first rather than hardcoding a guessed ID (ids drift as the session's migration history grows):
```
dotnet ef migrations add <Name>
dotnet ef database update
```

Tests (xUnit, in `HRM.Tests/`):
```
dotnet test
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"
```

## Architecture

**Modular monolith, not microservices.** One deploy, one `HRMContext`/database, but each business domain lives under its own folder and its own entity-name prefix: `Pay_*` (payroll), `Att_*` (attendance/shifts), `Lve_*`/`loa` (leave), `Wf_*`/`wf_*`/`job_*` (workflow approval engine), `Perf_*` (performance/KPI), `Idp_*` (development plans), `Talent_*`, `Rec_*` (recruitment/ATS), `Lms_*`, `Km_*`, `Hrd_*` (onboarding/offboarding lifecycle), `Org_*` (org-change requests), `Okr_*`. New modules follow this same convention: prefix + own `Services/<Module>/`, `Components/Pages/<Module>/`, own migration.

**This is a legacy JSP-era system being modernized in place, not a greenfield app.** `Model/` has 340+ entity classes because the original schema (JSP + a later `epms`/`vms` .NET rewrite) was scaffolded wholesale from the database, but only a fraction of those tables are wired to any service or UI — most are dormant scaffolding nobody has touched since the EF reverse-engineer. Recurring pattern discovered repeatedly across this codebase's history: **a table that looks like it needs to be designed from scratch usually already exists correctly in the legacy schema** (`sc_menu`'s 4-level tree, `job_master`/`job_user_list`/`job_subworkflow_master` for the workflow engine, `wf_sub_workflow_master`'s LOA/AND-condition/vertical-approval columns, `doc_center` as a generic file-attachment table keyed by `refid`+`doctypecode`). Before modeling a new table, search `Model/` and the live DB schema for something that already covers the need — check `sys.columns.is_identity`/`is_nullable` on any legacy table before writing a form against it, since several (`wf_org_type`, `wf_employee`, `wf_checklist`, `wf_customer_approver`) do **not** have real IDENTITY columns despite the EF model claiming `DatabaseGeneratedOption.Identity` (use `Services/Shared/EntitySearchHelper.NextIdAsync<T>()` for those instead of `id == 0` checks).

**Auth is a single scheme end-to-end.** Both HR staff and self-service employees sign in through `/login` → `Endpoints/LoginEndpoints.cs` → `SignInManager<ApplicationUser>`, landing in `IdentityConstants.ApplicationScheme` (the sole default scheme — an earlier dual-scheme setup was removed). `Services/Login/ScUserClaimsPrincipalFactory.cs` runs on every sign-in and bridges the legacy `sc_user` row to claims (`sc_userid`, `empno`, `payroll_company`, role, `menu`, `menu_edit`) that everything downstream depends on. Authorization is a dynamic `[Authorize(Policy = "Menu:XXX")]` per page, resolved at runtime by `Services/Login/MenuAuthorization.cs` against the `menu` claim — there's no fixed enum of policies to update when adding a page, just seed the right `sc_menu`/`sc_role_menu` rows in a migration.

**Company scoping is a string, not a numeric FK.** Every company-specific table uses `CompanyId`/`companyid` matching `Hremployee.companyid` (and the `payroll_company` claim), never a foreign key into `com_company` — that table's `code` lives in a different ID space than `companyid` and the two do not join.

**Audit/PDPA logging is automatic for writes, explicit for reads.** `Model/HRMContext.Audit.cs` overrides `SaveChangesAsync` and logs every Create/Update/Delete on every entity to `AuditLog` without any call site needing to do anything. The gap that auto-hook can't cover is *viewing* sensitive data (never touches `SaveChanges`), which is why pages showing PDPA-sensitive fields call `IAuditLogger.LogAccessAsync(...)` explicitly on open, and show a small "PDPA" badge. This is a hard compliance requirement (Thai พ.ร.บ.คอมพิวเตอร์ mandates ≥90-day retention of actor+IP+timestamp for sensitive access) — don't add any cleanup/purge job that deletes `AuditLog` rows before that.

**The workflow approval engine (`Services/Workflow/WorkflowEngineService.cs`) is generic, not per-module.** Any module needing multi-level approval (leave, OT, IDP plans, org changes, recruitment offers, LMS enrollment, disciplinary/reward cases, ...) calls `StartJobAsync(workflowId, reftable, refid, requesterUserId, requesterEmpId, subject, amount, ct)` and lets the engine resolve approvers (custom user/role, vertical org-chain via `com_organization.approver_empid`, LOA amount bands, AND/OR conditions) against `wf_sub_workflow_master` config rather than hardcoding approval logic per module. Callers read status back via a lazy apply-on-read pattern (`SyncStatusFromJobAsync`, called on page load — there is no background scheduler anywhere in this app) rather than a push/webhook.

**CRUD pages follow a fixed, deliberately un-abstracted shape** — see `.claude/skills/CRUD/SKILL.md` before writing any new list/detail page: direct `EditForm`/`IDbContextFactory<HRMContext>` per page (no shared generic CRUD shell — one was tried and reverted), one search box per list using `Services/Shared/EntitySearchHelper.ApplyTextSearch`, foreign keys always resolved to `MudSelect`/`MudAutocomplete` pickers never raw ID fields, soft delete via `IsActive` never hard `DELETE`. The `advance-crud-standard` and `advance-data-discipline` skills (user-level, always available) cover the broader master-data/drill-down/audit conventions this CRUD pattern sits inside of.

**AD.CRUDManage is ACTIVE in this project (CEO order, 2026-08-31).** Per-action page rights live in `sc_program_role` (one row per role × route prefix, independent Create/Read/Edit/Delete flags — `Model/sc_program_role.cs`), auto-seeded at every startup by `ProgramRoleService.SeedAsync` scanning all `@page` routes (parameter segments stripped; nobody registers pages by hand; existing rows never touched). Checks go through `Services/Security/ProgramRoleService` — table reads behind a ~60s memory cache, never login-cookie claims, longest-prefix path matching, fail-closed. **Documented deviation from the skill's all-read-only seed default**: the `Admin` role seeds with all four flags (activation on a live system must not read-only the operators); every other role seeds read-only. Wiring a page = both layers always: render Create/Edit/Delete buttons from `GetRightsAsync` (with a visible "หน้านี้ยังไม่ได้เปิดสิทธิ์แก้ไขให้บทบาทของคุณ" line when unwritable — never silently missing buttons), and call `RequireAsync` at the top of every write handler. The older `Program:XXX` progcode mechanism (`ProgramAuthorization.cs`, wired on JobFamilyAdmin only) is a superseded proof-of-concept — don't extend it to new pages.
