using GGML_Automation.ManualProcessing.Services;
using GGML_Automation.ManualProcessing.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GGML_Automation.ManualProcessing.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManualProcessingController : ControllerBase
    {
        private readonly IManualProcessingUploadService manualUploadService;

        public ManualProcessingController(
            IManualProcessingUploadService manualUploadService)
        {
            this.manualUploadService = manualUploadService;
        }

        // POST api/ManualProcessing/process
        // multipart/form-data:
        //   file   -> IFormFile (.xlsx, .xls, .csv)
        //   client -> string ("Cliente1", "Cliente2", "Cliente3", "Cliente4")
        [HttpPost("process")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Process([FromForm] ManualuploadrequestDto request)
        {
            try
            {
                var result = await manualUploadService.ProcessManualExcel(
                    request.Client,
                    request.File);

                if (!result.Success)
                {
                    return StatusCode(207, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ManualUploadResultDto
                {
                    Success = false,
                    Client = request.Client,
                    Status = "ERROR",
                    ErrorMessage = ex.Message
                });
            }
        }
    }
}