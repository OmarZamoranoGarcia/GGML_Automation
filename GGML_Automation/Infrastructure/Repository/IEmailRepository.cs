namespace GGML_Automation.Infrastructure.Repository
{
    public interface IEmailRepository
    {
        Task SaveEmail(
            string id,
            string arrivalEmail,
            string subject,
            string body,
            DateTime arrivalAt
        );

        Task SaveFile(
            string emailId,
            string fileName,
            string fileType,
            string storagePath
        );

        Task<bool> EmailExists(string id);
    }
}