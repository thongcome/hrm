namespace HRM.Services.Pay;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Renders a Form 50-Twi (หนังสือรับรองหักภาษี ณ ที่จ่าย) as a plain PDF —
// standard A4, no password protection (unlike PayslipPdfService — this
// document is issued once directly to the employee, not re-opened monthly).
public static class WithholdingCertificatePdfService
{
    public static byte[] Generate(WithholdingCertificateData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text("หนังสือรับรองการหักภาษี ณ ที่จ่าย").FontSize(16).Bold();
                    column.Item().AlignCenter().Text($"ตามมาตรา 50 ทวิ แห่งประมวลรัษฎากร  ปีภาษี {data.TaxYear}").FontSize(11);
                });

                page.Content().PaddingVertical(15).Column(column =>
                {
                    column.Item().Text("ผู้จ่ายเงินได้ (นายจ้าง)").Bold();
                    column.Item().Text($"ชื่อ: {data.CompanyName ?? "-"}");
                    column.Item().Text($"เลขประจำตัวผู้เสียภาษี: {data.CompanyTaxId ?? "-"}");
                    column.Item().Text($"ที่อยู่: {data.CompanyAddress ?? "-"}");

                    column.Item().PaddingTop(10).LineHorizontal(1);

                    column.Item().PaddingTop(10).Text("ผู้มีเงินได้ (พนักงาน)").Bold();
                    column.Item().Text($"ชื่อ-นามสกุล: {data.EmployeeName}");
                    column.Item().Text($"เลขประจำตัวประชาชน: {data.IdCard ?? "-"}");
                    column.Item().Text($"ที่อยู่ตามทะเบียน: {data.RegisteredAddressBlock ?? "-"}");

                    column.Item().PaddingTop(10).LineHorizontal(1);

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("ประเภทเงินได้").Bold();
                            header.Cell().Text("").Bold();
                            header.Cell().AlignRight().Text("จำนวนเงิน (บาท)").Bold();
                        });

                        table.Cell().Text("1.");
                        table.Cell().Text("เงินเดือน ค่าจ้าง เบี้ยเลี้ยง โบนัส ฯลฯ ตามมาตรา 40(1)");
                        table.Cell().AlignRight().Text(data.TotalTaxableIncome.ToString("N2"));
                    });

                    column.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem(5).Text("รวมเงินได้ที่จ่ายทั้งปี").Bold();
                        row.RelativeItem(2).AlignRight().Text(data.TotalTaxableIncome.ToString("N2")).Bold();
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem(5).Text("รวมภาษีที่หักและนำส่งไว้ทั้งปี").Bold();
                        row.RelativeItem(2).AlignRight().Text(data.TotalTaxWithheld.ToString("N2")).Bold();
                    });

                    column.Item().PaddingTop(10).LineHorizontal(1);

                    column.Item().PaddingTop(10).Text("รายการหักเพื่อการคำนวณภาษี (ข้อมูลประกอบ)").Bold();
                    column.Item().Row(row =>
                    {
                        row.RelativeItem(5).Text("เงินสมทบกองทุนประกันสังคม");
                        row.RelativeItem(2).AlignRight().Text(data.TotalSocialSecurity.ToString("N2"));
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem(5).Text("เงินสะสมกองทุนสำรองเลี้ยงชีพ");
                        row.RelativeItem(2).AlignRight().Text(data.TotalProvidentFund.ToString("N2"));
                    });

                    column.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem();
                        row.RelativeItem().Column(sig =>
                        {
                            sig.Item().AlignCenter().Text("....................................................");
                            sig.Item().AlignCenter().Text("ผู้จ่ายเงิน / ผู้รับรอง");
                        });
                    });
                });

                page.Footer().AlignCenter().Text($"ออกเอกสารเมื่อ {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
            });
        }).GeneratePdf();
    }
}
