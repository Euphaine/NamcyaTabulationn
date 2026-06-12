using System;
using System.Linq;
using NamcyaTabulation.Models;

namespace NamcyaTabulation.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            if (!context.Judges.Any())
            {
                context.Judges.AddRange(
                    new Judge { Name = "Lead Judge", PinCode = "1234" },
                    new Judge { Name = "Guest Judge", PinCode = "5678" }
                );
                context.SaveChanges();
            }

            if (!context.Categories.Any())
            {
                var mainEvent = new Event { Name = "NAMCYA 2026 National Finals", Date = DateTime.Now.AddDays(30) };
                context.Events.Add(mainEvent);
                context.SaveChanges();

                var subEvent = new SubEvent { Name = "Choral Competitions", EventId = mainEvent.Id, Date = DateTime.Now.AddDays(31), Details = "Main Theater" };
                
                // Automatically assign our seeded Judges to this SubEvent
                foreach (var judge in context.Judges.ToList())
                {
                    subEvent.Judges.Add(judge);
                }
                context.SubEvents.Add(subEvent);
                context.SaveChanges();

                var category = new Category { Name = "Youth Choir", SubEventId = subEvent.Id };
                context.Categories.Add(category);
                context.SaveChanges();

                context.Criteria.AddRange(
                    new Criterion { Name = "Intonation", MaxScore = 30.00m, SubEventId = subEvent.Id },
                    new Criterion { Name = "Choral Blend", MaxScore = 30.00m, SubEventId = subEvent.Id },
                    new Criterion { Name = "Musicality", MaxScore = 30.00m, SubEventId = subEvent.Id },
                    new Criterion { Name = "Deportment", MaxScore = 10.00m, SubEventId = subEvent.Id }
                );

                context.Contestants.AddRange(
                    new Contestant { Name = "Manila Children's Choir", CategoryId = category.Id },
                    new Contestant { Name = "Cebu Youth Ensemble", CategoryId = category.Id },
                    new Contestant { Name = "Davao Vocal Group", CategoryId = category.Id }
                );

                context.SaveChanges();
            }
        }
    }
}