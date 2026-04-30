namespace MakauTech.Models
{
    /// <summary>User tapped "like" on a place (needed for review bonus points).</summary>
    public class PlaceLike : BaseEntity
    {
        public int UserId { get; set; }
        public int PlaceId { get; set; }
        public User? User { get; set; }
        public Place? Place { get; set; }

        public override string GetDisplayInfo() => $"Like — User {UserId} → Place {PlaceId}";
    }
}
