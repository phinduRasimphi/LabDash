using System.Globalization;
using System.Text;
using LabDash.Helpers;
using LabDash.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabDash.Services
{
    /// <summary>
    /// Patient report (PDF): Laboratory Results, Health Trend or Abnormal Results.
    /// Built as a real document: title block with logo, generation details, summary,
    /// results grouped by category, repeating table headers, running header
    /// from page 2 and "Page X of Y" footer.
    /// </summary>
    public class PatientReportDocument : IDocument
    {
        private const string ColInk = "#1B2420";
        private const string ColMuted = "#5C6560";
        private const string ColLine = "#C9D1CD";
        private const string ColAccent = "#0A5850";
        private const string ColRed = "#A63A32";
        private const string ColGreen = "#227A4A";
        private const string ColRowRed = "#FCEEEC";
        private const string ColHeadFill = "#EEF2F0";

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private readonly string _patientName;
        private readonly string _idNumber;
        private readonly DateTime _from;
        private readonly DateTime _to;
        private readonly DateTime _generated;
        private readonly string _type;
        private readonly byte[]? _logo;                    // LabDash logo (PNG bytes), optional
        private readonly List<TestResultViewModel> _all;   // every result in the period
        private readonly List<TestResultViewModel> _rows;  // what this report lists, already sorted

        public PatientReportDocument(
            string patientName,
            string idNumber,
            DateTime from,
            DateTime to,
            string reportType,
            IEnumerable<TestResultViewModel> resultsInPeriod,
            DateTime generated,
            byte[]? logo = null)
        {
            _patientName = patientName;
            _idNumber = idNumber;
            _from = from;
            _to = to;
            _generated = generated;
            _logo = logo;
            _type = ReportTypes.Normalize(reportType);

            _all = resultsInPeriod.ToList();

            _rows = ReportHelper
                .OrderForReport(_type == ReportTypes.AbnormalResults
                    ? _all.Where(r => r.IsAbnormal)
                    : _all)
                .ToList();
        }

        private string Title => ReportTypes.Title(_type);

        private string Period => $"{_from:d MMMM yyyy} - {_to:d MMMM yyyy}";

        private string Subtitle => _type switch
        {
            ReportTypes.HealthTrend => "How your results for each test changed over the selected period.",
            ReportTypes.AbnormalResults => "Results that fall outside their reference range, grouped by test category.",
            _ => "All laboratory results released by your doctor, grouped by test category."
        };

        private string IncludesText => _type switch
        {
            ReportTypes.HealthTrend => "Released results, with trends",
            ReportTypes.AbnormalResults => "Abnormal results only",
            _ => "All released results"
        };

        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = Title,
            Author = "LabDash",
            Subject = Period
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
                    row.RelativeItem().Text(Title).Bold().FontSize(9);
                    row.AutoItem().Text($"{_patientName}  |  {Period}").FontSize(9).FontColor(ColMuted);
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
                        .Text("LabDash |  Patient Portal")
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
                col.Item().Element(SummaryBlock);

                if (_all.Count == 0)
                {
                    col.Item().Element(x => NoticeBox(
                        x,
                        "No records found for the selected date range.",
                        "There are no released laboratory results between " + Period +
                        ". Choose a wider date range and generate the report again."));
                }
                else if (_rows.Count == 0)
                {
                    col.Item().Element(x => NoticeBox(
                        x,
                        "No abnormal results found.",
                        "All " + _all.Count + " results in the selected date range were within their reference ranges."));
                }
                else if (_type == ReportTypes.HealthTrend)
                {
                    TrendBody(col);
                }
                else
                {
                    ResultsBody(col);
                }

                col.Item().PaddingTop(6).Element(Disclaimer);
            });
        }

        private void TitleBlock(IContainer c)
        {
            c.Column(col =>
            {
                if (_logo != null)
                {
                    // LabDash logo (icon + wordmark) replaces the plain text title
                    col.Item().AlignLeft().Height(34)
                        .Image(_logo)
                        .FitHeight()
                        .WithCompressionQuality(ImageCompressionQuality.High);
                }
                else
                {
                    // Fallback if the logo file could not be loaded
                    col.Item().Text("LabDash")
                        .FontSize(9).Bold().FontColor(ColAccent);
                }

                col.Item().PaddingTop(6).Text(Title).FontSize(20).Bold();

                col.Item().PaddingTop(2).Text(Subtitle).FontColor(ColMuted);

                col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(ColAccent);
            });
        }

        private void DetailsBlock(IContainer c)
        {
            c.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    KeyValue(col, "Patient", _patientName);
                    KeyValue(col, "ID number", _idNumber);
                    KeyValue(col, "Includes", IncludesText);
                });

                row.RelativeItem().Column(col =>
                {
                    KeyValue(col, "Report period", Period);
                    KeyValue(col, "Generated", $"{_generated:d MMMM yyyy} at {_generated:HH:mm}");
                    KeyValue(col, "Sorted by", "Category, test name, date");
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
            var abnormal = _all.Count(r => r.IsAbnormal);
            var normal = _all.Count - abnormal;
            var requests = _all.Select(r => r.RequestID).Distinct().Count();
            var categories = _all.Select(r => ReportHelper.CategoryName(r)).Distinct().Count();

            var abnormalText = _all.Count > 0
                ? $"{abnormal} ({(double)abnormal / _all.Count * 100:0}%)"
                : "0";

            c.Column(col =>
            {
                col.Item().Element(x => SectionHeading(x, "Summary"));

                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        for (var i = 0; i < 5; i++) cd.RelativeColumn();
                    });

                    SummaryCell(table, "Results in period", _all.Count.ToString());
                    SummaryCell(table, "Normal", normal.ToString());
                    SummaryCell(table, "Abnormal", abnormalText);
                    SummaryCell(table, "Test requests", requests.ToString());
                    SummaryCell(table, "Categories", categories.ToString());
                });
            });
        }

        private static void SummaryCell(TableDescriptor table, string label, string value)
        {
            table.Cell()
                .Border(0.5f).BorderColor(ColLine)
                .Padding(6)
                .Column(col =>
                {
                    col.Item().Text(value).FontSize(13).Bold();
                    col.Item().Text(label).FontSize(8).FontColor(ColMuted);
                });
        }

        // ============================================================
        // COMPLETE RESULTS + ABNORMAL RESULTS
        // ============================================================

        private void ResultsBody(ColumnDescriptor col)
        {
            col.Item().Element(x => SectionHeading(x, "Category Summary"));
            col.Item().Element(CategorySummary);

            col.Item().PaddingTop(4).Element(x => SectionHeading(
                x,
                _type == ReportTypes.AbnormalResults ? "Abnormal Results by Category" : "Results by Category"));

            foreach (var g in _rows.GroupBy(r => ReportHelper.CategoryName(r)))
            {
                var name = g.Key;
                var list = g.ToList();
                var abn = list.Count(r => r.IsAbnormal);

                var meta = _type == ReportTypes.AbnormalResults
                    ? list.Count + (list.Count == 1 ? " abnormal result" : " abnormal results")
                    : list.Count + (list.Count == 1 ? " result" : " results") + ", " + abn + " abnormal";

                // The heading asks for room for itself plus the first rows, so it never sits alone at the bottom of a page
                col.Item().EnsureSpace(110).Element(x => CategoryBar(x, name, meta));
                col.Item().Element(x => ResultsTable(x, list));
            }
        }

        private void CategorySummary(IContainer c)
        {
            var groups = _all
                .GroupBy(r => ReportHelper.CategoryName(r))
                .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            c.Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(4);
                    cd.RelativeColumn(1.5f);
                    cd.RelativeColumn(1.5f);
                    cd.RelativeColumn(1.5f);
                });

                table.Header(h =>
                {
                    h.Cell().Element(HeadCell).Text("Category").Bold();
                    h.Cell().Element(HeadCell).AlignRight().Text("Results").Bold();
                    h.Cell().Element(HeadCell).AlignRight().Text("Normal").Bold();
                    h.Cell().Element(HeadCell).AlignRight().Text("Abnormal").Bold();
                });

                foreach (var g in groups)
                {
                    var n = g.Count();
                    var ab = g.Count(r => r.IsAbnormal);

                    table.Cell().Element(x => BodyCell(x)).Text(g.Key);
                    table.Cell().Element(x => BodyCell(x)).AlignRight().Text(n.ToString());
                    table.Cell().Element(x => BodyCell(x)).AlignRight().Text((n - ab).ToString());
                    table.Cell().Element(x => BodyCell(x)).AlignRight().Text(ab.ToString()).FontColor(ab > 0 ? ColRed : ColInk);
                }

                var totalAb = _all.Count(r => r.IsAbnormal);

                table.Cell().Element(x => TotalCell(x)).Text("Total").Bold();
                table.Cell().Element(x => TotalCell(x)).AlignRight().Text(_all.Count.ToString()).Bold();
                table.Cell().Element(x => TotalCell(x)).AlignRight().Text((_all.Count - totalAb).ToString()).Bold();
                table.Cell().Element(x => TotalCell(x)).AlignRight().Text(totalAb.ToString()).Bold();
            });
        }

        private void ResultsTable(IContainer c, List<TestResultViewModel> rows)
        {
            c.Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.ConstantColumn(70);   // date
                    cd.ConstantColumn(50);   // request
                    cd.RelativeColumn(3);    // test
                    cd.RelativeColumn(2.2f); // result
                    cd.RelativeColumn(2.2f); // reference range
                    cd.ConstantColumn(54);   // status
                });

                // Header repeats automatically on every page the table spans
                table.Header(h =>
                {
                    h.Cell().Element(HeadCell).Text("Date").Bold();
                    h.Cell().Element(HeadCell).Text("Request").Bold();
                    h.Cell().Element(HeadCell).Text("Test").Bold();
                    h.Cell().Element(HeadCell).Text("Result").Bold();
                    h.Cell().Element(HeadCell).Text("Reference range").Bold();
                    h.Cell().Element(HeadCell).Text("Status").Bold();
                });

                foreach (var r in rows)
                {
                    var abn = r.IsAbnormal;
                    var range = ReportHelper.RangeText(r);
                    var rangeText = range == "-" ? "-" : (range + " " + r.Unit).Trim();

                    table.Cell().Element(x => BodyCell(x, abn)).Text(r.ResultDate.ToString("dd MMM yyyy"));
                    table.Cell().Element(x => BodyCell(x, abn)).Text("#" + r.RequestID);
                    table.Cell().Element(x => BodyCell(x, abn)).Text(r.TestName);

                    var value = table.Cell().Element(x => BodyCell(x, abn)).Text(ReportHelper.ValueText(r));
                    if (abn) value.Bold();

                    table.Cell().Element(x => BodyCell(x, abn)).Text(rangeText).FontColor(ColMuted);

                    table.Cell().Element(x => BodyCell(x, abn))
                        .Text(ReportHelper.Flag(r))
                        .Bold()
                        .FontColor(abn ? ColRed : ColGreen);
                }
            });
        }

        // ============================================================
        // HEALTH TREND
        // ============================================================

        private void TrendBody(ColumnDescriptor col)
        {
            var trends = ReportHelper.BuildTrends(_all);

            col.Item().Element(x => SectionHeading(x, "Trend Overview"));
            col.Item().Element(x => TrendOverviewTable(x, trends));

            col.Item().PaddingTop(4).Element(x => SectionHeading(x, "Detailed Trends by Test"));

            foreach (var cat in trends.GroupBy(t => t.Category))
            {
                var catName = cat.Key;
                var catTrends = cat.ToList();

                // Enough room for the heading plus the first chart block
                col.Item().EnsureSpace(300).Element(x => CategoryBar(
                    x, catName, catTrends.Count + (catTrends.Count == 1 ? " test" : " tests")));

                foreach (var t in catTrends)
                {
                    var trend = t;
                    col.Item().EnsureSpace(230).Element(x => TrendBlock(x, trend));
                }
            }
        }

        private void TrendOverviewTable(IContainer c, List<TestTrend> trends)
        {
            c.Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(3);    // test
                    cd.ConstantColumn(48);   // readings
                    cd.RelativeColumn(2);    // first
                    cd.RelativeColumn(2);    // latest
                    cd.ConstantColumn(50);   // change
                    cd.ConstantColumn(82);   // trend
                });

                table.Header(h =>
                {
                    h.Cell().Element(HeadCell).Text("Test").Bold();
                    h.Cell().Element(HeadCell).AlignRight().Text("Readings").Bold();
                    h.Cell().Element(HeadCell).Text("First").Bold();
                    h.Cell().Element(HeadCell).Text("Latest").Bold();
                    h.Cell().Element(HeadCell).AlignRight().Text("Change").Bold();
                    h.Cell().Element(HeadCell).Text("Trend").Bold();
                });

                foreach (var t in trends)
                {
                    var abn = t.AbnormalCount > 0;

                    table.Cell().Element(x => BodyCell(x)).Text(t.TestName);
                    table.Cell().Element(x => BodyCell(x)).AlignRight().Text(t.Readings.Count.ToString());
                    table.Cell().Element(x => BodyCell(x)).Text(t.FirstText);
                    table.Cell().Element(x => BodyCell(x)).Text(t.LatestText).Bold();
                    table.Cell().Element(x => BodyCell(x)).AlignRight().Text(t.ChangeText);
                    table.Cell().Element(x => BodyCell(x)).Text(t.Direction).FontColor(abn ? ColRed : ColInk);
                }
            });
        }

        private void TrendBlock(IContainer c, TestTrend t)
        {
            c.Column(col =>
            {
                col.Spacing(4);

                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(t.TestName).Bold().FontSize(10.5f);

                    row.AutoItem().Text("Reference range: " +
                        (t.RangeText == "-" ? "not set" : (t.RangeText + " " + t.Unit).Trim()))
                        .FontColor(ColMuted);
                });

                col.Item().Text(TrendSummaryLine(t)).FontColor(ColMuted);

                if (t.CanChart)
                {
                    col.Item().Element(x => TrendChart(x, t));
                }
                else
                {
                    var why = t.Readings.Count < 2
                        ? "Only one result in this period, so no trend can be shown."
                        : "These results are not numeric, so no chart is shown.";

                    col.Item().Text(why).Italic().FontColor(ColMuted);
                }

                col.Item().Element(x => ReadingsTable(x, t));
            });
        }

        private static string TrendSummaryLine(TestTrend t)
        {
            var n = t.Readings.Count;
            var readings = n + (n == 1 ? " reading" : " readings");

            if (t.Points.Count == 0) return readings;

            var unit = string.IsNullOrWhiteSpace(t.Unit) ? "" : " " + t.Unit;

            var line = readings +
                "  |  Lowest " + Num(t.Lowest!.Value) + unit +
                "  |  Highest " + Num(t.Highest!.Value) + unit +
                "  |  Average " + Num(t.Average!.Value) + unit;

            if (t.Readings.Count >= 2 && t.Points.Count >= 2) line += "  |  Trend: " + t.Direction;

            return line;
        }

        private void TrendChart(IContainer c, TestTrend t)
        {
            const float chartHeight = 110;

            var (lo, hi) = Scale(t);
            var first = t.Points[0].Date;
            var last = t.Points[t.Points.Count - 1].Date;

            c.Column(col =>
            {
                col.Item().Row(row =>
                {
                    // Axis labels are real PDF text (not SVG text), so they always render
                    row.ConstantItem(46).Height(chartHeight).Column(axis =>
                    {
                        axis.Item().Height(11).AlignRight().PaddingRight(4)
                            .Text(Num(hi)).FontSize(7).FontColor(ColMuted);

                        axis.Item().Height(88);

                        axis.Item().Height(11).AlignRight().PaddingRight(4)
                            .Text(Num(lo)).FontSize(7).FontColor(ColMuted);
                    });

                    row.RelativeItem().Height(chartHeight)
                        .Svg(size => BuildChartSvg(t, size.Width, size.Height));
                });

                col.Item().PaddingLeft(46).Row(row =>
                {
                    row.RelativeItem().Text(first.ToString("dd MMM yyyy")).FontSize(7).FontColor(ColMuted);

                    if (t.HasRange)
                    {
                        row.AutoItem().Text("Shaded band: reference range  |  Red dot: abnormal")
                            .FontSize(7).FontColor(ColMuted);
                    }

                    row.RelativeItem().AlignRight().Text(last.ToString("dd MMM yyyy")).FontSize(7).FontColor(ColMuted);
                });
            });
        }

        private void ReadingsTable(IContainer c, TestTrend t)
        {
            c.Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.ConstantColumn(80);
                    cd.ConstantColumn(60);
                    cd.RelativeColumn();
                    cd.ConstantColumn(70);
                });

                table.Header(h =>
                {
                    h.Cell().Element(HeadCell).Text("Date").Bold();
                    h.Cell().Element(HeadCell).Text("Request").Bold();
                    h.Cell().Element(HeadCell).Text("Result").Bold();
                    h.Cell().Element(HeadCell).Text("Status").Bold();
                });

                foreach (var r in t.Readings)
                {
                    var abn = r.IsAbnormal;

                    table.Cell().Element(x => BodyCell(x, abn)).Text(r.ResultDate.ToString("dd MMM yyyy"));
                    table.Cell().Element(x => BodyCell(x, abn)).Text("#" + r.RequestID);

                    var value = table.Cell().Element(x => BodyCell(x, abn)).Text(ReportHelper.ValueText(r));
                    if (abn) value.Bold();

                    table.Cell().Element(x => BodyCell(x, abn))
                        .Text(ReportHelper.Flag(r))
                        .Bold()
                        .FontColor(abn ? ColRed : ColGreen);
                }
            });
        }

        private static (double lo, double hi) Scale(TestTrend t)
        {
            var lo = t.Points.Min(p => p.Value);
            var hi = t.Points.Max(p => p.Value);

            if (t.HasRange)
            {
                lo = Math.Min(lo, t.RefMin);
                hi = Math.Max(hi, t.RefMax);
            }

            if (hi - lo < 1e-9)
            {
                lo -= 1;
                hi += 1;
            }

            var pad = (hi - lo) * 0.08;
            return (lo - pad, hi + pad);
        }

        // Shapes only (no SVG text), so it renders the same on any server
        private static string BuildChartSvg(TestTrend t, float w, float h)
        {
            var (lo, hi) = Scale(t);

            const float top = 5, bottom = 5, left = 6, right = 8;
            var plotW = w - left - right;
            var plotH = h - top - bottom;

            double d0 = t.Points[0].Date.Ticks;
            double d1 = t.Points[t.Points.Count - 1].Date.Ticks;

            float X(DateTime d) =>
                (float)(left + (d1 - d0 < 1 ? plotW / 2 : (d.Ticks - d0) / (d1 - d0) * plotW));

            float Y(double v) =>
                (float)(top + (hi - v) / (hi - lo) * plotH);

            var sb = new StringBuilder();

            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{N(w)}\" height=\"{N(h)}\" viewBox=\"0 0 {N(w)} {N(h)}\">");

            // frame
            sb.Append($"<rect x=\"0.5\" y=\"0.5\" width=\"{N(w - 1)}\" height=\"{N(h - 1)}\" fill=\"#FFFFFF\" stroke=\"{ColLine}\" stroke-width=\"0.75\"/>");

            // reference range band
            if (t.HasRange)
            {
                var yTop = Math.Max(1, Y(t.RefMax));
                var yBottom = Math.Min(h - 1, Y(t.RefMin));

                if (yBottom > yTop)
                {
                    sb.Append($"<rect x=\"1\" y=\"{N(yTop)}\" width=\"{N(w - 2)}\" height=\"{N(yBottom - yTop)}\" fill=\"#E3F4EA\"/>");
                }
            }

            // light grid
            for (var i = 1; i <= 3; i++)
            {
                var gy = top + plotH * i / 4f;
                sb.Append($"<line x1=\"1\" y1=\"{N(gy)}\" x2=\"{N(w - 1)}\" y2=\"{N(gy)}\" stroke=\"#E1E6E3\" stroke-width=\"0.5\"/>");
            }

            // line
            var points = string.Join(" ", t.Points.Select(p => N(X(p.Date)) + "," + N(Y(p.Value))));
            sb.Append($"<polyline fill=\"none\" stroke=\"#0E8C7F\" stroke-width=\"1.5\" points=\"{points}\"/>");

            // markers
            foreach (var p in t.Points)
            {
                var fill = p.IsAbnormal ? "#A63A32" : "#0E8C7F";
                sb.Append($"<circle cx=\"{N(X(p.Date))}\" cy=\"{N(Y(p.Value))}\" r=\"3\" fill=\"{fill}\" stroke=\"#FFFFFF\" stroke-width=\"0.75\"/>");
            }

            sb.Append("</svg>");

            return sb.ToString();
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

        private static void Disclaimer(IContainer c)
        {
            c.Text("This report is a summary of laboratory results released by your doctor. " +
                   "It is not a diagnosis. Please discuss any result with your doctor.")
                .FontSize(8).Italic().FontColor(ColMuted);
        }

        private static IContainer HeadCell(IContainer c) =>
            c.Background(ColHeadFill)
                .BorderBottom(1).BorderColor(ColMuted)
                .PaddingVertical(4).PaddingHorizontal(5);

        private static IContainer BodyCell(IContainer c, bool abnormal = false) =>
            c.Background(abnormal ? ColRowRed : "#FFFFFF")
                .BorderBottom(0.5f).BorderColor(ColLine)
                .PaddingVertical(4).PaddingHorizontal(5);

        private static IContainer TotalCell(IContainer c) =>
            c.BorderTop(1).BorderColor(ColMuted)
                .PaddingVertical(4).PaddingHorizontal(5);

        private static string N(double v) => v.ToString("0.##", Inv);

        private static string Num(double v) => v.ToString("0.##", Inv);
    }
}