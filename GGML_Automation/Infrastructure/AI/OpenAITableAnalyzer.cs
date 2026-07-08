using System.Text.Json;
using GGML_Automation.Infrastructure.AI.Models;
using OpenAI.Chat;

namespace GGML_Automation.Infrastructure.AI
{
    public class OpenAITableAnalyzer : ITableAnalyzer
    {
        private readonly IConfiguration configuration;

        public OpenAITableAnalyzer(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<TableLocation> Analyze(string csv)
        {
            var apiKey = configuration["OpenAI:ApiKey"];

            var model = configuration["OpenAI:Model"];

            var client = new ChatClient(model!,apiKey!);

            var prompt = """
                            You are an expert at identifying tables inside CSV files.

                            The CSV may contain:

                            - Empty rows
                            - Company titles
                            - Notes
                            - Totals
                            - Random text
                            - Garbage values

                            Your job is ONLY to identify the main product table.

                            Return ONLY valid JSON.

                            Example:

                            {
                                "HeaderRow":8,
                                "StartRow":10,
                                "EndRow":38
                            }

                            CSV:

                """ + csv;

            var response = await client.CompleteChatAsync(prompt);

            var text =
                response.Value.Content[0].Text;

            Console.WriteLine();
            Console.WriteLine("========== IA ==========");
            Console.WriteLine(text);
            Console.WriteLine("========================");
            Console.WriteLine();

            var location =JsonSerializer.Deserialize<TableLocation>(text,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (location == null)
            {
                throw new Exception("La IA no pudo identificar la tabla.");
            }

            return location;
        }
    }
}