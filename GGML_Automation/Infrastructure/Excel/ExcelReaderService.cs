using ClosedXML.Excel;
using System.Text;

namespace GGML_Automation.Infrastructure.Excel;

public class ExcelReaderService : IExcelReaderService
{
    public async Task<string> ConvertToCsv(byte[] fileBytes)
    {
        using var stream = new MemoryStream(fileBytes);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.First();

        // Detectar rango usado (tabla real)
        var range = worksheet.RangeUsed();

        if (range == null)
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var row in range.Rows())
        {
            var values = row.Cells()
                .Select(c => EscapeCsv(c.GetValue<string>()))
                .ToArray();

            sb.AppendLine(string.Join(",", values));
        }

        var csv = sb.ToString();

        Console.WriteLine("CSV generado correctamente:");
        Console.WriteLine(csv);

        return csv;
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        value = value.Replace("\"", "\"\"");

        if (value.Contains(",") || value.Contains("\n"))
        {
            value = $"\"{value}\"";
        }

        return value;
    }
}