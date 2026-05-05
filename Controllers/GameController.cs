using Microsoft.AspNetCore.Mvc;
using MakauTech.Data;
using MakauTech.Models;

namespace MakauTech.Controllers
{
    public class GameController : Controller
    {
        private readonly MakauTechDbContext _context;

        public GameController(MakauTechDbContext context)
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

        // GET: /Game
        public IActionResult Index()
        {
            SetViewBag();
            if (GetCurrentUser() == null) return RedirectToAction("Login", "Home");
            return View();
        }

        // GET: /Game/GetQuestions — returns JSON questions
        [HttpGet]
        public IActionResult GetQuestions()
        {
            try
            {
                var places = _context.Places.ToList();
                var categories = _context.Categories.ToList();
                var questions = new List<object>();
                var rng = new Random();

                foreach (var place in places.OrderBy(x => rng.Next()).Take(10))
                {
                    var cat = categories.FirstOrDefault(c => c.Id == place.CategoryId);
                    int qType = rng.Next(4);

                    if (qType == 0)
                    {
                        // Category question
                        var wrongCats = categories.Where(c => c.Id != place.CategoryId).OrderBy(x => rng.Next()).Take(3).Select(c => c.Name).ToList();
                        var options = wrongCats.Append(cat?.Name ?? "Nature").OrderBy(x => rng.Next()).ToList();
                        var hintDesc = place.Description ?? "";
                        questions.Add(new { question = $"What category is '{place.Name}'?", options, answer = cat?.Name ?? "Nature", hint = hintDesc.Length > 0 ? hintDesc[..Math.Min(50, hintDesc.Length)] + "..." : "" });
                    }
                    else if (qType == 1)
                    {
                        // Location question
                        var options = new List<string> { "Sibu", "Kuching", "Miri", "Bintulu" }.OrderBy(x => rng.Next()).ToList();
                        questions.Add(new { question = $"Where is '{place.Name}' located?", options, answer = place.Location, hint = $"It is a {cat?.Name ?? "place"} attraction." });
                    }
                    else if (qType == 2)
                    {
                        // Name from description
                        var desc = place.Description ?? "";
                        if (desc.Length < 20) continue; // Need a meaningful description for this question type
                        var wrongPlaces = places.Where(p => p.Id != place.Id).OrderBy(x => rng.Next()).Take(3).Select(p => p.Name).ToList();
                        var options = wrongPlaces.Append(place.Name).OrderBy(x => rng.Next()).ToList();
                        questions.Add(new { question = $"Which place is described as: '{desc[..Math.Min(60, desc.Length)]}...'?", options, answer = place.Name, hint = $"Category: {cat?.Name}" });
                    }
                    else
                    {
                        // Rating question
                        var correctRating = Math.Round(place.Rating, 1).ToString("F1");
                        var wrongRatings = new List<string> { "3.5", "4.0", "4.2", "4.5", "4.7", "4.8", "4.9", "5.0" }
                            .Where(r => r != correctRating).OrderBy(x => rng.Next()).Take(3).ToList();
                        var options = wrongRatings.Append(correctRating).OrderBy(x => rng.Next()).ToList();
                        questions.Add(new { question = $"What is the rating of '{place.Name}'?", options, answer = correctRating, hint = $"It is in the {cat?.Name} category." });
                    }
                }

                return Json(questions);
            }
            catch (Exception)
            {
                return Json(new List<object>());
            }
        }

        // POST: /Game/SubmitScore
        // Server-side bound: quiz has at most 10 questions × 3 pts = 30 pts max.
        // Defends against forged client submissions with inflated correctAnswers.
        [HttpPost]
        public IActionResult SubmitScore([FromBody] ScoreSubmission submission)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return Json(new { success = false });

                int correct = Math.Clamp(submission.CorrectAnswers, 0, 10);
                int points  = Math.Clamp(correct * 3, 0, 30);

                if (points > 0)
                {
                    user.AddPoints(points);
                    _context.SaveChanges();
                }
                return Json(new { success = true, pointsEarned = points, totalPoints = user.Points });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }
    }

    public class ScoreSubmission
    {
        public int CorrectAnswers { get; set; }
    }
}