// OOP CONCEPTS DEMONSTRATED:
// Abstract class: FoodItem cannot be instantiated directly
// Inheritance: CommonFood, UncommonFood, GoldenTrophy, HazardFood all inherit FoodItem
// Polymorphism: GetFoodDescription(), GetSpawnChance(), GetCatchMessage() behave differently per class
// Encapsulation: PointValue has private setter — outside code cannot change it directly
// Factory pattern: FoodCatchController decides which FoodItem subclass to spawn

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MakauTech.Data;
using MakauTech.Hubs;
using MakauTech.Models;

namespace MakauTech.Controllers
{
    public class FoodCatchController : Controller
    {
        private readonly MakauTechDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public FoodCatchController(MakauTechDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private User? GetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return null;
            return _context.Users.FirstOrDefault(u => u.Id == userId);
        }

        private void SetViewBag()
        {
            var user = GetCurrentUser();
            ViewBag.IsLoggedIn = user != null;
            ViewBag.UserName = user?.Name ?? "";
            ViewBag.UserPoints = user?.Points ?? 0;
            ViewBag.UserLevel = user?.Level ?? "";
        }

        // GET: /FoodCatch
        public IActionResult Index()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Home");
            SetViewBag();
            ViewBag.PlayCost = 20;
            // First game free: check session flag
            var hasPlayed = HttpContext.Session.GetString("FoodCatchPlayed");
            ViewBag.FirstGameFree = hasPlayed == null;
            return View();
        }

        // POST: /FoodCatch/PayToPlay — deducts 5 pts per round (first game is free)
        [HttpPost]
        public IActionResult PayToPlay()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return Json(new { success = false, message = "Not logged in" });

                // First game free — check session flag
                var hasPlayed = HttpContext.Session.GetString("FoodCatchPlayed");
                if (hasPlayed == null)
                {
                    HttpContext.Session.SetString("FoodCatchPlayed", "true");
                    return Json(new { success = true, remainingPoints = user.Points, free = true });
                }

                int cost = 20;
                if (!user.DeductPoints(cost))
                    return Json(new { success = false, message = "Not enough points! You need " + cost + " pts." });

                _context.SaveChanges();
                return Json(new { success = true, remainingPoints = user.Points });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error processing payment" });
            }
        }

        // POST: /FoodCatch/SubmitScore
        [HttpPost]
        public IActionResult SubmitScore([FromBody] FoodCatchScore result)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return Json(new { success = false });

                int capped = Math.Min(Math.Max(0, result.Score), 50);
                var previousBadgeCount = user.Badges.Count;
                if (capped > 0) user.AddPoints(capped);
                _context.SaveChanges();

                // Realtime: broadcast score update
                _ = _hubContext.Clients.All.SendAsync("LeaderboardUpdate", user.Name, user.Points);
                if (user.Badges.Count > previousBadgeCount)
                {
                    _ = _hubContext.Clients.All.SendAsync("ReceiveNotification",
                        $"\ud83c\udfc6 {user.Name} earned '{user.Badges.Last()}'!", "success");
                }

                return Json(new
                {
                    success = true,
                    pointsEarned = capped,
                    totalPoints = user.Points
                });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }
    }

    public class FoodCatchScore
    {
        public int Score { get; set; }
    }
}
