namespace HRM.Services.Pay;

using HRM.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Renders one Pay_PayrollEmployee's payslip as a password-protected PDF.
// QuestPDF's document builder has no direct "encrypted output" option, so
// this generates the plain PDF first, then runs QuestPDF's separate
// DocumentOperation.Encrypt step over a temp file (its encryption API only
// operates on files, not in-memory byte arrays).
public static class PayslipPdfService
{
    public static byte[] Generate(
        Pay_PayrollEmployee employee,
        List<Pay_PayrollLineItem> lineItems,
        string companyName,
        string password)
    {
        var plainBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(companyName).FontSize(14).Bold();
                    column.Item().Text($"สลิปเงินเดือน — งวด {employee.Pay_PayrollRun.PayrollPeriod}").FontSize(11);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Text($"รหัสพนักงาน: {employee.EmpNo}    ชื่อ: {employee.Hremployee.EmpName} {employee.Hremployee.EmpSurname}");
                    column.Item().PaddingTop(5).LineHorizontal(1);

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("รายการ").Bold();
                            header.Cell().AlignRight().Text("จำนวนเงิน").Bold();
                        });

                        foreach (var line in lineItems.OrderBy(l => l.SeqNo))
                        {
                            var label = line.SignFlag > 0 ? line.Pay_PayItemType.NameTh : $"หัก: {line.Pay_PayItemType.NameTh}";
                            table.Cell().Text(label);
                            table.Cell().AlignRight().Text((line.SignFlag * line.Amount).ToString("N2"));
                        }
                    });

                    column.Item().PaddingTop(10).LineHorizontal(1);
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("รายรับรวม").Bold();
                        row.RelativeItem().AlignRight().Text(employee.GrossEarnings.ToString("N2")).Bold();
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("รายหักรวม").Bold();
                        row.RelativeItem().AlignRight().Text(employee.TotalDeductions.ToString("N2")).Bold();
                    });
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("สุทธิ").FontSize(12).Bold();
                        row.RelativeItem().AlignRight().Text(employee.NetPay.ToString("N2")).FontSize(12).Bold();
                    });
                });

                page.Footer().AlignCenter().Text($"สร้างเมื่อ {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
            });
        }).GeneratePdf();

        return EncryptWithPassword(plainBytes, password);
    }

    private static byte[] EncryptWithPassword(byte[] plainPdfBytes, string password)
    {
        var tempIn = Path.GetTempFileName();
        var tempOut = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempIn, plainPdfBytes);

            DocumentOperation
                .LoadFile(tempIn)
                .Encrypt(new DocumentOperation.Encryption128Bit
                {
                    UserPassword = password,
                    OwnerPassword = password,
                    AllowPrinting = true,
                    AllowContentExtraction = false,
                    AllowFillingForms = false,
                    AllowAssembly = false,
                    AllowAnnotation = false,
                    EncryptMetadata = true,
                })
                .Save(tempOut);

            return File.ReadAllBytes(tempOut);
        }
        finally
        {
            File.Delete(tempIn);
            File.Delete(tempOut);
        }
    }
}
