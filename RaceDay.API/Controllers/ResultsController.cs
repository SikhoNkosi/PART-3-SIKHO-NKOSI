using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Captures the finish time and position for a participant's enrolment.
        /// </summary>
        /// <remarks>
        /// Access: the Organiser who owns the event only.
        /// Input: the enrolment id in the URL, plus finishTime and position.
        /// </remarks>
        /// <response code="201">Result captured. Returns the new result.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or the event belongs to a different Organiser.</response>
        /// <response code="404">No enrolment exists with that id.</response>
        /// <response code="409">A result has already been captured for this enrolment.</response>
        [HttpPost("api/enrolments/{enrolmentId}/result")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Capture(int enrolmentId, ResultCreateDto dto)
        {
            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                .ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

            if (enrolment == null) return NotFound();
            if (enrolment.Category!.Event!.OrganiserId != CurrentUserId) return Forbid();

            var alreadyCaptured = await _context.Results.AnyAsync(r => r.EnrolmentId == enrolmentId);
            if (alreadyCaptured)
                return Conflict(new { message = "A result has already been captured for this enrolment. Use PUT /api/results/{id} to change it." });

            var result = new Result
            {
                EnrolmentId = enrolmentId,
                CapturedByOrganiserId = CurrentUserId,
                FinishTime = dto.FinishTime,
                Position = dto.Position
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                result.ResultId,
                result.EnrolmentId,
                result.FinishTime,
                result.Position
            });
        }

        /// <summary>
        /// Updates a result that was captured earlier.
        /// </summary>
        /// <remarks>
        /// Access: the Organiser who owns the event only.
        /// Input: the result id in the URL, plus finishTime and position.
        /// </remarks>
        /// <response code="200">Result updated. Returns the updated result.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or the event belongs to a different Organiser.</response>
        /// <response code="404">No result exists with that id.</response>
        [HttpPut("api/results/{id}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, ResultCreateDto dto)
        {
            var result = await _context.Results
                .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Category)
                .ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null) return NotFound();
            if (result.Enrolment!.Category!.Event!.OrganiserId != CurrentUserId) return Forbid();

            result.FinishTime = dto.FinishTime;
            result.Position = dto.Position;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                result.ResultId,
                result.EnrolmentId,
                result.FinishTime,
                result.Position
            });
        }

        /// <summary>
        /// Lists the logged-in Participant's own race history.
        /// </summary>
        /// <remarks>
        /// Access: Participant only. Input: none. Only the participant's own results are returned, newest event first.
        /// Each result includes the event name and date, the category, the finish time and position,
        /// and totalFinishers: how many finishers have a time recorded in the same category (for "47th out of 312").
        /// </remarks>
        /// <response code="200">Returns the participant's results.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in, but not as a Participant.</response>
        [HttpGet("api/results/me")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMine()
        {
            var userId = CurrentUserId;

            var results = await _context.Results
                .Where(r => r.Enrolment!.ParticipantId == userId)
                .OrderByDescending(r => r.Enrolment!.Category!.Event!.EventDate)
                .Select(r => new
                {
                    r.ResultId,
                    EventId = r.Enrolment!.Category!.EventId,
                    EventName = r.Enrolment!.Category!.Event!.EventName,
                    EventDate = r.Enrolment!.Category!.Event!.EventDate,
                    CategoryName = r.Enrolment!.Category!.CategoryName,
                    r.FinishTime,
                    r.Position,
                    TotalFinishers = _context.Results.Count(x =>
                        x.Enrolment!.CategoryId == r.Enrolment!.CategoryId && x.FinishTime != null)
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// Lists all results for an event.
        /// </summary>
        /// <remarks>
        /// Access: public, no login needed (race results are public). Input: the event id in the URL.
        /// </remarks>
        /// <response code="200">Returns the results for the event.</response>
        [HttpGet("api/events/{eventId}/results")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetForEvent(int eventId)
        {
            var results = await _context.Results
                .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Category)
                .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Participant)
                .Where(r => r.Enrolment!.Category!.EventId == eventId)
                .Select(r => new
                {
                    ParticipantName = r.Enrolment!.Participant!.FullName,
                    CategoryName = r.Enrolment!.Category!.CategoryName,
                    r.FinishTime,
                    r.Position
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}