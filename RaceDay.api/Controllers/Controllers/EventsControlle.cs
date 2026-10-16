using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.api.DTOs;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private static readonly string[] ValidTypes = { "run", "walk", "cycle" };

        public EventsController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        /// <summary>Lists all events, soonest first (organiser or participant).</summary>
        /// <remarks>No body needed. Returns 200 with each event's id, organiser, name, type, date, location, description and distance, or 401 if not logged in.</remarks>
        [ProducesResponseType(200), ProducesResponseType(401)]
        // Both roles: list all events
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventID,
                    e.OrganiserID,
                    e.EventName,
                    e.EventType,
                    e.EventDate,
                    e.Location,
                    e.Description,
                    e.DistanceKM
                })
                .ToListAsync();

            return Ok(events);
        }
        /// <summary>Returns one event by its id (organiser or participant).</summary>
        /// <remarks>Needs the event id in the URL. Returns 200 with the event, 404 if it doesn't exist, or 401 if not logged in.</remarks>
        [ProducesResponseType(200), ProducesResponseType(404), ProducesResponseType(401)]
        // Both roles: view one event
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _context.Events
                .Where(e => e.EventID == id)
                .Select(e => new
                {
                    e.EventID,
                    e.OrganiserID,
                    e.EventName,
                    e.EventType,
                    e.EventDate,
                    e.Location,
                    e.Description,
                    e.DistanceKM
                })
                .FirstOrDefaultAsync();

            if (ev == null) return NotFound("Event not found.");
            return Ok(ev);
        }
        /// <summary>Creates an event owned by the logged-in organiser (organiser only).</summary>
        /// <remarks>Expects JSON: eventName, eventType ("run", "walk" or "cycle"), eventDate, location, description, distanceKM. Returns 201 with the new event id, 400 for invalid data, or 403 if the user is not an organiser.</remarks>
        [ProducesResponseType(201), ProducesResponseType(400), ProducesResponseType(403)]
        // Organiser only: create an event
        [HttpPost]
        /// <summary>Creates an event owned by the logged-in organiser (organiser only).</summary>
        /// <remarks>Expects JSON: eventName, eventType ("run", "walk" or "cycle"), eventDate, location, description, distanceKM. Returns 201 with the new event id, 400 for invalid data, or 403 if the user is not an organiser.</remarks>
        [ProducesResponseType(201), ProducesResponseType(400), ProducesResponseType(403)]
        [Authorize(Roles = "organiser")]
        public async Task<IActionResult> CreateEvent(EventDto dto)
        {
            if (!ValidTypes.Contains(dto.EventType))
                return BadRequest("EventType must be 'run', 'walk' or 'cycle'.");

            var ev = new Event
            {
                OrganiserID = CurrentUserId,
                EventName = dto.EventName,
                EventType = dto.EventType,
                EventDate = dto.EventDate,
                Location = dto.Location,
                Description = dto.Description ?? "",
                DistanceKM = dto.DistanceKM
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID },
                new { ev.EventID, message = "Event created." });
        }
        /// <summary>Updates one of the organiser's own events (organiser only).</summary>
        /// <remarks>Needs the event id in the URL and the same JSON body as create. Returns 200 "Event updated.", 400 for invalid data, 403 if not an organiser or not the owner, or 404 if the event doesn't exist.</remarks>
        [ProducesResponseType(200), ProducesResponseType(400), ProducesResponseType(403), ProducesResponseType(404)]
        // Organiser only: update their own event
        [HttpPut("{id}")]
        [Authorize(Roles = "organiser")]
        public async Task<IActionResult> UpdateEvent(int id, EventDto dto)
        {
            if (!ValidTypes.Contains(dto.EventType))
                return BadRequest("EventType must be 'run', 'walk' or 'cycle'.");

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound("Event not found.");
            if (ev.OrganiserID != CurrentUserId)
                return StatusCode(403, "You can only change your own events.");

            ev.EventName = dto.EventName;
            ev.EventType = dto.EventType;
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location;
            ev.Description = dto.Description ?? "";
            ev.DistanceKM = dto.DistanceKM;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Event updated." });
        }

        // Organiser only: delete their own event
        [HttpDelete("{id}")]
        [Authorize(Roles = "organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound("Event not found.");
            if (ev.OrganiserID != CurrentUserId)
                return StatusCode(403, "You can only delete your own events.");

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Event deleted." });
        }
    }
}