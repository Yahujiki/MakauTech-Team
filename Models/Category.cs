namespace MakauTech.Models
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public override string GetDisplayInfo() => $"{Icon} {Name}";
    }
}