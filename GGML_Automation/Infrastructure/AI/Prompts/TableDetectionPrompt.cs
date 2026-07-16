namespace GGML_Automation.Infrastructure.AI.Prompts;

public static class TableDetectionPrompt
{
    public static string Build(string csv)
    {
        return @"
TAREA: Encuentra la tabla de mercancías en este CSV.

REGLAS ESTRICTAS - NO MODIFICAR NINGÚN DATO

1. **ENCABEZADOS (HEADER ROW)**:
   - Busca la PRIMERA fila que contiene TODOS estos términos (pueden estar en mayúsculas/minúsculas, con o sin acentos):
     * ""Clave SAT"", ""SAT""
     * ""DESCRIPCION"", ""Mercancia""
     * ""Cantidad"", ""Piezas""
     * ""Unidad"", ""medida""
     * ""Peso"", ""Kg""
     * ""Valor""
     * ""Moneda""
     * ""Fraccion"", ""Arancelaria""
     * ""Pedimento""
   - La fila de encabezados DEBE tener al menos 7 de estos 9 términos.
   - Asigna el número de fila (0-index) a ""headerRow"".
   - SI NO encuentras una fila con al menos 7 coincidencias, asigna 0 (primera fila) y usa los headers de ahí.

2. **PRIMERA FILA DE DATOS (firstDataRow)**:
   - Es la fila INMEDIATAMENTE POSTERIOR a los encabezados.
   - DEBE contener al menos 3 valores numéricos (números o decimales).
   - IGNORA filas vacías o que solo tengan texto basura como 'jhkh', 'gfhfhfhfghf'.
   - Asigna el número de fila a ""firstDataRow"".

3. **ÚLTIMA FILA DE DATOS (lastDataRow)**:
   - Busca la última fila que contiene datos válidos (con números) ANTES de que aparezcan:
     * Palabras como ""Total"", ""Subtotal"", ""IVA"", ""Suma""
     * Filas completamente vacías
     * Texto basura
   - Asigna el número de fila a ""lastDataRow"".

4. **HEADERS**:
   - Usa los nombres EXACTOS de la fila de encabezados que identificaste.
   - NO modifiques los nombres, NO los traduzcas, NO los cambies.
   - Mantén el mismo formato (mayúsculas/minúsculas, espacios, etc.).
   - Asigna el array de headers a ""headers"".

IMPORTANTE: 
- Los encabezados SIEMPRE están en una sola fila, no en varias.
- Cada columna tiene UN SOLO header.
- No inventes headers que no existen.

RESPONDE ÚNICAMENTE CON ESTE JSON (sin markdown, sin explicaciones, sin ```json):

EJEMPLO DE RESPUESTA CORRECTA:
{
  ""headerRow"": 0,
  ""firstDataRow"": 1,
  ""lastDataRow"": 29,
  ""headers"": [
    ""Clave SAT del Producto/Servicio"",
    ""DESCRIPCION DE LA MERCANCIA"",
    ""Cantidad de piezas"",
    ""Unidad de medida"",
    ""Peso en Kg"",
    ""Valor de la mercancia"",
    ""Moneda"",
    ""Fraccion Arancelaria"",
    ""Pedimento""
  ]
}

CSV A ANALIZAR:
" + csv + @"

---
REGLAS DE ORO:
1. Los encabezados están en la PRIMERA fila que contenga los términos clave.
2. NO ASIGNES -1 a menos que estés 100% seguro de que no hay datos.
3. SIEMPRE intenta encontrar la tabla aunque tenga imperfecciones.
---";
    }
}