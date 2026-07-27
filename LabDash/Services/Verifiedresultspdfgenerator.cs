using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabDash.Services
{
    public interface IVerifiedResultsPdfGenerator
    {
        // reportRows: one row per TestRequestItem on the request, already
        // resolved by the caller (see VerifiedResultsNotificationService)
        // so this class only deals with plain data, not EF/DbContext.
        byte[] Generate(VerifiedResultsReportData data);
    }

    // Plain data shape the PDF generator needs — deliberately decoupled
    // from EF entities so this class has no DbContext dependency and is
    // easy to unit test.
    public class VerifiedResultsReportData
    {
        public int RequestId { get; set; }
        public string PatientFullName { get; set; } = string.Empty;
        public string? PatientIdNumber { get; set; }
        public string? RequestingDoctorFullName { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public List<VerifiedResultRow> Rows { get; set; } = new();
    }

    public class VerifiedResultRow
    {
        public string TestName { get; set; } = string.Empty;
        public string? ResultValue { get; set; }
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public bool IsAbnormal { get; set; }
        public string? Comments { get; set; }
        public string? VerifiedByFullName { get; set; }
        public DateTime? VerificationDate { get; set; }
    }

    public class VerifiedResultsPdfGenerator : IVerifiedResultsPdfGenerator
    {
        public byte[] Generate(VerifiedResultsReportData data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Calibri));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Laboratory Results Report")
                            .FontSize(18).Bold().FontColor("#00695C");

                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Request Number: TR-{data.RequestId}").Bold();
                                c.Item().Text($"Patient: {data.PatientFullName}");
                                if (!string.IsNullOrWhiteSpace(data.PatientIdNumber))
                                {
                                    c.Item().Text($"ID Number: {data.PatientIdNumber}");
                                }
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                if (!string.IsNullOrWhiteSpace(data.RequestingDoctorFullName))
                                {
                                    c.Item().Text($"Requesting Doctor: {data.RequestingDoctorFullName}");
                                }
                                c.Item().Text($"Report Generated: {data.GeneratedAt:dd MMM yyyy HH:mm}");
                            });
                        });

                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#009688");
                    });

                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                void HeaderCell(string text) =>
                                    header.Cell().Background("#009688").Padding(6)
                                        .Text(text).FontColor(Colors.White).Bold();

                                HeaderCell("Test");
                                HeaderCell("Result");
                                HeaderCell("Units");
                                HeaderCell("Reference Range");
                                HeaderCell("Flag");
                            });

                            foreach (var row in data.Rows)
                            {
                                var bg = row.IsAbnormal ? "#FDECEC" : "#FFFFFF";

                                table.Cell().Background(bg).Padding(6).Text(row.TestName);
                                table.Cell().Background(bg).Padding(6).Text(row.ResultValue ?? "-");
                                table.Cell().Background(bg).Padding(6).Text(row.Units ?? "-");
                                table.Cell().Background(bg).Padding(6).Text(row.ReferenceRange ?? "-");
                                table.Cell().Background(bg).Padding(6)
                                    .Text(row.IsAbnormal ? "Abnormal" : "Normal")
                                    .FontColor(row.IsAbnormal ? "#E13B3B" : "#188A52")
                                    .Bold();
                            }
                        });

                        col.Item().PaddingTop(20).Text("Comments & Verification").Bold().FontSize(12);

                        foreach (var row in data.Rows)
                        {
                            col.Item().PaddingTop(8).Column(c =>
                            {
                                c.Item().Text(row.TestName).Bold();

                                c.Item().Text(!string.IsNullOrWhiteSpace(row.Comments)
                                    ? row.Comments
                                    : "No laboratory comments captured.");

                                if (row.VerifiedByFullName != null)
                                {
                                    c.Item().PaddingTop(2).Text(
                                        $"Verified by {row.VerifiedByFullName}" +
                                        (row.VerificationDate.HasValue
                                            ? $" on {row.VerificationDate.Value:dd MMM yyyy HH:mm}"
                                            : ""))
                                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated automatically by LabDash — ").FontSize(8);
                        x.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm")).FontSize(8);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
