using GGML_Automation.ManualProcessing.DTOs;

namespace GGML_Automation.ManualProcessing.Services
{
    public interface IManualProcessingUploadService
    {
        Task<ManualUploadResultDto> ProcessManualExcel(
        string client,
        IFormFile file);
    }
}
