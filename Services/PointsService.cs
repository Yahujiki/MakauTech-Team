// ═══════════════════════════════════════════════════════════════════
// PointsService.cs — centralised points ledger with audit trail
// Demonstrates Encapsulation: all points mutations go through here,
// preventing scattered AddPoints calls across controllers.
// ═══════════════════════════════════════════════════════════════════
using MakauTech.Data;
using MakauTech.Models;

namespace MakauTech.Services
{
    /// <summary>
    /// Categorises why a user's points changed.
    /// Used for audit logging and analytics.
    /// </summary>
    public enum PointEventType
    {
        GameScore    = 1,  // earned from Food Catch, Food Blaster, Kitchen Rush
        GamePayment  = 2,  // deducted as entry fee to a game
        Review       = 3,  // left a review on a place
        Like         = 4,  // liked a place (combined with review for bonus)
        Feedback     = 5,  // submitted site feedback
        Admin        = 6,  // manually adjusted by admin
        Registration = 7   // welcome bonus on sign-up
    }

    /// <summary>
    /// An immutable record of one points transaction.
    /// Stored in-memory for the session; can be extended to persist to DB.
    /// </summary>
    public record PointTransaction(
        int            UserId,
        string         UserName,
        int            Delta,          // positive = earn, negative = spend
        int            BalanceAfter,
        PointEventType EventType,
        string         Description,
        DateTime       OccurredAt);

    /// <summary>
    /// Result returned from every points operation.
    /// </summary>
    public record PointsResult(
        bool   Success,
        int    Delta,
        int    NewBalance,
        string Message = "");

    /// <summary>
    /// Service that wraps all User.AddPoints / User.DeductPoints calls,
    /// adding logging, validation, and future extensibility (e.g. daily caps).
    ///
    /// Controllers use this instead of calling user.AddPoints() directly.
    /// </summary>
    public class PointsService
    {
        private readonly MakauTechDbContext _context;

        // In-memory audit log for the current server session.
        // In production this would be persisted to a PointTransactions table.
        private static readonly List<PointTransaction> AuditLog = new();
        private static readonly object AuditLock = new();

        // Optional: daily earning cap per user (anti-grind measure)
        private const int DailyEarningCap = 500;

        public PointsService(MakauTechDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ── Earn points ─────────────────────────────────────────────

        /// <summary>
        /// Awards <paramref name="amount"/> points to the user, respecting the
        /// daily cap, then records the transaction in the audit log.
        /// </summary>
        public PointsResult Earn(int userId, int amount,
            PointEventType eventType, string description = "")
        {
            if (amount <= 0)
                return new PointsResult(false, 0, 0, "Amount must be positive.");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return new PointsResult(false, 0, 0, "User not found.");

            // Check daily cap
            int earnedToday = GetTodayEarnings(userId);
            int effective   = Math.Min(amount, DailyEarningCap - earnedToday);
            if (effective <= 0)
                return new PointsResult(false, 0, user.Points,
                    $"Daily earning cap of {DailyEarningCap} pts reached.");

            user.AddPoints(effective);
            _context.SaveChanges();

            RecordTransaction(user, effective, eventType,
                description.Length > 0 ? description : eventType.ToString());

            return new PointsResult(true, effective, user.Points,
                effective < amount
                    ? $"Awarded {effective} pts (daily cap applied)."
                    : string.Empty);
        }

        // ── Spend points ────────────────────────────────────────────

        /// <summary>
        /// Deducts <paramref name="amount"/> points (game entry fee, etc.).
        /// Returns failure if the user cannot afford it.
        /// </summary>
        public PointsResult Spend(int userId, int amount,
            PointEventType eventType, string description = "")
        {
            if (amount <= 0)
                return new PointsResult(false, 0, 0, "Amount must be positive.");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return new PointsResult(false, 0, 0, "User not found.");

            if (!user.DeductPoints(amount))
                return new PointsResult(false, 0, user.Points,
                    $"Insufficient points — need {amount}, have {user.Points}.");

            _context.SaveChanges();
            RecordTransaction(user, -amount, eventType,
                description.Length > 0 ? description : eventType.ToString());

            return new PointsResult(true, -amount, user.Points);
        }

        // ── Audit ───────────────────────────────────────────────────

        /// <summary>Gets the most recent <paramref name="count"/> transactions.</summary>
        public static IReadOnlyList<PointTransaction> GetRecentTransactions(int count = 50)
        {
            lock (AuditLock)
            {
                return AuditLog
                    .OrderByDescending(t => t.OccurredAt)
                    .Take(count)
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>Gets all transactions for a specific user.</summary>
        public static IReadOnlyList<PointTransaction> GetUserHistory(int userId)
        {
            lock (AuditLock)
            {
                return AuditLog
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.OccurredAt)
                    .ToList()
                    .AsReadOnly();
            }
        }

        // ── Helpers ─────────────────────────────────────────────────

        private static int GetTodayEarnings(int userId)
        {
            var today = DateTime.Today;
            lock (AuditLock)
            {
                return AuditLog
                    .Where(t => t.UserId == userId
                             && t.Delta > 0
                             && t.OccurredAt.Date == today)
                    .Sum(t => t.Delta);
            }
        }

        private static void RecordTransaction(
            User user, int delta, PointEventType eventType, string description)
        {
            var tx = new PointTransaction(
                UserId:      user.Id,
                UserName:    user.Name,
                Delta:       delta,
                BalanceAfter:user.Points,
                EventType:   eventType,
                Description: description,
                OccurredAt:  DateTime.Now);

            lock (AuditLock)
            {
                AuditLog.Add(tx);
                // Keep log size bounded in memory
                if (AuditLog.Count > 10_000)
                    AuditLog.RemoveRange(0, 1_000);
            }
        }

        /// <summary>
        /// Human-readable emoji label for each event type.
        /// Used in admin dashboards and profile activity feeds.
        /// </summary>
        public static string DescribeEvent(PointEventType type) => type switch
        {
            PointEventType.GameScore    => "🎮 Game Score",
            PointEventType.GamePayment  => "🪙 Game Entry Fee",
            PointEventType.Review       => "✍️ Review",
            PointEventType.Like         => "❤️ Like",
            PointEventType.Feedback     => "💬 Feedback",
            PointEventType.Admin        => "⚙️ Admin Adjustment",
            PointEventType.Registration => "👋 Welcome Bonus",
            _                           => "❓ Unknown"
        };
    }
}
