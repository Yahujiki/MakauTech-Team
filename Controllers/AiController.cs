using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MakauTech.Data;
using MakauTech.Models;

namespace MakauTech.Controllers
{
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly MakauTechDbContext _context;

        public AiController(MakauTechDbContext context)
        {
            _context = context;
        }

        [HttpPost("chat")]
        public IActionResult Chat([FromBody] ChatRequest req)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized(new { reply = "Please log in to use the assistant." });

            if (string.IsNullOrWhiteSpace(req?.Message))
                return Ok(new { reply = "Hmm, I didn't catch that — try again?" });

            if (req.Message.Length > 1000)
                return Ok(new { reply = "That message is too long — keep it under 1000 characters, boleh!" });

            List<Place> places;
            try { places = _context.Places.Include(p => p.Category).ToList(); }
            catch { places = new List<Place>(); }

            return Ok(new { reply = GetRuleBasedReply(req.Message, places) });
        }

        private static string GetRuleBasedReply(string msg, List<Place> places)
        {
            var m = msg.ToLower().Trim();

            if (m.Contains("hello") || m.StartsWith("hi") || m.Contains(" hi ") || m.Contains("hai") || m.Contains("hey") || m.Contains("apa khabar"))
                return "Selamat datang! 👋 I'm Yenkah, your personal Sibu tourism guide! Ask me anything about food, places, nature, games or how to earn badges — I'm here to help you explore the hidden gem of Borneo, lah!";

            if (m.Contains("food") || m.Contains("eat") || m.Contains("makan") || m.Contains("kampua") || m.Contains("kolo") || m.Contains("kompia") || m.Contains("foochow"))
                return "Sibu's Foochow food is absolutely legendary! 🍜 Must-try: Kampua Mee (springy egg noodles with char siu), Kompia buns (like a Sibu bagel!), and Kolo Mee. Head to the Central Market area before 10am for the freshest and most authentic experience, boleh!";

            if (m.Contains("nature") || m.Contains("jungle") || m.Contains("hike") || m.Contains("trek") || m.Contains("bukit lima") || m.Contains("wildlife"))
                return "Bukit Lima Nature Reserve is Sibu's green escape right in the city! 🌿 Perfect for morning hikes with birdsong and fresh jungle air — expect hornbills and giant trees. Only 15 minutes from the city centre, very worth it kan!";

            if (m.Contains("temple") || m.Contains("tua pek") || m.Contains("heritage") || m.Contains("history") || m.Contains("museum") || m.Contains("culture"))
                return "Tua Pek Kong Temple is Sibu's most iconic landmark — over 100 years old and beautifully lit at night by the Rajang River! 🏛️ Pair it with the Sibu Heritage Centre nearby for the full Foochow cultural experience. Very photogenic place, lah!";

            if (m.Contains("river") || m.Contains("rajang") || m.Contains("boat") || m.Contains("cruise") || m.Contains("waterfront"))
                return "The mighty Rajang River is Southeast Asia's longest river and Sibu's beating heart! 🚢 Take a river cruise from Central Wharf for stunning city views — the waterfront promenade at sunset is absolutely gorgeous. Don't miss it, seriously!";

            if (m.Contains("market") || m.Contains("shop") || m.Contains("sungai merah") || m.Contains("bazaar") || m.Contains("pasar"))
                return "Sungai Merah is Sarawak's largest traditional market — a sensory feast! 🛒 Go early (6–9am) for the best local produce, hand-made Foochow snacks, wild honey, and a real taste of everyday Sibu life. Very lively and colourful, kan!";

            if (m.Contains("game") || m.Contains("play") || m.Contains("fun") || m.Contains("sprint") || m.Contains("kitchen rush") || m.Contains("memory"))
                return "There are 5 fun tourism games! 🎮 Try Sibu Sprint (endless runner through Sibu streets), Kitchen Rush (prep Foochow dishes against the clock), Memory Flip (match Sibu icons), Food Catch (catch falling treats), and Spin & Win. Each win earns up to 50 points — go beat that leaderboard!";

            if (m.Contains("badge") || m.Contains("achievement") || m.Contains("unlock") || m.Contains("award"))
                return "There are 8 badges to collect! 🏆 Start with Explorer (40 pts) → Active Reviewer (30 pts) → Sibu Expert (50 pts), all the way to the legendary Champion badge at 1000 pts. Visit places, write reviews and play games to level up fast — boleh!";

            if (m.Contains("point") || m.Contains("earn") || m.Contains("score") || m.Contains("how to get"))
                return "Earn points by: visiting places (+10 pts each), writing reviews (+5 pts each), and playing mini games (up to +50 pts per game win). 💪 More visits = faster badge unlocks and a higher rank on the global leaderboard. Every action counts, lah!";

            if (m.Contains("leaderboard") || m.Contains("rank") || m.Contains("top") || m.Contains("best"))
                return "The leaderboard shows the top explorers in Sibu! 🏅 Your rank goes up every time you visit a place, write a review, or win a game. Aim for the top 3 to show you're the ultimate Sibu explorer — champion level, kan!";

            if (m.Contains("weather") || m.Contains("climate") || m.Contains("when to visit") || m.Contains("hot") || m.Contains("rain"))
                return "Sibu has warm tropical weather year-round (26–35°C) with high humidity. 🌤️ The drier months (March–September) are best for outdoor activities. Always bring light breathable clothes and a small umbrella just in case — this is Borneo, after all, lah!";

            if (m.Contains("transport") || m.Contains("how to get") || m.Contains("flight") || m.Contains("bus") || m.Contains("travel to"))
                return "Fly to Sibu from Kuala Lumpur on Malaysia Airlines or AirAsia — flights are daily and take about 1.5 hours. ✈️ From Kuching, you can take an express boat via Sarikei (about 4–5 hours, very scenic!). Within Sibu, Grab and local taxis are convenient and affordable, boleh!";

            if (m.Contains("wisma") || m.Contains("sanyan"))
                return "Wisma Sanyan is Sibu's landmark riverside complex — iconic for photos with the Rajang River as your backdrop! 🏙️ The surrounding streets have great local cafes, Chinese medicine shops and street food stalls worth exploring. Very central location, kan!";

            if (m.Contains("register") || m.Contains("sign up") || m.Contains("account") || m.Contains("join"))
                return "Joining MakauTech is free and takes 30 seconds! 🚀 Register with your email, then start exploring Sibu places, writing reviews, playing games, and collecting badges. The more you explore, the higher you climb on the leaderboard, lah!";

            foreach (var p in places)
            {
                var parts = p.Name.ToLower().Split(' ');
                if (parts.Any(part => part.Length > 3 && m.Contains(part)))
                {
                    var desc = p.Description != null && p.Description.Length > 120
                        ? p.Description[..120] + "…" : p.Description ?? "A wonderful Sibu spot!";
                    return $"{p.Name} is definitely worth a visit! 📍 {desc} Check the Explore section to mark your visit and earn points, boleh!";
                }
            }

            return "Great question! 😊 Sibu has amazing Foochow food, river cruises, jungle treks, heritage temples, and 5 fun games to play. What would you like to know more about — food, places, games, or how to earn badges?";
        }
    }

    public class ChatRequest
    {
        public string? Message { get; set; }
    }
}
