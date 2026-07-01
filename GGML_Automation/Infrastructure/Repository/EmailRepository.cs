using Dapper;
using Npgsql;

namespace GGML_Automation.Infrastructure.Repository;

public class EmailRepository : IEmailRepository
{

    private readonly IConfiguration configuration;


    public EmailRepository(
        IConfiguration configuration)
    {
        this.configuration = configuration;
    }



    private NpgsqlConnection Connection()
    {
        return new NpgsqlConnection(
            configuration.GetConnectionString("Supabase")
        );
    }



    public async Task<bool> EmailExists(
        string id)
    {

        using var db = Connection();

        var result = await db.ExecuteScalarAsync<int>(
            """
            select count(1)
            from emails_received
            where id=@id
            """,
            new { id }
        );

        return result > 0;
    }

    public async Task SaveEmail(
        string id,
        string arrivalEmail,
        string subject,
        string body,
        DateTime arrivalAt)
    {

        using var db = Connection();

        await db.ExecuteAsync(
        """
        insert into emails_received
        (
            id,
            arrival_email,
            subject,
            body,
            arrival_at
        )
        values
        (
            @id,
            @arrivalEmail,
            @subject,
            @body,
            @arrivalAt
        )
        """,
        new
        {
            id,
            arrivalEmail,
            subject,
            body,
            arrivalAt
        });

    }
    public async Task SaveFile(
        string emailId,
        string fileName,
        string fileType,
        string storagePath)
    {

        using var db = Connection();

        await db.ExecuteAsync(
        """
        insert into email_files
        (
            email_id,
            file_name,
            file_type,
            storage_path
        )
        values
        (
            @emailId,
            @fileName,
            @fileType,
            @storagePath
        )
        """,
        new
        {
            emailId,
            fileName,
            fileType,
            storagePath
        });
    }
}