namespace GGML_Automation.Infrastructure.Storage
{
    public interface IStorageService
    {
        Task<string> UploadFile(string fileName, byte[] file);
    }
}