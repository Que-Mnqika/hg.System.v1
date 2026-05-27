using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    public class FrontendCompatibilityController : ControllerBase
    {
        // Redirect legacy auth and health routes to the API equivalents
        [HttpPost("/auth/login")]
        [AllowAnonymous]
        public IActionResult PostAuthLogin()
        {
            return RedirectPreserveMethod("/api/auth/login");
        }

        [HttpGet("/auth")]
        [AllowAnonymous]
        public IActionResult GetAuthRoot()
        {
            return Redirect("/api/auth");
        }

        [HttpGet("/health")]
        [AllowAnonymous]
        public IActionResult GetHealthRoot()
        {
            return Redirect("/api/health");
        }

        [HttpGet("/__compat/status")]
        [AllowAnonymous]
        public IActionResult GetCompatStatusRoot()
        {
            return Redirect("/api/__compat/status");
        }

        // Analytics stubs
        [HttpGet("/api/v1/analytics/summary")]
        [AllowAnonymous]
        public IActionResult GetAnalyticsSummary()
        {
            return Ok(new
            {
                totalTrips = 0,
                totalBoardings = 0,
                activeDevices = 0,
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("/analytics/attendance")]
        [AllowAnonymous]
        public IActionResult GetAnalyticsAttendance()
        {
            return Ok(new { attendance = Array.Empty<object>() });
        }

        [HttpGet("/analytics/utilization")]
        [AllowAnonymous]
        public IActionResult GetAnalyticsUtilization()
        {
            return Ok(new { utilization = Array.Empty<object>() });
        }

        [HttpGet("/analytics/sla-dashboard")]
        [AllowAnonymous]
        public IActionResult GetAnalyticsSlaDashboard()
        {
            return Ok(new { sla = Array.Empty<object>() });
        }

        // Admin cloud services stub
        [HttpGet("/admin/cloud/services")]
        [AllowAnonymous]
        public IActionResult GetAdminCloudServices()
        {
            var services = new[]
            {
                new { name = "auth", status = "ok" },
                new { name = "database", status = "ok" },
                new { name = "realtime", status = "ok" }
            };

            return Ok(new { services, timestamp = DateTime.UtcNow });
        }

        // Feature pages stubs
        [HttpGet("/fuel/dashboard")]
        [AllowAnonymous]
        public IActionResult GetFuelDashboard()
        {
            return Ok(new { message = "fuel dashboard stub" });
        }

        [HttpGet("/maintenance-health/fleet/dashboard")]
        [AllowAnonymous]
        public IActionResult GetMaintenanceFleetDashboard()
        {
            return Ok(new { message = "maintenance health fleet dashboard stub" });
        }

        [HttpGet("/routes/optimization-stats")]
        [AllowAnonymous]
        public IActionResult GetRoutesOptimizationStats()
        {
            return Ok(new { stats = Array.Empty<object>() });
        }

        [HttpGet("/routes/optimization-history")]
        [AllowAnonymous]
        public IActionResult GetRoutesOptimizationHistory()
        {
            return Ok(new { history = Array.Empty<object>() });
        }

        // Panic and NoGoZone endpoints REMOVED - conflicts with real controllers
    }
}