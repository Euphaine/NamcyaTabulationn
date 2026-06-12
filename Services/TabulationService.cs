using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NamcyaTabulation.Data;
using NamcyaTabulation.Models;

namespace NamcyaTabulation.Services
{
    public class TabulationResult
    {
        public int ContestantId { get; set; }
        public string ContestantName { get; set; } = string.Empty;
        public decimal RankSum { get; set; }
        public decimal FinalScore { get; set; }
        public int Rank { get; set; }
    }

    public class JudgeScoreBreakdown
    {
        public string JudgeName { get; set; } = string.Empty;
        public int ContestantId { get; set; }
        public string ContestantName { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public decimal JudgeRank { get; set; }
    }

    public class TabulationService
    {
        private readonly AppDbContext _context;

        public TabulationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TabulationResult>> GetRankingAsync(int categoryId)
        {
            // Fetch our global settings to check if ties are allowed
            bool allowTies = false;
            int decimalPrecision = 2;
            try
            {
                var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
                allowTies = settings?.AllowTies ?? false;
                decimalPrecision = settings?.DecimalPrecision ?? 2;
            }
            catch
            {
                // Safe fallback in case the SystemSettings table hasn't been created in MySQL yet!
            }

            var contestants = await _context.Contestants
                .AsNoTracking()
                .Where(c => c.CategoryId == categoryId)
                .ToListAsync();

            if (!contestants.Any()) return new List<TabulationResult>();

            var scores = await _context.ScoreSheets
                .AsNoTracking()
                .Where(s => s.Contestant!.CategoryId == categoryId)
                .ToListAsync();

            var activeJudgeIds = scores.Select(s => s.JudgeId).Distinct().ToList();
            if (!activeJudgeIds.Any()) 
            {
                return contestants.Select(c => new TabulationResult
                {
                    ContestantId = c.Id,
                    ContestantName = c.Name,
                    RankSum = 0,
                    FinalScore = 0,
                    Rank = 0
                }).OrderBy(c => c.ContestantName).ToList();
            }

            // 1. Calculate Total Score per judge per contestant (safely handling un-scored contestants as 0)
            var contestantJudgeScores = new List<(int ContestantId, string ContestantName, int JudgeId, decimal TotalScore)>();
            
            foreach (var judgeId in activeJudgeIds)
            {
                foreach (var contestant in contestants)
                {
                    var totalScore = scores.Where(s => s.JudgeId == judgeId && s.ContestantId == contestant.Id).Sum(s => s.Points);
                    contestantJudgeScores.Add((contestant.Id, contestant.Name, judgeId, totalScore));
                }
            }

            // 2. Judge-Level Ranking
            var judgeRankings = new List<(int ContestantId, string ContestantName, decimal TotalScore, decimal JudgeRank)>();
            
            var scoresByJudge = contestantJudgeScores.GroupBy(s => s.JudgeId);
            foreach (var judgeGroup in scoresByJudge)
            {
                var sortedScores = judgeGroup.OrderByDescending(s => s.TotalScore).ToList();
                int judgeCurrentRank = 1;
                decimal judgeLastRank = 1;
                
                for (int i = 0; i < sortedScores.Count; i++)
                {
                    decimal assignedRank = judgeCurrentRank;
                    if (i > 0 && sortedScores[i].TotalScore == sortedScores[i - 1].TotalScore)
                    {
                        // Handle tie: give them the same rank as the previous contestant
                        assignedRank = judgeLastRank;
                    }
                    
                    judgeRankings.Add((sortedScores[i].ContestantId, sortedScores[i].ContestantName, sortedScores[i].TotalScore, assignedRank));
                    
                    judgeLastRank = assignedRank;
                    judgeCurrentRank++;
                }
            }

            // 3. Rank Summation & 4. Sum of Scores
            var results = new List<TabulationResult>();
            var contestantGroups = judgeRankings.GroupBy(r => new { r.ContestantId, r.ContestantName });

            foreach (var group in contestantGroups)
            {
                decimal rankSum = group.Sum(r => r.JudgeRank);
                decimal averageScore = Math.Round(group.Average(r => r.TotalScore), decimalPrecision);

                results.Add(new TabulationResult
                {
                    ContestantId = group.Key.ContestantId,
                    ContestantName = group.Key.ContestantName,
                    RankSum = rankSum,
                    FinalScore = averageScore
                });
            }

            // 5. Final Leaderboard Sorting
            var rankedResults = results
                .OrderBy(r => r.RankSum) // Primary Sort: Lowest sum of ranks wins
                .ThenByDescending(r => r.FinalScore) // Secondary Sort: Highest sum of scores breaks the tie
                .ThenBy(r => r.ContestantName) // Tertiary Sort: Alphabetical tie-breaker if ties are strictly disabled
                .ToList();
                
            int currentRank = 1;
            for (int i = 0; i < rankedResults.Count; i++)
            {
                bool isTied = i > 0 && 
                              rankedResults[i].RankSum == rankedResults[i - 1].RankSum && 
                              rankedResults[i].FinalScore == rankedResults[i - 1].FinalScore;
                              
                rankedResults[i].Rank = (isTied && allowTies) ? rankedResults[i - 1].Rank : currentRank;
                currentRank++;
            }
            
            return rankedResults;
        }

        public async Task<List<JudgeScoreBreakdown>> GetJudgeBreakdownAsync(int categoryId)
        {
            var contestants = await _context.Contestants
                .AsNoTracking()
                .Where(c => c.CategoryId == categoryId)
                .ToListAsync();
                
            if (!contestants.Any()) return new List<JudgeScoreBreakdown>();

            var scores = await _context.ScoreSheets
                .AsNoTracking()
                .Include(s => s.Judge)
                .Where(s => s.Contestant!.CategoryId == categoryId)
                .ToListAsync();

            var activeJudges = scores.Where(s => s.Judge != null).GroupBy(s => s.JudgeId).Select(g => g.First().Judge!).ToList();
            if (!activeJudges.Any()) return new List<JudgeScoreBreakdown>();

            var contestantJudgeScores = new List<JudgeScoreBreakdown>();
            foreach (var judge in activeJudges)
            {
                foreach (var contestant in contestants)
                {
                    var totalScore = scores.Where(s => s.JudgeId == judge.Id && s.ContestantId == contestant.Id).Sum(s => s.Points);
                    contestantJudgeScores.Add(new JudgeScoreBreakdown { ContestantId = contestant.Id, ContestantName = contestant.Name, JudgeName = judge.Name, TotalScore = totalScore });
                }
            }

            var breakdown = new List<JudgeScoreBreakdown>();
            var scoresByJudge = contestantJudgeScores.GroupBy(s => s.JudgeName);
            
            foreach (var judgeGroup in scoresByJudge)
            {
                var sortedScores = judgeGroup.OrderByDescending(s => s.TotalScore).ToList();
                int currentRank = 1;
                decimal lastRank = 1;
                
                for (int i = 0; i < sortedScores.Count; i++)
                {
                    decimal assignedRank = currentRank;
                    if (i > 0 && sortedScores[i].TotalScore == sortedScores[i - 1].TotalScore)
                    {
                        assignedRank = lastRank;
                    }
                    
                    sortedScores[i].JudgeRank = assignedRank;
                    breakdown.Add(sortedScores[i]);
                    
                    lastRank = assignedRank;
                    currentRank++;
                }
            }
            
            return breakdown.OrderBy(b => b.JudgeName).ThenBy(b => b.JudgeRank).ToList();
        }
    }
}