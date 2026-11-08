using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // "Organiser" or "Participant"
        [Required]
        public string Role { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        // Added in Part 3: link to the profile picture stored in Azure Blob Storage
        public string? ProfilePictureUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Event> Events { get; set; } = new List<Event>();
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    public class Event
    {
        public int EventId { get; set; }

        public int OrganiserId { get; set; }
        public User? Organiser { get; set; }

        [Required]
        public string EventName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime EventDate { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        public string? Province { get; set; }

        // Added for Part 2 functional requirements - not in the original Part 1 ERD.
        public decimal Distance { get; set; }

        // "Run", "Walk", or "Cycle" - added for Part 2 functional requirements.
        [Required]
        public string EventType { get; set; } = string.Empty;

        public string? RouteMapUrl { get; set; }

        // Added in Part 3: link to the banner image stored in Azure Blob Storage
        public string? BannerImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }

    public class Category
    {
        public int CategoryId { get; set; }

        public int EventId { get; set; }
        public Event? Event { get; set; }

        [Required]
        public string CategoryName { get; set; } = string.Empty;

        public decimal? DistanceKm { get; set; }

        public decimal EntryFee { get; set; }

        public int MaxParticipants { get; set; } = 100;

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    public class Enrolment
    {
        public int EnrolmentId { get; set; }

        public int ParticipantId { get; set; }
        public User? Participant { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Confirmed";

        public Result? Result { get; set; }
        public Payment? Payment { get; set; }
    }

    public class Result
    {
        public int ResultId { get; set; }

        public int EnrolmentId { get; set; }
        public Enrolment? Enrolment { get; set; }

        public int CapturedByOrganiserId { get; set; }
        public User? CapturedByOrganiser { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        public DateTime CapturedDate { get; set; } = DateTime.UtcNow;
    }

    public class Payment
    {
        public int PaymentId { get; set; }

        public int EnrolmentId { get; set; }
        public Enrolment? Enrolment { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = "Card";

        public string PaymentStatus { get; set; } = "Paid";

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    }
}