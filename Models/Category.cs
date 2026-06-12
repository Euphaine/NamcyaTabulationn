using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NamcyaTabulation.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public int SubEventId { get; set; }
        public SubEvent? SubEvent { get; set; }

        public ICollection<Contestant> Contestants { get; set; } = new List<Contestant>();
        public ICollection<Criterion> Criteria { get; set; } = new List<Criterion>();
    }
}