using ClosedXML.Excel;
using GGML_Automation.Infrastructure.AI.Models;
using GGML_Automation.Infrastructure.Sorting;
using GGML_Automation.Infrastructure.Sorting.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace GGML_Automation.Infrastructure.Grouping.Algorithms
{
    public class ExcelSorter
    {
        public TableData Execute(TableData table, SortingRule rule)
        {
            // 1. Resolver nombres de columnas -> indices
            int[] indicesGrupo = ResolverIndices(table.Headers, rule.GroupColumns);
            int[] indicesSuma = ResolverIndices(table.Headers, rule.SumColumns);

            // 2. Ejecutar agrupamiento
            var grupos = ObtenerFilasAgrupadas(table.Rows, indicesGrupo);

            // 3. Construir el TableData de salida
            var resultado = new TableData
            {
                Headers = new List<string>(table.Headers),
                Rows = new List<TableRow>()
            };

            foreach (var grupo in grupos)
            {
                // Sumas por cada columna indicada
                var sumas = new double[indicesSuma.Length];

                foreach (var fila in grupo)
                {
                    resultado.Rows.Add(fila);

                    for (int s = 0; s < indicesSuma.Length; s++)
                    {
                        // proteger índice fuera de rango
                        var idx = indicesSuma[s];
                        if (idx >= 0 && idx < fila.Cells.Count)
                            sumas[s] += TryParseDouble(fila.Cells[idx]);
                    }
                }

                // Fila de suma
                var filaSuma = new TableRow
                {
                    Cells = Enumerable.Repeat(string.Empty, table.Headers.Count).ToList()
                };
                for (int s = 0; s < indicesSuma.Length; s++)
                {
                    filaSuma.Cells[indicesSuma[s]] = sumas[s].ToString(CultureInfo.InvariantCulture);
                }
                resultado.Rows.Add(filaSuma);

                // Fila en blanco de separacion
                resultado.Rows.Add(new TableRow
                {
                    Cells = Enumerable.Repeat(string.Empty, table.Headers.Count).ToList()
                });
            }

            return resultado;
        }

        private static int[] ResolverIndices(List<string> headers,List<string> nombresColumnas)
        {
            var resolver = new HeaderResolver();

            return nombresColumnas
                .Select(nombre => resolver.Resolve(headers, nombre))
                .ToArray();
        }

        static double TryParseDouble(string valor)
        {
            double.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out double resultado);
            return resultado;
        }

        // Agrupamiento y orden
        static List<List<TableRow>> ObtenerFilasAgrupadas(List<TableRow> filas, params int[] indices)
        {
            var rows = filas.Where(r => !r.Cells.All(c => string.IsNullOrWhiteSpace(c)));

            // Aplicar orden por los indices proporcionados
            IOrderedEnumerable<TableRow>? ordered = null;
            for (int i = 0; i < indices.Length; i++)
            {
                int idx = indices[i];
                if (i == 0)
                    ordered = rows.OrderBy(r => r.Cells.ElementAtOrDefault(idx));
                else
                    ordered = ordered.ThenBy(r => r.Cells.ElementAtOrDefault(idx));
            }

            var finalSeq = (ordered ?? rows);

            // Agrupar por clave concatenada
            const char delim = '\u0001';
            var grupos = finalSeq
                .GroupBy(r => string.Join(delim.ToString(), indices.Select(i => r.Cells.ElementAtOrDefault(i) ?? string.Empty)))
                .Select(g => g.ToList())
                .ToList();

            return grupos;
        }

        // Metodo de depuracion para crear el xls sorteado localmanete
        public string ExportarParaDepuracion(TableData table, string rutaSalida)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (string.IsNullOrWhiteSpace(rutaSalida)) throw new ArgumentException("rutaSalida is required", nameof(rutaSalida));

            var dir = Path.GetDirectoryName(rutaSalida);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Crear workbook en memoria
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Salida_Procesada");

            // Headers
            for (int col = 0; col < table.Headers.Count; col++)
            {
                var cell = ws.Cell(1, col + 1);
                cell.Value = table.Headers[col] ?? string.Empty;
                cell.Style.Font.Bold = true;
            }

            ws.SheetView.FreezeRows(1);

            // Rows
            int filaActual = 2;
            foreach (var fila in table.Rows)
            {
                for (int col = 0; col < table.Headers.Count; col++)
                {
                    string value = string.Empty;
                    if (col < fila.Cells.Count)
                        value = fila.Cells[col] ?? string.Empty;

                    ws.Cell(filaActual, col + 1).Value = value;
                }
                filaActual++;
            }

            ws.Columns(1, Math.Max(1, table.Headers.Count)).AdjustToContents();

            // Guardar en memoria y luego escribir en disco (evita handles abiertos por ClosedXML)
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();

            try
            {
                File.WriteAllBytes(rutaSalida, bytes);
                return rutaSalida;
            }
            catch (IOException)
            {
                // Archivo destino bloqueado -> crear archivo alterno con timestamp
                var fileName = Path.GetFileNameWithoutExtension(rutaSalida);
                var ext = Path.GetExtension(rutaSalida);
                var safeName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                var altPath = Path.Combine(dir ?? string.Empty, safeName);
                File.WriteAllBytes(altPath, bytes);
                return altPath;
            }
        }
    }
}