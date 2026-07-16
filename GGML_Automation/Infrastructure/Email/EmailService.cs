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

    public async Task CheckEmails()
    {
        var emailUser = configuration["Email:User"];
        var emailPassword = configuration["Email:Password"];
        var emailHost = configuration["Email:Service"];
        var emailPort = configuration["Email:Port"];

        using var client = new ImapClient();

        await client.ConnectAsync(
            emailHost,
            int.Parse(emailPort!),
            SecureSocketOptions.SslOnConnect);

        await client.AuthenticateAsync(
            emailUser,
            emailPassword);

        //------------------------------------------------
        // BANDEJA DE ENTRADA
        //------------------------------------------------

        await ProcessFolder(client.Inbox);

        //------------------------------------------------
        // SPAM
        //------------------------------------------------

        try
        {
            var spam =
                await client.GetFolderAsync("[Gmail]/Spam");

            await ProcessFolder(spam);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("No fue posible abrir la carpeta Spam.");
            Console.WriteLine(ex.Message);
            Console.WriteLine("========================================");
        }

        await client.DisconnectAsync(true);
    }

    //read emails from Spam
    private async Task ProcessFolder(
    IMailFolder folder)
    {
        await folder.OpenAsync(FolderAccess.ReadWrite);

        var uids =
            await folder.SearchAsync(SearchQuery.NotSeen);

        Console.WriteLine();
        Console.WriteLine($"Carpeta : {folder.FullName}");
        Console.WriteLine($"Correos : {uids.Count}");

        foreach (var uid in uids)
        {
            var message =
                await folder.GetMessageAsync(uid);

            await ProcessEmail(message);

            await folder.AddFlagsAsync(
                uid,
                MessageFlags.Seen,
                true);
        }

        await folder.CloseAsync();
    }

    private async Task ProcessEmail(MimeMessage message)
    {
        var emailId =
            message.MessageId;

        PrintEmail(message);

        if (await repository.EmailExists(emailId))
        {
            Console.WriteLine("Correo ya registrado.");
            return;
        }

        var body =
            GetBody(message);

        await repository.SaveEmail(
            emailId,
            message.From.ToString(),
            message.Subject ?? "",
            body,
            message.Date.DateTime);

        await repository.CreateProcess(
            emailId);

        await repository.UpdateEmailStatus(
            emailId,
            "PROCESSING");

        try
        {
            await SaveAttachments(
                emailId,
                message);

            await repository.UpdateEmailStatus(
                emailId,
                "COMPLETED");
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
            throw;
        }
    }
    private async Task SaveAttachments(
        string emailId,
        MimeMessage message)
    {
        if (!message.Attachments.Any())
        {
            Console.WriteLine("Adjuntos: Ninguno");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Adjuntos ({message.Attachments.Count()}):");

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

                Console.WriteLine("=== INICIANDO PROCESO EXCEL ===");

                await processingService.ProcessExcel(
                    emailId,
                    upload.StoragePath,
                    message.Subject ?? "",
                    GetBody(message),
                    message.From.ToString());

                Console.WriteLine("=== PROCESO EXCEL FINALIZADO ===");
                Console.WriteLine();          
            }
            else if (attachment is MessagePart rfc822)
            {
                Console.WriteLine(
                    $" - Mensaje adjunto: {rfc822.Message.Subject}");
            }
        }
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