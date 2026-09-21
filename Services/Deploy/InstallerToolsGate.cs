using Microsoft.Extensions.Configuration;

namespace HRM.Services.Deploy;

// เครื่องมือ "ตอนขึ้นระบบ" ของ Advance Digital ไม่ใช่เมนูที่ลูกค้าใช้ประจำ (CEO, 20 ก.ย. 2569):
// นำเข้าพนักงาน/หน่วยงาน/ยอดยกมาจาก Excel ทำครั้งเดียวตอนติดตั้ง แล้วควรปิด — ถ้าเปิดค้างไว้
// พนักงานลูกค้าที่บังเอิญมีสิทธิ์เมนูอาจอัปไฟล์ทับทะเบียนพนักงานทั้งบริษัทโดยไม่ตั้งใจ
// (ต่างจาก "คีย์เงินได้-เงินหักรายงวด" /pay/adhoc/key ซึ่งเป็นงานประจำเดือนของลูกค้าเอง — เปิดตลอด)
//
// เปิด/ปิดที่ appsettings ของเครื่องนั้น ไม่ต้อง deploy ใหม่:
//   "Deploy": { "InstallerTools": true }    ← เปิดช่วงขึ้นระบบ
// ค่าเริ่มต้น = เปิดบนเครื่อง Development, ปิดบนเครื่องจริง (ตั้งค่าไว้ใน appsettings ของแม่แบบลูกค้า)
public static class InstallerToolsGate
{
    public const string ConfigKey = "Deploy:InstallerTools";

    public const string DisabledMessage =
        "เครื่องมือช่วงติดตั้งถูกปิดอยู่ — หน้านี้ใช้ตอนขึ้นระบบเท่านั้น (ทีมติดตั้ง Advance Digital) "
        + "ถ้าต้องนำเข้าข้อมูลอีกครั้ง ให้ติดต่อทีมติดตั้งเปิดค่า Deploy:InstallerTools ก่อน";

    // ค่าที่ตั้งไว้ชนะเสมอ ไม่ได้ตั้ง = เปิดบน Development · โฮสต์ที่ไม่มี IHostEnvironment (เช่น test host)
    // ถือว่าเปิด — ด่านนี้มีไว้กันผู้ใช้บนเครื่องลูกค้า ไม่ได้มีไว้กันเทสของเราเอง
    public static bool IsEnabled(IConfiguration? configuration, IHostEnvironment? environment)
        => configuration?.GetValue<bool?>(ConfigKey) ?? environment?.IsDevelopment() ?? true;

    public static bool IsEnabled(IServiceProvider services)
        => IsEnabled(services.GetService(typeof(IConfiguration)) as IConfiguration,
                     services.GetService(typeof(IHostEnvironment)) as IHostEnvironment);
}
