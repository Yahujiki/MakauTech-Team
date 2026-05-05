using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MakauTech.Data;
using MakauTech.Models;

namespace MakauTech.Controllers
{
    public class AdminController : Controller
    {
        private readonly MakauTechDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(MakauTechDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private bool IsAdmin()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return false;
            var user = _context.Users.Find(userId);
            return user is Admin;
        }

        private void SetViewBag()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var user = userId != null ? _context.Users.Find(userId) : null;
            ViewBag.IsLoggedIn = user != null;
            ViewBag.UserName = user?.Name ?? "";
            ViewBag.UserPoints = user?.Points ?? 0;
            ViewBag.UserLevel = user?.Level ?? "";
        }

        // GET: /Admin/Dashboard
        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetViewBag();
            ViewBag.TotalPlaces = _context.Places.Count();
            ViewBag.TotalUsers = _context.Users.Count(u => u.Email != "admin@makautech.com");
            ViewBag.TotalReviews = _context.Reviews.Count();
            ViewBag.TotalCategories = _context.Categories.Count();
            ViewBag.TotalAchievements = _context.Achievements.Count();
            try { ViewBag.TotalLikes = _context.PlaceLikes.Count(); } catch { ViewBag.TotalLikes = 0; }
            ViewBag.TopPlaces = _context.Places.OrderByDescending(p => p.VisitCount).Take(5).ToList();
            ViewBag.RecentReviews = _context.Reviews.Include(r => r.Place).OrderByDescending(r => r.CreatedAt).Take(5).ToList();
            ViewBag.TopUsers = _context.Users.Where(u => u.Email != "admin@makautech.com").OrderByDescending(u => u.Points).Take(5).ToList();
            return View();
        }

        // ── PLACES ──────────────────────────────────────────
        public IActionResult Places()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetViewBag();
            var places = _context.Places.Include(p => p.Category).ToList();
            return View(places);
        }

        public IActionResult CreatePlace()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetViewBag();
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePlace(string name, string location, string description, string imageUrl, double rating, int categoryId)
        {
            try
            {
                if (!IsAdmin()) return RedirectToAction("Login", "Home");
                _context.Places.Add(new Place { Name = name, Location = location, Description = description, ImageUrl = imageUrl, Rating = rating, CategoryId = categoryId });
                _context.SaveChanges();
                return RedirectToAction("Places");
            }
            catch (Exception) { return RedirectToAction("Places"); }
        }

        public IActionResult EditPlace(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetViewBag();
            var place = _context.Places.Find(id);
            if (place == null) return NotFound();
            ViewBag.Categories = _context.Categories.ToList();
            return View(place);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPlace(int id, string name, string location, string description, string imageUrl, double rating, int categoryId)
        {
            try
            {
                if (!IsAdmin()) return RedirectToAction("Login", "Home");
                var place = _context.Places.Find(id);
                if (place == null) return NotFound();
                place.Name = name; place.Location = location; place.Description = description;
                place.ImageUrl = imageUrl; place.Rating = rating; place.CategoryId = categoryId;
                _context.SaveChanges();
                return RedirectToAction("Places");
            }
            catch (Exception) { return RedirectToAction("Places"); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePlace(int id)
        {
            try
            {
                if (!IsAdmin()) return RedirectToAction("Login", "Home");
                var place = _context.Places.Find(id);
                if (place != null) { _context.Places.Remove(place); _context.SaveChanges(); }
                return RedirectToAction("Places");
            }
            catch (Exception) { return RedirectToAction("Places"); }
        }

        // ── CATEGORIES ──────────────────────────────────────
        public IActionResult Categories()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetViewBag();
            return View(_context.Categories.ToList());
        }

        public IActionResult CreateCategory()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetViewBag();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCategory(string name, string icon)
        {
            try
            {
                if (!IsAdmin()) return RedirectToAction("Login", "Home");
                _context.Categories.Add(new Category { Name = name, Icon = icon });
                _context.SaveChanges();
                return RedirectToAction("Categories");
            }
            catch (Exception) { return RedirectToAction("Categories"); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategory(int id)
        {
            try
            {
                if (!IsAdmin()) return RedirectToAction("Login", "Home");
                var cat = _context.Categories.Find(id);
                if (cat != null) { _context.Categories.Remove(cat); _context.SaveChanges(); }
                return RedirectToAction("Categories");
            }
            catch (Exception) { return RedirectToAction("Categories"); }
        }

        // ── REVIEWS ─────────────────────────────────────────
        public IActionResult Reviews()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetViewBag();
            var reviews = _context.Reviews.Include(r => r.Place).ToList();
            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteReview(int id)
        {
            try
            {
                if (!IsAdmin()) return RedirectToAction("Login", "Home");
                var review = _context.Reviews.Find(id);
                if (review != null) { _context.Reviews.Remove(review); _context.SaveChanges(); }
                return RedirectToAction("Reviews");
            }
            catch (Exception) { return RedirectToAction("Reviews"); }
        }

        // ── USERS ────────────────────────────────────────────
        public IActionResult Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetViewBag();
            return View(_context.Users.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                if (!IsAdmin()) return RedirectToAction("Login", "Home");
                var user = _context.Users.Find(id);
                if (user != null) { _context.Users.Remove(user); _context.SaveChanges(); }
                return RedirectToAction("Users");
            }
            catch (Exception) { return RedirectToAction("Users"); }
        }

        // ── ACHIEVEMENTS ─────────────────────────────────────
        public IActionResult Achievements()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetViewBag();
            return View(_context.Achievements.ToList());
        }

        public IActionResult CreateAchievement()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetViewBag();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAchievement(string name, string description, string icon, int pointsRequired, int placesRequired)
        {
            try
            {
                if (!IsAdmin()) return RedirectToAction("Login", "Home");
                _context.Achievements.Add(new Achievement { Name = name, Description = description, Icon = icon, PointsRequired = pointsRequired, PlacesRequired = placesRequired });
                _context.SaveChanges();
                return RedirectToAction("Achievements");
            }
            catch (Exception) { return RedirectToAction("Achievements"); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAchievement(int id)
        {
            try
            {
                if (!IsAdmin()) return RedirectToAction("Login", "Home");
                var a = _context.Achievements.Find(id);
                if (a != null) { _context.Achievements.Remove(a); _context.SaveChanges(); }
                return RedirectToAction("Achievements");
            }
            catch (Exception) { return RedirectToAction("Achievements"); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetLeaderboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            MakauTech.Data.DbSeeder.AdminResetLeaderboard(_context);
            TempData["AdminSuccess"] = "Leaderboard reset — all user points, badges and visits cleared.";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FactoryReset(string? confirmText)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            // Require user to type "RESET" exactly — second confirmation barrier.
            if (!string.Equals(confirmText, "RESET", StringComparison.Ordinal))
            {
                TempData["AdminError"] = "Factory reset cancelled — confirmation phrase did not match.";
                return RedirectToAction("Dashboard");
            }

            using var tx = _context.Database.BeginTransaction();
            try
            {
                // MySQL syntax with backticks. All-or-nothing via transaction.
                _context.Database.ExecuteSqlRaw("DELETE FROM `Reviews`");
                _context.Database.ExecuteSqlRaw("DELETE FROM `PlaceLikes`");
                _context.Database.ExecuteSqlRaw("DELETE FROM `Feedbacks`");
                _context.Database.ExecuteSqlRaw("DELETE FROM `Updates`");
                _context.Database.ExecuteSqlRaw("DELETE FROM `Users` WHERE `Discriminator` != 'Admin'");
                _context.Database.ExecuteSqlRaw("UPDATE `Places` SET `VisitCount` = 0, `Rating` = 0");

                _context.SaveChanges();
                tx.Commit();

                TempData["AdminSuccess"] = "Factory reset complete — all user data wiped. Admin account preserved.";
            }
            catch (Exception ex)
            {
                tx.Rollback();
                _logger.LogError(ex, "Factory reset failed; transaction rolled back");
                TempData["AdminError"] = "Factory reset failed. Database rolled back to previous state. Check server logs.";
            }
            return RedirectToAction("Dashboard");
        }

        // ============ UPDATES (public daily news) ============
        public IActionResult Updates()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetViewBag();
            var list = _context.Updates.OrderByDescending(u => u.CreatedAt).ToList();
            return View(list);
        }

        public IActionResult CreateUpdate()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetViewBag();
            return View(new Update());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUpdate(Update model, IFormFile? image)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Body))
            {
                TempData["AdminError"] = "Title and body are required.";
                SetViewBag();
                return View(model);
            }

            // Image upload (optional, validated like other uploads)
            if (image != null && image.Length > 0)
            {
                if (image.Length > 4 * 1024 * 1024)
                {
                    TempData["AdminError"] = "Image too large. Max 4 MB.";
                    SetViewBag();
                    return View(model);
                }
                var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                if (!allowed.Contains(ext))
                {
                    TempData["AdminError"] = "Image must be jpg/png/webp/gif.";
                    SetViewBag();
                    return View(model);
                }
                try
                {
                    var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "updates");
                    Directory.CreateDirectory(dir);
                    var fileName = $"upd_{Guid.NewGuid():N}{ext}";
                    using var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
                    image.CopyTo(fs);
                    model.ImageUrl = $"/uploads/updates/{fileName}";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save update image");
                }
            }

            var admin = _context.Users.Find(HttpContext.Session.GetInt32("UserId")) as Admin;
            model.AuthorName = admin?.Name ?? "Admin";
            model.CreatedAt = DateTime.UtcNow;

            _context.Updates.Add(model);
            _context.SaveChanges();
            TempData["AdminSuccess"] = "Update published.";
            return RedirectToAction("Updates");
        }

        public IActionResult EditUpdate(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var u = _context.Updates.Find(id);
            if (u == null) return RedirectToAction("Updates");
            SetViewBag();
            return View(u);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUpdate(int id, Update model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var existing = _context.Updates.Find(id);
            if (existing == null) return RedirectToAction("Updates");

            existing.Title = model.Title?.Trim() ?? "";
            existing.Summary = model.Summary?.Trim() ?? "";
            existing.Body = model.Body ?? "";
            existing.IsPublished = model.IsPublished;
            _context.SaveChanges();
            TempData["AdminSuccess"] = "Update edited.";
            return RedirectToAction("Updates");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUpdate(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var u = _context.Updates.Find(id);
            if (u != null)
            {
                _context.Updates.Remove(u);
                _context.SaveChanges();
                TempData["AdminSuccess"] = "Update deleted.";
            }
            return RedirectToAction("Updates");
        }
    }
}