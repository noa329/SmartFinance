# SmartFinance

SmartFinance is a personal finance management REST API built with ASP.NET Core (.NET 9). It helps individual users track expenses, set category-based budgets, and stay on top of their spending. Its standout feature is **automatic expense capture**: it connects to a user's Gmail account, pulls in invoice/receipt emails, and uses Google's Gemini AI to extract structured expense data (merchant, amount, date, installments, recurring charges). It also supports budget calculation from income, spending-risk prediction, event-based budgets, and daily email alert summaries. It's aimed at everyday users who want an automated, low-effort way to understand and control their finances.

> **Note:** This repository contains the **backend API only**. The API is CORS-configured for a React (Vite) client at `http://localhost:5173`, which appears to be maintained in a separate repository. See [Known Limitations](#known-limitations--todo).

## Key Features

- **JWT authentication** with email/password registration and login, plus **Google Sign-In** (Google ID token verification).
- **Gmail integration** – connect a Gmail account via OAuth (offline/refresh token), then scan the inbox for invoices and receipts.
- **AI-powered invoice/receipt parsing** – uses Google Gemini to extract expense details from email text and PDF attachments (via PdfPig for PDF handling).
- **Manual receipt text processing** – submit raw receipt text to be parsed and stored as an expense.
- **Expense management** – full CRUD, installment tracking, recurring charges, and linking expenses to events.
- **Budgets** – per-category percentage-based budgets with alert thresholds, plus budget calculation from monthly/yearly income.
- **Categories** – system and user-defined categories, with automatic categorization of new expenses (including Google Places lookups to help classify merchants).
- **Events** – time-bound budgets (e.g. a trip or a party) with per-event spending statistics.
- **Alerts** – in-app alerts with read/unread tracking and budget-overrun notifications.
- **Spending-risk prediction** – estimates how many days remain before the user is projected to exceed their budget.
- **Daily background sync** – a hosted background service runs once a day (20:00 local time) to fetch new invoices per user and send a consolidated email summary of accumulated alerts (via SMTP/MailKit).
- **Swagger/OpenAPI** documentation with Bearer auth support (Development environment).

## Tech Stack

**Backend**
- Language/Runtime: C# on **.NET 9** (ASP.NET Core Web API)
- ORM / Database: **Entity Framework Core 9** with **SQL Server**
- Authentication: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) + **Google OAuth / Google Sign-In** (`Google.Apis.Auth`)
- Object mapping: **AutoMapper**
- API docs: **Swashbuckle (Swagger)**

**External APIs & Integrations**
- **Google Gemini** (`generativelanguage.googleapis.com`) – AI extraction of expense data from text and PDFs
- **Gmail API** (`Google.Apis.Gmail.v1`) – reading invoice/receipt emails
- **Google Places API** (`places.googleapis.com`) – merchant/category classification
- **SMTP email** via **MailKit / MimeKit** – alert and daily-summary emails
- **PdfPig** – PDF invoice parsing
- **Newtonsoft.Json** – JSON (de)serialization for external API payloads

**Frontend**
- React + Vite (expected, based on CORS config for `http://localhost:5173`) — _not included in this repository_.

## Architecture Overview

The solution follows a layered / clean-architecture style split across five projects:

```
React client (Vite, :5173)  ──HTTP/JSON──▶  SmartFinance (ASP.NET Core Web API)
                                                  │
                                                  ▼
                                            Service layer (business logic,
                                            AI parsing, Gmail sync, email,
                                            prediction, background jobs)
                                                  │
                                                  ▼
                                            Repository layer (EF Core repos)
                                                  │
                                                  ▼
                                            DataContext (EF Core DbContext
                                            + migrations)  ──▶  SQL Server
```

- **SmartFinance** – Web API host: controllers, middleware, DI/extension wiring, JWT/CORS/Swagger config, and the daily background service registration.
- **Service** – business logic and integrations (users, budgets, categories, expenses, events, alerts, Gmail invoice sync, AI parsing, Google Places, email, prediction).
- **Repository** – generic EF Core repositories and domain entities.
- **DataContext** – `SmartFinanceContext` (EF Core `DbContext`) and database migrations.
- **Common** – shared DTOs, enums, and configuration models (e.g. `EmailSettings`).

Cross-cutting: exception-handling middleware translates errors into HTTP responses; a hosted `DailySyncBackgroundService` triggers `SyncService` once per day.

## Prerequisites

- **.NET 9 SDK**
- **SQL Server** (LocalDB, Express, or full) reachable via a connection string
- **EF Core CLI tools** (for migrations): `dotnet tool install --global dotnet-ef`
- Valid credentials for the external services you intend to use:
  - Google OAuth Client ID/Secret (Gmail + Google Sign-In)
  - Google Gemini API key
  - Google Places API key
  - An SMTP account for outgoing email
- **Node.js 18+** — only if/when running the separate React frontend (not in this repo).

## Installation & Setup

### Backend

1. Clone the repository and enter it:

```bash
git clone <your-repo-url>
cd SmartFinance
```

2. Restore dependencies:

```bash
dotnet restore
```

3. Create configuration. `appsettings.json` is intentionally **git-ignored** (it holds secrets), so create `SmartFinance/appsettings.json` with the keys from the [Environment Variables](#environment-variables) table. Example skeleton (no real secrets):

```json
{
  "ConnectionStrings": {
    "SmartFinanceDB": "Server=localhost;Database=SmartFinanceDB;Trusted_Connection=True;TrustServerCertificate=True;"
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
    "SmtpServer": "smtp.example.com",
    "Port": 587,
    "SenderEmail": "<sender@example.com>",
    "SenderPassword": "<smtp-password-or-app-password>"
  }
}
```

4. Apply database migrations:

```bash
dotnet ef database update --project DataContext --startup-project SmartFinance
```

### Frontend

Not included in this repository. If you have the separate React client, follow its own README (it should point its API base URL at the backend below and run on `http://localhost:5173`).

## Environment Variables

These are read from configuration (`appsettings.json` / environment variables). Nested keys use `:` (or `__` when set as OS environment variables). No real values are shown.

| Name | Description | Required |
| --- | --- | --- |
| `ConnectionStrings:SmartFinanceDB` | SQL Server connection string for the app database | Yes |
| `JwtSettings:Key` | Secret key used to sign/validate JWT access tokens | Yes |
| `JwtSettings:Issuer` | JWT issuer (validated) | Yes |
| `JwtSettings:Audience` | JWT audience (validated) | Yes |
| `Authentication:Google:ClientId` | Google OAuth client ID (Gmail connect + Google Sign-In); app fails to start if missing | Yes |
| `Authentication:Google:ClientSecret` | Google OAuth client secret; app fails to start if missing | Yes |
| `Gemini:ApiKey` | Google Gemini API key for AI invoice/receipt parsing | Yes (for invoice/receipt parsing) |
| `Google:ApiKey` | Google Places API key for merchant/category classification | Yes (for Places-based categorization) |
| `EmailSettings:SmtpServer` | SMTP host for outgoing alert/summary emails | Yes (for email features) |
| `EmailSettings:Port` | SMTP port (e.g. 587 for STARTTLS) | Yes (for email features) |
| `EmailSettings:SenderEmail` | From/authentication email address | Yes (for email features) |
| `EmailSettings:SenderPassword` | SMTP password or app password | Yes (for email features) |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET environment (`Development` enables Swagger) | Optional |

> The Google Client ID/Secret are validated at startup — the API will throw on boot if they are missing. Gemini/Places/SMTP keys are validated lazily when their respective features are used.

## Running Locally (Dev Mode)

Start the backend API:

```bash
dotnet run --project SmartFinance
```

Default URLs (from `launchSettings.json`):

- HTTP: `http://localhost:5203`
- HTTPS: `https://localhost:7215`

In the `Development` environment, Swagger UI is available at:

```
https://localhost:7215/swagger
```

To run the HTTPS profile explicitly:

```bash
dotnet run --project SmartFinance --launch-profile https
```

Run the frontend (separate repo) on `http://localhost:5173` so it matches the configured CORS policy.

## Project Structure

```
SmartFinance/
├── SmartFinance.sln          # Visual Studio solution
├── SmartFinance/             # ASP.NET Core Web API host (controllers, middleware, DI/extensions, Program.cs)
├── Service/                  # Business logic & integrations (Gmail, Gemini AI, Places, email, prediction, background sync)
├── Repository/               # Domain entities + generic EF Core repositories
├── DataContext/              # EF Core DbContext (SmartFinanceContext) and migrations
└── Common/                   # Shared DTOs, enums, and configuration models
```

## API Endpoints Overview

Base URL: `http://localhost:5203` (or the HTTPS URL). Most endpoints require a `Bearer` JWT; `[Authorize]` is noted where applicable. `{controller}`-templated routes are shown with their resolved paths.

### Users & Auth
| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| POST | `/api/User/register` | Register a new user | Public |
| POST | `/api/User/login` | Log in with email/password, returns JWT | Public |
| POST | `/api/User/google-login` | Log in / register via Google ID token | Public |
| GET | `/api/User/me` | Get current user | Yes |
| PUT | `/api/User/me` | Update current user | Yes |
| DELETE | `/api/User/me` | Delete current user | Yes |
| GET | `/api/User/profile` | Get full profile of current user | Yes |
| PUT | `/api/User/profile` | Update profile (income, name, phone, alert settings) | Yes |
| GET | `/api/User/all` | List all users | Admin |

### Onboarding / Initial Setup
| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/initial/income` | Set initial monthly/yearly income |
| POST | `/api/initial/categories/init` | Initialize default categories for the user |
| GET | `/api/initial/budget/calculate` | Calculate suggested budgets |
| GET | `/api/initial/budget/calculate-monthly` | Calculate budgets for a given month (`year`, `month`) |
| POST | `/api/initial/fixed-expenses` | Save the user's fixed expenses |

### Expenses
| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/expenses` | List the user's expenses |
| POST | `/api/expenses` | Create an expense |
| PUT | `/api/expenses/{id}` | Update an expense |
| PUT | `/api/expenses/{id}/event` | Link/unlink an expense to an event |
| DELETE | `/api/expenses/{id}` | Delete an expense |

### Budgets
| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/Budget` | List the user's budgets |
| GET | `/api/Budget/{id}` | Get a budget by id |
| POST | `/api/Budget` | Create a budget |
| PATCH | `/api/Budget/{id}` | Update a budget |
| DELETE | `/api/Budget/{id}` | Delete a budget |

### Categories
| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/Category` | List the user's categories |
| GET | `/api/Category/{id}` | Get a category by id |
| POST | `/api/Category` | Create a category |
| PUT | `/api/Category/{id}` | Update a category |
| DELETE | `/api/Category/{id}` | Delete a category |

### Events
| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/events` | List the user's events |
| GET | `/api/events/{id}/stats` | Get spending statistics for an event |
| POST | `/api/events` | Create an event |
| PUT | `/api/events/{id}` | Update an event |
| DELETE | `/api/events/{id}` | Delete an event |

### Alerts
| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/alerts` | List the user's alerts |
| GET | `/api/alerts/unread-count` | Count unread alerts |
| POST | `/api/alerts` | Create an alert |
| PATCH | `/api/alerts/{id}/read` | Mark an alert as read |

### Gmail & Invoices
| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/Gmail/connect-gmail` | Exchange an auth code for a Gmail refresh token and store it |
| POST | `/api/Gmail/disconnect-gmail` | Disconnect the Gmail account |
| POST | `/api/Gmail/test-scan` | Manually scan Gmail and process invoices |
| POST | `/api/Gmail/test-sync` | Manually trigger the daily sync |
| POST | `/api/Invoices/sync` | Sync invoices using a provided Google access token |

### Receipts & Predictions
| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/receipts/process` | Parse raw receipt text and store it as an expense |
| GET | `/api/Prediction/risk-days` | Predicted days until projected budget overrun |

## Known Limitations / TODO

- **Frontend is not in this repository.** The API expects a React (Vite) client at `http://localhost:5173`, but no `package.json`/frontend source is present here. _TODO: add the frontend or link/submodule it._
- **No `appsettings.json` or `.env.example` committed** (config files are git-ignored). New developers must recreate configuration manually. _TODO: add a checked-in `appsettings.example.json`._
- **Leftover project template code**: `WeatherForecastController` + `WeatherForecast` model and empty `Class1.cs` files in several projects (`Service`, `Repository`, `DataContext`, `Common`) can be removed.
- **Commented-out / dead code**: `UserController` contains large blocks of commented-out Google-login/Gmail experiments that should be cleaned up.
- **Localized (Hebrew) strings** are hard-coded in API responses and email templates; there is no i18n layer.
- **Background sync schedule is hard-coded** to 20:00 local time in `DailySyncBackgroundService` (the doc comment mentions `appsettings`, but the time is not actually read from configuration). _TODO: make the schedule configurable._
- **AI parser retry behavior**: `AiInvoiceParserService` uses aggressive retry settings (up to 50 retries with delays) for Gemini rate-limits, which can cause long-running requests.
- **No automated tests** are present in the solution.
- **Inconsistent user-id claim usage**: most controllers read `ClaimTypes.NameIdentifier`, but `PredictionController` reads a custom `"id"` claim — verify token claims are consistent.
- **CORS origin is hard-coded** to `http://localhost:5173`; production origins are not configured.
