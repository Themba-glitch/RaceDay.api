using RaceDay.Api.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, MaxLength(150)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required, MaxLength(20)]
        public string Role { get; set; } // "organiser" or "participant"

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Profile Profile { get; set; }
        public ICollection<Event> OrganisedEvents { get; set; }
        public ICollection<Enrolment> Enrolments { get; set; }
    }
}