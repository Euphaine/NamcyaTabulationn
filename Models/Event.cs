using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NamcyaTabulation.Models
{
    public class Event
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public ICollection<SubEvent> SubEvents { get; set; } = new List<SubEvent>();
    }
}