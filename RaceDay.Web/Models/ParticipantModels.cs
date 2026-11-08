namespace RaceDay.Web.Models
{
    // ---------- What the API sends back ----------

    public class MyEnrolmentDto
    {
        public int EnrolmentId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
    }

    public class MyResultDto
    {
        public int ResultId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public int TotalFinishers { get; set; }
    }

    public class PublicResultDto
    {
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
    }

    // ---------- Page models ----------

    public class HomeViewModel
    {
        public List<EventDto> Events { get; set; } = new();
        public string? Search { get; set; }
        public string? EventType { get; set; }
        public string? Province { get; set; }
    }

    public class EventDetailsViewModel
    {
        public EventDto Event { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();
        public List<PublicResultDto> Results { get; set; } = new();

        // The categories this Participant has already entered
        public HashSet<int> EnrolledCategoryIds { get; set; } = new();

        public bool IsLoggedIn { get; set; }
        public string? Role { get; set; }
    }

    // Small helpers for showing things nicely
    public static class DisplayHelpers
    {
        // 1 becomes 1st, 2 becomes 2nd, 12 becomes 12th, 47 becomes 47th
        public static string Ordinal(int number)
        {
            var lastTwoDigits = number % 100;
            if (lastTwoDigits >= 11 && lastTwoDigits <= 13) return number + "th";

            return (number % 10) switch
            {
                1 => number + "st",
                2 => number + "nd",
                3 => number + "rd",
                _ => number + "th"
            };
        }

        public static string Time(TimeSpan? time)
        {
            return time.HasValue ? time.Value.ToString(@"h\:mm\:ss") : "-";
        }
    }
}
