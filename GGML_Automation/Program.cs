using GGML_Automation.Infrastructure.AI;
using GGML_Automation.Infrastructure.Email;
using GGML_Automation.Infrastructure.Excel;
using GGML_Automation.Infrastructure.Grouping;
using GGML_Automation.Infrastructure.Processing;
using GGML_Automation.Infrastructure.Repository;
using GGML_Automation.Infrastructure.Sorting;
using GGML_Automation.Infrastructure.Storage;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//My services
builder.Configuration.AddUserSecrets<Program>(); //User secrets
builder.Services.AddScoped<IEmailService, EmailService>(); //Email service
builder.Services.AddScoped<IStorageService, SupabaseStorageService>(); //Storage service
builder.Services.AddScoped<IEmailRepository, EmailRepository>(); //Repository service
builder.Services.AddScoped<IExcelReaderService, ExcelReaderService>(); //Excel reader service
builder.Services.AddHttpClient<GeminiTableExtractionService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        var proxyUrl = Environment.GetEnvironmentVariable("HTTPS_PROXY") ?? Environment.GetEnvironmentVariable("HTTP_PROXY");
        if (!string.IsNullOrEmpty(proxyUrl))
        {
            handler.UseProxy = true;
            handler.Proxy = new System.Net.WebProxy(proxyUrl);
            handler.DefaultProxyCredentials = System.Net.CredentialCache.DefaultCredentials;
        }
        return handler;
    }); //HttpClient for Gemini table extraction service
builder.Services.AddScoped<ITableExtractionService>(sp => sp.GetRequiredService<GeminiTableExtractionService>()); //Table extraction service
builder.Services.AddScoped<ICsvTableExtractor, CsvTableExtractor>(); //Csv table extractor service
builder.Services.AddScoped<IExcelCleanerService,ExcelCleanerService>(); //Excel cleaner service
builder.Services.AddScoped<IExcelProcessingService, ExcelProcessingService>(); //Excel processing service
builder.Services.AddScoped<IExcelCleanerService, ExcelCleanerService>(); //Excel cleaner service
builder.Services.AddScoped<ISortingRuleService, SortingRuleService>(); //Sorting rule service
builder.Services.AddScoped<IGroupingService, GroupingService>(); //Grouping service

var supabaseUrl = builder.Configuration["Supabase:Url"];

var supabaseKey = builder.Configuration["Supabase:Key"];

var supabaseClient =
    new Supabase.Client(
        supabaseUrl,
        supabaseKey,
        new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        }
    );

builder.Services.AddSingleton(
    supabaseClient
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();