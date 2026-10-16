using System.ComponentModel.DataAnnotations;

namespace RaceDay.api.DTOs
{
    public class ResultDto
    {
        public TimeSpan FinishTime { get; set; }

        [Range(1, 100000)]
        public int PositionOverall { get; set; }

        [Range(1, 100000)]
        public int PositionCategory { get; set; }
    }
}