using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MakauTech.Data;
using MakauTech.Hubs;
using MakauTech.Models;

namespace MakauTech.Controllers
{
    /// <summary>Kitchen Rush — free tap-sequence game, separate from Food Blaster.</summary>
    public class KitchenRushController : Controller
    {
        private readonly MakauTechDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public KitchenRushController(MakauTechDbContext context, IHubContext<NotificationHub> hubContext)
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

        public IActionResult Index()
        {
            if (GetCurrentUser() == null) return RedirectToAction("Login", "Home");
            SetViewBag();
            var hasPlayed = HttpContext.Session.GetString("KitchenPlayed");
            ViewBag.FirstGameFree = hasPlayed == null;
            ViewBag.PlayCost = 20;
            return View();
        }

        [HttpPost]
        public IActionResult PayToPlay()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return Json(new { success = false, message = "Not logged in" });
                var hasPlayed = HttpContext.Session.GetString("KitchenPlayed");
                if (hasPlayed == null)
                {
                    HttpContext.Session.SetString("KitchenPlayed", "true");
                    return Json(new { success = true, remainingPoints = user.Points, free = true });
                }
                int cost = 20;
                if (!user.DeductPoints(cost))
                    return Json(new { success = false, message = "Not enough points! Need " + cost + " pts." });
                _context.SaveChanges();
                return Json(new { success = true, remainingPoints = user.Points });
            }
            catch (Exception) { return Json(new { success = false, message = "Error" }); }
        }

        [HttpPost]
        public IActionResult SubmitScore([FromBody] KitchenScoreResult body)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return Json(new { success = false });
                int capped = Math.Min(Math.Max(0, body.Score), 40);
                var previousBadgeCount = user.Badges.Count;
                if (capped > 0) user.AddPoints(capped);
                _context.SaveChanges();
                _ = _hubContext.Clients.All.SendAsync("LeaderboardUpdate", user.Name, user.Points);
                if (user.Badges.Count > previousBadgeCount)
                    _ = _hubContext.Clients.All.SendAsync("ReceiveNotification",
                        $"\ud83c\udfc6 {user.Name} earned '{user.Badges.Last()}'!", "success");
                return Json(new { success = true, pointsEarned = capped, totalPoints = user.Points });
            }
            catch (Exception) { return Json(new { success = false }); }
        }
    }

    public class KitchenScoreResult { public int Score { get; set; } }
}
