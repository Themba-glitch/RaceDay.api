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
    [Route("api")]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Captures a finish time and positions for an enrolment on the organiser's own event (organiser only).</summary>
        /// <remarks>Needs the enrolment id in the URL. Expects JSON: finishTime (e.g. "00:52:30"), positionOverall, positionCategory. Returns 201 with the result id, 400 if a result already exists or the enrolment was cancelled, 403 if not the event's organiser, or 404 if the enrolment doesn't exist.</remarks>
        [ProducesResponseType(201), ProducesResponseType(400), ProducesResponseType(403), ProducesResponseType(404)]
        [HttpPost("enrolments/{enrolmentId}/result")]
        [Authorize(Roles = "organiser")]
        public async Task<IActionResult> CaptureResult(int enrolmentId, ResultDto dto)
        {
            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

            if (enrolment == null) return NotFound("Enrolment not found.");
            if (enrolment.Category.Event.OrganiserID != CurrentUserId)
                return StatusCode(403, "You can only capture results for your own events.");
            if (enrolment.Status == "cancelled")
                return BadRequest("This enrolment was cancelled.");
            if (await _context.Results.AnyAsync(r => r.EnrolmentID == enrolmentId))
                return BadRequest("A result has already been captured for this enrolment.");

            var result = new Result
            {
                EnrolmentID = enrolmentId,
                FinishTime = dto.FinishTime,
                PositionOverall = dto.PositionOverall,
                PositionCategory = dto.PositionCategory
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { result.ResultID, message = "Result captured." });
        }

        /// <summary>Corrects an existing result on the organiser's own event (organiser only).</summary>
        /// <remarks>Needs the result id in the URL and the same JSON body as capture. Returns 200 "Result updated.", 403 if not the event's organiser, or 404 if the result doesn't exist.</remarks>
        [ProducesResponseType(200), ProducesResponseType(403), ProducesResponseType(404)]
        [HttpPut("results/{id}")]
        [Authorize(Roles = "organiser")]
        public async Task<IActionResult> UpdateResult(int id, ResultDto dto)
        {
            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Category)
                        .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null) return NotFound("Result not found.");
            if (result.Enrolment.Category.Event.OrganiserID != CurrentUserId)
                return StatusCode(403, "You can only change results for your own events.");

            result.FinishTime = dto.FinishTime;
            result.PositionOverall = dto.PositionOverall;
            result.PositionCategory = dto.PositionCategory;
            result.RecordedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Result updated." });
        }

        /// <summary>Lists all results for one of the organiser's own events, ordered by overall position (organiser only).</summary>
        /// <remarks>Needs the event id in the URL. Returns 200 with participant name, category, finish time and positions, 403 if not the event's organiser, or 404 if the event doesn't exist.</remarks>
        [ProducesResponseType(200), ProducesResponseType(403), ProducesResponseType(404)]
        [HttpGet("events/{eventId}/results")]
        [Authorize(Roles = "organiser")]
        public async Task<IActionResult> EventResults(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound("Event not found.");
            if (ev.OrganiserID != CurrentUserId)
                return StatusCode(403, "You can only view results for your own events.");

            var list = await _context.Results
                .Where(r => r.Enrolment.Category.EventID == eventId)
                .OrderBy(r => r.PositionOverall)
                .Select(r => new
                {
                    r.ResultID,
                    r.EnrolmentID,
                    ParticipantName = r.Enrolment.User.FullName,
                    CategoryName = r.Enrolment.Category.CategoryName,
                    r.FinishTime,
                    r.PositionOverall,
                    r.PositionCategory
                })
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>Lists the logged-in participant's own results (participant only).</summary>
        /// <remarks>No body needed. Returns 200 with event, category, finish time and positions, or 403 if not a participant.</remarks>
        [ProducesResponseType(200), ProducesResponseType(403)]
        [HttpGet("results/my")]
        [Authorize(Roles = "participant")]
        public async Task<IActionResult> MyResults()
        {
            var list = await _context.Results
                .Where(r => r.Enrolment.UserID == CurrentUserId)
                .Select(r => new
                {
                    r.ResultID,
                    EventName = r.Enrolment.Category.Event.EventName,
                    EventDate = r.Enrolment.Category.Event.EventDate,
                    CategoryName = r.Enrolment.Category.CategoryName,
                    r.FinishTime,
                    r.PositionOverall,
                    r.PositionCategory
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}