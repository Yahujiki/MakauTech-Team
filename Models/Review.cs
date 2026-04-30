namespace MakauTech.Models
{
    public class Review : BaseEntity
    {
        public int UserId { get; set; }
        public int PlaceId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; } = 5;
        public string Comment { get; set; } = string.Empty;
        /// <summary>Optional user-uploaded photo for this review (stored in /uploads/reviews/).</summary>
        public string? ImageUrl { get; set; }
        public User? User { get; set; }
        public Place? Place { get; set; }
        public override string GetDisplayInfo() => $"Review by {UserName} — {Rating}/5: {Comment}";
        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    }
}