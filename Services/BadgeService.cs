// ═══════════════════════════════════════════════════════════════════
// BadgeService.cs — achievement and badge evaluation logic
// Demonstrates OCP: new badge types can be added without modifying
// the User model or existing controller code.
// ═══════════════════════════════════════════════════════════════════
using MakauTech.Data;
using MakauTech.Models;

namespace MakauTech.Services
{
    /// <summary>
    /// Describes the result of an achievement check — which badges were
    /// newly awarded this session (so the UI can celebrate them).
    /// </summary>
    public record BadgeCheckResult(
        IReadOnlyList<string> NewlyAwarded,
        IReadOnlyList<string> AllBadges,
        bool HasNewBadge)
    {
        public static BadgeCheckResult Empty { get; } = new(
            Array.Empty<string>(), Array.Empty<string>(), false);
    }

    /// <summary>
    /// A rule that determines whether a user should receive a badge.
    /// Each rule is a pure function: (User, DbContext) → bool.
    /// This follows the Strategy pattern — rules are swappable at runtime.
    /// </summary>
    public class BadgeRule
    {
        public string   BadgeName   { get; init; } = string.Empty;
        public string   Description { get; init; } = string.Empty;
        public string   Icon        { get; init; } = "🏅";
        public Func<User, MakauTechDbContext, bool> Condition { get; init; } = (_, _) => false;
    }

    /// <summary>
    /// Evaluates badge rules against a user and awards any newly earned badges.
    ///
    /// Controllers inject this service to run badge checks after point events,
    /// keeping the User model thin and the award logic centralised.
    /// </summary>
    public class BadgeService
    {
        private readonly MakauTechDbContext _context;

        /// <summary>
        /// Master list of all badge rules.  Adding a new badge means appending
        /// one BadgeRule here — no other code needs to change.
        /// </summary>
        private static readonly IReadOnlyList<BadgeRule> Rules = new List<BadgeRule>
        {
            new()
            {
                BadgeName   = "First Step",
                Description = "Visited your very first place in Sibu.",
                Icon        = "👣",
                Condition   = (u, _) => u.VisitedPlaceIds.Count >= 1
            },
            new()
            {
                BadgeName   = "Explorer",
                Description = "Visited 3 or more places.",
                Icon        = "🗺️",
                Condition   = (u, _) => u.VisitedPlaceIds.Count >= 3
            },
            new()
            {
                BadgeName   = "Food Hunter",
                Description = "Visited 5 or more places.",
                Icon        = "🍽️",
                Condition   = (u, _) => u.VisitedPlaceIds.Count >= 5
            },
            new()
            {
                BadgeName   = "Active Reviewer",
                Description = "Earned 100 or more points.",
                Icon        = "✍️",
                Condition   = (u, _) => u.Points >= 100
            },
            new()
            {
                BadgeName   = "Sibu Expert",
                Description = "Earned 200 or more points.",
                Icon        = "🏆",
                Condition   = (u, _) => u.Points >= 200
            },
            new()
            {
                BadgeName   = "Champion",
                Description = "Reached 500 points — top tier explorer!",
                Icon        = "👑",
                Condition   = (u, _) => u.Points >= 500
            },
            new()
            {
                BadgeName   = "Game Addict",
                Description = "Earned 50+ pts from games in a single session.",
                Icon        = "🎮",
                // Checked manually from GameService; marked by session flag
                Condition   = (u, _) => u.Points >= 50
            },
            new()
            {
                BadgeName   = "Community Voice",
                Description = "Left reviews on 3 or more places.",
                Icon        = "💬",
                Condition   = (u, db) =>
                    db.Reviews.Count(r => r.UserId == u.Id) >= 3
            },
            new()
            {
                BadgeName   = "Liked & Loved",
                Description = "Liked 5 or more places.",
                Icon        = "❤️",
                Condition   = (u, db) =>
                    db.PlaceLikes.Count(l => l.UserId == u.Id) >= 5
            }
        }.AsReadOnly();

        public BadgeService(MakauTechDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ── Public API ───────────────────────────────────────────────

        /// <summary>
        /// Evaluates all badge rules for <paramref name="user"/>.
        /// Awards any newly earned badges and persists to the database.
        /// Returns which badges (if any) were newly granted.
        /// </summary>
        public BadgeCheckResult EvaluateAndAward(User user)
        {
            if (user == null) return BadgeCheckResult.Empty;

            var newlyAwarded = new List<string>();

            foreach (var rule in Rules)
            {
                if (user.Badges.Contains(rule.BadgeName)) continue;

                bool earned;
                try   { earned = rule.Condition(user, _context); }
                catch { earned = false; }

                if (!earned) continue;

                user.Badges.Add(rule.BadgeName);
                newlyAwarded.Add(rule.BadgeName);
            }

            if (newlyAwarded.Count > 0)
                _context.SaveChanges();

            return new BadgeCheckResult(
                NewlyAwarded: newlyAwarded.AsReadOnly(),
                AllBadges:    user.Badges.AsReadOnly(),
                HasNewBadge:  newlyAwarded.Count > 0);
        }

        /// <summary>
        /// Returns all badge rules (for displaying locked/unlocked states in UI).
        /// </summary>
        public static IReadOnlyList<BadgeRule> GetAllRules() => Rules;

        /// <summary>
        /// Returns the icon for a given badge name, or 🏅 if unknown.
        /// </summary>
        public static string GetIcon(string badgeName)
            => Rules.FirstOrDefault(r => r.BadgeName == badgeName)?.Icon ?? "🏅";

        /// <summary>
        /// Returns a user's progress toward their next unearned badge.
        /// Used to display motivating progress messages on the profile page.
        /// </summary>
        public string GetNextBadgeHint(User user)
        {
            if (user == null) return string.Empty;

            foreach (var rule in Rules)
            {
                if (user.Badges.Contains(rule.BadgeName)) continue;
                try
                {
                    bool earned = rule.Condition(user, _context);
                    if (!earned)
                        return $"Next: {rule.Icon} {rule.BadgeName} — {rule.Description}";
                }
                catch { /* skip */ }
            }

            return "🌟 You've unlocked all available badges!";
        }
    }
}
