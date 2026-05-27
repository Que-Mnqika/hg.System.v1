using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("/panic")]
    public class PanicController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PanicController> _logger;

        public PanicController(AppDbContext context, ILogger<PanicController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /panic/active
        [HttpGet("/panic/active")]
        public async Task<IActionResult> GetActive()
        {
            var events = await _context.PanicEvents
                .OrderByDescending(e => e.CreatedAt)
                .Take(50)
                .Select(e => new {
                    id = e.PanicEventId,
                    status = e.Status,
                    createdAt = e.CreatedAt,
                    detail = e.Details
                })
                .ToListAsync();

            // Basic aggregates
            var total = await _context.PanicEvents.CountAsync();
            var active = await _context.PanicEvents.CountAsync(e => e.Status == "active");
            var resolved = await _context.PanicEvents.CountAsync(e => e.Status == "resolved");

            return Ok(new { total, active, resolved, events });
        }

        [HttpGet("/panic/events/{id:guid}")]
        public async Task<IActionResult> GetEvent(Guid id)
        {
            var ev = await _context.PanicEvents
                //.Include(e => e.Organization)
                .FirstOrDefaultAsync(e => e.PanicEventId == id);

            if (ev == null)
                return NotFound();

            var chats = await _context.PanicChatMessages
                .Where(c => c.PanicEventId == ev.PanicEventId)
                .OrderBy(c => c.SentAt)
                .Select(c => new { id = c.PanicChatMessageId, sender = c.SenderId, message = c.Message, sentAt = c.SentAt })
                .ToListAsync();

            return Ok(new { id = ev.PanicEventId, status = ev.Status, details = ev.Details, createdAt = ev.CreatedAt, chats });
        }

        [HttpPatch("/panic/events/{id:guid}")]
        public async Task<IActionResult> PatchEvent(Guid id, [FromBody] PanicEvent update)
        {
            var ev = await _context.PanicEvents.FindAsync(id);
            if (ev == null)
                return NotFound();

            if (!string.IsNullOrEmpty(update.Status))
                ev.Status = update.Status;
            if (!string.IsNullOrEmpty(update.Details))
                ev.Details = update.Details;

            await _context.SaveChangesAsync();
            return Ok(ev);
        }

        [HttpPost("/panic/events/{id:guid}/chat")]
        public async Task<IActionResult> PostChat(Guid id, [FromBody] PanicChatMessage msg)
        {
            var ev = await _context.PanicEvents.FindAsync(id);
            if (ev == null)
                return NotFound();

            msg.PanicChatMessageId = Guid.NewGuid();
            msg.PanicEventId = id;
            msg.SentAt = DateTime.UtcNow;
            _context.PanicChatMessages.Add(msg);
            await _context.SaveChangesAsync();

            return Ok(msg);
        }

        [HttpPost("/panic/events")]
        public async Task<IActionResult> CreateEvent([FromBody] PanicEvent ev)
        {
            ev.PanicEventId = Guid.NewGuid();
            ev.CreatedAt = DateTime.UtcNow;
            _context.PanicEvents.Add(ev);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = ev.PanicEventId }, ev);
        }
    }
}
