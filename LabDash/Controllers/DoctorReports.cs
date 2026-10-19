using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorReportsController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        private const string OrgName = "LabDash Haematology Laboratory";
        private const string ReportTitle = "Test Request Summary Report";

        public DoctorReportsController(LabDbContext context, UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Shared query (preview page AND PDF use it so they always match)
        // Sorted oldest first, then by patient surname.
        private async Task<List<TestRequest>> GetRequestsAsync(
            string doctorId, DateTime from, DateTime to, string? status)
        {
            DateTime toInclusive = to.AddDays(1);

            var query = _context.TestRequests
                .Include(r => r.Patient)
                .Include(r => r.TestRequestItems).ThenInclude(i => i.TestType)
                .Include(r => r.TestRequestItems).ThenInclude(i => i.TestResults)
                .Where(r => r.RequestingDoctorId == doctorId)
                .Where(r => r.RequestDate >= from && r.RequestDate < toInclusive);

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                query = query.Where(r => r.Status == status);

            return await query
                .OrderBy(r => r.RequestDate)
                .ThenBy(r => r.Patient!.Surname)
                .ToListAsync();
        }

        private static int AbnormalCount(TestRequest r) =>
            r.TestRequestItems?
                .SelectMany(i => i.TestResults ?? new List<TestResult>())
                .Count(tr => tr.IsAbnormal) ?? 0;

        // INDEX: on-screen preview with filters
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string? status)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            DateTime today = DateTime.Today;
            DateTime from = fromDate?.Date ?? today.AddDays(-30);
            DateTime to = toDate?.Date ?? today;

            var requests = await GetRequestsAsync(doctor.Id, from, to, status);

            var model = new DoctorReportViewModel
            {
                FromDate = from,
                ToDate = to,
                StatusFilter = status ?? "All",
                DoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}",
                DoctorHpcsa = doctor.HPCSANumber ?? "",
                Rows = requests.Select(r => new DoctorReportRow
                {
                    RequestId = r.RequestId,
                    RequestDate = r.RequestDate,
                    PatientName = $"{r.Patient?.Name} {r.Patient?.Surname}".Trim(),
                    PatientIdNum = r.Patient?.IDNumber ?? "",
                    Urgency = r.Urgency,
                    Status = r.Status,
                    TestsCount = r.TestRequestItems?.Count ?? 0,
                    AbnormalCount = AbnormalCount(r)
                }).ToList()
            };

            ViewBag.TotalRequests = model.Rows.Count;
            ViewBag.CompletedCount = model.Rows.Count(r => r.Status == "Completed" || r.Status == "Released by doctor");
            ViewBag.CancelledCount = model.Rows.Count(r => r.Status == "Cancelled");
            ViewBag.AbnormalTotal = model.Rows.Sum(r => r.AbnormalCount);

            return View(model);
        }

        // DOWNLOAD PDF
        [HttpGet]
        public async Task<IActionResult> DownloadPdf(DateTime? fromDate, DateTime? toDate, string? status)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            DateTime today = DateTime.Today;
            DateTime from = fromDate?.Date ?? today.AddDays(-30);
            DateTime to = toDate?.Date ?? today;

            var requests = await GetRequestsAsync(doctor.Id, from, to, status);

            string doctorName = $"Dr. {doctor.FirstName} {doctor.LastName}";
            string hpcsa = string.IsNullOrWhiteSpace(doctor.HPCSANumber) ? "Not recorded" : doctor.HPCSANumber;
            string statusFilter = string.IsNullOrWhiteSpace(status) || status == "All" ? "All statuses" : status;
            string period = $"{from:d MMMM yyyy} – {to:d MMMM yyyy}";
            DateTime generated = DateTime.Now;

            int total = requests.Count;
            int completed = requests.Count(r => r.Status == "Completed" || r.Status == "Released by doctor");
            int cancelled = requests.Count(r => r.Status == "Cancelled");
            int totalTests = requests.Sum(r => r.TestRequestItems?.Count ?? 0);
            int abnormalTotal = requests.Sum(AbnormalCount);

            var byStatus = requests.GroupBy(r => r.Status)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList();

            var byUrgency = requests.GroupBy(r => r.Urgency)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList();

            const string Dark = "#1f2937";
            const string Muted = "#6b7280";
            const string Line = "#9ca3af";
            const string HeadBg = "#e5e7eb";

            byte[] pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Calibri).FontColor(Dark));

                    // HEADER: compact running header from page 2 onwards
                    page.Header().ShowIf(ctx => ctx.PageNumber > 1).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(ReportTitle).Bold().FontSize(10);
                            row.RelativeItem().AlignRight()
                                .Text($"{doctorName}  |  {period}").FontSize(8).FontColor(Muted);
                        });
                        col.Item().PaddingTop(4).LineHorizontal(0.75f).LineColor(Line);
                    });

                    // CONTENT
                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Spacing(10);

                        // Title block
                        col.Item().Column(c =>
                        {
                            c.Item().Text(OrgName.ToUpper()).FontSize(9).FontColor(Muted).LetterSpacing(0.05f);
                            c.Item().PaddingTop(2).Text(ReportTitle).FontSize(20).Bold();
                            c.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Dark);
                        });

                        // Report information
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.ConstantColumn(110); c.RelativeColumn(); c.ConstantColumn(110); c.RelativeColumn(); });

                            void L(string s) => t.Cell().PaddingVertical(2).Text(s).FontColor(Muted);
                            void V(string s) => t.Cell().PaddingVertical(2).Text(s).Bold();

                            L("Requesting doctor:"); V(doctorName);
                            L("Date generated:"); V($"{generated:d MMMM yyyy}");
                            L("HPCSA number:"); V(hpcsa);
                            L("Time generated:"); V($"{generated:HH:mm}");
                            L("Reporting period:"); V(period);
                            L("Status filter:"); V(statusFilter);
                        });

                        // Empty state
                        if (requests.Count == 0)
                        {
                            col.Item().PaddingTop(30).AlignCenter().Column(c =>
                            {
                                c.Item().AlignCenter().Text("No records found for the selected date range.")
                                    .FontSize(12).Bold();
                                c.Item().AlignCenter().PaddingTop(4)
                                    .Text("Try widening the date range or changing the status filter.")
                                    .FontColor(Muted);
                            });
                            return;
                        }

                        // 1. Summary
                        col.Item().EnsureSpace(120).Column(c =>
                        {
                            c.Item().Text("1. Summary").FontSize(12).Bold();
                            c.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(Line);

                            c.Item().PaddingTop(6).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.ConstantColumn(60); });

                                void Row(string label, string value, bool bold = false)
                                {
                                    var l = t.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").PaddingVertical(4).Text(label);
                                    var v = t.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").PaddingVertical(4).AlignRight().Text(value);
                                    if (bold) { l.Bold(); v.Bold(); }
                                }

                                Row("Total test requests", total.ToString(), true);
                                Row("Completed / released", completed.ToString());
                                Row("Cancelled", cancelled.ToString());
                                Row("Total tests ordered", totalTests.ToString());
                                Row("Abnormal results", abnormalTotal.ToString(), true);
                                Row("Average tests per request", (total == 0 ? 0 : (double)totalTests / total).ToString("0.0"));
                            });
                        });

                        // 2. Breakdown
                        col.Item().EnsureSpace(120).Column(c =>
                        {
                            c.Item().Text("2. Breakdown").FontSize(12).Bold();
                            c.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(Line);

                            c.Item().PaddingTop(6).Row(row =>
                            {
                                void Block(IContainer cont, string heading, IEnumerable<(string Label, int Count)> items)
                                {
                                    cont.Column(bc =>
                                    {
                                        bc.Item().Text(heading).Bold().FontColor(Muted);
                                        bc.Item().PaddingTop(3).Table(t =>
                                        {
                                            t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.ConstantColumn(40); cd.ConstantColumn(45); });
                                            t.Header(h =>
                                            {
                                                h.Cell().Background(HeadBg).Padding(4).Text("").FontSize(8);
                                                h.Cell().Background(HeadBg).Padding(4).AlignRight().Text("Count").Bold().FontSize(8);
                                                h.Cell().Background(HeadBg).Padding(4).AlignRight().Text("%").Bold().FontSize(8);
                                            });
                                            foreach (var (label, count) in items)
                                            {
                                                t.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).Text(label);
                                                t.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).AlignRight().Text(count.ToString());
                                                t.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).AlignRight()
                                                    .Text($"{(double)count / total:P0}");
                                            }
                                        });
                                    });
                                }

                                Block(row.RelativeItem().PaddingRight(10), "By status",
                                    byStatus.Select(x => (x.Label, x.Count)));
                                Block(row.RelativeItem().PaddingLeft(10), "By urgency",
                                    byUrgency.Select(x => (x.Label, x.Count)));
                            });
                        });

                        // 3. Details
                        col.Item().EnsureSpace(100).Column(c =>
                        {
                            c.Item().Text("3. Test Request Details").FontSize(12).Bold();
                            c.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(Line);
                            c.Item().PaddingTop(2).Text("Sorted by request date (oldest first), then patient surname. Rows marked * contain abnormal results.")
                                .FontSize(8).FontColor(Muted);
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(28);    // No.
                                cd.ConstantColumn(66);    // Date
                                cd.RelativeColumn(3);     // Patient
                                cd.RelativeColumn(2.2f);  // ID number
                                cd.ConstantColumn(48);    // Urgency
                                cd.ConstantColumn(78);    // Status
                                cd.ConstantColumn(34);    // Tests
                                cd.ConstantColumn(50);    // Abnormal
                            });

                            // Header repeats automatically on every page
                            table.Header(h =>
                            {
                                void H(string t, bool right = false)
                                {
                                    var cell = h.Cell().Background(HeadBg).BorderBottom(1).BorderColor(Dark).Padding(5);
                                    var txt = right ? cell.AlignRight().Text(t) : cell.Text(t);
                                    txt.Bold().FontSize(8);
                                }
                                H("No."); H("Date"); H("Patient"); H("ID Number");
                                H("Urgency"); H("Status"); H("Tests", true); H("Abnormal", true);
                            });

                            int index = 1;
                            foreach (var r in requests)
                            {
                                int tests = r.TestRequestItems?.Count ?? 0;
                                int abnormal = AbnormalCount(r);
                                bool hasAbn = abnormal > 0;

                                void C(string text, bool right = false, bool bold = false)
                                {
                                    var cell = table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(5);
                                    var txt = right ? cell.AlignRight().Text(text) : cell.Text(text);
                                    txt.FontSize(8);
                                    if (bold) txt.Bold();
                                }

                                C(index.ToString());
                                C(r.RequestDate.ToString("dd MMM yyyy"));
                                C($"{r.Patient?.Name} {r.Patient?.Surname}".Trim());
                                C(string.IsNullOrWhiteSpace(r.Patient?.IDNumber) ? "—" : r.Patient!.IDNumber);
                                C(r.Urgency ?? "—");
                                C(r.Status ?? "—");
                                C(tests.ToString(), true);
                                C(hasAbn ? $"{abnormal} *" : "—", true, hasAbn);
                                index++;
                            }

                            // Totals row
                            void T(string text, bool right = false)
                            {
                                var cell = table.Cell().BorderTop(1).BorderColor(Dark).PaddingVertical(5).PaddingHorizontal(5);
                                (right ? cell.AlignRight().Text(text) : cell.Text(text)).Bold().FontSize(8);
                            }
                            T(""); T(""); T($"Total: {total} request(s)"); T(""); T(""); T("");
                            T(totalTests.ToString(), true);
                            T(abnormalTotal.ToString(), true);
                        });

                        col.Item().PaddingTop(10).Text("— End of report —")
                            .FontSize(8).FontColor(Muted).AlignCenter();
                    });

                    // FOOTER
                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(Line);
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text($"{OrgName}  |  Generated {generated:dd MMM yyyy HH:mm}")
                                .FontSize(8).FontColor(Muted);
                            row.ConstantItem(100).AlignRight().Text(t =>
                            {
                                t.DefaultTextStyle(s => s.FontSize(8).FontColor(Muted));
                                t.Span("Page ");
                                t.CurrentPageNumber();
                                t.Span(" of ");
                                t.TotalPages();
                            });
                        });
                    });
                });
            }).GeneratePdf();

            string filename = $"TestRequestSummary_{from:yyyyMMdd}_to_{to:yyyyMMdd}.pdf";
            return File(pdf, "application/pdf", filename);
        }
    }
}