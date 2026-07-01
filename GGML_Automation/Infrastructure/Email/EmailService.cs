using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

using GGML_Automation.Infrastructure.Repository;
using GGML_Automation.Infrastructure.Storage;

namespace GGML_Automation.Infrastructure.Email;


public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly IStorageService storage;
    private readonly IEmailRepository repository;


    public EmailService(
        IConfiguration configuration,
        IStorageService storage,
        IEmailRepository repository)
    {
        _configuration = configuration;
        this.storage = storage;
        this.repository = repository;
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


        await client.AuthenticateAsync(
            emailUser,
            emailPassword
        );


        var inbox = client.Inbox;


        await inbox.OpenAsync(
            FolderAccess.ReadWrite
        );


        var messages = await inbox.SearchAsync(
            SearchQuery.NotSeen
        );


        if (!messages.Any())
        {
            Console.WriteLine(
                "No hay correos nuevos por leer !!!!!"
            );

            await client.DisconnectAsync(true);
            return;
        }



        Console.WriteLine(
            $"Se encontraron {messages.Count} correos sin leer:"
        );

        Console.WriteLine(
            new string('-', 50)
        );


        int counter = 1;



        foreach (var id in messages)
        {

            var message =
                await inbox.GetMessageAsync(id);



            var emailId =
                message.MessageId;



            Console.WriteLine($"Correo #{counter}");
            Console.WriteLine($"Asunto: {message.Subject}");
            Console.WriteLine($"De: {message.From}");
            Console.WriteLine($"Fecha: {message.Date}");



            Console.WriteLine("Cuerpo:");

            if (!string.IsNullOrEmpty(message.TextBody))
            {
                Console.WriteLine(
                    message.TextBody
                );
            }
            else if (!string.IsNullOrEmpty(message.HtmlBody))
            {
                var plainText =
                    message.HtmlBody
                    .Replace("<br>", "\n")
                    .Replace("<p>", "\n");

                Console.WriteLine(
                    plainText
                );
            }
            else
            {
                Console.WriteLine(
                    "(Sin contenido de texto)"
                );
            }



            Console.WriteLine();



            if (await repository.EmailExists(emailId))
            {
                Console.WriteLine(
                    "Correo ya procesado anteriormente."
                );
            }
            else
            {

                await repository.SaveEmail(
                    emailId,
                    message.From.ToString(),
                    message.Subject,
                    message.TextBody ?? "",
                    message.Date.DateTime
                );



                if (message.Attachments.Any())
                {

                    Console.WriteLine(
                        $"Adjuntos ({message.Attachments.Count()}):"
                    );



                    foreach (var attachment in message.Attachments)
                    {

                        if (attachment is MimePart file)
                        {

                            Console.WriteLine(
                                $" - {file.FileName} ({file.ContentType.MimeType})"
                            );


                            using var ms =
                                new MemoryStream();


                            await file.Content.DecodeToAsync(ms);


                            var bytes =
                                ms.ToArray();



                            var path =
                                await storage.UploadFile(
                                    file.FileName,
                                    bytes
                                );



                            await repository.SaveFile(
                                emailId,
                                file.FileName,
                                file.ContentType.MimeType,
                                path
                            );



                            Console.WriteLine(
                                $"   Guardado en Supabase: {path}"
                            );

                        }
                        else if (attachment is MessagePart rfc822)
                        {

                            Console.WriteLine(
                                $" - Mensaje adjunto: {rfc822.Message.Subject}"
                            );

                        }

                    }

                }
                else
                {
                    Console.WriteLine(
                        "Adjuntos: Ninguno"
                    );
                }

            }



            Console.WriteLine(
                new string('-', 50)
            );



            await inbox.AddFlagsAsync(
                id,
                MessageFlags.Seen,
                true
            );


            counter++;

        }


        Console.WriteLine(
            $"Procesados {messages.Count} correos correctamente."
        );


        await client.DisconnectAsync(true);
    }
}