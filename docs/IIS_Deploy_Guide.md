# HRM — คู่มือ Deploy ขึ้น IIS (Windows)

แอปเป็น **ASP.NET Core (.NET 10) Blazor Server** — host บน IIS ผ่าน ASP.NET Core Module (ANCM)

## 1. เตรียมเครื่องปลายทาง (ครั้งเดียว)

1. **ติดตั้ง IIS** (เปิด Windows Features → Internet Information Services) ให้มี role service "ASP.NET" / "Web Server"
2. **ติดตั้ง .NET 10 Hosting Bundle** (สำคัญที่สุด — ตัวนี้ลง ASP.NET Core Module ให้ IIS)
   - ดาวน์โหลด "**.NET 10.0 Hosting Bundle**" จาก dotnet.microsoft.com/download
   - ติดตั้งเสร็จ **restart IIS**: เปิด cmd (admin) รัน `net stop was /y && net start w3svc`
3. **SQL Server** ต้องเข้าถึงได้จากเครื่องนี้ (มี database `hrm` แล้ว หรือจะสร้างใหม่)

## 2. Publish แอป

จากโฟลเดอร์โปรเจกต์:
```bash
dotnet publish HRM.csproj -c Release -o C:\inetpub\hrm
```
จะได้ไฟล์ทั้งหมด + `web.config` (ANCM) ในโฟลเดอร์ `C:\inetpub\hrm`

## 3. ตั้งค่า IIS Site

1. เปิด **IIS Manager** → คลิกขวา Sites → **Add Website**
   - Site name: `HRM`
   - Physical path: `C:\inetpub\hrm`
   - Binding: http, port ที่ต้องการ (เช่น 8080) หรือใส่ hostname
2. **Application Pool** ของ site นี้ → Basic Settings → **.NET CLR version = "No Managed Code"**
   (ANCM host .NET เอง ไม่ใช้ CLR ของ IIS)

## 4. Connection string + Environment (สำคัญ)

Connection string **ไม่ได้เก็บใน appsettings.json** (จงใจ ตาม OWASP) — ต้องตั้งผ่าน environment variable

แก้ `C:\inetpub\hrm\web.config` เพิ่ม `<environmentVariables>` ใน `<aspNetCore>`:
```xml
<aspNetCore processPath="dotnet" arguments=".\HRM.dll" stdoutLogEnabled="false" hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ConnectionStrings__DefaultConnection"
      value="Server=.;Database=hrm;Trusted_Connection=True;TrustServerCertificate=True;" />
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />
  </environmentVariables>
</aspNetCore>
```
- ปรับ `Server=` / `Database=` ตามจริง ถ้าใช้ SQL login ให้เป็น `Server=.;Database=hrm;User Id=xxx;Password=xxx;TrustServerCertificate=True;`
- **App Pool identity ต้องมีสิทธิ์เข้า SQL** (ถ้าใช้ Trusted_Connection ให้ตั้ง App Pool identity เป็น account ที่ login SQL ได้ หรือใช้ SQL login ใน connection string แทน)

### ⚠️ Development vs Production
- ตั้ง `ASPNETCORE_ENVIRONMENT=Development` สำหรับ **ทดลอง/demo** → จะได้ demo seeders + login `admin`/`advadmin` (Dev@12345) + ข้อมูลตัวอย่างทั้งหมด
- ตั้งเป็น `Production` เมื่อขึ้นจริง → **ไม่มี demo data และไม่มี dev login** ต้องมี `sc_user` จริงใน DB ก่อน ไม่งั้น login ไม่ได้

## 5. Migrate database (ก่อน start)

แอป **ไม่ auto-migrate** — ต้อง apply migration เองก่อน:
```bash
dotnet ef database update --context HRMContext
```
(รันจากเครื่อง dev ที่ชี้ไป DB เดียวกัน หรือ generate SQL script ด้วย `dotnet ef migrations script` แล้วรันบน SQL Server ปลายทาง)

## 6. เปิดใช้งาน

- Browse site จาก IIS Manager → เปิด `http://localhost:8080` (หรือ binding ที่ตั้ง)
- Startup จะ seed config ที่จำเป็นให้อัตโนมัติ (sc_program_role, sc_menu, WELFARE_CLAIM workflow ฯลฯ) ทุก environment

## Troubleshooting
- **HTTP 500.30 / 500.31** = แอป start ไม่ขึ้น → เปิด `stdoutLogEnabled="true"` ใน web.config ชั่วคราว ดู log ที่ `.\logs\`; มักเป็น connection string ผิด หรือ DB ยังไม่ migrate
- **500.19** = ยังไม่ได้ลง Hosting Bundle (ANCM หาย)
- **login ไม่ได้บน Production** = ไม่มี dev seeder → ใช้ Development environment หรือสร้าง sc_user จริง
- แก้ไฟล์ในโฟลเดอร์ publish แล้วต้อง **recycle App Pool** (หรือแตะไฟล์ `web.config`)
