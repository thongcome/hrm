using System.Globalization;
using System.Text;
using System.Text.Json;

namespace HRM.Services.Pay;

// แปลงบันทึกการคำนวณภาษี (JSON ที่ PayrollCalculationService เก็บลง Pay_PayrollAuditLog
// EventType = TaxCalculationDetail) ให้เป็นคำอธิบายที่คนทำเงินเดือนอ่านรู้เรื่อง
//
// CEO, 20 ก.ย. 2569: กดปุ่ม "ดูการคำนวณ" ที่บรรทัดภาษีแล้วเจอแค่ประโยคว่า "ดูรายละเอียดด้านล่าง"
// ซึ่งด้านล่างเป็น JSON ดิบ — ต้องกางตัวเลขจริงให้เห็นตรงนั้นเลย
//
// ลำดับที่แสดงคือลำดับที่กฎหมายคิดจริง: เงินได้ทั้งปี → หักค่าใช้จ่าย/ลดหย่อน → ฐานภาษี
// → ภาษีทั้งปีตามขั้นบันได → หักภาษีที่หักไปแล้ว → เฉลี่ยลงงวดที่เหลือ
public static class TaxCalculationNarrative
{
    public static bool TryBuild(string? detailJson, out string html)
    {
        html = "";
        if (string.IsNullOrWhiteSpace(detailJson)) return false;

        try
        {
            using var doc = JsonDocument.Parse(detailJson);
            var root = doc.RootElement;
            var sb = new StringBuilder();

            var monthly = Num(root, "MonthlyWithholding");
            sb.Append("<div style=\"font-size:.95rem;line-height:1.7\">");
            sb.Append($"<p style=\"margin:0 0 .6rem\"><b>ภาษีที่หักงวดนี้ {Money(monthly)} บาท</b></p>");

            // ── เงินได้ ────────────────────────────────────────────────────
            sb.Append(Section("1. เงินได้ที่นำมาคิดภาษี"));
            sb.Append(Row("เงินได้งวดนี้ (ก่อนหักอะไร)", Num(root, "GrossEarnings")));
            sb.Append(Row("เงินได้สะสมตั้งแต่ต้นปีถึงงวดก่อน", Num(root, "YtdIncomeBeforeThisPeriod")));
            var oneOff = Num(root, "OneOffTaxableIncome");
            if (oneOff != 0m) sb.Append(Row("เงินได้ครั้งเดียว (โบนัส/ค่าคอมฯ) ในงวดนี้", oneOff));
            if (root.TryGetProperty("PriorEmployerIncomeIncluded", out var prior) && prior.ValueKind == JsonValueKind.Object)
            {
                var name = prior.TryGetProperty("PriorEmployerName", out var n) ? n.GetString() : null;
                sb.Append(Row($"เงินได้จากนายจ้างเดิม{(string.IsNullOrWhiteSpace(name) ? "" : $" ({name})")}", Num(prior, "IncomeAmount")));
                sb.Append(Row("ภาษีที่นายจ้างเดิมหักไว้แล้ว", Num(prior, "TaxWithheldAmount")));
            }

            // ── รายการหัก ──────────────────────────────────────────────────
            if (root.TryGetProperty("DeductionBreakdown", out var ded) && ded.ValueKind == JsonValueKind.Object)
            {
                sb.Append(Section("2. หักค่าใช้จ่ายและลดหย่อน"));
                var rate = Num(ded, "ExpenseDeductionRate");
                var cap = Num(ded, "ExpenseDeductionCap");
                if (rate > 0m)
                    sb.Append(Row($"ค่าใช้จ่าย {rate:0.##}% ของเงินได้ (ไม่เกิน {Money(cap)})", null, "ตามประมวลรัษฎากร"));
                sb.Append(Row("ลดหย่อนส่วนตัวและอื่น ๆ (ต่อปี)", Num(ded, "PersonalAllowancePerYear")));
                var elected = Num(ded, "ElectedAnnualDeductions");
                if (elected != 0m) sb.Append(Row("ลดหย่อนที่พนักงานแจ้งไว้ (ต่อปี)", elected));
                if (ded.TryGetProperty("ElectedDeductionItems", out var items) && items.ValueKind == JsonValueKind.Array)
                    foreach (var item in items.EnumerateArray())
                        if (item.GetString() is { Length: > 0 } text)
                            sb.Append($"<div style=\"color:#666;padding-left:1rem\">· {System.Net.WebUtility.HtmlEncode(text)}</div>");
                sb.Append(Row("ประกันสังคมงวดนี้", Num(ded, "SocialSecurity")));
                var pf = Num(ded, "ProvidentFund");
                if (pf != 0m) sb.Append(Row("กองทุนสำรองเลี้ยงชีพงวดนี้", pf));
            }

            // ── ขั้นบันไดภาษี ─────────────────────────────────────────────
            if (root.TryGetProperty("AnnualCalculation", out var annual) && annual.ValueKind == JsonValueKind.Object)
            {
                sb.Append(Section("3. ภาษีทั้งปีตามขั้นบันได"));
                if (annual.TryGetProperty("Breakdown", out var steps) && steps.ValueKind == JsonValueKind.Array && steps.GetArrayLength() > 0)
                {
                    sb.Append("<table style=\"width:100%;border-collapse:collapse;margin:.2rem 0 .5rem\">");
                    sb.Append("<tr style=\"color:#666\"><th style=\"text-align:left;font-weight:500\">ช่วงเงินได้</th>"
                            + "<th style=\"text-align:right;font-weight:500\">อัตรา</th>"
                            + "<th style=\"text-align:right;font-weight:500\">เงินได้ในช่วง</th>"
                            + "<th style=\"text-align:right;font-weight:500\">ภาษี</th></tr>");
                    foreach (var s in steps.EnumerateArray())
                    {
                        var min = Num(s, "MinIncome");
                        var max = s.TryGetProperty("MaxIncome", out var mx) && mx.ValueKind is JsonValueKind.Number
                            ? (decimal?)mx.GetDecimal() : null;
                        sb.Append("<tr>");
                        sb.Append($"<td>{Money(min)} – {(max is null ? "ขึ้นไป" : Money(max.Value))}</td>");
                        sb.Append($"<td style=\"text-align:right\">{Num(s, "RatePercent"):0.##}%</td>");
                        sb.Append($"<td style=\"text-align:right\">{Money(Num(s, "TaxableInBracket"))}</td>");
                        sb.Append($"<td style=\"text-align:right\">{Money(Num(s, "TaxInBracket"))}</td>");
                        sb.Append("</tr>");
                    }
                    sb.Append("</table>");
                }
                sb.Append(Row("<b>ภาษีทั้งปีรวม</b>", Num(annual, "TotalAnnualTax")));
            }

            // ── เฉลี่ยลงงวด ────────────────────────────────────────────────
            sb.Append(Section("4. เฉลี่ยลงงวดที่เหลือ"));
            var periods = Num(root, "RemainingPeriods");
            var months = Num(root, "RemainingMonths");
            var perMonth = Num(root, "PeriodsPerMonth");
            sb.Append(Row("งวดที่เหลือในปีนี้ (รวมงวดนี้)", null, $"{periods:0.##} งวด · {months:0.##} เดือน · จ่ายเดือนละ {perMonth:0.##} งวด"));
            sb.Append(Row("<b>ภาษีที่หักงวดนี้</b>", monthly));

            sb.Append("<p style=\"margin:.7rem 0 0;color:#666;font-size:.85rem\">"
                    + "ตัวเลขชุดนี้บันทึกไว้ตอนคำนวณ (audit trail) — ดูฉบับเต็มแบบข้อมูลดิบได้ที่หัวข้อ \"รายละเอียดการคำนวณภาษี\" ด้านล่างหน้านี้</p>");
            sb.Append("</div>");

            html = sb.ToString();
            return true;
        }
        catch (JsonException)
        {
            return false;   // บันทึกเสียหาย/รูปแบบเก่า — ให้กล่องเดิมทำงานต่อไป ไม่ทำให้หน้าพัง
        }
    }

    private static string Section(string title) =>
        $"<div style=\"margin:.7rem 0 .2rem;font-weight:700\">{title}</div>";

    private static string Row(string label, decimal? amount, string? note = null)
    {
        var right = amount is null ? (note ?? "") : Money(amount.Value) + (note is null ? "" : $" <span style=\"color:#666\">({note})</span>");
        return "<div style=\"display:flex;justify-content:space-between;gap:1rem\">"
             + $"<span>{label}</span><span style=\"text-align:right\">{right}</span></div>";
    }

    private static decimal Num(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : 0m;

    private static string Money(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
