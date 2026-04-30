// ═══════════════════════════════════════════════════════════════════
// TourismService.cs — place statistics, rankings, and recommendations
// ═══════════════════════════════════════════════════════════════════
using MakauTech.Data;
using MakauTech.Models;

namespace MakauTech.Services
{
    /// <summary>
    /// Aggregated statistics for a single place — returned as a value record.
    /// </summary>
    public record PlaceStats(
        int    PlaceId,
        string PlaceName,
        string Category,
        int    VisitCount,
        int    ReviewCount,
        int    LikeCount,
        double AverageRating,
        double PopularityScore);

    /// <summary>
    /// A lightweight recommendation returned to the view layer.
    /// </summary>
    public record PlaceRecommendation(
        int    PlaceId,
        string PlaceName,
        string ImageUrl,
        string Reason,
        double Score);

    /// <summary>
    /// Service that aggregates place data, computes popularity rankings,
    /// and produces personalised recommendations for logged-in users.
    ///
    /// Design note: Business logic lives here rather than in the controller
    /// to keep HomeController focused on HTTP concerns only (SRP).
    /// </summary>
    public class TourismService
    {
        private readonly MakauTechDbContext _context;

        // Weights used in the composite popularity score formula
        private const double VisitWeight  = 0.30;
        private const double RatingWeight = 0.35;
        private const double LikeWeight   = 0.25;
        private const double ReviewWeight = 0.10;

        public TourismService(MakauTechDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ── Stats ───────────────────────────────────────────────────

        /// <summary>
        /// Builds a full stats snapshot for every place in the database.
        /// Composite popularity score is a weighted sum of normalised metrics.
        /// </summary>
        public IReadOnlyList<PlaceStats> GetAllPlaceStats()
        {
            var places  = _context.Places.ToList();
            var reviews = _context.Reviews.ToList();
            var likes   = _context.PlaceLikes.ToList();

            if (!places.Any()) return Array.Empty<PlaceStats>();

            // Normalise visit count against maximum across all places
            int maxVisits = places.Max(p => p.VisitCount);
            if (maxVisits == 0) maxVisits = 1;

            return places.Select(place =>
            {
                int    reviewCount  = reviews.Count(r => r.PlaceId == place.Id);
                int    likeCount    = likes.Count(l => l.PlaceId == place.Id);
                double normVisit    = (double)place.VisitCount / maxVisits;
                double normRating   = place.Rating / 5.0;
                double normLikes    = Math.Log10(likeCount + 1) / 3.0;   // log scale
                double normReviews  = Math.Log10(reviewCount + 1) / 3.0; // log scale

                double popularity   = (normVisit  * VisitWeight)
                                    + (normRating * RatingWeight)
                                    + (normLikes  * LikeWeight)
                                    + (normReviews* ReviewWeight);

                return new PlaceStats(
                    PlaceId:        place.Id,
                    PlaceName:      place.Name,
                    Category:       place.Category?.Name ?? "General",
                    VisitCount:     place.VisitCount,
                    ReviewCount:    reviewCount,
                    LikeCount:      likeCount,
                    AverageRating:  place.Rating,
                    PopularityScore: Math.Round(popularity * 100, 1));
            })
            .OrderByDescending(s => s.PopularityScore)
            .ToList()
            .AsReadOnly();
        }

        /// <summary>Gets stats for a single place by ID, or null if not found.</summary>
        public PlaceStats? GetPlaceStats(int placeId)
            => GetAllPlaceStats().FirstOrDefault(s => s.PlaceId == placeId);

        // ── Recommendations ─────────────────────────────────────────

        /// <summary>
        /// Produces up to <paramref name="count"/> personalised recommendations
        /// for the given user, prioritising places they have not yet visited.
        /// Falls back to highest-rated places for anonymous visitors.
        /// </summary>
        public IReadOnlyList<PlaceRecommendation> GetRecommendations(
            int? userId, int count = 4)
        {
            var visited = userId.HasValue
                ? _context.Users
                      .FirstOrDefault(u => u.Id == userId.Value)
                      ?.VisitedPlaceIds ?? new List<int>()
                : new List<int>();

            var stats = GetAllPlaceStats();

            // Prefer unvisited; among equals, rank by composite score
            var sorted = stats
                .OrderBy(s => visited.Contains(s.PlaceId) ? 1 : 0)
                .ThenByDescending(s => s.PopularityScore)
                .Take(count);

            return sorted.Select(s =>
            {
                var place = _context.Places.Find(s.PlaceId);
                string reason = BuildRecommendationReason(s, visited);
                return new PlaceRecommendation(
                    PlaceId:   s.PlaceId,
                    PlaceName: s.PlaceName,
                    ImageUrl:  place?.ImageUrl ?? "/Images/placeholder.svg",
                    Reason:    reason,
                    Score:     s.PopularityScore);
            })
            .ToList()
            .AsReadOnly();
        }

        // ── Helpers ─────────────────────────────────────────────────

        private static string BuildRecommendationReason(
            PlaceStats s, IList<int> visited)
        {
            if (visited.Contains(s.PlaceId))
                return "You've been here — come back and leave a review!";
            if (s.AverageRating >= 4.5)
                return $"⭐ Highly rated at {s.AverageRating:F1}/5 — visitors love it!";
            if (s.LikeCount >= 5)
                return $"❤️ {s.LikeCount} people liked this spot — see why!";
            if (s.VisitCount >= 10)
                return $"📍 Popular with {s.VisitCount} visits — worth a look!";
            return "🗺️ Hidden gem — be among the first to explore!";
        }

        /// <summary>
        /// Returns the top N most active reviewers (by review count),
        /// used to feature community contributors on the homepage.
        /// </summary>
        public IReadOnlyList<(string Name, int ReviewCount, double AvgRating)>
            GetTopReviewers(int topN = 5)
        {
            return _context.Reviews
                .GroupBy(r => r.UserName)
                .Select(g => new
                {
                    Name        = g.Key,
                    ReviewCount = g.Count(),
                    AvgRating   = g.Average(r => r.Rating)
                })
                .OrderByDescending(x => x.ReviewCount)
                .Take(topN)
                .ToList()
                .Select(x => (x.Name, x.ReviewCount, Math.Round(x.AvgRating, 1)))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Returns a dictionary of category name → place count,
        /// used for displaying distribution charts or filters.
        /// </summary>
        public Dictionary<string, int> GetCategoryDistribution()
        {
            var places     = _context.Places.ToList();
            var categories = _context.Categories.ToList();

            return categories.ToDictionary(
                c => c.Name,
                c => places.Count(p => p.CategoryId == c.Id));
        }
    }
}
