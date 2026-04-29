namespace MakauTech.Models
{
    public class Place : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public double Rating { get; set; } = 0.0;
        public int VisitCount { get; set; } = 0;
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public override string GetDisplayInfo() => $"{Name} — {Location} (Rating: {Rating:F1})";
    }
}