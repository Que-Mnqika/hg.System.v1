using Microsoft.AspNetCore.Mvc;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        // GET /api/health
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }

        // GET /api/__compat/status
        [HttpGet("__compat/status")]
        public IActionResult CompatStatus()
        {
            return Ok(new
            {
                apiVersion = "1.0.0",
                features = new[]
                {
                    "hce",
                    "offline_sync",
                    "realtime",
                    "password_reset",
                    "bulk_import",
                    "analytics"
                },
                supported = true,
                endpoints = new[]
                {
                    "/api/auth/login",
                    "/api/auth/me",
                    "/api/organizations",
                    "/api/users",
                    "/api/students",
                    "/api/routes",
                    "/api/vehicles",
                    "/api/trips",
                    "/api/boarding/validate",
                    "/api/devices"
                }
            });
        }
    }
}