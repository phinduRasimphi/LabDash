namespace LabDash.Services
{
    public class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromName { get; set; } = "";
        public string FromAddress { get; set; } = "";
    }
}
