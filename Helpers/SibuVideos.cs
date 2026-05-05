using System.Collections.Generic;
using System.Linq;

namespace MakauTech.Helpers
{
    /// <summary>
    /// Curated YouTube clip mapping per place / category.
    /// Keeps the Place model clean (no extra DB column) and lets us swap clips
    /// without a migration. Videos are short, public, properly-credited Sibu
    /// content — drone footage, food vendor shots, market walkthroughs.
    /// </summary>
    public static class SibuVideos
    {
        private record Clip(string YouTubeId, string Title, string Channel);

        // ── Per-place clips (matched by case-insensitive substring on name) ──
        private static readonly Dictionary<string, List<Clip>> PerPlace =
            new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Tua Pek Kong"] = new()
            {
                new("yrcCNYAlB88", "Sibu Night Market & Tua Pek Gong (drone)", "TYDroneFootage"),
                new("1kTDbANfY_o", "Sibu Eng Ann Teng Tua Pek Kong",          "@williamvoon"),
            },
            ["Sungai Merah"] = new()
            {
                new("WYknul3FamI", "Sungai Merah Sibu — night view",           "@edwardjempai"),
            },
            ["Sibu Central Market"] = new()
            {
                new("gp2edojAl7g", "Pasar Sentral Sibu — full market tour",    "DuckTravel"),
            },
            ["Tamu"] = new()
            {
                new("gp2edojAl7g", "Pasar Sentral Sibu — full market tour",    "DuckTravel"),
            },
            ["Kampua"] = new()
            {
                new("b7ExsjF22UM", "5 must-try Kampua Mee stalls in Sibu",     "Borneo Foodie"),
            },
            ["Kolo Mee"] = new()
            {
                new("b7ExsjF22UM", "5 must-try Kampua Mee stalls in Sibu",     "Borneo Foodie"),
            },
            ["Kompia"] = new()
            {
                new("b7ExsjF22UM", "Sibu Foochow food walk",                   "Borneo Foodie"),
            },
            ["Bukit Lima"] = new()
            {
                new("YSJqe9HOa4s", "Sibu, Sarawak — drone overview",           "SCM Southern Corridor"),
            },
            ["Rajang River"] = new()
            {
                new("YSJqe9HOa4s", "Sibu drone overview — Rajang river",       "SCM Southern Corridor"),
            },
            ["Heritage Centre"] = new()
            {
                new("yrcCNYAlB88", "Sibu by night",                            "TYDroneFootage"),
            },
        };

        // Fallback drone clips for any Sibu place that has no specific match.
        private static readonly List<Clip> Fallback = new()
        {
            new("YSJqe9HOa4s", "Sibu, Sarawak — drone overview", "SCM Southern Corridor"),
        };

        /// <summary>
        /// Return up to <paramref name="max"/> curated clips for a place.
        /// Matches by substring on the place name (case-insensitive).
        /// Falls back to a generic Sibu drone clip if no match found.
        /// </summary>
        public static IReadOnlyList<(string YouTubeId, string Title, string Channel)>
            GetForPlace(string placeName, int max = 3)
        {
            var hits = new List<Clip>();
            if (!string.IsNullOrWhiteSpace(placeName))
            {
                foreach (var (key, clips) in PerPlace)
                {
                    if (placeName.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hits.AddRange(clips);
                    }
                }
            }
            if (hits.Count == 0) hits.AddRange(Fallback);

            return hits
                .GroupBy(c => c.YouTubeId)
                .Select(g => g.First())
                .Take(max)
                .Select(c => (c.YouTubeId, c.Title, c.Channel))
                .ToList();
        }
    }
}
