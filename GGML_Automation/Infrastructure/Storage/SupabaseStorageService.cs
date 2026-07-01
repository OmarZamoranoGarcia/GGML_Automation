using Supabase.Storage;

namespace GGML_Automation.Infrastructure.Storage;

public class SupabaseStorageService : IStorageService
{
    private readonly Supabase.Client _client;

    public SupabaseStorageService(
        Supabase.Client client)
    {
        _client = client;
    }

    public async Task<string> UploadFile(
        string fileName,
        byte[] file)
    {

        var bucket =
            _client.Storage
            .From("excel-files");

        // Crear nombre único
        var uniqueName =
            $"{Guid.NewGuid()}_{fileName}";

        await bucket.Upload(
            file,
            uniqueName
        );

        return uniqueName;
    }
}