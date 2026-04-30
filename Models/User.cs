namespace MakauTech.Models
{
    public class User : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Points { get; private set; } = 0;
        public List<int> VisitedPlaceIds { get; set; } = new();
        public List<string> Badges { get; set; } = new();

        public bool IsOnboarded { get; set; } = false;
        public string TravelType { get; set; } = string.Empty;
        public List<string> Interests { get; set; } = new();

        /// <summary>Set true after user finishes or skips the post-login UI walkthrough.</summary>
        public bool UiTutorialSeen { get; set; } = false;

        // Terms & privacy acceptance (professional agreement gate).
        public string TermsVersionAccepted { get; set; } = string.Empty;
        public DateTime? TermsAcceptedAt { get; set; }

        // Security fields
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockedUntil { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Password reset (set when user requests "forgot password", cleared on use)
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpires { get; set; }

        public string Level
        {
            get
            {
                if (Points >= 100) return "Gold";
                if (Points >= 50) return "Silver";
                if (Points >= 20) return "Bronze";
                return "Newbie";
            }
        }

        public bool IsLockedOut => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

        public void RecordFailedLogin()
        {
            FailedLoginAttempts++;
            if (FailedLoginAttempts >= 5)
                LockedUntil = DateTime.UtcNow.AddMinutes(15);
        }

        public void RecordSuccessfulLogin()
        {
            FailedLoginAttempts = 0;
            LockedUntil = null;
            LastLoginAt = DateTime.UtcNow;
        }

        public void AddPoints(int amount)
        {
            if (amount <= 0) return;
            Points += amount;
            CheckBadges();
        }

        public bool DeductPoints(int amount)
        {
            if (amount <= 0 || Points < amount) return false;
            Points -= amount;
            return true;
        }

        public bool RecordVisit(int placeId)
        {
            if (VisitedPlaceIds.Contains(placeId)) return false;
            VisitedPlaceIds.Add(placeId);
            CheckBadges();
            return true;
        }

        private void CheckBadges()
        {
            void Give(string b) { if (!Badges.Contains(b)) Badges.Add(b); }
            if (VisitedPlaceIds.Count >= 1)  Give("First Step");
            if (VisitedPlaceIds.Count >= 5)  Give("Food Hunter");
            if (VisitedPlaceIds.Count >= 10) Give("Explorer");
            if (VisitedPlaceIds.Count >= 20) Give("True Native");
            if (Points >= 30)   Give("Active Reviewer");
            if (Points >= 50)   Give("Sibu Expert");
            if (Points >= 100)  Give("Adventurer");
            if (Points >= 200)  Give("Game Master");
            if (Points >= 500)  Give("Legend");
            if (Points >= 1000) Give("Champion");
        }

        public override string GetDisplayInfo() => $"{Name} — Level: {Level} ({Points} pts)";
    }
}