using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public AuthController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registers a new account as either an Organiser or a Participant.
        /// </summary>
        /// <remarks>
        /// Access: public, no login needed.
        /// Input: fullName, email, password (minimum 6 characters), role ("Organiser" or "Participant") and an optional phoneNumber.
        /// The password is hashed before it is stored.
        /// </remarks>
        /// <response code="201">Account created. Returns the new user's id, name and role.</response>
        /// <response code="400">The role is not "Organiser" or "Participant", or the input is invalid.</response>
        /// <response code="409">That email address is already registered.</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (dto.Role != "Organiser" && dto.Role != "Participant")
                return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict(new { message = "Email already registered." });

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                PhoneNumber = dto.PhoneNumber
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { userId = user.UserId, user.FullName, user.Role });
        }

        /// <summary>
        /// Logs a user in and starts a server-side session that stores their user ID and role.
        /// </summary>
        /// <remarks>
        /// Access: public, no login needed.
        /// Input: email and password.
        /// After a successful login the server remembers the user for all following requests.
        /// </remarks>
        /// <response code="200">Login successful. Returns the user's id, name and role.</response>
        /// <response code="401">The email or password is wrong.</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Role", user.Role);

            return Ok(new { userId = user.UserId, user.FullName, user.Role });
        }

        /// <summary>
        /// Logs the current user out by clearing their server-side session.
        /// </summary>
        /// <remarks>
        /// Access: public. Input: none.
        /// </remarks>
        /// <response code="204">Session cleared. The user is now logged out.</response>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return NoContent();
        }
    }
}
