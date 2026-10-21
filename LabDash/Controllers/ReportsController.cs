
using LabDash.Areas.Identity.Data;
using LabDash.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class ReportsController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ReportsController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new TechnicianReportViewModel
            {
                FromDate = DateTime.Today.AddDays(-7),
                ToDate = DateTime.Today
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(
            TechnicianReportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            if (model.FromDate.Date > model.ToDate.Date)
            {
                ModelState.AddModelError(
                    "ToDate",
                    "The To Date cannot be earlier than the From Date.");

                return View("Index", model);
            }

            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
            {
                TempData["Error"] =
                    "Technician account could not be found.";

                return View("Index", model);
            }

            var fromDate = model.FromDate.Date;
            var toDateExclusive = model.ToDate.Date.AddDays(1);

            // Load completed tests assigned to the logged-in technician.
            var completedTests = await _context.TestRequestItems
                .Include(x => x.TestType)
                .Include(x => x.TestRequest)
                    .ThenInclude(x => x.Patient)
                .Where(x =>
                    x.AssignedTechnicianId == technician.Id &&
                    (
                        x.Status == "Completed" ||
                        x.Status == "Verified" ||
                        x.Status == "To Be Reviewed"
                    ) &&
                    x.CompletionDateTime.HasValue &&
                    x.CompletionDateTime.Value >= fromDate &&
                    x.CompletionDateTime.Value < toDateExclusive)
                .OrderBy(x => x.TestType.Category)
                .ThenBy(x => x.CompletionDateTime)
                .ToListAsync();

            if (!completedTests.Any())
            {
                TempData["Error"] =
                    "No completed tests were found for the selected date range.";

                return View("Index", model);
            }

            QuestPDF.Settings.License = LicenseType.Community;

            var technicianName =
                $"{technician.FirstName} {technician.LastName}".Trim();

            // Locate the logo in wwwroot/images.
            var logoPath = Path.Combine(
                _environment.WebRootPath ?? string.Empty,
                "images",
                "labdash-logo-light.png");

            byte[]? logoBytes = null;

            if (System.IO.File.Exists(logoPath))
            {
                logoBytes = await System.IO.File.ReadAllBytesAsync(logoPath);
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);

                    page.DefaultTextStyle(style =>
                        style.FontSize(9));

                    // =================================================
                    // HEADER: LOGO + LABDASH BRANDING
                    // =================================================
                    page.Header()
                        .Column(column =>
                        {
                            column.Item()
                                .Row(row =>
                                {
                                    if (logoBytes != null)
                                    {
                                        row.ConstantItem(65)
                                            .Height(55)
                                            .AlignMiddle()
                                            .Image(logoBytes)
                                            .FitArea();
                                    }

                                    row.RelativeItem()
                                        .AlignMiddle()
                                        .Column(brand =>
                                        {
                                            brand.Item()
                                                .Text("LabDash")
                                                .FontSize(24)
                                                .Bold()
                                                .FontColor("#008f83");

                                            brand.Item()
                                                .Text(
                                                    "LABORATORY MANAGEMENT SYSTEM")
                                                .FontSize(8)
                                                .FontColor("#006f67");
                                        });
                                });

                            column.Item()
                                .PaddingTop(12)
                                .Text("Technician Completed Tests Report")
                                .FontSize(16)
                                .Bold()
                                .FontColor("#173a3a");

                            column.Item()
                                .PaddingTop(5)
                                .Text($"Technician: {technicianName}")
                                .FontSize(10);

                            column.Item()
                                .Text(
                                    $"Reporting Period: {fromDate:dd MMM yyyy} - {model.ToDate:dd MMM yyyy}")
                                .FontSize(10);

                            column.Item()
                                .PaddingTop(10)
                                .LineHorizontal(1)
                                .LineColor("#008f83");
                        });

                    // =================================================
                    // REPORT CONTENT
                    // =================================================
                    page.Content()
                        .PaddingTop(20)
                        .Column(column =>
                        {
                            column.Item()
                                .Background("#e8f7f5")
                                .Padding(12)
                                .Text(
                                    $"Total Completed Tests: {completedTests.Count}")
                                .Bold()
                                .FontSize(12)
                                .FontColor("#006f67");

                            column.Item()
                                .PaddingTop(20)
                                .Text("Completed Tests by Category")
                                .Bold()
                                .FontSize(14)
                                .FontColor("#005c55");

                            var categoryGroups = completedTests
                                .GroupBy(x =>
                                    string.IsNullOrWhiteSpace(
                                        x.TestType?.Category)
                                        ? "Uncategorised"
                                        : x.TestType.Category)
                                .OrderBy(group => group.Key);

                            foreach (var categoryGroup in categoryGroups)
                            {
                                column.Item()
                                    .PaddingTop(15)
                                    .Text(categoryGroup.Key)
                                    .Bold()
                                    .FontSize(12)
                                    .FontColor("#008f83");

                                column.Item()
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(1.5f);
                                            columns.RelativeColumn(1.5f);
                                        });

                                        // Table header
                                        table.Header(header =>
                                        {
                                            header.Cell()
                                                .Background("#008f83")
                                                .Padding(6)
                                                .Text("Test Type")
                                                .FontColor("#FFFFFF")
                                                .Bold();

                                            header.Cell()
                                                .Background("#008f83")
                                                .Padding(6)
                                                .Text("Patient")
                                                .FontColor("#FFFFFF")
                                                .Bold();

                                            header.Cell()
                                                .Background("#008f83")
                                                .Padding(6)
                                                .Text("Completed")
                                                .FontColor("#FFFFFF")
                                                .Bold();

                                            header.Cell()
                                                .Background("#008f83")
                                                .Padding(6)
                                                .Text("Status")
                                                .FontColor("#FFFFFF")
                                                .Bold();
                                        });

                                        foreach (var item in categoryGroup)
                                        {
                                            var patientName =
                                                item.TestRequest?.Patient != null
                                                    ? $"{item.TestRequest.Patient.Name} {item.TestRequest.Patient.Surname}"
                                                    : "Unknown";

                                            table.Cell()
                                                .Padding(6)
                                                .Text(
                                                    item.TestType?.Name
                                                    ?? "Unknown Test");

                                            table.Cell()
                                                .Padding(6)
                                                .Text(patientName);

                                            table.Cell()
                                                .Padding(6)
                                                .Text(
                                                    item.CompletionDateTime
                                                        ?.ToString("dd/MM/yyyy HH:mm")
                                                    ?? "-");

                                            table.Cell()
                                                .Padding(6)
                                                .Text(item.Status ?? "-");
                                        }
                                    });
                            }
                        });

                    // =================================================
                    // FOOTER
                    // =================================================
                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("LabDash Technician Report | ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            });

            var pdfBytes = document.GeneratePdf();

            var fileName =
                $"Technician_Report_{fromDate:yyyyMMdd}_{model.ToDate:yyyyMMdd}.pdf";

            return File(
                pdfBytes,
                "application/pdf",
                fileName);
        }
    }
}