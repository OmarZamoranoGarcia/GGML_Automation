using ClosedXML.Excel;
using ExcelDataReader;
using System.Text;

namespace GGML_Automation.Infrastructure.Excel;

public class ExcelReaderService : IExcelReaderService
{
    // Firma ZIP (OOXML / .xlsx, .xlsm) -> "PK"
    private static readonly byte[] XlsxSignature = { 0x50, 0x4B };

    // Firma OLE2 (BIFF / .xls legacy)
    private static readonly byte[] XlsSignature = { 0xD0, 0xCF, 0x11, 0xE0 };

    public async Task<string> ConvertToCsv(byte[] fileBytes)
    {
        var format = DetectFormat(fileBytes);

        Console.WriteLine($"Formato de Excel detectado: {format}");

        return format switch
        {
            ExcelFormat.Xlsx => ConvertXlsxToCsv(fileBytes),
            ExcelFormat.Xls => ConvertXlsToCsv(fileBytes),
            _ => throw new InvalidDataException(
                "El archivo no tiene una firma de Excel reconocible (ni OOXML .xlsx ni BIFF .xls). " +
                "Verifica que el adjunto no esté dañado o que realmente sea un archivo de Excel.")
        };
    }

    private static ExcelFormat DetectFormat(byte[] fileBytes)
    {
        if (fileBytes.Length >= XlsxSignature.Length &&
            fileBytes.Take(XlsxSignature.Length).SequenceEqual(XlsxSignature))
        {
            return ExcelFormat.Xlsx;
        }

        if (fileBytes.Length >= XlsSignature.Length &&
            fileBytes.Take(XlsSignature.Length).SequenceEqual(XlsSignature))
        {
            return ExcelFormat.Xls;
        }

        return ExcelFormat.Unknown;
    }

    // ---------- .xlsx (OOXML) con ClosedXML ----------
    private string ConvertXlsxToCsv(byte[] fileBytes)
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

        return sb.ToString();
    }

    // ---------- .xls (BIFF legacy) con ExcelDataReader ----------
    private string ConvertXlsToCsv(byte[] fileBytes)
    {
        // Requiere el paquete System.Text.Encoding.CodePages para leer
        // los encodings (code pages) que usan los archivos .xls antiguos.
        Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var stream = new MemoryStream(fileBytes);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var sb = new StringBuilder();

        // Solo la primera hoja, igual que el flujo de .xlsx (Worksheets.First())
        if (reader.Read())
        {
            do
            {
                var values = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => EscapeCsv(reader.GetValue(i)?.ToString() ?? string.Empty))
                    .ToArray();

                sb.AppendLine(string.Join(",", values));
            }
            while (reader.Read());
        }

        return sb.ToString();
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

    private enum ExcelFormat
    {
        Unknown,
        Xlsx,
        Xls
    }
}