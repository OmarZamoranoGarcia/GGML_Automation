using GGML_Automation.Infrastructure.AI;
using GGML_Automation.Infrastructure.Excel;
using GGML_Automation.Infrastructure.Grouping;
using GGML_Automation.Infrastructure.Repository;
using GGML_Automation.Infrastructure.Sorting;
using GGML_Automation.Infrastructure.Storage;

namespace GGML_Automation.Infrastructure.Processing;

public class ExcelProcessingService : IExcelProcessingService
{
    private readonly IConfiguration configuration;
    private readonly IStorageService storage;
    private readonly ITableExtractionService tableService;
    private readonly IExcelCleanerService excelCleaner;
    private readonly IEmailRepository repository;
    private readonly ISortingRuleService sortingRuleService;
    private readonly IGroupingService groupingService;

    public ExcelProcessingService(
        IStorageService storage,
        ITableExtractionService tableService,
        IExcelCleanerService excelCleaner,
        IEmailRepository repository,
        ISortingRuleService sortingRuleService,
        IConfiguration configuration,
        IGroupingService groupingService)
    {
        this.storage = storage;
        this.tableService = tableService;
        this.excelCleaner = excelCleaner;
        this.repository = repository;
        this.sortingRuleService = sortingRuleService;
        this.configuration = configuration;
        this.groupingService = groupingService;
    }

    public async Task ProcessExcel(
        string emailId,
        string originalStoragePath,
        string subject,
        string body,
        string sender)
    {
        Console.WriteLine();
        Console.WriteLine("========== PROCESANDO EXCEL ==========");

        //------------------------------------------------
        // 1 OBTENER REGLAS DEL CLINTE
        //------------------------------------------------

        var rule =
        await sortingRuleService.GetRule(
            sender,
            subject,
            body);

        if (rule is null)
        {
            await repository.UpdateProcessConfiguration(
                emailId,
                "UNKNOWN",
                "",
                "",
                configuration["Gemini:Model"]!);

            await repository.UpdateProcess(
                emailId,
                "IGNORED",
                DateTime.Now,
                DateTime.Now,
                "No existe una regla para este cliente.");

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("No existen reglas para este cliente.");
            Console.WriteLine("Se omite el procesamiento.");
            Console.WriteLine("========================================");

            return;
        }

        await repository.UpdateProcessConfiguration(
            emailId,
            rule.Customer,
            string.Join(",", rule.GroupColumns),
            string.Join(",", rule.SumColumns),
            configuration["GoogleAI:Model"]!);

        Console.WriteLine($"Cliente : {rule.Customer}");
        Console.WriteLine($"Agrupar : {string.Join(", ", rule.GroupColumns)}");
        Console.WriteLine($"Sumar   : {string.Join(", ", rule.SumColumns)}");

        //------------------------------------------------
        // 2 DESCARGAR EXCEL ORIGINAL
        //------------------------------------------------

        var originalFile =
            await storage.DownloadFile(originalStoragePath);

        Console.WriteLine("Excel descargado.");

        //------------------------------------------------
        // NOMBRE DEL ARCHIVO
        //------------------------------------------------

        var originalName =
            Path.GetFileName(originalStoragePath);

        var sortName =
            $"{Path.GetFileNameWithoutExtension(originalName)}_SORT{Path.GetExtension(originalName)}";

        Console.WriteLine($"Archivo original : {originalName}");
        Console.WriteLine($"Archivo SORT     : {sortName}");

        //------------------------------------------------
        // 3 EXTRAER TABLA CON IA
        //------------------------------------------------

        var table =
            await tableService.ExtractTable(originalFile);

        Console.WriteLine("Tabla detectada.");

        Console.WriteLine($"Columnas : {table.Headers.Count}");

        Console.WriteLine($"Filas    : {table.Rows.Count}");

        table = await groupingService.Group(table,rule); // Llenar los campos para sortear y sumar según la regla

        //------------------------------------------------
        // 4 CREAR EXCEL LIMPIO
        //------------------------------------------------

        //var cleanExcel =
        //await excelCleaner.CreateCleanExcel(table);

        var sortedExcel =
            excelCleaner.CreateFinalExcel(table);

        Console.WriteLine("Excel CLEAN generado.");

        //------------------------------------------------
        // 5 SUBIR A SUPABASE
        //------------------------------------------------

        var upload =
            await storage.UploadFile(
                sortName,
                sortedExcel);

        Console.WriteLine("Excel CLEAN subido.");

        //------------------------------------------------
        // 6 REGISTRAR EN email_files
        //------------------------------------------------

        await repository.SaveFile(
            emailId,
            sortName,
            upload.StoredName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "SORT",
            upload.StoragePath);

        Console.WriteLine("Registro CLEAN guardado en supabase.");

        Console.WriteLine();
        Console.WriteLine("========== FIN ==========");
    }
}