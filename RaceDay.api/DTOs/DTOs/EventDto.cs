using System.ComponentModel.DataAnnotations;

namespace RaceDay.api.DTOs
{
    public class EventDto
    {
        [Required, MaxLength(150)]
        public string EventName { get; set; } = "";

        [Required]
        public string EventType { get; set; } = ""; // run, walk or cycle

        [Required]
        public DateTime EventDate { get; set; }

        [Required, MaxLength(150)]
        public string Location { get; set; } = "";

        public string? Description { get; set; }

        [Range(0.01, 999)]
        public decimal DistanceKM { get; set; }
    }
}