using GGML_Automation.Infrastructure.Sorting.Aliases;

namespace GGML_Automation.Infrastructure.Sorting
{
    public class HeaderResolver
    {
        public int Resolve(
        List<string> headers,
        string key)
        {
            if (!HeaderAliases.Map.TryGetValue(key, out var aliases))
            {
                throw new Exception(
                    $"No existen alias registrados para '{key}'.");
            }

            for (int i = 0; i < headers.Count; i++)
            {
                var excelHeader = Normalize(headers[i]);

                foreach (var alias in aliases)
                {
                    if (excelHeader == Normalize(alias))
                    {
                        return i;
                    }
                }
            }

            throw new Exception(
                $"No se encontró ninguna columna equivalente a '{key}'.");
        }

        private static string Normalize(string value)
        {
            return value
                .ToLowerInvariant()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace("/", "")
                .Replace("\\", "");
        }
    }
}