using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    // ---------- Auth ----------
    public class RegisterDto
    {
        [Required] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
        [Required] public string Role { get; set; } = string.Empty; // "Organiser" or "Participant"
        public string? PhoneNumber { get; set; }
    }

    public class LoginDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    // ---------- User Profile ----------
    public class ProfileDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    public class UpdateProfileDto
    {
        [Required] public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    // ---------- Events ----------
    public class EventCreateDto
    {
        [Required] public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required] public DateTime EventDate { get; set; }
        [Required] public string Location { get; set; } = string.Empty;
        public string? Province { get; set; }
        public decimal Distance { get; set; }
        [Required] public string EventType { get; set; } = string.Empty; // Run, Walk, Cycle
        public string? RouteMapUrl { get; set; }
    }

    public class EventResponseDto
    {
        public int EventId { get; set; }
        public int OrganiserId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Province { get; set; }
        public decimal Distance { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? RouteMapUrl { get; set; }
        public string? BannerImageUrl { get; set; }
    }

    // ---------- Categories ----------
    public class CategoryCreateDto
    {
        [Required] public string CategoryName { get; set; } = string.Empty;
        public decimal? DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public int MaxParticipants { get; set; } = 100;
    }

    public class CategoryResponseDto
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal? DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public int MaxParticipants { get; set; }
    }

    // ---------- Results ----------
    public class ResultCreateDto
    {
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
    }
}
