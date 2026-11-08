using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;
using RaceDay.API.DTOs;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public UsersController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Returns the profile of the user who is currently logged in.
        /// </summary>
        /// <remarks>
        /// Access: any logged-in user (Organiser or Participant). Input: none.
        /// </remarks>
        /// <response code="200">Returns the user's id, name, email, role, phone number and profile picture address.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="404">The user no longer exists.</response>
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMe()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            return Ok(MapToDto(user));
        }

        /// <summary>
        /// Updates the full name and phone number of the user who is currently logged in.
        /// </summary>
        /// <remarks>
        /// Access: any logged-in user. Input: fullName and an optional phoneNumber.
        /// Email and role cannot be changed here. Use the profile picture upload for the picture.
        /// </remarks>
        /// <response code="200">Profile updated. Returns the updated profile.</response>
        /// <response code="400">The input is invalid.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="404">The user no longer exists.</response>
        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;

            await _context.SaveChangesAsync();

            return Ok(MapToDto(user));
        }

        private static ProfileDto MapToDto(Models.User user) => new ProfileDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
    }
}
