using GGML_Automation.Infrastructure.AI.Models;

public interface ITableExtractionService
{
    Task<TableData> ExtractTable(string csvContent);
    Task<TableData> ExtractTable(byte[] fileBytes);
    Task<TableDetectionResult> DetectTable(string csv);
}