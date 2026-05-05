using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using MakauTech.Data;
using MakauTech.Hubs;
using MakauTech.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddSignalR();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required. Set it via User Secrets, environment variables, or appsettings.Development.json.");
var serverVersion = ServerVersion.AutoDetect(connectionString);
builder.Services.AddDbContext<MakauTechDbContext>(options =>
    options.UseMySql(connectionString, serverVersion,
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// Zero-trust cookie posture.
// __Host- prefix is the strongest cookie pinning the platform offers:
//   * forces Secure
//   * forces Path=/
//   * forbids Domain attribute (no subdomain bleed)
// Combined with HttpOnly + SameSite=Strict this neutralises XSS exfiltration,
// CSRF cross-origin replay, and subdomain cookie-injection attacks.
// Dev (HTTP) gracefully falls back to a non-prefixed name so localhost still works.
var isProd = !builder.Environment.IsDevelopment();
var sessionCookieName    = isProd ? "__Host-MakauTech.Sid"  : ".MakauTech.Session";
var antiforgeryCookieName = isProd ? "__Host-MakauTech.Csrf" : ".MakauTech.Csrf";

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = sessionCookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = antiforgeryCookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
    options.SuppressXFrameOptionsHeader = false;
});

// Brute-force + abuse protection
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login POSTs — per-IP, 10/min (belt-and-braces with account lockout)
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    // Review submissions — per-user, 5/min (anti-spam)
    options.AddPolicy("review", httpContext =>
    {
        var key = httpContext.Session.GetInt32("UserId")?.ToString()
               ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });

    // AI chat — per-user, 30/min
    options.AddPolicy("ai", httpContext =>
    {
        var key = httpContext.Session.GetInt32("UserId")?.ToString()
               ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });
});

// ─── Brevo transactional email (sender: noreply@makautech.com) ────────────
// API key comes from User Secrets (dev) or environment variable BREVO__APIKEY (prod).
// Never commit the key to git.
builder.Services.Configure<BrevoOptions>(builder.Configuration.GetSection("Brevo"));
builder.Services.AddHttpClient<IEmailService, BrevoEmailService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MakauTechDbContext>();
    context.Database.EnsureCreated();
    // Additive schema migrations for columns introduced after initial creation.
    // MySQL syntax: backticks for identifiers, DOUBLE for floats.
    // Additive columns added after initial creation. Probe information_schema first
    // so we don't fire failing ALTERs on every startup (cleaner logs for demos).
    (string Table, string Column, string Sql)[] additive = new[]
    {
        ("Reviews", "ImageUrl",                  "ALTER TABLE `Reviews` ADD COLUMN `ImageUrl` TEXT NULL"),
        ("Places",  "Latitude",                  "ALTER TABLE `Places` ADD COLUMN `Latitude` DOUBLE NULL"),
        ("Places",  "Longitude",                 "ALTER TABLE `Places` ADD COLUMN `Longitude` DOUBLE NULL"),
        ("Users",   "IsOnboarded",               "ALTER TABLE `Users` ADD COLUMN `IsOnboarded` TINYINT(1) NOT NULL DEFAULT 0"),
        ("Users",   "TravelType",                "ALTER TABLE `Users` ADD COLUMN `TravelType` VARCHAR(100) NOT NULL DEFAULT ''"),
        ("Users",   "Interests",                 "ALTER TABLE `Users` ADD COLUMN `Interests` TEXT NOT NULL DEFAULT ''"),
        ("Users",   "FailedLoginAttempts",       "ALTER TABLE `Users` ADD COLUMN `FailedLoginAttempts` INT NOT NULL DEFAULT 0"),
        ("Users",   "LockedUntil",               "ALTER TABLE `Users` ADD COLUMN `LockedUntil` DATETIME NULL"),
        ("Users",   "LastLoginAt",               "ALTER TABLE `Users` ADD COLUMN `LastLoginAt` DATETIME NULL"),
        ("Users",   "UiTutorialSeen",            "ALTER TABLE `Users` ADD COLUMN `UiTutorialSeen` TINYINT(1) NOT NULL DEFAULT 0"),
        ("Users",   "TermsVersionAccepted",      "ALTER TABLE `Users` ADD COLUMN `TermsVersionAccepted` VARCHAR(40) NOT NULL DEFAULT ''"),
        ("Users",   "TermsAcceptedAt",           "ALTER TABLE `Users` ADD COLUMN `TermsAcceptedAt` DATETIME NULL"),
        ("Users",   "PasswordResetToken",        "ALTER TABLE `Users` ADD COLUMN `PasswordResetToken` VARCHAR(128) NULL"),
        ("Users",   "PasswordResetTokenExpires", "ALTER TABLE `Users` ADD COLUMN `PasswordResetTokenExpires` DATETIME NULL"),
        ("Updates", "Category",                  "ALTER TABLE `Updates` ADD COLUMN `Category` VARCHAR(60) NULL"),
        ("Updates", "SourceUrl",                 "ALTER TABLE `Updates` ADD COLUMN `SourceUrl` VARCHAR(500) NULL"),
    };
    foreach (var (table, column, sql) in additive)
    {
        try
        {
            var exists = context.Database
                .SqlQueryRaw<int>(
                    @"SELECT COUNT(*) AS Value FROM information_schema.columns
                       WHERE table_schema = DATABASE()
                         AND table_name = {0}
                         AND column_name = {1}",
                    table, column)
                .AsEnumerable()
                .FirstOrDefault();
            if (exists == 0)
            {
                context.Database.ExecuteSqlRaw(sql);
            }
        }
        catch { /* idempotent: ignore */ }
    }

    // Idempotent table creates — only run if table is missing (cleaner logs).
    bool TableExists(string name)
    {
        try
        {
            return context.Database
                .SqlQueryRaw<int>(
                    @"SELECT COUNT(*) AS Value FROM information_schema.tables
                       WHERE table_schema = DATABASE() AND table_name = {0}",
                    name)
                .AsEnumerable().FirstOrDefault() > 0;
        }
        catch { return true; /* assume exists on probe failure */ }
    }

    if (!TableExists("Feedbacks"))
    {
        try
        {
            context.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS `Feedbacks` (
  `Id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `UserId` INT NULL,
  `UserName` VARCHAR(200) NOT NULL DEFAULT '',
  `Rating` INT NOT NULL DEFAULT 5,
  `Subject` VARCHAR(300) NOT NULL DEFAULT '',
  `Description` TEXT NULL,
  `AttachmentUrl` TEXT NULL,
  `CreatedAt` DATETIME NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }
        catch { }
    }

    if (!TableExists("Updates"))
    {
        try
        {
            context.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS `Updates` (
  `Id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `Title` VARCHAR(300) NOT NULL DEFAULT '',
  `Summary` VARCHAR(500) NOT NULL DEFAULT '',
  `Body` TEXT NOT NULL,
  `ImageUrl` TEXT NULL,
  `AuthorName` VARCHAR(150) NOT NULL DEFAULT 'Admin',
  `IsPublished` TINYINT(1) NOT NULL DEFAULT 1,
  `CreatedAt` DATETIME NOT NULL,
  INDEX `IX_Updates_CreatedAt` (`CreatedAt`),
  INDEX `IX_Updates_IsPublished` (`IsPublished`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }
        catch { }
    }

    MakauTech.Data.DbSeeder.Seed(context, app.Configuration);
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { },
    KnownProxies  = { }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (ctx, next) =>
{
    var headers = ctx.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["X-XSS-Protection"] = "0";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)";
    headers["Cross-Origin-Opener-Policy"] = "same-origin";
    headers["Cross-Origin-Resource-Policy"] = "same-origin";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://unpkg.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com https://unpkg.com; " +
        "font-src 'self' https://cdnjs.cloudflare.com https://fonts.gstatic.com data:; " +
        "img-src 'self' data: https: blob:; " +
        "connect-src 'self' https://api.groq.com https://*.tile.openstreetmap.org; " +
        "frame-src https://www.youtube.com https://www.youtube-nocookie.com; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "object-src 'none';";
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<NotificationHub>("/hubs/notifications");
app.Run();
