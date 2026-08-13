using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BackendApi.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new 
            { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                message = "Backend is running"
            });
        }

        [HttpGet("python")]
        public IActionResult CheckPython()
        {
            try
            {
                var pythonPath = Environment.GetEnvironmentVariable("PYTHON_PATH") ?? "python";
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                {
                    return Ok(new { status = "error", message = "Could not start Python process" });
                }

                process.WaitForExit(5000);
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    return Ok(new { status = "ok", python_version = output.Trim() });
                }
                else
                {
                    return Ok(new { status = "error", message = error.Trim() });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet("groq")]
        public IActionResult CheckGroq()
        {
            var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                return Ok(new { status = "error", message = "GROQ_API_KEY not set" });
            }

            return Ok(new { status = "ok", message = "GROQ_API_KEY is configured" });
        }
    }
}