using Dapper;
using Npgsql;

namespace GGML_Automation.Infrastructure.Repository;

public class EmailRepository : IEmailRepository
{
    private readonly IConfiguration configuration;

    public EmailRepository(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    private NpgsqlConnection Connection()
    {
        return new NpgsqlConnection(
            configuration.GetConnectionString("Supabase")
        );
    }

    public async Task<bool> EmailExists(string id)
    {
        using var db = Connection();

        var result = await db.ExecuteScalarAsync<int>(
            """
            select count(*)
            from emails_received
            where id=@id
            """,
            new { id });

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
            new { id, arrivalEmail, subject, body, arrivalAt });
    }

    public async Task SaveFile(
        string emailId,
        string fileName,
        string storedName,
        string fileType,
        string fileRole,
        string storagePath)
    {
        using var db = Connection();

        await db.ExecuteAsync(
            """
            insert into email_files
            (
                email_id,
                file_name,
                stored_name,
                file_type,
                file_role,
                storage_path
            )
            values
            (
                @emailId,
                @fileName,
                @storedName,
                @fileType,
                @fileRole,
                @storagePath
            )
            """,
            new { emailId, fileName, storedName, fileType, fileRole, storagePath });
    }

    public async Task UpdateEmailStatus(
        string emailId,
        string status)
    {
        using var db = Connection();

        await db.ExecuteAsync(
            """
            update emails_received
            set status=@status
            where id=@emailId
            """,
            new { emailId, status });
    }

    public async Task CreateProcess(string emailId)
    {
        using var db = Connection();

        await db.ExecuteAsync(
            """
            insert into email_processes(email_id)
            values(@emailId)
            """,
            new { emailId });
    }

    // SIMPLIFICADO (SIN COLUMNAS QUE NO EXISTEN)
    public async Task UpdateProcess(
        string emailId,
        string status,
        DateTime startedAt,
        DateTime finishedAt,
        string? errorMessage)
    {
        using var db = Connection();

        await db.ExecuteAsync(
            """
            update email_processes
            set
                status=@status,
                process_started_at=@startedAt,
                process_finished_at=@finishedAt,
                error_message=@errorMessage
            where email_id=@emailId
            """,
            new
            {
                emailId,
                status,
                startedAt,
                finishedAt,
                errorMessage
            });
    }
    public async Task UpdateProcessConfiguration(
    string emailId,
    string customer,
    string groupingColumns,
    string sumColumns,
    string aiModel)
    {
        using var db = Connection();

        await db.ExecuteAsync(
            """
        update email_processes
        set
            customer=@customer,
            grouping_columns=@groupingColumns,
            sum_columns=@sumColumns,
            ai_model=@aiModel
        where email_id=@emailId
        """,
            new
            {
                emailId,
                customer,
                groupingColumns,
                sumColumns,
                aiModel
            });
    }
}