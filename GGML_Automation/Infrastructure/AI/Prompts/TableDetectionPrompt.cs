namespace GGML_Automation.Infrastructure.AI.Prompts;

public static class TableDetectionPrompt
{
    public static string Build(string csv)
    {
        return
        """
            Analiza el siguiente CSV.

            Puede contener:

            - títulos
            - notas
            - encabezados
            - celdas vacías
            - varias tablas
            - texto basura

            Encuentra únicamente la tabla principal de mercancías.

            Devuelve EXCLUSIVAMENTE un JSON válido.

            Formato:

            {
              "headerRow": 0,
              "firstDataRow": 1,
              "lastDataRow": 25,
              "headers":[]
            }

            No escribas markdown.

            No escribas ```json.

            No expliques nada.

            CSV:

            """ +
        csv;
    }
}