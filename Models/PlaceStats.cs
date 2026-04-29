// ═══════════════════════════════════════════════════════════════════
// PlaceStats.cs — extended view-model for place analytics display
// Extends the base Place model with computed/aggregated fields.
// ═══════════════════════════════════════════════════════════════════
namespace MakauTech.Models
{
    /// <summary>
    /// View-model carrying a Place alongside its computed social stats.
    /// Keeps aggregation logic out of the Place domain model (SRP).
    /// </summary>
    public class PlaceCardViewModel
    {
        public Place  Place       { get; set; } = null!;
        public int    LikeCount   { get; set; }
        public int    ReviewCount { get; set; }
        public bool   UserLiked   { get; set; }
        public bool   UserVisited { get; set; }

        /// <summary>True when the place has no rating and no reviews yet.</summary>
        public bool IsNew => ReviewCount == 0 && Place.Rating == 0;

        /// <summary>Formatted rating string, or a prompt for the first reviewer.</summary>
        public string RatingDisplay
            => Place.Rating > 0 ? $"⭐ {Place.Rating:F1}" : "Be the first to rate!";

        /// <summary>Abbreviated description safe to show on cards (max 120 chars).</summary>
        public string ShortDescription
            => string.IsNullOrWhiteSpace(Place.Description)
               ? $"Explore {Place.Name} in Sibu, Sarawak."
               : (Place.Description.Length > 120
                  ? Place.Description[..117] + "…"
                  : Place.Description);
    }

    /// <summary>
    /// Dashboard-level summary used by the admin analytics view.
    /// </summary>
    public class SiteAnalytics
    {
        public int TotalUsers       { get; set; }
        public int TotalPlaces      { get; set; }
        public int TotalReviews     { get; set; }
        public int TotalLikes       { get; set; }
        public int TotalVisits      { get; set; }
        public int ActiveThisWeek   { get; set; }
        public double AvgRating     { get; set; }
        public string MostPopular   { get; set; } = string.Empty;
        public string TopPlayer     { get; set; } = string.Empty;
        public int    TopPlayerPts  { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Lightweight struct passed to the leaderboard view so it doesn't
    /// need to expose full User objects (privacy, performance).
    /// </summary>
    public class LeaderboardRow
    {
        public int    Rank         { get; set; }
        public string Name         { get; set; } = string.Empty;
        public string Level        { get; set; } = string.Empty;
        public int    Points       { get; set; }
        public int    PlaceCount   { get; set; }
        public int    ReviewCount  { get; set; }
        public string RankBadge    => Rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{Rank}" };
        public string LevelBadge   => Level switch
        {
            "Gold"   => "🥇 Gold",
            "Silver" => "🥈 Silver",
            "Bronze" => "🥉 Bronze",
            _        => "🆕 Newbie"
        };
    }

}
