namespace GGML_Automation.Infrastructure.Email
{
    public enum EmailLogLevel
    {
        Info,
        Warning,
        Error
    }

    public class EmailLogEntry
    {
        public EmailLogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class EmailProcessResult
    {
        public string EmailId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // COMPLETED, ERROR, SKIPPED
        public string? ErrorMessage { get; set; }
        public string? Note { get; set; } // aclaraciones que no son error, ej: "sin excel adjunto"
    }

    public class EmailCheckResult
    {
        public bool Success { get; set; } = true;
        public int TotalEmailsFound { get; set; }
        public int Processed { get; set; }
        public int NotProcessed { get; set; }
        public int Skipped { get; set; }
        public int Errors { get; set; }

        public List<EmailProcessResult> Emails { get; set; } = new();
        public List<EmailLogEntry> Logs { get; set; } = new();

        public void AddLog(EmailLogLevel level, string message)
        {
            Logs.Add(new EmailLogEntry
            {
                Level = level,
                Message = message
            });

            if (level == EmailLogLevel.Error)
            {
                Success = false;
            }
        }
    }
}