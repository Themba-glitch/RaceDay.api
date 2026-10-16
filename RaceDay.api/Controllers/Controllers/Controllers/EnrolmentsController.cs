using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.api.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        /// <summary>Enrols the logged-in participant in a category, which also links them to its event (participant only).</summary>
        /// <remarks>Needs the category id in the URL; no body. Returns 201 with the enrolment, event and category details, 400 if already enrolled or the category is full, 403 if not a participant, or 404 if the category doesn't exist.</remarks>
        [ProducesResponseType(201), ProducesResponseType(400), ProducesResponseType(403), ProducesResponseType(404)]
        // Participant only: enrol in a category (this also links them to the event)
        [HttpPost("categories/{categoryId}/enrol")]
        [Authorize(Roles = "participant")]
        public async Task<IActionResult> Enrol(int categoryId)
        {
            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

            if (category == null) return NotFound("Category not found.");

            if (await _context.Enrolments.AnyAsync(e =>
                    e.UserID == CurrentUserId && e.CategoryID == categoryId))
                return BadRequest("You are already enrolled in this category.");

            if (category.MaxParticipants.HasValue)
            {
                var taken = await _context.Enrolments.CountAsync(e =>
                    e.CategoryID == categoryId && e.Status != "cancelled");
                if (taken >= category.MaxParticipants.Value)
                    return BadRequest("This category is full.");
            }

            var enrolment = new Enrolment
            {
                UserID = CurrentUserId,
                CategoryID = categoryId
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                enrolment.EnrolmentID,
                EventID = category.EventID,
                EventName = category.Event.EventName,
                enrolment.CategoryID,
                category.CategoryName,
                enrolment.Status,
                enrolment.PaymentStatus,
                message = "Enrolment successful."
            });
        }
        /// <summary>Lists the logged-in participant's own enrolments (participant only).</summary>
        /// <remarks>No body needed. Returns 200 with each enrolment's event, category, status and payment status, or 403 if not a participant.</remarks>
        [ProducesResponseType(200), ProducesResponseType(403)]
        // Participant only: view their own enrolments
        [HttpGet("enrolments/my")]
        [Authorize(Roles = "participant")]
        public async Task<IActionResult> MyEnrolments()
        {
            var list = await _context.Enrolments
                .Where(e => e.UserID == CurrentUserId)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.EnrolmentDate,
                    e.Status,
                    e.PaymentStatus,
                    e.CategoryID,
                    CategoryName = e.Category.CategoryName,
                    EventID = e.Category.EventID,
                    EventName = e.Category.Event.EventName,
                    EventDate = e.Category.Event.EventDate
                })
                .ToListAsync();

            return Ok(list);
        }
        /// <summary>Lists every enrolment on one of the organiser's own events (organiser only).</summary>
        /// <remarks>Needs the event id in the URL. Returns 200 with each participant's name, email, category and enrolment status, 403 if not the event's organiser, or 404 if the event doesn't exist.</remarks>
        [ProducesResponseType(200), ProducesResponseType(403), ProducesResponseType(404)]
        // Organiser only: view all enrolments for one of their events
        [HttpGet("events/{eventId}/enrolments")]
        [Authorize(Roles = "organiser")]
        public async Task<IActionResult> EventEnrolments(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound("Event not found.");
            if (ev.OrganiserID != CurrentUserId)
                return StatusCode(403, "You can only view enrolments for your own events.");

            var list = await _context.Enrolments
                .Where(e => e.Category.EventID == eventId)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.UserID,
                    ParticipantName = e.User.FullName,
                    ParticipantEmail = e.User.Email,
                    e.CategoryID,
                    CategoryName = e.Category.CategoryName,
                    e.EnrolmentDate,
                    e.Status,
                    e.PaymentStatus
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}