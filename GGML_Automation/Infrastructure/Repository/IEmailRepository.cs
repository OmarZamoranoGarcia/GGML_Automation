namespace GGML_Automation.Infrastructure.Repository
{
    public interface IEmailRepository
    {
        Task<bool> EmailExists(string id);

        Task SaveEmail(
            string id,
            string arrivalEmail,
            string subject,
            string body,
            DateTime arrivalAt);

        Task SaveFile(
            string emailId,
            string fileName,
            string storedName,
            string fileType,
            string fileRole,
            string storagePath);

        Task UpdateEmailStatus(
            string emailId,
            string status);

        Task CreateProcess(
            string emailId);

        Task UpdateProcess(
            string emailId,
            string status,
            DateTime startedAt,
            DateTime finishedAt,
            string? errorMessage);
    }
}