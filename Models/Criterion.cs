using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NamcyaTabulation.Models
{
    public class Criterion
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Column(TypeName = "decimal(5,2)")]
        public decimal MaxScore { get; set; }
          public int SubEventId { get; set; }
        public SubEvent? SubEvent { get; set; }
    }
}