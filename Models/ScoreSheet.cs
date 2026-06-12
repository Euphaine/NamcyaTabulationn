using System.ComponentModel.DataAnnotations.Schema;

namespace NamcyaTabulation.Models
{
    public class ScoreSheet
    {
        public int Id { get; set; }
        public int JudgeId { get; set; }
        public Judge? Judge { get; set; }
        public int ContestantId { get; set; }
        public Contestant? Contestant { get; set; }
        public int CriterionId { get; set; }
        public Criterion? Criterion { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal Points { get; set; }
    }
}