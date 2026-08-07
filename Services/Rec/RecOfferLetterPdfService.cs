namespace HRM.Services.Rec;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Offer-letter PDF, mirrors SalaryCertificatePdfService.cs's plain-letter
// A4 layout style (formal letter, not a tabular report).
public static class RecOfferLetterPdfService
{
    public static byte[] Generate(string companyName, string? companyAddress, string candidateName,
        string positionTitle, decimal offeredSalary, DateOnly startDate, DateOnly? expiryDate, DateTime issueDate)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Content().Column(column =>
                {
                    column.Item().AlignCenter().Text(companyName).FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(companyAddress))
                        column.Item().AlignCenter().Text(companyAddress).FontSize(10);

                    column.Item().PaddingTop(20).AlignRight().Text($"วันที่ {issueDate:dd MMMM yyyy}");

                    column.Item().PaddingTop(15).Text("เรื่อง  ข้อเสนอการจ้างงาน (Offer of Employment)").Bold();
                    column.Item().Text($"เรียน  {candidateName}");

                    column.Item().PaddingTop(15).Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(12));
                        text.Line($"บริษัท {companyName} มีความยินดีที่จะเสนอตำแหน่งงาน {positionTitle} ให้แก่ท่าน");
                        text.Line($"อัตราเงินเดือนที่เสนอ {offeredSalary:N2} บาทต่อเดือน");
                        text.Line($"วันที่คาดว่าจะเริ่มงาน {startDate:dd MMMM yyyy}");
                        if (expiryDate is not null)
                            text.Line($"ข้อเสนอนี้มีผลถึงวันที่ {expiryDate:dd MMMM yyyy}");
                    });

                    column.Item().PaddingTop(10).Text("กรุณาแจ้งผลการตอบรับกลับมายังบริษัทฯ ภายในระยะเวลาที่กำหนด");
                    column.Item().PaddingTop(10).Text("จึงเรียนมาเพื่อโปรดพิจารณา");

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
}
