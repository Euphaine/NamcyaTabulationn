using System.ComponentModel.DataAnnotations;

namespace NamcyaTabulation.Models
{
    public class JudgeComment
    {
        public int Id { get; set; }
        public int JudgeId { get; set; }
        public Judge? Judge { get; set; }
        public int ContestantId { get; set; }
        public Contestant? Contestant { get; set; }
        [MaxLength(1000)]
        public string Comment { get; set; } = string.Empty;
    }
}