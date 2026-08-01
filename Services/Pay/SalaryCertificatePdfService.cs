namespace HRM.Services.Pay;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Renders a หนังสือรับรองเงินเดือน (salary/employment certification letter)
// as a plain A4 PDF — a formal letter format, not a tabular report like the
// other Pay_* PDF services. Position/department/purpose/addressee are
// supplied by the caller at generation time (see SalaryCertificateGenerate.razor)
// rather than pulled from SalaryCertificateData, since HR may need to
// correct/fill in what the underlying employee master data leaves blank.
public static class SalaryCertificatePdfService
{
    public static byte[] Generate(SalaryCertificateData data, string position, string department, string? purpose, string? addressee, DateTime issueDate)
    {
        var isActive = data.ResignDate is null;
        var employmentClause = isActive
            ? $"ได้เข้าทำงานกับบริษัทฯ ตั้งแต่วันที่ {FormatDate(data.WorkDate)} จนถึงปัจจุบัน"
            : $"ได้ทำงานกับบริษัทฯ ตั้งแต่วันที่ {FormatDate(data.WorkDate)} ถึงวันที่ {FormatDate(data.ResignDate)}";
        var salaryClause = data.SalaryAmt is decimal salary
            ? $"ปัจจุบันได้รับเงินเดือนอัตรา {salary:N2} บาท"
            : "ไม่มีข้อมูลอัตราเงินเดือนในระบบ";

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

                    column.Item().PaddingTop(20).AlignRight().Text($"วันที่ {FormatDate(issueDate)}");

                    column.Item().PaddingTop(15).Text("เรื่อง  หนังสือรับรองเงินเดือน").Bold();
                    column.Item().Text($"เรียน  {(string.IsNullOrWhiteSpace(addressee) ? "ผู้ที่เกี่ยวข้อง" : addressee)}");

                    column.Item().PaddingTop(15).Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(12));
                        text.Line($"บริษัท {data.CompanyName} ขอรับรองว่า {data.EmployeeName}");
                        text.Line($"เลขประจำตัวประชาชน {data.IdCard ?? "-"} ตำแหน่ง {(string.IsNullOrWhiteSpace(position) ? "-" : position)}");
                        text.Line($"สังกัด {(string.IsNullOrWhiteSpace(department) ? "-" : department)}");
                        text.Line(employmentClause);
                        text.Line(salaryClause);
                    });

                    if (!string.IsNullOrWhiteSpace(purpose))
                        column.Item().PaddingTop(10).Text($"ทั้งนี้ เพื่อใช้{purpose}");

                    column.Item().PaddingTop(10).Text("จึงเรียนมาเพื่อโปรดทราบ");

                    column.Item().PaddingTop(40).Row(row =>
                    {
                        row.RelativeItem();
                        row.RelativeItem().Column(sig =>
                        {
                            sig.Item().AlignCenter().Text("....................................................");
                            sig.Item().AlignCenter().Text("ผู้มีอำนาจลงนาม");
                        });
                    });
                });

                page.Footer().AlignCenter().Text($"ออกเอกสารเมื่อ {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
            });
        }).GeneratePdf();
    }

    private static string FormatDate(DateTime? date) => date is null ? "-" : date.Value.ToString("dd MMMM yyyy");
}
