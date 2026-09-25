<#
.SYNOPSIS
    ตั้งฐานข้อมูลลูกค้าใหม่จากแม่แบบสะอาด แล้วเปิดระบบให้ลูกค้าดูได้จากเครื่องของเขาเอง

.DESCRIPTION
    ทำสามขั้นที่เคยต้องทำมือทีละคำสั่ง (ดู README.md):
      1. restore แม่แบบ (hrm_template_vX.bak) เป็นฐานของลูกค้า
      2. 20_new_customer.sql — เปลี่ยนรหัส/ชื่อบริษัทเป็นของลูกค้าทุกตารางที่เก็บเป็นสตริง
         (com_company.isActive ถูกตั้งเป็น 1 ในสคริปต์นั้นอยู่แล้ว — HRM ไม่มี appsettings
         "บริษัท active" แยกต่างหากแบบ Advance.Payroll, ActiveCompanyHelper อ่านจากฐานข้อมูลตรงๆ)
      3. dotnet HRM.dll --init-admin — ตั้งรหัสผ่าน advadmin ครั้งเดียว บังคับเปลี่ยนตอน login แรก
    แล้วบอกคำสั่งสำหรับรันเซิร์ฟเวอร์ให้เครื่องอื่นในวงเดียวกันเปิดได้ (-Serve = รันให้เลย)

    ปลอดภัยโดยตั้งใจ: ปฏิเสธถ้าฐานปลายทางมีอยู่แล้วและมีพนักงาน (กันทับฐานจริง)

.EXAMPLE
    .\new_customer.ps1 -Database hrm_acme -Code ACME -Name "บริษัท เอซีเอ็มอี จำกัด" -TaxId 0105561000001 -Serve
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Database,
    [Parameter(Mandatory)][string]$Code,
    [Parameter(Mandatory)][string]$Name,
    [string]$NameEn = "",
    [string]$TaxId = "",
    [string]$Backup,                       # เว้นว่าง = ใช้ไฟล์ hrm_template_v*.bak ล่าสุดในโฟลเดอร์ backup ของ SQL Server
    [string]$Server = ".",
    [switch]$Serve,                        # รันเซิร์ฟเวอร์ต่อท้ายให้เลย
    [int]$Port = 5052
)

$ErrorActionPreference = 'Stop'
# ตัวโปรแกรมที่ใช้รัน --init-admin และเปิดเซิร์ฟเวอร์ — หาได้ทั้งสองแบบ:
#   · ในชุดติดตั้งของลูกค้า  <kit>\โปรแกรม\HRM.dll   (publish แล้ว เครื่องปลายทางไม่มี .NET SDK)
#   · ในเครื่องพัฒนา         <repo>\bin\{Release,Debug}\net10.0\HRM.dll
# เรียกด้วย dotnet <dll> ไม่ใช่ dotnet run: repo นี้ build ด้วย -p:UseAppHost=false และเครื่องลูกค้าไม่มี SDK
$candidates = @(
    (Join-Path $PSScriptRoot '..\โปรแกรม\HRM.dll'),
    (Join-Path $PSScriptRoot '..\..\bin\Release\net10.0\HRM.dll'),
    (Join-Path $PSScriptRoot '..\..\bin\Debug\net10.0\HRM.dll')
)
$appDll = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $appDll) { throw "ไม่พบ HRM.dll — ในชุดติดตั้งต้องมีโฟลเดอร์ 'โปรแกรม', ในเครื่องพัฒนาให้ build ก่อน (dotnet build -p:UseAppHost=false)" }
$appDll = (Resolve-Path $appDll).Path

# คืนผลเป็นแถว — PowerShell คลี่ผลลัพธ์แถวเดียวออกเป็น DataRow ตัวเดียว ดังนั้นทุกจุดที่เรียกต้องครอบด้วย @(...) ก่อน [0]
function Invoke-Sql([string]$sql, [string]$db = 'master') {
    $conn = New-Object System.Data.SqlClient.SqlConnection "Server=$Server;Database=$db;Integrated Security=true;TrustServerCertificate=true"
    $conn.Open()
    try {
        $out = @()
        foreach ($batch in ($sql -split '(?m)^\s*GO\s*$')) {
            if ([string]::IsNullOrWhiteSpace($batch)) { continue }
            $cmd = $conn.CreateCommand(); $cmd.CommandText = $batch; $cmd.CommandTimeout = 1800
            $da = New-Object System.Data.SqlClient.SqlDataAdapter $cmd
            $ds = New-Object System.Data.DataSet
            [void]$da.Fill($ds)
            foreach ($t in $ds.Tables) { foreach ($row in $t.Rows) { $out += $row } }
        }
        return $out
    } finally { $conn.Close() }
}

Write-Host "== 1/3 restore แม่แบบเป็นฐาน $Database ==" -ForegroundColor Cyan

if (-not $Backup) {
    $dir = @(Invoke-Sql "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000)) p")[0].p
    $Backup = (Get-ChildItem (Join-Path $dir 'hrm_template_v*.bak') | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
    if (-not $Backup) { throw "ไม่พบไฟล์แม่แบบ hrm_template_v*.bak ใน $dir — สร้างก่อนตาม README ข้อ 1" }
}
Write-Host "   ใช้แม่แบบ: $Backup"

$existing = @(Invoke-Sql "SELECT DB_ID('$Database') id")
if ($null -ne $existing[0].id -and $existing[0].id -isnot [System.DBNull]) {
    $count = @(Invoke-Sql "SELECT COUNT(*) n FROM HREMPLOYEE" $Database)[0].n
    if ($count -gt 0) { throw "ฐาน $Database มีอยู่แล้วและมีพนักงาน $count คน — ปฏิเสธการทับ ถ้าตั้งใจจริงให้ลบฐานเองก่อน" }
}

# ชื่อไฟล์ตรรกะอ่านจากตัว backup เอง ไม่ hardcode — แม่แบบรุ่นหน้าอาจสร้างจากฐานที่ชื่อไฟล์ต่างไป
$files = @(Invoke-Sql "RESTORE FILELISTONLY FROM DISK = N'$Backup'")
$dataLogical = ($files | Where-Object Type -eq 'D' | Select-Object -First 1).LogicalName
$logLogical  = ($files | Where-Object Type -eq 'L' | Select-Object -First 1).LogicalName
$data = @(Invoke-Sql "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(4000)) p")[0].p
Invoke-Sql @"
IF DB_ID('$Database') IS NOT NULL
BEGIN
    ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$Database];
END
RESTORE DATABASE [$Database] FROM DISK = N'$Backup'
    WITH MOVE '$dataLogical' TO N'$data$Database.mdf', MOVE '$logLogical' TO N'${data}${Database}_log.ldf', RECOVERY;
ALTER DATABASE [$Database] SET RECOVERY SIMPLE;
"@ | Out-Null
Write-Host "   restore เรียบร้อย" -ForegroundColor Green

Write-Host "== 2/3 ตั้งชื่อบริษัทเป็นของลูกค้า ($Code) ==" -ForegroundColor Cyan
$sql = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot '20_new_customer.sql')
$sql = $sql.Replace('$(CODE)', $Code).Replace('$(NAME)', $Name).Replace('$(NAME_EN)', $NameEn).Replace('$(TAXID)', $TaxId)
$rows = @(Invoke-Sql $sql $Database)
$rows | Format-Table -AutoSize | Out-String -Width 200 | Write-Host

Write-Host "== 3/3 ตั้งรหัสผ่าน advadmin ครั้งแรก ==" -ForegroundColor Cyan
Write-Host "   (รหัสจะแสดงครั้งเดียว จดไว้ — ระบบบังคับเปลี่ยนตอน login แรก)" -ForegroundColor Yellow
$cs = "Server=$Server;Database=$Database;Integrated Security=true;TrustServerCertificate=true"
# --init-admin อ่าน connection string จาก config ปกติของแอป จึงส่งผ่าน environment variable
$env:ConnectionStrings__DefaultConnection = $cs
& dotnet $appDll --init-admin
if ($LASTEXITCODE -ne 0) { throw "--init-admin ไม่สำเร็จ (exit $LASTEXITCODE) — ดูข้อความข้างบน" }

Write-Host ""
Write-Host "เสร็จแล้ว — เปิดระบบให้ลูกค้าดูด้วยคำสั่งนี้:" -ForegroundColor Green
$run = "dotnet `"$appDll`" --urls http://0.0.0.0:$Port"
Write-Host "   `$env:ConnectionStrings__DefaultConnection = '$cs'"
Write-Host "   $run"
$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.PrefixOrigin -ne 'WellKnown' -and $_.IPAddress -notlike '169.*' } | Select-Object -First 1).IPAddress
Write-Host "   ลูกค้าเปิดจากเครื่องเขา: http://$ip`:$Port" -ForegroundColor Green

if ($Serve) {
    $env:ConnectionStrings__DefaultConnection = $cs
    & dotnet $appDll --urls "http://0.0.0.0:$Port"
}
