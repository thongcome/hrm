<#
.SYNOPSIS
    ประกอบ "ชุดติดตั้ง" ที่คนติดตั้งหยิบไปใช้หน้างานได้ทันที แล้วเก็บเป็นรุ่นใหม่ใน Advance.Platform

.DESCRIPTION
    ของที่คนติดตั้งต้องใช้ ห้ามกระจายหลายที่ (repo / โฟลเดอร์ backup ของ SQL / เครื่อง dev)
    สคริปต์นี้รวบทุกอย่างไว้โฟลเดอร์เดียว ก๊อปลง USB ไปหน้างานได้เลย และ **เก็บแยกรุ่นเสมอ ไม่เขียนทับของเดิม**
    (กติกาเดียวกับเอกสาร feature/brochure ใน Advance.Platform)

        D:\GitWorkspace\Advance.Platform\HumanOk\v<รุ่น>-<วันที่>\ชุดติดตั้ง\
            อ่านก่อน.txt
            1_ติดตั้งลูกค้าใหม่.ps1      ← ถามชื่อบริษัทแล้วทำให้หมด
            2_ตรวจความพร้อม.ps1          ← รายงาน ผ่าน/ควรกรอก/ต้องแก้
            คู่มือ_ขึ้นระบบลูกค้า.md
            แม่แบบ\hrm_template_vX.bak
            โปรแกรม\                     ← ระบบที่ publish แล้ว (เครื่องปลายทางไม่ต้องมี .NET SDK)
            สคริปต์ฐานข้อมูล\            ← 10_/20_/30_ ฉบับเต็มไว้ใช้มือถ้าจำเป็น

    ต้นฉบับยังอยู่ใน repo ที่เดียว — ชุดนี้เป็นผลลัพธ์ที่ generate ใหม่ได้ทุกครั้งที่ออกรุ่น

.EXAMPLE
    .\tools\deploy\make_install_kit.ps1 -Version 1.0 -Note "ชุดติดตั้งแรก — id/code discipline + install kit"
    .\tools\deploy\make_install_kit.ps1 -Version 1.0 -NoApp      # ไม่ต้อง publish ตัวโปรแกรม (เร็วกว่า ไฟล์เล็กกว่า)
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,          # เช่น 1.0
    [string]$Note = "",                              # ข้อความสั้น ๆ ลงตาราง README ของ Advance.Platform
    [string]$Backup,                                 # เว้นว่าง = ใช้ hrm_template_v*.bak ใหม่สุด
    [string]$PlatformRoot = "D:\GitWorkspace\Advance.Platform\HumanOk",
    [switch]$NoApp                                   # ข้ามการ publish ตัวโปรแกรม
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$deploy = Join-Path $repoRoot 'tools\deploy'
$today = Get-Date -Format 'yyyy-MM-dd'
$versionFolder = Join-Path $PlatformRoot "v$Version-$today"
$kit = Join-Path $versionFolder 'ชุดติดตั้ง'

if (Test-Path $kit) { throw "มีชุดติดตั้งรุ่นนี้อยู่แล้ว: $kit — กติกาของ Advance.Platform คือไม่เขียนทับ ให้เพิ่มเลขรุ่นแทน" }
New-Item -ItemType Directory -Force -Path $kit | Out-Null

# ── แม่แบบฐานข้อมูล ────────────────────────────────────────────────────────
if (-not $Backup) {
    $backupDir = & {
        $conn = New-Object System.Data.SqlClient.SqlConnection "Server=.;Database=master;Integrated Security=true;TrustServerCertificate=true"
        $conn.Open()
        try {
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))"
            return $cmd.ExecuteScalar()
        } finally { $conn.Close() }
    }
    $Backup = (Get-ChildItem (Join-Path $backupDir 'hrm_template_v*.bak') |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
}
if (-not $Backup -or -not (Test-Path $Backup)) { throw "ไม่พบไฟล์แม่แบบ hrm_template_v*.bak — สร้างก่อนตาม tools/deploy/README.md" }

$templateDir = Join-Path $kit 'แม่แบบ'
New-Item -ItemType Directory -Force -Path $templateDir | Out-Null
Copy-Item $Backup $templateDir
$templateName = Split-Path $Backup -Leaf
Write-Host "แม่แบบ: $templateName ($([math]::Round((Get-Item $Backup).Length/1MB,1)) MB)" -ForegroundColor Green

# ── สคริปต์ + คู่มือ ───────────────────────────────────────────────────────
$sqlDir = Join-Path $kit 'สคริปต์ฐานข้อมูล'
New-Item -ItemType Directory -Force -Path $sqlDir | Out-Null
Copy-Item (Join-Path $deploy '10_make_clean_template.sql') $sqlDir
Copy-Item (Join-Path $deploy '20_new_customer.sql') $sqlDir
Copy-Item (Join-Path $deploy '30_check_ready.sql') $sqlDir
Copy-Item (Join-Path $deploy 'new_customer.ps1') $sqlDir
Copy-Item (Join-Path $repoRoot 'docs\Customer_Setup_Steps.md') (Join-Path $kit 'คู่มือ_ขึ้นระบบลูกค้า.md')

# ── ตัวโปรแกรม ─────────────────────────────────────────────────────────────
if (-not $NoApp) {
    $appDir = Join-Path $kit 'โปรแกรม'
    Write-Host "กำลัง publish ตัวโปรแกรม (ใช้เวลาสักครู่)..." -ForegroundColor Cyan
    & dotnet publish (Join-Path $repoRoot 'HRM.csproj') -c Release -o $appDir --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "publish ไม่สำเร็จ" }
    # ห้ามให้ connection string / ความลับของเครื่อง dev ติดไปกับชุดติดตั้ง
    Get-ChildItem $appDir -Filter 'appsettings.Development.json' -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
}

# ── ตัวช่วยสองไฟล์ที่คนติดตั้งกดจริง ───────────────────────────────────────
@'
# ติดตั้งลูกค้าใหม่ — ถามข้อมูลแล้วทำให้หมด (restore แม่แบบ → เปลี่ยนเป็นบริษัทลูกค้า → ตั้งรหัส advadmin)
$ErrorActionPreference = 'Stop'
$kit = Split-Path $MyInvocation.MyCommand.Path -Parent

Write-Host "=== ติดตั้ง HumanOk ให้ลูกค้าใหม่ ===" -ForegroundColor Cyan
$db     = Read-Host "ชื่อฐานข้อมูลที่จะสร้าง (เช่น hrm_acme)"
$code   = Read-Host "รหัสบริษัท ภาษาอังกฤษตัวพิมพ์ใหญ่ (เช่น ACME)"
$name   = Read-Host "ชื่อบริษัท ภาษาไทย"
$nameEn = Read-Host "ชื่อบริษัท ภาษาอังกฤษ (เว้นว่างได้)"
$taxId  = Read-Host "เลขประจำตัวผู้เสียภาษี 13 หลัก"
$serve  = Read-Host "เปิดระบบให้ลูกค้าดูต่อเลยไหม (y/n)"

$backup = (Get-ChildItem (Join-Path $kit 'แม่แบบ\hrm_template_v*.bak') | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
$args = @('-Database', $db, '-Code', $code, '-Name', $name, '-NameEn', $nameEn, '-TaxId', $taxId, '-Backup', $backup)
if ($serve -eq 'y') { $args += '-Serve' }

& (Join-Path $kit 'สคริปต์ฐานข้อมูล\new_customer.ps1') @args
'@ | Set-Content -Encoding utf8 (Join-Path $kit '1_ติดตั้งลูกค้าใหม่.ps1')

@'
# ตรวจว่าฐานของลูกค้าพร้อมใช้งานจริงหรือยัง (อ่านอย่างเดียว ไม่แก้อะไร)
$ErrorActionPreference = 'Stop'
$kit = Split-Path $MyInvocation.MyCommand.Path -Parent
$db = Read-Host "ชื่อฐานข้อมูลของลูกค้า"

$sql = Get-Content -Raw -Encoding UTF8 (Join-Path $kit 'สคริปต์ฐานข้อมูล\30_check_ready.sql')
$conn = New-Object System.Data.SqlClient.SqlConnection "Server=.;Database=$db;Integrated Security=true;TrustServerCertificate=true"
$conn.Open()
try {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 600
    $da = New-Object System.Data.SqlClient.SqlDataAdapter $cmd
    $ds = New-Object System.Data.DataSet
    [void]$da.Fill($ds)
    foreach ($t in $ds.Tables) { $t | Format-Table -AutoSize | Out-String -Width 250 | Write-Host }
} finally { $conn.Close() }
Read-Host "กด Enter เพื่อปิด"
'@ | Set-Content -Encoding utf8 (Join-Path $kit '2_ตรวจความพร้อม.ps1')

# ── อ่านก่อน ───────────────────────────────────────────────────────────────
@"
ชุดติดตั้ง HumanOk รุ่น $Version ($today)
แม่แบบฐานข้อมูล: $templateName
=====================================================================

ต้องมีที่เครื่องปลายทาง: SQL Server + สิทธิ์สร้างฐานข้อมูล

ทำ 3 ขั้น
---------------------------------------------------------------------
1) คลิกขวาที่  1_ติดตั้งลูกค้าใหม่.ps1  -> Run with PowerShell
   ใส่ชื่อฐานข้อมูล / รหัสบริษัท / ชื่อบริษัท / เลขผู้เสียภาษี
   **จดรหัสผ่าน advadmin ที่แสดงบนจอ (แสดงครั้งเดียว)**

2) เปิดระบบ แล้ว login ด้วย advadmin (ระบบบังคับเปลี่ยนรหัสทันที)
   ตั้งค่าตามคู่มือ ขั้นที่ 3-5:
     - ตั้งค่าสลิป: ชื่อบริษัท เลขผู้เสียภาษี เลขบัญชีนายจ้าง สปส. จังหวัด
     - รูปแบบไฟล์ธนาคารของลูกค้า  (ต้องมีสเปกจากธนาคาร)
     - ค่าจ้างขั้นต่ำของจังหวัดนั้น
     - ผู้ใช้ + บทบาท: เจ้าหน้าที่เงินเดือน / ผู้อนุมัติ (ต้องคนละคน)
     - นำเข้าพนักงานจาก Excel

3) คลิกขวาที่  2_ตรวจความพร้อม.ps1  -> Run with PowerShell
   ต้องไม่เหลือรายการที่ขึ้นว่า "ต้องแก้" ก่อนใช้งานจริง

ส่งมอบแล้วอย่าลืม
---------------------------------------------------------------------
- ปิดเครื่องมือช่วงติดตั้งใน appsettings ของเครื่องลูกค้า:
      "Deploy": { "InstallerTools": false }
  (ปิดหน้านำเข้าพนักงาน ไม่ให้พนักงานลูกค้าอัปไฟล์ทับทะเบียนพนักงาน)
- เปลี่ยนรหัส advadmin แล้วเก็บไว้ที่ทีมติดตั้ง  ** ห้ามลบบัญชีนี้ **
- ตั้ง backup ฐานข้อมูลอัตโนมัติให้ลูกค้า

รายละเอียดทั้งหมด: คู่มือ_ขึ้นระบบลูกค้า.md
ต้นฉบับของทุกไฟล์ในชุดนี้: D:\GitWorkspace\HRM (repo)
"@ | Set-Content -Encoding utf8 (Join-Path $kit 'อ่านก่อน.txt')

$size = [math]::Round(((Get-ChildItem $kit -Recurse -File | Measure-Object Length -Sum).Sum / 1MB), 1)
Write-Host ""
Write-Host "ชุดติดตั้งพร้อมแล้ว: $kit  ($size MB)" -ForegroundColor Green
Write-Host "อย่าลืมเพิ่มบรรทัดรุ่นนี้ในตารางของ $PlatformRoot\README.md" -ForegroundColor Yellow
if ($Note) { Write-Host "หมายเหตุรุ่นนี้: $Note" }
