using RaceDay.Api.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    public class Event
    {
        [Column(TypeName = "decimal(5,2)")]
        public decimal DistanceKM { get; set; }
        [Key]
        public int EventID { get; set; }

        [Required]
        public int OrganiserID { get; set; }
        [ForeignKey("OrganiserID")]
        public User Organiser { get; set; }

        [Required, MaxLength(150)]
        public string EventName { get; set; }

        [Required, MaxLength(20)]
        public string EventType { get; set; } // "run", "walk", "cycle"

        [Required]
        public DateTime EventDate { get; set; }

        [Required, MaxLength(150)]
        public string Location { get; set; }

        public string Description { get; set; }

        public ICollection<Category> Categories { get; set; }
    }
}