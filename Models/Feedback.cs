namespace MakauTech.Models
{
    public class Feedback : BaseEntity
    {
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; } = 5;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public User? User { get; set; }

        public override string GetDisplayInfo() => $"Feedback by {UserName} — {Rating}/5: {Subject}";
    }
}
