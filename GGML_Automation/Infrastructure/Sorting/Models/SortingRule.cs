namespace GGML_Automation.Infrastructure.Sorting.Models
{
    public class SortingRule
    {
        public string Customer { get; set; } = "";

        public List<string> GroupColumns { get; set; } = new List<string>();

        public List<string> SumColumns { get; set; } = new List<string>();
    }
}
