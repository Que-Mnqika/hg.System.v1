using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/NFCCredential")]
    public class NFCCredentialController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NFCCredentialController> _logger;

        public NFCCredentialController(AppDbContext context, ILogger<NFCCredentialController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /nfccredential
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NFCCredentialDto>>> GetAll()
        {
            try
            {
                var credentials = await _context.NFCCredentials
                    .Include(c => c.Student)
                    .Select(c => new NFCCredentialDto
                    {
                        CredentialId = c.CredentialId,
                        CredentialUid = c.CredentialUid,
                        CredentialType = c.CredentialType,
                        StudentId = c.StudentId,
                        StudentName = c.Student != null ? c.Student.FullName : null,
                        StudentNumber = c.Student != null ? c.Student.StudentNumber : null,
                        IssuedDate = c.IssuedDate,
                        IsActive = c.IsActive
                    })
                    .ToListAsync();

                return Ok(credentials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credentials");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /nfccredential/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<NFCCredentialDto>> GetById(Guid id)
        {
            try
            {
                var credential = await _context.NFCCredentials
                    .Include(c => c.Student)
                    .FirstOrDefaultAsync(c => c.CredentialId == id);

                if (credential == null)
                    return NotFound();

                var dto = new NFCCredentialDto
                {
                    CredentialId = credential.CredentialId,
                    CredentialUid = credential.CredentialUid,
                    CredentialType = credential.CredentialType,
                    StudentId = credential.StudentId,
                    StudentName = credential.Student?.FullName,
                    StudentNumber = credential.Student?.StudentNumber,
                    IssuedDate = credential.IssuedDate,
                    IsActive = credential.IsActive
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /nfccredential/uid/{uid}
        [HttpGet("uid/{uid}")]
        public async Task<ActionResult<NFCCredentialDto>> GetByUid(string uid)
        {
            try
            {
                var credential = await _context.NFCCredentials
                    .Include(c => c.Student)
                    .FirstOrDefaultAsync(c => c.CredentialUid == uid);

                if (credential == null)
                    return NotFound();

                var dto = new NFCCredentialDto
                {
                    CredentialId = credential.CredentialId,
                    CredentialUid = credential.CredentialUid,
                    CredentialType = credential.CredentialType,
                    StudentId = credential.StudentId,
                    StudentName = credential.Student?.FullName,
                    StudentNumber = credential.Student?.StudentNumber,
                    IssuedDate = credential.IssuedDate,
                    IsActive = credential.IsActive
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential by UID");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /nfccredential
        [HttpPost]
        public async Task<ActionResult<NFCCredentialDto>> Create([FromBody] CreateNFCCredentialDto dto)
        {
            try
            {
                // Check if UID already exists
                var existing = await _context.NFCCredentials
                    .FirstOrDefaultAsync(c => c.CredentialUid == dto.CredentialUid);

                if (existing != null)
                    return BadRequest(new { error = "Credential UID already exists" });

                // Check if student exists
                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student == null)
                    return BadRequest(new { error = "Student not found" });

                var credential = new NFCCredential
                {
                    CredentialId = Guid.NewGuid(),
                    CredentialUid = dto.CredentialUid,
                    CredentialType = dto.CredentialType,
                    StudentId = dto.StudentId,
                    IssuedDate = DateTime.UtcNow,
                    IsActive = dto.IsActive
                };

                _context.NFCCredentials.Add(credential);
                await _context.SaveChangesAsync();

                var response = new NFCCredentialDto
                {
                    CredentialId = credential.CredentialId,
                    CredentialUid = credential.CredentialUid,
                    CredentialType = credential.CredentialType,
                    StudentId = credential.StudentId,
                    StudentName = student.FullName,
                    StudentNumber = student.StudentNumber,
                    IssuedDate = credential.IssuedDate,
                    IsActive = credential.IsActive
                };

                return CreatedAtAction(nameof(GetById), new { id = credential.CredentialId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating credential");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PUT /nfccredential/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNFCCredentialDto dto)
        {
            try
            {
                var credential = await _context.NFCCredentials.FindAsync(id);
                if (credential == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(dto.CredentialType))
                    credential.CredentialType = dto.CredentialType;

                credential.IsActive = dto.IsActive;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Credential updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating credential");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE /nfccredential/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var credential = await _context.NFCCredentials.FindAsync(id);
                if (credential == null)
                    return NotFound();

                _context.NFCCredentials.Remove(credential);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Credential deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting credential");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /nfccredential/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<object>> CreateBulkCredentials([FromBody] List<CreateNFCCredentialDto> requests)
        {
            try
            {
                var created = new List<NFCCredentialDto>();
                var errors = new List<string>();

                foreach (var request in requests)
                {
                    try
                    {
                        // Check if UID already exists
                        var existing = await _context.NFCCredentials
                            .FirstOrDefaultAsync(c => c.CredentialUid == request.CredentialUid);

                        if (existing != null)
                        {
                            errors.Add($"Credential UID {request.CredentialUid} already exists");
                            continue;
                        }

                        // Check if student exists
                        var student = await _context.Students.FindAsync(request.StudentId);
                        if (student == null)
                        {
                            errors.Add($"Student {request.StudentId} not found for credential {request.CredentialUid}");
                            continue;
                        }

                        var credential = new NFCCredential
                        {
                            CredentialId = Guid.NewGuid(),
                            CredentialUid = request.CredentialUid,
                            CredentialType = request.CredentialType,
                            StudentId = request.StudentId,
                            IssuedDate = DateTime.UtcNow,
                            IsActive = request.IsActive
                        };

                        _context.NFCCredentials.Add(credential);

                        var response = new NFCCredentialDto
                        {
                            CredentialId = credential.CredentialId,
                            CredentialUid = credential.CredentialUid,
                            CredentialType = credential.CredentialType,
                            StudentId = credential.StudentId,
                            StudentName = student.FullName,
                            StudentNumber = student.StudentNumber,
                            IssuedDate = credential.IssuedDate,
                            IsActive = credential.IsActive
                        };

                        created.Add(response);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error adding {request.CredentialUid}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Successfully created {created.Count} credentials",
                    totalRequested = requests.Count,
                    created,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk credentials");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}