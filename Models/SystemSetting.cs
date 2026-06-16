namespace NamcyaTabulation.Models
{
    public class SystemSetting
    {
        public int Id { get; set; }
        public bool AllowTies { get; set; }
        
        // Option 1: Hide/Show Leaderboard
        public bool PublishLeaderboard { get; set; } = true;
        
        // Option 2: Lock Global Scoring
        public bool LockScoring { get; set; } = false;
                public string RankingSystem { get; set; } = "Standard";

        
        // Option 3: Decimal Precision Tie-Breaker
        public int DecimalPrecision { get; set; } = 2;
    }
}