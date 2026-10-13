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

        public DoctorReportsController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =============================================================
        // INDEX — report preview page with date range filter
        // =============================================================
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string? status)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            DateTime today = DateTime.Today;
            DateTime effectiveFrom = fromDate?.Date ?? today.AddDays(-30);
            DateTime effectiveTo = toDate?.Date ?? today;
            DateTime toInclusive = effectiveTo.AddDays(1);

            var query = _context.TestRequests
                .Include(r => r.Patient)
                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .Where(r => r.RequestingDoctorId == doctor.Id)
                .Where(r => r.RequestDate >= effectiveFrom && r.RequestDate < toInclusive);

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(r => r.Status == status);
            }

            var requests = await query
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            var model = new DoctorReportViewModel
            {
                FromDate = effectiveFrom,
                ToDate = effectiveTo,
                StatusFilter = status ?? "All",
                DoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}",
                DoctorHpcsa = doctor.HPCSANumber ?? "",
                Rows = requests.Select(r =>
                {
                    int totalItems = r.TestRequestItems?.Count ?? 0;
                    int abnormalItems = r.TestRequestItems?
                        .Count(i => i.TestResults != null &&
                                    i.TestResults.Any(tr => tr.IsAbnormal)) ?? 0;

                    return new DoctorReportRow
                    {
                        RequestId = r.RequestId,
                        RequestDate = r.RequestDate,
                        PatientName = $"{r.Patient?.Name} {r.Patient?.Surname}",
                        PatientIdNum = r.Patient?.IDNumber ?? "",
                        Urgency = r.Urgency,
                        Status = r.Status,
                        TestsCount = totalItems,
                        AbnormalCount = abnormalItems
                    };
                }).ToList()
            };

            ViewBag.TotalRequests = model.Rows.Count;
            ViewBag.CompletedCount = model.Rows.Count(r => r.Status == "Completed" || r.Status == "Released by doctor");
            ViewBag.CancelledCount = model.Rows.Count(r => r.Status == "Cancelled");
            ViewBag.AbnormalTotal = model.Rows.Sum(r => r.AbnormalCount);

            return View(model);
        }

        // =============================================================
        // DOWNLOAD PDF
        // =============================================================
        [HttpGet]
        public async Task<IActionResult> DownloadPdf(DateTime? fromDate, DateTime? toDate, string? status)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            DateTime today = DateTime.Today;
            DateTime effectiveFrom = fromDate?.Date ?? today.AddDays(-30);
            DateTime effectiveTo = toDate?.Date ?? today;
            DateTime toInclusive = effectiveTo.AddDays(1);

            var query = _context.TestRequests
                .Include(r => r.Patient)
                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestResults)
                .Where(r => r.RequestingDoctorId == doctor.Id)
                .Where(r => r.RequestDate >= effectiveFrom && r.RequestDate < toInclusive);

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(r => r.Status == status);
            }

            var requests = await query
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            string doctorName = $"Dr. {doctor.FirstName} {doctor.LastName}";
            string hpcsa = string.IsNullOrWhiteSpace(doctor.HPCSANumber) ? "" : $"HPCSA: {doctor.HPCSANumber}";

            byte[] pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                    page.Header().Column(col =>
                    {
                        col.Item().Background("#0d6b63").Padding(16).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("NMB HAEMATOLOGY LABORATORY")
                                    .FontSize(14).Bold().FontColor("#ffffff");
                                c.Item().Text("Doctor Test Request Report")
                                    .FontSize(10).FontColor("#d1ede8");
                            });
                            row.ConstantItem(200).AlignRight().Column(c =>
                            {
                                c.Item().Text(doctorName).FontSize(10).Bold().FontColor("#ffffff");
                                if (!string.IsNullOrEmpty(hpcsa))
                                    c.Item().Text(hpcsa).FontSize(8).FontColor("#d1ede8");
                                c.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}")
                                    .FontSize(8).FontColor("#d1ede8");
                            });
                        });

                        col.Item().PaddingTop(10).PaddingBottom(10)
                            .Text($"Reporting period: {effectiveFrom:dd MMM yyyy} – {effectiveTo:dd MMM yyyy}")
                            .FontSize(10).Bold().FontColor("#0d6b63");
                    });

                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        int totalReq = requests.Count;
                        int completedReq = requests.Count(r => r.Status == "Completed" || r.Status == "Released by doctor");
                        int cancelledReq = requests.Count(r => r.Status == "Cancelled");
                        int abnormalTotal = requests.Sum(r =>
                            r.TestRequestItems?.Count(i =>
                                i.TestResults != null && i.TestResults.Any(tr => tr.IsAbnormal)) ?? 0);

                        col.Item().Border(1).BorderColor("#e3e8ee").Padding(12).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"{totalReq}").FontSize(20).Bold().FontColor("#0d6b63");
                                c.Item().Text("Total Requests").FontSize(8).FontColor("#7d8e8a");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"{completedReq}").FontSize(20).Bold().FontColor("#22a06b");
                                c.Item().Text("Completed").FontSize(8).FontColor("#7d8e8a");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"{cancelledReq}").FontSize(20).Bold().FontColor("#d92d20");
                                c.Item().Text("Cancelled").FontSize(8).FontColor("#7d8e8a");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"{abnormalTotal}").FontSize(20).Bold().FontColor("#d97706");
                                c.Item().Text("Abnormal Results").FontSize(8).FontColor("#7d8e8a");
                            });
                        });

                        col.Item().PaddingTop(16).Text("Requests")
                            .FontSize(12).Bold().FontColor("#0d6b63");

                        col.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(45);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(45);
                                columns.ConstantColumn(60);
                            });

                            table.Header(header =>
                            {
                                void H(string t) =>
                                    header.Cell().Background("#f3faf9").Padding(6)
                                        .Text(t).FontSize(8).Bold().FontColor("#0d6b63");

                                H("#"); H("Date"); H("Patient"); H("ID Number");
                                H("Urgency"); H("Status"); H("Tests"); H("Abnormal");
                            });

                            foreach (var r in requests)
                            {
                                int totalItems = r.TestRequestItems?.Count ?? 0;
                                int abnormalItems = r.TestRequestItems?
                                    .Count(i => i.TestResults != null &&
                                                i.TestResults.Any(tr => tr.IsAbnormal)) ?? 0;

                                bool hasAbnormal = abnormalItems > 0;
                                string rowBg = hasAbnormal ? "#fff6f6" : "#ffffff";

                                void C(string text, bool bold = false) =>
                                    table.Cell().Background(rowBg).Padding(6)
                                        .Text(text).FontSize(8)
                                        .FontColor(bold && hasAbnormal ? "#b42318" : "#18324f");

                                C(r.RequestId.ToString());
                                C(r.RequestDate.ToString("dd MMM yyyy"));
                                C($"{r.Patient?.Name} {r.Patient?.Surname}");
                                C(r.Patient?.IDNumber ?? "—");
                                C(r.Urgency);
                                C(r.Status);
                                C(totalItems.ToString());
                                C(abnormalItems > 0 ? abnormalItems.ToString() : "—", true);
                            }
                        });

                        col.Item().PaddingTop(20).Text(
                            "This report was generated by NMB Haematology Laboratory. " +
                            "For clinical questions, please contact your referring doctor.")
                            .FontSize(8).FontColor("#7d8e8a");
                    });

                    page.Footer().AlignCenter().Column(c =>
                    {
                        c.Item().Text(text =>
                        {
                            text.Span("Generated on ").FontSize(8).FontColor("#7d8e8a");
                            text.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm"))
                                .FontSize(8).FontColor("#7d8e8a");
                        });
                        c.Item().Text(text =>
                        {
                            text.CurrentPageNumber().FontSize(8).FontColor("#7d8e8a");
                            text.Span(" / ").FontSize(8).FontColor("#7d8e8a");
                            text.TotalPages().FontSize(8).FontColor("#7d8e8a");
                        });
                    });
                });
            }).GeneratePdf();

            string filename = $"DoctorReport_{effectiveFrom:yyyyMMdd}_to_{effectiveTo:yyyyMMdd}.pdf";
            return File(pdf, "application/pdf", filename);
        }
    }
}