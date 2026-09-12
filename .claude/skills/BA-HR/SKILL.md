---
name: BA-HR
description: World-class HR Director-level business/management judgment PLUS a map of which table in the HRM codebase already models each concept — correct definitions of workforce concepts (job vs position vs incumbent, position-based budgeting vs person-based compensation, อัตรากำลัง vs headcount, org design, workforce planning cycle, Thai labor law basics on leave/severance/tax/social security). Load this before writing, explaining, or reviewing ANY feature that touches organization structure, positions/headcount, employee assignment, leave, payroll, recruitment, performance, or workflow approval in the HRM project — even if the user's request doesn't use the English/technical term, since Thai HR terminology (อัตรากำลัง, สังกัด, เลขที่อัตรา, ตำแหน่ง, ผังองค์กร) maps to specific, non-obvious concepts that are easy to get wrong by guessing from the words alone or from generic software-engineering instinct instead of senior HR-practitioner judgment. Also load it before modeling, changing, or REMOVING any field/table for an HR concept — a field that looks like duplicated or resurrected legacy data is not automatically redundant; check whether it answers a distinct, real business need before touching it.
---

# HR domain knowledge for HRM

This project is a real HR system, not a generic CRUD app. Getting the domain concepts right matters as much as getting the code right. Being a good engineer on this codebase requires thinking like a senior HR Director evaluating a real business need, not just a developer pattern-matching field names against each other.

## Director-level judgment: "looks duplicated" is not the same as "is redundant"

A real mistake from this session: two tables both had `MinSalary`/`MaxSalary`-shaped fields (`Pos_PositionSlot` at the seat level, `Pay_SalaryGrade` at the person level) and the instinct was to treat that as an accidental duplication to delete — without first asking what business question each one answers. A world-class HR Director asks **"what decision does this number support, and who makes that decision"** before ever calling two similar-looking fields redundant:

- **Position-based budgeting** (a SEAT's approved salary range, e.g. on `Pos_PositionSlot`) answers: "when Finance approves this headcount, what's the ceiling we're allowed to pay whoever fills it?" — a workforce-planning/budget-approval question, decided BEFORE anyone is hired, independent of who ends up in the seat.
- **Person-based compensation** (`Hremployee.SalaryGradeId` → `Pay_SalaryGrade`) answers: "what is this specific person actually paid, and is it within their assigned grade?" — a compensation-administration question, decided per person, per comp review cycle.

Both are standard, legitimate, coexisting HR/Finance practices — most mature HRIS and ERP systems (SAP SuccessFactors, Workday) keep exactly this split (position budget vs. incumbent compensation). Seeing that `Pos_PositionSlot`'s salary fields resemble the old legacy `pos_position` table's fields is not evidence they're dead weight — a field inherited from a legacy table can still be answering a real, current business need. **Before proposing to delete or merge a field because it "looks like" another one, name the specific business decision each one serves — if they serve different decisions, keep both.**

## General HR management knowledge (true regardless of this codebase)

## General HR management knowledge (true regardless of this codebase)

**Job vs Position vs Incumbent** — the foundational triad of workforce/org management everywhere, not just here:
- **Job** — a reusable role definition/title ("ผู้จัดการฝ่ายขาย"), independent of any org unit or person.
- **Position** (a.k.a. seat, อัตรา) — one specific, countable instance of a job, tied to one org unit, that may be filled or vacant.
- **Incumbent** — the actual person currently occupying a position, if any.
A position existing does not imply it's filled; a person existing does not imply their position was ever formally created. Systems that conflate these three lose the ability to answer basic workforce questions ("how many approved seats do we have" vs "how many people do we have" vs "what titles exist").

**The workforce planning cycle** — Plan/Budget (how many seats SHOULD exist) → Establish (create the seats) → Recruit/Fill (assign an incumbent) → Manage (transfer, promote, re-grade) → Separate (resignation/termination frees the seat). A mature HR system reports variance at every stage (budgeted vs established, established vs filled) — these are meant to differ; that's the point of tracking them separately, not a data-quality problem to "fix" by collapsing them together.

**Org design basics** — span of control (how many direct reports is reasonable per manager), hierarchy depth, line functions (produce the core business) vs staff/support functions (HR, finance, IT), and that reporting-line hierarchy (who approves your leave) can differ from cost-center/budget hierarchy (who pays your salary) — don't assume a single tree always answers both questions.

**Thai labor law basics relevant to this system** (see also `[[hrm_leave_module_upgrade_status]]` and the payroll memories for what's actually built): statutory leave types and minimums (ลาป่วยได้ค่าจ้างไม่เกิน 30 วัน/ปี ม.32, ลากิจธุระจำเป็นไม่น้อยกว่า 3 วัน ม.34/1, ลาพักร้อนไม่น้อยกว่า 6 วันหลังทำงานครบ 1 ปี ม.30, ลาคลอด 98 วัน ม.41), ค่าชดเชยเลิกจ้างตามอายุงาน (ม.118), ภาษีเงินได้บุคคลธรรมดาแบบขั้นบันได (ไม่ใช่ flat rate), เงินสมทบประกันสังคม, กองทุนสำรองเลี้ยงชีพ. Never assume a Western/generic HR rule (e.g. flat-rate tax, a fixed number of PTO days regardless of tenure) applies here.

**Core HR process areas** (why this codebase has the module prefixes it has — see root `CLAUDE.md`'s module list): Recruitment (`Rec_*`) → Onboarding (`Hrd_*`) → Org/Position management (`com_organization`, `Pos_*`) → Time & Attendance (`Att_*`) → Leave (`Lve_*`) → Payroll (`Pay_*`) → Performance/OKR (`Perf_*`, `Okr_*`) → Learning & Development (`Lms_*`, `Idp_*`) → Talent/Succession (`Talent_*`) → Engagement (`Eng_*`) → Offboarding (`Hrd_*`). A feature request almost always belongs to one of these well-understood HR processes — placing it in the right one (and knowing what that process normally does elsewhere in the industry) beats inventing a bespoke shape for it.

## Concept map for THIS codebase — don't conflate these

| Thai term | What it actually means | Table in this codebase |
|---|---|---|
| **อัตรากำลัง** (establishment / manpower plan) | A budgeted/approved SEAT that may or may not be filled. Counting "อัตรากำลัง" means counting seats, not people — a vacant seat still counts. | `Pos_PositionSlot` (the real, currently-used one — one row per seat, `HremployeeId` null = vacant) |
| **งบประมาณอัตรากำลัง** (headcount budget) | The CEILING on how many seats are allowed to exist (a plan/approval number, e.g. "5 seats approved for Accounting in FY2026") — NOT the seats themselves | `Pos_HeadcountBudget` — deliberately a separate table from `Pos_PositionSlot` so "approved vs actual" can be compared. Don't ever suggest merging these. |
| **จำนวนพนักงานจริง** (actual headcount) | The real count of people currently assigned, independent of whether a seat was ever formally created for them | `Hremployee.OrganizationId` (denormalized snapshot — see below) |
| **สังกัด / ผังองค์กร** (org structure) | The org-unit tree itself — WHERE units sit relative to each other. Has nothing to do with how many people or seats are in a unit. | `com_organization` (self-referencing via `code`/`parent_code`) |
| **ตำแหน่ง** (position/job title, the reusable text) | A reusable title like "ผู้จัดการฝ่ายขาย" — not tied to a specific org unit or person | `Pos_ExecType` (current) vs `pos_position` (legacy JSP table, reference-only, explicitly marked "(Legacy)" in its own page — see `Components/Pages/Pos/PosPositionAdmin.razor`'s own on-page caption before assuming it's dead weight to delete) |
| **ช่วงเงินเดือนของอัตรา** (position budget range) | The approved salary CEILING for a seat, set at budget-approval time, independent of who fills it — a legitimate, kept field, NOT a redundant copy of the person's actual pay | `Pos_PositionSlot.MinSalary/NormalSalary/MaxSalary` — coexists with, does not duplicate, `Hremployee.SalaryGradeId → Pay_SalaryGrade` (the person's actual grade) |
| **เลขที่อัตรา** (the slot admin page) | The UI for managing `Pos_PositionSlot` rows directly — this IS the manpower/establishment management screen, not a duplicate of anything | `/pos/position-slots` (`PositionSlotAdmin.razor`) |

**Headcount COUNT vs. salary AMOUNT need opposite enforcement — never conflate the two (CEO, 12 ก.ย. 2569):**
- **Headcount count** (how many seats exist) MAY be hard-blocked against `Pos_HeadcountBudget.ApprovedCount` — this is a structural/establishment decision, and the existing `BlockOverBudgetCreation` company setting is correct as-is.
- **Salary amount** (what a specific hire is paid) must **NEVER** be hard-blocked against a position's budget range or a person's grade band — "งบเอาไว้คำนวณตอนจ้าง ถ้าคนเก่งงบเกิน ก็ต้องเกินได้ ห้าม lock เงินเดือนด้วย" (the range is for calculating an offer, not a cap — an exceptional hire must be payable above it). `EmployeeCoreFieldsForm.razor`'s compa-ratio check already gets this right today (`Severity.Warning`, never blocks save) — keep any future position-budget-vs-actual-salary comparison informational only, the same way.

**The single most common mistake**: seeing `Pos_PositionSlot` sparsely populated (few rows) next to `Hremployee.OrganizationId` fully populated (thousands of rows) and concluding the slot table is "broken" or "the wrong source" — then either rewriting a page to read `Hremployee.OrganizationId` instead, or trying to auto-generate/backfill slot rows to "fix" the gap. **Neither is correct.** A sparse `Pos_PositionSlot` table most often means HR genuinely hasn't gone and created formal seats yet — that is real information (อัตรากำลังยังไม่ได้ตั้ง ไม่ใช่บั๊ก), and a page whose job is to show อัตรากำลัง must show that sparse truth as-is. **This system's job is to display and manage real data — never to manufacture data (real or synthetic) to make a screen look more populated than reality.** If a number legitimately needs to come from actual employee assignment instead (e.g. "how many people work here right now" as opposed to "how many seats are budgeted here"), that's a genuinely different question with a genuinely different answer — decide which question is actually being asked before picking the data source, and when unsure, ask instead of guessing.

## Known gaps in `Pos_HeadcountBudget` (Director-level review, 12 ก.ย. 2569)

`PositionSlotAdmin.razor` already gates slot creation against `Pos_HeadcountBudget.ApprovedCount` (block or warn, per `Pay_PayslipSettings.BlockOverBudgetCreation` — see `Services/Pos/HeadcountBudgetService.CheckBeforeAddAsync`), which correctly prevents silent over-creation one row at a time. But the budget NUMBER itself has real, identified gaps — don't rediscover these, and don't fix them without the user picking which to prioritize:

1. **`ApprovedCount` is a free-text admin field, not an approved document.** Whoever can open `HeadcountBudgetAdmin.razor` can type any number — no CFO/CEO sign-off captured, despite it gating a real future payroll commitment. A mature system routes this through the existing generic Workflow Engine (`StartJobAsync`), same as every other approval-worthy document here.
2. **Budget is headcount-count-only, not cost.** Two approved seats at different salary bands have very different financial impact; a real Finance/HR Director budgets in currency first, headcount second. This connects to `Pos_PositionSlot.MinSalary/NormalSalary/MaxSalary` (the position budget range) — a cost rollup would sum `ApprovedCount × band` per scope.
3. **`FiscalYear` is a label, not an enforced window.** No policy yet for what happens to unused approved headcount at year-end (expire vs. carry forward).
4. **No variance dashboard.** Today the only signal is reactive (a warning/block at the moment someone tries to over-create). A Director wants to see budget vs. established vs. actually-filled, per org, at a glance — not discover the gap one creation attempt at a time.

## `Hremployee.OrganizationId` is a snapshot, not a master

See the doc comment on `Hremployee.OrganizationId` in `Model/Hremployee.cs`: it is a denormalized snapshot whose source of truth is `Pos_PositionSlot.HremployeeId`, kept in sync one-way by `Services/Shared/EmployeePositionSync.cs` (slot → employee) and `Services/Hr/SeparationRequestService.cs` (employee resignation → frees the slot). Never write to it from a new code path directly; never treat it as more authoritative than the slot it was denormalized from, even if it happens to be more completely populated in a particular dataset (that populate-without-going-through-slots pattern is a demo-seeding shortcut, e.g. `Services/Dev/AdvdHrdDemoSeeder.cs`, not the production data model).

## Before building anything HR-shaped

1. **Ask what the Thai term actually means to HR, in plain business terms, before picking a data source or a UI shape.** "จำนวนคน" and "อัตรากำลัง" sound similar and are NOT the same number.
2. **Check the concept map above and `Model/`/`Services/` for an existing table/service** — this legacy-schema-first codebase (see root `CLAUDE.md`) very often already has the right table; a table that "looks empty" is a data-completeness question, not a design gap (see [[feedback_build_on_users_existing_db_design]]).
3. **Never auto-generate or backfill data to make a report/tree "look right"** — real emptiness is a legitimate, correct answer. If a number is missing and matters, say so and ask whether HR needs to go enter it, rather than deriving a stand-in number from a different, not-quite-equivalent source.
4. When genuinely unsure which of two similar-looking numbers (budgeted vs actual, established vs occupied, direct vs subtree) is being asked for, **ask** — per [[feedback_table_config_when_unclear]] and [[feedback_ask_on_typo_or_unclear_table_name]].
