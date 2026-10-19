using LabDash.Areas.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Services
{
    public interface IVerifiedResultsNotificationService
    {
        Task<bool> SendVerifiedResultsAsync(int requestId);
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

        public async Task<bool> SendVerifiedResultsAsync(int requestId)
        {
            try
            {
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
                    return false;
                }

                var doctorEmail = request.RequestingDoctor?.Email;
                var doctorName = request.RequestingDoctor?.FullName;

                if (string.IsNullOrWhiteSpace(doctorEmail))
                {
                    _logger.LogWarning(
                        "SendVerifiedResultsAsync: no email on file for the requesting doctor on request {RequestId}.",
                        requestId);
                    return false;
                }

                var itemIds = request.TestRequestItems
                    .Select(i => i.TestRequestItemId)
                    .ToList();

                var results = await _context.TestResults
                    .Include(r => r.VerifiedByTechnician)
                    .Where(r => itemIds.Contains(r.TestRequestItemId)
                             && r.Status == "Verified")
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
                    "The full results are attached as a PDF and are also available in the patient folder.</p>" +
                    "<p>Regards,<br/>LabDash Laboratory Team</p>";

                var fileName = $"LabResults_TR-{request.RequestId}.pdf";

                await _emailSender.SendEmailWithAttachmentAsync(
                    doctorEmail, subject, htmlBody, pdfBytes, fileName);

                _logger.LogInformation(
                    "Verified-results PDF emailed to {DoctorEmail} for request {RequestId}.",
                    doctorEmail, requestId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send verified-results notification for request {RequestId}.",
                    requestId);

                return false;
            }
        }
    }
}