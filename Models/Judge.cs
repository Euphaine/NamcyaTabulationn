using System.Collections.Generic;

namespace NamcyaTabulation.Models
{
    public class Judge
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        
        public ICollection<SubEvent> SubEvents { get; set; } = new List<SubEvent>();
    }
}