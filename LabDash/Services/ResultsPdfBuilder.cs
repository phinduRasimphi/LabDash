using LabDash.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabDash.Services
{
    public static class ResultsPdfBuilder
    {
        public static byte[] Build(TestRequest request, List<TestResult> results)
        {
            var p = request.Patient;

            return Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(t => t.FontSize(10));

                    page.Header().Column(c =>
                    {
                        c.Item().Text("Laboratory Results").FontSize(20).Bold();
                        c.Item().PaddingTop(5).Text($"Patient: {p?.Name} {p?.Surname}");
                        c.Item().Text($"DOB: {p?.DOB:yyyy-MM-dd}");
                        c.Item().Text($"Request #{request.RequestId}  |  Requested: {request.RequestDate:yyyy-MM-dd}");
                        c.Item().PaddingTop(5).LineHorizontal(1);
                    });

                    page.Content().PaddingVertical(15).Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Text("Test").Bold();
                            h.Cell().Text("Result").Bold();
                            h.Cell().Text("Units").Bold();
                            h.Cell().Text("Reference").Bold();
                            h.Cell().Text("Verified").Bold();
                        });

                        foreach (var r in results)
                        {
                            t.Cell().PaddingVertical(3).Text(r.TestRequestItem?.TestType?.Name ?? "");

                            var resultCell = t.Cell().PaddingVertical(3);
                            if (r.IsAbnormal)
                                resultCell.Text($"{r.ResultValue} *").Bold().FontColor(Colors.Red.Medium);
                            else
                                resultCell.Text(r.ResultValue);

                            t.Cell().PaddingVertical(3).Text(r.Units ?? "");
                            t.Cell().PaddingVertical(3).Text(r.ReferenceRange ?? "");
                            t.Cell().PaddingVertical(3).Text(r.VerificationDate?.ToString("yyyy-MM-dd HH:mm") ?? "");
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("* = outside reference range").FontSize(8);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                    });
                });
            }).GeneratePdf();
        }
    }
}
