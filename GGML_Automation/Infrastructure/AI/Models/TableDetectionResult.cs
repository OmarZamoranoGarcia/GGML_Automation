namespace GGML_Automation.Infrastructure.AI.Models
{
    public class TableDetectionResult
    {
        public int HeaderRow { get; set; }

        public int FirstDataRow { get; set; }

        public int LastDataRow { get; set; }

        public List<string> Headers { get; set; } = [];
    }
}