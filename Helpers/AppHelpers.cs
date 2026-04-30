// ═══════════════════════════════════════════════════════════════════
// AppHelpers.cs — static utility methods and extension methods
// Grouped here to avoid duplication across controllers and views.
// ═══════════════════════════════════════════════════════════════════
using MakauTech.Models;

namespace MakauTech.Helpers
{
    /// <summary>
    /// General-purpose extension methods used across the MakauTech codebase.
    /// Extension methods are a C# language feature that adds behaviour to
    /// existing types without subclassing (open/closed principle in practice).
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>Truncates a string to at most <paramref name="max"/> chars, appending "…".</summary>
        public static string Truncate(this string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s[..max] + "…";
        }

        /// <summary>Returns a title-cased version of the string.</summary>
        public static string ToTitleCase(this string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', words.Select(w =>
                char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
        }

        /// <summary>Returns true if the string is a valid e-mail address (lightweight check).</summary>
        public static bool IsValidEmail(this string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Trim().Split('@');
            return parts.Length == 2
                && parts[0].Length > 0
                && parts[1].Contains('.')
                && parts[1].Length > 2;
        }

        /// <summary>
        /// Returns a URL-safe slug from an arbitrary string.
        /// Used for generating clean URLs for place names.
        /// </summary>
        public static string ToSlug(this string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "place";
            return new string(s.ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray())
                .Trim('-');
        }

        /// <summary>Masks an e-mail for display: b***@example.com</summary>
        public static string MaskEmail(this string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "***";
            var idx = email.IndexOf('@');
            if (idx <= 1) return "***" + email[idx..];
            return email[0] + new string('*', Math.Min(idx - 1, 4)) + email[idx..];
        }
    }

    /// <summary>
    /// Formatting helpers used in Razor views and controllers to keep
    /// presentation logic out of the domain model.
    /// </summary>
    public static class FormatHelpers
    {
        /// <summary>Formats a points value with a 🪙 emoji and comma separator.</summary>
        public static string FormatPoints(int pts)
            => $"🪙 {pts:N0} pt{(pts == 1 ? "" : "s")}";

        /// <summary>Formats a rating value as a star string, e.g. ⭐ 4.5 / 5.</summary>
        public static string FormatRating(double rating)
            => rating > 0 ? $"⭐ {rating:F1} / 5" : "No ratings yet";

        /// <summary>Returns a relative time string: "just now", "3m ago", "2h ago", "4d ago".</summary>
        public static string RelativeTime(DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalSeconds < 60)  return "just now";
            if (diff.TotalMinutes < 60)  return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24)    return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7)      return $"{(int)diff.TotalDays}d ago";
            return dt.ToString("dd MMM yyyy");
        }

        /// <summary>Returns a level badge with emoji colour for a given level string.</summary>
        public static string LevelBadge(string level) => level switch
        {
            "Gold"   => "🥇 Gold",
            "Silver" => "🥈 Silver",
            "Bronze" => "🥉 Bronze",
            _        => "🆕 Newbie"
        };

        /// <summary>Returns a heat emoji based on a popularity score 0–100.</summary>
        public static string PopularityEmoji(double score) => score switch
        {
            >= 80 => "🔥 Hot",
            >= 60 => "⭐ Popular",
            >= 40 => "🌟 Rising",
            >= 20 => "🗺️ Explore",
            _     => "💎 Hidden Gem"
        };

        /// <summary>Formats a file size in bytes to a human-readable string.</summary>
        public static string FormatFileSize(long bytes)
        {
            if (bytes < 1024)        return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
    }

    /// <summary>
    /// Validates user input across controllers — centralised to avoid
    /// copy-paste validation logic in every action method.
    /// </summary>
    public static class ValidationHelpers
    {
        public const int MinPasswordLength = 6;
        public const int MaxNameLength     = 100;
        public const int MaxCommentLength  = 2000;

        /// <summary>Returns an error message if the name is invalid, else null.</summary>
        public static string? ValidateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Name is required.";
            if (name.Trim().Length < 2)          return "Name must be at least 2 characters.";
            if (name.Trim().Length > MaxNameLength) return $"Name must be under {MaxNameLength} chars.";
            return null;
        }

        /// <summary>Returns an error message if the password is invalid, else null.</summary>
        public static string? ValidatePassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password)) return "Password is required.";
            if (password.Length < MinPasswordLength)
                return $"Password must be at least {MinPasswordLength} characters.";
            return null;
        }

        /// <summary>Returns an error message if the comment is invalid, else null.</summary>
        public static string? ValidateComment(string? comment)
        {
            if (string.IsNullOrWhiteSpace(comment)) return "Comment cannot be empty.";
            if (comment.Trim().Length < 5)          return "Comment must be at least 5 characters.";
            if (comment.Length > MaxCommentLength)  return $"Comment must be under {MaxCommentLength} chars.";
            return null;
        }

        /// <summary>Returns an error if the rating is not in 1–5 range.</summary>
        public static string? ValidateRating(int rating)
            => (rating < 1 || rating > 5) ? "Rating must be between 1 and 5." : null;
    }

    /// <summary>
    /// Pagination helper — avoids duplicate paging math in controllers.
    /// </summary>
    public class PaginationInfo
    {
        public int CurrentPage   { get; }
        public int PageSize      { get; }
        public int TotalItems    { get; }
        public int TotalPages    => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPrevious  => CurrentPage > 1;
        public bool HasNext      => CurrentPage < TotalPages;
        public int  SkipCount    => (CurrentPage - 1) * PageSize;

        public PaginationInfo(int currentPage, int pageSize, int totalItems)
        {
            PageSize    = Math.Max(1, pageSize);
            TotalItems  = Math.Max(0, totalItems);
            CurrentPage = Math.Clamp(currentPage, 1, Math.Max(1, TotalPages));
        }

        /// <summary>Returns an IQueryable slice for this page (for EF Core queries).</summary>
        public IQueryable<T> Apply<T>(IQueryable<T> query)
            => query.Skip(SkipCount).Take(PageSize);

        /// <summary>Returns a list slice for in-memory collections.</summary>
        public IReadOnlyList<T> Apply<T>(IReadOnlyList<T> list)
            => list.Skip(SkipCount).Take(PageSize).ToList().AsReadOnly();
    }
}
