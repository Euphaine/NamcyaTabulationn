using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NamcyaTabulation.Models
{
    public class SubEvent
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public string Details { get; set; } = string.Empty;
        public int EventId { get; set; }
        public Event? Event { get; set; }
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Judge> Judges { get; set; } = new List<Judge>();
        public int? ChairmanJudgeId { get; set; }
    }
}