namespace MakauTech.Models
{
    public class Admin : User
    {
        public string Role { get; set; } = "Admin";
        public DateTime LastLogin { get; set; } = DateTime.Now;

        public bool CanDeleteReview(DateTime reviewDate)
        {
            if (Role == "SuperAdmin") return true;
            return (DateTime.Now - reviewDate).TotalDays <= 30;
        }

        public override string GetDisplayInfo() => $"[{Role}] {Name} — Last Login: {LastLogin:dd MMM yyyy}";
    }
}