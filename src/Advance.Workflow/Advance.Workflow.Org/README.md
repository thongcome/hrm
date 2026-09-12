# Advance.Workflow.Org — Workflow's own people/org-chart component

## What this is

CEO, 12 ก.ย. 2569: *"Advance.Workflow จะต้องแยกไปขาย โดยมี workflow component HR
เกี่ยวกับผัง และ workflow"* — Advance.Workflow must be sellable **without**
HumanOk, which means it needs its own registry of people and its own org
chart, not a hard dependency on `Hremployee`/`com_organization`.

This is not a new design. Two tables already exist in the legacy JSP-era
schema for exactly this purpose:

| Table | Entity | Meaning |
|---|---|---|
| `wf_org_type` | `wf_org_type.cs` | The org chart, as a tree — `orgcode`/`orgcodefull`/`upperorg` (parent link), `istoplevel`, `levelno`. |
| `wf_employee` | `wf_employee.cs` | The people registry — name (Thai/English), card id, department (`orgcode`), status, contact info. |

Both tables are copied **field-for-field** from `HRM/Model/wf_org_type.cs` and
`HRM/Model/wf_employee.cs` — same table name, same column names and types, same
primary key shape. This is deliberate: it means

- **Zero migration needed to use this project inside HRM.** The tables
  already exist in HRM's live database (currently 0/1 rows — dormant
  scaffolding, per `CLAUDE.md`'s note on 340+ mostly-unused `Model/` classes).
  HRM already has two working admin CRUD pages against them
  (`Components/Pages/Wf/WfEmployeeAdmin.razor`, `WfOrgTypeAdmin.razor`, menu
  entries `/wf/employees` and `/wf/org-types`), so the UI pattern is proven —
  see `Advance.Workflow.Blazor` for the ported copies.
- **A standalone deployment creates them fresh.** Point `WorkflowOrgDbContext`
  at a brand-new database and its own migrations (not yet written — Phase 1)
  create these two tables with nothing else attached, ready for a customer
  who has no HumanOk at all.

## What is NOT done yet (Phase 0 honesty)

- No sync job exists. `Advance.Workflow.Contracts.IOrgDirectorySource` is the
  interface HRM would implement to push `Hremployee`/`com_organization`
  changes into these tables one-way, but nothing calls it yet.
- The engine (`Advance.Workflow.Engine`) does **not** read from this project
  yet — see the `TODO(seam)` comments in
  `Advance.Workflow.Engine/WorkflowOrgChainResolver.cs`. Wiring the engine to
  resolve approvers from `wf_employee`/`wf_org_type` instead of
  `Hremployee`/`com_organization` is Phase 1 work, not Phase 0.
- Only the two tables the plan explicitly named are here. The plan's
  architecture diagram also mentions "ผู้ใช้/บทบาทของ Workflow" (Workflow's own
  user/role tables, for login when sold standalone) and `Wf_ApproverDelegation`
  — neither exists as a table today; designing those is future work, flagged
  in `EXTRACTION-PLAN.md`.
- `id` on both tables is **not** a real IDENTITY column in the live database
  (verified via `sys.columns.is_identity = 0`) despite the EF model's
  `DatabaseGeneratedOption.None` looking unusual for a `[Key]` — this is
  intentional and copied verbatim; see `CLAUDE.md`'s standing note about this
  and the other tables like it (`wf_checklist`, `wf_customer_approver`). Any
  code that inserts a row must assign `id` itself (HRM's pattern:
  `Services/Shared/EntitySearchHelper.NextIdAsync`).
