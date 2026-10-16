using RaceDay.Api.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        [Required]
        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public User User { get; set; }

        [Required]
        public int CategoryID { get; set; }
        [ForeignKey("CategoryID")]
        public Category Category { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.Now.Date;

        [Required, MaxLength(20)]
        public string Status { get; set; } = "pending";

        [Required, MaxLength(20)]
        public string PaymentStatus { get; set; } = "unpaid";

        public Result Result { get; set; }
    }
}