namespace MakauTech.Helpers
{
    public class SibuEvent
    {
        public DateTime Date     { get; init; }
        public string  Title     { get; init; } = string.Empty;
        public string  Category  { get; init; } = "Cultural";
        public string  Emoji     { get; init; } = "🎉";
        public string  Location  { get; init; } = "Sibu, Sarawak";
        public string? Note      { get; init; }
        public bool    AllDay    { get; init; } = true;
    }

    /// <summary>
    /// Curated calendar of Sibu festivals and notable annual events.
    /// Hard-coded for now; can be moved to DB later.
    /// </summary>
    public static class SibuEvents
    {
        public static IReadOnlyList<SibuEvent> GetAll()
        {
            int y = DateTime.UtcNow.Year;
            var events = new List<SibuEvent>
            {
                new() { Date = new DateTime(y, 1, 1),   Title = "New Year's Day",                 Category = "Public Holiday", Emoji = "🎆", Note = "Public holiday" },
                new() { Date = new DateTime(y, 1, 29),  Title = "Chinese New Year (Day 1)",        Category = "Cultural",       Emoji = "🧧", Note = "Streets and temples lit; family reunions" },
                new() { Date = new DateTime(y, 1, 30),  Title = "Chinese New Year (Day 2)",        Category = "Cultural",       Emoji = "🧧" },
                new() { Date = new DateTime(y, 2, 12),  Title = "Chap Goh Meh",                    Category = "Festival",       Emoji = "🏮", Location = "Tua Pek Kong Temple", Note = "15th day of CNY; mandarin orange tossing" },
                new() { Date = new DateTime(y, 4, 1),   Title = "Sibu Town Anniversary",           Category = "Civic",          Emoji = "🦢", Note = "Sibu municipality anniversary" },
                new() { Date = new DateTime(y, 5, 1),   Title = "Labour Day",                      Category = "Public Holiday", Emoji = "🛠️" },
                new() { Date = new DateTime(y, 6, 1),   Title = "Gawai Dayak (Day 1)",             Category = "Festival",       Emoji = "🌾", Note = "Iban / Dayak harvest festival" },
                new() { Date = new DateTime(y, 6, 2),   Title = "Gawai Dayak (Day 2)",             Category = "Festival",       Emoji = "🌾" },
                new() { Date = new DateTime(y, 7, 16),  Title = "Borneo Cultural Festival opens",  Category = "Festival",       Emoji = "🎭", Location = "Sibu Town Square", Note = "10-day food, music, heritage festival" },
                new() { Date = new DateTime(y, 7, 22),  Title = "Sarawak Independence Day",        Category = "Public Holiday", Emoji = "🟥", Note = "Hari Sarawak Merdeka" },
                new() { Date = new DateTime(y, 7, 25),  Title = "Borneo Cultural Festival closes", Category = "Festival",       Emoji = "🎭" },
                new() { Date = new DateTime(y, 8, 31),  Title = "Merdeka Day",                     Category = "Public Holiday", Emoji = "🇲🇾" },
                new() { Date = new DateTime(y, 9, 16),  Title = "Malaysia Day",                    Category = "Public Holiday", Emoji = "🇲🇾" },
                new() { Date = new DateTime(y, 9, 20),  Title = "Mooncake Festival",               Category = "Cultural",       Emoji = "🥮", Note = "Mid-autumn lantern walk by the Rajang" },
                new() { Date = new DateTime(y,10, 14),  Title = "Hungry Ghost Festival peak",      Category = "Cultural",       Emoji = "🕯️", Location = "Tua Pek Kong" },
                new() { Date = new DateTime(y,11, 1),   Title = "Deepavali",                       Category = "Cultural",       Emoji = "🪔" },
                new() { Date = new DateTime(y,12, 25),  Title = "Christmas",                       Category = "Public Holiday", Emoji = "🎄" },
            };
            return events.OrderBy(e => e.Date).ToList();
        }

        public static List<SibuEvent> ForMonth(int year, int month)
        {
            return GetAll().Where(e => e.Date.Year == year && e.Date.Month == month).ToList();
        }
    }
}
