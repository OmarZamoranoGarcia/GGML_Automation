using GGML_Automation.Infrastructure.Sorting.Models;

namespace GGML_Automation.Infrastructure.Sorting
{
    public class SortingRuleService : ISortingRuleService
    {
        private readonly IConfiguration configuration;

        public SortingRuleService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public Task<SortingRule> GetRule(string from,string subject,string body)
        {
            var cliente1 = configuration["Cliente1:Email"];
            var cliente2 = configuration["Cliente2:Email"];
            from = from.ToLower();
            subject = subject.ToLower();
            body = body.ToLower();

            return Task.FromResult(
                from switch
                {
                    var f when f.Contains(cliente1) => new SortingRule
                    {
                        Customer = cliente1,

                        GroupColumns =
                        [
                            "ClaveSAT",
                            "Unidaddemedida",
                            "FraccionArancelaria",                
                        ],

                        SumColumns =
                        [
                            "Cantidaddepiezas",
                            "Peso",
                        ]
                    },
                    var f when f.Contains(cliente2) => new SortingRule
                    {
                        Customer = cliente2,

                        GroupColumns =
                        [                          
                            "FraccionArancelaria"
                        ],

                        SumColumns =
                        [
                            "Cantidaddepiezas",
                            "Peso",
                        ]
                    },               

                    _ => throw new Exception(
                        $"No existen reglas para el cliente: {from}")
                });
        }
    }
}