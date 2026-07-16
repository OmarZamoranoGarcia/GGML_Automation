using GGML_Automation.Infrastructure.AI.Models;
using GGML_Automation.Infrastructure.Sorting.Models;

namespace GGML_Automation.Infrastructure.Grouping
{
    public interface IGroupingService
    {
        Task<TableData> Group(
        TableData table,
        SortingRule rule);
    }
}
