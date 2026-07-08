using GGML_Automation.Infrastructure.AI.Models;

namespace GGML_Automation.Infrastructure.Excel
{
    public interface ICsvTableExtractor
    {
        string Extract(string csv,int headerRow,int startRow,int endRow);
    }
}
