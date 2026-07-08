using GGML_Automation.Infrastructure.AI.Models;

namespace GGML_Automation.Infrastructure.AI
{
    public interface ITableAnalyzer
    {
        Task<TableLocation> Analyze(string csv);
    }
}