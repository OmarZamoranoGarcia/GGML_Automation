namespace GGML_Automation.Infrastructure.Excel;

public interface IExcelReaderService
{
    Task<string> ConvertToCsv(byte[] fileBytes);
}