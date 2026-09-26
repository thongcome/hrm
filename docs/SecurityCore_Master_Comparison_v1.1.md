# HRM ↔ Advance.SecurityCore (ต้นฉบับหลัก ADP.AI) — เทียบและแผนย้าย · ระยะ 0

**ฉบับ v1.1 · 26 ก.ย. 2569** (v1.0 = ผลเทียบก่อน CEO ตัดสิน · v1.1 เพิ่ม "คำตัดสินและสิ่งที่ทำแล้ว" ด้านล่าง เนื้อหาเทียบเดิมไม่เปลี่ยน) · งานจาก CEO (เซสชัน ADP AI): "ใช้ของ ADP.AI เป็นตัวหลัก ส่งงานให้ HRM กับ Payroll"

| | |
|---|---|
| ฝั่ง HRM | `D:\GitWorkspace\HRM` · branch `develop` HEAD **`a785885`** (ตรวจแล้ว: ไม่มี commit ใหม่หลังจุดนี้ ไม่มี branch ค้างที่แตะไฟล์ security) |
| ต้นฉบับหลัก | `D:\GitWorkspace\ADplateformAI\adagenthub\src\Advance.SecurityCore` · github.com/thongcome/ADP `main` **`2b4abb5`** · ทะเบียนข้อเบี่ยง `adagenthub/MODULES.md` |
| ขอบเขต v1.0 | อ่านอย่างเดียวทั้งสองฝั่ง · ไม่ได้เปลี่ยน ProjectReference · ไม่ได้ลบสำเนาใด |
| ขอบเขต v1.1 | CEO สั่ง "ทำเลย" (26 ก.ย. 2569) — แก้บั๊ก 5 ข้อใน HRM + D3 ฝั่งการสลับบริษัท · ไม่ได้แก้ไฟล์ใน `ADplateformAI` (แจ้งเซสชัน ADP AI ให้เริ่มขั้น 1 แล้ว) |

---

## คำตัดสินของ CEO และสิ่งที่ทำแล้ว (v1.1)

CEO ตอบรายงาน v1.0 ว่า "ครับ ทำเลยครับ" แล้วเลือกทั้ง 4 ข้อ: แจ้ง ADP AI เริ่มขั้น 1 · แก้ `Abcd@2025` · แก้บั๊ก HRM ครบ 5 ข้อ · รับทุกข้อแนะนำ D1–D8

| # | คำตัดสิน | สถานะ |
|---|---|---|
| D1 | แจกไลบรารีเป็น **NuGet ส่วนตัว** | งานของ ADP.AI — แจ้งแล้ว |
| D2 | **ทาง ค**: HRM ใช้หน้า MudBlazor ของตัวเองระหว่างย้าย แล้วค่อยเปลี่ยนเป็นหน้าของไลบรารี | จึงแก้บั๊กในหน้า MudBlazor ของ HRM รอบนี้ |
| D3 | role ที่ไม่มีขอบเขต **ไม่เห็นทุกบริษัทอีกต่อไป** — Admin ยังเห็นทุกบริษัท | ✅ ฝั่ง HRM: `CompanySwitchService` (ด้านล่าง) · ความหมายระดับไลบรารี (ตัวกรองแถวข้อมูล) = งานของ ADP.AI |
| D4 | **แปลงเวลาเก่าเป็น UTC ครั้งเดียว** (ทาง ก) + เปลี่ยนตัวเขียน `AuditLog` ของ HRM เป็น UTC ในรอบเดียวกัน | รอขั้น 1 ของ ADP.AI เสร็จก่อน แล้วซ้อมบนสำเนาฐาน (ข้อ 3.4) — **ยังไม่แตะฐานข้อมูล** |
| D5 | แก้บั๊ก HRM ทั้ง 5 ข้อเลย | ✅ ทำแล้ว (ด้านล่าง) |
| D6 | คงชื่อผู้ใช้ `@local.humanok` | ไม่ต้องแก้ HRM · ADP.AI ต้องเพิ่ม option (ข้อ 1.2 #5) |
| D7 | ไลบรารีเป็นเจ้าของตาราง `AuditLog` | ต้องได้ข้อ 1.2 #1 + #9 ก่อน |
| D8 | อนุมัติรายการยกเข้าข้อ 1.2 และให้ทำ #1, #2 ก่อน | แจ้ง ADP AI แล้ว |

### แก้ใน HRM รอบนี้

| บั๊ก | แก้อย่างไร | ไฟล์ |
|---|---|---|
| (1) cache สิทธิ์รายหน้า | แถวสิทธิ์กับแผนที่ชื่อ role อยู่ใน cache **ก้อนเดียว** (เดิมสองก้อน `…all` / `…all.rolemap` ที่หลุดจังหวะกันได้) · ส่วน "คีย์ใส่ tenant" ของต้นฉบับหลัก **ไม่เกี่ยวกับ HRM** เพราะ HRM เป็นลูกค้ารายเดียวต่อฐาน และสิทธิ์รายหน้าผูกกับ role ไม่ใช่บริษัท | `Services/Security/ProgramRoleService.cs` |
| (2) role ชื่อซ้ำ → ทุกหน้าโยน exception | ชื่อ role ชี้ไปที่ **ทุก** id ที่ชื่อตรง (ไม่สนตัวพิมพ์) แทน `ToDictionaryAsync` | `ProgramRoleService.cs` + เทสต์ `ProgramRoleAccessTests` 2 เคส |
| (3) รหัสเริ่มต้น/รีเซ็ต `Abcd@2025` | สุ่มรหัสชั่วคราวใหม่ทุกครั้ง (12 ตัว ผ่านกฎ Identity) แสดงให้ผู้ดูแลครั้งเดียว ไม่เก็บตัวอักษรจริง · รีเซ็ตที่ตั้งรหัส Identity ไม่สำเร็จจะแจ้ง error แทนการบอกว่าสำเร็จ · หน้าระบบเดิม `/sc_users/create` ก็ใช้ค่าเดียวกัน แก้ด้วย | `Services/Security/TemporaryPassword.cs` (ยกจากต้นฉบับหลัก) · `Components/Pages/Admin/Access/UserAdmin.razor` · `Components/Pages/sc_userPages/Create.razor` |
| (4) ไม่มีด่านที่สอง | เพิ่ม `GetRightsAsync` (ซ่อนปุ่ม + ข้อความ "หน้านี้ยังไม่ได้เปิดสิทธิ์แก้ไขให้บทบาทของคุณ") และ `RequireAsync` ต้น handler เขียนทุกตัว: เพิ่ม/ลบขอบเขต (Create/Delete) · ยกเลิกเซสชัน (Edit — ตามต้นฉบับหลัก) | `RoleScopeAdmin.razor` · `UserSessionAdmin.razor` |
| (5) ติ๊กค้างเมื่อถูกปฏิเสธ | ช่องติ๊กมี `@key` ที่มีเลขรุ่น · ถูกปฏิเสธ/ไม่พบแถว = เพิ่มเลขรุ่น → Blazor สร้างช่องใหม่จากค่าจริง | `ProgramRightsAdmin.razor` |
| D3 | สลับบริษัทได้เฉพาะ: **Admin → ทุกบริษัท** · คนอื่น → บริษัทที่ได้จากขอบเขตประเภท "บริษัท" (`scope_company`) + **บริษัทของตัวเอง** + บริษัทที่อยู่ตอนนี้ · role ที่ไม่มีขอบเขต = บริษัทตัวเองเท่านั้น · บริษัทของตัวเองคำนวณจากบัญชี (`PayrollCompanyResolver`) จึงสลับกลับได้เสมอ · endpoint ตรวจซ้ำฝั่ง server ด้วยกติกาเดียวกันอยู่แล้ว · **ตัวกรองแถวข้อมูล (`RoleScopeSnapshot.AllowsEmployee`) ไม่เปลี่ยน** | `Services/Security/CompanySwitchService.cs` · เทสต์ `CompanySwitchRuleTests` 4 เคส · ข้อความอธิบายใน `RoleScopeAdmin.razor` |

### 🔴 เจอระหว่างแก้ — ยังไม่แก้ รอ CEO

หน้าระบบเดิม `/sc_users/create`, `/sc_users/edit`, `/sc_users/delete`, `/sc_users/details` (`Components/Pages/sc_userPages/`) ติดแค่ `[Authorize]` — **ใครล็อกอินได้ก็เปิดได้ รวมพนักงาน ESS** (หน้า `/sc_users` ที่เป็นรายการติด `Menu:SYS_ADMIN` ถูกต้อง แต่อีก 4 หน้าไม่ได้) · `Delete.razor:237` ลบแถว `sc_user` จริง (hard delete) · `Edit.razor:295` `Attach` ทั้งแถวจากฟอร์ม (แก้ `loginname`/`company_id`/`isdisable` ของใครก็ได้) · มาจาก `a6a84278` (29 ส.ค.) ที่ใส่ `[Authorize]` ให้หน้า scaffold 195 หน้าที่ไม่มีเลย แต่ไม่ได้ใส่ policy · ทางแก้ที่เสนอ: ใส่ `[Authorize(Policy = "Menu:SYS_ADMIN")]` ให้ 4 หน้าเหมือนหน้ารายการ (ไม่ลบหน้า ตามกติกา "ยังไม่ลบจนขึ้น production") และควรไล่ตรวจหน้า scaffold อื่นจากชุด 195 หน้าที่ยังเขียนข้อมูลได้ด้วย

⚠️ ผลของ D3 ต่อผู้ใช้: ตอนนี้ `sc_role_scope` มี 0 แถว ⇒ หลัง deploy **เฉพาะบทบาท Admin** ที่สลับบริษัทได้ ผู้ใช้ HR ของบริษัทแม่ที่ต้องดูบริษัทลูก ต้องให้ผู้ดูแลตั้งขอบเขตประเภท "บริษัท" ที่ `/admin/system/role-scopes` ก่อน
| วิธีเทียบ | `diff` ทีละไฟล์ (ตัด comment ออกแล้วเทียบเฉพาะโค้ด) ทุกคู่ service/endpoint/middleware · อ่านหน้าจัดการสิทธิ์ 7 คู่ครบทุกบรรทัด · นับข้อมูลจริงในฐาน `hrm` (SQL Server เครื่อง dev) แบบอ่านอย่างเดียว ไม่ดึงค่ารหัสผ่านใด ๆ |

---

## สรุปสำหรับ CEO (อ่านหน้าเดียวจบ)

1. **ของใหม่หลัง `954c500`/`a785885` ไม่มี** — HRM หยุดที่ `a785885` ซึ่ง ADP.AI ยกไปแล้ว (เมนูไม่จำกัดชั้น + `MenuTreeAdmin` หาลูกด้วย `uppermenucode` อย่างเดียว)
2. **แต่ที่ `954c500` เองยังมีของที่ต้นฉบับหลักยกไปไม่ครบ** เพราะมันอยู่ใน `HRMContext` ไม่ได้อยู่ในไฟล์ security:
   - 🔴 **บันทึก audit อัตโนมัติทุกการเขียน + ปิดบังรหัสผ่าน/เลขบัตร** (`Model/HRMContext.Audit.cs`) — ต้นฉบับหลักไม่มี ⇒ ถ้าย้ายวันนี้ การให้/ถอนสิทธิ์ รีเซ็ตรหัส ปลดล็อก ถอนเซสชัน **จะไม่มีร่องรอยเลย** (ขัด พ.ร.บ.คอมพิวเตอร์ ≥90 วัน)
   - 🔴 **เพิ่ม `permversion` อัตโนมัติเมื่อสิทธิ์เปลี่ยน** (`Model/HRMContext.PermVersion.cs`) — ต้นฉบับหลักมีตัวเทียบ แต่ไม่มีตัวเพิ่ม ⇒ ถอนสิทธิ์แล้วยังใช้ได้จนกว่าจะล็อกอินใหม่
   - 🟠 หน้า 2FA (กรอกรหัส / ตั้งแอป + QR) — ต้นฉบับหลักพาไปหน้าที่ตัวเองไม่มี
   - 🟠 Security headers (CSP ฯลฯ) · ชื่อผู้ใช้ Identity ของ HRM คือ `@local.humanok` ไม่ใช่ `@local.advance` อย่างที่ MODULES.md เขียน
3. **ช่องเสียบของต้นฉบับหลักรองรับของเฉพาะ HRM ได้เกือบทั้งหมด** — ที่ยังเสียบไม่ได้มี 6 ข้อ (ข้อ 2.3) ต้องให้ ADP.AI เพิ่มช่องก่อน
4. **ฐานข้อมูล: ย้ายได้โดยไม่รีเซ็ตรหัสใคร** — Identity ใช้ตาราง `AspNetUsers` เดิมทั้ง 26 แถว (hash แบบ Identity v3 ทั้งหมด) ไม่ต้องย้ายข้อมูลเลย แค่เพิ่มคอลัมน์ `TenantId` + ดัชนี · **เรื่องใหญ่คือเวลา**: HRM เก็บเวลาท้องถิ่น ต้นฉบับหลักอ่านเป็น UTC ⇒ เวลาเลื่อน 7 ชั่วโมง รวมถึง `AuditLog` 127,380 แถวที่ทุกโมดูลของ HRM เขียนร่วม
5. **ต้องให้ CEO ตัดสิน 8 เรื่อง** (ข้อ 4.2) — ที่สำคัญที่สุดคือ "role ไม่มีขอบเขตข้อมูล = เห็นทุกบริษัท": ตอนนี้ **role ที่ใช้งาน 16 ตัว ไม่มีขอบเขตสักตัว** (`sc_role_scope` = 0 แถว) ⇒ ทุกคนที่สลับบริษัทได้ เห็นได้ทุกบริษัท

---

## ข้อ 1 — ของที่ HRM มี แต่ต้นฉบับหลักไม่มี (รายการให้ ADP.AI ยกเข้า)

### 1.1 หลัง `954c500` / `a785885`

| commit | ไฟล์ | สถานะในต้นฉบับหลัก |
|---|---|---|
| `a785885` 26 ก.ย. | `Services/Security/ScMenuNavSeeder.cs` (ชั้นเมนูไล่ `ParentGroupCode` + กันวน) | ✅ ยกแล้ว (`ScMenuNavSeeder` + `MenuTree`) |
| `a785885` | `Components/Pages/Admin/Access/MenuTreeAdmin.razor` (หาลูกด้วย `uppermenucode` อย่างเดียว) | ✅ ยกแล้ว |
| `a785885` | `Components/Layout/DbNavMenu.razor` (วาดเมนู recursive) | ของโฮสต์ (MudBlazor) — SampleHost มี `DbNavMenu` ของตัวเองแล้ว ไม่ต้องยก |
| `a785885` | `Services/Dev/UiTestPayrollFixture.cs` (บัญชี uitest.*) | ข้อมูลทดสอบของ HRM — ไม่เกี่ยว |

`advance-solution-export` (12 ก.ย.) เป็น branch แยกโปรเจกต์รุ่นเก่า ไม่มีของใหม่ด้าน security · ไม่มีงานค้างที่ยังไม่ commit ใน worktree ใดที่แตะไฟล์ security

### 1.2 ของที่มีอยู่แล้วที่ `954c500` แต่ต้นฉบับหลักยังไม่มี — **เรียงตามความสำคัญ**

| # | ความสำคัญ | ของ | ไฟล์ HRM | commit | ทำไมต้องยก | ทั่วไป / เฉพาะ HRM |
|---|---|---|---|---|---|---|
| 1 | 🔴 | **Audit อัตโนมัติทุก Create/Update/Delete + ปิดบังค่าอ่อนไหว** (`password`,`pwd`,`securitystamp`,`idcard`,`citizenid`,`nationalid`,`passportno`,`salary`,`bankaccount`,`accountno`,`accountnumber` → `***MASKED***`) · ไม่ audit ตาราง audit เอง · RecordId จาก PK metadata | `Model/HRMContext.Audit.cs` | `7b797dc` (สร้าง) · `f409027` (ไม่ audit ตัวเอง) · `37ff677` (A09 masking) · `73116fe`,`8a2340a`,`2d18502` (ปรับตามกติกา id/code) | `SecurityDbContext.SaveChangesAsync` มีแค่ `StampTenant()` · `AuditLogger.cs` ของต้นฉบับหลักเขียนเองว่า "NOT copied" และ "AuditMasking.cs ไม่มีใน HRM" — **ถูกครึ่งเดียว: masking อยู่ใน `HRMContext.Audit.cs` ไม่ได้แยกไฟล์** · ผลถ้าไม่ยก: หน้าจัดการสิทธิ์ทั้ง 7 หน้าไม่ทิ้งร่องรอย (แถว audit เดียวที่ต้นฉบับหลักเขียนคือตอนเปลี่ยนชื่อเข้าระบบ) | ทั่วไป — ยกเข้า `SecurityDbContext` |
| 2 | 🔴 | **เพิ่ม `sc_user.permversion` อัตโนมัติ** เมื่อ `sc_user_role` / `sc_role_menu` / `sc_role_scope` เปลี่ยน → `IdentityRevalidatingAuthenticationStateProvider` เตะเซสชันออกภายใน ~5 นาที | `Model/HRMContext.PermVersion.cs` | `727e849` | ต้นฉบับหลักมี claim `permversion` + ตัวเทียบ แต่ **ไม่มีอะไรเพิ่มค่า** ⇒ ถอนสิทธิ์ไม่มีผลจนล็อกอินใหม่ | ทั่วไป — ยกเข้า `SecurityDbContext` |
| 3 | 🟠 | **หน้า 2FA ของ Identity**: `LoginWith2fa`, `LoginWithRecoveryCode`, `Manage/EnableAuthenticator` (**QR จริงด้วย QRCoder** แทนลิงก์ "learn how…" ของ scaffold), `TwoFactorAuthentication`, `ResetAuthenticator`, `GenerateRecoveryCodes`, `Disable2fa` | `Components/Account/Pages/**` | `47ecd17` (QR + ทางล็อกอิน 2FA) | ต้นฉบับหลัก `LoginEndpoints` พาไป `/Account/LoginWith2fa` และ `RequireMfaMiddleware` พาไป `/Account/Manage/EnableAuthenticator` แต่ **ไลบรารีไม่มีสองหน้านี้** ⇒ โฮสต์ที่ไม่ได้ scaffold Identity เองจะ 404 ทันทีที่ผู้ใช้เปิด 2FA / เปิด `RequireMfaForAdmin` · HRM ไม่เดือดร้อน (มีหน้าเอง) แต่ระบบอื่นเดือดร้อน | ทั่วไป — ยกเป็นหน้า AD.Theme หรือเขียนใน MODULES.md ว่าโฮสต์ต้องมี |
| 4 | 🟠 | **Security headers** 5 ตัว (CSP · X-Frame-Options · nosniff · Referrer-Policy · Permissions-Policy) | `Middleware/SecurityHeadersMiddleware.cs` | ก่อน 8 ก.ย. (OWASP 1 ก.ย.) | baseline ของ Advance บังคับทุกระบบ · CSP ต้องตั้งได้ (HRM เปิด Google Fonts + `unsafe-inline` เพราะ MudBlazor) | ทั่วไป — ยกเป็น middleware + option CSP |
| 5 | 🟠 | **ชื่อผู้ใช้ Identity แบบสังเคราะห์ของ HRM = `{login}@local.humanok`** | `Services/Login/UserProvisioningService.cs`, `Components/Pages/Pay/PayrollEmployeeAdmin.razor:609`, `Services/Dev/DevAuthSeeder.cs` | — | ต้นฉบับหลัก `UserProvisioningService.IdentityUserName` ใช้ `@local.advance` และคอมเมนต์เขียนว่า "รูปแบบเดิมของ HRM ทุกตัวอักษร" — **ไม่จริง** ในฐาน dev มี 20/26 แถวเป็น `@local.humanok` · ล็อกอินไม่พัง (หาด้วย `userid`) แต่บัญชีใหม่กับเก่าจะคนละรูปแบบ และ `DevAuthSeeder` หาด้วยอีเมล `admin@local.humanok` | ทั่วไป — เพิ่ม `SecurityCoreOptions.SyntheticUserNameDomain` (HRM ตั้ง `local.humanok`) + แก้ข้อความใน MODULES.md |
| 6 | 🟡 | **บทบาทเริ่มต้นตอนสร้างบัญชี** — ตามประเภทพนักงาน (`sc_role.employeetype_code`) ไม่มีก็ใช้ role `Employee` | `UserProvisioningService.EnsureDefaultRoleAsync` | `1654689` · ปรับเป็น id ใน `8a2340a` | ต้นฉบับหลักตัดทิ้ง (ถูก — เป็นตรรกะ HR) แต่ **ไม่มีช่องเสียบแทน** ⇒ HRM เสียพฤติกรรมนี้ | ช่องเสียบใหม่ เช่น `IDefaultRoleResolver` (HRM เขียนตัวจริง) |
| 7 | 🟡 | **ผูกบัญชีกับพนักงานด้วย id** (`sc_user.hremployee_id`, FK) · ใส่รหัสอย่างเดียวจะถูกแปลงเป็น id ภายในบริษัท รหัสไม่พบ/กำกวม = ปฏิเสธ · `sc_user_role.empid` ตามให้ | `Model/HRMContext.UserEmployee.cs`, `Model/sc_user.Employee.cs`, `UserAdmin.razor` | `8a2340a` | `IUserEmployeeLookup` / `LookupItem(Code, Label)` มีแค่รหัส ⇒ ขัดกติกา id/code (CEO 21 ก.ย.) และบันทึกรหัสพิมพ์ผิด/ข้ามบริษัทได้เงียบ ๆ | ปรับช่องเสียบ: `LookupItem` มี id + hook ตรวจก่อนบันทึก (โฮสต์เป็นเจ้าของคอลัมน์ id) |
| 8 | 🟡 | **ลืมรหัสผ่าน / ตั้งรหัสใหม่ด้วยตัวเอง** (หน้า + endpoint + rate limit `forgot-password` + CSRF) | `Endpoints/ForgotPasswordEndpoints.cs`, `Components/Login/{ForgotPassword,ResetPassword}.razor` | `37ff677` (CSRF) · `8a2340a` | ต้นฉบับหลักเลื่อนไว้ "ต้องมีตัวส่งอีเมลจริงก่อน" — แต่ช่อง `IEmailSender<ApplicationUser>` มีอยู่แล้ว (`TryAdd` NoOp) โฮสต์ที่มีตัวส่งจริงเสียบได้เลย | ทั่วไป — ยกได้ ปิดเป็นค่าตั้งต้นเมื่อยังเป็น NoOp |
| 9 | 🟡 | **เก็บ AuditLog เก่าเป็นไฟล์** (ไม่ลบก่อน 90 วัน) | `Services/Audit/AuditArchiveService.cs`, `AuditArchiveOptions.cs` | `f409027` | ต้นฉบับหลักมีตาราง `AuditLog` ของตัวเองแต่ไม่มีการเก็บถาวร | ทั่วไป — ถ้าไลบรารีเป็นเจ้าของ `AuditLog` (ดู D7) |
| 10 | 🟡 | **ข้อความแจ้งเตือนความปลอดภัยเป็นภาษาไทย + ชื่อระบบ `[HumanOk Security]` + เวลาไทย** | `Services/Security/SecurityAlertService.cs` | `47ecd17`,`e9352bf` | ต้นฉบับหลักเปลี่ยนเป็นอังกฤษ + `[Advance.SecurityCore]` + UTC ตายตัว | ทั่วไป — ส่งผ่าน `ISecurityCoreText` + option ชื่อระบบ/เขตเวลา |
| 11 | 🟡 | **ข้อความหน้าล็อกอิน 4 ภาษา** (th/en/ja/zh — `JsonLocalizationService`) | `wwwroot/Resources/*.json` | `a2c8db7` | `i18n/` ของต้นฉบับหลักมี th/en เท่านั้น — HRM เสียบ `ISecurityCoreText` ของตัวเองได้ แต่ระบบอื่นที่ต้องการ ja/zh ไม่มี | ทั่วไป — เพิ่ม `ja.json`/`zh.json` |

**ทั้งสองฝั่งขาดเหมือนกัน (ไม่ใช่ของ HRM แต่ควรทำที่ต้นฉบับหลักครั้งเดียว):** หน้า `UserAdmin` (อีเมล/โทรศัพท์/ผูกพนักงาน) และ `UserSessionAdmin` (IP/UserAgent) แสดงข้อมูลส่วนบุคคล แต่ **ไม่มีป้าย PDPA และไม่เรียก `LogAccessAsync`** ทั้ง HRM และต้นฉบับหลัก

### 1.3 ตรวจแล้ว "ตรงกัน" (ไม่ต้องทำอะไร)

`PasswordPolicyService` · `PasswordPolicyOptions` · `ForcePasswordChangeMiddleware` (allowlist ตรงทุกตัว) · `RequireMfaMiddleware` · `PasswordPolicyEndpoints` · `RoleScopeSnapshot` · `ScProgramRouteSeeder` · `IdentityRevalidatingAuthenticationStateProvider` (ถอนเซสชัน + permversion) · `LoginEndpoints` (CSRF `37ff677`, alert `47ecd17`/`e9352bf`, 2FA redirect, ล็อก/ปลดล็อก mirror `invalidpwcount`, AD ต้องเรียก `AccessFailedAsync` เอง) · `ProgramRoleService.SeedAsync` (Admin ได้ 4 สิทธิ์ ที่เหลืออ่านอย่างเดียว) · `ScUserClaimsPrincipalFactory` ส่วน menu/menu_edit/Admin เห็นทุกเมนู/scope claims · cookie 60 นาที Strict/HttpOnly/Secure · Identity password rules · rate limit `login` 5 ครั้ง/นาที — **ต่างกันเฉพาะ `DateTime.Now`→`UtcNow`, tenant และชื่อ namespace**

ข้อต่างเล็กที่ตั้งใจ (ไม่ต้องยก): ต้นฉบับหลัก `Trim()` ชื่อเข้าระบบ · ไม่ยก claim `program`/`sc_role_program` (0 หน้าใช้ — ส่วนนี้ใน `PermissionAdmin` ของ HRM เป็นโค้ดตายจริง) · ไม่ยก SSO

### 1.4 ของที่ต้นฉบับหลักแก้แล้วแต่ HRM ยังมีบั๊ก — **v1.1: แก้ใน HRM แล้วทั้ง 5 ข้อ** (ดู "คำตัดสินของ CEO และสิ่งที่ทำแล้ว")

5 ข้อที่ HRM ยืนยันแล้ว: (1) cache สิทธิ์รายหน้าคีย์เดียว (HRM รายเดียวต่อฐาน จึงยังไม่กระทบ) (2) role ชื่อซ้ำ → `ToDictionaryAsync` โยน exception ทุกหน้า (ฐาน dev ตอนนี้ **0 ชื่อซ้ำ**) (3) รหัสเริ่มต้น/รีเซ็ต `Abcd@2025` ตายตัว (`UserAdmin.razor` 4 จุด) (4) `RoleScopeAdmin`/`UserSessionAdmin` ไม่มีด่าน `RequireAsync` (5) `ProgramRightsAdmin` ติ๊กค้างเมื่อถูกปฏิเสธ

ข้อสังเกตเพิ่ม (ยังไม่ได้ยืนยันด้วยการรันจริง): `UserAdmin` สร้างบัญชีแล้วเรียก `SyncRolesAsync` ซึ่งอาจปิด role อัตโนมัติจากข้อ 1.2 #6 ถ้าผู้ดูแลไม่ได้ติ๊กไว้ — ควรทดสอบก่อนนับเป็นบั๊กข้อที่ 6

---

## ข้อ 2 — ของเฉพาะ HRM ↔ ช่องเสียบของต้นฉบับหลัก

### 2.1 จับคู่

| ของ HRM | เสียบที่ | ตัวที่ HRM ต้องเขียน | หมายเหตุ |
|---|---|---|---|
| claim `empno` · `hremployee_id` (`EssEmployeeResolver.EmployeeIdClaim`) · `payroll_company` (`PayrollCompanyResolver`, ใช้ `sc_user.company_id` → `com_company.code`) | `IHostClaimsEnricher` | `HrmClaimsEnricher` | 163 ไฟล์อ่าน `payroll_company` — ชื่อ claim ต้องคงเดิมทุกตัวอักษร |
| ESS: พนักงานทุกคนได้เมนู `ESS_ACCESS`,`LEAVE_ACCESS`,`EXP_ACCESS`,`HR_ANNOUNCE_ACCESS` (ตั้งทับที่ `SelfService:EmployeeMenuCodes`) · MSS: หัวหน้าตามผังองค์กร → claim `headed_org` + เมนู MSS | `IHostClaimsEnricher` (เพิ่ม `Claim("menu", …)` เอง) | ใน `HrmClaimsEnricher` | ใช้ได้เพราะ `MenuAuthorization` ดูแค่ `HasClaim("menu", x)` · ต่างจากเดิมตรงที่ enricher รันหลังขั้นรวมรหัสเมนู ⇒ enricher ต้องกันซ้ำเอง |
| ช่องผูกพนักงานใน `UserAdmin` (ค้น EmpNo/ชื่อ/นามสกุล 20 แถว · เติมชื่อตอนสร้าง · non-Admin ต้องผูก) | `IUserEmployeeLookup` | `HrmUserEmployeeLookup` | **เสียบได้ไม่ครบ** — ต้องมี id (ข้อ 1.2 #7) |
| ตัวเลือก "ได้บทบาทอัตโนมัติ" ใน `RoleAdmin` | `IRoleAutoAssignOptions` | `HrmRoleAutoAssignOptions` | HRM อ่าน `employeetypes` เดิม แต่ตั้งแต่ `2d18502` ความจริงอยู่ที่ `Pos_EmployeeType` (id) — ตัดสินตอนเขียน plug-in |
| ตัวเลือกขอบเขตข้อมูล (บริษัท = `Hremployee.companyid` distinct + ชื่อจาก `com_company` · หน่วยงาน/สาขา = `com_organization` เก็บ `orgcodefull` · ศูนย์ต้นทุน = `Com_ChartOfAccount.IsCostCenter`) | `IScopeValueSource` | `HrmScopeValueSource` | ค่าที่เก็บเป็นรหัส (ของเดิม) |
| `ScMenuNavCatalog` (413 บรรทัด, 13 กลุ่มบนสุด AD.Menu) | `IMenuNavContributor` | `HrmMenuNavContributor` (ห่อ catalog เดิม) | record shape เดียวกัน · `menugroupid` ต้นฉบับหลักหาเอง (HRM = 1) |
| `Services/Auth/LdapAuthService` (`TryBindAsync` คืน result `.Succeeded`) | `ILdapAuthenticator` (คืน `bool`) | adapter บรรทัดเดียว | ฐาน dev มีบัญชี AD 0 บัญชี |
| `JsonLocalizationService` (`L.Translate`, 4 ภาษา) | `ISecurityCoreText` | `HrmSecurityCoreText` | คีย์ชุดเดียวกัน (ต้นฉบับหลักยกจาก `a2c8db7`) |
| SQL Server | `SecurityCoreOptions.ConfigureDbContext = (o, cs) => o.UseSqlServer(cs)` | — | HRM อ้าง `Microsoft.EntityFrameworkCore.SqlServer` อยู่แล้ว · migration ต้องเขียนเอง (ข้อ 3) |
| ลูกค้ารายเดียวต่อฐาน | `FixedSecurityTenantProvider` · `TenantId = SecurityTenant.Single` (`Guid.Empty`) | — | `LoginNamesUniqueAcrossTenants = false` |
| หน้าล็อกอิน `/login` | `LoginPagePath = "/login"` | — | HRM มีหน้าเอง ไม่ต้องใช้ `/Account/Login` ของไลบรารี |
| ภาษาตามผู้ใช้ (`LanguageSettingsAdmin`) | `UiLanguage = null` | — | ตาม RequestLocalization ของ HRM |
| `@page` routes สำหรับ AD.CRUDManage | `RouteAssemblies.Add(typeof(HRM.Components.App).Assembly)` | — | |
| appsettings `PasswordPolicy` | ชื่อ section เดียวกัน (`PasswordPolicyOptions.SectionName`) | — | `MaxAgeDays: 0` คงเดิม |
| `SecurityAlerts` (+ `RequireMfaForAdmin`) | ชื่อ section เดียวกัน | — | |
| `MainLayout` (MudBlazor) | `HostLayoutType` | — | ใช้เฉพาะถ้า HRM เลือกหน้าของไลบรารี (D2) |

### 2.2 ของ HRM ที่อยู่ต่อใน HRM (ไม่ใช่งานของไลบรารี)

`CompanySwitchService` + `CompanySwitchEndpoints` (อ่าน `RoleScopeSnapshot` ของไลบรารี) · `ScopedDbContextExtensions` + query filter ใน `HRMContext.Security.cs` (4 หน้าใช้) · `DerivedRoleSyncService` / `EmployeeTypeRoleSeeder` / `PositionRoleSeeder` (เขียน `sc_user_role` จากข้อมูล HR) · `AuditArchiveService` (ถ้า D7 = HRM เป็นเจ้าของ AuditLog) · OpenIddict IdP (`OidcEndpoints`, cert) · SSO ขาออก (`ExternalSsoEndpoints`, `SsoRoleMappingAdmin`) · JWT `ExternalApi` + `ExternalApiCallerRequirement` · `CompanyAdmin`/`CompanyDetail`/`DocTypeAdmin`/`LanguageSettingsAdmin` (ไม่ใช่ security) · `RedirectToLogin` (`2281751`) · `DbNavMenu` · หน้า Identity scaffold ทั้งชุด

### 2.3 เสียบไม่ได้ — รายงาน ไม่ได้แก้ต้นฉบับหลัก

| # | เรื่อง | ทำไมเสียบไม่ได้ | ขอให้ ADP.AI |
|---|---|---|---|
| X1 | audit อัตโนมัติ + masking ของการเขียน sc_* | อยู่ใน `SaveChangesAsync` ของ context ไลบรารี โฮสต์แทรกไม่ได้ | ข้อ 1.2 #1 |
| X2 | `permversion` bump | เหมือน X1 | ข้อ 1.2 #2 |
| X3 | บทบาทเริ่มต้นตอนสร้างบัญชี | ไม่มีช่อง | `IDefaultRoleResolver` |
| X4 | ผูกพนักงานด้วย id + ตรวจก่อนบันทึก | `LookupItem` ไม่มี id · ไม่มี hook ก่อนบันทึก | ข้อ 1.2 #7 |
| X5 | ชื่อผู้ใช้ `@local.humanok` | ค่าคงที่ในโค้ด | option ข้อ 1.2 #5 |
| X6 | เวลาท้องถิ่นในตารางเดิม | ตัวแปลง UTC ใส่ใน `OnModelCreating` ทุกคอลัมน์ `DateTime` โดยไม่มีทางปิด · `ConfigureDbContext` แทรกไม่ถึง | ขึ้นกับ D4 — ถ้า CEO เลือกไม่แปลงข้อมูลเก่า ต้องมี option ปิด/ตั้งเขตเวลา |

**จุดชนเมื่อเรียก `AddAdvanceSecurityCore` ใน HRM (ไม่ใช่ข้อบกพร่องของไลบรารี แต่ต้องเอาของ HRM ออกพร้อมกัน):** `AddRateLimiter` ของ HRM ลงทะเบียน policy `"login"` ซ้ำกับไลบรารี (ชื่อซ้ำ = ล้มตอนสตาร์ต) · `AddIdentityCore<ApplicationUser>` ของ HRM ผูก `ApplicationDbContext` ส่วนไลบรารีผูก `SecurityDbContext` (ตารางเดียวกัน — ต้องเหลือตัวเดียว; `ApplicationDbContext` คงไว้เฉพาะตาราง OpenIddict) · `ConfigureApplicationCookie`, middleware สองตัว, `MapLoginEndpoints`/`MapPasswordPolicyEndpoints`, seeder 3 ตัว — ต้องถอดของ HRM ออกในรอบเดียวกับที่ใส่ของไลบรารี

### 2.4 หน้าจัดการสิทธิ์: ของไลบรารี (HTML + AD.Theme) หรือ MudBlazor ของ HRM — **ให้ CEO ตัดสิน (D2)**

| ทาง | ข้อดี | ข้อเสีย |
|---|---|---|
| **ก. ใช้หน้าของไลบรารี** (เส้นทางเดิม `/admin/system/*`, นโยบายเดิม `Menu:SYS_ADMIN`) | แก้ครั้งเดียวได้ทุกระบบ · ได้บั๊กที่แก้แล้วทั้ง 5 ข้อ + แบ่งหน้า + ราวกันตก Admin | หน้าตาต่างจากส่วนอื่นของ HRM (MudBlazor) · ต้องมี AD.Theme CSS ใน HRM (`<ScThemeLinks>`) |
| **ข. คงหน้า MudBlazor ของ HRM** | ผู้ใช้ไม่เห็นความเปลี่ยนแปลง | บั๊ก 5 ข้อยังอยู่จนกว่าจะแก้เอง · สองสำเนาแยกกันแก้ต่อ (ปัญหาเดิมที่ CEO ต้องการเลิก) |
| ค. ก่อนแล้วค่อยเลิก ข | เริ่มจาก ข ระหว่างย้ายข้อมูล แล้วสลับเป็น ก เมื่อเทสต์ผ่าน | สองช่วง |

ความเห็น: **ค** — ลดความเสี่ยงตอนย้ายข้อมูล แล้วจบที่สำเนาเดียว

---

## ข้อ 3 — ฐานข้อมูลเดิม (SQL Server)

### 3.1 ข้อเท็จจริงจากฐาน `hrm` เครื่อง dev (26 ก.ย. 2569, อ่านอย่างเดียว)

> ตัวเลขนี้เป็นของ **ฐาน dev เท่านั้น** — ฐานลูกค้าจริงแต่ละรายต้องรัน query ชุดเดียวกัน (ภาคผนวก ก) ก่อนย้าย

| ตาราง | แถว | | ตรวจ | ผล |
|---|---:|---|---|---:|
| `sc_user` | 7,010 | | loginname ซ้ำ (ตรงตัว) | 0 |
| `sc_role` | 17 (ใช้งาน 16) | | loginname ซ้ำ (ไม่สนตัวพิมพ์ + ตัดช่องว่าง) | 0 |
| `sc_user_role` | 7,175 | | loginname มีช่องว่างหัวท้าย | 0 |
| `sc_role_menu` | 184 | | role ใช้งานชื่อซ้ำ | 0 |
| `sc_role_scope` | **0** | | role ใช้งานที่ไม่มีขอบเขต | **16 / 16** |
| `sc_program_role` | 8,804 | | `sc_program_role` (roleid, progpath) ซ้ำ | 0 |
| `sc_user_session` | 308 (เปิดอยู่ 307) | | `sc_menu.menucode` ซ้ำ | 31 กลุ่ม (หนี้เดิม) |
| `Sso_ClientRoleMapping` | 1 | | `sc_user` ไม่มีบัญชี Identity | 6,993 (ชุด AUTOX ที่ไม่เคยล็อกอินได้) |
| `AuditLog` | 127,380 (เก่าสุด 16 วัน) | | `AspNetUsers` ที่ `userid` ชี้ไม่เจอ | 9 |
| `AspNetUsers` | 26 | | hash แบบ Identity v3 (`AQAAAA…`) / ว่าง | 26 / 0 |
| `sc_menu` / `sc_program` | 291 / 558 | | เปิด 2FA · บัญชี AD · `isforcechanged=1` · ตั้ง `pwdexpdate` | 0 · 0 · 0 · 0 |
| | | | `sc_user.password` (คอลัมน์เก่า) ไม่ว่าง | 4 แถว |
| | | | `company_id` ต่างกันใน `sc_user` | 4 |

ดัชนีปัจจุบันของ `sc_user`: `PK_sc_user_userid` · `UXC_sc_user_loginname` (ไม่ซ้ำ, จาก `20260921160000_UniqueMasterCodes`) · `IX_sc_user(userid, password, isEmployee)` · `IX_sc_user_company_id` · `IX_sc_user_hremployee_id` · FK ที่ชี้เข้า `sc_user`/`sc_role`/`sc_menu` = 13 ตัว

### 3.2 migration SQL Server ที่ HRM ต้องเขียนเอง (ตามกติกา repo: เขียนมือ · Designer แบบ attribute-only · `migrationBuilder.Sql`)

1. **`TenantId uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'`** บน 9 ตาราง: `sc_user`, `sc_role`, `sc_user_role`, `sc_role_menu`, `sc_role_scope`, `sc_program_role`, `sc_user_session`, `Sso_ClientRoleMapping`, `AuditLog` — **คง DEFAULT constraint ไว้ถาวร** เพราะ `HRMContext` (ทุกโมดูลเขียน `AuditLog`; `DerivedRoleSyncService`/`PayrollEmployeeAdmin` เขียน `sc_user_role`/`sc_user`) ไม่รู้จักคอลัมน์นี้ แถวใหม่จะได้ `Guid.Empty` = tenant เดียวของ HRM ถูกต้อง
2. **ดัชนี**: สร้าง `IX_sc_user_TenantId_loginname` (ไม่ซ้ำ) แล้วค่อยลบ `UXC_sc_user_loginname` · ปรับ `code-index-baseline`/`MasterCodes` ของ `SchemaConventionTests` ให้รู้จักดัชนีใหม่ · ดัชนี `(TenantId, …)` อีก 7 ตัวตาม `SecurityDbContext` (ชื่อให้ตรง snapshot ของต้นฉบับหลัก) · `IX_sc_user(userid,password,isEmployee)` — ต้นฉบับหลักตัดทิ้ง; มีคอลัมน์รหัสผ่านเก่าอยู่ในดัชนี ควรลบแต่ให้ CEO ยืนยัน (กติกา "ยังไม่ลบจนขึ้น production")
3. **ไม่มี `InsertData`/ไม่สร้างข้อมูลใด** — เพิ่มคอลัมน์และดัชนีเท่านั้น · ไม่แตะ `AspNetUsers`
4. เทียบโครงสร้างด้วยวิธีเดียวกับ `SchemaParityTests` ของต้นฉบับหลัก (`tools/securitycore_hrm_snapshot.py`) หลัง migration — ต้องตรงทุกคอลัมน์ยกเว้นข้อเบี่ยงที่ MODULES.md จดไว้

### 3.3 บัญชีและรหัสผ่าน — **ไม่รีเซ็ตใคร**

- ต้นฉบับหลักเป็น `IdentityDbContext<ApplicationUser>` บนตาราง `AspNetUsers` ชุดเดียวกัน `ApplicationUser` ช่องเดียวกัน (`FirstName`, `LastName`, `userid`) ⇒ **ไม่มีการย้ายข้อมูลบัญชีเลย** hash เดิม 26 แถวใช้ต่อได้ (Identity v3; `CompatPasswordHasher` ส่งต่อให้ตัวมาตรฐาน)
- `LockoutEnd` เป็น `datetimeoffset` — ไม่โดนปัญหาเวลา · `AccessFailedCount` ใช้ต่อ
- 6,993 `sc_user` ที่ไม่มีบัญชี Identity: **ไม่สร้างให้** (ไม่สร้างข้อมูลสมมุติ) · ถ้าลูกค้าต้องการเปิดใช้ ใช้หน้าผู้ใช้ตามปกติ
- 9 แถว `AspNetUsers` ที่ `userid` ชี้ไม่เจอ: รายงานเท่านั้น ไม่ลบ
- `PayrollEmployeeAdmin.razor:609` สร้างผู้ใช้ Identity เองนอก `UserProvisioningService` — ต้องย้ายไปเรียก `UserProvisioningService` ตอนต่อไลบรารี (ไม่งั้นได้ชื่อผู้ใช้คนละรูปแบบ)
- claim ทุกตัวต้องเหมือนเดิมหลังย้าย — ทดสอบด้วยการถ่าย claim ของบัญชีทดสอบทุกบัญชีก่อน/หลัง (ข้อ 4.4)

### 3.4 เวลาท้องถิ่น vs UTC — ⚠️ เรื่องที่ต้องตัดสิน (D4)

HRM เขียน `DateTime.Now` (เวลาไทย) ลงคอลัมน์ `datetime`/`datetime2` ต้นฉบับหลักเขียน `UtcNow` และ **ตัวแปลงในโมเดลติดป้าย Kind=Utc ให้ทุกค่าที่อ่าน** ⇒ ค่าเก่าจะถูกมองว่าเร็วกว่าจริง 7 ชม.

| คอลัมน์ | ผลถ้าไม่แปลง |
|---|---|
| `sc_user.pwdexpdate` | หมดอายุช้าไป 7 ชม. (ตอนนี้ 0 แถว · นโยบายหมดอายุปิดอยู่) |
| `sc_user.lasttimelogin`, `lastinvalidpwd`, `moddate`, `registerdate` · `sc_role.moddate` · `sc_role_menu.moddate` · `sc_user_role.modate` · `sc_role_scope.moddate` · `sc_program_role.moddate` | แสดงผิด 7 ชม. |
| `sc_user_session.createddate`, `lastseendate`, `revokeddate` (307 เซสชันเปิด) | หน้าเซสชันแสดงผิด |
| **`AuditLog.EventDate`** (127,380 แถว — **ทุกโมดูล HRM เขียนผ่าน `HRMContext` ด้วย `DateTime.Now`**) | ตารางเดียวมีสองเขตเวลาปนกัน: แถวจากไลบรารีเป็น UTC แถวจากโมดูลอื่นเป็นเวลาไทย — ร่องรอยตามกฎหมายต้องบอกเวลาได้ถูก จึงยอมไม่ได้ |
| `startdate`/`enddate` (ชนิด `date`) | ไม่กระทบ (วันที่ธุรกิจ) — ต้องยืนยันว่าต้นฉบับหลัก map `sc_user`/`sc_user_role`/`sc_role_menu` เป็น `DateOnly` เหมือน scope |

ทางเลือก:
- **ก. แปลงทั้งหมดเป็น UTC ครั้งเดียว** (`UPDATE … = DATEADD(hour,-7, …)` ใน migration เดียว) **และ** เปลี่ยนตัวเขียน `AuditLog` ของ `HRMContext` เป็น `UtcNow` พร้อมหน้าดู audit แปลงกลับเป็นเวลาไทย ในรอบ deploy เดียวกัน — ถูกต้องที่สุด กระทบ HRM ทั้งระบบ (75 ไฟล์ใช้ `IAuditLogger`; ตัวเขียนหลักคือ `HRMContext.Audit.cs` + `AuditLogger` ที่เดียว)
- ข. ไม่แปลง — ต้องให้ ADP.AI เพิ่ม option ปิดตัวแปลง/ตั้งเขตเวลาของโฮสต์ (X6) · HRM อยู่กับเวลาไทยต่อ
- ค. แปลงเฉพาะตาราง sc_* (น้อยแถว) และให้ `AuditLog` เป็นของ HRM ต่อไป (ไลบรารีเขียน audit ผ่าน `IAuditLogger` ที่โฮสต์เสียบ) — ขึ้นกับ D7

ความเห็น: **ก** ถ้า CEO ต้องการให้ทุกระบบใช้กติกา UTC เดียวกัน · ต้องทำบนสำเนาฐานก่อน และหยุดระบบระหว่าง `UPDATE`

---

## ข้อ 4 — แผนย้าย

### 4.1 ลำดับขั้น

| ขั้น | ใคร | งาน | เงื่อนไขผ่าน |
|---|---|---|---|
| **0** (งานนี้) | HRM | เทียบ + แผน | เอกสารนี้ |
| **1** | ADP.AI | ยกข้อ 1.2 #1–#11 เข้าต้นฉบับหลัก + ช่องเสียบ X3–X6 + แก้ข้อความ MODULES.md เรื่อง `@local.humanok` | เทสต์ต้นฉบับหลักเขียว (58 + เคสใหม่: audit มีแถวเมื่อให้สิทธิ์, permversion เพิ่มเมื่อถอด role, masking) |
| **2** | CEO | ตัดสิน D1 (ช่องทางแจกไลบรารี) และ D2–D8 | — |
| **3** | HRM | migration SQL Server (ข้อ 3.2 + 3.4 ตาม D4) — ซ้อมบนสำเนาฐานก่อน | `SchemaConventionTests` เขียว · snapshot ตรงต้นฉบับหลัก · รัน query ภาคผนวก ก ก่อน/หลัง ตัวเลขเท่าเดิม |
| **4** | HRM | ต่อไลบรารีหลังสวิตช์ config: เขียน plug-in 7 ตัว (ข้อ 2.1) · ถอด registration ซ้ำ (ข้อ 2.3) · เปลี่ยน namespace (`ProgramRoleService` 63 ไฟล์ · `IAuditLogger` 75 · `RoleScopeSnapshot` 9 · `PasswordPolicyService` 8) · `PayrollEmployeeAdmin` ไปใช้ `UserProvisioningService` · หน้าจัดการสิทธิ์ตาม D2 | เทสต์ข้อ 4.4 เขียวทั้งหมด · ล็อกอินบัญชีเดิมทุกบัญชีทดสอบได้โดยไม่รีเซ็ต |
| **5** | HRM | ใช้งานจริงคู่ขนาน → ปิดสวิตช์ของเดิม | ไม่มี regression 1 รอบเงินเดือน |
| **6** | HRM (CEO อนุมัติ) | ลบสำเนาใน HRM (`Services/Security/*`, `Services/Login/*` ส่วนที่ย้าย, `src/Advance.SecurityCore/` ซึ่งเป็นโครงเปล่าจาก 12 ก.ย. ไม่มี ProjectReference) | ตามกติกา "ยังไม่ลบจนขึ้น production" |

### 4.2 ต้องให้ CEO ตัดสิน

| # | เรื่อง | ทางเลือก | ความเห็น |
|---|---|---|---|
| D1 | ระบบอื่นรับไลบรารีทางไหน | NuGet ส่วนตัว / อ้าง project ข้าม repo (build ได้เครื่องเดียว) / git submodule | NuGet ส่วนตัว — build ได้ทุกเครื่องและปักรุ่นได้ |
| D2 | หน้าจัดการสิทธิ์ | ก ไลบรารี / ข MudBlazor เดิม / ค ข แล้วไป ก | ค (ข้อ 2.4) |
| **D3** | **role ไม่มีขอบเขตข้อมูล = เห็นทุกบริษัท** | คงความหมายเดิม / เปลี่ยนเป็น "เห็นเฉพาะบริษัทตัวเอง จนกว่าจะตั้งขอบเขต" | ตอนนี้ **16/16 role ไม่มีขอบเขต** ⇒ ทุกคนที่มีสิทธิ์สลับบริษัท เห็นทุกบริษัท (HRM พิสูจน์แล้ว: `PAYROLL_APPROVER` ของ UITEST เห็นงวดเงินเดือน ADVD) · เปลี่ยนความหมายต้องตั้งขอบเขตให้ role ที่ต้องเห็นหลายบริษัทก่อน (เช่น Admin) ไม่งั้นเขาจะเห็นน้อยลงทันที · ความเห็น: เปลี่ยน แต่ `CompanySwitchService` ปิดก่อน (เห็นแต่บริษัทตัวเอง) แยกจากความหมายระดับไลบรารี |
| D4 | เวลาเก่า | ก / ข / ค (ข้อ 3.4) | ก |
| D5 | บั๊ก 5 ข้อของ HRM | แก้ใน HRM ตอนนี้ / รอไปใช้หน้าไลบรารี | ข้อ (3) `Abcd@2025` ความเสี่ยงสูงสุด ควรแก้ก่อนไม่ต้องรอ |
| D6 | ชื่อผู้ใช้สังเคราะห์ | คง `@local.humanok` (option) / เปลี่ยนเป็น `@local.advance` ทั้งฐาน | คงเดิม — เปลี่ยนต้องแก้ 26 แถว + seeder ไม่ได้อะไร |
| D7 | ใครเป็นเจ้าของตาราง `AuditLog` | ไลบรารี (ทุกโมดูล HRM เขียนร่วม + มี TenantId) / HRM (ไลบรารีเขียนผ่าน `IAuditLogger` ที่โฮสต์เสียบ) | ไลบรารี — แต่ต้องได้ข้อ 1.2 #1 + #9 ก่อน |
| D8 | อนุมัติรายการยกเข้า (ข้อ 1.2) และลำดับ | — | #1, #2 ก่อนอย่างอื่น (กฎหมาย + ความปลอดภัย) |

### 4.3 ความเสี่ยง

| ความเสี่ยง | ผล | กันด้วย |
|---|---|---|
| ย้ายก่อนได้ audit อัตโนมัติ | การแก้สิทธิ์ไม่มีร่องรอย (ขัดกฎหมาย) | ขั้น 1 ต้องเสร็จก่อนขั้น 4 |
| ย้ายก่อนได้ permversion bump | ถอดสิทธิ์แล้วยังใช้ได้ถึง 60 นาที (อายุ cookie) | เหมือนกัน |
| เวลาเลื่อน 7 ชม. | audit ผิดเวลา · หน้าเซสชันผิด | D4 + ซ้อมบนสำเนา |
| registration ซ้ำ (`login` rate limit, Identity store สองตัว) | แอปล้มตอนสตาร์ต / ล็อกอินเขียนผิด context | ขั้น 4 ทำในรอบเดียว + เทสต์สตาร์ตแอป |
| claim เปลี่ยนรูปแบบ | 366 หน้า `Menu:XXX` · 163 ไฟล์ `payroll_company` เข้าไม่ได้ | เทสต์เทียบ claim ก่อน/หลัง |
| ESS/MSS หาย (enricher ไม่ได้ใส่เมนู) | พนักงานเข้า ESS ไม่ได้ทั้งบริษัท | เทสต์บัญชี `uitest.ess` + หัวหน้า |
| ฐานลูกค้าจริงมีชื่อซ้ำ / role ชื่อซ้ำ | สร้างดัชนีไม่ผ่าน / ทุกหน้าโยน exception (HRM เดิม) | รันภาคผนวก ก ทุกฐานก่อน |
| อ้าง project ข้าม repo | build ได้เครื่องเดียว | D1 |

### 4.4 เทสต์ที่ต้องเขียวก่อนและหลังย้าย

**ก่อน (baseline ต้องเขียววันนี้ และเก็บผลไว้เทียบ):** `HRM.Tests/Security/{ProgramRoleAccessTests, ProgramRoleSeederTests, SecurityAlertCounterTests}` · `HRM.Tests/Schema/SchemaConventionTests` · `Integration/PayrollApprovalE2ETests` (ใช้สิทธิ์ maker/checker) · เทสต์ต้นฉบับหลัก 58 เคส

**ต้องเขียนเพิ่ม (ก่อนขั้น 4 — ให้เขียวกับของเดิมก่อน แล้วรันซ้ำหลังย้าย):**
1. **Claim snapshot** — ล็อกอินบัญชีทดสอบทุกบัญชี (`admin`, `advadmin`, `KB0001`, `ess-test`, `uitest.maker`/`checker`/`ess`, หัวหน้า MSS 1 คน) เก็บชุด claim (`menu`, `menu_edit`, `scope_*`, `empno`, `payroll_company`, `headed_org`, ESS menus) → หลังย้ายต้องเท่าเดิมทุกตัว (ยกเว้น `sc_tenant` ที่เพิ่ม)
2. ล็อกอินด้วยรหัสเดิมผ่าน **โดยไม่รีเซ็ต** · ผิด 5 ครั้งล็อก + `invalidpwcount` mirror · ปลดล็อกแล้วเข้าได้
3. `isforcechanged=1` → ถูกพาไป `/force-change-password` และออกได้หลังเปลี่ยน
4. ถอด role ของผู้ใช้ที่ล็อกอินอยู่ → ถูกเตะออกภายในรอบ revalidate (permversion)
5. ให้/ถอนสิทธิ์ → มีแถว `AuditLog` พร้อมค่าก่อน/หลัง และค่า `password`/`securitystamp` ถูกปิดบัง
6. ถอนเซสชันจากหน้าเซสชัน → ผู้ใช้หลุด
7. `ProgramRoleService.RequireAsync` ปฏิเสธ role อ่านอย่างเดียว · Admin เขียนได้
8. `CompanySwitchService` — role ที่มีขอบเขตบริษัทสลับได้เฉพาะบริษัทนั้น (และตาม D3)
9. เวลา: `lasttimelogin`/`AuditLog.EventDate` แสดงเป็นเวลาไทยถูกต้องหลังแปลง
10. แอปสตาร์ตได้ใน Development และ Production config (จับ registration ซ้ำ)

---

## ภาคผนวก ก — query ตรวจความพร้อมก่อนย้าย (อ่านอย่างเดียว · ไม่ดึงค่ารหัสผ่าน)

```sql
-- ชื่อเข้าระบบซ้ำ (ต้องเป็น 0 ก่อนสร้างดัชนี (TenantId, loginname))
select count(*) from (select loginname from sc_user group by loginname having count(*)>1) x;
select count(*) from (select lower(ltrim(rtrim(loginname))) l from sc_user group by lower(ltrim(rtrim(loginname))) having count(*)>1) x;
-- role ใช้งานชื่อซ้ำ (บั๊ก ToDictionaryAsync ของ HRM)
select count(*) from (select name from sc_role where isactive=1 group by name having count(*)>1) x;
-- role ที่ไม่มีขอบเขต (D3)
select count(*) from sc_role r where isactive=1 and not exists(select 1 from sc_role_scope s where s.roleid=r.roleid and s.isactive=1);
-- บัญชี Identity: hash แบบ v3 / ว่าง / ชื่อผู้ใช้สังเคราะห์ / userid ชี้ไม่เจอ
select sum(case when PasswordHash like 'AQAAAA%' then 1 else 0 end) v3, sum(case when PasswordHash is null then 1 else 0 end) nohash,
       sum(case when UserName like '%@local.humanok' then 1 else 0 end) humanok, count(*) total from AspNetUsers;
select count(*) from AspNetUsers a where not exists(select 1 from sc_user u where u.userid=a.userid);
-- สิทธิ์รายหน้าซ้ำ
select count(*) from (select roleid,progpath from sc_program_role group by roleid,progpath having count(*)>1) x;
-- ขนาดงานแปลงเวลา
select count(*) from AuditLog; select count(*) from sc_user_session where isrevoked=0;
```
