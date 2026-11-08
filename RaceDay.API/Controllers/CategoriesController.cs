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
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Lists the categories (for example 10km or Under 20) available for an event.
        /// </summary>
        /// <remarks>
        /// Access: public, no login needed. Input: the event id in the URL.
        /// </remarks>
        /// <response code="200">Returns the event's categories.</response>
        /// <response code="404">No event exists with that id.</response>
        [HttpGet("api/events/{eventId}/categories")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetForEvent(int eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists) return NotFound();

            var categories = await _context.Categories
                .Where(c => c.EventId == eventId)
                .ToListAsync();

            return Ok(categories.Select(MapToDto));
        }

        /// <summary>
        /// Adds a new category to an event.
        /// </summary>
        /// <remarks>
        /// Access: the Organiser who owns the event only.
        /// Input: the event id in the URL, plus categoryName, distanceKm (optional), entryFee and maxParticipants.
        /// </remarks>
        /// <response code="201">Category created. Returns the new category.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or the event belongs to a different Organiser.</response>
        /// <response code="404">No event exists with that id.</response>
        [HttpPost("api/events/{eventId}/categories")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create(int eventId, CategoryCreateDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != CurrentUserId) return Forbid();

            var category = new Category
            {
                EventId = eventId,
                CategoryName = dto.CategoryName,
                DistanceKm = dto.DistanceKm,
                EntryFee = dto.EntryFee,
                MaxParticipants = dto.MaxParticipants
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return StatusCode(201, MapToDto(category));
        }

        /// <summary>
        /// Updates an existing category.
        /// </summary>
        /// <remarks>
        /// Access: the Organiser who owns the parent event only.
        /// Input: the category id in the URL and the same fields as when creating a category.
        /// </remarks>
        /// <response code="200">Category updated. Returns the updated category.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or the event belongs to a different Organiser.</response>
        /// <response code="404">No category exists with that id.</response>
        [HttpPut("api/categories/{id}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, CategoryCreateDto dto)
        {
            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound();
            if (category.Event!.OrganiserId != CurrentUserId) return Forbid();

            category.CategoryName = dto.CategoryName;
            category.DistanceKm = dto.DistanceKm;
            category.EntryFee = dto.EntryFee;
            category.MaxParticipants = dto.MaxParticipants;

            await _context.SaveChangesAsync();

            return Ok(MapToDto(category));
        }

        /// <summary>
        /// Deletes a category from an event.
        /// </summary>
        /// <remarks>
        /// Access: the Organiser who owns the parent event only. Input: the category id in the URL.
        /// </remarks>
        /// <response code="204">Category deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or the event belongs to a different Organiser.</response>
        /// <response code="404">No category exists with that id.</response>
        [HttpDelete("api/categories/{id}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound();
            if (category.Event!.OrganiserId != CurrentUserId) return Forbid();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static CategoryResponseDto MapToDto(Category c) => new CategoryResponseDto
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            CategoryName = c.CategoryName,
            DistanceKm = c.DistanceKm,
            EntryFee = c.EntryFee,
            MaxParticipants = c.MaxParticipants
        };
    }
}