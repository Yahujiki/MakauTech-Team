# 🌿 MakauTech

A Sibu tourism platform built with **ASP.NET Core 8 MVC**. Players discover Foochow food, climb leaderboards via mini-games, and review places — all gamified.

> 🛠️ **Status:** Local build · server deployment planned after class demo.

---

## 📚 OOP concepts demonstrated

- **Inheritance** (3 levels) — `BaseEntity` → `User` → `Admin`
- **Encapsulation** — `User.Points { get; private set; }` + controlled `AddPoints()`
- **Polymorphism** (4 types):
  1. Abstract method override (`GetDisplayInfo`)
  2. Virtual method override (`FoodItem.GetCatchMessage`)
  3. Method overloading (`Login()` GET vs `Login(LoginViewModel)` POST)
  4. Runtime polymorphism (`if (user is Admin)`)
- **Exception handling** — try/catch in every controller + `app.UseExceptionHandler`

---

## 🛠️ Tech stack

ASP.NET Core 8 MVC · MySQL 8 · Entity Framework Core · BCrypt · SignalR · Brevo

---

## 🚀 Run on localhost

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL Server 8.x](https://dev.mysql.com/downloads/installer/)
- Visual Studio 2022 Community

### Step 1 — Create the database

```sql
CREATE DATABASE makautech CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### Step 2 — Configure `appsettings.Development.json`

Create at the project root:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=makautech;User=root;Password=YOUR_MYSQL_PASSWORD;AllowPublicKeyRetrieval=true;SslMode=None;"
  }
}
```

### Step 3 — Run

```bash
dotnet restore
dotnet build
dotnet run
```

Open **`https://localhost:5001`** ✅

Tables auto-created on first run.

---

University of Technology Sarawak (UTS) · OOP coursework · 2026
