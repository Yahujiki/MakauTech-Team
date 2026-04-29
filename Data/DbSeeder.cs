using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MakauTech.Models;

namespace MakauTech.Data
{
    public static class DbSeeder
    {
        public static void Seed(MakauTechDbContext context, IConfiguration? configuration = null)
        {
            EnsureCategories(context);
            EnsurePlaces(context);
            EnsureNewPlaces(context);
            NormalizeLegacyImageUrls(context);
            EnsureWismaUsesPhotoFallback(context);
            UpsertAchievements(context);
            EnsureAdmin(context, configuration);
            context.SaveChanges();
        }

        /// <summary>Called manually from admin panel — resets all non-admin points/badges/visits.</summary>
        public static void AdminResetLeaderboard(MakauTechDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(
                    @"UPDATE ""Users"" SET ""Points"" = 0, ""Badges"" = '', ""VisitedPlaceIds"" = '' WHERE ""Email"" != 'admin@makautech.com'");
                context.SaveChanges();
            }
            catch { }
        }

        /// <summary>Ensure the four tourism categories exist (fixes empty Explore when categories were wiped but table wasn’t).</summary>
        private static void EnsureCategories(MakauTechDbContext context)
        {
            var required = new[]
            {
                ("Nature", "🌿"),
                ("Heritage", "🏛️"),
                ("Food", "🍜"),
                ("Adventure", "🎯")
            };
            var added = false;
            foreach (var (name, icon) in required)
            {
                if (context.Categories.Any(c => c.Name == name)) continue;
                context.Categories.Add(new Category { Name = name, Icon = icon });
                added = true;
            }
            if (added) context.SaveChanges();
        }

        /// <summary>Re-seed default Sibu places if the table is empty (e.g. cleared DB, failed first seed).</summary>
        private static void EnsurePlaces(MakauTechDbContext context)
        {
            if (context.Places.Any()) return;
            AddDefaultSibuPlaces(context);
        }

        /// <summary>Call from Home/Explore so places reappear without restarting the app after an empty database.</summary>
        public static void EnsureMinimumTourismData(MakauTechDbContext context)
        {
            EnsureCategories(context);
            if (!context.Places.Any())
                AddDefaultSibuPlaces(context);
            else
                BackfillCoordinates(context);
            EnsureNewPlaces(context);
        }

        /// <summary>Seed Lat/Lng for existing places that were inserted before coordinates were added.</summary>
        private static void BackfillCoordinates(MakauTechDbContext context)
        {
            var map = new Dictionary<string, (double Lat, double Lng)>(StringComparer.OrdinalIgnoreCase)
            {
                { "Bukit Lima Nature Reserve", (2.2802, 111.8115) },
                { "Sungai Merah",              (2.2961, 111.8212) },
                { "Tua Pek Kong Temple",       (2.2958, 111.8191) },
                { "Sibu Heritage Centre",      (2.2951, 111.8200) },
                { "Kolo Mee Sarawak",          (2.2943, 111.8175) },
                { "Kampua Mee",                (2.2938, 111.8180) },
                { "Kompia",                    (2.2930, 111.8170) },
                { "Sibu Central Market",       (2.2946, 111.8172) },
                { "Rajang River Cruise",       (2.2900, 111.8100) },
                { "Wisma Sanyan",              (2.2957, 111.8195) },
                { "Masjid Al-Qadim",           (2.2965, 111.8205) },
                { "Lau King Howe Hospital Memorial Museum", (2.2870, 111.8260) },
                { "Sibu Night Market",         (2.2945, 111.8168) },
                { "Star Mega Mall",            (2.3050, 111.8450) },
                { "Sibu Lake Garden",          (2.2780, 111.8350) },
                { "Sacred Heart Cathedral",    (2.2940, 111.8230) }
            };
            var dirty = false;
            foreach (var p in context.Places.ToList())
            {
                if (p.Latitude.HasValue) continue;
                if (map.TryGetValue(p.Name, out var c)) { p.Latitude = c.Lat; p.Longitude = c.Lng; dirty = true; }
            }
            if (dirty) context.SaveChanges();
        }

        private static void AddDefaultSibuPlaces(MakauTechDbContext context)
        {
            Category? Cat(string name) => context.Categories.FirstOrDefault(c => c.Name == name);

            var nature = Cat("Nature");
            var heritage = Cat("Heritage");
            var food = Cat("Food");
            var adventure = Cat("Adventure");

            if (nature == null || heritage == null || food == null || adventure == null)
                return;

            context.Places.AddRange(
                new Place { Name = "Bukit Lima Nature Reserve", Location = "Sibu", Description = "A peaceful nature reserve perfect for hiking and bird watching.", ImageUrl = "/Images/bukit-lima.jpg", Rating = 4.5, CategoryId = nature.Id, Latitude = 2.2802, Longitude = 111.8115 },
                new Place { Name = "Sungai Merah", Location = "Sibu", Description = "Historic waterfront with traditional shophouses and river views.", ImageUrl = "/Images/sungai-merah.jpg", Rating = 4.2, CategoryId = heritage.Id, Latitude = 2.2961, Longitude = 111.8212 },
                new Place { Name = "Tua Pek Kong Temple", Location = "Sibu", Description = "One of the oldest Chinese temples in Sarawak, rich in culture.", ImageUrl = "/Images/tua-pek-kong.jpg", Rating = 4.7, CategoryId = heritage.Id, Latitude = 2.2958, Longitude = 111.8191 },
                new Place { Name = "Sibu Heritage Centre", Location = "Sibu", Description = "Museum showcasing the history and culture of Sibu.", ImageUrl = "/Images/heritage-centre.jpg", Rating = 4.3, CategoryId = heritage.Id, Latitude = 2.2951, Longitude = 111.8200 },
                new Place { Name = "Kolo Mee Sarawak", Location = "Sibu", Description = "Iconic dry noodle dish unique to Sarawak.", ImageUrl = "/Images/kolo-mee.jpg", Rating = 4.8, CategoryId = food.Id, Latitude = 2.2943, Longitude = 111.8175 },
                new Place { Name = "Kampua Mee", Location = "Sibu", Description = "Sibu's signature noodle dish, beloved by locals.", ImageUrl = "/Images/kampua-mee.jpg", Rating = 4.9, CategoryId = food.Id, Latitude = 2.2938, Longitude = 111.8180 },
                new Place { Name = "Kompia", Location = "Sibu", Description = "Traditional Foochow baked bun, crispy outside soft inside.", ImageUrl = "/Images/kompia.jpg", Rating = 4.6, CategoryId = food.Id, Latitude = 2.2930, Longitude = 111.8170 },
                new Place { Name = "Sibu Central Market", Location = "Sibu", Description = "Largest market in Sarawak. Fresh produce, local crafts, street food.", ImageUrl = "/Images/central-market.jpg", Rating = 4.4, CategoryId = adventure.Id, Latitude = 2.2946, Longitude = 111.8172 },
                new Place { Name = "Rajang River Cruise", Location = "Sibu", Description = "Scenic cruise on one of the longest rivers in Malaysia.", ImageUrl = "/Images/rajang-river.jpg", Rating = 4.5, CategoryId = adventure.Id, Latitude = 2.2900, Longitude = 111.8100 },
                new Place { Name = "Masjid Al-Qadim", Location = "Sibu", Description = "Masjid Al-Qadim (also known as Masjid Lama Sibu) is one of the oldest mosques in Sibu, Sarawak. Built in the early 20th century by the local Malay and Melanau Muslim community, this historic mosque stands as a proud symbol of Islamic heritage in the heart of Sibu. The mosque features traditional Malay-Islamic architecture with a distinctive whitewashed exterior, elegant arched windows, and a classic minaret tower that has become a beloved landmark. Surrounded by lush greenery, Masjid Al-Qadim offers a serene atmosphere for worship and reflection. It remains an important spiritual centre for the Muslim community in Sibu and a testament to the town's multicultural and multi-faith identity. Visitors are welcome to admire the architecture and learn about the rich Islamic history woven into Sibu's cultural tapestry.", ImageUrl = "/Images/masjid-al-qadim.jpg", Rating = 4.6, CategoryId = heritage.Id, Latitude = 2.2965, Longitude = 111.8205 },
                new Place { Name = "Lau King Howe Hospital Memorial Museum", Location = "Sibu", Description = "The Lau King Howe Hospital Memorial Museum is a heritage museum located in the grounds of the former Lau King Howe Hospital — one of the earliest modern medical facilities in Sibu, originally established in the 1930s. The museum preserves the legacy of Dr. Lau King Howe, a pioneering physician who dedicated his life to serving the health needs of the local community during a time when modern medicine was scarce in rural Sarawak. Exhibits include vintage medical equipment, historical photographs, original ward furnishings, and documents tracing the hospital's evolution from a small clinic to a full-fledged medical centre. The museum also highlights the broader story of healthcare development in Sarawak and the sacrifices made during the Japanese occupation of World War II. It stands as a tribute to community resilience and the spirit of service that defined early Sibu.", ImageUrl = "/Images/lau-king-howe.jpg", Rating = 4.4, CategoryId = heritage.Id, Latitude = 2.2870, Longitude = 111.8260 },
                new Place { Name = "Sibu Night Market", Location = "Sibu", Description = "The Sibu Night Market (Pasar Malam Sibu) is one of the most vibrant and beloved evening attractions in the town. Located along the streets near the town centre, the night market comes alive every evening with rows of brightly lit stalls selling an incredible variety of local street food, fresh produce, clothing, accessories, and household goods. Food lovers can feast on char-grilled satay, Foochow-style fried noodles, fresh tropical fruit juices, barbecued seafood, and an endless parade of Sarawak-style snacks and desserts. The atmosphere is electric — locals and visitors mingle under strings of fairy lights while the aroma of sizzling woks fills the air. The Sibu Night Market is the perfect place to experience the authentic pulse of everyday Sibu life and sample flavours you won't find anywhere else.", ImageUrl = "/Images/sibu-night-market.jpg", Rating = 4.1, CategoryId = food.Id, Latitude = 2.2945, Longitude = 111.8168 },
                new Place { Name = "Star Mega Mall", Location = "Sibu", Description = "Star Mega Mall is the largest and most modern shopping complex in Sibu, serving as the town's premier retail and entertainment destination. Opened to cater to the growing urban population, the mall features multiple floors of international and local retail brands, a modern cinema complex, a well-stocked supermarket, a dedicated food court with a wide selection of local and international cuisines, and family-friendly entertainment zones including an arcade and indoor playground. The mall's contemporary architecture and air-conditioned comfort make it a popular gathering spot for families, young people, and shoppers looking for everything from fashion and electronics to home goods and specialty items. Star Mega Mall also hosts seasonal events, cultural festivals, and promotional activities throughout the year, making it a dynamic hub of activity in the heart of Sibu.", ImageUrl = "/Images/star-mega-mall.jpg", Rating = 4.3, CategoryId = adventure.Id, Latitude = 2.3050, Longitude = 111.8450 },
                new Place { Name = "Sibu Lake Garden", Location = "Sibu", Description = "Sibu Lake Garden (Taman Tasik Sibu) is a beautifully landscaped public park built around a tranquil man-made lake on the outskirts of Sibu town. Designed as a recreational retreat for locals and visitors alike, the park features well-maintained walking and jogging trails that wind through lush tropical greenery, a scenic wooden boardwalk over the lake, shaded rest areas with gazebos, and a children's playground. The lake itself is home to various species of freshwater fish and attracts local birdlife, making it a peaceful spot for nature photography and birdwatching. In the early morning and late evening, the park fills with joggers, tai chi practitioners, and families enjoying the cool breeze. Sibu Lake Garden offers a perfect escape from the bustling town centre — a green oasis where you can unwind, exercise, and reconnect with nature.", ImageUrl = "/Images/sibu-lake-garden.jpg", Rating = 4.3, CategoryId = nature.Id, Latitude = 2.2780, Longitude = 111.8350 },
                new Place { Name = "Sacred Heart Cathedral", Location = "Sibu", Description = "The Sacred Heart Cathedral (also known as the Cathedral of the Sacred Heart of Jesus) is the principal Roman Catholic church in Sibu and one of the most architecturally striking religious buildings in the region. Originally established by Catholic missionaries who arrived in Sarawak in the late 19th century, the current cathedral building showcases a blend of modern ecclesiastical design with traditional elements — featuring soaring arched ceilings, beautiful stained-glass windows depicting biblical scenes, and a grand altar that serves as the spiritual centrepiece. The cathedral is the seat of the Catholic Diocese of Sibu and serves a vibrant and diverse congregation drawn from the Iban, Chinese, Malay, and other ethnic communities. Throughout the year, the cathedral hosts important liturgical celebrations, community outreach programmes, and cultural events that reflect Sibu's deep spirit of interfaith harmony. Visitors are welcome to admire the architecture, attend services, and experience the peaceful atmosphere within its hallowed walls.", ImageUrl = "/Images/sacred-heart-cathedral.jpg", Rating = 4.7, CategoryId = heritage.Id, Latitude = 2.2940, Longitude = 111.8230 }
            );
            context.SaveChanges();
        }

        /// <summary>Add newer places that were introduced after initial seed. Skips if already present.</summary>
        private static void EnsureNewPlaces(MakauTechDbContext context)
        {
            Category? Cat(string name) => context.Categories.FirstOrDefault(c => c.Name == name);
            var heritage = Cat("Heritage");
            var food = Cat("Food");
            var nature = Cat("Nature");
            var adventure = Cat("Adventure");
            var dirty = false;

            var newPlaces = new List<(string Name, string Desc, string Img, double Rating, Category? Cat, double Lat, double Lng)>
            {
                ("Masjid Al-Qadim",
                 "Masjid Al-Qadim (also known as Masjid Lama Sibu) is one of the oldest mosques in Sibu, Sarawak. Built in the early 20th century by the local Malay and Melanau Muslim community, this historic mosque stands as a proud symbol of Islamic heritage in the heart of Sibu. The mosque features traditional Malay-Islamic architecture with a distinctive whitewashed exterior, elegant arched windows, and a classic minaret tower that has become a beloved landmark. Surrounded by lush greenery, Masjid Al-Qadim offers a serene atmosphere for worship and reflection. It remains an important spiritual centre for the Muslim community in Sibu and a testament to the town's multicultural and multi-faith identity. Visitors are welcome to admire the architecture and learn about the rich Islamic history woven into Sibu's cultural tapestry.",
                 "/Images/masjid-al-qadim.jpg", 4.6, heritage, 2.2965, 111.8205),

                ("Lau King Howe Hospital Memorial Museum",
                 "The Lau King Howe Hospital Memorial Museum is a heritage museum located in the grounds of the former Lau King Howe Hospital — one of the earliest modern medical facilities in Sibu, originally established in the 1930s. The museum preserves the legacy of Dr. Lau King Howe, a pioneering physician who dedicated his life to serving the health needs of the local community during a time when modern medicine was scarce in rural Sarawak. Exhibits include vintage medical equipment, historical photographs, original ward furnishings, and documents tracing the hospital's evolution from a small clinic to a full-fledged medical centre. The museum also highlights the broader story of healthcare development in Sarawak and the sacrifices made during the Japanese occupation of World War II. It stands as a tribute to community resilience and the spirit of service that defined early Sibu.",
                 "/Images/lau-king-howe.jpg", 4.4, heritage, 2.2870, 111.8260),

                ("Sibu Night Market",
                 "The Sibu Night Market (Pasar Malam Sibu) is one of the most vibrant and beloved evening attractions in the town. Located along the streets near the town centre, the night market comes alive every evening with rows of brightly lit stalls selling an incredible variety of local street food, fresh produce, clothing, accessories, and household goods. Food lovers can feast on char-grilled satay, Foochow-style fried noodles, fresh tropical fruit juices, barbecued seafood, and an endless parade of Sarawak-style snacks and desserts. The atmosphere is electric — locals and visitors mingle under strings of fairy lights while the aroma of sizzling woks fills the air. The Sibu Night Market is the perfect place to experience the authentic pulse of everyday Sibu life and sample flavours you won't find anywhere else.",
                 "/Images/sibu-night-market.jpg", 4.1, food, 2.2945, 111.8168),

                ("Star Mega Mall",
                 "Star Mega Mall is the largest and most modern shopping complex in Sibu, serving as the town's premier retail and entertainment destination. Opened to cater to the growing urban population, the mall features multiple floors of international and local retail brands, a modern cinema complex, a well-stocked supermarket, a dedicated food court with a wide selection of local and international cuisines, and family-friendly entertainment zones including an arcade and indoor playground. The mall's contemporary architecture and air-conditioned comfort make it a popular gathering spot for families, young people, and shoppers looking for everything from fashion and electronics to home goods and specialty items. Star Mega Mall also hosts seasonal events, cultural festivals, and promotional activities throughout the year, making it a dynamic hub of activity in the heart of Sibu.",
                 "/Images/star-mega-mall.jpg", 4.3, adventure, 2.3050, 111.8450),

                ("Sibu Lake Garden",
                 "Sibu Lake Garden (Taman Tasik Sibu) is a beautifully landscaped public park built around a tranquil man-made lake on the outskirts of Sibu town. Designed as a recreational retreat for locals and visitors alike, the park features well-maintained walking and jogging trails that wind through lush tropical greenery, a scenic wooden boardwalk over the lake, shaded rest areas with gazebos, and a children's playground. The lake itself is home to various species of freshwater fish and attracts local birdlife, making it a peaceful spot for nature photography and birdwatching. In the early morning and late evening, the park fills with joggers, tai chi practitioners, and families enjoying the cool breeze. Sibu Lake Garden offers a perfect escape from the bustling town centre — a green oasis where you can unwind, exercise, and reconnect with nature.",
                 "/Images/sibu-lake-garden.jpg", 4.3, nature, 2.2780, 111.8350),

                ("Sacred Heart Cathedral",
                 "The Sacred Heart Cathedral (also known as the Cathedral of the Sacred Heart of Jesus) is the principal Roman Catholic church in Sibu and one of the most architecturally striking religious buildings in the region. Originally established by Catholic missionaries who arrived in Sarawak in the late 19th century, the current cathedral building showcases a blend of modern ecclesiastical design with traditional elements — featuring soaring arched ceilings, beautiful stained-glass windows depicting biblical scenes, and a grand altar that serves as the spiritual centrepiece. The cathedral is the seat of the Catholic Diocese of Sibu and serves a vibrant and diverse congregation drawn from the Iban, Chinese, Malay, and other ethnic communities. Throughout the year, the cathedral hosts important liturgical celebrations, community outreach programmes, and cultural events that reflect Sibu's deep spirit of interfaith harmony. Visitors are welcome to admire the architecture, attend services, and experience the peaceful atmosphere within its hallowed walls.",
                 "/Images/sacred-heart-cathedral.jpg", 4.7, heritage, 2.2940, 111.8230),
            };

            foreach (var (name, desc, img, rating, cat, lat, lng) in newPlaces)
            {
                if (cat == null || context.Places.Any(p => p.Name == name)) continue;
                context.Places.Add(new Place
                {
                    Name = name, Location = "Sibu", Description = desc,
                    ImageUrl = img, Rating = rating, CategoryId = cat.Id,
                    Latitude = lat, Longitude = lng
                });
                dirty = true;
            }
            if (dirty) context.SaveChanges();
        }

        /// <summary>Migrate old paths to JPEGs in wwwroot/Images (same filenames you add locally).</summary>
        private static void NormalizeLegacyImageUrls(MakauTechDbContext context)
        {
            // Ordinal — avoids duplicate-key crash: OrdinalIgnoreCase treats /images/X and /Images/X as the same key.
            var map = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "/images/bukit-lima.jpg", "/Images/bukit-lima.jpg" },
                { "/images/sungai-merah.jpg", "/Images/sungai-merah.jpg" },
                { "/images/tua-pek-kong.jpg", "/Images/tua-pek-kong.jpg" },
                { "/images/heritage-centre.jpg", "/Images/heritage-centre.jpg" },
                { "/images/kolo-mee.jpg", "/Images/kolo-mee.jpg" },
                { "/images/kampua-mee.jpg", "/Images/kampua-mee.jpg" },
                { "/images/kompia.jpg", "/Images/kompia.jpg" },
                { "/images/central-market.jpg", "/Images/central-market.jpg" },
                { "/images/rajang-river.jpg", "/Images/rajang-river.jpg" },
                { "/images/wisma-sanyan.jpg", "/Images/wisma-sanyan.jpg" },
                { "/images/wisma-sanyan.svg", "/Images/wisma-sanyan.jpg" },
                { "/images/placeholder.svg", "/Images/placeholder.svg" },
                { "/Images/place-bukit-lima.svg", "/Images/bukit-lima.jpg" },
                { "/Images/place-sungai-merah.svg", "/Images/sungai-merah.jpg" },
                { "/Images/place-tua-pek-kong.svg", "/Images/tua-pek-kong.jpg" },
                { "/Images/place-heritage-centre.svg", "/Images/heritage-centre.jpg" },
                { "/Images/place-kolo-mee.svg", "/Images/kolo-mee.jpg" },
                { "/Images/place-kampua-mee.svg", "/Images/kampua-mee.jpg" },
                { "/Images/place-kompia.svg", "/Images/kompia.jpg" },
                { "/Images/place-central-market.svg", "/Images/central-market.jpg" },
                { "/Images/place-rajang-river.svg", "/Images/rajang-river.jpg" },
                { "/Images/wisma-sanyan.svg", "/Images/wisma-sanyan.jpg" },
                { "/places/bukit-lima.jpg", "/Images/bukit-lima.jpg" },
                { "/places/sungai-merah.jpg", "/Images/sungai-merah.jpg" },
                { "/places/tua-pek-kong.jpg", "/Images/tua-pek-kong.jpg" },
                { "/places/heritage-centre.jpg", "/Images/heritage-centre.jpg" },
                { "/places/kolo-mee.jpg", "/Images/kolo-mee.jpg" },
                { "/places/kampua-mee.jpg", "/Images/kampua-mee.jpg" },
                { "/places/kompia.jpg", "/Images/kompia.jpg" },
                { "/places/central-market.jpg", "/Images/central-market.jpg" },
                { "/places/rajang-river.jpg", "/Images/rajang-river.jpg" },
                { "/places/wisma-sanyan.jpg", "/Images/wisma-sanyan.jpg" },
                { "/images/masjid-al-qadim.jpg", "/Images/masjid-al-qadim.jpg" },
                { "/places/masjid-al-qadim.jpg", "/Images/masjid-al-qadim.jpg" },
                { "/images/lau-king-howe.jpg", "/Images/lau-king-howe.jpg" },
                { "/images/sibu-night-market.jpg", "/Images/sibu-night-market.jpg" },
                { "/images/star-mega-mall.jpg", "/Images/star-mega-mall.jpg" },
                { "/images/sibu-lake-garden.jpg", "/Images/sibu-lake-garden.jpg" },
                { "/images/sacred-heart-cathedral.jpg", "/Images/sacred-heart-cathedral.jpg" }
            };

            var dirty = false;
            foreach (var p in context.Places.ToList())
            {
                if (string.IsNullOrWhiteSpace(p.ImageUrl)) continue;
                var key = p.ImageUrl.Trim();
                if (map.TryGetValue(key, out var replacement))
                {
                    p.ImageUrl = replacement;
                    dirty = true;
                    continue;
                }
                foreach (var kv in map)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(key, kv.Key))
                    {
                        p.ImageUrl = kv.Value;
                        dirty = true;
                        break;
                    }
                }
            }
            if (dirty) context.SaveChanges();
        }

        /// <summary>Wisma is inserted with an SVG URL from runtime code; bump to bundled photo on each startup.</summary>
        private static void EnsureWismaUsesPhotoFallback(MakauTechDbContext context)
        {
            var w = context.Places.FirstOrDefault(p => p.Name == "Wisma Sanyan" && p.Location == "Sibu");
            if (w == null) return;
            var url = w.ImageUrl?.Trim() ?? "";
            if (url.EndsWith("wisma-sanyan.svg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(url, "/Images/wisma-sanyan.svg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(url, "/images/wisma-sanyan.svg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(url, "/places/wisma-sanyan.jpg", StringComparison.OrdinalIgnoreCase))
            {
                w.ImageUrl = "/Images/wisma-sanyan.jpg";
                context.SaveChanges();
            }
        }

        private static void UpsertAchievements(MakauTechDbContext context)
        {
            var achievements = new List<Achievement>
            {
                new Achievement { Name = "First Step",      Description = "Visit your very first place in Sibu",                  Icon = "🌟", PointsRequired = 0,     PlacesRequired = 1  },
                new Achievement { Name = "Food Hunter",     Description = "Visit 5 different places around Sibu",                Icon = "🍽️", PointsRequired = 0,    PlacesRequired = 5  },
                new Achievement { Name = "Explorer",        Description = "Visit 10 Sibu landmarks — true explorer!",            Icon = "🗺️", PointsRequired = 0,    PlacesRequired = 10 },
                new Achievement { Name = "True Native",     Description = "Discover all 20 places in Sibu — you're a local!",   Icon = "🏡", PointsRequired = 0,     PlacesRequired = 20 },
                new Achievement { Name = "Active Reviewer", Description = "Earn 200 points by engaging with the app",            Icon = "✍️", PointsRequired = 200,   PlacesRequired = 0  },
                new Achievement { Name = "Sibu Expert",     Description = "Earn 500 points — you really know Sibu!",             Icon = "🏆", PointsRequired = 500,   PlacesRequired = 0  },
                new Achievement { Name = "Adventurer",      Description = "Reach 1000 points — a true adventurer!",              Icon = "⚔️", PointsRequired = 1000,  PlacesRequired = 0  },
                new Achievement { Name = "Game Master",     Description = "Earn 2000 points mastering the mini games",           Icon = "🎮", PointsRequired = 2000,  PlacesRequired = 0  },
                new Achievement { Name = "Legend",          Description = "Earn 3500 points — a legendary Sibu explorer!",       Icon = "⚡", PointsRequired = 3500,  PlacesRequired = 0  },
                new Achievement { Name = "Champion",        Description = "Reach 5000 points — the ultimate champion!",          Icon = "👑", PointsRequired = 5000,  PlacesRequired = 0  },
            };
            foreach (var a in achievements)
            {
                var existing = context.Achievements.FirstOrDefault(x => x.Name == a.Name);
                if (existing == null)
                    context.Achievements.Add(a);
                else
                {
                    existing.Description = a.Description;
                    existing.Icon = a.Icon;
                    existing.PointsRequired = a.PointsRequired;
                    existing.PlacesRequired = a.PlacesRequired;
                }
            }
            context.SaveChanges();
        }

        /// <summary>Reset all non-admin user points/badges to 0 for a clean leaderboard.</summary>
        private static void ResetLeaderboard(MakauTechDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(
                    @"UPDATE ""Users"" SET ""Points"" = 0, ""Badges"" = '', ""VisitedPlaceIds"" = '' WHERE ""Email"" != 'admin@makautech.com'");
            }
            catch { }
        }

        private static void EnsureAdmin(MakauTechDbContext context, IConfiguration? configuration)
        {
            var email = configuration?["Admin:SeedEmail"] ?? "admin@makautech.com";
            var seedPassword = configuration?["Admin:SeedPassword"];

            var existing = context.Users.FirstOrDefault(u => u.Email == email);
            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(seedPassword))
                {
                    bool matches = existing.Password.StartsWith("$2")
                        && BCrypt.Net.BCrypt.Verify(seedPassword, existing.Password);
                    if (!matches)
                    {
                        existing.Password = BCrypt.Net.BCrypt.HashPassword(seedPassword, workFactor: 12);
                        context.SaveChanges();
                    }
                }
                else if (!existing.Password.StartsWith("$2"))
                {
                    existing.Password = BCrypt.Net.BCrypt.HashPassword(existing.Password, workFactor: 12);
                    context.SaveChanges();
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(seedPassword)) return;

            context.Users.Add(new Admin
            {
                Name = "Admin",
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(seedPassword, workFactor: 12),
                Role = "SuperAdmin"
            });
            context.SaveChanges();
        }
    }
}
