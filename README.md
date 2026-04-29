# 🌿 MakauTech

A Sibu tourism platform built with **ASP.NET Core 8 MVC**. Players discover Foochow food, climb leaderboards via mini-games, and review places — all gamified.

🌐 **Live:** [makautech.com](https://makautech.com)

---

## 📂 Project structure

This repo is organised into role-based branches:

| Branch | Layer | Contents |
|--------|-------|----------|
| `main` | **Backend** | Models · Data · Services · Helpers · Program.cs |
| `midend` | **Mid-end** | Controllers · ViewModels · Hubs |
| `frontend` | **Frontend** | Views · CSS · JS |

Final merged code lives on `main` after each layer's branch is integrated.

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

Create at the project root (gitignored):

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

Open **`https://localhost:5001`** ✅ — tables auto-created on first run.

---

## 🌐 Deploy to production

### Option A — Existing Oracle Cloud server

The site already runs at https://makautech.com on Oracle Cloud Always Free tier:

```
Browser ─▶ Cloudflare ─▶ Nginx :80 ─▶ Kestrel :5000 ─▶ MySQL :3306
                                              │
                                              └─▶ Brevo API (transactional email)
```

After main has all merged code, publish:

```bash
dotnet publish -c Release -o ./bin/publish --runtime linux-x64 --self-contained false
scp -i key.pem -r ./bin/publish/* ubuntu@<server-ip>:/var/www/makautech/
ssh -i key.pem ubuntu@<server-ip> "sudo systemctl restart makautech"
```

Live within 2 minutes.

### Option B — MonsterASP.NET (free .NET hosting)

1. Sign up at [monsterasp.net](https://www.monsterasp.net)
2. Create new site → .NET 8 runtime
3. `dotnet publish -c Release -o ./bin/publish`
4. Upload via FTP (FileZilla)

### Option C — Azure App Service (Student tier)

1. Sign up for [Azure for Students](https://azure.microsoft.com/free/students)
2. Visual Studio → right-click project → **Publish** → Azure App Service
3. Auto-deploys to `<name>.azurewebsites.net`

---

## 📋 Workflow

```
1. Each developer receives their .rar + .docx tutorial
2. Extract .rar → open MakauTech_Team.csproj in Visual Studio
3. Follow Phase 1 → 12 in the tutorial:
   • Phase 1-3:  Setup tools + database + appsettings
   • Phase 4-9:  Copy code from tutorial into Visual Studio
   • Phase 10:   Push to YOUR branch (midend / frontend)
   • Phase 11:   Pull other branches + run locally
   • Phase 12:   Deploy (optional)
4. Lead merges all branches → main
5. Class demo from main
```

---

University of Technology Sarawak (UTS) · OOP coursework · 2026
