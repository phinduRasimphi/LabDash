using LabDash.Areas.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Services
{
    public interface IVerifiedResultsNotificationService
    {
        Task SendVerifiedResultsAsync(int requestId);
    }

    public class VerifiedResultsNotificationService : IVerifiedResultsNotificationService
    {
        private readonly LabDbContext _context;
        private readonly IVerifiedResultsPdfGenerator _pdfGenerator;
        private readonly IEmailAttachmentSender _emailSender;
        private readonly ILogger<VerifiedResultsNotificationService> _logger;

        public VerifiedResultsNotificationService(
            LabDbContext context,
            IVerifiedResultsPdfGenerator pdfGenerator,
            IEmailAttachmentSender emailSender,
            ILogger<VerifiedResultsNotificationService> logger)
        {
            _context = context;
            _pdfGenerator = pdfGenerator;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task SendVerifiedResultsAsync(int requestId)
        {
            // Everything in this method is best-effort: the verification
            // itself is already committed to the database by the time
            // this runs, so a failure here (bad doctor email, SMTP down,
            // PDF error, etc.) must never look like the verification
            // failed. Log loudly, return quietly.
            try
            {
                // ------------------------------------------------------------------
                // ASSUMPTION: TestRequest has a "RequestingDoctor" navigation
                // property (a LabUser, same pattern as AssignedTechnician /
                // CapturedByTechnician elsewhere) with FullName + Email.
                // If your model instead stores a plain doctor name/email as
                // strings directly on TestRequest, or references a separate
                // Doctor entity, adjust the two lines marked below.
                // ------------------------------------------------------------------
                var request = await _context.TestRequests
                    .Include(r => r.Patient)
                    .Include(r => r.RequestingDoctor)
                    .Include(r => r.TestRequestItems)
                        .ThenInclude(i => i.TestType)
                    .FirstOrDefaultAsync(r => r.RequestId == requestId);

                if (request == null)
                {
                    _logger.LogWarning(
                        "SendVerifiedResultsAsync: TestRequest {RequestId} not found.", requestId);
                    return;
                }

                var doctorEmail = request.RequestingDoctor?.Email; // ASSUMPTION
                var doctorName = request.RequestingDoctor?.FullName; // ASSUMPTION

                if (string.IsNullOrWhiteSpace(doctorEmail))
                {
                    _logger.LogWarning(
                        "SendVerifiedResultsAsync: no email on file for the requesting doctor on request {RequestId}. Notification skipped.",
                        requestId);
                    return;
                }

                var itemIds = request.TestRequestItems
                    .Select(i => i.TestRequestItemId)
                    .ToList();

                // ------------------------------------------------------------------
                // ASSUMPTION: TestResult has a "VerifiedByTechnician" navigation
                // property, parallel to the existing CapturedByTechnician one.
                // ------------------------------------------------------------------
                var results = await _context.TestResults
                    .Include(r => r.VerifiedByTechnician)
                    .Where(r => itemIds.Contains(r.TestRequestItemId))
                    .ToListAsync();

                var reportData = new VerifiedResultsReportData
                {
                    RequestId = request.RequestId,
                    PatientFullName = $"{request.Patient?.Name} {request.Patient?.Surname}".Trim(),
                    PatientIdNumber = request.Patient?.IDNumber,
                    RequestingDoctorFullName = doctorName,
                    GeneratedAt = DateTime.Now
                };

                foreach (var item in request.TestRequestItems)
                {
                    var result = results.FirstOrDefault(r => r.TestRequestItemId == item.TestRequestItemId);

                    reportData.Rows.Add(new VerifiedResultRow
                    {
                        TestName = item.TestType?.Name ?? "Unknown Test",
                        ResultValue = result?.ResultValue,
                        Units = result?.Units,
                        ReferenceRange = result?.ReferenceRange,
                        IsAbnormal = result?.IsAbnormal ?? false,
                        Comments = result?.Comments,
                        VerifiedByFullName = result?.VerifiedByTechnician?.FullName,
                        VerificationDate = result?.VerificationDate
                    });
                }

                var pdfBytes = _pdfGenerator.Generate(reportData);

                var subject = $"Laboratory Results Verified — Request TR-{request.RequestId}";

                var htmlBody =
                    $"<p>Dear {System.Net.WebUtility.HtmlEncode(doctorName ?? "Doctor")},</p>" +
                    $"<p>All laboratory tests for request <strong>TR-{request.RequestId}</strong> " +
                    $"({System.Net.WebUtility.HtmlEncode(reportData.PatientFullName)}) have been verified. " +
                    "The full results are attached as a PDF.</p>" +
                    "<p>Regards,<br/>LabDash Laboratory Team</p>";

                var fileName = $"LabResults_TR-{request.RequestId}.pdf";

                await _emailSender.SendEmailWithAttachmentAsync(
                    doctorEmail, subject, htmlBody, pdfBytes, fileName);

                _logger.LogInformation(
                    "Verified-results PDF emailed to {DoctorEmail} for request {RequestId}.",
                    doctorEmail, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send verified-results notification for request {RequestId}.",
                    requestId);
            }
        }
    }
}