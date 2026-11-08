using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IImageStorageService _storage;

        public ImagesController(RaceDayContext context, IImageStorageService storage)
        {
            _context = context;
            _storage = storage;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Uploads a banner image for an event. The image is stored in Azure Blob Storage and its web address is saved on the event.
        /// </summary>
        /// <remarks>
        /// Access: the Organiser who owns the event only.
        /// Input: the event id in the URL and an image file (JPG, PNG or WEBP, maximum 5 MB) sent as form data in a field called "file".
        /// </remarks>
        /// <response code="200">Banner uploaded. Returns the image's web address in bannerImageUrl.</response>
        /// <response code="400">No file was sent, the file is too large, or it is not a JPG, PNG or WEBP image.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or the event belongs to a different Organiser.</response>
        /// <response code="404">No event exists with that id.</response>
        [HttpPost("api/events/{eventId}/banner")]
        [Authorize(Roles = "Organiser")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadBanner(int eventId, IFormFile file)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != CurrentUserId) return Forbid();

            var error = ImageUploadRules.Validate(file);
            if (error != null) return BadRequest(new { message = error });

            await using var stream = file.OpenReadStream();
            var url = await _storage.UploadImageAsync(stream, file.FileName, file.ContentType, "event-banners");

            ev.BannerImageUrl = url;
            await _context.SaveChangesAsync();

            return Ok(new { bannerImageUrl = url });
        }

        /// <summary>
        /// Uploads a profile picture for the logged-in user. The image is stored in Azure Blob Storage and its web address is saved on their profile.
        /// </summary>
        /// <remarks>
        /// Access: any logged-in user (Organiser or Participant).
        /// Input: an image file (JPG, PNG or WEBP, maximum 5 MB) sent as form data in a field called "file".
        /// </remarks>
        /// <response code="200">Picture uploaded. Returns the image's web address in profilePictureUrl.</response>
        /// <response code="400">No file was sent, the file is too large, or it is not a JPG, PNG or WEBP image.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="404">The user no longer exists.</response>
        [HttpPost("api/users/me/profile-picture")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var error = ImageUploadRules.Validate(file);
            if (error != null) return BadRequest(new { message = error });

            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            await using var stream = file.OpenReadStream();
            var url = await _storage.UploadImageAsync(stream, file.FileName, file.ContentType, "profile-pictures");

            user.ProfilePictureUrl = url;
            await _context.SaveChangesAsync();

            return Ok(new { profilePictureUrl = url });
        }
    }
}
