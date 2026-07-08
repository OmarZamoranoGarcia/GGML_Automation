namespace GGML_Automation.Infrastructure.Excel
{
    public class ExtractedTable
    {
        public List<string> Headers { get; set; } = [];

        public List<List<string>> Rows { get; set; } = [];
    }
}