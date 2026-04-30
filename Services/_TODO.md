# Services — Alieya's tasks

Business logic lives here. Controllers should be thin and call services.

---

## P1

- [ ] **BadgeService.cs** — implement unlock logic for every achievement in `Achievement.cs`.
  - Read all achievements in the Models folder first.
  - For each badge, write a method: `CheckAndUnlock(userId)` that checks the condition and awards if met.
  - When unlocked, call `NotificationHub.SendAsync("badgeEarned", ...)` to push to client.
  - Write at least one unit test per badge.

- [ ] **GameService.cs** — extract scoring, anti-cheat, leaderboard logic out of controllers.
  - After you finish this, no controller should directly query `_db.Scores` or similar.
  - Controllers call `_gameService.SubmitScore(...)` or `_gameService.GetLeaderboard(...)`.

- [ ] **PointsService.cs** — central place to add/remove points.
  - Method: `AwardPoints(userId, amount, source, reason)`.
  - Every call writes an audit row: who, when, how much, why.
  - Never modify user points without going through this service.

## P2

- [ ] **TourismService.cs** — add caching for Place queries.
  - Use `IMemoryCache` with 5-minute expiry.
  - Invalidate cache when a Place is created/updated/deleted.
  - Check logs: hot endpoints should be faster after this change.

---

## Rules

1. Services must be **async** (methods end with `Async`, return `Task<T>`).
2. Services don't know about HTTP. No `HttpContext`, no `IActionResult`. Just data in, data out.
3. Inject the DbContext through constructor. Don't use `new ApplicationDbContext()`.
4. One service = one responsibility. If `GameService` is getting huge, split it.

---

## Example skeleton (copy this pattern)

```csharp
public class PointsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PointsService> _log;

    public PointsService(ApplicationDbContext db, ILogger<PointsService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task AwardPointsAsync(int userId, int amount, string source, string reason)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) throw new ArgumentException("User not found");

        user.Points += amount;
        _db.PointsLog.Add(new PointsLog {
            UserId = userId,
            Delta = amount,
            Source = source,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        _log.LogInformation("Awarded {Amount} points to user {UserId} from {Source}", amount, userId, source);
    }
}
```

Register it in `Program.cs`:
```csharp
builder.Services.AddScoped<PointsService>();
```

Use in controller:
```csharp
private readonly PointsService _points;
public MyController(PointsService points) { _points = points; }
// then inside an action:
await _points.AwardPointsAsync(userId, 10, "FoodCatch", "Game completed");
```
