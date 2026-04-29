namespace MakauTech.Models
{
    /// <summary>
    /// Public daily updates / announcements posted by admin. Visible to all (no auth required).
    /// </summary>
    public class Update : BaseEntity
    {
        public string Title       { get; set; } = string.Empty;
        public string Summary     { get; set; } = string.Empty;
        public string Body        { get; set; } = string.Empty;
        public string? ImageUrl   { get; set; }
        public string AuthorName  { get; set; } = "Admin";
        public bool   IsPublished { get; set; } = true;

        public override string GetDisplayInfo()
            => $"{Title} — {CreatedAt:dd MMM yyyy}";

        public override string GetEntityType() => "Update";
    }
}
