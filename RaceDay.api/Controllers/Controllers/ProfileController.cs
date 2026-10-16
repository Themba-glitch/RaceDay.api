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
    public class ProfileController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ProfileController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        /// <summary>Returns the logged-in user's own details and profile (organiser or participant).</summary>
        /// <remarks>No body needed; the user comes from the session cookie. Returns 200 with name, email, role and profile (null until saved), or 401 if not logged in.</remarks>
        [ProducesResponseType(200), ProducesResponseType(401)]
        // Both roles: view own profile
        [HttpGet]
        public async Task<IActionResult> GetMyProfile()
        {
            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.UserID == CurrentUserId);

            if (user == null) return NotFound();

            return Ok(new
            {
                user.UserID,
                user.FullName,
                user.Email,
                user.Role,
                Profile = user.Profile == null ? null : new
                {
                    user.Profile.PhoneNumber,
                    user.Profile.DateOfBirth,
                    user.Profile.Gender,
                    user.Profile.EmergencyContact,
                    user.Profile.Province
                }
            });
        }
        /// <summary>Creates or updates the logged-in user's own profile (organiser or participant).</summary>
        /// <remarks>Expects JSON: phoneNumber, dateOfBirth, gender, emergencyContact, province. Returns 200 "Profile updated." or 401 if not logged in.</remarks>
        [ProducesResponseType(200), ProducesResponseType(401)]

        // Both roles: update own profile (creates it on first save)
        [HttpPut]
        public async Task<IActionResult> UpdateMyProfile(ProfileDto dto)
        {
            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserID == CurrentUserId);

            if (profile == null)
            {
                profile = new Profile { UserID = CurrentUserId };
                _context.Profiles.Add(profile);
            }

            profile.PhoneNumber = dto.PhoneNumber ?? "";
            profile.DateOfBirth = dto.DateOfBirth;
            profile.Gender = dto.Gender ?? "";
            profile.EmergencyContact = dto.EmergencyContact ?? "";
            profile.Province = dto.Province ?? "";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated." });
        }
    }
}