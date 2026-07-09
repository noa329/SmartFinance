# SmartFinance 💰

**Smart personal finance management — automatic expense tracking powered by Gmail invoice sync and AI-powered receipt parsing.**

![.NET](https://img.shields.io/badge/.NET%209-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Entity Framework](https://img.shields.io/badge/EF%20Core-512BD4?style=for-the-badge&logo=nuget&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)
![Google Gemini](https://img.shields.io/badge/Google%20Gemini-4285F4?style=for-the-badge&logo=google&logoColor=white)
![React](https://img.shields.io/badge/React%2019-61DAFB?style=for-the-badge&logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)

SmartFinance is a full-stack personal finance platform that removes the most painful part of budgeting: manual data entry. It connects to a user's Gmail account, automatically detects incoming invoices and receipts (including PDF attachments), and uses **Google Gemini AI** to extract structured expense data — merchant, amount, date, installments, and recurring charges. On top of that data it provides category-based budgets, spending-risk prediction, event-based expense planning, proactive alerts, and daily email summaries — all behind secure JWT + Google OAuth authentication.

> **This repository contains the backend (ASP.NET Core Web API).**
> 🎨 **The React frontend lives here: [SmartFinance-Frontend](https://github.com/noa329/SmartFinance-Frontend)** — running the full application requires both repositories (see [Getting Started](#-getting-started)).

## ✨ Key Features

- 🔐 **Authentication** — JWT with email/password registration and login, plus Google Sign-In (Google ID token verification)
- 📧 **Gmail integration** — connect a Gmail account via OAuth (offline/refresh token), then scan the inbox for invoices and receipts
- 🤖 **AI-powered invoice/receipt parsing** — Google Gemini extracts expense details from email text and PDF attachments (PdfPig for PDF handling)
- 🧾 **Manual receipt processing** — submit raw receipt text to be parsed and stored as an expense
- 💸 **Expense management** — full CRUD, installment tracking, recurring charges, and linking expenses to events
- 🎯 **Budgets** — per-category percentage-based budgets with alert thresholds, plus budget calculation from monthly/yearly income
- 🏷️ **Categories** — system and user-defined categories, with automatic categorization (including Google Places merchant lookup)
- 📅 **Events** — time-bound budgets (e.g. a trip or a party) with per-event spending statistics
- 🔔 **Alerts** — in-app alerts with read/unread tracking and budget-overrun notifications
- 🔮 **Spending-risk prediction** — estimates how many days remain before the user is projected to exceed their budget
- 📬 **Daily background sync** — a hosted service runs once a day (20:00 local time) to fetch new invoices per user and send a consolidated email summary of accumulated alerts (via SMTP/MailKit)
- 📖 **Swagger/OpenAPI** documentation with Bearer auth support (Development environment)

## 🛠 Tech Stack

**Backend (this repo)**

| | |
|---|---|
| Language / Runtime | C# on **.NET 9** (ASP.NET Core Web API) |
| ORM / Database | **Entity Framework Core 9** + **SQL Server** |
| Authentication | JWT Bearer + **Google OAuth / Google Sign-In** |
| Object mapping | **AutoMapper** |
| API docs | **Swashbuckle (Swagger)** |

**External APIs & Integrations**

| | |
|---|---|
| AI parsing | **Google Gemini** (`generativelanguage.googleapis.com`) |
| Email sync | **Gmail API** (`Google.Apis.Gmail.v1`) |
| Merchant lookup | **Google Places API** (`places.googleapis.com`) |
| Email delivery | **MailKit / MimeKit** (SMTP) |
| PDF parsing | **PdfPig** |
| JSON | **Newtonsoft.Json** |

**Frontend ([SmartFinance-Frontend](https://github.com/noa329/SmartFinance-Frontend))**

| | |
|---|---|
| Framework | React 19 + TypeScript, built with Vite 8 |
| State | Redux Toolkit |
| UI | Material UI (MUI), Recharts, Lucide icons |
| Routing / Auth | React Router 7, `@react-oauth/google` |
| Interface | Hebrew / RTL |

## 🏗 Architecture

```
┌──────────────────────┐        HTTPS / JSON        ┌───────────────────────────────┐
│  SmartFinance-       │  ─────────────────────────▶ │  SmartFinance API (this repo) │
│  Frontend (React)    │   JWT bearer tokens         │  ASP.NET Core 9               │
└──────────────────────┘                             │                               │
                                                     │  Controllers                  │
        ┌────────────────────┐                       │      │                        │
        │  Google OAuth      │◀──────────────────────│  Service layer                │
        │  Gmail API         │   invoice sync        │  (Gmail sync, Gemini AI,      │
        │  Google Gemini     │   AI parsing          │   Places, email, prediction)  │
        │  Google Places     │   merchant lookup     │      │                        │
        └────────────────────┘                       │  Repository layer             │
                                                     │      │                        │
        ┌────────────────────┐                       │  EF Core DbContext            │
        │  SMTP (MailKit)    │◀──────────────────────│      │                        │
        │  alert emails      │                       └──────────────┬────────────────┘
        └────────────────────┘                                      │
                                                          ┌─────────▼─────────┐
                                                          │    SQL Server     │
                                                          └───────────────────┘
```

The solution follows a layered architecture:

```
SmartFinance.sln
├── SmartFinance/   # API host — controllers, middleware, DI extensions, background service
├── Service/        # Business logic — Gmail sync, Gemini AI parsing, Places, email, prediction
├── Repository/     # Domain entities + generic EF Core repositories
├── DataContext/    # SmartFinanceContext (DbContext) + migrations
└── Common/         # Shared DTOs, enums, and configuration models
```

Cross-cutting: exception-handling middleware translates errors into HTTP responses; a hosted `DailySyncBackgroundService` triggers `SyncService` once per day.

## 🚀 Getting Started

Running the full application requires **both** repositories.

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/) (for the frontend)
- SQL Server (LocalDB, Express, or full instance)
- EF Core CLI: `dotnet tool install --global dotnet-ef`
- A [Google Cloud project](https://console.cloud.google.com/) with OAuth credentials, Gmail API, Gemini API key, and Places API key (see below)

### 1. Backend (this repo)

```bash
git clone https://github.com/noa329/SmartFinance.git
cd SmartFinance
dotnet restore
```

Create `SmartFinance/appsettings.json` (git-ignored — holds secrets). See the [Environment Variables](#-environment-variables) table for all keys. Example skeleton:

```json
{
  "ConnectionStrings": {
    "SmartFinanceDB": "Server=(localdb)\\MSSQLLocalDB;Database=SmartFinance;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "Key": "<long-random-secret>",
    "Issuer": "SmartFinance",
    "Audience": "SmartFinanceClient"
  },
  "Authentication": {
    "Google": {
      "ClientId": "<google-oauth-client-id>",
      "ClientSecret": "<google-oauth-client-secret>"
    }
  },
  "Gemini": {
    "ApiKey": "<gemini-api-key>"
  },
  "Google": {
    "ApiKey": "<google-places-api-key>"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderEmail": "<sender@example.com>",
    "SenderPassword": "<smtp-app-password>"
  }
}
```

Apply migrations and run:

```bash
dotnet ef database update --project DataContext --startup-project SmartFinance
dotnet run --project SmartFinance --launch-profile https
```

Default URLs (from `launchSettings.json`):

| | URL |
|---|---|
| HTTP | `http://localhost:5203` |
| HTTPS | `https://localhost:7215` |
| Swagger UI | `https://localhost:7215/swagger` |

> Google Client ID/Secret are validated at startup — the API will throw on boot if they are missing.

### 2. Frontend

```bash
git clone https://github.com/noa329/SmartFinance-Frontend.git
cd SmartFinance-Frontend
npm install
```

Create a `.env` file:

```env
VITE_API_URL=https://localhost:7215/api
VITE_GOOGLE_CLIENT_ID=<your-google-client-id>.apps.googleusercontent.com
```

Start the dev server:

```bash
npm run dev
```

The app runs at `http://localhost:5173` (the backend CORS policy already allows this origin).

### Google Cloud setup

1. In the [Google Cloud Console](https://console.cloud.google.com/), create a project and enable **Gmail API**, **Places API**, and the **Generative Language API** (Gemini).
2. Under **APIs & Services → Credentials**, create an **OAuth 2.0 Client ID** (Web application) and add `http://localhost:5173` to authorized JavaScript origins.
3. Copy the Client ID and Client Secret into `appsettings.json` (backend) and the same Client ID into `.env` as `VITE_GOOGLE_CLIENT_ID` (frontend).
4. Create API keys for Gemini and Places and add them to `appsettings.json`.

## 🔧 Environment Variables

Read from `appsettings.json` or OS environment variables (nested keys use `:` or `__`).

| Name | Description | Required |
| --- | --- | --- |
| `ConnectionStrings:SmartFinanceDB` | SQL Server connection string | Yes |
| `JwtSettings:Key` | Secret key for signing/validating JWT tokens | Yes |
| `JwtSettings:Issuer` | JWT issuer | Yes |
| `JwtSettings:Audience` | JWT audience | Yes |
| `Authentication:Google:ClientId` | Google OAuth client ID (Gmail + Sign-In) | Yes |
| `Authentication:Google:ClientSecret` | Google OAuth client secret | Yes |
| `Gemini:ApiKey` | Google Gemini API key for AI invoice/receipt parsing | Yes (for parsing) |
| `Google:ApiKey` | Google Places API key for merchant classification | Yes (for Places) |
| `EmailSettings:SmtpServer` | SMTP host for alert/summary emails | Yes (for email) |
| `EmailSettings:Port` | SMTP port (e.g. 587) | Yes (for email) |
| `EmailSettings:SenderEmail` | From/authentication email address | Yes (for email) |
| `EmailSettings:SenderPassword` | SMTP password or app password | Yes (for email) |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET environment (`Development` enables Swagger) | Optional |

## 📡 API Endpoints Overview

Base URL: `https://localhost:7215` (or `http://localhost:5203`). Most endpoints require a `Bearer` JWT.

<details>
<summary><strong>Users & Auth</strong></summary>

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| POST | `/api/User/register` | Register a new user | Public |
| POST | `/api/User/login` | Log in with email/password, returns JWT | Public |
| POST | `/api/User/google-login` | Log in / register via Google ID token | Public |
| GET | `/api/User/me` | Get current user | Yes |
| PUT | `/api/User/me` | Update current user | Yes |
| DELETE | `/api/User/me` | Delete current user | Yes |
| GET | `/api/User/profile` | Get full profile | Yes |
| PUT | `/api/User/profile` | Update profile (income, alerts, etc.) | Yes |

</details>

<details>
<summary><strong>Onboarding / Initial Setup</strong></summary>

| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/initial/income` | Set initial monthly/yearly income |
| POST | `/api/initial/categories/init` | Initialize default categories |
| GET | `/api/initial/budget/calculate` | Calculate suggested budgets |
| GET | `/api/initial/budget/calculate-monthly` | Calculate budgets for a given month |
| POST | `/api/initial/fixed-expenses` | Save fixed expenses |

</details>

<details>
<summary><strong>Expenses, Budgets, Categories, Events, Alerts</strong></summary>

| Method | Route | Description |
| --- | --- | --- |
| GET/POST | `/api/expenses` | List / create expenses |
| PUT/DELETE | `/api/expenses/{id}` | Update / delete an expense |
| GET/POST | `/api/Budget` | List / create budgets |
| PATCH/DELETE | `/api/Budget/{id}` | Update / delete a budget |
| GET/POST | `/api/Category` | List / create categories |
| PUT/DELETE | `/api/Category/{id}` | Update / delete a category |
| GET/POST | `/api/events` | List / create events |
| GET | `/api/events/{id}/stats` | Event spending statistics |
| GET | `/api/alerts` | List alerts |
| GET | `/api/alerts/unread-count` | Count unread alerts |
| PATCH | `/api/alerts/{id}/read` | Mark alert as read |

</details>

<details>
<summary><strong>Gmail, Receipts & Predictions</strong></summary>

| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/Gmail/connect-gmail` | Exchange auth code for Gmail refresh token |
| POST | `/api/Gmail/disconnect-gmail` | Disconnect Gmail account |
| POST | `/api/Gmail/test-scan` | Manually scan Gmail for invoices |
| POST | `/api/receipts/process` | Parse raw receipt text into an expense |
| GET | `/api/Prediction/risk-days` | Predicted days until budget overrun |

</details>

## 📁 Project Structure

```
SmartFinance/
├── SmartFinance.sln          # Visual Studio solution
├── SmartFinance/             # ASP.NET Core Web API host
├── Service/                  # Business logic & integrations
├── Repository/               # Domain entities + EF Core repositories
├── DataContext/              # DbContext + migrations
└── Common/                   # Shared DTOs and configuration models
```

## 🔗 Related Repositories

| Repository | Description |
|---|---|
| **[SmartFinance](https://github.com/noa329/SmartFinance)** (this repo) | ASP.NET Core backend — API, business logic, database |
| **[SmartFinance-Frontend](https://github.com/noa329/SmartFinance-Frontend)** | React + TypeScript client application (Hebrew / RTL) |

## ⚠️ Known Limitations

- **No `appsettings.example.json` committed** — new developers must recreate configuration manually.
- **CORS origin is hard-coded** to `http://localhost:5173`; production origins are not yet configured.
- **Background sync schedule is hard-coded** to 20:00 local time (not read from configuration).
- **No automated tests** in the solution.
- **Hebrew strings** are hard-coded in API responses and email templates; no i18n layer.
- **Leftover template code** (`WeatherForecastController`, `Class1.cs` stubs) can be removed.
