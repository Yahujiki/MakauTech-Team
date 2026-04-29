# 🌿 MakauTech — Team OOP Project

A Sibu tourism platform built with **ASP.NET Core 8 MVC** for the OOP class final project. Players discover Foochow food, climb leaderboards via mini-games, and review places — all gamified.

🌐 **Live:** [makautech.com](https://makautech.com)

---

## 👥 Team & branches

| Member | Role | Branch | Status |
|--------|------|--------|--------|
| 🛠️ **Bryan Rozel Bin Leo** | Backend (Models, DbContext, Services, Helpers, Program.cs) | `main` | ✅ pushed |
| 🔧 **Allysha** | Mid-end (Controllers, ViewModels, Hubs) | `allysha-midend` | ⏳ pending |
| 🎨 **Gabriel** | Frontend (Views, CSS, JS) | `gabriel-frontend` | ⏳ pending |

When `allysha-midend` and `gabriel-frontend` are pushed, Bryan merges them into `main`.

---

## 📚 OOP concepts demonstrated

- **Inheritance** (3 levels) — `BaseEntity` → `User` → `Admin`
- **Encapsulation** — `User.Points { get; private set; }` + controlled `AddPoints()`
- **Polymorphism** (4 types):
  1. Abstract method override (`GetDisplayInfo` — overridden in 14+ classes)
  2. Virtual method override (`FoodItem.GetCatchMessage` — 6 subclasses)
  3. Method overloading (`Login()` GET vs `Login(LoginViewModel)` POST)
  4. Runtime polymorphism (`if (user is Admin)`)
- **Exception handling** — try/catch in every controller + `app.UseExceptionHandler` global handler

---

## 🛠️ Tech stack

ASP.NET Core 8 MVC · MySQL 8 · Entity Framework Core · BCrypt · SignalR · Brevo · Cloudflare · Oracle Cloud

---

## 🚀 Run on localhost

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL Server 8.x](https://dev.mysql.com/downloads/installer/)
- Visual Studio 2022 Community OR VS Code with C# extension

### Step 1 — Clone

```bash
git clone https://github.com/Yahujiki/MakauTech-Team.git
cd MakauTech-Team
```

### Step 2 — Create the database

```sql
CREATE DATABASE makautech CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### Step 3 — Configure `appsettings.Development.json`

Create this file at the project root (it's `.gitignored` — your password stays local):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=makautech;User=root;Password=YOUR_MYSQL_PASSWORD;AllowPublicKeyRetrieval=true;SslMode=None;"
  }
}
```

### Step 4 — Restore + run

```bash
dotnet restore
dotnet build
dotnet run
```

Open **`https://localhost:5001`** ✅ — tables auto-created on first run via EF Core migrations.

---

## 🌐 Deploy to production

### Option A — Bryan's existing Oracle Cloud server (recommended for class demo)

The project is already live at **https://makautech.com** running on Oracle Cloud Always Free tier:

```
Browser ─▶ Cloudflare (HTTPS, DNS) ─▶ Nginx :80 ─▶ Kestrel :5000 ─▶ MySQL :3306
                                                          │
                                                          └─▶ Brevo API (transactional email)
```

When team code merges to `main`, Bryan publishes:

```bash
dotnet publish -c Release -o ./bin/publish --runtime linux-x64 --self-contained false
scp -i key.pem -r ./bin/publish/* ubuntu@149.118.158.72:/var/www/makautech/
ssh -i key.pem ubuntu@149.118.158.72 "sudo systemctl restart makautech"
```

Live within 2 minutes.

### Option B — MonsterASP.NET (free .NET hosting)

1. Sign up at [monsterasp.net](https://www.monsterasp.net) — pick free tier
2. Create new site → **.NET 8 runtime**
3. `dotnet publish -c Release -o ./bin/publish`
4. Upload via FileZilla (FTP) using credentials from your dashboard
5. Bind a free MySQL database (MonsterASP includes one)

### Option C — Azure App Service (Student tier)

1. Sign up for [Azure for Students](https://azure.microsoft.com/free/students) — free for 1 year + $100 credit
2. In Visual Studio: right-click project → **Publish** → Azure App Service
3. Create new App Service + Azure MySQL flexible server (free tier)
4. Click **Publish** — auto-deploys to `<name>.azurewebsites.net`

---

## 📋 Team workflow

```
1. Each member receives their .rar + .docx tutorial
2. Extract .rar → open MakauTech_Team.csproj in Visual Studio
3. Follow Phase 1 → 12 in the tutorial:
   - Phase 1-3:  Setup tools + database + appsettings
   - Phase 4-9:  Copy code from tutorial into Visual Studio
   - Phase 10:   Push to YOUR branch on this repo
   - Phase 11:   Pull other branches + run locally
   - Phase 12:   Deploy (optional)
4. Bryan merges all branches → main
5. Class demo from main
```

---

## 🎓 Class presentation

15-minute team pitch — 5 slides:

1. **Title + team intro**
2. **Product overview** (Gabriel)
3. **Inheritance + Encapsulation** (Bryan)
4. **Polymorphism (4 types) + Exception handling** (Allysha)
5. **Live demo + architecture map** (all)

Followed by 10-min Q&A — each member answers their domain.

---

University of Technology Sarawak (UTS) · OOP coursework · 2026
