using MakauTech.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MakauTech.Data
{
    public class MakauTechDbContext : DbContext
    {
        public MakauTechDbContext(DbContextOptions<MakauTechDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Place> Places { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<PlaceLike> PlaceLikes { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Update> Updates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── TPH inheritance ──
            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<User>("User")
                .HasValue<Admin>("Admin");

            // ── User indexes ──
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // ── List<int> value conversion (VisitedPlaceIds) ──
            modelBuilder.Entity<User>()
                .Property(u => u.VisitedPlaceIds)
                .HasConversion(
                    v => string.Join(',', v),
                    v => ParseIntList(v)
                )
                .Metadata.SetValueComparer(new ValueComparer<List<int>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    v => v.Aggregate(0, (h, i) => HashCode.Combine(h, i)),
                    v => v.ToList()));

            // ── List<string> value conversion (Badges) ──
            modelBuilder.Entity<User>()
                .Property(u => u.Badges)
                .HasConversion(
                    v => string.Join(',', v),
                    v => string.IsNullOrWhiteSpace(v) || v == "[]" ? new List<string>() :
                         v.Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => s.Trim()).Where(s => s != "").ToList()
                )
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                    v => v.ToList()));

            // ── List<string> value conversion (Interests) ──
            modelBuilder.Entity<User>()
                .Property(u => u.Interests)
                .HasConversion(
                    v => string.Join(',', v),
                    v => string.IsNullOrWhiteSpace(v) ? new List<string>() :
                         v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                )
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                    v => v.ToList()));

            // ── PlaceLike unique constraint ──
            modelBuilder.Entity<PlaceLike>()
                .HasIndex(l => new { l.UserId, l.PlaceId })
                .IsUnique();

            // ── Place indexes ──
            modelBuilder.Entity<Place>()
                .HasIndex(p => p.CategoryId);

            modelBuilder.Entity<Place>()
                .HasIndex(p => p.Name);

            // ── Review indexes + relationships ──
            modelBuilder.Entity<Review>()
                .HasIndex(r => r.PlaceId);

            modelBuilder.Entity<Review>()
                .HasIndex(r => r.UserId);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Place)
                .WithMany()
                .HasForeignKey(r => r.PlaceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── PlaceLike relationships ──
            modelBuilder.Entity<PlaceLike>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlaceLike>()
                .HasOne(l => l.Place)
                .WithMany()
                .HasForeignKey(l => l.PlaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Place → Category relationship ──
            modelBuilder.Entity<Place>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Feedback ──
            modelBuilder.Entity<Feedback>()
                .HasIndex(f => f.UserId);

            // ── Category unique name ──
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // ── Achievement unique name ──
            modelBuilder.Entity<Achievement>()
                .HasIndex(a => a.Name)
                .IsUnique();
        }

        private static List<int> ParseIntList(string v)
        {
            if (string.IsNullOrWhiteSpace(v) || v == "[]") return new List<int>();
            var result = new List<int>();
            foreach (var part in v.Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out var n) && n >= 0)
                    result.Add(n);
            }
            return result;
        }
    }
}
