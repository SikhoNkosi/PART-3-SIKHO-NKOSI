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
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EventsController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Lists events so participants can browse them, with optional filters.
        /// </summary>
        /// <remarks>
        /// Access: public, no login needed.
        /// Optional filters in the URL: search (matches the event name or location), eventType ("Run", "Walk" or "Cycle"), province, and upcomingOnly (true hides events that have already happened).
        /// With no filters, every event is returned. Results are sorted by date.
        /// </remarks>
        /// <response code="200">Returns the matching events.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? eventType,
            [FromQuery] string? province,
            [FromQuery] bool upcomingOnly = false)
        {
            var query = _context.Events.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(e => e.EventName.Contains(term) || e.Location.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(eventType))
                query = query.Where(e => e.EventType == eventType);

            if (!string.IsNullOrWhiteSpace(province))
                query = query.Where(e => e.Province == province);

            if (upcomingOnly)
            {
                var now = DateTime.UtcNow;
                query = query.Where(e => e.EventDate >= now);
            }

            var events = await query.OrderBy(e => e.EventDate).ToListAsync();
            return Ok(events.Select(MapToDto));
        }

        /// <summary>
        /// Lists the events created by the logged-in Organiser, with category and enrolment counts for their dashboard.
        /// </summary>
        /// <remarks>
        /// Access: Organiser only. Input: none. Only the Organiser's own events are returned, sorted by date.
        /// </remarks>
        /// <response code="200">Returns the Organiser's events with categoryCount and totalEnrolments.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in, but not as an Organiser.</response>
        [HttpGet("mine")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMine()
        {
            var organiserId = CurrentUserId;

            var mine = await _context.Events
                .Where(e => e.OrganiserId == organiserId)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.Province,
                    e.Distance,
                    e.EventType,
                    e.BannerImageUrl,
                    CategoryCount = e.Categories.Count(),
                    TotalEnrolments = e.Categories.SelectMany(c => c.Enrolments).Count()
                })
                .ToListAsync();

            return Ok(mine);
        }

        /// <summary>
        /// Returns the details of one event.
        /// </summary>
        /// <remarks>
        /// Access: public, no login needed. Input: the event id in the URL.
        /// </remarks>
        /// <response code="200">Returns the event.</response>
        /// <response code="404">No event exists with that id.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            return Ok(MapToDto(ev));
        }

        /// <summary>
        /// Creates a new event owned by the logged-in Organiser.
        /// </summary>
        /// <remarks>
        /// Access: Organiser only.
        /// Input: eventName, description, eventDate, location, province, distance, eventType ("Run", "Walk" or "Cycle") and routeMapUrl.
        /// </remarks>
        /// <response code="201">Event created. Returns the new event.</response>
        /// <response code="400">The input is invalid.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in, but not as an Organiser.</response>
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create(EventCreateDto dto)
        {
            var ev = new Event
            {
                OrganiserId = CurrentUserId,
                EventName = dto.EventName,
                Description = dto.Description,
                EventDate = dto.EventDate,
                Location = dto.Location,
                Province = dto.Province,
                Distance = dto.Distance,
                EventType = dto.EventType,
                RouteMapUrl = dto.RouteMapUrl
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, MapToDto(ev));
        }

        /// <summary>
        /// Updates an existing event.
        /// </summary>
        /// <remarks>
        /// Access: the Organiser who created this event only.
        /// Input: the event id in the URL and the same fields as when creating an event.
        /// </remarks>
        /// <response code="200">Event updated. Returns the updated event.</response>
        /// <response code="400">The input is invalid.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or this event belongs to a different Organiser.</response>
        /// <response code="404">No event exists with that id.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, EventCreateDto dto)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != CurrentUserId) return Forbid();

            ev.EventName = dto.EventName;
            ev.Description = dto.Description;
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location;
            ev.Province = dto.Province;
            ev.Distance = dto.Distance;
            ev.EventType = dto.EventType;
            ev.RouteMapUrl = dto.RouteMapUrl;

            await _context.SaveChangesAsync();

            return Ok(MapToDto(ev));
        }

        /// <summary>
        /// Deletes an event.
        /// </summary>
        /// <remarks>
        /// Access: the Organiser who created this event only. Input: the event id in the URL.
        /// </remarks>
        /// <response code="204">Event deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or this event belongs to a different Organiser.</response>
        /// <response code="404">No event exists with that id.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != CurrentUserId) return Forbid();

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static EventResponseDto MapToDto(Event ev) => new EventResponseDto
        {
            EventId = ev.EventId,
            OrganiserId = ev.OrganiserId,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Province = ev.Province,
            Distance = ev.Distance,
            EventType = ev.EventType,
            RouteMapUrl = ev.RouteMapUrl,
            BannerImageUrl = ev.BannerImageUrl
        };
    }
}