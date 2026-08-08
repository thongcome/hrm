namespace HRM.Services.Lms;

using HRM.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Renders a training completion certificate as an A4 PDF — mirrors
// SalaryCertificatePdfService.cs's static-class, no-DI, generate-fresh-
// every-download pattern (no PDF is ever stored permanently; the endpoint
// regenerates on each request).
public static class LmsCertificatePdfService
{
    public static byte[] Generate(Hremployee employee, Lms_Course course, Lms_CourseSession session, DateTime completedDate)
    {
        var employeeName = $"{employee.EmpName} {employee.EmpSurname}".Trim();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Content().Column(column =>
                {
                    column.Item().AlignCenter().PaddingTop(20).Text("ใบรับรองผลการอบรม").FontSize(22).Bold();
                    column.Item().AlignCenter().Text("Certificate of Completion").FontSize(14);

                    column.Item().PaddingTop(40).AlignCenter().Text("ขอมอบใบรับรองฉบับนี้เพื่อแสดงว่า").FontSize(13);
                    column.Item().PaddingTop(10).AlignCenter().Text(employeeName).FontSize(20).Bold();

                    column.Item().PaddingTop(20).AlignCenter().Text($"ได้สำเร็จหลักสูตร \"{course.Title}\"").FontSize(14);
                    column.Item().AlignCenter().Text($"รอบอบรม {session.SessionCode} วันที่ {FormatDate(session.StartDate)} — {FormatDate(session.EndDate)}").FontSize(11);

                    column.Item().PaddingTop(30).AlignCenter().Text($"สำเร็จหลักสูตรเมื่อวันที่ {completedDate:dd MMMM yyyy}").FontSize(12);

                    column.Item().PaddingTop(60).Row(row =>
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

    private static string FormatDate(DateOnly date) => date.ToString("dd MMMM yyyy");
}
