using System.ComponentModel.DataAnnotations;

namespace MakauTech.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "Password must be 6–128 characters.")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2–100 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-_.]+$", ErrorMessage = "Name contains invalid characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "Password must be 6–128 characters.")]
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "Password must be 6–128 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class HomeViewModel
    {
        public List<Place> Places { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<User> TopUsers { get; set; } = new();
        public List<Achievement> Achievements { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();
        public Dictionary<int, int> LikeCounts { get; set; } = new();
        public string? SearchQuery { get; set; }
        public int? SelectedCategory { get; set; }
    }

    /// <summary>Minimal home dashboard: featured places + optional top-3 snippet.</summary>
    public class DashboardViewModel
    {
        public List<Place> FeaturedPlaces { get; set; } = new();
        public List<User> TopThree { get; set; } = new();
        public Dictionary<int, int> LikeCounts { get; set; } = new();
    }
    public class ProfileViewModel
    {
        public User User { get; set; } = null!;
        public List<Achievement> AllAchievements { get; set; } = new();
        public List<Review> UserReviews { get; set; } = new();
        public int Rank { get; set; }
    }

    public class PlaceDetailViewModel
    {
        public Place Place { get; set; } = null!;
        public List<Review> Reviews { get; set; } = new();
        public bool AlreadyVisited { get; set; }
        public bool IsLoggedIn { get; set; }
        public bool UserHasLiked { get; set; }
        public int LikeCount { get; set; }
    }
}