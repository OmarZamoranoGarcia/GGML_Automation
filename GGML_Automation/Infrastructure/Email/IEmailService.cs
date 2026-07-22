namespace GGML_Automation.Infrastructure.Email
{
    public interface IEmailService
    {
        Task<EmailCheckResult> CheckEmails();
    }
}