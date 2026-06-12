using Microsoft.EntityFrameworkCore;
using NamcyaTabulation.Models;

namespace NamcyaTabulation.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Event> Events { get; set; }
        public DbSet<SubEvent> SubEvents { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Contestant> Contestants { get; set; }
        public DbSet<Judge> Judges { get; set; }
        public DbSet<Criterion> Criteria { get; set; }
        public DbSet<ScoreSheet> ScoreSheets { get; set; }
        public DbSet<Organizer> Organizers { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
    }
}