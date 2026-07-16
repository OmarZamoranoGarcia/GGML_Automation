using GGML_Automation.Infrastructure.Sorting.Models;

namespace GGML_Automation.Infrastructure.Sorting
{
    public interface ISortingRuleService
    {
        Task<SortingRule?> GetRule(
        string from,
        string subject,
        string body);
    }
}
