namespace GGML_Automation.Infrastructure.Storage;

public interface IStorageService
{
    Task<UploadResult> UploadFile(
        string fileName,
        byte[] file);

    Task<byte[]> DownloadFile(
        string storagePath);
}