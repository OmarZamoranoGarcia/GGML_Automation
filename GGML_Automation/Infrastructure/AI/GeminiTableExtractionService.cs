using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using GGML_Automation.Infrastructure.AI.Models;
using GGML_Automation.Infrastructure.AI.Prompts;
using GGML_Automation.Infrastructure.Excel;
using Microsoft.AspNetCore.Http.HttpResults;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using static Dapper.SqlMapper;

namespace GGML_Automation.Infrastructure.AI;

public class GeminiTableExtractionService : ITableExtractionService
{
    private readonly IConfiguration configuration;
    private readonly HttpClient http;
    public GeminiTableExtractionService(IConfiguration configuration, HttpClient http)
    {
        this.configuration = configuration;
        this.http = http;
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

        Console.WriteLine();
        Console.WriteLine("========== CSV ==========");
        Console.WriteLine(csv);
        Console.WriteLine("=========================");

        var detection =
            await DetectTable(csv);

        Console.WriteLine();
        Console.WriteLine("===== TABLA DETECTADA =====");
        Console.WriteLine($"HeaderRow    : {detection.HeaderRow}");
        Console.WriteLine($"FirstDataRow : {detection.FirstDataRow}");
        Console.WriteLine($"LastDataRow  : {detection.LastDataRow}");
        Console.WriteLine("===========================");

        return BuildTable(csv, detection);
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
        var apiKey = configuration["GoogleAI:ApiKey"];
        //URL BASE CORRECTA para Gemini
        var baseUrl = configuration["GoogleAI:BaseUrl"];
        //MODELO CORRECTO de Gemini
        var model = configuration["GoogleAI:Model"];

        var prompt = TableDetectionPrompt.Build(csv);

        //ENDPOINT CORRECTO para Gemini (generateContent)
        var url = $"{baseUrl}/models/{model}:generateContent" + (string.IsNullOrEmpty(apiKey) ? "" : $"?key={apiKey}");

        //PAYLOAD CORRECTO para Gemini (formato generateContent)
        var payload = new
        {
            contents = new[]
            {
            new
            {
                parts = new[]
                {
                    new { text = prompt }
                }
            }
        },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 16000,
                topP = 0.95
            }
        };

        var json = JsonSerializer.Serialize(payload);

        // ---- Llamada a Gemini con reintentos ante 429 / 503 ----
        var (response, responseBody) = await SendWithRetryAsync(url, json);

        //Debug para ver que muuestra la respuesta de Gemini
        Console.WriteLine();
        Console.WriteLine("========== RESPUESTA GOOGLE AI ==========");
        Console.WriteLine($"Status: {(int)response.StatusCode}");
        Console.WriteLine(responseBody);
        Console.WriteLine("=======================================");

        if (!response.IsSuccessStatusCode)
        {
            // Si después de los reintentos sigue fallando, dejamos claro
            // en el mensaje de error que el problema viene del lado de Gemini,
            // no de nuestra API.
            throw new Exception(
                $"La API de Google Gemini no respondió correctamente después de varios intentos " +
                $"(status {(int)response.StatusCode} - {response.StatusCode}). " +
                $"Esto normalmente indica saturación o límite de cuota en Gemini, no un error en GGML_Automation. " +
                $"Detalle: {responseBody}");
        }

        // PARSEO CORRECTO para respuesta de Gemini
        string contentText = null!;
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Gemini devuelve: candidates[0].content.parts[0].text
            if (root.TryGetProperty("candidates", out var candidates) &&
                candidates.ValueKind == JsonValueKind.Array &&
                candidates.GetArrayLength() > 0)
            {
                var first = candidates[0];
                if (first.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.ValueKind == JsonValueKind.Array &&
                    parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("text", out var text) &&
                        text.ValueKind == JsonValueKind.String)
                    {
                        contentText = text.GetString()!;
                    }
                }
            }

            // Fallback si no encuentra la estructura esperada
            if (string.IsNullOrEmpty(contentText))
            {
                contentText = responseBody;
            }
        }
        catch
        {
            contentText = responseBody;
        }

        if (string.IsNullOrWhiteSpace(contentText))
            throw new Exception("Google AI did not return any textual output.");

        // Extract JSON embedded in the text
        string cleanJson;
        try
        {
            cleanJson = ExtractJson(contentText);
        }
        catch
        {
            cleanJson = contentText.Trim();
        }

        var detection = JsonSerializer.Deserialize<TableDetectionResult>(
            cleanJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (detection == null)
            throw new Exception("Unable to parse JSON response from Google AI.");

        return detection;
    }

    // Reintenta la llamada a Gemini cuando responde 429 (Too Many Requests)
    // o 503 (Service Unavailable / modelo saturado), con backoff exponencial.
    // Estos dos códigos casi siempre son transitorios del lado de Google,
    // no errores de nuestro código.
    private async Task<(HttpResponseMessage response, string body)> SendWithRetryAsync(
        string url,
        string jsonPayload,
        int maxRetries = 3)
    {
        HttpResponseMessage response = null!;
        string body = "";

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            response = await http.SendAsync(request);
            body = await response.Content.ReadAsStringAsync();

            var isTransient =
                response.StatusCode == HttpStatusCode.TooManyRequests ||   // 429
                response.StatusCode == HttpStatusCode.ServiceUnavailable;  // 503

            if (response.IsSuccessStatusCode || !isTransient)
            {
                // Éxito, o un error que no tiene sentido reintentar (400, 401, etc.)
                return (response, body);
            }

            if (attempt < maxRetries)
            {
                var delaySeconds = Math.Pow(2, attempt); // 2s, 4s, 8s...
                Console.WriteLine(
                    $"[Gemini] Intento {attempt}/{maxRetries} falló con {(int)response.StatusCode} " +
                    $"({response.StatusCode}). Reintentando en {delaySeconds}s...");

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }

        // Se agotaron los reintentos, regresamos la última respuesta tal cual
        // para que el llamador decida qué hacer (en DetectTable se convierte
        // en una excepción clara).
        return (response, body);
    }

    private TableData BuildTable(
    string csv,
    TableDetectionResult detection)
    {
        var lines = csv
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .ToList();

        if (detection.HeaderRow >= lines.Count)
            throw new Exception("HeaderRow fuera de rango.");

        if (detection.FirstDataRow >= lines.Count)
            throw new Exception("FirstDataRow fuera de rango.");

        if (detection.LastDataRow >= lines.Count)
            detection.LastDataRow = lines.Count - 1;

        var table = new TableData();

        table.Headers =
            ParseCsvLine(lines[detection.HeaderRow]);

        for (int i = detection.FirstDataRow;
             i <= detection.LastDataRow;
             i++)
        {
            table.Rows.Add(
                new TableRow
                {
                    Cells = ParseCsvLine(lines[i])
                });
        }

        return table;
    }
    private string ExtractJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new Exception("IA no devolvió contenido.");

        // Elimina bloques Markdown
        text = text.Replace("```json", "")
                   .Replace("```", "")
                   .Trim();

        // Busca el primer '{'
        int start = text.IndexOf('{');

        // Busca el último '}'
        int end = text.LastIndexOf('}');

        if (start < 0 || end < 0 || end <= start)
            throw new Exception("No se encontró un JSON válido en la respuesta de la IA.");

        return text.Substring(start, end - start + 1);
    }
}