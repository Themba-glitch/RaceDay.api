using RaceDay.Api.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        public int EventID { get; set; }
        [ForeignKey("EventID")]
        public Event Event { get; set; }

        [Required, MaxLength(50)]
        public string CategoryName { get; set; }

        [Required, Column(TypeName = "decimal(5,2)")]
        public decimal DistanceKM { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal EntryFee { get; set; } = 0;

        public int? MaxParticipants { get; set; }

        public ICollection<Enrolment> Enrolments { get; set; }
    }
}