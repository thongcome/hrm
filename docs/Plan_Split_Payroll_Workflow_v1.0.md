# แผนแยกโปรเจค: Advance.Payroll · Advance.Workflow · HumanOk (HRM)

เวอร์ชัน 1.0 — 12 ก.ย. 2569 — ร่างเพื่อตัดสินใจ (ยังไม่เริ่มทำ)

> โจทย์จาก CEO: "ถ้าจะแยกโปรเจค Payroll ออกมาเป็น Payroll Lite (HumanOk) ตัวนี้จะชื่อ Advance.Payroll แต่ยังไม่ชัวร์ ช่วยทำ plan แยกทั้ง HRM, Workflow" และก่อนหน้านี้ "เดี๋ยวผมจะแยกโปรเจคเอาไปขายบริษัทเล็ก ๆ รวมทั้ง SaaS"

---

## 1. เป้าหมาย: 3 ผลิตภัณฑ์ 1 แกนร่วม

| ผลิตภัณฑ์ | ขายให้ใคร | รูปแบบ | มาจากโค้ดส่วนไหนของ HRM วันนี้ |
|---|---|---|---|
| **HumanOk (HRM)** | องค์กรกลาง–ใหญ่ | on-premise / private cloud ต่อลูกค้า | ทั้งหมด (24 โมดูล) |
| **Advance.Payroll** (ชื่อตลาด: HumanOk Payroll) | บริษัทเล็ก–กลาง | SaaS หลายบริษัทในระบบเดียว + ติดตั้งเดี่ยวได้ | `Pay_*` 45 ตาราง · `Services/Pay` 39 ไฟล์ · หน้าจอ 41 หน้า + ESS 2 หน้า |
| **Advance.Workflow** | ขายแยก / ฝังในทุกผลิตภัณฑ์ของเครือ | package ที่ทุก app ฝังได้ + ตัวเดี่ยวมี API ในอนาคต | `wf_*`/`job_*` 29 ตาราง · `Services/Workflow` 7 ไฟล์ · หน้าจอ 28 หน้า |

หลักการที่ยึด (ตาม `D:\GitWorkspace\ADVANCE-ARCHITECTURE.md`):

- โครง `X.Domain / X.Data / X.Web + tests` ทุกผลิตภัณฑ์ · แชร์ผ่าน package ไม่ copy โค้ด
- app ห้ามอ้าง app — คุยกันผ่าน interface/package หรือ API เท่านั้น
- **rule of two**: ทำเป็น package เมื่อมีผู้ใช้จริง ≥ 2 ตัว — Workflow มีแล้ว 2 (HumanOk + Payroll จะใช้), Payroll เองคือผลิตภัณฑ์ไม่ใช่ package
- แยกทีละขั้นในที่เดิมก่อน (project ใน solution เดียวกัน) แล้วค่อยแยก repo เมื่อ build/ทดสอบผ่านนิ่ง — ไม่ "copy โฟลเดอร์แล้วแก้สองที่"

---

## 2. สภาพวันนี้ (สำรวจโค้ดจริง 12 ก.ย. 2569)

### 2.1 Payroll พึ่งพาอะไรนอกตัวเอง

| สิ่งที่ Payroll อ่าน | ใช้ทำอะไร | ใน Advance.Payroll เดี่ยว ๆ มาจากไหน |
|---|---|---|
| `Hremployee` (ใช้ ~20 จาก 108 คอลัมน์: รหัส ชื่อ บัตรประชาชน วันเกิด เพศ เข้า/ออก เงินเดือน/ค่าจ้างรายวัน ธนาคาร อัตรากองทุน ศูนย์ต้นทุน เลขสมาชิกสหกรณ์) | ใครอยู่ในรอบ ฐานค่าจ้าง ไฟล์ธนาคาร สลิป 50 ทวิ | ตารางพนักงานของ Payroll เอง (`pay_employee` ~25 คอลัมน์) — ใน HumanOk ซิงก์จาก `Hremployee` |
| `Lve_CompanySetting`, `Lve_CompanyHoliday` | วันทำงาน/วันหยุด (ค่าจ้างรายวัน) | ตารางวันหยุดของ Payroll เอง |
| `Att_DailyAttendance` | หักสาย/ขาด | "feed" — HumanOk ส่งจากระบบเวลา / Lite กรอกหรือนำเข้า Excel |
| `HrwOt` (OT ระบบเก่า) | ค่าล่วงเวลา | feed เดียวกัน |
| `Kptempreceive*` (เงินกู้สหกรณ์ระบบเก่า) | หักเงินกู้ | feed (Lite: ไม่มี ใช้ `Pay_EmployeeLoan` ที่มีอยู่แล้วแทน) |
| `Wel_BenefitType`, `Wel_Entitlement`, `Pos_PositionSlot` | สวัสดิการจ่ายประจำ (ค่ารถ ฯลฯ) | feed (Lite: "รายการจ่ายประจำ" ต่อคน) |
| `Hrd_*` (แยกจากงาน/ค่าชดเชย) | ค่าชดเชย ม.118 | feed |
| `wf_workflow`, `job_master` | คำขอกองทุน (ออกจากกองทุน/เปลี่ยนอัตรา) | Advance.Workflow package |
| `sc_user`, `sc_role`, `sc_menu`, `sc_program_role`, Identity | login สิทธิ์เมนู สิทธิ์รายหน้า | Advance.Auth + ตารางสิทธิ์ของ Payroll เอง |
| `AuditLog` (interceptor), อีเมล | audit/PDPA, ส่งสลิป | Advance.Platform + Advance.Notify |

**สิ่งที่ "พร้อมแยก" อยู่แล้ว**: ตัวคำนวณเป็น pure static ทั้งหมด (`TaxBracketCalculator`, `ProrationCalculator`, `PayScheduleResolver`, `SocialSecurityCalculator`, `ProvidentFundCalculator`, `FoldYtd`) ไม่แตะฐานข้อมูล · ทุกค่าตั้งต้นเป็น config ต่อบริษัทแล้ว (รอบจ่าย ภาษี ประกันสังคม กองทุน นโยบายหัก บัญชี GL) · state machine ของรอบ (Draft→Calculated→Reviewed→Approved→Posted→Paid, กลับรายการ/ปรับปรุง) อยู่ใน service เดียว · มีเทสทั้งปี 2568 (`HRM.Tests/Integration/Payroll2025ScenarioTests.cs`) เป็นชุดตรวจรับ

**สิ่งที่ยังขวาง**: `CompanyId` เป็น string ผูกกับ `Hremployee.companyid` (SaaS ต้องการ TenantId ที่ระบบคุมเอง) · trigger กันแก้แถวรอบที่ post แล้วอยู่ในฐานข้อมูล SQL Server (ต้องย้ายเป็นกฎในโค้ดถ้าจะรองรับ Postgres) · หน้า Payroll ระบบเก่า 13 หน้า (~10k บรรทัด) ยังอยู่ · ยังไม่มีไฟล์ e-Filing (ภ.ง.ด.1 ของสรรพากร, สปส.1-10 ของประกันสังคม) และ format ธนาคารจริง (มีแต่ CSV กลาง) — ของจำเป็นก่อนขาย Lite

### 2.2 Workflow พึ่งพาอะไรนอกตัวเอง

| สิ่งที่ engine อ่าน | ใช้ทำอะไร | ใน package จะเป็น |
|---|---|---|
| `sc_user`, `sc_role`, `sc_user_role` | ผู้ยื่น/ผู้อนุมัติเป็นใคร บทบาท | `IWorkflowUserDirectory` — host (HumanOk/Payroll) implement |
| `Hremployee`, `com_organization` (approver_empid, สายบังคับบัญชา) | หาผู้อนุมัติแนวดิ่งจากผังองค์กร | `IApproverResolver` — host implement (Lite: หัวหน้าตรงจากตารางพนักงาน) |
| `AuditLog`, อีเมล | บันทึก/แจ้งเตือน | Advance.Platform + `IWorkflowNotifier` |
| 17 handler เขียนกลับเอกสารเมื่อปิดงาน (`WorkflowDocumentWriteback`) | บอกโมดูลเจ้าของเอกสารว่าอนุมัติ/ปฏิเสธแล้ว | **อยู่กับโมดูลเจ้าของ ไม่ย้ายไปกับ engine** — engine รู้จักแค่ interface `IWorkflowDocumentHandler` |

engine ใหม่ (`WorkflowService`) เดินงานอยู่แล้ว 25 workflow ผ่านสวิตช์ `useNewEngine`; engine เก่า (`WorkflowEngineService`) ยังเป็น facade — **ต้องปลดของเก่าออกก่อนแยก** ไม่งั้นได้ package ที่มีสอง engine

---

## 3. สถาปัตยกรรมเป้าหมาย

```
ระดับ 0–1  Advance.Platform (audit, access log, headers)  ·  Advance.Thai (วันที่ไทย, บัตรประชาชน, BahtText)
           Advance.Auth (login/SSO — ยังไม่มี: ระหว่างนี้ HumanOk ใช้ Identity เดิม, Lite ใช้ Identity ชุดใหม่แบบเดียวกัน)

ระดับ 2    Advance.Workflow  ──────────────────────────────────────────────┐
           ├─ Advance.Workflow.Contracts  (IWorkflowEngine, IWorkflowDocumentHandler,          │
           │                               IApproverResolver, IWorkflowUserDirectory,          │
           │                               IWorkflowNotifier, WorkflowClosedEvent)             │
           ├─ Advance.Workflow.Domain     (wf_*, job_* 29 entities — ชื่อตารางเดิม)            │
           ├─ Advance.Workflow.Data       (WorkflowDbContext + migrations ของตัวเอง)           │
           ├─ Advance.Workflow.Engine     (WorkflowService, health check, stepper model)        │
           └─ Advance.Workflow.Blazor     (Razor Class Library: 28 หน้า + WorkflowStepper)      │
                                                                                               │
ระดับ 3    Advance.Payroll  (ผลิตภัณฑ์)                    HumanOk (ผลิตภัณฑ์)                  │
           ├─ Payroll.Core      pure calculators           ├─ HRM.Web (ทุกโมดูลที่เหลือ)         │
           ├─ Payroll.Domain    Pay_* 45 + pay_employee    ├─ อ้าง Advance.Workflow ◄───────────┘
           ├─ Payroll.Data      PayrollDbContext           ├─ อ้าง Advance.Payroll.* (Engine+Blazor)
           ├─ Payroll.Engine    รอบ/คำนวณ/state machine    └─ HRM.Adapters: IEmployeeSource,
           │                    + feed interfaces              IAttendanceFeed, IOvertimeFeed,
           ├─ Payroll.Reports   ภ.ง.ด.1/1ก/50ทวิ/สปส./       ILoanFeed, IAllowanceFeed,
           │                    ไฟล์ธนาคาร/GL                 IApproverResolver, IWorkflowUserDirectory
           ├─ Payroll.Blazor    RCL: 41 หน้า + ESS 2 หน้า
           └─ Payroll.Web       host SaaS: tenant, onboarding, นำเข้า Excel, billing/entitlement
```

กติกา: Payroll.Engine ห้ามอ้าง HRM · HumanOk อ้าง Payroll/Workflow ได้ · Workflow ห้ามอ้างทั้งสอง · ทุก "ข้อมูลจากโมดูลอื่น" เข้า Payroll ผ่าน feed interface เท่านั้น (HumanOk implement จากระบบเวลา/OT/สวัสดิการ, Lite implement จากหน้ากรอก/นำเข้า)

### 3.1 เจ้าของตาราง (ownership) หลังแยก

| กลุ่ม | ตาราง | เจ้าของ | หมายเหตุ |
|---|---|---|---|
| Workflow | `wf_*` (18), `job_*` (11) | Advance.Workflow | schema `wf` — ชื่อคอลัมน์เดิม ไม่ migrate ข้อมูล |
| Payroll | `Pay_*` (45), `Hrucfsecurity` (อัตราประกันสังคม), `Pay_PayrollPeriod` | Advance.Payroll | เพิ่ม `TenantId` (SaaS) — HumanOk ใส่ค่าเดียว |
| พนักงานสำหรับจ่าย | `pay_employee` (ใหม่ ~25 คอลัมน์) | Advance.Payroll | HumanOk ซิงก์จาก `Hremployee` ผ่าน `IEmployeeSource` (one-way, ทุกครั้งก่อนคำนวณ) — Lite เป็นทะเบียนพนักงานตัวจริง |
| วันหยุด/วันทำงาน | `pay_holiday`, `pay_workday_setting` (ใหม่) | Advance.Payroll | HumanOk ซิงก์จาก `Lve_*` |
| ทุกอย่างที่เหลือ (`HR*`, `Att_*`, `Lve_*`, `Wel_*`, `Perf_*`, `Rec_*`, `sc_*` ฯลฯ) | HumanOk | | |

---

## 4. แผนงานเป็นเฟส

| เฟส | งาน | ผลลัพธ์ | ประมาณ |
|---|---|---|---|
| **0 — ทำตะเข็บให้เห็น** (ใน repo HRM เดิม) | 1) ปิดผลทดสอบเงินเดือนทั้งปี 2568 + แก้บั๊กที่เจอ 2) ประกาศ interface ทั้ง 7 ตัว (ข้อ 3) ไว้ใน HRM แล้วให้ `PayrollCalculationService`/`WorkflowService` เรียกผ่าน interface แทนอ่านตารางอื่นตรง ๆ 3) ปลด engine เก่าของ workflow (เหลือ facade บาง ๆ) 4) เทสสถาปัตยกรรม 1 ตัว: `Services/Pay` ห้ามอ้าง namespace อื่นนอกจาก Pay/Shared/Contracts 5) ย้ายหน้า Payroll ระบบเก่า 13 หน้าเข้าโฟลเดอร์ `Legacy/` ปิดเมนู (ยังไม่ลบ ตามคำสั่งเดิม) | HRM ยัง deploy เหมือนเดิม แต่ Pay/Workflow ไม่มีสายพันกับโมดูลอื่นแล้ว | 2 สัปดาห์ |
| **1 — แยก Advance.Workflow เป็น project** (solution เดียวกัน) | สร้าง 5 project ตามข้อ 3 ใน `HRM.sln` · ย้าย entity/service/หน้าจอ · `WorkflowDbContext` ชี้ตารางเดิม · HRM implement `IApproverResolver`/`IWorkflowUserDirectory`/`IWorkflowNotifier` · 17 handler ยังอยู่ใน HRM · `HRMContext` ตัด DbSet ของ wf/job ออก | HumanOk ใช้ workflow ผ่าน package (ProjectReference ก่อน, NuGet ทีหลัง) เทส 167 ตัวยังเขียว | 2–3 สัปดาห์ |
| **2 — แยก Advance.Payroll เป็น project** (solution เดียวกัน) | สร้าง Core/Domain/Data/Engine/Reports/Blazor · `pay_employee` + ซิงก์จาก `Hremployee` · feed interfaces 5 ตัว HRM implement · `PayrollDbContext` · เทสทั้งปี 2568 ย้ายไปเป็นชุดตรวจรับของ Payroll · ตัด trigger SQL → กฎในโค้ด (interceptor) | HumanOk ใช้ Payroll ผ่าน package · Payroll คำนวณได้โดยไม่มี HRM (เทสพิสูจน์ด้วย feed จำลอง) | 4–6 สัปดาห์ |
| **3 — Advance.Payroll SaaS host** (repo ใหม่ `Advance.Payroll`) | Payroll.Web: tenant (TenantId + filter ทุก query), สมัคร/ตั้งค่าบริษัท (wizard: รอบจ่าย วันหยุด ประกันสังคม กองทุน บัญชี), ทะเบียนพนักงาน + นำเข้า Excel, กรอก OT/ขาด/สาย/รายการเฉพาะกิจ, ESS สลิป/50 ทวิ, ส่งสลิปอีเมล · **e-Filing**: ภ.ง.ด.1/1ก (text ตาม spec สรรพากร), สปส.1-10 (ไฟล์ประกันสังคม), กองทุนสำรองเลี้ยงชีพ (ไฟล์ตาม บลจ.) · format ธนาคาร 3 ธนาคารหลัก · แพ็กเกจ/สิทธิ์ผ่าน Advance.Center (entitlement) · login ผ่าน Identity ชุดของ Payroll (ย้ายไป Advance.Auth เมื่อมี) | ขายได้: บริษัทเล็กสมัครใช้เอง จ่ายรายเดือน | 6–8 สัปดาห์ |
| **4 — Advance.Workflow เป็นผลิตภัณฑ์เดี่ยว** | host + REST API (start/act/inbox/query) + designer (`/wf/canvas`) + ตัวแทน "คน/องค์กร" ผ่าน Advance.Member · ทำเมื่อมีลูกค้าถามซื้อแยกจริง (AutoX กลุ่ม D เป็นกรณีแรก) | ขายแยกได้ | 4–6 สัปดาห์ (หลังเฟส 1 นิ่ง) |

รวมถึงจุดขาย Lite ได้: **เฟส 0–3 ≈ 14–19 สัปดาห์** (คนเดียว + Claude Code เต็มเวลา) — เฟส 1 กับ 2 ทำสลับกันได้ถ้ามีคนสอง

---

## 5. ขอบเขต Advance.Payroll Lite (เสนอ)

**มี**: บริษัทหลายแห่งในระบบเดียว (tenant) · พนักงานรายเดือน/รายวัน · รอบจ่ายเดือนละ 1 หรือ 2 งวด (config) · เข้า/ออกกลางเดือน · OT/ขาด/สาย/รายการเฉพาะกิจ/เงินกู้บริษัท/เบิกล่วงหน้า · ภาษีหัก ณ ที่จ่ายขั้นบันได + ค่าลดหย่อน + เงินได้จากนายจ้างเดิม · ประกันสังคม (เพดานรายเดือน ทั้งสองฝั่ง) · กองทุนสำรองเลี้ยงชีพ · โบนัสภาษีส่วนต่าง · กลับรายการ/ปรับปรุง · พัก/กันออกรายคน · อนุมัติแยกหน้าที่ · ไฟล์ธนาคาร · GL export · สลิป PDF ใส่รหัส + อีเมล + ESS · ภ.ง.ด.1/1ก · 50 ทวิ · e-Filing · audit ครบ

**ไม่มี** (ขาย HumanOk แทน): ระบบเวลา/กะ/ลา · ผังองค์กร/ตำแหน่ง · ประเมินผล/OKR/IDP · สรรหา/อบรม · สวัสดิการซับซ้อน · workflow ปรับแต่งได้ (Lite มีสายอนุมัติตายตัว: ผู้คำนวณ → ผู้อนุมัติ)

---

## 6. ข้อที่ต้องตัดสินใจ (CEO)

| # | คำถาม | ข้อเสนอ |
|---|---|---|
| 1 | ชื่อ repo/ผลิตภัณฑ์ | repo `Advance.Payroll` (ตามเครือ) · ชื่อตลาด **HumanOk Payroll** (ต่อยอดแบรนด์ที่มีเอกสารขายแล้ว) |
| 2 | แยก repo ตอนไหน | เฟส 1–2 อยู่ใน `HRM.sln` เดิม (ProjectReference) · แยก repo + local NuGet feed ตอนเริ่มเฟส 3 |
| 3 | ฐานข้อมูลของ Lite | เริ่มด้วย SQL Server เหมือน HumanOk (โค้ด/trigger/migration ใช้ต่อได้ทันที) · Postgres เป็นเฟสถัดไปเมื่อ trigger ย้ายเข้าโค้ดแล้ว |
| 4 | tenant ของ SaaS | ฐานข้อมูลเดียว + `TenantId` ทุกตาราง (ถูก/ง่าย) · ลูกค้าที่ต้องการแยกฐาน = ติดตั้งเดี่ยวราคาอีกระดับ |
| 5 | ทะเบียนพนักงานใน HumanOk | `Hremployee` ยังเป็นต้นทาง · Payroll ซิงก์เข้า `pay_employee` ก่อนคำนวณทุกครั้ง (ไม่แก้สองที่) |
| 6 | ทำอะไรก่อนระหว่างเฟส 1 (Workflow) กับเฟส 2 (Payroll) | **Workflow ก่อน** — เล็กกว่า พึ่งพาน้อยกว่า และ Payroll ต้องใช้มัน (คำขอกองทุน) |
| 7 | e-Filing/format ธนาคาร | ทำในเฟส 3 (ก่อนขาย Lite) — วันนี้ยังไม่มีทั้งใน HumanOk ควรทำครั้งเดียวใน Payroll.Reports ให้ทั้งสองผลิตภัณฑ์ใช้ |

---

## 7. ความเสี่ยงและวิธีกัน

- **แยกแล้วเลขเปลี่ยน** — เทสทั้งปี 2568 คือตัวกัน: ผลรวมภาษี/ประกันสังคม/กองทุน/ไฟล์ธนาคาร/GL ต้องเท่าเดิมทุกข้อหลังย้ายทุกเฟส
- **สอง engine ของ workflow** — ปลดของเก่าในเฟส 0 ก่อน ไม่ยกไปด้วย
- **หน้า Blazor ใน Razor Class Library** — MudBlazor/Radzen/localization (`JsonLocalizationService`) ต้องส่งผ่าน DI ของ host; ลองกับ Workflow (28 หน้า) ก่อนจึงรู้แนวทางสำหรับ Payroll (41 หน้า)
- **สิทธิ์รายหน้า (AD.CRUDManage) และเมนู** — เป็นของ host; package ประกาศ route + สิทธิ์ที่ต้องการ ให้ host seed (`ProgramRoleService` สแกน route อยู่แล้ว ใช้ต่อได้)
- **Identity/SSO ยังไม่มี Advance.Auth** — Lite เริ่มด้วย ASP.NET Identity ชุดของตัวเอง (โครงเดียวกับ HumanOk หลัง migrate) แล้วย้ายเข้า Advance.Auth เมื่อมี ไม่รอ
- **โฟลเดอร์สำเนา** (`HRM - Copy (2)` ฯลฯ) — ห้าม copy repo มาเป็น Payroll; แยกด้วย project/branch/tag ตามสถาปัตยกรรมกลาง

---

## 8. ก้าวแรกที่ทำได้ทันที (ไม่ต้องรอตัดสินใจข้อ 6)

1. ปิดเทสเงินเดือนทั้งปี 2568 ให้เขียว (กำลังทำ)
2. เขียน interface 7 ตัว + เทสสถาปัตยกรรมกันสายพัน (เฟส 0 ข้อ 2 และ 4)
3. ปลด `WorkflowEngineService` เก่าให้เหลือ facade (เฟส 0 ข้อ 3)
