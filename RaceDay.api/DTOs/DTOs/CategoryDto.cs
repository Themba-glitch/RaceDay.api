using System.ComponentModel.DataAnnotations;

namespace RaceDay.api.DTOs
{
    public class CategoryDto
    {
        [Required, MaxLength(50)]
        public string CategoryName { get; set; } = "";

        [Range(0.01, 999)]
        public decimal DistanceKM { get; set; }

        [Range(0, 100000)]
        public decimal EntryFee { get; set; }

        public int? MaxParticipants { get; set; }
    }
}