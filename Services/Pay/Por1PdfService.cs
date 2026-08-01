namespace HRM.Services.Pay;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Renders ภ.ง.ด.1 (monthly) / ภ.ง.ด.1ก (annual) as plain A4 PDFs — internal
// remittance-support documents the employer keeps/files alongside the actual
// RD e-filing submission, not a certificate handed to any one employee (see
// WithholdingCertificatePdfService for that).
public static class Por1PdfService
{
    public static byte[] GenerateMonthly(Por1MonthlyData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text("แบบยื่นรายการภาษีเงินได้หัก ณ ที่จ่าย (ภ.ง.ด.1)").FontSize(15).Bold();
                    column.Item().AlignCenter().Text("ตามมาตรา 40(1) แห่งประมวลรัษฎากร — สรุปประจำงวด").FontSize(10);
                    column.Item().PaddingTop(5).Text($"ผู้จ่ายเงินได้: {data.CompanyName}").Bold();
                    column.Item().Text($"เลขประจำตัวผู้เสียภาษี: {data.CompanyTaxId ?? "-"}   ที่อยู่: {data.CompanyAddress ?? "-"}");
                    column.Item().Text($"งวดเดือนภาษี: {data.PeriodStart:MM/yyyy}   จำนวนผู้มีเงินได้: {data.Lines.Count} คน");
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("ลำดับ").Bold();
                        header.Cell().Text("รหัสพนักงาน").Bold();
                        header.Cell().Text("ชื่อ-นามสกุล").Bold();
                        header.Cell().Text("เลขประจำตัวประชาชน").Bold();
                        header.Cell().AlignRight().Text("เงินได้ (บาท)").Bold();
                        header.Cell().AlignRight().Text("ภาษีหัก (บาท)").Bold();
                    });

                    var seq = 1;
                    foreach (var line in data.Lines)
                    {
                        table.Cell().Text(seq.ToString());
                        table.Cell().Text(line.EmpNo);
                        table.Cell().Text(line.EmployeeName);
                        table.Cell().Text(line.IdCard ?? "-");
                        table.Cell().AlignRight().Text(line.TaxableIncome.ToString("N2"));
                        table.Cell().AlignRight().Text(line.TaxWithheld.ToString("N2"));
                        seq++;
                    }
                });

                page.Footer().Column(column =>
                {
                    column.Item().LineHorizontal(1);
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(8).Text("รวมทั้งสิ้น").Bold();
                        row.RelativeItem(2).AlignRight().Text(data.TotalTaxableIncome.ToString("N2")).Bold();
                        row.RelativeItem(2).AlignRight().Text(data.TotalTaxWithheld.ToString("N2")).Bold();
                    });
                    column.Item().PaddingTop(5).AlignCenter().Text($"ออกเอกสารเมื่อ {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerateAnnual(Por1KorAnnualData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text("แบบยื่นรายการสรุปภาษีเงินได้หัก ณ ที่จ่ายประจำปี (ภ.ง.ด.1ก)").FontSize(15).Bold();
                    column.Item().AlignCenter().Text($"ตามมาตรา 40(1) แห่งประมวลรัษฎากร — ปีภาษี {data.TaxYear}").FontSize(10);
                    column.Item().PaddingTop(5).Text($"ผู้จ่ายเงินได้: {data.CompanyName}").Bold();
                    column.Item().Text($"เลขประจำตัวผู้เสียภาษี: {data.CompanyTaxId ?? "-"}   ที่อยู่: {data.CompanyAddress ?? "-"}");
                    column.Item().Text($"จำนวนผู้มีเงินได้ทั้งปี: {data.Lines.Count} คน");
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("ลำดับ").Bold();
                        header.Cell().Text("รหัสพนักงาน").Bold();
                        header.Cell().Text("ชื่อ-นามสกุล").Bold();
                        header.Cell().Text("เลขประจำตัวประชาชน").Bold();
                        header.Cell().AlignRight().Text("เงินได้รวมทั้งปี (บาท)").Bold();
                        header.Cell().AlignRight().Text("ภาษีหักรวมทั้งปี (บาท)").Bold();
                    });

                    var seq = 1;
                    foreach (var line in data.Lines)
                    {
                        table.Cell().Text(seq.ToString());
                        table.Cell().Text(line.EmpNo);
                        table.Cell().Text(line.EmployeeName);
                        table.Cell().Text(line.IdCard ?? "-");
                        table.Cell().AlignRight().Text(line.TotalTaxableIncome.ToString("N2"));
                        table.Cell().AlignRight().Text(line.TotalTaxWithheld.ToString("N2"));
                        seq++;
                    }
                });

                page.Footer().Column(column =>
                {
                    column.Item().LineHorizontal(1);
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(8).Text("รวมทั้งสิ้น").Bold();
                        row.RelativeItem(2).AlignRight().Text(data.TotalTaxableIncome.ToString("N2")).Bold();
                        row.RelativeItem(2).AlignRight().Text(data.TotalTaxWithheld.ToString("N2")).Bold();
                    });
                    column.Item().PaddingTop(5).AlignCenter().Text($"ออกเอกสารเมื่อ {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                });
            });
        }).GeneratePdf();
    }
}
