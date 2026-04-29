namespace MakauTech.Models
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public abstract string GetDisplayInfo();
        public virtual string GetEntityType() => this.GetType().Name;
    }
}
