using DocumentFormat.OpenXml.Wordprocessing;
using GGML_Automation.Infrastructure.AI;
using GGML_Automation.Infrastructure.Excel;
using GGML_Automation.Infrastructure.Processing;
using GGML_Automation.Infrastructure.Repository;
using GGML_Automation.Infrastructure.Storage;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace GGML_Automation.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration configuration;
    private readonly IStorageService storage;
    private readonly IEmailRepository repository;
    private readonly ITableExtractionService tableService;
    private readonly IExcelCleanerService excelCleaner;
    private readonly IExcelProcessingService processingService;

    public EmailService(
        IConfiguration configuration,
        IStorageService storage,
        IEmailRepository repository,
        ITableExtractionService tableService,
        IExcelCleanerService excelCleaner,
        IExcelProcessingService processingService)
    {
        this.configuration = configuration;
        this.storage = storage;
        this.repository = repository;
        this.tableService = tableService;
        this.excelCleaner = excelCleaner;
        this.processingService = processingService;
    }

    public async Task<EmailCheckResult> CheckEmails()
    {
        var result = new EmailCheckResult();

        var emailUser = configuration["Email:User"];
        var emailPassword = configuration["Email:Password"];
        var emailHost = configuration["Email:Service"];
        var emailPort = configuration["Email:Port"];

        using var client = new ImapClient();

        try
        {
            await client.ConnectAsync(
                emailHost,
                int.Parse(emailPort!),
                SecureSocketOptions.SslOnConnect);

            await client.AuthenticateAsync(
                emailUser,
                emailPassword);
        }
        catch (Exception ex)
        {
            result.AddLog(EmailLogLevel.Error, $"No fue posible conectar/autenticar: {ex.Message}");
            return result; // sin conexión no tiene caso seguir
        }

        //------------------------------------------------
        // BANDEJA DE ENTRADA
        //------------------------------------------------

        try
        {
            await ProcessFolder(client.Inbox, result);
        }
        catch (Exception ex)
        {
            result.AddLog(EmailLogLevel.Error, $"Error procesando bandeja de entrada: {ex.Message}");
        }

        //------------------------------------------------
        // SPAM
        //------------------------------------------------

        try
        {
            var spam =
                await client.GetFolderAsync("[Gmail]/Spam");

            await ProcessFolder(spam, result);
        }
        catch (Exception ex)
        {
            result.AddLog(EmailLogLevel.Warning, $"No fue posible abrir la carpeta Spam: {ex.Message}");
        }

        await client.DisconnectAsync(true);

        result.AddLog(
            EmailLogLevel.Info,
            $"Revisión completada. Procesados: {result.Processed}, Omitidos: {result.Skipped}, Errores: {result.Errors}");

        return result;
    }

    private async Task ProcessFolder(
        IMailFolder folder,
        EmailCheckResult result)
    {
        await folder.OpenAsync(FolderAccess.ReadWrite);

        var uids =
            await folder.SearchAsync(SearchQuery.NotSeen);

        result.TotalEmailsFound += uids.Count;
        result.AddLog(EmailLogLevel.Info, $"Carpeta '{folder.FullName}': {uids.Count} correo(s) sin leer.");

        foreach (var uid in uids)
        {
            var message =
                await folder.GetMessageAsync(uid);

            await ProcessEmail(message, result);

            await folder.AddFlagsAsync(
                uid,
                MessageFlags.Seen,
                true);
        }

        await folder.CloseAsync();
    }

    private async Task ProcessEmail(
        MimeMessage message,
        EmailCheckResult result)
    {
        var emailId =
            message.MessageId;

        PrintEmail(message);

        var entry = new EmailProcessResult
        {
            EmailId = emailId,
            Subject = message.Subject ?? "",
            From = message.From.ToString()
        };

        if (await repository.EmailExists(emailId))
        {
            entry.Status = "SKIPPED";
            result.Skipped++;
            result.Emails.Add(entry);
            result.AddLog(EmailLogLevel.Info, $"Correo ya registrado: {emailId}");
            return;
        }

        var body =
            GetBody(message);

        await repository.SaveEmail(
            emailId,
            message.From.ToString(),
            message.Subject ?? "",
            body,
            message.Date.UtcDateTime);

        await repository.CreateProcess(
            emailId);

        await repository.UpdateEmailStatus(
            emailId,
            "PROCESSING");

        try
        {
            var excelFilesProcessed =
                await SaveAttachments(
                    emailId,
                    message);

            await repository.UpdateEmailStatus(
                emailId,
                "COMPLETED");

            if (excelFilesProcessed == 0)
            {
                entry.Status = "NOT_PROCESSED";
                entry.Note = "El adjunto no es un archivo Excel (tipo OTHER); no se procesa, solo se almacena.";
                result.NotProcessed++;
                result.AddLog(EmailLogLevel.Info, $"Correo con adjunto tipo OTHER, no procesado: {emailId} ({entry.Subject})");
            }
            else
            {
                entry.Status = "COMPLETED";
                result.Processed++;
                result.AddLog(EmailLogLevel.Info, $"Correo procesado correctamente: {emailId} ({entry.Subject})");
            }
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

            entry.Status = "ERROR";
            entry.ErrorMessage = ex.Message;
            result.Errors++;
            result.AddLog(EmailLogLevel.Error, $"Error procesando correo {emailId} ({entry.Subject}): {ex.Message}");
        }

        result.Emails.Add(entry);
    }

    private async Task<int> SaveAttachments(
    string emailId,
    MimeMessage message)
    {
        if (!message.Attachments.Any())
        {
            Console.WriteLine("Adjuntos: Ninguno");
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine($"Adjuntos ({message.Attachments.Count()}):");

        var excelFilesProcessed = 0;

        foreach (var attachment in message.Attachments)
        {
            if (attachment is MimePart file)
            {
                Console.WriteLine(
                    $" - {file.FileName} ({file.ContentType.MimeType})");

                using var ms = new MemoryStream();

                await file.Content.DecodeToAsync(ms);

                var bytes = ms.ToArray();

                var upload =
                    await storage.UploadFile(
                        file.FileName!,
                        bytes);

                var role =
                    GetFileRole(file.FileName!);

                await repository.SaveFile(
                    emailId,
                    file.FileName!,
                    upload.StoredName,
                    file.ContentType.MimeType,
                    role,
                    upload.StoragePath);

                Console.WriteLine($"   ✔ Guardado en Storage");
                Console.WriteLine($"   Nombre original : {file.FileName}");
                Console.WriteLine($"   Nombre Storage  : {upload.StoredName}");
                Console.WriteLine($"   Rol             : {role}");

                if (role != "ORIGINAL")
                {
                    Console.WriteLine($"   (Omitido del procesamiento Excel: rol '{role}')");
                    Console.WriteLine();
                    continue;
                }

                Console.WriteLine("=== INICIANDO PROCESO EXCEL ===");

                await processingService.ProcessExcel(
                    emailId,
                    upload.StoragePath,
                    message.Subject ?? "",
                    GetBody(message),
                    message.From.ToString());

                excelFilesProcessed++;

                Console.WriteLine("=== PROCESO EXCEL FINALIZADO ===");
                Console.WriteLine();
            }
            else if (attachment is MessagePart rfc822)
            {
                Console.WriteLine(
                    $" - Mensaje adjunto: {rfc822.Message.Subject}");
            }
        }

        return excelFilesProcessed;
    }

    private void PrintEmail(
        MimeMessage message)
    {
        Console.WriteLine();

        Console.WriteLine($"Asunto : {message.Subject}");
        Console.WriteLine($"De     : {message.From}");
        Console.WriteLine($"Fecha  : {message.Date}");

        Console.WriteLine();

        Console.WriteLine("Cuerpo:");

        Console.WriteLine(
            GetBody(message));

        Console.WriteLine();
    }

    private string GetBody(
        MimeMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.TextBody))
        {
            return message.TextBody;
        }

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            return message.HtmlBody
                .Replace("<br>", "\n")
                .Replace("<br/>", "\n")
                .Replace("<br />", "\n")
                .Replace("<p>", "\n")
                .Replace("</p>", "")
                .Replace("&nbsp;", " ");
        }

        return "";
    }

    private string GetFileRole(
        string fileName)
    {
        var extension =
            Path.GetExtension(fileName)
            .ToLower();

        return extension switch
        {
            ".xlsx" => "ORIGINAL",
            ".xls" => "ORIGINAL",
            ".csv" => "ORIGINAL",

            _ => "OTHER"
        };
    }
}