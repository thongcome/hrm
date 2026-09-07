namespace HRM.Services.Perf;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Renders the คำสั่งขึ้นเงินเดือน (salary increase order) as a formal A4 letter —
// same plain-letter shape as SalaryCertificatePdfService, not a tabular
// report. Signature lines cover both the person who authorized the raise and
// the employee acknowledging receipt, matching how a Thai HR order is
// actually countersigned on paper.
public static class SalaryIncreaseOrderPdfService
{
    public static byte[] Generate(SalaryIncreaseOrderData data)
    {
        var orderLine = string.IsNullOrWhiteSpace(data.OrderNo)
            ? "เลขที่คำสั่ง .....................................​"
            : $"คำสั่งเลขที่ {data.OrderNo}";
        var orderDate = data.OrderDate ?? data.AppliedDate;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Content().Column(column =>
                {
                    column.Item().AlignCenter().Text(data.CompanyName).FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(data.CompanyAddress))
                        column.Item().AlignCenter().Text(data.CompanyAddress).FontSize(10);

                    column.Item().PaddingTop(20).AlignCenter().Text(orderLine).Bold();
                    column.Item().AlignCenter().Text("เรื่อง  ปรับอัตราเงินเดือน").Bold();
                    column.Item().PaddingTop(4).AlignRight().Text($"วันที่ {FormatDate(orderDate)}");

                    column.Item().PaddingTop(15).Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(12));
                        text.Line($"ด้วยบริษัทฯ ได้พิจารณาผลการปฏิบัติงาน{(string.IsNullOrWhiteSpace(data.PeriodName) ? "" : $"ประจำรอบ {data.PeriodName}")}ของ");
                        text.Line($"{data.EmployeeName} รหัสพนักงาน {data.EmpNo ?? "-"} ตำแหน่ง {data.PositionName ?? "-"}");
                        text.Line($"สังกัด {data.OrganizationName ?? "-"} แล้ว มีผลการประเมินอยู่ในเกรด {data.Grade}"
                            + (data.FinalScorePercent is decimal score ? $" (คะแนนรวม {score:0.##}%)" : ""));
                        text.Line("จึงเห็นสมควรปรับอัตราเงินเดือนตามรายละเอียดดังนี้");
                    });

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        table.Cell().PaddingVertical(2).Text("อัตราเงินเดือนเดิม");
                        table.Cell().PaddingVertical(2).Text($"{data.OldSalary:N2} บาท").Bold();

                        table.Cell().PaddingVertical(2).Text("อัตราเงินเดือนใหม่");
                        table.Cell().PaddingVertical(2).Text($"{data.NewSalary:N2} บาท").Bold();

                        table.Cell().PaddingVertical(2).Text("อัตราการปรับ");
                        table.Cell().PaddingVertical(2).Text($"{data.EffectiveIncreasePercent:0.##}%").Bold();
                    });

                    column.Item().PaddingTop(10).Text($"ทั้งนี้ตั้งแต่วันที่ {FormatDate(orderDate)} เป็นต้นไป");
                    column.Item().PaddingTop(6).Text("จึงประกาศคำสั่งมาเพื่อทราบและถือปฏิบัติ");

                    column.Item().PaddingTop(50).Row(row =>
                    {
                        row.RelativeItem().Column(sig =>
                        {
                            sig.Item().AlignCenter().Text("....................................................");
                            sig.Item().AlignCenter().Text("ผู้มีอำนาจลงนาม");
                        });
                        row.RelativeItem().Column(sig =>
                        {
                            sig.Item().AlignCenter().Text("....................................................");
                            sig.Item().AlignCenter().Text("ผู้รับทราบคำสั่ง (พนักงาน)");
                        });
                    });
                });

                page.Footer().AlignCenter().Text($"ออกเอกสารเมื่อ {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
            });
        }).GeneratePdf();
    }

    private static string FormatDate(DateTime date) => date.ToString("dd MMMM yyyy");
}
