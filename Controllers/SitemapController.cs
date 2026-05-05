using Microsoft.AspNetCore.Mvc;
using MakauTech.Data;
using System.Text;

namespace MakauTech.Controllers
{
    public class SitemapController : Controller
    {
        private readonly MakauTechDbContext _context;

        public SitemapController(MakauTechDbContext context)
        {
            _context = context;
        }

        [Route("sitemap.xml")]
        [ResponseCache(Duration = 3600)]
        public IActionResult Index()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            // ── Static pages ──────────────────────────────────────────
            var staticPages = new[]
            {
                new { Url = "/",              Priority = "1.0", Freq = "daily"   },
                new { Url = "/Home/Explore",  Priority = "0.9", Freq = "daily"   },
                new { Url = "/Home/Games",    Priority = "0.9", Freq = "weekly"  },
                new { Url = "/Home/Leaderboard", Priority = "0.8", Freq = "hourly" },
                new { Url = "/Home/Achievements", Priority = "0.8", Freq = "weekly" },
                new { Url = "/Home/HowItWorks",   Priority = "0.7", Freq = "monthly" },
                new { Url = "/Home/About",    Priority = "0.6", Freq = "monthly" },
                new { Url = "/Home/Terms",    Priority = "0.4", Freq = "yearly"  },
                new { Url = "/Home/Privacy",  Priority = "0.4", Freq = "yearly"  },
                new { Url = "/Home/Map",      Priority = "0.7", Freq = "weekly"  },
                new { Url = "/Home/Login",    Priority = "0.5", Freq = "monthly" },
                new { Url = "/Home/Register", Priority = "0.5", Freq = "monthly" },
                // Games
                new { Url = "/FoodCatch",       Priority = "0.8", Freq = "weekly" },
                new { Url = "/Memory",          Priority = "0.8", Freq = "weekly" },
                new { Url = "/KitchenRush",     Priority = "0.8", Freq = "weekly" },
                new { Url = "/Sibusprinter",    Priority = "0.8", Freq = "weekly" },
            };

            foreach (var page in staticPages)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}{page.Url}</loc>");
                sb.AppendLine($"    <lastmod>{today}</lastmod>");
                sb.AppendLine($"    <changefreq>{page.Freq}</changefreq>");
                sb.AppendLine($"    <priority>{page.Priority}</priority>");
                sb.AppendLine("  </url>");
            }

            // ── Dynamic: Places ───────────────────────────────────────
            try
            {
                var places = _context.Places.ToList();
                foreach (var place in places)
                {
                    sb.AppendLine("  <url>");
                    sb.AppendLine($"    <loc>{baseUrl}/Home/Explore?search={Uri.EscapeDataString(place.Name)}</loc>");
                    sb.AppendLine($"    <lastmod>{today}</lastmod>");
                    sb.AppendLine("    <changefreq>weekly</changefreq>");
                    sb.AppendLine("    <priority>0.7</priority>");
                    sb.AppendLine("  </url>");
                }
            }
            catch { /* graceful - skip if Places table issue */ }

            sb.AppendLine("</urlset>");

            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}
