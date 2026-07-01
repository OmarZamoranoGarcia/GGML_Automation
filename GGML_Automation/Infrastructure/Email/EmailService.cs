using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace GGML_Automation.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public async Task CheckEmails()
    {
        var emailUser = _configuration["Email:User"];
        var emailPassword = _configuration["Email:Password"];
        var emailService = _configuration["Email:Service"];
        var emailPort = _configuration["Email:Port"];

        using var client = new ImapClient();

        await client.ConnectAsync(
            emailService,
            int.Parse(emailPort),
            SecureSocketOptions.SslOnConnect
        );

        await client.AuthenticateAsync(emailUser, emailPassword);

        var inbox = client.Inbox;

        await inbox.OpenAsync(
            FolderAccess.ReadWrite
        );

        var messages = await inbox.SearchAsync(
            SearchQuery.NotSeen
        );

        // Verificar si hay correos sin leer
        if (!messages.Any())
        {
            Console.WriteLine("No hay correos nuevos por leer !!!!!");
        }
        else
        {
            Console.WriteLine($"Se encontraron {messages.Count} correos sin leer:");
            Console.WriteLine(new string('-', 50));

            // Enumerar cada correo
            int counter = 1;
            foreach (var id in messages)
            {
                var message = await inbox.GetMessageAsync(id);

                Console.WriteLine($"   Correo #{counter}");
                Console.WriteLine($"   Asunto: {message.Subject}");
                Console.WriteLine($"   De: {message.From}");
                Console.WriteLine($"   Fecha: {message.Date}");           
                Console.WriteLine($"   Cuerpo:");
                if (!string.IsNullOrEmpty(message.TextBody))
                {
                    Console.WriteLine($"   {message.TextBody}");
                }
                else if (!string.IsNullOrEmpty(message.HtmlBody))
                {
                    // Si solo tiene HTML, lo mostramos pero sin etiquetas (versión simplificada)
                    var plainText = message.HtmlBody.Replace("<br>", "\n").Replace("<p>", "\n");
                    Console.WriteLine($"   {plainText}");
                }
                else
                {
                    Console.WriteLine("   (Sin contenido de texto)");
                }

                // Mostrar información de adjuntos si existen
                if (message.Attachments.Any())
                {
                    Console.WriteLine($"   Adjuntos ({message.Attachments.Count()}):");
                    foreach (var attachment in message.Attachments)
                    {
                        if (attachment is MimePart file)
                        {
                            Console.WriteLine($"      - {file.FileName} ({file.ContentType.MimeType})");
                        }
                        else if (attachment is MessagePart rfc822)
                        {
                            Console.WriteLine($"      - Mensaje adjunto: {rfc822.Message.Subject}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("   Adjuntos: Ninguno");
                }

                Console.WriteLine(new string('-', 50));

                // Marcar como leído
                await inbox.AddFlagsAsync(
                    id,
                    MessageFlags.Seen,
                    true
                );

                counter++;
            }

            Console.WriteLine($"Procesados {messages.Count} correos correctamente.");
        }

        await client.DisconnectAsync(true);
    }
}