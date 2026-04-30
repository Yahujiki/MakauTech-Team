using System.Net.Http.Headers;
using System.Net.Http.Json;
using MakauTech.Models;
using Microsoft.Extensions.Options;

namespace MakauTech.Services
{
    /// <summary>
    /// Strongly-typed config bound from <c>appsettings.json → "Brevo"</c> section
    /// (or User Secrets / environment variables in production).
    /// </summary>
    public class BrevoOptions
    {
        public string ApiKey      { get; set; } = string.Empty;
        public string FromEmail   { get; set; } = "noreply@makautech.com";
        public string FromName    { get; set; } = "MakauTech";
        public string SiteBaseUrl { get; set; } = "https://makautech.com";
    }

    /// <summary>Application-level email sender contract.</summary>
    public interface IEmailService
    {
        Task<bool> SendWelcomeEmailAsync(User user, CancellationToken ct = default);
        Task<bool> SendPasswordResetEmailAsync(User user, string resetLink, CancellationToken ct = default);
        Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default);
    }

    /// <summary>
    /// Brevo (Sendinblue) transactional email implementation using the v3 REST API.
    /// Endpoint:  POST https://api.brevo.com/v3/smtp/email
    /// Auth:      api-key header
    /// </summary>
    public class BrevoEmailService : IEmailService
    {
        private const string BrevoEndpoint = "https://api.brevo.com/v3/smtp/email";

        private readonly HttpClient _http;
        private readonly BrevoOptions _opts;
        private readonly ILogger<BrevoEmailService> _log;

        public BrevoEmailService(
            HttpClient http,
            IOptions<BrevoOptions> opts,
            ILogger<BrevoEmailService> log)
        {
            _http = http;
            _opts = opts.Value;
            _log  = log;
        }

        // ─── Public API ────────────────────────────────────────────────────────

        public Task<bool> SendWelcomeEmailAsync(User user, CancellationToken ct = default)
        {
            var subject = "Welcome to MakauTech 🌿 — your Sibu adventure starts here";
            var html    = WelcomeTemplate(user.Name, _opts.SiteBaseUrl);
            return SendEmailAsync(user.Email, user.Name, subject, html, ct);
        }

        public Task<bool> SendPasswordResetEmailAsync(User user, string resetLink, CancellationToken ct = default)
        {
            var subject = "Reset your MakauTech password";
            var html    = PasswordResetTemplate(user.Name, resetLink);
            return SendEmailAsync(user.Email, user.Name, subject, html, ct);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_opts.ApiKey))
            {
                _log.LogError("Brevo API key is not configured. Email to {Email} not sent.", toEmail);
                return false;
            }

            var payload = new
            {
                sender      = new { name = _opts.FromName, email = _opts.FromEmail },
                to          = new[] { new { email = toEmail, name = toName } },
                subject     = subject,
                htmlContent = htmlBody
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, BrevoEndpoint)
            {
                Content = JsonContent.Create(payload)
            };
            req.Headers.TryAddWithoutValidation("api-key", _opts.ApiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                using var resp = await _http.SendAsync(req, ct);
                if (resp.IsSuccessStatusCode)
                {
                    _log.LogInformation("Brevo email sent: subject=\"{Subject}\" → {Email}", subject, toEmail);
                    return true;
                }

                var body = await resp.Content.ReadAsStringAsync(ct);
                _log.LogError("Brevo email failed ({Status}): {Body}", (int)resp.StatusCode, body);
                return false;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Brevo email request threw for {Email}", toEmail);
                return false;
            }
        }

        // ─── HTML templates ────────────────────────────────────────────────────
        // Steam/industrial style — solid colours, no gradients, real photography,
        // structured sections, thin dividers, monospaced section labels.

        // Branded hero photo (Sibu / Borneo). Keep on a stable CDN so Gmail can
        // fetch it. We use Unsplash's direct image URL (always-on, reliable).
        private const string HeroImageUrl =
            "https://images.unsplash.com/photo-1545569310-31ea3df0ec27?auto=format&fit=crop&w=1200&q=70";

        private const string SibuFoodImageUrl =
            "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=400&q=70";

        private const string SibuGameImageUrl =
            "https://images.unsplash.com/photo-1493711662062-fa541adb3fc8?auto=format&fit=crop&w=400&q=70";

        private const string SibuRiverImageUrl =
            "https://images.unsplash.com/photo-1518998053901-5348d3961a04?auto=format&fit=crop&w=400&q=70";

        // Master shell shared by every transactional template.
        private static string Shell(string preheader, string heroImageUrl, string innerHtml)
        {
            // Pre-build the optional hero image row so we don't nest verbatim
            // interpolated strings (the C# parser can't handle that cleanly).
            var heroRow = string.IsNullOrEmpty(heroImageUrl)
                ? ""
                : $@"<tr><td style=""padding:0;""><img src=""{heroImageUrl}"" alt="""" width=""620"" style=""display:block;width:100%;max-width:620px;height:auto;border:0;""/></td></tr>";

            return $@"<!doctype html>
<html lang=""en""><head>
<meta charset=""utf-8""/>
<meta name=""viewport"" content=""width=device-width, initial-scale=1""/>
<meta name=""color-scheme"" content=""dark light""/>
<meta name=""supported-color-schemes"" content=""dark light""/>
<title>MakauTech</title>
</head>
<body style=""margin:0;padding:0;background:#0e0f10;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:#dcdedf;"">
<!-- Inbox preview text -->
<div style=""display:none;max-height:0;overflow:hidden;mso-hide:all;font-size:1px;line-height:1px;color:#0e0f10;opacity:0;"">{preheader}</div>

<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background:#0e0f10;padding:32px 12px;"">
  <tr><td align=""center"">
    <table role=""presentation"" width=""620"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width:620px;width:100%;background:#171a1d;border:1px solid #262a2f;border-radius:6px;overflow:hidden;"">

      <!-- Brand bar (solid, no gradient) -->
      <tr><td style=""background:#0a3d24;padding:18px 28px;border-bottom:2px solid #082314;"">
        <table role=""presentation"" width=""100%""><tr>
          <td align=""left"" style=""color:#ffffff;font-size:18px;font-weight:700;letter-spacing:0.5px;"">
            <span style=""color:#6ee7b7;"">MAKAU</span><span style=""color:#ffffff;"">TECH</span>
          </td>
          <td align=""right"" style=""color:rgba(255,255,255,0.6);font-size:11px;letter-spacing:1.2px;text-transform:uppercase;font-family:ui-monospace,'SF Mono',Menlo,Consolas,monospace;"">
            Sibu / Sarawak
          </td>
        </tr></table>
      </td></tr>

      <!-- Hero image (real photo, full bleed) -->
      {heroRow}

      <!-- Body -->
      <tr><td style=""padding:32px 36px 24px;color:#dcdedf;"">
        {innerHtml}
      </td></tr>

      <!-- Footer -->
      <tr><td style=""background:#0e0f10;border-top:1px solid #262a2f;padding:22px 36px;color:#7d8389;font-size:11px;line-height:1.7;font-family:ui-monospace,'SF Mono',Menlo,Consolas,monospace;letter-spacing:0.3px;"">
        <table role=""presentation"" width=""100%""><tr>
          <td align=""left"" style=""color:#7d8389;font-size:11px;"">
            <strong style=""color:#a8aeb3;font-weight:600;"">MAKAUTECH</strong> · UNIVERSITY OF TECHNOLOGY SARAWAK<br/>
            Sibu · Sarawak · Borneo · Malaysia
          </td>
          <td align=""right"" style=""color:#7d8389;font-size:11px;"">
            <a href=""mailto:support@makautech.com"" style=""color:#6ee7b7;text-decoration:none;"">SUPPORT</a>
            &nbsp;·&nbsp;
            <a href=""https://makautech.com/Home/Privacy"" style=""color:#6ee7b7;text-decoration:none;"">PRIVACY</a>
            &nbsp;·&nbsp;
            <a href=""https://makautech.com/Home/Terms"" style=""color:#6ee7b7;text-decoration:none;"">TERMS</a>
          </td>
        </tr></table>
        <div style=""margin-top:14px;padding-top:14px;border-top:1px solid #1f2326;color:#5a6066;font-size:10.5px;letter-spacing:0.4px;text-transform:uppercase;"">
          You're receiving this because of activity on your MakauTech account.
        </div>
      </td></tr>

    </table>
  </td></tr>
</table>
</body></html>";
        }

        // ── Helper components ──────────────────────────────────────────────────
        private static string SectionLabel(string text) =>
            $@"<div style=""font-family:ui-monospace,'SF Mono',Menlo,Consolas,monospace;font-size:11px;font-weight:700;letter-spacing:1.4px;color:#6ee7b7;text-transform:uppercase;margin-bottom:10px;"">{text}</div>";

        private static string Divider() =>
            @"<div style=""height:1px;background:#262a2f;margin:24px 0;line-height:1px;font-size:1px;"">&nbsp;</div>";

        private static string SolidButton(string href, string label) => $@"
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin:24px auto;""><tr>
  <td style=""background:#10b981;border:1px solid #059669;border-radius:4px;"">
    <a href=""{href}"" style=""display:inline-block;color:#ffffff;text-decoration:none;font-size:14px;font-weight:700;letter-spacing:0.6px;padding:14px 38px;text-transform:uppercase;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;"">
      {label}
    </a>
  </td>
</tr></table>";

        // ── Welcome email (Steam-style with feature cards) ────────────────────
        private static string WelcomeTemplate(string name, string siteUrl)
        {
            var safe = System.Web.HttpUtility.HtmlEncode(name ?? "Explorer");
            var inner = $@"
{SectionLabel("Account Activated")}
<h1 style=""font-size:26px;font-weight:700;color:#ffffff;margin:0 0 12px;letter-spacing:-0.3px;line-height:1.2;"">
  Welcome aboard, {safe}.
</h1>
<p style=""color:#a8aeb3;font-size:14.5px;line-height:1.7;margin:0 0 4px;"">
  Your MakauTech account is now active. You've joined a growing community of explorers documenting Sibu's hidden food spots, riverside trails, and Foochow heritage.
</p>

{Divider()}

{SectionLabel("What's available to you")}
<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr>
  <td valign=""top"" width=""33%"" style=""padding:0 6px 0 0;"">
    <div style=""background:#1f2326;border:1px solid #2a2f34;border-radius:4px;padding:14px;"">
      <img src=""{SibuFoodImageUrl}"" width=""160"" alt="""" style=""display:block;width:100%;height:80px;object-fit:cover;border-radius:3px;border:0;""/>
      <div style=""color:#6ee7b7;font-size:10.5px;font-weight:700;letter-spacing:1.3px;margin-top:10px;font-family:ui-monospace,'SF Mono',Menlo,Consolas,monospace;text-transform:uppercase;"">PLACES</div>
      <div style=""color:#ffffff;font-size:13.5px;font-weight:600;margin-top:3px;line-height:1.4;"">Curated food &amp; sights</div>
    </div>
  </td>
  <td valign=""top"" width=""33%"" style=""padding:0 3px;"">
    <div style=""background:#1f2326;border:1px solid #2a2f34;border-radius:4px;padding:14px;"">
      <img src=""{SibuGameImageUrl}"" width=""160"" alt="""" style=""display:block;width:100%;height:80px;object-fit:cover;border-radius:3px;border:0;""/>
      <div style=""color:#6ee7b7;font-size:10.5px;font-weight:700;letter-spacing:1.3px;margin-top:10px;font-family:ui-monospace,'SF Mono',Menlo,Consolas,monospace;text-transform:uppercase;"">GAMES</div>
      <div style=""color:#ffffff;font-size:13.5px;font-weight:600;margin-top:3px;line-height:1.4;"">4 minigames · 50 pts each</div>
    </div>
  </td>
  <td valign=""top"" width=""33%"" style=""padding:0 0 0 6px;"">
    <div style=""background:#1f2326;border:1px solid #2a2f34;border-radius:4px;padding:14px;"">
      <img src=""{SibuRiverImageUrl}"" width=""160"" alt="""" style=""display:block;width:100%;height:80px;object-fit:cover;border-radius:3px;border:0;""/>
      <div style=""color:#6ee7b7;font-size:10.5px;font-weight:700;letter-spacing:1.3px;margin-top:10px;font-family:ui-monospace,'SF Mono',Menlo,Consolas,monospace;text-transform:uppercase;"">REVIEWS</div>
      <div style=""color:#ffffff;font-size:13.5px;font-weight:600;margin-top:3px;line-height:1.4;"">Earn pts for honest reviews</div>
    </div>
  </td>
</tr></table>

{Divider()}

{SectionLabel("Get started in 60 seconds")}
<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr>
  <td style=""padding:6px 0;color:#dcdedf;font-size:14px;line-height:1.6;""><span style=""color:#6ee7b7;font-family:ui-monospace,'SF Mono',Menlo,Consolas,monospace;font-weight:700;"">01</span> &nbsp; Browse curated Sibu places &amp; cafés.</td>
</tr><tr>
  <td style=""padding:6px 0;color:#dcdedf;font-size:14px;line-height:1.6;""><span style=""color:#6ee7b7;font-family:ui-monospace,'SF Mono',Menlo,Consolas,monospace;font-weight:700;"">02</span> &nbsp; Play any minigame to earn your first points.</td>
</tr><tr>
  <td style=""padding:6px 0;color:#dcdedf;font-size:14px;line-height:1.6;""><span style=""color:#6ee7b7;font-family:ui-monospace,'SF Mono',Menlo,Consolas,monospace;font-weight:700;"">03</span> &nbsp; Write your first review for a +10 pts bonus.</td>
</tr></table>

{SolidButton(siteUrl, "Open MakauTech")}

<p style=""color:#7d8389;font-size:12px;line-height:1.6;margin:14px 0 0;text-align:center;"">
  Bookmark <a href=""{siteUrl}"" style=""color:#6ee7b7;text-decoration:none;"">{siteUrl}</a> for one-click access.
</p>";
            return Shell("Your MakauTech account is ready — Sibu, Sarawak.", HeroImageUrl, inner);
        }

        // ── Password reset email (security-focused, no hero image) ─────────────
        private static string PasswordResetTemplate(string name, string resetLink)
        {
            var safe = System.Web.HttpUtility.HtmlEncode(name ?? "Explorer");
            var inner = $@"
{SectionLabel("Account Security")}
<h1 style=""font-size:24px;font-weight:700;color:#ffffff;margin:0 0 12px;letter-spacing:-0.3px;line-height:1.2;"">
  Reset your password.
</h1>
<p style=""color:#a8aeb3;font-size:14.5px;line-height:1.7;margin:0 0 4px;"">
  Hi {safe}, we received a request to reset the password on your MakauTech account. Use the button below to choose a new one — the link expires in <strong style=""color:#ffffff;"">1 hour</strong>.
</p>

{SolidButton(resetLink, "Reset Password")}

{Divider()}

{SectionLabel("If this wasn't you")}
<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr>
  <td style=""background:#1f2326;border:1px solid #2a2f34;border-left:3px solid #f59e0b;border-radius:4px;padding:16px 18px;"">
    <div style=""color:#ffffff;font-weight:600;font-size:13.5px;margin-bottom:6px;"">No action needed.</div>
    <div style=""color:#a8aeb3;font-size:13px;line-height:1.6;"">
      Your password stays the same. The link will expire on its own. If you keep seeing reset emails you didn't request, contact <a href=""mailto:support@makautech.com"" style=""color:#6ee7b7;text-decoration:none;"">support@makautech.com</a>.
    </div>
  </td>
</tr></table>

{Divider()}

{SectionLabel("Manual link")}
<p style=""color:#7d8389;font-size:11.5px;line-height:1.7;margin:0;font-family:ui-monospace,'SF Mono',Menlo,Consolas,monospace;word-break:break-all;"">
  {resetLink}
</p>";
            return Shell("Reset your MakauTech password — link expires in 1 hour.", "", inner);
        }
    }
}
