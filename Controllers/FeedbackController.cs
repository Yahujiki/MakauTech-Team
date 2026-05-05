using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MakauTech.Data;
using MakauTech.Models;

namespace MakauTech.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly MakauTechDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FeedbackController(MakauTechDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
            SetViewBag();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 6_000_000)]
        public async Task<IActionResult> Submit(string subject, string description, IFormFile? attachment)
        {
            try
            {
                SetViewBag();
                subject = (subject ?? "").Trim();
                description = (description ?? "").Trim();
                if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(description))
                {
                    TempData["FeedbackError"] = "Subject and description are required.";
                    return RedirectToAction("Index");
                }

                if (subject.Length > 200) subject = subject[..200];
                if (description.Length > 2000) description = description[..2000];

                var user = GetCurrentUser();
                string? attachmentUrl = null;

                if (attachment != null && attachment.Length > 0)
                {
                    if (attachment.Length > 4 * 1024 * 1024)
                    {
                        TempData["FeedbackError"] = "Attachment must be 4 MB or smaller.";
                        return RedirectToAction("Index");
                    }
                    var ext = Path.GetExtension(attachment.FileName).ToLowerInvariant();
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf", ".txt" };
                    if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
                    {
                        TempData["FeedbackError"] = "Use JPG, PNG, WebP, GIF, PDF, or TXT for attachments.";
                        return RedirectToAction("Index");
                    }
                    var dir = Path.Combine(_env.WebRootPath, "uploads", "feedback");
                    Directory.CreateDirectory(dir);
                    var name = $"{Guid.NewGuid():N}{ext}";
                    var path = Path.Combine(dir, name);
                    await using (var fs = System.IO.File.Create(path))
                        await attachment.CopyToAsync(fs);
                    attachmentUrl = name;
                }

                var feedback = new Feedback
                {
                    UserId = user?.Id,
                    UserName = user?.Name ?? "Anonymous",
                    Rating = 5,
                    Subject = subject,
                    Description = description,
                    AttachmentUrl = attachmentUrl
                };
                _context.Feedbacks.Add(feedback);

                if (user != null)
                {
                    user.AddPoints(2);
                    TempData["FeedbackBonus"] = "true";
                }

                _context.SaveChanges();
                TempData["FeedbackSuccess"] = "true";
                TempData["FeedbackSubject"] = subject;
                return RedirectToAction("Thanks");
            }
            catch (Exception)
            {
                TempData["FeedbackError"] = "Something went wrong. Please try again.";
                return RedirectToAction("Index");
            }
        }

        public IActionResult Thanks()
        {
            SetViewBag();
            return View();
        }
    }
}
