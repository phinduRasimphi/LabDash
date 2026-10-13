using LabDash.Areas.Identity.Data;
using LabDash.Models;
using System.Net;
using System.Text;

namespace LabDash.Helpers
{
    public static class ResultEmailBuilder
    {
        public static string BuildResultsEmail(
            TestRequest request,
            LabUser doctor,
            string? note)
        {
            var patient = request.Patient;
            var items = request.TestRequestItems?.OrderBy(i => i.TestType?.Name).ToList()
                        ?? new List<TestRequestItem>();

            // ---------- RESULTS TABLE ----------
            var resultsTable = new StringBuilder();
            resultsTable.Append(@"
                <table role='presentation' cellpadding='0' cellspacing='0' border='0'
                       style='width:100%;border-collapse:collapse;margin-top:14px;font-size:13px;'>
                    <thead>
                        <tr>
                            <th style='text-align:left;padding:10px 12px;background:#f3faf9;
                                       color:#0d6b63;font-size:11px;font-weight:700;
                                       text-transform:uppercase;letter-spacing:0.5px;
                                       border-bottom:1px solid #cce5e1;'>Test</th>
                            <th style='text-align:left;padding:10px 12px;background:#f3faf9;
                                       color:#0d6b63;font-size:11px;font-weight:700;
                                       text-transform:uppercase;letter-spacing:0.5px;
                                       border-bottom:1px solid #cce5e1;'>Result</th>
                            <th style='text-align:left;padding:10px 12px;background:#f3faf9;
                                       color:#0d6b63;font-size:11px;font-weight:700;
                                       text-transform:uppercase;letter-spacing:0.5px;
                                       border-bottom:1px solid #cce5e1;'>Reference</th>
                            <th style='text-align:left;padding:10px 12px;background:#f3faf9;
                                       color:#0d6b63;font-size:11px;font-weight:700;
                                       text-transform:uppercase;letter-spacing:0.5px;
                                       border-bottom:1px solid #cce5e1;'>Status</th>
                        </tr>
                    </thead>
                    <tbody>");

            foreach (var item in items)
            {
                var result = item.TestResults?
                    .OrderByDescending(r => r.DateCaptured)
                    .FirstOrDefault();

                string testName = WebUtility.HtmlEncode(item.TestType?.Name ?? "Test");
                string value = WebUtility.HtmlEncode(result?.ResultValue ?? "—");
                string unit = WebUtility.HtmlEncode(result?.Units ?? "");
                string range = WebUtility.HtmlEncode(result?.ReferenceRange ?? "—");
                bool isAbnormal = result?.IsAbnormal ?? false;

                string statusCell = isAbnormal
                    ? "<span style='display:inline-block;padding:4px 10px;background:#fde8e8;color:#b42318;border-radius:10px;font-size:11px;font-weight:700;'>ABNORMAL</span>"
                    : "<span style='display:inline-block;padding:4px 10px;background:#e6f5f1;color:#0d6b63;border-radius:10px;font-size:11px;font-weight:700;'>Normal</span>";

                string rowBg = isAbnormal ? "background:#fff6f6;" : "";

                resultsTable.Append($@"
                    <tr style='{rowBg}'>
                        <td style='padding:12px;border-bottom:1px solid #eef3f2;color:#18324f;font-weight:600;'>{testName}</td>
                        <td style='padding:12px;border-bottom:1px solid #eef3f2;color:#18324f;'>{value} {unit}</td>
                        <td style='padding:12px;border-bottom:1px solid #eef3f2;color:#7d8e8a;'>{range}</td>
                        <td style='padding:12px;border-bottom:1px solid #eef3f2;'>{statusCell}</td>
                    </tr>");
            }
            resultsTable.Append("</tbody></table>");

            int abnormalCount = items.Count(i =>
                i.TestResults != null && i.TestResults.Any(r => r.IsAbnormal));

            string safeName = WebUtility.HtmlEncode(patient.Name);
            string safeDoctor = WebUtility.HtmlEncode(
                $"Dr. {doctor.FirstName} {doctor.LastName}");

            string safeNote = string.IsNullOrWhiteSpace(note)
                ? ""
                : $@"<div style='background:#f3faf9;border-left:4px solid #0d6b63;
                                padding:14px 18px;border-radius:10px;margin:20px 0;
                                color:#344054;font-size:13px;line-height:1.6;'>
                        <strong style='color:#0d6b63;'>Note from your doctor:</strong><br/>
                        {WebUtility.HtmlEncode(note)}
                    </div>";

            string abnormalBanner = abnormalCount > 0
                ? $@"<div style='background:#fff6f6;border-left:4px solid #d92d20;
                                padding:14px 18px;border-radius:10px;margin:20px 0;
                                color:#b42318;font-size:13px;'>
                        <strong>⚠ {abnormalCount} abnormal result{(abnormalCount == 1 ? "" : "s")} detected.</strong><br/>
                        Highlighted rows below require your attention.
                    </div>"
                : "";

            // ---------- FULL EMAIL ----------
            return $@"
                <!DOCTYPE html>
                <html><body style='margin:0;padding:0;background:#eef2f5;font-family:Arial,Helvetica,sans-serif;'>
                    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'
                           style='background:#eef2f5;padding:30px 15px;'>
                        <tr><td align='center'>
                            <table role='presentation' width='600' cellpadding='0' cellspacing='0' border='0'
                                   style='max-width:600px;width:100%;background:#ffffff;border-radius:14px;
                                          overflow:hidden;box-shadow:0 6px 28px rgba(15,35,55,0.08);'>
                                <tr><td align='center'
                                    style='background:linear-gradient(135deg,#0d6b63,#1a8c82);
                                           padding:32px 30px;color:#ffffff;'>
                                    <div style='width:56px;height:56px;border-radius:50%;
                                                background:rgba(255,255,255,0.15);display:inline-block;
                                                line-height:56px;margin-bottom:12px;font-size:24px;'>🧪</div>
                                    <h1 style='margin:0;font-size:20px;font-weight:700;'>Your Test Results</h1>
                                    <p style='margin:6px 0 0;font-size:12px;opacity:0.85;'>
                                        NMB Haematology Laboratory
                                    </p>
                                </td></tr>
                                <tr><td style='padding:36px 40px 20px;'>
                                    <h2 style='margin:0 0 12px;color:#18324f;font-size:18px;'>Dear {safeName},</h2>
                                    <p style='margin:0 0 16px;color:#4a5a6e;font-size:14px;line-height:1.65;'>
                                        Your recent laboratory results have been reviewed by
                                        <strong style='color:#0d6b63;'>{safeDoctor}</strong>.
                                        Please find a summary below.
                                    </p>
                                    {safeNote}
                                    {abnormalBanner}
                                    <p style='margin:20px 0 0;color:#4a5a6e;font-size:14px;font-weight:600;'>
                                        Your results:
                                    </p>
                                    {resultsTable}
                                    <p style='margin:24px 0 0;color:#7d8e8a;font-size:12px;line-height:1.6;'>
                                        Please contact your doctor if you have any questions about these results.
                                    </p>
                                </td></tr>
                                <tr><td style='padding:10px 40px 30px;'>
                                    <p style='margin:0 0 6px;color:#4a5a6e;font-size:13px;'>Regards,</p>
                                    <p style='margin:0;color:#0d6b63;font-weight:700;font-size:13px;'>
                                        {safeDoctor}<br/>
                                        <span style='color:#7d8e8a;font-weight:400;'>NMB Haematology Laboratory</span>
                                    </p>
                                </td></tr>
                                <tr><td align='center' style='background:#fafbfc;padding:20px 30px;
                                                            border-top:1px solid #e9edf2;'>
                                    <p style='margin:0;color:#a4afbb;font-size:11px;'>
                                        This is an automated message. Please do not reply to this email.
                                    </p>
                                </td></tr>
                            </table>
                        </td></tr>
                    </table>
                </body></html>";
        }

        public static string BuildConsultationEmail(
            TestRequest request,
            LabUser doctor,
            string? note)
        {
            var patient = request.Patient;
            string safeName = WebUtility.HtmlEncode(patient.Name);
            string safeDoctor = WebUtility.HtmlEncode(
                $"Dr. {doctor.FirstName} {doctor.LastName}");

            string safeNote = string.IsNullOrWhiteSpace(note)
                ? ""
                : $@"<div style='background:#f3faf9;border-left:4px solid #0d6b63;
                                padding:14px 18px;border-radius:10px;margin:20px 0;
                                color:#344054;font-size:13px;line-height:1.6;'>
                        <strong style='color:#0d6b63;'>Note from your doctor:</strong><br/>
                        {WebUtility.HtmlEncode(note)}
                    </div>";

            return $@"
                <!DOCTYPE html>
                <html><body style='margin:0;padding:0;background:#eef2f5;font-family:Arial,Helvetica,sans-serif;'>
                    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'
                           style='background:#eef2f5;padding:30px 15px;'>
                        <tr><td align='center'>
                            <table role='presentation' width='600' cellpadding='0' cellspacing='0' border='0'
                                   style='max-width:600px;width:100%;background:#ffffff;border-radius:14px;
                                          overflow:hidden;box-shadow:0 6px 28px rgba(15,35,55,0.08);'>
                                <tr><td align='center'
                                    style='background:linear-gradient(135deg,#0d6b63,#1a8c82);
                                           padding:32px 30px;color:#ffffff;'>
                                    <div style='width:56px;height:56px;border-radius:50%;
                                                background:rgba(255,255,255,0.15);display:inline-block;
                                                line-height:56px;margin-bottom:12px;font-size:24px;'>📅</div>
                                    <h1 style='margin:0;font-size:20px;font-weight:700;'>Consultation Invitation</h1>
                                    <p style='margin:6px 0 0;font-size:12px;opacity:0.85;'>
                                        NMB Haematology Laboratory
                                    </p>
                                </td></tr>
                                <tr><td style='padding:36px 40px 20px;'>
                                    <h2 style='margin:0 0 12px;color:#18324f;font-size:18px;'>Dear {safeName},</h2>
                                    <p style='margin:0 0 16px;color:#4a5a6e;font-size:14px;line-height:1.65;'>
                                        Your recent laboratory results have been reviewed by
                                        <strong style='color:#0d6b63;'>{safeDoctor}</strong>.
                                    </p>
                                    <p style='margin:0 0 16px;color:#4a5a6e;font-size:14px;line-height:1.65;'>
                                        We would like you to book an appointment to discuss the results
                                        in person. Please contact the practice to arrange a suitable time.
                                    </p>
                                    {safeNote}
                                    <div style='background:#fffbeb;border:1px solid #fde68a;border-radius:10px;
                                                padding:14px 18px;margin:20px 0;color:#92400e;font-size:13px;'>
                                        <strong>Please bring your ID</strong> to your appointment.
                                    </div>
                                </td></tr>
                                <tr><td style='padding:20px 40px 30px;'>
                                    <p style='margin:0 0 6px;color:#4a5a6e;font-size:13px;'>Regards,</p>
                                    <p style='margin:0;color:#0d6b63;font-weight:700;font-size:13px;'>
                                        {safeDoctor}<br/>
                                        <span style='color:#7d8e8a;font-weight:400;'>NMB Haematology Laboratory</span>
                                    </p>
                                </td></tr>
                                <tr><td align='center' style='background:#fafbfc;padding:20px 30px;
                                                            border-top:1px solid #e9edf2;'>
                                    <p style='margin:0;color:#a4afbb;font-size:11px;'>
                                        This is an automated message. Please do not reply to this email.
                                    </p>
                                </td></tr>
                            </table>
                        </td></tr>
                    </table>
                </body></html>";
        }
    }
}