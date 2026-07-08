using System.Text.Json.Serialization;

namespace GGML_Automation.Infrastructure.AI.Responses
{
    public class OpenAIResponse
    {
        [JsonPropertyName("headerRow")]
        public int HeaderRow { get; set; }

        [JsonPropertyName("firstDataRow")]
        public int FirstDataRow { get; set; }

        [JsonPropertyName("lastDataRow")]
        public int LastDataRow { get; set; }

        [JsonPropertyName("headers")]
        public List<string> Headers { get; set; } = [];
    }
}