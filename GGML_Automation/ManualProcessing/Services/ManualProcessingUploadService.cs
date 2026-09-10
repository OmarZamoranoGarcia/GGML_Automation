using GGML_Automation.Infrastructure.Processing;
using GGML_Automation.Infrastructure.Repository;
using GGML_Automation.Infrastructure.Storage;
using GGML_Automation.ManualProcessing.DTOs;
using GGML_Automation.ManualProcessing.Services;
using Microsoft.AspNetCore.Http;

namespace GGML_Automation.ManualProcessing.Services;

public class ManualProcessingUploadService : IManualProcessingUploadService
{
    private readonly IConfiguration configuration;
    private readonly IStorageService storage;
    private readonly IEmailRepository repository;
    private readonly IExcelProcessingService processingService;

    // Nombres de cliente válidos y su llave de configuración correspondiente.
    // Debe coincidir con las llaves que ya usa SortingRuleService
    // (Cliente1:Email, Cliente2:Email, etc.) para que ambos flujos
    // (correo real y carga manual) resuelvan la MISMA regla de sorteo.
    private static readonly Dictionary<string, string> ClientConfigKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cliente3"] = "Cliente3:Email",
        ["Cliente4"] = "Cliente4:Email",
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xlsx", ".xls", ".csv"
    };

    public ManualProcessingUploadService(
        IConfiguration configuration,
        IStorageService storage,
        IEmailRepository repository,
        IExcelProcessingService processingService)
    {
        this.configuration = configuration;
        this.storage = storage;
        this.repository = repository;
        this.processingService = processingService;
    }

    public async Task<ManualUploadResultDto> ProcessManualExcel(
        string client,
        IFormFile file)
    {
        var result = new ManualUploadResultDto
        {
            Client = client
        };

        //------------------------------------------------
        // VALIDACIONES BÁSICAS
        //------------------------------------------------

        if (file is null || file.Length == 0)
        {
            result.Success = false;
            result.Status = "ERROR";
            result.ErrorMessage = "No se recibió ningún archivo.";
            return result;
        }

        var extension = Path.GetExtension(file.FileName);

        if (!AllowedExtensions.Contains(extension))
        {
            result.Success = false;
            result.Status = "ERROR";
            result.ErrorMessage = $"Extensión no soportada: {extension}. Se esperaba .xlsx, .xls o .csv.";
            return result;
        }

        if (!ClientConfigKeys.TryGetValue(client, out var configKey))
        {
            result.Success = false;
            result.Status = "ERROR";
            result.ErrorMessage = $"Cliente no reconocido: {client}.";
            return result;
        }

        var clientEmail = configuration[configKey];

        if (string.IsNullOrWhiteSpace(clientEmail))
        {
            result.Success = false;
            result.Status = "ERROR";
            result.ErrorMessage = $"No hay un correo configurado para {client} ({configKey}).";
            return result;
        }

        //------------------------------------------------
        // ID SINTÉTICO PARA REUTILIZAR LAS MISMAS TABLAS
        // QUE EL FLUJO DE CORREOS (emails / email_files / processes)
        //------------------------------------------------

        var emailId = $"manual-{Guid.NewGuid()}";
        var subject = $"Carga manual - {client}";
        var body = "";

        result.EmailId = emailId;

        try
        {
            //------------------------------------------------
            // 1. REGISTRAR "EMAIL" SINTÉTICO
            //------------------------------------------------

            await repository.SaveEmail(
                emailId,
                clientEmail,
                subject,
                body,
                DateTime.UtcNow);

            //------------------------------------------------
            // 2. SUBIR ARCHIVO ORIGINAL A STORAGE
            //------------------------------------------------

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            var upload = await storage.UploadFile(
                file.FileName,
                bytes);

            await repository.SaveFile(
                emailId,
                file.FileName,
                upload.StoredName,
                file.ContentType,
                "ORIGINAL",
                upload.StoragePath);

            //------------------------------------------------
            // 3. CREAR PROCESO Y MARCAR COMO EN PROGRESO
            //------------------------------------------------

            await repository.CreateProcess(emailId);

            await repository.UpdateEmailStatus(
                emailId,
                "PROCESSING");

            //------------------------------------------------
            // 4. REUTILIZAR EL PROCESAMIENTO EXISTENTE
            //    (reglas de sorteo, extracción con Gemini,
            //    agrupado, subida del archivo SORT)
            //------------------------------------------------

            await processingService.ProcessExcel(
                emailId,
                upload.StoragePath,
                subject,
                body,
                clientEmail);

            await repository.UpdateEmailStatus(
                emailId,
                "COMPLETED");

            result.Success = true;
            result.Status = "COMPLETED";
        }
        catch (Exception ex)
        {
            await repository.UpdateEmailStatus(
                emailId,
                "ERROR");

            await repository.UpdateProcess(
                emailId,
                "ERROR",
                DateTime.Now,
                DateTime.Now,
                ex.Message);

            result.Success = false;
            result.Status = "ERROR";
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}