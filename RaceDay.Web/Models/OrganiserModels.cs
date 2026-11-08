using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Web.Models
{
    // ---------- What the API sends back ----------

    public class OrganiserEventDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Province { get; set; }
        public decimal Distance { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public int CategoryCount { get; set; }
        public int TotalEnrolments { get; set; }
    }

    public class EventDto
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

    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal? DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public int MaxParticipants { get; set; }
    }

    public class EventEnrolmentDto
    {
        public int EnrolmentId { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public int? ResultId { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
    }

    // ---------- Page models ----------

    public class DashboardViewModel
    {
        public List<OrganiserEventDto> Events { get; set; } = new();
        public List<OrganiserEventDto> UpcomingEvents { get; set; } = new();
        public int TotalEnrolments { get; set; }
    }

    // Used for both creating and editing an event (EventId is 0 when creating)
    public class EventFormViewModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Please enter the event name.")]
        [StringLength(150, ErrorMessage = "The event name must be 150 characters or fewer.")]
        [Display(Name = "Event name")]
        public string EventName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please choose the date and time of the event.")]
        [Display(Name = "Date and time")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Please enter the location.")]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a province.")]
        [Display(Name = "Province")]
        public string? Province { get; set; }

        [Range(0.1, 1000, ErrorMessage = "Please enter a distance between 0.1 and 1000 km.")]
        [Display(Name = "Distance (km)")]
        public decimal Distance { get; set; }

        [Required(ErrorMessage = "Please choose the event type.")]
        [RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "Please choose Run, Walk or Cycle.")]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = "Run";

        [Url(ErrorMessage = "Please enter a full web address, starting with https://")]
        [Display(Name = "Route map link (optional)")]
        public string? RouteMapUrl { get; set; }

        // The banner that is already saved (shown on the page, not posted back)
        public string? BannerImageUrl { get; set; }

        // A new banner picked by the Organiser
        [Display(Name = "Banner image (optional)")]
        public IFormFile? Banner { get; set; }
    }

    // Used for both adding and editing a category (CategoryId is 0 when adding)
    public class CategoryFormViewModel
    {
        public int EventId { get; set; }
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Please enter a category name, for example 10km Run or Under 20.")]
        [StringLength(100, ErrorMessage = "The category name must be 100 characters or fewer.")]
        [Display(Name = "Category name")]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0.1, 1000, ErrorMessage = "Please enter a distance between 0.1 and 1000 km, or leave it empty.")]
        [Display(Name = "Distance in km (optional)")]
        public decimal? DistanceKm { get; set; }

        [Range(0, 100000, ErrorMessage = "Please enter an entry fee of 0 or more.")]
        [Display(Name = "Entry fee (R)")]
        public decimal EntryFee { get; set; }

        [Range(1, 100000, ErrorMessage = "Please allow at least 1 participant.")]
        [Display(Name = "Maximum participants")]
        public int MaxParticipants { get; set; } = 100;
    }

    public class CategoriesPageViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public List<CategoryDto> Categories { get; set; } = new();
        public Dictionary<int, int> EnrolmentCounts { get; set; } = new();
        public CategoryFormViewModel NewCategory { get; set; } = new();
    }

    // Used by both the enrolments page and the results page
    public class EventEnrolmentsViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public List<EventEnrolmentDto> Enrolments { get; set; } = new();
    }

    public class SaveResultViewModel
    {
        public int EventId { get; set; }
        public int EnrolmentId { get; set; }
        public int? ResultId { get; set; }

        [Required(ErrorMessage = "Please enter the finish time.")]
        public string FinishTime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the finishing position.")]
        [Range(1, 100000, ErrorMessage = "The position must be 1 or more.")]
        public int? Position { get; set; }
    }
}
