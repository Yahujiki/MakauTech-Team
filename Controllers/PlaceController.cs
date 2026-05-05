using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MakauTech.Data;
using MakauTech.Hubs;
using MakauTech.Models;

namespace MakauTech.Controllers
{
    public class PlaceController : Controller
    {
        private readonly MakauTechDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<NotificationHub> _hubContext;

        public PlaceController(MakauTechDbContext context, IWebHostEnvironment env, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _env = env;
            _hubContext = hubContext;
        }

        private void EnsureReviewImageColumn()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(
                    "ALTER TABLE Reviews ADD COLUMN ImageUrl TEXT NULL;");
            }
            catch { /* column already exists — SQLite throws on duplicate ALTER */ }
        }

        private User? GetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return null;
            return _context.Users.FirstOrDefault(u => u.Id == userId);
        }

        private void SetViewBagUser()
        {
            var user = GetCurrentUser();
            ViewBag.IsLoggedIn = user != null;
            ViewBag.UserName = user?.Name ?? "";
            ViewBag.UserPoints = user?.Points ?? 0;
            ViewBag.UserLevel = user?.Level ?? "";
        }


        private void EnsurePlaceLikesTable()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS `PlaceLikes` (
  `Id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `UserId` INT NOT NULL,
  `PlaceId` INT NOT NULL,
  `CreatedAt` DATETIME NOT NULL,
  UNIQUE KEY `IX_PlaceLikes_UserId_PlaceId` (`UserId`, `PlaceId`),
  KEY `IX_PlaceLikes_PlaceId` (`PlaceId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
            }
            catch (Exception)
            {
                // If DB is read-only or locked, likes will just be unavailable.
            }
        }

        public IActionResult Detail(int id)
        {
            try
            {
                SetViewBagUser();
                var place = _context.Places.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
                if (place == null) return NotFound();

                var user = GetCurrentUser();
                bool alreadyVisited = false;
                if (user != null)
                {
                    bool isNewVisit = user.RecordVisit(id);
                    if (isNewVisit) { place.VisitCount++; _context.SaveChanges(); SetViewBagUser(); }
                    alreadyVisited = !isNewVisit;
                }

                var reviews = _context.Reviews.Where(r => r.PlaceId == id).OrderByDescending(r => r.CreatedAt).ToList();
                EnsurePlaceLikesTable();
                var likeCount = _context.PlaceLikes.Count(l => l.PlaceId == id);
                var userHasLiked = user != null && _context.PlaceLikes.Any(l => l.PlaceId == id && l.UserId == user.Id);
                return View(new PlaceDetailViewModel
                {
                    Place = place,
                    Reviews = reviews,
                    AlreadyVisited = alreadyVisited,
                    IsLoggedIn = user != null,
                    UserHasLiked = userHasLiked,
                    LikeCount = likeCount
                });
            }
            catch (Exception) { return RedirectToAction("Index", "Home"); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("review")]
        [RequestSizeLimit(8_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 8_000_000)]
        public async Task<IActionResult> AddReview(int placeId, int rating, string comment, IFormFile? reviewPhoto)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return RedirectToAction("Login", "Home");
                var trimmed = (comment ?? "").Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    TempData["ReviewError"] = "Please write a comment — share something real about your visit!";
                    return RedirectToAction("Detail", new { id = placeId });
                }

                // Handle optional photo upload
                string? photoUrl = null;
                if (reviewPhoto != null && reviewPhoto.Length > 0)
                {
                    var ext = Path.GetExtension(reviewPhoto.FileName).ToLowerInvariant();
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                    if (reviewPhoto.Length > 5 * 1024 * 1024)
                    {
                        TempData["ReviewError"] = "Photo must be 5 MB or smaller.";
                        return RedirectToAction("Detail", new { id = placeId });
                    }
                    if (!allowed.Contains(ext))
                    {
                        TempData["ReviewError"] = "Only JPG, PNG, WebP or GIF photos allowed.";
                        return RedirectToAction("Detail", new { id = placeId });
                    }
                    var dir = Path.Combine(_env.WebRootPath, "uploads", "reviews");
                    Directory.CreateDirectory(dir);
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(dir, fileName);
                    await using (var fs = System.IO.File.Create(filePath))
                        await reviewPhoto.CopyToAsync(fs);
                    photoUrl = $"/uploads/reviews/{fileName}";
                }

                EnsureReviewImageColumn();
                _context.Reviews.Add(new Review
                {
                    UserId = user.Id, PlaceId = placeId, UserName = user.Name,
                    Rating = rating, Comment = trimmed, ImageUrl = photoUrl
                });

                EnsurePlaceLikesTable();
                bool liked = _context.PlaceLikes.Any(l => l.UserId == user.Id && l.PlaceId == placeId);
                if (liked)
                    user.AddPoints(10);
                else
                    TempData["ReviewNotice"] = "Review saved! Tap ❤️ Like first next time to earn 10 pts with your comment.";

                var previousBadgeCount = user.Badges.Count;
                var place = _context.Places.Find(placeId);
                if (place != null)
                {
                    var ratings = _context.Reviews.Where(r => r.PlaceId == placeId).Select(r => r.Rating).ToList();
                    ratings.Add(rating);
                    place.Rating = ratings.Average();
                }
                _context.SaveChanges();

                // Realtime: broadcast review notification to all users
                var placeName = place?.Name ?? "a place";
                _ = _hubContext.Clients.All.SendAsync("ReceiveNotification",
                    $"\ud83d\udcdd {user.Name} just reviewed {placeName}!", "info");

                // Realtime: broadcast if user earned a new badge
                if (user.Badges.Count > previousBadgeCount)
                {
                    var newBadge = user.Badges.Last();
                    _ = _hubContext.Clients.All.SendAsync("ReceiveNotification",
                        $"\ud83c\udfc6 {user.Name} earned the '{newBadge}' badge!", "success");
                }

                // Realtime: update leaderboard
                _ = _hubContext.Clients.All.SendAsync("LeaderboardUpdate", user.Name, user.Points);

                return RedirectToAction("Detail", new { id = placeId });
            }
            catch (Exception) { return RedirectToAction("Detail", new { id = placeId }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleLike(int placeId)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Home");
            EnsurePlaceLikesTable();
            var existing = _context.PlaceLikes.FirstOrDefault(l => l.UserId == user.Id && l.PlaceId == placeId);
            if (existing != null)
                _context.PlaceLikes.Remove(existing);
            else
                _context.PlaceLikes.Add(new PlaceLike { UserId = user.Id, PlaceId = placeId });
            _context.SaveChanges();
            return RedirectToAction("Detail", new { id = placeId });
        }

        [HttpGet]
        public IActionResult EditReview(int id)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return RedirectToAction("Login", "Home");
                SetViewBagUser();
                var review = _context.Reviews.Find(id);
                if (review == null || review.UserId != user.Id) return Forbid();
                return View(review);
            }
            catch (Exception) { return RedirectToAction("Index", "Home"); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditReview(int id, int rating, string comment)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return RedirectToAction("Login", "Home");
                var review = _context.Reviews.Find(id);
                if (review == null || review.UserId != user.Id) return Forbid();
                review.Rating = rating;
                review.Comment = comment;
                _context.SaveChanges();
                return RedirectToAction("Detail", new { id = review.PlaceId });
            }
            catch (Exception) { return RedirectToAction("Index", "Home"); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteReview(int id)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return RedirectToAction("Login", "Home");
                var review = _context.Reviews.Find(id);
                if (review == null || review.UserId != user.Id) return Forbid();
                int placeId = review.PlaceId;
                _context.Reviews.Remove(review);
                _context.SaveChanges();
                return RedirectToAction("Detail", new { id = placeId });
            }
            catch (Exception) { return RedirectToAction("Index", "Home"); }
        }
    }
}