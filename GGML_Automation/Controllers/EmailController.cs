using DocumentFormat.OpenXml.VariantTypes;
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
            
            await emailService.CheckEmails();

            return Ok("Correo revisado");
        }
    }
}
