using ClosedXML.Excel;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay.AdhocImport;

// คีย์เงินได้/เงินหักรายครั้งทีละหลายคน (หน้า /pay/adhoc/key) และนำเข้าจาก Excel
//
// กติกาที่ต้องไม่หลุด (CEO, 20 ก.ย. 2569):
//   · ไฟล์เดียวใช้ได้ทุกประเภทรายการ — คอลัมน์คือรหัสประเภท
//   · เลือกงวด/รอบที่จ่ายบนหน้าจอ ไม่ใช่ในไฟล์
//   · คีย์ซ้ำ/อัปโหลดซ้ำ คน+ประเภท+งวด+รอบเดิม = แก้ยอดของรายการเดิม ไม่สร้างซ้ำ
//     แต่ถ้ารายการนั้น "อนุมัติแล้ว/ใช้ไปแล้ว" จะไม่ทับเงียบ ๆ — รายงานกลับให้คนตัดสินใจ
//   · ทุกอย่างที่บันทึกเป็น "รออนุมัติ" เสมอ ไม่มีทางลัดอนุมัติอัตโนมัติ
//     เครื่องคำนวณหยิบเฉพาะรายการที่อนุมัติแล้ว จึงไม่มีทางจ่ายโดยไม่มีคนกดอนุมัติ
public class AdhocBulkEntryService(IDbContextFactory<HRMContext> dbFactory)
{
    // ItemDate/Remark/ReferenceNo เป็นของแถว (คนนั้นในไฟล์/ตาราง) ใช้กับทุกประเภทรายการของคนนั้นในชุดนี้
    public sealed record Row(long HremployeeId, string EmpNo, int PayItemTypeId, decimal Amount,
        DateTime? ItemDate = null, string? Remark = null, string? ReferenceNo = null);

    public sealed record SaveResult(int Created, int Updated, int Skipped, List<string> Warnings);

    // ไฟล์ที่ชุดนี้มาจาก (null = คีย์มือ) — บันทึกลง Pay_AdhocImportBatch เพื่อกันวนไฟล์เดิม/ผิดไฟล์ในครั้งถัดไป
    public sealed record ImportFileInfo(string CompanyId, string FileName, string FileSha256, string? FilePeriodStamp, string ImportedByName);

    public sealed record PreviousImport(DateTime ImportedAt, string ImportedByName, string TargetPeriod, PayrollRunType TargetRunType, int? TargetTermNo, int RowCount);

    /// <summary>ไฟล์นี้ (SHA-256 เดิม) เคยนำเข้าบริษัทนี้แล้วหรือยัง — เรียงล่าสุดก่อน</summary>
    public async Task<List<PreviousImport>> GetPreviousImportsAsync(string companyId, string fileSha256, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Pay_AdhocImportBatches
            .Where(b => b.CompanyId == companyId && b.FileSha256 == fileSha256)
            .OrderByDescending(b => b.ImportedAt)
            .Select(b => new PreviousImport(b.ImportedAt, b.ImportedByName ?? b.ImportedByUserId.ToString(), b.TargetPeriod, b.TargetRunType, b.TargetTermNo, b.RowCount))
            .ToListAsync(ct);
    }

    /// <summary>ประเภทรายการที่เลือกได้ (Earning/Deduction ที่เปิดใช้อยู่)</summary>
    public async Task<List<Pay_PayItemType>> GetItemTypesAsync(CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Pay_PayItemTypes
            .Where(t => t.IsActive && t.Category != PayItemCategory.Informational)
            .OrderBy(t => t.Category).ThenBy(t => t.SortOrder).ThenBy(t => t.Code)
            .ToListAsync(ct);
    }

    public async Task<AdhocImportParser.Lookups> GetLookupsAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var types = await context.Pay_PayItemTypes
            .Where(t => t.IsActive && t.Category != PayItemCategory.Informational)
            .Select(t => new { t.Id, t.Code, t.NameTh })
            .ToListAsync(ct);
        var employees = await context.Hremployee
            .Where(e => e.companyid == companyId && e.IsActive && e.EmpNo != null)
            .Select(e => new { e.id, e.EmpNo })
            .ToListAsync(ct);

        return new AdhocImportParser.Lookups(
            types.ToDictionary(t => t.Code, t => (t.Id, t.NameTh), StringComparer.OrdinalIgnoreCase),
            employees.GroupBy(e => e.EmpNo!, StringComparer.OrdinalIgnoreCase)
                     .ToDictionary(g => g.Key, g => g.First().id, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// บันทึกทั้งชุดเป็นรายการ "รออนุมัติ" ของงวดที่เลือก — คน+ประเภทเดิมในงวดเดิมถือเป็นการแก้ยอด
    /// </summary>
    public async Task<SaveResult> SaveAsync(IReadOnlyCollection<Row> rows, string targetPeriod, PayrollRunType targetRunType,
        int? targetTermNo, string reason, long actorUserId, ImportFileInfo? file = null, CancellationToken ct = default)
    {
        if (rows.Count == 0) throw new InvalidOperationException("ไม่มีรายการให้บันทึก");
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("กรุณาระบุเหตุผล/ที่มาของรายการ (เช่น \"ค่าคอมมิชชั่นเดือนกันยายน\")");
        if (targetPeriod.Length != 6 || !int.TryParse(targetPeriod, out _))
            throw new InvalidOperationException("งวดต้องเป็นรูปแบบ yyyyMM");

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var typeIds = rows.Select(r => r.PayItemTypeId).Distinct().ToList();
        var empIds = rows.Select(r => r.HremployeeId).Distinct().ToList();

        var existing = await context.Pay_AdhocPayItems
            .Where(a => a.TargetPeriod == targetPeriod && a.TargetRunType == targetRunType
                        && typeIds.Contains(a.PayItemTypeId) && empIds.Contains(a.HremployeeId)
                        && a.Status != PayAdhocItemStatus.Rejected && a.Status != PayAdhocItemStatus.Cancelled)
            .ToListAsync(ct);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var warnings = new List<string>();

        foreach (var row in rows)
        {
            var match = existing.FirstOrDefault(a => a.HremployeeId == row.HremployeeId
                                                     && a.PayItemTypeId == row.PayItemTypeId
                                                     && a.TargetTermNo == targetTermNo);
            if (match is null)
            {
                context.Pay_AdhocPayItems.Add(new Pay_AdhocPayItem
                {
                    HremployeeId = row.HremployeeId,
                    PayItemTypeId = row.PayItemTypeId,
                    TargetPeriod = targetPeriod,
                    TargetRunType = targetRunType,
                    TargetTermNo = targetTermNo,
                    Amount = row.Amount,
                    IsTaxable = await context.Pay_PayItemTypes.Where(t => t.Id == row.PayItemTypeId)
                        .Select(t => t.IsTaxable).FirstAsync(ct),
                    Reason = reason.Trim(),
                    ItemDate = row.ItemDate?.Date,
                    Remark = Clean(row.Remark, 500),
                    ReferenceNo = Clean(row.ReferenceNo, 100),
                    Status = PayAdhocItemStatus.Pending,      // ต้องมีคนอนุมัติก่อนเสมอ
                    RequestedByUserId = actorUserId,
                });
                created++;
                continue;
            }

            // มีอยู่แล้วและยังไม่ถูกอนุมัติ = แก้ยอด/รายละเอียดได้เลย
            if (match.Status == PayAdhocItemStatus.Pending)
            {
                var newRemark = Clean(row.Remark, 500);
                var newRef = Clean(row.ReferenceNo, 100);
                var changed = match.Amount != row.Amount || match.ItemDate != row.ItemDate?.Date
                              || match.Remark != newRemark || match.ReferenceNo != newRef;
                if (changed)
                {
                    match.Amount = row.Amount;
                    match.ItemDate = row.ItemDate?.Date;
                    match.Remark = newRemark;
                    match.ReferenceNo = newRef;
                    match.Reason = reason.Trim();
                    match.RequestedByUserId = actorUserId;
                    match.RequestedDate = DateTime.Now;
                    updated++;
                }
                else skipped++;
                continue;
            }

            // อนุมัติ/ใช้ไปแล้ว — ห้ามทับเงียบ ๆ
            skipped++;
            warnings.Add($"{row.EmpNo}: มีรายการของงวดนี้ที่{(match.Status == PayAdhocItemStatus.Consumed ? "จ่ายไปแล้ว" : "อนุมัติแล้ว")} " +
                         $"ยอด {match.Amount:N2} — ไม่ทับให้ ถ้าต้องแก้ ให้ยกเลิกรายการเดิมในหน้ารายการเฉพาะกิจก่อน");
        }

        if (file is not null)
        {
            context.Pay_AdhocImportBatches.Add(new Pay_AdhocImportBatch
            {
                CompanyId = file.CompanyId,
                TargetPeriod = targetPeriod,
                TargetRunType = targetRunType,
                TargetTermNo = targetTermNo,
                FilePeriodStamp = file.FilePeriodStamp,
                FileName = Clean(file.FileName, 260),
                FileSha256 = file.FileSha256,
                RowCount = rows.Count,
                Created = created,
                Updated = updated,
                Skipped = skipped,
                ImportedByUserId = actorUserId,
                ImportedByName = Clean(file.ImportedByName, 200),
            });
        }

        await context.SaveChangesAsync(ct);
        return new SaveResult(created, updated, skipped, warnings);
    }

    private static string? Clean(string? s, int max)
    {
        var t = s?.Trim();
        return string.IsNullOrEmpty(t) ? null : t.Length <= max ? t : t[..max];
    }

    /// <summary>
    /// ไฟล์เทมเพลต: คอลัมน์ EMP_NO + หนึ่งคอลัมน์ต่อประเภทรายการที่เลือก พร้อมรายชื่อพนักงานปัจจุบัน
    /// และตรางวด (ชีต "ข้อมูลไฟล์" + Subject) ที่หน้าอัปโหลดใช้ล็อกว่าไฟล์นี้เป็นของงวดไหน
    /// </summary>
    public async Task<byte[]> BuildTemplateAsync(string companyId, IReadOnlyCollection<int> payItemTypeIds,
        string targetPeriod, PayrollRunType targetRunType, int? targetTermNo, string generatedBy, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var types = await context.Pay_PayItemTypes
            .Where(t => payItemTypeIds.Contains(t.Id))
            .OrderBy(t => t.Category).ThenBy(t => t.SortOrder)
            .Select(t => new { t.Code, t.NameTh, t.Category, t.GLAccountCode })
            .ToListAsync(ct);
        if (types.Count == 0) throw new InvalidOperationException("เลือกประเภทรายการอย่างน้อยหนึ่งประเภทก่อนดาวน์โหลดเทมเพลต");

        var employees = await context.Hremployee
            .Where(e => e.companyid == companyId && e.IsActive)
            .OrderBy(e => e.EmpNo)
            .Select(e => new { e.EmpNo, e.EmpName, e.EmpSurname })
            .ToListAsync(ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"รายการ งวด {targetPeriod}");
        ws.Cell(1, 1).Value = AdhocImportParser.EmpNoHeader;
        ws.Cell(1, 2).Value = "ชื่อ-สกุล (ไม่ต้องกรอก)";
        for (var i = 0; i < types.Count; i++)
        {
            var cell = ws.Cell(1, 3 + i);
            cell.Value = types[i].Code;                     // หัวคอลัมน์ = รหัส (ระบบอ่านจากตรงนี้)
            cell.GetComment().AddText($"{types[i].NameTh} ({(types[i].Category == PayItemCategory.Earning ? "เงินได้" : "เงินหัก")})");
            ws.Cell(2, 3 + i).Value = types[i].GLAccountCode ?? "";   // แถว 2 = เลขบัญชี GL (CEO 20 ก.ย. 2569: "ในไฟล์ให้มี code บัญชีเพิ่มอีก 1 แถว")
        }
        ws.Cell(2, 1).Value = AdhocImportParser.AccountRowLabel;        // parser เห็นป้ายนี้ในคอลัมน์ A แถว 2 → ข้อมูลเริ่มแถว 3
        ws.Cell(2, 2).Value = "(แถวนี้ระบบไม่อ่าน)";
        ws.Row(2).Style.Font.Italic = true;
        ws.Row(2).Style.Font.FontColor = XLColor.Gray;
        // คอลัมน์ประกอบรายแถว (ไม่บังคับ) ต่อท้ายประเภทรายการ — CEO 20 ก.ย. 2569: "ต้องเพิ่ม field มากกว่านี้ เช่น วันที่สรุป รายละเอียด"
        var dateCol = 3 + types.Count;
        ws.Cell(1, dateCol).Value = AdhocImportParser.ItemDateHeader;
        ws.Cell(1, dateCol).GetComment().AddText("วันที่สรุปยอด/วันที่เกิดรายการ (ไม่บังคับ) — ไม่ใช่วันที่จ่าย · พิมพ์ วว/ดด/ปปปป ได้ทั้ง พ.ศ. และ ค.ศ.");
        ws.Cell(1, dateCol + 1).Value = AdhocImportParser.RemarkHeader;
        ws.Cell(1, dateCol + 1).GetComment().AddText("รายละเอียดของรายการคนนี้ (ไม่บังคับ) เช่น ยอดขาย ส.ค. 1,200,000 × 3% — ติดไปกับทุกรายการในแถวนี้");
        ws.Cell(1, dateCol + 2).Value = AdhocImportParser.ReferenceNoHeader;
        ws.Cell(1, dateCol + 2).GetComment().AddText("เลขที่เอกสารต้นทาง (ไม่บังคับ) เช่น เลขบันทึกข้อความ / เลขรายงานยอดขาย");
        ws.Row(1).Style.Font.Bold = true;

        // ข้อมูลเริ่มแถว 3 (แถว 1 หัว, แถว 2 บัญชี GL) — รายชื่อพนักงานทั้งบริษัทเติมมาให้ (Excel รับ 5,000 แถวสบาย
        // คนกรอกกรองเอาหรือลบแถวที่ไม่มียอดทิ้งได้ แถวว่างระบบข้าม) · ตรึงหัวตารางไว้ให้เลื่อนดูได้
        for (var r = 0; r < employees.Count; r++)
        {
            ws.Cell(r + 3, 1).Value = employees[r].EmpNo;
            ws.Cell(r + 3, 2).Value = $"{employees[r].EmpName} {employees[r].EmpSurname}".Trim();
            for (var i = 0; i < types.Count; i++) ws.Cell(r + 3, 3 + i).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(r + 3, dateCol).Style.NumberFormat.Format = "dd/MM/yyyy";
        }
        ws.SheetView.FreezeRows(2);
        ws.Columns().AdjustToContents();
        ws.Column(dateCol).Width = 14;
        ws.Column(dateCol + 1).Width = 40;
        ws.Column(dateCol + 2).Width = 18;

        var guide = wb.Worksheets.Add(AdhocImportParser.GuideSheetName);
        guide.Cell(1, 1).Value = "วิธีใช้ไฟล์นี้";
        guide.Cell(1, 1).Style.Font.Bold = true;
        var help = new[]
        {
            "1. กรอกจำนวนเงินในคอลัมน์ของประเภทรายการที่ต้องการ (เว้นว่างหรือ 0 = ไม่มีรายการของคนนั้น)",
            "2. ห้ามใส่จำนวนติดลบ — เงินหักให้ใช้คอลัมน์ของประเภทเงินหัก และกรอกเป็นจำนวนบวก",
            "3. คอลัมน์ \"ชื่อ-สกุล\" มีไว้ให้ดูเท่านั้น ระบบไม่อ่าน",
            "4. งวดที่จ่ายและรอบ (ปกติ/โบนัส/คนออก) เลือกบนหน้าจอตอนอัปโหลด ไม่ได้อยู่ในไฟล์",
            "5. อัปโหลดซ้ำคนเดิม ประเภทเดิม งวดเดิม = แก้ยอดของรายการเดิม ไม่สร้างซ้ำ",
            "6. ทุกรายการที่นำเข้าเป็นสถานะ \"รออนุมัติ\" ต้องมีผู้มีสิทธิ์กดอนุมัติก่อน จึงจะถูกจ่ายในรอบเงินเดือนของงวดนั้น",
            "7. เพิ่มประเภทรายการใหม่ได้ที่หน้า \"ประเภทรายการเงินได้-เงินหัก\" แล้วดาวน์โหลดเทมเพลตนี้ใหม่",
            "8. คอลัมน์ \"วันที่สรุป\" \"รายละเอียด\" \"เลขที่อ้างอิง\" ไม่บังคับ — ใช้กับทุกรายการของคนนั้นในแถวนั้น เก็บไว้ให้ผู้อนุมัติดูและตรวจย้อนหลังได้",
            "9. รหัสประเภทรายการ (หัวคอลัมน์) ต้องตรงกับประเภทที่เลือกบนหน้าจอตอนอัปโหลด ถ้าไม่ตรง ระบบจะไม่นำเข้าและบอกว่าคอลัมน์ไหน",
        };
        for (var i = 0; i < help.Length; i++) guide.Cell(2 + i, 1).Value = help[i];
        guide.Columns().AdjustToContents();

        // ตรางวด — หน้าอัปโหลดเทียบกับงวดที่เลือก ไม่ตรง = ไม่นำเข้า (กันเอาไฟล์เดือนเก่ามาวน / ทำผิดไฟล์)
        var stamp = wb.Worksheets.Add(AdhocImportParser.StampSheetName);
        stamp.Cell(1, 1).Value = "งวด (ปีเดือน ค.ศ.)";      stamp.Cell(1, 2).Value = targetPeriod;
        stamp.Cell(2, 1).Value = "รอบ";                     stamp.Cell(2, 2).Value = targetRunType.ToString();
        stamp.Cell(3, 1).Value = "งวดที่ของเดือน";           stamp.Cell(3, 2).Value = targetTermNo?.ToString() ?? "";
        stamp.Cell(4, 1).Value = "สร้างเมื่อ";               stamp.Cell(4, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        stamp.Cell(5, 1).Value = "สร้างโดย";                 stamp.Cell(5, 2).Value = generatedBy;
        stamp.Cell(7, 1).Value = "ระบบใช้ชีตนี้ตรวจว่าไฟล์เป็นของงวดไหน — ห้ามแก้ ถ้าจะใช้กับงวดอื่นให้ดาวน์โหลดเทมเพลตใหม่";
        stamp.Column(1).Style.Font.Bold = true;
        stamp.Columns().AdjustToContents();
        stamp.Protect("advance");
        wb.Properties.Subject = AdhocImportParser.WriteStampSubject(targetPeriod, targetRunType.ToString(), targetTermNo);
        wb.Properties.Title = $"เงินได้-เงินหักรายงวด {targetPeriod}";

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
