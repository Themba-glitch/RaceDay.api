using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    public class Result
    {
        [Key]
        public int ResultID { get; set; }

        [Required]
        public int EnrolmentID { get; set; }
        [ForeignKey("EnrolmentID")]
        public Enrolment Enrolment { get; set; }

        public TimeSpan? FinishTime { get; set; }
        public int? PositionOverall { get; set; }
        public int? PositionCategory { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }
}