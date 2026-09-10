namespace GGML_Automation.ManualProcessing.DTOs
{
    public class ManualUploadResultDto
    {
        public bool Success { get; set; }
        public string EmailId { get; set; } = "";
        public string Client { get; set; } = "";
        public string Status { get; set; } = "";
        public string? ErrorMessage { get; set; }
    }
}
