using Supabase.Storage;

namespace GGML_Automation.Infrastructure.Storage;

public class SupabaseStorageService : IStorageService
{
    private readonly Supabase.Client client;

    public SupabaseStorageService(
        Supabase.Client client)
    {
        this.client = client;
    }

    public async Task<UploadResult> UploadFile(string fileName,byte[] file)
    {
        var bucket =
            client.Storage
            .From("excel-files");

        var uniqueName =
            $"{Guid.NewGuid()}_{fileName}";

        await bucket.Upload(
            file,
            uniqueName);

        return new UploadResult
        {
            StoredName = uniqueName,
            StoragePath = uniqueName
        };
    }

    public async Task<byte[]> DownloadFile(string storagePath)
    {
        var bucket = client.Storage
            .From("excel-files");

        // Specify null for the optional TransformOptions parameter to resolve ambiguity
        var file = await bucket.Download(storagePath, (Supabase.Storage.TransformOptions?)null, (EventHandler<float>?)null);

        Console.WriteLine(
            $"Archivo descargado desde Supabase: {storagePath}");

        return file;
    }
}