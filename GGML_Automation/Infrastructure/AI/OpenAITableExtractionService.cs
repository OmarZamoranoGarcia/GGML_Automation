using GGML_Automation.Infrastructure.AI.Models;
using GGML_Automation.Infrastructure.Excel;
using System.Text;

namespace GGML_Automation.Infrastructure.AI;

public class OpenAITableExtractionService : ITableExtractionService
{
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
                Values = values
            });
        }

        return Task.FromResult(result);
    }

    // Accepts Excel bytes, converts to CSV using your ExcelReaderService and forwards to the CSV method.
    public async Task<TableData> ExtractTable(byte[] fileBytes)
    {
        var excelReader = new ExcelReaderService();
        var csv = await excelReader.ConvertToCsv(fileBytes);
        return await ExtractTable(csv);
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
}