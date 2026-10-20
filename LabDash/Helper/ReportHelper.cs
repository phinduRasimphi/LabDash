using System.Globalization;
using LabDash.Models;

namespace LabDash.Helpers
{
    public class TrendPoint
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
        public bool IsAbnormal { get; set; }
    }

    public class TestTrend
    {
        public string Category { get; set; } = "";
        public string TestName { get; set; } = "";
        public string Unit { get; set; } = "";
        public string RangeText { get; set; } = "-";
        public bool HasRange { get; set; }
        public double RefMin { get; set; }
        public double RefMax { get; set; }

        // Every reading for this test, oldest first
        public List<TestResultViewModel> Readings { get; set; } = new();

        // Numeric readings only (used for the chart)
        public List<TrendPoint> Points { get; set; } = new();

        public int AbnormalCount { get; set; }

        public string FirstText { get; set; } = "-";
        public string LatestText { get; set; } = "-";
        public string ChangeText { get; set; } = "-";

        // up | down | flat | none
        public string DirectionKey { get; set; } = "none";
        public string Direction { get; set; } = "Not enough data";

        public double? Lowest { get; set; }
        public double? Highest { get; set; }
        public double? Average { get; set; }

        public bool CanChart => Points.Count >= 2;
    }

    public static class ReportHelper
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string CategoryName(TestResultViewModel r) =>
            string.IsNullOrWhiteSpace(r.Category) ? "Uncategorised" : r.Category.Trim();

        public static bool TryNumber(string? text, out double value)
        {
            var t = (text ?? "").Trim().Replace(',', '.');
            return double.TryParse(t, NumberStyles.Float, Inv, out value);
        }

        public static bool HasRange(TestResultViewModel r) => r.NormalMin + r.NormalMax > 0;

        public static string RangeText(TestResultViewModel r) =>
            HasRange(r)
                ? r.NormalMin.ToString("0.##", Inv) + " - " + r.NormalMax.ToString("0.##", Inv)
                : "-";

        public static string ValueText(TestResultViewModel r) =>
            (r.ResultValue + " " + r.Unit).Trim();

        // "Normal", or "High" / "Low" when the numeric value is outside its range
        public static string Flag(TestResultViewModel r)
        {
            if (!r.IsAbnormal) return "Normal";

            if (HasRange(r) && TryNumber(r.ResultValue, out var v))
            {
                if (v > r.NormalMax) return "High";
                if (v < r.NormalMin) return "Low";
            }

            return "Abnormal";
        }

        // Category, then test name, then date: the order used everywhere in the report
        public static IEnumerable<TestResultViewModel> OrderForReport(IEnumerable<TestResultViewModel> source) =>
            source
                .OrderBy(r => CategoryName(r), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(r => r.TestName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(r => r.ResultDate);

        public static List<TestTrend> BuildTrends(IEnumerable<TestResultViewModel> results)
        {
            return results
                .GroupBy(r => new { Cat = CategoryName(r), Name = r.TestName ?? "Unknown Test" })
                .OrderBy(g => g.Key.Cat, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(g => g.Key.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(g =>
                {
                    var readings = g.OrderBy(r => r.ResultDate).ToList();
                    var last = readings[readings.Count - 1];

                    var t = new TestTrend
                    {
                        Category = g.Key.Cat,
                        TestName = g.Key.Name,
                        Unit = last.Unit ?? "",
                        RangeText = RangeText(last),
                        HasRange = HasRange(last),
                        RefMin = last.NormalMin,
                        RefMax = last.NormalMax,
                        Readings = readings,
                        AbnormalCount = readings.Count(r => r.IsAbnormal),
                        FirstText = ValueText(readings[0]),
                        LatestText = ValueText(last)
                    };

                    foreach (var r in readings)
                    {
                        if (TryNumber(r.ResultValue, out var v))
                        {
                            t.Points.Add(new TrendPoint
                            {
                                Date = r.ResultDate,
                                Value = v,
                                IsAbnormal = r.IsAbnormal
                            });
                        }
                    }

                    if (t.Points.Count > 0)
                    {
                        t.Lowest = t.Points.Min(p => p.Value);
                        t.Highest = t.Points.Max(p => p.Value);
                        t.Average = t.Points.Average(p => p.Value);
                    }

                    if (readings.Count < 2)
                    {
                        t.Direction = "Single reading";
                    }
                    else if (t.Points.Count < 2)
                    {
                        t.Direction = "Not numeric";
                    }
                    else
                    {
                        var first = t.Points[0].Value;
                        var latest = t.Points[t.Points.Count - 1].Value;
                        var delta = latest - first;

                        var pct = first != 0
                            ? delta / Math.Abs(first) * 100
                            : (delta == 0 ? 0 : 100 * Math.Sign(delta));

                        t.ChangeText = delta.ToString("+0.##;-0.##;0", Inv);

                        if (pct > 5) { t.DirectionKey = "up"; t.Direction = "Rising"; }
                        else if (pct < -5) { t.DirectionKey = "down"; t.Direction = "Falling"; }
                        else { t.DirectionKey = "flat"; t.Direction = "Stable"; }
                    }

                    return t;
                })
                .ToList();
        }
    }
}