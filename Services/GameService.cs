// ═══════════════════════════════════════════════════════════════════
// GameService.cs  — centralises scoring, leaderboard, and session logic
// OOP: encapsulates all game-domain operations behind a service boundary
// ═══════════════════════════════════════════════════════════════════
using MakauTech.Data;
using MakauTech.Models;
using Microsoft.EntityFrameworkCore;

namespace MakauTech.Services
{
    /// <summary>
    /// Supported game modes in MakauTech.
    /// Used for distinguishing score submissions and leaderboard queries.
    /// </summary>
    public enum GameType
    {
        FoodCatch   = 1,   // pay-to-play catch game   (cost 5 pts)
        FoodBlaster = 2,   // pay-to-play shooter       (cost 10 pts)
        KitchenRush = 3    // pay-to-play kitchen game  (cost 10 pts)
    }

    /// <summary>
    /// Immutable snapshot of one game round result — returned by service methods.
    /// Using a record ensures consumers cannot mutate the returned data.
    /// </summary>
    public record GameResult(
        bool   Success,
        int    PointsEarned,
        int    TotalPoints,
        string Message = "");

    /// <summary>
    /// A single entry in the all-time leaderboard for a specific game.
    /// </summary>
    public record LeaderboardEntry(
        int    Rank,
        string PlayerName,
        string PlayerLevel,
        int    BestScore,
        int    TotalPoints,
        DateTime LastPlayed);

    /// <summary>
    /// Service layer for all game-related operations.
    /// Follows SRP: controllers handle HTTP, this class handles business logic.
    /// </summary>
    public class GameService
    {
        private readonly MakauTechDbContext _context;

        // Points caps per game type (server-side anti-cheat)
        private static readonly Dictionary<GameType, int> ScoreCaps = new()
        {
            { GameType.FoodCatch,   100 },
            { GameType.FoodBlaster,  45 },
            { GameType.KitchenRush,  40 }
        };

        // Cost to play per game type
        public static readonly Dictionary<GameType, int> PlayCosts = new()
        {
            { GameType.FoodCatch,   20 },
            { GameType.FoodBlaster, 20 },
            { GameType.KitchenRush, 20 }
        };

        public GameService(MakauTechDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ── Score Submission ────────────────────────────────────────

        /// <summary>
        /// Validates, caps, and awards points for a completed game round.
        /// Returns a GameResult describing what happened.
        /// </summary>
        public GameResult SubmitScore(int userId, int rawScore, GameType gameType)
        {
            if (rawScore < 0)
                return new GameResult(false, 0, 0, "Score cannot be negative.");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return new GameResult(false, 0, 0, "User not found.");

            int cap     = ScoreCaps.GetValueOrDefault(gameType, 50);
            int capped  = Math.Min(rawScore, cap);

            if (capped > 0)
            {
                user.AddPoints(capped);
                _context.SaveChanges();
            }

            return new GameResult(
                Success:      true,
                PointsEarned: capped,
                TotalPoints:  user.Points,
                Message:      capped < rawScore
                    ? $"Score capped at {cap} pts for fair play."
                    : string.Empty);
        }

        // ── Pay To Play ─────────────────────────────────────────────

        /// <summary>
        /// Deducts the entry fee for a game, honouring first-game-free sessions.
        /// Returns success=true with remainingPoints if the deduction worked.
        /// </summary>
        public GameResult PayToPlay(int userId, GameType gameType, bool isFreeSession)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return new GameResult(false, 0, 0, "Not logged in.");

            if (isFreeSession)
                return new GameResult(true, 0, user.Points, "First game is on us!");

            int cost = PlayCosts.GetValueOrDefault(gameType, 5);
            if (!user.DeductPoints(cost))
                return new GameResult(false, 0, user.Points,
                    $"Not enough points! You need {cost} pts to play.");

            _context.SaveChanges();
            return new GameResult(true, 0, user.Points);
        }

        // ── Leaderboard ─────────────────────────────────────────────

        /// <summary>
        /// Returns the top N players by total points (global leaderboard).
        /// </summary>
        public IReadOnlyList<LeaderboardEntry> GetGlobalLeaderboard(int topN = 10)
        {
            var users = _context.Users
                .OrderByDescending(u => u.Points)
                .Take(topN)
                .ToList();

            return users.Select((u, idx) => new LeaderboardEntry(
                Rank:        idx + 1,
                PlayerName:  u.Name,
                PlayerLevel: u.Level,
                BestScore:   u.Points,
                TotalPoints: u.Points,
                LastPlayed:  u.CreatedAt
            )).ToList().AsReadOnly();
        }

        // ── Validation Helpers ───────────────────────────────────────

        /// <summary>
        /// Returns true if the score looks humanly achievable in the allotted time.
        /// Scores far beyond the cap are suspicious and rejected before DB write.
        /// </summary>
        public static bool IsScorePlausible(int score, GameType gameType)
        {
            int cap = ScoreCaps.GetValueOrDefault(gameType, 50);
            // Allow 10 % over cap (rounding artefacts) but reject obvious cheats
            return score <= (int)(cap * 1.1);
        }

        /// <summary>
        /// Describes a game type in a human-readable format for views/emails.
        /// Demonstrates polymorphism through a strategy-like switch.
        /// </summary>
        public static string DescribeGame(GameType type) => type switch
        {
            GameType.FoodCatch   => "🍜 Sibu Food Catch — catch falling street food with your bowl",
            GameType.FoodBlaster => "🔫 Sibu Food Blaster — shoot food targets before time runs out",
            GameType.KitchenRush => "🍳 Kitchen Rush — memorise dish orders and tap ingredients fast",
            _                    => "Unknown game"
        };
    }
}
