# 🌿 MakauTech

A Sibu tourism platform built with **ASP.NET Core 8 MVC**. Players discover Foochow food, climb leaderboards via mini-games, and review places — all gamified.

🌐 **Live:** [makautech.com](https://makautech.com)

---

## 🚨 TEAM WORKFLOW & RULES — READ FIRST

> **⚠️ These rules are NON-NEGOTIABLE. Breaking them corrupts the repository and wastes everyone's time.**

### 🔒 RULE 1 — STRICT BRANCHING (do NOT push to main)

**Only the Technical Lead pushes to `main`.**

If you are a Mid-end or Frontend developer, you must **NEVER** run:

```
git push origin main
```

Doing so will overwrite the integrated codebase. The Technical Lead handles all merges into `main` after reviewing your branch.

---

### 🔒 RULE 2 — ROLE-LOCKED BRANCHES

| Your role | Your branch | Push allowed? |
|-----------|-------------|:-:|
| **Technical Lead** | `main` | ✅ |
| **Mid-end developer** | `midend` | ✅ |
| **Frontend developer** | `frontend` | ✅ |
| Any role → any other branch | — | ❌ |

You MUST push **only** to your assigned branch:

- Mid-end developer pushes → `midend` (and nothing else)
- Frontend developer pushes → `frontend` (and nothing else)

The exact commands are in **Phase 10 of your `.docx` tutorial** — copy them character-for-character.

---

### 🔒 RULE 3 — DO NOT USE `git clone`

**❌ Forbidden command:**

```
git clone https://github.com/Yahujiki/MakauTech-Team.git
```

**Why:** You already have a pre-extracted `.rar` folder on your Desktop. `git clone` will create a duplicate folder elsewhere and confuse your local setup.

**✅ Correct workflow:**

1. Right-click your `.rar` file → **Extract Here**
2. Open the extracted folder (e.g., `MakauTech_Allysha_MidEnd`)
3. Click the address bar at the top of File Explorer, type `powershell`, press **Enter**
4. The PowerShell window opens **inside that folder**
5. Run the Git commands listed in **Phase 10 of your `.docx` tutorial**, line by line

That is it. Your folder is already a project — you just need to attach it to GitHub via the commands.

---

### 🔒 RULE 4 — DO NOT MERGE YOUR OWN CODE

**❌ Do NOT run any of these:**

```
git merge ...
git pull origin main          (use git fetch + git checkout instead)
git rebase ...
git cherry-pick ...
```

**Why:** Merging requires understanding which lines to keep when conflicts happen. Wrong choices delete other developers work.

**✅ The Technical Lead handles all merges.** When you finish your branch, the Lead pulls it into `main`, resolves any conflicts, runs the tests, and deploys.

Your only job is:

1. Build your part
2. Push to **your** branch
3. Tell the Lead you are done

---

### 🔒 RULE 5 — DO NOT COMMIT SECRETS

Never commit these files:

| File | Why |
|------|-----|
| `appsettings.Development.json` | Contains your local MySQL password |
| `*.user` (e.g., `MakauTech.csproj.user`) | Personal IDE settings |
| `bin/`, `obj/`, `.vs/` | Build artifacts (huge, regenerated automatically) |
| Any file with API keys, tokens, or passwords | Public repo = public secrets |

The `.gitignore` should handle this automatically. If you see those files appearing in `git status`, **stop and ask the Technical Lead**.

---

### 🔒 RULE 6 — STUCK? ASK. DO NOT IMPROVISE.

If a command throws an error, **screenshot it and tag the Technical Lead in the team chat**. Do NOT:

- Run random commands you found online
- Force-push (`git push --force`)
- Delete `.git` folder
- Ask AI to "fix" your repo

Wrong fixes can wipe other developers commits.

---

## 📂 Project structure

This repo is organised into role-based branches:

| Branch | Layer | Contents |
|--------|-------|----------|
| `main` | **Backend** | Models · Data · Services · Helpers · Program.cs |
| `midend` | **Mid-end** | Controllers · ViewModels · Hubs |
| `frontend` | **Frontend** | Views · CSS · JS |

Final merged code lives on `main` after each branch is integrated by the Technical Lead.

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
- [MySQL Server 8.x](https://dev.mysql.com/downloads/installer/) — **NOT SQL Server**
- Visual Studio 2022 Community OR VS Code with C# extension

### Step 1 — Get the code (Technical Lead only)

```bash
git clone https://github.com/Yahujiki/MakauTech-Team.git
cd MakauTech-Team
```

> Mid-end / Frontend developers: skip this step. You already have your folder from the `.rar`. See `RULE 3`.

### Step 2 — Create the database

```sql
CREATE DATABASE makautech CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### Step 3 — Configure `appsettings.Development.json`

Create at the project root (gitignored — your password stays local):

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

### Option A — Existing Oracle Cloud server (recommended for class demo)

The site already runs at https://makautech.com on Oracle Cloud Always Free tier:

```
Browser ─▶ Cloudflare ─▶ Nginx :80 ─▶ Kestrel :5000 ─▶ MySQL :3306
                                              │
                                              └─▶ Brevo API (transactional email)
```

After main has all merged code, the Technical Lead publishes:

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

## 📋 Workflow summary

```
1. Each developer receives their .rar + .docx tutorial
2. Extract .rar → open MakauTech_Team.csproj in Visual Studio
3. Follow Phase 1 → 12 in the tutorial:
   • Phase 1-3:  Setup tools + database + appsettings
   • Phase 4-9:  Copy code from tutorial into Visual Studio
   • Phase 10:   Push to YOUR branch (midend / frontend)
   • Phase 11:   (Lead only) Pull other branches + run locally
   • Phase 12:   (Lead only) Deploy
4. Lead merges all branches → main
5. Class demo from main
```

---

University of Technology Sarawak (UTS) · OOP coursework · 2026
