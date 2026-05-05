using Microsoft.AspNetCore.Mvc;
using MakauTech.Data;
using MakauTech.Models;

namespace MakauTech.Controllers
{
    public class MemoryController : Controller
    {
        private readonly MakauTechDbContext _context;

        public MemoryController(MakauTechDbContext context)
        {
            _context = context;
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
            var hasPlayed = HttpContext.Session.GetString("MemoryPlayed");
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
                var hasPlayed = HttpContext.Session.GetString("MemoryPlayed");
                if (hasPlayed == null)
                {
                    HttpContext.Session.SetString("MemoryPlayed", "true");
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
        public IActionResult SubmitScore([FromBody] MemoryScoreResult body)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return Json(new { success = false });
                int capped = Math.Min(Math.Max(0, body.Score), 30);
                if (capped > 0) user.AddPoints(capped);
                _context.SaveChanges();
                return Json(new { success = true, pointsEarned = capped, totalPoints = user.Points });
            }
            catch (Exception) { return Json(new { success = false }); }
        }
    }

    public class MemoryScoreResult { public int Score { get; set; } }
}
