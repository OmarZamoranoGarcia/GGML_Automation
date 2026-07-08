using GGML_Automation.Infrastructure.AI.Models;

namespace GGML_Automation.Infrastructure.Excel
{
    public interface IExcelCleanerService
    {
        Task<byte[]> CreateCleanExcel(TableData table);
    }
}