using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MakauTech.Data;
using MakauTech.Helpers;
using MakauTech.Models;
using MakauTech.Services;

namespace MakauTech.Controllers
{
    public class HomeController : Controller
    {
        private readonly MakauTechDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _email;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            MakauTechDbContext context,
            IWebHostEnvironment env,
            IEmailService email,
            ILogger<HomeController> logger)
        {
            _context = context;
            _env = env;
            _email = email;
            _logger = logger;
        }

        /// <summary>Cryptographically-strong URL-safe token for email password resets.</summary>
        private static string GenerateSecureToken()
        {
            var bytes = new byte[32]; // 256 bits
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private User? GetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return null;
            return _context.Users.FirstOrDefault(u => u.Id == userId);
        }

        private void QueuePostLoginTutorialIfNeeded(User user)
        {
            if (user is Admin) return;

            TempData["PostLoginPixel"] = true;
            if (!user.UiTutorialSeen)
                HttpContext.Session.SetString("mk-show-tutorial", "pending");
        }

        private string? GetAvatarPublicPath(int userId)
        {
            foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" })
            {
                var full = Path.Combine(_env.WebRootPath, "uploads", "avatars", userId + ext);
                if (System.IO.File.Exists(full))
                    return $"~/uploads/avatars/{userId}{ext}";
            }
            return null;
        }

        private void SetViewBagUser()
        {
            var user = GetCurrentUser();
            ViewBag.IsLoggedIn = user != null;
            ViewBag.UserName = user?.Name ?? "";
            ViewBag.UserPoints = user?.Points ?? 0;
            ViewBag.UserLevel = user?.Level ?? "";
            ViewBag.AvatarUrl = user != null ? GetAvatarPublicPath(user.Id) : null;
        }

        public IActionResult Index()
        {
            try
            {
                SetViewBagUser();
                if (TempData["PostLoginPixel"] != null)
                    ViewBag.PostLoginPixel = true;

                DbSeeder.EnsureMinimumTourismData(_context);
                EnsureWismaSanyan();
                EnsurePlaceLikesTable();
                var allPlaces = _context.Places.Include(p => p.Category).ToList();
                var featured = PickFeaturedPlaces(allPlaces, 3);
                var ids = featured.Select(p => p.Id).ToHashSet();
                Dictionary<int, int> likeCounts;
                try
                {
                    likeCounts = ids.Count == 0
                        ? new Dictionary<int, int>()
                        : _context.PlaceLikes
                            .Where(l => ids.Contains(l.PlaceId))
                            .GroupBy(l => l.PlaceId)
                            .ToDictionary(g => g.Key, g => g.Count());
                }
                catch { likeCounts = new Dictionary<int, int>(); }
                var topThree = _context.Users
                    .Where(u => u.Email != "admin@makautech.com")
                    .OrderByDescending(u => u.Points).Take(3).ToList();

                List<Update> latestUpdates;
                try
                {
                    latestUpdates = _context.Updates
                        .Where(u => u.IsPublished)
                        .OrderByDescending(u => u.CreatedAt)
                        .Take(3)
                        .ToList();
                }
                catch { latestUpdates = new List<Update>(); }
                ViewBag.LatestUpdates = latestUpdates;
                // Promo video embed — SCM Southern Corridor "SIBU, Sarawak - 2024"
                ViewBag.PromoVideoId = "YSJqe9HOa4s";

                return View(new DashboardViewModel
                {
                    FeaturedPlaces = featured,
                    TopThree = topThree,
                    LikeCounts = likeCounts
                });
            }
            catch (Exception)
            {
                ViewBag.Error = "Something went wrong.";
                return View(new DashboardViewModel());
            }
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
            catch { }
        }

        public IActionResult Explore(string? search, int? categoryId)
        {
            try
            {
                SetViewBagUser();
                DbSeeder.EnsureMinimumTourismData(_context);
                EnsureWismaSanyan();
                EnsurePlaceLikesTable();
                if (categoryId.HasValue && categoryId.Value <= 0)
                    categoryId = null;
                var placesQuery = _context.Places.Include(p => p.Category).AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    placesQuery = placesQuery.Where(p =>
                        p.Name.Contains(term)
                        || (p.Description != null && p.Description.Contains(term))
                        || (p.Location != null && p.Location.Contains(term)));
                }
                if (categoryId.HasValue)
                    placesQuery = placesQuery.Where(p => p.CategoryId == categoryId.Value);

                Dictionary<int, int> likeCounts;
                try
                {
                    likeCounts = _context.PlaceLikes
                        .GroupBy(l => l.PlaceId)
                        .ToDictionary(g => g.Key, g => g.Count());
                }
                catch { likeCounts = new Dictionary<int, int>(); }

                var viewModel = new HomeViewModel
                {
                    Places = placesQuery.ToList(),
                    Categories = _context.Categories.ToList(),
                    TopUsers = _context.Users.Where(u => u.Email != "admin@makautech.com").OrderByDescending(u => u.Points).Take(10).ToList(),
                    Achievements = _context.Achievements.ToList(),
                    Reviews = _context.Reviews.ToList(),
                    LikeCounts = likeCounts,
                    SearchQuery = search,
                    SelectedCategory = categoryId
                };
                return View(viewModel);
            }
            catch (Exception)
            {
                ViewBag.Error = "Something went wrong.";
                return View(new HomeViewModel());
            }
        }

        public IActionResult Achievements()
        {
            SetViewBagUser();
            var user = GetCurrentUser();
            ViewBag.UserBadges = user?.Badges ?? new List<string>();
            ViewBag.UserPoints = user?.Points ?? 0;
            ViewBag.UserPlaces = user?.VisitedPlaceIds?.Count ?? 0;
            var achievements = _context.Achievements
                .OrderBy(a => a.PlacesRequired > 0 ? a.PlacesRequired : 9999)
                .ThenBy(a => a.PointsRequired)
                .ToList();
            return View(achievements);
        }

        public IActionResult Leaderboard()
        {
            SetViewBagUser();
            var user = GetCurrentUser();
            ViewBag.IsAdmin = user is Admin;
            ViewBag.CurrentUserId = user?.Id ?? 0;
            var users = _context.Users
                .Where(u => u.Email != "admin@makautech.com")
                .OrderByDescending(u => u.Points)
                .Take(200)
                .ToList()
                .Where(u => u.Points > 0 || u.Badges.Count > 0 || u.VisitedPlaceIds.Count > 0)
                .Take(50)
                .ToList();
            return View(users);
        }

        public IActionResult Games()
        {
            SetViewBagUser();
            return View();
        }

        public IActionResult HowItWorks()
        {
            SetViewBagUser();
            return View();
        }

        private static List<Place> PickFeaturedPlaces(List<Place> all, int count)
        {
            string[] keys = { "Bukit Lima", "Sungai Merah", "Tua Pek" };
            var list = new List<Place>();
            foreach (var key in keys)
            {
                var p = all.FirstOrDefault(x => x.Name.Contains(key, StringComparison.OrdinalIgnoreCase));
                if (p != null && !list.Any(x => x.Id == p.Id))
                    list.Add(p);
            }
            foreach (var p in all.OrderByDescending(x => x.VisitCount))
            {
                if (list.Count >= count) break;
                if (!list.Any(x => x.Id == p.Id))
                    list.Add(p);
            }
            return list.Take(count).ToList();
        }

        private void EnsureWismaSanyan()
        {
            try
            {
                var existing = _context.Places.FirstOrDefault(p => p.Name == "Wisma Sanyan" && p.Location == "Sibu");
                if (existing != null)
                {
                    var url = existing.ImageUrl ?? "";
                    bool needsFix = string.IsNullOrWhiteSpace(url)
                        || url.Contains("placeholder", StringComparison.OrdinalIgnoreCase)
                        || url.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
                        || (!url.Contains("wisma", StringComparison.OrdinalIgnoreCase));
                    if (needsFix)
                    {
                        existing.ImageUrl = "/Images/wisma-sanyan.jpg";
                        _context.SaveChanges();
                    }
                    return;
                }

                var cityCat = _context.Categories.FirstOrDefault(c => c.Name == "City");
                if (cityCat == null)
                {
                    cityCat = new Category { Name = "City", Icon = "🏙️" };
                    _context.Categories.Add(cityCat);
                    _context.SaveChanges();
                }

                _context.Places.Add(new Place
                {
                    Name = "Wisma Sanyan",
                    Location = "Sibu",
                    Description = "A landmark riverside complex in Sibu with offices, shops and views near the Rajang River — a classic city stop for photos and exploring the town centre.",
                    ImageUrl = "/Images/wisma-sanyan.jpg",
                    Rating = 0.0,
                    VisitCount = 0,
                    CategoryId = cityCat.Id
                });
                _context.SaveChanges();
            }
            catch (Exception) { }
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null) return RedirectToAction("Index");
            ViewBag.HideAuthButtons = true;
            ViewBag.IsLoggedIn = false;
            ViewBag.UserCount  = _context.Users.Count();
            ViewBag.PlaceCount = _context.Places.Count();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
        public IActionResult Login(LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Error = "Please enter a valid email and password.";
                    ViewBag.HideAuthButtons = true;
                    ViewBag.IsLoggedIn = false;
                    return View(model);
                }

                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user == null)
                {
                    ViewBag.Error = "Invalid email or password.";
                    ViewBag.HideAuthButtons = true;
                    ViewBag.IsLoggedIn = false;
                    return View(model);
                }

                if (user.IsLockedOut)
                {
                    var mins = (int)Math.Ceiling((user.LockedUntil!.Value - DateTime.UtcNow).TotalMinutes);
                    ViewBag.Error = $"Account locked. Try again in {Math.Max(1, mins)} minute(s).";
                    ViewBag.HideAuthButtons = true;
                    ViewBag.IsLoggedIn = false;
                    return View(model);
                }

                bool passwordValid = VerifyPassword(model.Password, user.Password);
                if (!passwordValid)
                {
                    user.RecordFailedLogin();
                    _context.SaveChanges();
                    ViewBag.Error = "Invalid email or password.";
                    ViewBag.HideAuthButtons = true;
                    ViewBag.IsLoggedIn = false;
                    return View(model);
                }

                user.RecordSuccessfulLogin();
                MigratePasswordIfNeeded(user, model.Password);

                // Auto-accept current Terms version on successful login —
                // user already ticked the agreement checkbox on the login form,
                // so no need to show a second post-login modal.
                const string currentTermsVersionAtLogin = "2026-04";
                if (user.TermsVersionAccepted != currentTermsVersionAtLogin)
                {
                    user.TermsVersionAccepted = currentTermsVersionAtLogin;
                    user.TermsAcceptedAt = DateTime.UtcNow;
                }

                _context.SaveChanges();

                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserName", user.Name);
                QueuePostLoginTutorialIfNeeded(user);
                if (user is Admin) return RedirectToAction("Dashboard", "Admin");
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ViewBag.Error = "Login failed.";
                ViewBag.HideAuthButtons = true;
                ViewBag.IsLoggedIn = false;
                return View(model);
            }
        }

        /// <summary>Verify password against stored hash, with fallback for pre-hash plaintext passwords.</summary>
        private static bool VerifyPassword(string input, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            if (stored.StartsWith("$2"))
                return BCrypt.Net.BCrypt.Verify(input, stored);
            return stored == input;
        }

        /// <summary>Auto-upgrade plaintext passwords to BCrypt on successful login.</summary>
        private void MigratePasswordIfNeeded(User user, string plaintext)
        {
            if (!user.Password.StartsWith("$2"))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(plaintext, workFactor: 12);
            }
        }

        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null) return RedirectToAction("Index");
            ViewBag.HideAuthButtons = true;
            ViewBag.IsLoggedIn = false;
            ViewBag.UserCount  = _context.Users.Count();
            ViewBag.PlaceCount = _context.Places.Count();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Error = "Please fix the errors below.";
                    ViewBag.HideAuthButtons = true;
                    ViewBag.IsLoggedIn = false;
                    return View(model);
                }

                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ViewBag.Error = "Email already registered.";
                    ViewBag.HideAuthButtons = true;
                    ViewBag.IsLoggedIn = false;
                    return View(model);
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 12);
                // User already ticked the Terms & Privacy checkbox on the register form,
                // so we record acceptance directly — no second modal needed after sign-in.
                const string currentTermsVersionAtRegister = "2026-04";
                var user = new User
                {
                    Name = model.Name.Trim(),
                    Email = model.Email.Trim().ToLowerInvariant(),
                    Password = hashedPassword,
                    TermsVersionAccepted = currentTermsVersionAtRegister,
                    TermsAcceptedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                _context.SaveChanges();

                // Fire-and-forget welcome email — never block the registration UX
                // if the email provider is slow or rate-limited.
                _ = _email.SendWelcomeEmailAsync(user)
                    .ContinueWith(t =>
                    {
                        if (t.Exception != null)
                            _logger.LogWarning(t.Exception, "Welcome email background task failed for {Email}", user.Email);
                    }, TaskScheduler.Default);

                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserName", user.Name);
                return RedirectToAction("Onboarding");
            }
            catch (Exception)
            {
                ViewBag.Error = "Registration failed.";
                ViewBag.HideAuthButtons = true;
                ViewBag.IsLoggedIn = false;
                return View(model);
            }
        }

        // ─── Forgot / Reset password flow ──────────────────────────────────

        public IActionResult ForgotPassword()
        {
            if (HttpContext.Session.GetInt32("UserId") != null) return RedirectToAction("Index");
            ViewBag.HideAuthButtons = true;
            ViewBag.IsLoggedIn = false;
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            ViewBag.HideAuthButtons = true;
            ViewBag.IsLoggedIn = false;

            if (!ModelState.IsValid) return View(model);

            try
            {
                var email = model.Email.Trim().ToLowerInvariant();
                var user = _context.Users.FirstOrDefault(u => u.Email == email);

                // Generate token + send email only when user exists.
                // BUT we always show the same confirmation page to prevent
                // attackers from enumerating which emails are registered.
                if (user != null)
                {
                    var token = GenerateSecureToken();
                    user.PasswordResetToken = token;
                    user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1);
                    _context.SaveChanges();

                    var resetLink = Url.Action(
                        "ResetPassword", "Home",
                        new { email = user.Email, token = token },
                        protocol: Request.Scheme,
                        host: Request.Host.Value)!;

                    _ = _email.SendPasswordResetEmailAsync(user, resetLink)
                        .ContinueWith(t =>
                        {
                            if (t.Exception != null)
                                _logger.LogWarning(t.Exception, "Password reset email failed for {Email}", user.Email);
                        }, TaskScheduler.Default);
                }
                else
                {
                    _logger.LogInformation("Password reset requested for unknown email {Email}", email);
                }

                ViewBag.Sent = true;
                return View(new ForgotPasswordViewModel { Email = model.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ForgotPassword failed for {Email}", model.Email);
                ViewBag.Error = "Could not process the request. Please try again later.";
                return View(model);
            }
        }

        public IActionResult ResetPassword(string? email, string? token)
        {
            if (HttpContext.Session.GetInt32("UserId") != null) return RedirectToAction("Index");
            ViewBag.HideAuthButtons = true;
            ViewBag.IsLoggedIn = false;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Error = "Invalid reset link.";
                return View(new ResetPasswordViewModel());
            }

            var normalised = email.Trim().ToLowerInvariant();
            var user = _context.Users.FirstOrDefault(u => u.Email == normalised);
            if (user == null
                || string.IsNullOrEmpty(user.PasswordResetToken)
                || !CryptographicEquals(user.PasswordResetToken, token)
                || user.PasswordResetTokenExpires == null
                || user.PasswordResetTokenExpires.Value < DateTime.UtcNow)
            {
                ViewBag.Error = "This reset link is invalid or has expired. Please request a new one.";
                return View(new ResetPasswordViewModel());
            }

            return View(new ResetPasswordViewModel { Email = normalised, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            ViewBag.HideAuthButtons = true;
            ViewBag.IsLoggedIn = false;

            if (!ModelState.IsValid) return View(model);

            try
            {
                var normalised = model.Email.Trim().ToLowerInvariant();
                var user = _context.Users.FirstOrDefault(u => u.Email == normalised);
                if (user == null
                    || string.IsNullOrEmpty(user.PasswordResetToken)
                    || !CryptographicEquals(user.PasswordResetToken, model.Token)
                    || user.PasswordResetTokenExpires == null
                    || user.PasswordResetTokenExpires.Value < DateTime.UtcNow)
                {
                    ViewBag.Error = "This reset link is invalid or has expired. Please request a new one.";
                    return View(model);
                }

                // Hash + persist new password, invalidate token, reset lockout state.
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword, workFactor: 12);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpires = null;
                user.RecordSuccessfulLogin(); // clears FailedLoginAttempts + LockedUntil
                _context.SaveChanges();

                TempData["LoginNotice"] = "Your password has been reset. Please sign in with the new password.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetPassword failed for {Email}", model.Email);
                ViewBag.Error = "Could not reset the password. Please try again.";
                return View(model);
            }
        }

        /// <summary>Constant-time string comparison to prevent timing-attack token leaks.</summary>
        private static bool CryptographicEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        public IActionResult Onboarding()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login");
            if (user.IsOnboarded) return RedirectToAction("Index");
            ViewBag.HideAuthButtons = true;
            ViewBag.IsLoggedIn = false;
            ViewBag.UserName = user.Name;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Onboarding(string travelType, List<string> interests)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login");
            user.TravelType = travelType ?? string.Empty;
            user.Interests = interests ?? new List<string>();
            user.IsOnboarded = true;
            _context.SaveChanges();
            QueuePostLoginTutorialIfNeeded(user);
            return RedirectToAction("Index");
        }

        /// <summary>Marks UI walkthrough completed (persisted). Called from wizard JS.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DismissTutorial()
        {
            var user = GetCurrentUser();
            if (user != null && !user.UiTutorialSeen)
            {
                user.UiTutorialSeen = true;
                _context.SaveChanges();
            }
            HttpContext.Session.Remove("mk-show-tutorial");
            return Json(new { ok = true });
        }

        /// <summary>Accepts the platform Terms & Privacy agreement (required after login).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptAgreement()
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            const string currentTermsVersion = "2026-04";
            if (user.TermsVersionAccepted != currentTermsVersion)
            {
                user.TermsVersionAccepted = currentTermsVersion;
                user.TermsAcceptedAt = DateTime.UtcNow;
                _context.SaveChanges();
            }
            return Json(new { ok = true, version = currentTermsVersion });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        public IActionResult Profile()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return RedirectToAction("Login");
                SetViewBagUser();
                var allUsers = _context.Users.OrderByDescending(u => u.Points).ToList();
                var rank = allUsers.FindIndex(u => u.Id == user.Id) + 1;
                var viewModel = new ProfileViewModel
                {
                    User = user,
                    AllAchievements = _context.Achievements.ToList(),
                    UserReviews = _context.Reviews.Include(r => r.Place).Where(r => r.UserId == user.Id).ToList(),
                    Rank = rank
                };
                return View(viewModel);
            }
            catch (Exception) { return RedirectToAction("Index"); }
        }

        public IActionResult EditProfile()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login");
            SetViewBagUser();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(string name, string email)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return RedirectToAction("Login");
                if (!string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2)
                    user.Name = name.Trim();
                if (!string.IsNullOrWhiteSpace(email) && email.Contains('@'))
                    user.Email = email.Trim().ToLowerInvariant();
                _context.SaveChanges();
                HttpContext.Session.SetString("UserName", user.Name);
                return RedirectToAction("Profile");
            }
            catch (Exception) { return RedirectToAction("EditProfile"); }
        }

        public IActionResult About()
        {
            SetViewBagUser();
            return View();
        }

        // Academic showcase page — ERD, PK/FK, inheritance, polymorphism for lecturer review.
        public IActionResult Architecture()
        {
            SetViewBagUser();
            return View();
        }

        // Public daily updates feed — visible to everyone.
        public IActionResult Updates()
        {
            SetViewBagUser();
            var list = _context.Updates
                .Where(u => u.IsPublished)
                .OrderByDescending(u => u.CreatedAt)
                .ToList();
            return View(list);
        }

        public IActionResult UpdateDetail(int id)
        {
            SetViewBagUser();
            var u = _context.Updates.FirstOrDefault(x => x.Id == id && x.IsPublished);
            if (u == null) return RedirectToAction("Updates");
            return View(u);
        }

        // Sibu events calendar — public, hard-coded curated list of festivals and notable dates.
        public IActionResult Calendar(int? month, int? year)
        {
            SetViewBagUser();
            var today = DateTime.Today;
            var m = month ?? today.Month;
            var y = year ?? today.Year;
            ViewBag.CurrentMonth = m;
            ViewBag.CurrentYear  = y;
            return View(SibuEvents.GetAll());
        }

        public IActionResult Terms()
        {
            SetViewBagUser();
            return View();
        }

        public IActionResult Privacy()
        {
            SetViewBagUser();
            return View();
        }

        public IActionResult Map()
        {
            SetViewBagUser();
            var places = _context.Places.Include(p => p.Category).ToList();
            return View(places);
        }

        public IActionResult Settings()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login");
            SetViewBagUser();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Settings(string name, string email)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return RedirectToAction("Login");
                if (!string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2)
                    user.Name = name.Trim();
                if (!string.IsNullOrWhiteSpace(email) && email.Contains('@'))
                    user.Email = email.Trim().ToLowerInvariant();
                _context.SaveChanges();
                HttpContext.Session.SetString("UserName", user.Name);
                TempData["SettingsSuccess"] = "Settings saved successfully!";
                return RedirectToAction("Settings");
            }
            catch (Exception) { return RedirectToAction("Settings"); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(3_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 3_000_000)]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatar)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login");
            if (avatar == null || avatar.Length == 0)
            {
                TempData["SettingsError"] = "Please choose an image file.";
                return RedirectToAction("Settings");
            }
            if (avatar.Length > 2 * 1024 * 1024)
            {
                TempData["SettingsError"] = "Image must be 2 MB or smaller.";
                return RedirectToAction("Settings");
            }
            var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (string.IsNullOrEmpty(avatar.ContentType) || !allowed.Contains(avatar.ContentType))
            {
                TempData["SettingsError"] = "Use JPG, PNG, WebP, or GIF only.";
                return RedirectToAction("Settings");
            }
            var ext = avatar.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".jpg"
            };
            var dir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(dir);
            foreach (var e in new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" })
            {
                var oldPath = Path.Combine(dir, user.Id + e);
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }
            var dest = Path.Combine(dir, user.Id + ext);
            await using (var fs = new FileStream(dest, FileMode.Create))
                await avatar.CopyToAsync(fs);
            TempData["SettingsSuccess"] = "Profile picture updated!";
            return RedirectToAction("Settings");
        }

    }
}
