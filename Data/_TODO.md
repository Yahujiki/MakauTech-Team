# Data — Alieya's tasks

Database migrations + seed data.

---

## P1

- [ ] Add indexes on columns that are queried a lot.
  - In `ApplicationDbContext`, inside `OnModelCreating`:
    ```csharp
    modelBuilder.Entity<Review>().HasIndex(r => r.UserId);
    modelBuilder.Entity<Review>().HasIndex(r => r.PlaceId);
    modelBuilder.Entity<Review>().HasIndex(r => r.CreatedAt);
    modelBuilder.Entity<PlaceLike>().HasIndex(l => new { l.UserId, l.PlaceId });
    ```
  - Run migration:
    ```bash
    dotnet ef migrations add AddPerformanceIndexes
    dotnet ef database update
    ```

## P2

- [ ] Write a seed script for dev environment.
  - Create a `DbSeeder.cs` class.
  - On startup (dev only), insert: 5 users, 10 places, 5 categories, all achievements, 20 reviews.
  - Lets new devs / demos have data without manual entry.

---

## Rules

1. **Never edit the database by hand** in SQL. Always use EF migrations.
2. **Always review the generated migration file** before running `database update`. Sometimes EF does weird things.
3. **Don't delete old migrations.** They're part of history.
4. **Back up the dev DB** before running migrations you're unsure about.

---

## Migration cheatsheet

```bash
# Create a new migration
dotnet ef migrations add <Name>

# Apply pending migrations to DB
dotnet ef database update

# Undo last migration (if not applied yet)
dotnet ef migrations remove

# Roll DB back to a specific migration
dotnet ef database update <PreviousMigrationName>
```
