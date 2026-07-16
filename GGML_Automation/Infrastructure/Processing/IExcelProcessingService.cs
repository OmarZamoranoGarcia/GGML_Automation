namespace GGML_Automation.Infrastructure.Processing
{
    public interface IExcelProcessingService
    {
        Task ProcessExcel(
        string emailId,
        string originalStoragePath,
        string subject,
        string body,
        string sender);
    }
}
