namespace LabDash.Helpers
{
    public static class EmailTemplates
    {
        // ------------------------------------------------------------
        // Themes
        // ------------------------------------------------------------
        private sealed record Theme(string Accent, string Soft, string Border, string Icon, string Label);

        private static readonly Theme Green = new(
            Accent: "#0A5850",
            Soft: "#E8F3F1",
            Border: "#BFE0DB",
            Icon: "&#10003;",   // check mark
            Label: "Access granted");

        private static readonly Theme Red = new(
            Accent: "#C0392B",
            Soft: "#FDECEA",
            Border: "#F5C6C1",
            Icon: "&#10005;",   // cross
            Label: "Access revoked");

        private const string Text = "#1F2933";
        private const string Muted = "#6B7280";

        private static string Enc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");

        // ------------------------------------------------------------
        // Shared layout
        // ------------------------------------------------------------
        private static string Layout(
            Theme t,
            string preheader,
            string heading,
            string innerHtml,
            string? buttonUrl = null,
            string? buttonText = null)
        {
            var button = !string.IsNullOrWhiteSpace(buttonUrl) && !string.IsNullOrWhiteSpace(buttonText)
                ? $@"
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" align=""center"" style=""margin:30px auto 10px;"">
                  <tr>
                    <td align=""center"" bgcolor=""{t.Accent}"" style=""border-radius:10px;"">
                      <a href=""{Enc(buttonUrl)}""
                         style=""display:inline-block;padding:15px 34px;font-family:Segoe UI,Arial,sans-serif;
                                font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:10px;"">
                        {Enc(buttonText)} &rarr;
                      </a>
                    </td>
                  </tr>
                </table>"
                : "";

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>{Enc(heading)}</title>
</head>
<body style=""margin:0;padding:0;background:#EEF1F4;"">
  <div style=""display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;"">{Enc(preheader)}</div>

  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#EEF1F4;padding:36px 12px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0""
               style=""width:100%;max-width:600px;background:#ffffff;border-radius:16px;overflow:hidden;
                      border:1px solid #E5E7EB;font-family:Segoe UI,Arial,sans-serif;color:{Text};"">

          <!-- Accent bar -->
          <tr><td height=""6"" bgcolor=""{t.Accent}"" style=""height:6px;line-height:6px;font-size:0;"">&nbsp;</td></tr>

          <!-- Brand -->
          <tr>
            <td align=""center"" style=""padding:28px 32px 0;"">
              <div style=""font-size:22px;font-weight:800;color:{t.Accent};letter-spacing:1px;"">LABDASH</div>
            </td>
          </tr>

          <!-- Status icon -->
          <tr>
            <td align=""center"" style=""padding:26px 32px 0;"">
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                <tr>
                  <td align=""center"" valign=""middle"" width=""72"" height=""72"" bgcolor=""{t.Soft}""
                      style=""width:72px;height:72px;border-radius:36px;border:2px solid {t.Border};
                             font-size:34px;font-weight:700;color:{t.Accent};line-height:72px;"">
                    {t.Icon}
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Heading -->
          <tr>
            <td align=""center"" style=""padding:20px 40px 0;"">
              <h1 style=""margin:0;font-size:24px;line-height:1.3;font-weight:700;color:{Text};"">{Enc(heading)}</h1>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:22px 40px 8px;"">
              <div style=""font-size:15px;line-height:1.7;color:{Text};"">
                {innerHtml}
              </div>
              {button}
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td bgcolor=""#F9FAFB"" style=""background:#F9FAFB;padding:22px 40px 26px;border-top:1px solid #E5E7EB;"">
              <p style=""margin:0 0 6px;font-size:12px;line-height:1.6;color:{Muted};"">
                This is an automated message from the LabDash portal. Please do not reply to this email.
              </p>
              <p style=""margin:0;font-size:12px;line-height:1.6;color:{Muted};"">
                This message contains confidential health information intended only for the named recipient.
                If you received it in error, please delete it.
              </p>
            </td>
          </tr>
        </table>

        <p style=""margin:18px 0 0;font-family:Segoe UI,Arial,sans-serif;font-size:11px;color:#9CA3AF;"">
          &copy; {DateTime.Now:yyyy} LabDash
        </p>
      </td>
    </tr>
  </table>
</body>
</html>";
        }

        // ------------------------------------------------------------
        // Details card (label / value rows, optional status pill)
        // ------------------------------------------------------------
        private static string DetailsCard(Theme t, string pillText, params (string Label, string Value)[] rows)
        {
            var sb = new System.Text.StringBuilder();

            // Status pill row
            sb.Append($@"
                <tr>
                  <td style=""padding:10px 0;font-size:12px;color:{Muted};width:36%;vertical-align:middle;text-transform:uppercase;letter-spacing:0.8px;"">Status</td>
                  <td style=""padding:10px 0;vertical-align:middle;"">
                    <span style=""display:inline-block;padding:4px 12px;border-radius:999px;background:{t.Accent};
                                 color:#ffffff;font-size:12px;font-weight:700;letter-spacing:0.4px;"">{Enc(pillText)}</span>
                  </td>
                </tr>");

            foreach (var (label, value) in rows)
            {
                sb.Append($@"
                <tr>
                  <td style=""padding:10px 0;border-top:1px solid {t.Border};font-size:12px;color:{Muted};width:36%;vertical-align:top;text-transform:uppercase;letter-spacing:0.8px;"">{Enc(label)}</td>
                  <td style=""padding:10px 0;border-top:1px solid {t.Border};font-size:15px;color:{Text};font-weight:600;vertical-align:top;"">{Enc(value)}</td>
                </tr>");
            }

            return $@"
            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
                   style=""background:{t.Soft};border:1px solid {t.Border};border-left:5px solid {t.Accent};border-radius:10px;margin:22px 0;"">
              <tr>
                <td style=""padding:10px 22px;"">
                  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                    {sb}
                  </table>
                </td>
              </tr>
            </table>";
        }

        // ------------------------------------------------------------
        // Consent granted (green)
        // ------------------------------------------------------------
        public static string ConsentGranted(
            string doctorLastName,
            string patientName,
            int itemCount,
            DateTime grantedOn,
            string? portalUrl)
        {
            var inner =
                $"<p style=\"margin:0 0 14px;\">Dear Dr. {Enc(doctorLastName)},</p>" +
                $"<p style=\"margin:0;\"><strong>{Enc(patientName)}</strong> has shared test results with you " +
                "through the LabDash patient portal.</p>" +
                DetailsCard(Green, Green.Label,
                    ("Patient", patientName),
                    ("Results shared", $"{itemCount} test result item{(itemCount == 1 ? "" : "s")}"),
                    ("Date shared", grantedOn.ToString("dd MMMM yyyy, HH:mm"))) +
                "<p style=\"margin:0;\">Log in to the Doctor Portal to review the results.</p>" +
                $"<p style=\"margin:14px 0 0;font-size:13px;color:{Muted};\">" +
                "The patient can withdraw this access at any time, after which the results will no longer be visible to you.</p>";

            return Layout(
                Green,
                preheader: $"{patientName} has shared {itemCount} test result item(s) with you.",
                heading: "A patient has shared results with you",
                innerHtml: inner,
                buttonUrl: portalUrl,
                buttonText: "View shared results");
        }

        // ------------------------------------------------------------
        // Consent revoked (red)
        // ------------------------------------------------------------
        public static string ConsentRevoked(string doctorLastName, string patientName)
        {
            var inner =
                $"<p style=\"margin:0 0 14px;\">Dear Dr. {Enc(doctorLastName)},</p>" +
                $"<p style=\"margin:0;\"><strong>{Enc(patientName)}</strong> has withdrawn your access to their test results.</p>" +
                DetailsCard(Red, Red.Label,
                    ("Patient", patientName),
                    ("Effective", DateTime.Now.ToString("dd MMMM yyyy, HH:mm"))) +
                "<p style=\"margin:0;\">You will no longer be able to view these results in the Doctor Portal. " +
                "If you still need access, please ask the patient to share the results with you again.</p>";

            return Layout(
                Red,
                preheader: $"{patientName} has revoked your access to their results.",
                heading: "Access has been revoked",
                innerHtml: inner);
        }
    }
}