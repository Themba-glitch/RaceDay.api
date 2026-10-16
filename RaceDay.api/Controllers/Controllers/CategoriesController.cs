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
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        /// <summary>Lists the categories defined for an event (organiser or participant).</summary>
        /// <remarks>Needs the event id in the URL. Returns 200 with each category's id, name, distance, entry fee and maximum participants, or 404 if the event doesn't exist.</remarks>
        [ProducesResponseType(200), ProducesResponseType(404)]
        // Both roles: list categories for an event
        [HttpGet("events/{eventId}/categories")]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            if (!await _context.Events.AnyAsync(e => e.EventID == eventId))
                return NotFound("Event not found.");

            var categories = await _context.Categories
                .Where(c => c.EventID == eventId)
                .Select(c => new
                {
                    c.CategoryID,
                    c.EventID,
                    c.CategoryName,
                    c.DistanceKM,
                    c.EntryFee,
                    c.MaxParticipants
                })
                .ToListAsync();

            return Ok(categories);
        }
        /// <summary>Adds a category (e.g. 10km, Under 20, Senior) to the organiser's own event (organiser only).</summary>
        /// <remarks>Needs the event id in the URL. Expects JSON: categoryName, distanceKM, entryFee, maxParticipants. Returns 201 with the category id, 403 if not the event's organiser, or 404 if the event doesn't exist.</remarks>
        [ProducesResponseType(201), ProducesResponseType(403), ProducesResponseType(404)]

        // Organiser only: add a category to their own event
        [HttpPost("events/{eventId}/categories")]
        [Authorize(Roles = "organiser")]
        public async Task<IActionResult> CreateCategory(int eventId, CategoryDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound("Event not found.");
            if (ev.OrganiserID != CurrentUserId)
                return StatusCode(403, "You can only add categories to your own events.");

            var category = new Category
            {
                EventID = eventId,
                CategoryName = dto.CategoryName,
                DistanceKM = dto.DistanceKM,
                EntryFee = dto.EntryFee,
                MaxParticipants = dto.MaxParticipants
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { category.CategoryID, message = "Category created." });
        }
        /// <summary>Updates a category on the organiser's own event (organiser only).</summary>
        /// <remarks>Needs the category id in the URL and the same JSON body as create. Returns 200 "Category updated.", 403 if not the event's organiser, or 404 if the category doesn't exist.</remarks>
        [ProducesResponseType(200), ProducesResponseType(403), ProducesResponseType(404)]

        // Organiser only: update a category on their own event
        [HttpPut("categories/{id}")]
        [Authorize(Roles = "organiser")]
        public async Task<IActionResult> UpdateCategory(int id, CategoryDto dto)
        {
            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null) return NotFound("Category not found.");
            if (category.Event.OrganiserID != CurrentUserId)
                return StatusCode(403, "You can only change categories on your own events.");

            category.CategoryName = dto.CategoryName;
            category.DistanceKM = dto.DistanceKM;
            category.EntryFee = dto.EntryFee;
            category.MaxParticipants = dto.MaxParticipants;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Category updated." });
        }
        /// <summary>Deletes a category from the organiser's own event (organiser only).</summary>
        /// <remarks>Needs the category id in the URL. Returns 200 "Category deleted.", 403 if not the event's organiser, or 404 if the category doesn't exist.</remarks>
        [ProducesResponseType(200), ProducesResponseType(403), ProducesResponseType(404)]
        // Organiser only: delete a category on their own event
        [HttpDelete("categories/{id}")]
        [Authorize(Roles = "organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null) return NotFound("Category not found.");
            if (category.Event.OrganiserID != CurrentUserId)
                return StatusCode(403, "You can only delete categories on your own events.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Category deleted." });
        }
    }
}
