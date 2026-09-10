namespace GGML_Automation.ManualProcessing.DTOs
{
    public class ManualuploadrequestDto
    {
        public string Client { get; set; } = "";
        public IFormFile File { get; set; } = null!;
    }
}
