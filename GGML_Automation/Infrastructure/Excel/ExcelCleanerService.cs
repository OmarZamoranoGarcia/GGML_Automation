using ClosedXML.Excel;
using GGML_Automation.Infrastructure.AI.Models;

namespace GGML_Automation.Infrastructure.Excel
{
    public class ExcelCleanerService : IExcelCleanerService
    {
        public async Task<byte[]> CreateCleanExcel(TableData table)
        {
            using var workbook = new XLWorkbook();

            var sheet = workbook.Worksheets.Add("Tabla");

            //--------------------------------------------------
            // Encabezados
            //--------------------------------------------------

            for (int col = 0; col < table.Headers.Count; col++)
            {
                sheet.Cell(1, col + 1).Value =
                    table.Headers[col];

                sheet.Cell(1, col + 1).Style.Font.Bold = true;
            }

            //--------------------------------------------------
            // Filas
            //--------------------------------------------------

            for (int row = 0; row < table.Rows.Count; row++)
            {
                for (int col = 0; col < table.Rows[row].Cells.Count; col++)
                {
                    sheet.Cell(row + 2, col + 1).Value =
                        table.Rows[row].Cells[col];
                }
            }

            sheet.Columns().AdjustToContents();

            using var ms = new MemoryStream();

            workbook.SaveAs(ms);

            Console.WriteLine();
            Console.WriteLine("Excel limpio generado.");
            Console.WriteLine($"Filas: {table.Rows.Count}");
            Console.WriteLine($"Columnas: {table.Headers.Count}");

            return await Task.FromResult(ms.ToArray());
        }

        public byte[] CreateFinalExcel(TableData table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            using var workbook = new XLWorkbook();

            var sheet = workbook.Worksheets.Add("Resultado");

            //------------------------------------------------
            // Headers
            //------------------------------------------------

            for (int col = 0; col < table.Headers.Count; col++)
            {
                sheet.Cell(1, col + 1).Value = table.Headers[col];

                sheet.Cell(1, col + 1).Style.Font.Bold = true;
            }

            //------------------------------------------------
            // Rows
            //------------------------------------------------

            for (int row = 0; row < table.Rows.Count; row++)
            {
                for (int col = 0; col < table.Rows[row].Cells.Count; col++)
                {
                    sheet.Cell(row + 2, col + 1).Value =
                        table.Rows[row].Cells[col];
                }
            }

            sheet.Columns().AdjustToContents();

            using var ms = new MemoryStream();

            workbook.SaveAs(ms);

            return ms.ToArray();
        }
    }
}