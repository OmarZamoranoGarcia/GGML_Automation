using GGML_Automation.Infrastructure.AI.Models;
using System.Text;

namespace GGML_Automation.Infrastructure.Excel
{
    public class CsvTableExtractor : ICsvTableExtractor
    {
        public string Extract(string csv,int headerRow,int startRow,int endRow)
        {
            var lines = csv
                .Replace("\r", "")
                .Split('\n');

            var builder = new StringBuilder();

            builder.AppendLine(lines[headerRow]);

            for (int i = startRow; i <= endRow; i++)
            {
                builder.AppendLine(lines[i]);
            }

            return builder.ToString();
        }
    }
}