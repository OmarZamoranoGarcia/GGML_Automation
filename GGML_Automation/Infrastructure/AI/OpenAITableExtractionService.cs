using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using GGML_Automation.Infrastructure.AI.Models;
using GGML_Automation.Infrastructure.AI.Prompts;
using GGML_Automation.Infrastructure.Excel;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text;
using System.Text.Json;
using static Dapper.SqlMapper;
using OpenAI;
using OpenAI.Chat;

namespace GGML_Automation.Infrastructure.AI;

public class OpenAITableExtractionService : ITableExtractionService
{
    private readonly IConfiguration configuration;
    public OpenAITableExtractionService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }
    public Task<TableData> ExtractTable(string csvContent)
    {
        var result = new TableData();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return Task.FromResult(result);
        }

        // Normaliza saltos de línea
        var normalized = csvContent
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        var lines = normalized
            .Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count == 0)
        {
            return Task.FromResult(result);
        }

        // Primera línea = encabezados
        result.Headers = ParseCsvLine(lines[0]);

        // Resto = filas de datos
        for (int i = 1; i < lines.Count; i++)
        {
            var values = ParseCsvLine(lines[i]);

            result.Rows.Add(new TableRow
            {
                Cells = values
            });
        }

        return Task.FromResult(result);
    }

    // Accepts Excel bytes, converts to CSV using your ExcelReaderService and forwards to the CSV method.
    public async Task<TableData> ExtractTable(byte[] fileBytes)
    {
        var excelReader = new ExcelReaderService();

        var csv =
            await excelReader.ConvertToCsv(fileBytes);


        var detection =
            await DetectTable(csv);


        var table =
            BuildTableData(
                csv,
                detection
            );


        return table;
    }

    // Parser simple de una línea CSV, soporta comillas y comas dentro de campos entre comillas.
    private List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (insideQuotes)
            {
                if (c == '"')
                {
                    // Comilla escapada ("")
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        insideQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    insideQuotes = true;
                }
                else if (c == ',' || c == ';')
                {
                    values.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        values.Add(current.ToString().Trim());

        return values;
    }
    // metodo del promprt de deteccion de tabla
    public async Task<TableDetectionResult> DetectTable(string csv)
    {
        var apiKey =
            configuration["OpenAI:ApiKey"];

        var model =
            configuration["OpenAI:Model"];


        ChatClient chatClient =
            new(
                model,
                apiKey
            );


        var prompt =
            TableDetectionPrompt.Build(csv);


        ChatCompletion completion =
            await chatClient.CompleteChatAsync(
            [
                new UserChatMessage(prompt)
            ]);


        var content =
            completion.Content[0].Text;


        Console.WriteLine();
        Console.WriteLine("========== RESPUESTA IA ==========");
        Console.WriteLine(content);
        Console.WriteLine("==================================");


        var result =
            JsonSerializer.Deserialize<TableDetectionResult>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });


        return result
            ?? throw new Exception(
                "La IA no devolvió una detección válida");
    }
    private TableData BuildTableData(
    string csv,
    TableDetectionResult detection)
    {
        var result = new TableData();


        var lines = csv
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .ToList();



        result.Headers =
            detection.Headers;



        for (
            int i = detection.FirstDataRow;
            i <= detection.LastDataRow;
            i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;


            result.Rows.Add(
                new TableRow
                {
                    Cells =
                        ParseCsvLine(lines[i])
                });
        }


        return result;
    }
}