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
        public List<string> Comments { get; set; } = new();
    }

    public class JudgeScoreBreakdown
    {
        public int JudgeId { get; set; }
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

            var allComments = new List<string>();
            try 
            {
                allComments = await _context.JudgeComments
                    .AsNoTracking()
                    .Where(c => c.Contestant!.CategoryId == categoryId && !string.IsNullOrWhiteSpace(c.Comment))
                    .Select(c => c.Comment)
                    .ToListAsync();
            }
            catch 
            {
                // Failsafe: Ignore comments if the table is unavailable
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
                // Force standard mathematical rounding (Half-Up) instead of C#'s default Banker's Rounding
                decimal averageScore = Math.Round(group.Average(r => r.TotalScore), decimalPrecision, MidpointRounding.AwayFromZero);

                results.Add(new TabulationResult
                {
                    ContestantId = group.Key.ContestantId,
                    ContestantName = group.Key.ContestantName,
                    RankSum = rankSum,
                    FinalScore = averageScore,
                    Comments = allComments // Use the safely fetched comments
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
                    contestantJudgeScores.Add(new JudgeScoreBreakdown { JudgeId = judge.Id, ContestantId = contestant.Id, ContestantName = contestant.Name, JudgeName = judge.Name, TotalScore = totalScore });
                }
            }

            var breakdown = new List<JudgeScoreBreakdown>();
            var scoresByJudge = contestantJudgeScores.GroupBy(s => new { s.JudgeId, s.JudgeName });
            
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

        public string GenerateResultsExcelHtml(List<TabulationResult> results, string eventName, string subEventName, string categoryName)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            
            // Native Excel XML settings to force A4 Page Size and Print Layout
            sb.AppendLine("<!--[if gte mso 9]>");
            sb.AppendLine("<xml>");
            sb.AppendLine(" <x:ExcelWorkbook>");
            sb.AppendLine("  <x:ExcelWorksheets>");
            sb.AppendLine("   <x:ExcelWorksheet>");
            sb.AppendLine("    <x:Name>Official Results</x:Name>");
            sb.AppendLine("    <x:WorksheetOptions>");
            sb.AppendLine("     <x:PageSetup>");
            sb.AppendLine("      <x:Layout x:Orientation=\"Portrait\"/>");
            sb.AppendLine("     </x:PageSetup>");
            sb.AppendLine("     <x:Print>");
            sb.AppendLine("      <x:ValidPrinterInfo/>");
            sb.AppendLine("      <x:PaperSizeIndex>9</x:PaperSizeIndex> <!-- 9 = A4 Size -->");
            sb.AppendLine("      <x:FitWidth>1</x:FitWidth>");
            sb.AppendLine("      <x:FitHeight>100</x:FitHeight>");
            sb.AppendLine("     </x:Print>");
            sb.AppendLine("     <x:Selected/>");
            sb.AppendLine("     <x:FitToPage/>");
            sb.AppendLine("    </x:WorksheetOptions>");
            sb.AppendLine("   </x:ExcelWorksheet>");
            sb.AppendLine("  </x:ExcelWorksheets>");
            sb.AppendLine(" </x:ExcelWorkbook>");
            sb.AppendLine("</xml>");
            sb.AppendLine("<![endif]-->");

            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Times New Roman', serif; }");
            sb.AppendLine("table { border-collapse: collapse; width: 550pt; margin: 0 auto; }");
            sb.AppendLine("th { background-color: #f2f2f2; color: #000000; font-weight: bold; padding: 12px; border: 1pt solid black; text-align: center; font-size: 12pt; text-transform: uppercase; }");
            sb.AppendLine("td { padding: 10px; border: 1pt solid black; text-align: center; vertical-align: middle; font-size: 11pt; color: #000000; }");
            sb.AppendLine(".no-border { border: none !important; }");
            sb.AppendLine(".title { font-size: 20pt; font-weight: bold; color: #000000; text-align: center; border: none; letter-spacing: 1px; text-transform: uppercase; }");
            sb.AppendLine(".subtitle { font-size: 14pt; font-weight: bold; color: #333333; text-align: center; border: none; }");
            sb.AppendLine(".meta-info { font-size: 11pt; font-weight: bold; text-align: left; border: none; color: #000000; }");
            sb.AppendLine(".text-left { text-align: left; }");
            sb.AppendLine(".gold { background-color: #fff9e6; font-weight: bold; }"); // Very subtle professional gold
            sb.AppendLine(".silver { background-color: #f0f4f8; font-weight: bold; }"); // Very subtle professional silver
            sb.AppendLine(".bronze { background-color: #fff3e6; font-weight: bold; }"); // Very subtle professional bronze
            sb.AppendLine(".sig-line { border-bottom: 1pt solid black; border-top: none; border-left: none; border-right: none; }");
            sb.AppendLine("</style></head><body>");
            
            sb.AppendLine("<table>");
            
            // Formal Header
            sb.AppendLine($"<tr><td colspan=\"4\" class=\"title\">Official Tabulation Results</td></tr>");
            sb.AppendLine($"<tr><td colspan=\"4\" class=\"subtitle\">{eventName}</td></tr>");
            sb.AppendLine("<tr><td colspan=\"4\" class=\"no-border\">&nbsp;</td></tr>");
            
            // Structured Metadata (Side by side)
            sb.AppendLine($"<tr><td colspan=\"2\" class=\"meta-info\">Sub-Event: {subEventName}</td><td colspan=\"2\" class=\"meta-info\" style=\"text-align: right;\">Date: {DateTime.Now:MMMM dd, yyyy}</td></tr>");
            sb.AppendLine($"<tr><td colspan=\"4\" class=\"meta-info\">Category: {categoryName}</td></tr>");
            sb.AppendLine("<tr><td colspan=\"4\" class=\"no-border\">&nbsp;</td></tr>");
            
            sb.AppendLine("<tr>");
            sb.AppendLine("<th style=\"width: 70pt;\">Rank</th>");
            sb.AppendLine("<th class=\"text-left\" style=\"width: 280pt;\">Contestant Name</th>");
            sb.AppendLine("<th style=\"width: 100pt;\">Rank Sum</th>");
            sb.AppendLine("<th style=\"width: 100pt;\">Average Score</th>");
            sb.AppendLine("</tr>");

            foreach (var rank in results)
            {
                var rankDisplay = (rank.FinalScore == 0 && rank.Rank == 0) ? "-" : rank.Rank.ToString();
                var sumDisplay = (rank.FinalScore == 0 && rank.Rank == 0) ? "-" : rank.RankSum.ToString("0.##");
                var scoreDisplay = (rank.FinalScore == 0 && rank.Rank == 0) ? "-" : rank.FinalScore.ToString("0.##");
                
                string rowClass = rank.Rank == 1 ? "gold" : rank.Rank == 2 ? "silver" : rank.Rank == 3 ? "bronze" : "";

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td class=\"{rowClass}\">{rankDisplay}</td>");
                sb.AppendLine($"<td class=\"text-left {rowClass}\">{rank.ContestantName}</td>");
                sb.AppendLine($"<td class=\"{rowClass}\">{sumDisplay}</td>");
                sb.AppendLine($"<td class=\"{rowClass}\">{scoreDisplay}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");

            // Add Signature Lines for Physical Document Signing
            sb.AppendLine("<br><br><br>");
            sb.AppendLine("<table style=\"width: 550pt; margin: 0 auto; border: none;\">");
            sb.AppendLine("<tr><td class=\"no-border\" style=\"width: 10%;\"></td><td class=\"sig-line\" style=\"width: 35%;\">&nbsp;</td><td class=\"no-border\" style=\"width: 10%;\"></td><td class=\"sig-line\" style=\"width: 35%;\">&nbsp;</td><td class=\"no-border\" style=\"width: 10%;\"></td></tr>");
            sb.AppendLine("<tr><td class=\"no-border\"></td><td class=\"no-border\" style=\"text-align: center; font-weight: bold; font-size: 11pt;\">Chairman, Board of Judges</td><td class=\"no-border\"></td><td class=\"no-border\" style=\"text-align: center; font-weight: bold; font-size: 11pt;\">Official Tabulator</td><td class=\"no-border\"></td></tr>");
            sb.AppendLine("</table>");

            // Add Official Subdued NAMCYA Branding Footer
            sb.AppendLine("<br><br>");
            sb.AppendLine("<table style=\"width: 550pt; margin: 0 auto; border: none;\">");
            sb.AppendLine("<tr><td style=\"font-weight: bold; text-align: center; color: #000000; font-size: 10pt; border: none;\">NAMCYA - National Music Competitions for Young Artists</td></tr>");
            sb.AppendLine("<tr><td style=\"text-align: center; color: #666666; font-size: 9pt; border: none;\">www.namcya.com | namcya@gmail.com | 8836-4928 / 0949 993 2592</td></tr>");
            sb.AppendLine($"<tr><td style=\"text-align: center; color: #666666; font-size: 9pt; border: none; font-style: italic;\">Generated by NAMCYA Tabulation System on {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}</td></tr>");
            sb.AppendLine("</table></body></html>");
            return sb.ToString();
        }
    }
}