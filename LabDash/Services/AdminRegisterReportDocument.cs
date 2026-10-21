using LabDash.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabDash.Services
{
    public class RegisterReportRow
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";      // empty = uncategorised
        public string Description { get; set; } = "";
        public string Status { get; set; } = "Active";
    }

    /// <summary>
    /// Admin register report (PDF) for Allergies, Conditions or Medications.
    /// Same document structure as PatientReportDocument: title block with logo,
    /// generation details, summary, records grouped by category, repeating table
    /// headers, running header from page 2 and "Page X of Y" footer.
    /// </summary>
    public class AdminRegisterReportDocument : IDocument
    {
        private const string ColInk = "#1B2420";
        private const string ColMuted = "#5C6560";
        private const string ColLine = "#C9D1CD";
        private const string ColAccent = "#0A5850";
        private const string ColAmber = "#94650A";
        private const string ColHeadFill = "#EEF2F0";

        private const string Uncategorised = "Uncategorised";

        private readonly string _title;
        private readonly string _subtitle;
        private readonly string _noun;          // e.g. "allergies"
        private readonly List<RegisterReportRow> _rows;
        private readonly List<string> _filters;
        private readonly DateTime _generated;
        private readonly string _generatedBy;
        private readonly byte[]? _logo;
        private readonly bool _showStatus;

        public AdminRegisterReportDocument(
            string title,
            string subtitle,
            string noun,
            IEnumerable<RegisterReportRow> rows,
            List<string> filters,
            DateTime generated,
            string generatedBy,
            byte[]? logo = null)
        {
            _title = title;
            _subtitle = subtitle;
            _noun = noun;
            _filters = filters;
            _generated = generated;
            _generatedBy = generatedBy;
            _logo = logo;

            _rows = rows
                .OrderBy(r => string.IsNullOrWhiteSpace(r.Category) ? 1 : 0)
                .ThenBy(r => r.Category, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            _showStatus = _rows.Select(r => r.Status).Distinct().Count() > 1;
        }

        private static string CatName(RegisterReportRow r) =>
            string.IsNullOrWhiteSpace(r.Category) ? Uncategorised : r.Category;

        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = _title,
            Author = "LabDash",
            Subject = "Administration"
        };

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        // ============================================================
        // PAGE
        // ============================================================

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginVertical(36);
                page.MarginHorizontal(40);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(ColInk));

                // Page 1 carries the full title block, so the running header starts on page 2
                page.Header().SkipOnce().Element(RunningHeader);
                page.Content().Element(Body);
                page.Footer().Element(FooterBar);
            });
        }

        private void RunningHeader(IContainer c)
        {
            c.PaddingBottom(10).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(_title).Bold().FontSize(9);
                    row.AutoItem().Text($"Generated {_generated:d MMMM yyyy}").FontSize(9).FontColor(ColMuted);
                });

                col.Item().PaddingTop(4).LineHorizontal(0.75f).LineColor(ColLine);
            });
        }

        private void FooterBar(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(ColLine);

                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem()
                        .Text("LabDash |  Administration")
                        .FontSize(8).FontColor(ColMuted);

                    row.RelativeItem().AlignCenter()
                        .Text($"Generated {_generated:d MMMM yyyy}")
                        .FontSize(8).FontColor(ColMuted);

                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(8).FontColor(ColMuted));
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            });
        }

        // ============================================================
        // BODY
        // ============================================================

        private void Body(IContainer c)
        {
            c.Column(col =>
            {
                col.Spacing(12);

                col.Item().Element(TitleBlock);
                col.Item().Element(DetailsBlock);

                if (_rows.Count == 0)
                {
                    col.Item().Element(x => NoticeBox(
                        x,
                        "No records found for the selected filters.",
                        "There are no " + _noun + " matching the filters listed above. " +
                        "Clear or change the filters and generate the report again."));
                }
                else
                {
                    col.Item().Element(SummaryBlock);

                    col.Item().Element(x => SectionHeading(x, "Category Summary"));
                    col.Item().Element(CategorySummary);

                    col.Item().PaddingTop(4).Element(x => SectionHeading(x, "Register by Category"));

                    foreach (var g in _rows.GroupBy(CatName))
                    {
                        var list = g.ToList();
                        var meta = list.Count + (list.Count == 1 ? " record" : " records");

                        // Heading asks for room for itself plus the first rows, so it never sits alone at the bottom of a page
                        col.Item().EnsureSpace(110).Element(x => CategoryBar(x, g.Key, meta));
                        col.Item().Element(x => RegisterTable(x, list));
                    }
                }

                col.Item().PaddingTop(6).Element(Note);
            });
        }

        private void TitleBlock(IContainer c)
        {
            c.Column(col =>
            {
                if (_logo != null)
                {
                    col.Item().AlignLeft().Height(34)
                        .Image(_logo)
                        .FitHeight()
                        .WithCompressionQuality(ImageCompressionQuality.High);
                }
                else
                {
                    col.Item().Text("LabDash")
                        .FontSize(9).Bold().FontColor(ColAccent);
                }

                col.Item().PaddingTop(6).Text(_title).FontSize(20).Bold();
                col.Item().PaddingTop(2).Text(_subtitle).FontColor(ColMuted);
                col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(ColAccent);
            });
        }

        private void DetailsBlock(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        KeyValue(left, "Generated", $"{_generated:d MMMM yyyy} at {_generated:HH:mm}");
                        KeyValue(left, "Generated by", string.IsNullOrWhiteSpace(_generatedBy) ? "System" : _generatedBy);
                    });

                    row.RelativeItem().Column(right =>
                    {
                        KeyValue(right, "Subsystem", "Administration");
                        KeyValue(right, "Sorted by", "Category, then name");
                    });
                });

                col.Item().PaddingTop(2).Row(row =>
                {
                    row.ConstantItem(74).Text("Filters").FontColor(ColMuted);
                    row.RelativeItem()
                        .Text(_filters.Any() ? string.Join("; ", _filters) : "None (all records)")
                        .Bold();
                });
            });
        }

        private static void KeyValue(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(2).Row(row =>
            {
                row.ConstantItem(74).Text(label).FontColor(ColMuted);
                row.RelativeItem().Text(value).Bold();
            });
        }

        private void SummaryBlock(IContainer c)
        {
            var total = _rows.Count;
            var active = _rows.Count(r => r.Status == "Active");
            var archived = total - active;
            var categories = _rows.Where(r => !string.IsNullOrWhiteSpace(r.Category)).Select(r => r.Category).Distinct().Count();
            var noDesc = _rows.Count(r => string.IsNullOrWhiteSpace(r.Description));
            var uncategorised = _rows.Count(r => string.IsNullOrWhiteSpace(r.Category));

            var cells = new List<(string Label, string Value, bool Warn)>
            {
                ("Records in report", total.ToString(), false)
            };

            if (_showStatus)
            {
                cells.Add(("Active", active.ToString(), false));
                cells.Add(("Archived", archived.ToString(), false));
            }

            cells.Add(("Categories", categories.ToString(), false));
            cells.Add(("No description", noDesc.ToString(), noDesc > 0));
            cells.Add(("Uncategorised", uncategorised.ToString(), uncategorised > 0));

            c.Column(col =>
            {
                col.Item().Element(x => SectionHeading(x, "Summary"));

                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        for (var i = 0; i < cells.Count; i++) cd.RelativeColumn();
                    });

                    foreach (var cell in cells)
                    {
                        table.Cell()
                            .Border(0.5f).BorderColor(ColLine)
                            .Padding(6)
                            .Column(inner =>
                            {
                                inner.Item().Text(cell.Value).FontSize(13).Bold()
                                    .FontColor(cell.Warn ? ColAmber : ColInk);
                                inner.Item().Text(cell.Label).FontSize(8).FontColor(ColMuted);
                            });
                    }
                });
            });
        }

        private void CategorySummary(IContainer c)
        {
            var groups = _rows.GroupBy(CatName).ToList();
            var total = _rows.Count;

            c.Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(4);
                    cd.RelativeColumn(1.5f);
                    cd.RelativeColumn(1.5f);
                });

                table.Header(h =>
                {
                    h.Cell().Element(HeadCell).Text("Category").Bold();
                    h.Cell().Element(HeadCell).AlignRight().Text("Records").Bold();
                    h.Cell().Element(HeadCell).AlignRight().Text("Share").Bold();
                });

                foreach (var g in groups)
                {
                    var n = g.Count();

                    table.Cell().Element(BodyCell).Text(g.Key);
                    table.Cell().Element(BodyCell).AlignRight().Text(n.ToString());
                    table.Cell().Element(BodyCell).AlignRight().Text($"{Math.Round(100.0 * n / total)}%");
                }

                table.Cell().Element(TotalCell).Text("Total").Bold();
                table.Cell().Element(TotalCell).AlignRight().Text(total.ToString()).Bold();
                table.Cell().Element(TotalCell).AlignRight().Text("100%").Bold();
            });
        }

        private void RegisterTable(IContainer c, List<RegisterReportRow> rows)
        {
            c.Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(2);
                    cd.RelativeColumn(3);
                    if (_showStatus) cd.ConstantColumn(58);
                });

                // Header repeats automatically on every page the table spans
                table.Header(h =>
                {
                    h.Cell().Element(HeadCell).Text("Name").Bold();
                    h.Cell().Element(HeadCell).Text("Description").Bold();
                    if (_showStatus) h.Cell().Element(HeadCell).Text("Status").Bold();
                });

                foreach (var r in rows)
                {
                    var hasDesc = !string.IsNullOrWhiteSpace(r.Description);

                    table.Cell().Element(BodyCell).Text(r.Name).Bold();

                    table.Cell().Element(BodyCell)
                        .Text(hasDesc ? r.Description : "No description recorded")
                        .FontColor(hasDesc ? ColInk : ColMuted);

                    if (_showStatus)
                    {
                        table.Cell().Element(BodyCell)
                            .Text(r.Status)
                            .FontColor(r.Status == "Active" ? ColAccent : ColMuted);
                    }
                }
            });
        }

        // ============================================================
        // SHARED PIECES
        // ============================================================

        private static void SectionHeading(IContainer c, string text)
        {
            c.BorderBottom(0.75f)
                .BorderColor(ColLine)
                .PaddingBottom(3)
                .Text(text)
                .FontSize(12)
                .Bold()
                .FontColor(ColAccent);
        }

        private static void CategoryBar(IContainer c, string name, string meta)
        {
            c.Background(ColHeadFill)
                .BorderLeft(3).BorderColor(ColAccent)
                .PaddingVertical(5).PaddingHorizontal(8)
                .Row(row =>
                {
                    row.RelativeItem().Text(name).Bold().FontSize(10.5f);
                    row.AutoItem().Text(meta).FontColor(ColMuted);
                });
        }

        private static void NoticeBox(IContainer c, string title, string text)
        {
            c.Border(0.75f).BorderColor(ColLine)
                .Background(ColHeadFill)
                .Padding(14)
                .Column(col =>
                {
                    col.Item().Text(title).Bold().FontSize(11);
                    col.Item().PaddingTop(3).Text(text).FontColor(ColMuted);
                });
        }

        private static void Note(IContainer c)
        {
            c.Text("This report lists the records held in the system at the time it was generated. " +
                   "Archived records are kept for reference and are not used across the system.")
                .FontSize(8).Italic().FontColor(ColMuted);
        }

        private static IContainer HeadCell(IContainer c) =>
            c.Background(ColHeadFill)
                .BorderBottom(1).BorderColor(ColMuted)
                .PaddingVertical(4).PaddingHorizontal(5);

        private static IContainer BodyCell(IContainer c) =>
            c.Background("#FFFFFF")
                .BorderBottom(0.5f).BorderColor(ColLine)
                .PaddingVertical(4).PaddingHorizontal(5);

        private static IContainer TotalCell(IContainer c) =>
            c.BorderTop(1).BorderColor(ColMuted)
                .PaddingVertical(4).PaddingHorizontal(5);
    }
}