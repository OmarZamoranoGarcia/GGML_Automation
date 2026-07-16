using System.Collections.Generic;

namespace GGML_Automation.Infrastructure.AI.Models;

public class TableData
{
    public List<string> Headers { get; set; } = new List<string>();

    public List<TableRow> Rows { get; set; } = new List<TableRow>();
}