using GGML_Automation.Infrastructure.AI.Models;
using GGML_Automation.Infrastructure.Sorting.Models;
using GGML_Automation.Infrastructure.Excel;
using GGML_Automation.Infrastructure.Grouping.Algorithms;
using System.IO;
using System.Threading.Tasks;

namespace GGML_Automation.Infrastructure.Grouping
{
    public class GroupingService : IGroupingService
    {
        private static int contadorArchivos = 0;
        public async Task<TableData> Group(
        TableData table,
        SortingRule rule)
        {
            Console.WriteLine();
            Console.WriteLine("========== GROUPING ==========");

            Console.WriteLine($"Columnas para agrupar:");
            foreach (var column in rule.GroupColumns)
                Console.WriteLine($" - {column}");

            Console.WriteLine();
            Console.WriteLine("Columnas para sumar:");
            foreach (var column in rule.SumColumns)
                Console.WriteLine($" - {column}");

            Console.WriteLine();

            // Llamar al ExcelSorter para que haga el orden y sumas
            var sorter = new ExcelSorter();
            TableData result;
            try
            {
                result = sorter.Execute(table, rule);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[ERROR] ExcelSorter falló: {ex.Message}");
                // devolver la tabla original si falla
                return await Task.FromResult(table);
            }         

            return await Task.FromResult(result);
        }
    }
}
