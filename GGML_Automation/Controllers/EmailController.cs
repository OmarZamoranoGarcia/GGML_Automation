using GGML_Automation.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc;

namespace GGML_Automation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService emailService;

        public EmailController(
            IEmailService emailService)
        {
            this.emailService = emailService;
        }

        [HttpGet("check")]
        public async Task<IActionResult> Check()
        {
            try
            {
                var result = await emailService.CheckEmails();

                if (!result.Success)
                {
                    // Hubo al menos un error, pero la petición sí se ejecutó
                    return StatusCode(207, result); // 207 Multi-Status
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Falla no controlada (no debería llegar aquí si EmailService
                // captura bien sus propios errores, pero es la red de seguridad)
                return StatusCode(500, new EmailCheckResult
                {
                    Success = false,
                    Logs = new List<EmailLogEntry>
                    {
                        new EmailLogEntry
                        {
                            Level = EmailLogLevel.Error,
                            Message = ex.Message
                        }
                    }
                });
            }
        }
    }
}