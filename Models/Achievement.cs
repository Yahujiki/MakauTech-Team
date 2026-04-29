namespace MakauTech.Models
{
    public class Achievement : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int PointsRequired { get; set; } = 0;
        public int PlacesRequired { get; set; } = 0;

        public bool IsUnlockedBy(User user)
        {
            return user.Points >= PointsRequired && user.VisitedPlaceIds.Count >= PlacesRequired;
        }

        public override string GetDisplayInfo() => $"{Icon} {Name} — {Description}";
    }
}