# 🔏 SignVault — Digital Signature Platform

A full-stack web app where the **server is the cryptographic signing authority**. Users upload a document, the platform signs it with its private RSA key (SHA-256 + X.509), and **anyone** can later verify the document is authentic and untampered using the public key.

Built with **Angular 20** + **ASP.NET Core (.NET 10) Web API** + **Entity Framework Core**. Runs out of the box on **SQLite** (zero install) and is a one-line change away from MySQL / SQL Server / Oracle / PostgreSQL.

![CI](https://github.com/Anas-Lees/digital-signature-platform/actions/workflows/ci.yml/badge.svg)

> New here? Open [`how-it-works.html`](how-it-works.html) for a 2-minute plain-language overview.
> Signatures are attributed to the signer (e.g. "Signed by Anas"), and **anyone can verify without an account** — by dropping the file or opening a share link.

---

## The three promises

| Promise | Meaning | How it's kept |
|---|---|---|
| **Integrity** | Not one byte changed since signing | SHA-256 hash |
| **Authenticity** | Signed by SignVault, not an impostor | RSA digital signature with a private key only the server holds |
| **Non-repudiation** | The action can't later be denied | Append-only audit log + timestamps |

---

## Tech stack

- **Frontend:** Angular 20 (standalone components, signals), TypeScript, bilingual **English/Arabic (LTR/RTL)** UI
- **Backend:** ASP.NET Core Web API (.NET 10), C#, dependency injection, middleware pipeline
- **Data:** Entity Framework Core + **SQLite** (default) — swappable to MySQL/SQL Server/Oracle/PostgreSQL
- **Auth:** JWT bearer tokens, BCrypt password hashing, role-based access
- **Crypto:** `System.Security.Cryptography` — RSA-3072, SHA-256, self-signed X.509 certificate
- **API docs:** OpenAPI + Scalar UI

```
┌──────────────┐   HTTPS/JSON   ┌────────────────────┐   EF Core   ┌──────────┐
│  Angular SPA │ ─────────────▶ │  ASP.NET Core API   │ ──────────▶ │  SQLite  │
│ (browser UI) │ ◀───────────── │  signing authority  │ ◀────────── │   (DB)   │
└──────────────┘                └─────────┬──────────┘             └──────────┘
                                          │ holds the PRIVATE signing key
                                          ▼
                                   keys/signvault.pfx
```

---

## Quick start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- Angular CLI: `npm install -g @angular/cli`

> No database to install — SQLite is created automatically on first run.

### 1) Run the backend (terminal 1)
```bash
cd backend/SignVault.Api
dotnet run
```
- API: **http://localhost:5080**
- Interactive API docs (Scalar): **http://localhost:5080/scalar/v1**
- On first run it creates the SQLite DB, applies migrations, seeds a demo user, and generates the signing certificate.

### 2) Run the frontend (terminal 2)
```bash
cd frontend
npm install      # first time only
npm start
```
- App: **http://localhost:4200** (opens in Chrome)
- `ng serve` proxies `/api/*` to the backend (see `frontend/proxy.conf.json`).

### 3) Log in
A demo account is seeded automatically:
```
email:    demo@signvault.local
password: Demo1234!
```
(or register a new account from the UI)

### Running in VS Code
Open the `digital-signature-platform` folder, then use two integrated terminals — one for the backend, one for the frontend. Recommended extensions: **C# Dev Kit** and **Angular Language Service**.

---

## How to use it

1. **Log in** with the demo account.
2. **Upload** a file (any document) — its SHA-256 hash is recorded.
3. **Sign** it — the server signs it with its private key; status becomes *Signed*.
4. **Verify** — click *Verify* (or use the public **Verify** page) to confirm it's authentic. Try editing the file and verifying again to see tampering detected.
5. **Toggle العربية / English** in the header to see the RTL/LTR bilingual UI.

---

## API overview

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `POST` | `/api/auth/register` | — | Create an account, get a JWT |
| `POST` | `/api/auth/login` | — | Log in, get a JWT |
| `GET`  | `/api/auth/me` | ✅ | Current user |
| `GET`  | `/api/documents` | ✅ | List my documents |
| `POST` | `/api/documents/upload` | ✅ | Upload a file |
| `POST` | `/api/documents/{id}/sign` | ✅ | Sign a document |
| `GET`  | `/api/documents/{id}/download` | ✅ | Download the original |
| `GET`  | `/api/verify/public-key` | 🌐 | The platform's public key |
| `GET`  | `/api/verify/{documentId}` | 🌐 | Verify the stored copy |
| `POST` | `/api/verify/{documentId}` | 🌐 | Verify an uploaded file (tamper check) |

---

## Security notes

- Passwords are stored only as **BCrypt** hashes (work factor 12) — never plaintext.
- The API authenticates every protected endpoint with a **validated JWT**; ownership is re-checked server-side (you can't sign someone else's document).
- The **private signing key** lives in `keys/signvault.pfx` (git-ignored) and is generated on first run. **In production this belongs in an HSM / key vault, never in a file or in source control.**
- Every signature + audit row commit inside a single **EF Core transaction** (non-repudiation).
- This is a learning/portfolio project. For **legally-binding** signatures use a qualified Certificate Authority and a hardware HSM, and review against eIDAS/ESIGN and local regulations.

---

## Switching the database (the EF Core payoff)

SQLite is the default for zero-setup. To target an enterprise database, change **one provider line** in `Program.cs` and swap the NuGet package — entities, services, and the whole frontend stay untouched:

```csharp
// SQLite (default)
opt.UseSqlite(conn);
// SQL Server   →  package Microsoft.EntityFrameworkCore.SqlServer
opt.UseSqlServer(conn);
// PostgreSQL   →  package Npgsql.EntityFrameworkCore.PostgreSQL
opt.UseNpgsql(conn);
// MySQL        →  package Pomelo.EntityFrameworkCore.MySql
opt.UseMySql(conn, ServerVersion.AutoDetect(conn));
```
Then update the `ConnectionStrings:Default` in `appsettings.json` and regenerate migrations.

---

## Project structure

```
digital-signature-platform/
├── backend/SignVault.Api/
│   ├── Domain/         # entities + enums
│   ├── Data/           # AppDbContext, Migrations, Seed
│   ├── Dtos/           # request/response contracts
│   ├── Services/       # ISigner/RsaSigner, JWT, file store, audit, signing cert
│   ├── Controllers/    # Auth, Documents, Verify
│   └── Program.cs      # DI, JWT, CORS, OpenAPI wiring
├── frontend/
│   └── src/app/
│       ├── core/       # auth service, JWT interceptor, guard, document service, i18n
│       └── features/   # login, register, documents, verify
└── .github/workflows/  # CI: build + test both halves
```

## Testing

```bash
# backend (from backend/SignVault.Api) — add an xUnit test project to expand
dotnet build

# frontend unit tests (Jasmine/Karma)
cd frontend && npm test
```

## License

[MIT](LICENSE) © 2026 Anas-Lees
