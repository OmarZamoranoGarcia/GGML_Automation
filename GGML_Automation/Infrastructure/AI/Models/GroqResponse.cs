namespace GGML_Automation.Infrastructure.AI.Models;

public class GroqResponse
{
    public string Id { get; set; }
    public string Object { get; set; }
    public long Created { get; set; }
    public string Model { get; set; }
    public List<GroqChoice> Choices { get; set; }
    public GroqUsage Usage { get; set; }
}

public class GroqChoice
{
    public int Index { get; set; }
    public GroqMessage Message { get; set; }
    public string FinishReason { get; set; }
}

public class GroqMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
}

public class GroqUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}