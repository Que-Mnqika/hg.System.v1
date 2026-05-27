using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HGTSWebApi.DTOs;
using HGTSWebApi.Services;
using HGTSWebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/hce")]
    public class HceController : ControllerBase
    {
        private readonly ICredentialService _credentialService;
        private readonly AppDbContext _context;
        private readonly ILogger<HceController> _logger;

        public HceController(ICredentialService credentialService, AppDbContext context, ILogger<HceController> logger)
        {
            _credentialService = credentialService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/hce/validate - Validate HCE token (called by ESP32)
        /// </summary>
        [HttpPost("validate")]
        [AllowAnonymous]
        public async Task<ActionResult<HceValidationResponseDto>> ValidateToken([FromBody] HceValidationRequestDto request)
        {
            try
            {
                _logger.LogInformation("Validating HCE token");

                var (isValid, studentId, credentialType) = await _credentialService.ValidateCredentialTokenAsync(request.Token);

                if (!isValid)
                {
                    return Ok(new HceValidationResponseDto
                    {
                        AccessGranted = false,
                        Message = "Invalid or inactive credential",
                        ResultCode = "INVALID_TOKEN"
                    });
                }

                // Get student name
                var student = await _context.Students.FindAsync(studentId);

                return Ok(new HceValidationResponseDto
                {
                    AccessGranted = true,
                    Message = $"Welcome {student?.FullName ?? "Student"}",
                    StudentName = student?.FullName ?? "",
                    ResultCode = "SUCCESS"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating HCE token");
                return Ok(new HceValidationResponseDto
                {
                    AccessGranted = false,
                    Message = "System error",
                    ResultCode = "ERROR"
                });
            }
        }
    }
}