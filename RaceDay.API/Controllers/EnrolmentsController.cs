using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Authorize]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Enters the logged-in Participant into a category of an event.
        /// </summary>
        /// <remarks>
        /// Access: Participant only. Input: the category id in the URL.
        /// The system records the link between the participant and the selected category (and so the event).
        /// </remarks>
        /// <response code="201">Enrolment created. Returns the enrolment id, category id, status and date.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in, but not as a Participant.</response>
        /// <response code="404">No category exists with that id.</response>
        /// <response code="409">The participant is already enrolled in this category.</response>
        [HttpPost("api/categories/{categoryId}/enrol")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enrol(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null) return NotFound();

            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e => e.ParticipantId == CurrentUserId && e.CategoryId == categoryId);

            if (alreadyEnrolled)
                return Conflict(new { message = "You are already enrolled in this category." });

            var enrolment = new Enrolment
            {
                ParticipantId = CurrentUserId,
                CategoryId = categoryId,
                Status = "Confirmed"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                enrolment.EnrolmentId,
                enrolment.CategoryId,
                enrolment.Status,
                enrolment.EnrolmentDate
            });
        }

        /// <summary>
        /// Lists all enrolments of the logged-in Participant, including the event they are for.
        /// </summary>
        /// <remarks>
        /// Access: Participant only. Input: none. Only the participant's own enrolments are returned.
        /// Each enrolment includes the event id, name, date, location, type and banner image, plus the category and status.
        /// </remarks>
        /// <response code="200">Returns the participant's enrolments.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in, but not as a Participant.</response>
        [HttpGet("api/enrolments/me")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMine()
        {
            var userId = CurrentUserId;

            var mine = await _context.Enrolments
                .Where(e => e.ParticipantId == userId)
                .OrderBy(e => e.Category!.Event!.EventDate)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.CategoryId,
                    CategoryName = e.Category!.CategoryName,
                    EventId = e.Category!.EventId,
                    EventName = e.Category!.Event!.EventName,
                    EventDate = e.Category!.Event!.EventDate,
                    Location = e.Category!.Event!.Location,
                    EventType = e.Category!.Event!.EventType,
                    BannerImageUrl = e.Category!.Event!.BannerImageUrl,
                    e.Status,
                    e.EnrolmentDate
                })
                .ToListAsync();

            return Ok(mine);
        }

        /// <summary>
        /// Lists everyone enrolled in any category of an event, including any result already captured.
        /// </summary>
        /// <remarks>
        /// Access: the Organiser who owns the event only. Input: the event id in the URL.
        /// Each enrolment includes the participant, category and status, plus resultId, finishTime and position (empty until a result is captured).
        /// </remarks>
        /// <response code="200">Returns all enrolments for the event.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or the event belongs to a different Organiser.</response>
        /// <response code="404">No event exists with that id.</response>
        [HttpGet("api/events/{eventId}/enrolments")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetForEvent(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != CurrentUserId) return Forbid();

            var enrolments = await _context.Enrolments
                .Where(e => e.Category!.EventId == eventId)
                .OrderBy(e => e.Participant!.FullName)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.ParticipantId,
                    ParticipantName = e.Participant!.FullName,
                    e.CategoryId,
                    CategoryName = e.Category!.CategoryName,
                    e.Status,
                    e.EnrolmentDate,
                    ResultId = e.Result != null ? (int?)e.Result.ResultId : null,
                    FinishTime = e.Result != null ? e.Result.FinishTime : null,
                    Position = e.Result != null ? e.Result.Position : null
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        /// <summary>
        /// Cancels an enrolment.
        /// </summary>
        /// <remarks>
        /// Access: the Participant who made the enrolment only. Input: the enrolment id in the URL.
        /// </remarks>
        /// <response code="204">Enrolment cancelled.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not a Participant, or the enrolment belongs to a different participant.</response>
        /// <response code="404">No enrolment exists with that id.</response>
        [HttpDelete("api/enrolments/{id}")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id)
        {
            var enrolment = await _context.Enrolments.FindAsync(id);
            if (enrolment == null) return NotFound();
            if (enrolment.ParticipantId != CurrentUserId) return Forbid();

            _context.Enrolments.Remove(enrolment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
