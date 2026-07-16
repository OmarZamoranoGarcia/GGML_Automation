namespace GGML_Automation.Infrastructure.Sorting.Aliases
{
    public class HeaderAliases
    {
        public static readonly Dictionary<string, List<string>> Map = new()
        {
            ["ClaveSAT"] = [
                "Clave SAT",
                "Clave SAT del Producto/Servicio",
                "Clave Producto SAT",
                "Código SAT",
                "Codigo SAT",
                "SAT",
                "sat"
            ],

            ["FraccionArancelaria"] = [
                "Fraccion en KAI",
                "Fraccion Arancelaria",
                "Fracción Arancelaria"
            ],

            ["Unidaddemedida"] = [
                "Unidad de medida",
                "unidad de medida",
                "Unidad de medida",
                "medida",
                "Medida"
            ],

            ["Cantidaddepiezas"] = [
                "Cantidad de piezas",
                "Cant.",
                "Cant",
                "cant.",
                "cant",
                "cantidad",
                "Cantidad"
            ],

            ["Peso"] = [
                "Peso",
                "peso",
                "Peso en Kg",
                "peso en Kg"
            ]
        };
    }
}