# Bankweave 💰

A privacy-focused personal financial dashboard with **zero-knowledge encryption** that keeps your financial data secure even from database administrators.

## ✨ Key Features

### 🔐 Security & Privacy
- **🔒 Zero-Knowledge Encryption**: Your financial data is encrypted client-side with AES-GCM-256
- **🔑 Password-Derived Keys**: Encryption keys derived from your password (never stored)
- **🛡️ User Authentication**: Secure JWT-based authentication
- **👤 Multi-User Support**: Complete data isolation between users
- **🚫 No External Services**: All data stays on your machine

### 💰 Financial Management
- **📂 CSV Import**: Support for Trading212, Trade Republic, Raisin, Revolut, PTSB, and generic CSV formats
- **📊 Rich Analytics**: Income, expenses, net worth, balance tracking, and trends
- **🎯 Smart Categorization**: Auto-categorize transactions with custom rules
- **📈 What-If Scenarios**: Plan future expenses and see projected balances
- **💡 Insights**: Recurring transactions, essential expenses, and spending patterns

### 🚀 Technology
- **📱 Modern Web UI**: Clean, mobile-friendly interface
- **🐳 Docker Ready**: One-command deployment
- **💾 PostgreSQL**: Reliable data storage
- **⚡ Fast & Responsive**: Built with .NET 8 and vanilla JavaScript

## 🔐 Zero-Knowledge Encryption

Bankweave implements Signal-style zero-knowledge encryption:

**How it works:**
1. Your password derives an encryption key using PBKDF2 (100,000 iterations)
2. All sensitive transaction data is encrypted in your browser before reaching the server
3. The server stores only encrypted blobs - **completely unreadable** even with database access
4. Only you can decrypt your data with your password

**What's encrypted:**
- Transaction descriptions
- Transaction amounts
- Merchant/counterparty names
- Categories

**What's NOT encrypted** (needed for queries):
- Transaction dates
- Account associations
- Timestamps

**Security guarantees:**
- ✅ Encryption key **never** leaves your browser
- ✅ Server **never** sees plaintext financial data
- ✅ Database administrators **cannot** read your transactions
- ✅ Even if database is compromised, data remains secure
- ✅ Key stored only in browser session (cleared on logout)

## Supported Banks

All banks that export CSV statements are supported:
- ✅ Trading 212
- ✅ Trade Republic
- ✅ Raisin
- ✅ Revolut
- ✅ PTSB (Permanent TSB)
- ✅ Any bank with CSV export (use Generic format)

## Prerequisites

1. **Docker & Docker Compose** - [Download](https://www.docker.com/products/docker-desktop)

That's it! No external API keys or bank credentials needed.

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/YourUsername/Bankweave.git
cd Bankweave
```

### 2. Start Bankweave (Development Mode)

```powershell
docker-compose -f docker-compose.dev.yml up -d --build
```

Access the app at: **http://localhost:8082**

### 3. Create Your Account

1. Navigate to http://localhost:8082
2. Click "Get Started" or "Sign Up"
3. Create your account with email and password
4. Login and start importing your financial data

### 4. Import Your Bank Data

1. **Export CSV** from your bank (see [CSV Import Guide](CSV_IMPORT_GUIDE.md))
2. **Open Dashboard** at http://localhost:8082
3. **Click "Import CSV"**
4. **Select your bank** and upload file
5. **View your stats** immediately

## Docker Modes

### Development Mode (Recommended for Development)
```powershell
docker-compose -f docker-compose.dev.yml up -d
```
- Hot reload enabled for C#, HTML, CSS, JS files
- Access: http://localhost:8082
- Database: localhost:5435

### Production Mode
```powershell
docker-compose up -d
```
- Optimized Release build
- Access: http://localhost:8083
- Database: localhost:5436

See [DOCKER_QUICK_START.md](DOCKER_QUICK_START.md) for detailed Docker instructions.

## CSV Import Guide

See **[CSV_IMPORT_GUIDE.md](CSV_IMPORT_GUIDE.md)** for detailed instructions on:
- Exporting CSV from each bank
- Supported CSV formats
- Troubleshooting import issues
- Regular usage patterns

## Project Structure

```
Bankweave/
├── Controllers/              # API endpoints
│   ├── AccountsController.cs     # Account management
│   ├── StatsController.cs        # Financial statistics
│   ├── CsvImportController.cs    # CSV upload & parsing
│   ├── RulesController.cs        # Categorization rules
│   ├── ScenariosController.cs    # What-if scenarios
│   ├── AuthController.cs         # Authentication (login/register/password)
│   └── AdminController.cs        # User management (admin only)
├── Entities/                 # Database models
│   ├── ApplicationUser.cs        # User entity with Identity
│   ├── FinancialAccount.cs       # Bank accounts
│   ├── MoneyMovement.cs          # Transactions
│   ├── CategorizationRule.cs     # Auto-categorization rules
│   └── WhatIfScenario.cs         # Financial scenarios
├── Infrastructure/           # Database context
│   └── AppDbContext.cs           # EF Core + Identity setup
├── Models/                   # DTOs
│   └── AuthDtos.cs               # Auth request/response models
├── wwwroot/                  # Frontend (HTML/CSS/JS)
│   ├── index.html                # Landing page
│   ├── login.html                # Login page
│   ├── register.html             # Registration page
│   ├── dashboard.html            # Main dashboard
│   ├── scenarios.html            # What-if scenario planner
│   ├── admin.html                # Admin panel
│   ├── settings.html             # Account settings
│   └── auth.js                   # Auth utilities
├── docker-compose.yml        # Production configuration
├── docker-compose.dev.yml    # Development with hot reload
├── Dockerfile                # Production container
├── Dockerfile.dev            # Development container
├── CSV_IMPORT_GUIDE.md      # CSV import instructions
├── DOCKER_QUICK_START.md    # Docker usage guide
└── README.md                 # This file
```

## API Endpoints

### Authentication
- `POST /api/auth/register` - Create new user account (returns encryption salt)
- `POST /api/auth/login` - Login and get JWT token (returns encryption salt)
- `POST /api/auth/logout` - Logout (client-side token removal)
- `GET /api/auth/me` - Get current user info
- `POST /api/auth/change-password` - Change password (requires auth)

### Encrypted Transactions (Zero-Knowledge)
- `GET /api/encryptedtransactions` - List all encrypted transactions
- `GET /api/encryptedtransactions/{id}` - Get single encrypted transaction
- `POST /api/encryptedtransactions` - Create encrypted transaction
- `PUT /api/encryptedtransactions/{id}` - Update encrypted transaction
- `DELETE /api/encryptedtransactions/{id}` - Delete encrypted transaction

### Encryption Migration
- `GET /api/encryptionmigration/status` - Check encryption status (encrypted vs unencrypted count)
- `POST /api/encryptionmigration/migrate` - Batch encrypt existing transactions

### Accounts
- `GET /api/accounts` - List all accounts for current user
- `GET /api/accounts/{id}` - Get account details
- `GET /api/accounts/{id}/transactions` - Get transactions for account
- `POST /api/accounts` - Create account manually
- `DELETE /api/accounts/{id}` - Delete account

### Statistics
- `GET /api/stats/overview` - Summary stats (balance, income, expenses, net)

### CSV Import
- `POST /api/csv/import?provider={provider}` - Upload CSV file
  - Providers: `trading212`, `traderepublic`, `raisin`, `revolut`, `ptsb`, `generic`

### Categorization Rules
- `GET /api/rules` - List all rules for current user
- `POST /api/rules` - Create new rule
- `PUT /api/rules/{id}` - Update rule
- `DELETE /api/rules/{id}` - Delete rule
- `POST /api/rules/apply` - Apply all rules to transactions

### What-If Scenarios
- `GET /api/scenarios` - List all scenarios
- `GET /api/scenarios/{id}` - Get scenario details
- `POST /api/scenarios` - Create new scenario
- `PUT /api/scenarios/{id}` - Update scenario
- `DELETE /api/scenarios/{id}` - Delete scenario

### Admin (Requires Admin Role)
- `GET /api/admin/users` - List all users
- `GET /api/admin/users/{id}/stats` - Get user statistics
- `DELETE /api/admin/users/{id}` - Delete user
- `POST /api/admin/users/{id}/lock` - Lock user account
- `POST /api/admin/users/{id}/unlock` - Unlock user account
- `POST /api/admin/users/{id}/roles/{roleName}` - Assign role to user
- `DELETE /api/admin/users/{id}/roles/{roleName}` - Remove role from user

### Health & Diagnostics
- `GET /api/health/db` - Database connection and migration status
- `POST /api/health/migrate` - Apply pending migrations

### Test Data (Development)
- `POST /api/test/seed` - Generate sample data
- `DELETE /api/test/clear-data` - Clear all data

## Dashboard Features

### Overview Stats
- **Total Balance**: Sum across all accounts
- **Income (30 days)**: Total deposits/credits
- **Expenses (30 days)**: Total debits/payments
- **Net (30 days)**: Income - Expenses

### Account Management
- View all accounts with balances
- Click to see transaction history
- Shows provider name and IBAN

### CSV Import
- Simple upload interface
- Select bank provider from dropdown
- Instant feedback on import success
- Duplicate detection built-in

## Docker Commands

```powershell
# Start Bankweave
docker-compose up -d

# Stop Bankweave
docker-compose down

# View logs
docker-compose logs -f web

# Rebuild after code changes
docker-compose build web
docker-compose up -d

# Check container status
docker-compose ps
```

## Database Access

PostgreSQL is exposed on port **5435** (to avoid conflicts with local PostgreSQL).

```powershell
# Connect with psql
docker exec -it bankweave-postgres psql -U admin -d bankweavedb

# View tables
\dt

# View accounts
SELECT * FROM "FinancialAccounts";

# View transactions
SELECT * FROM "MoneyMovements";
```

## Development

### Run Locally (without Docker)

```powershell
# Start PostgreSQL only
docker-compose up -d postgres

# Run .NET app
dotnet run
```

Access at: **http://localhost:5000**

### Database Migrations

```powershell
# Install EF Core tools (one time)
dotnet tool install --global dotnet-ef

# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `Database__Host` | PostgreSQL host | `localhost` |
| `Database__Port` | PostgreSQL port | `5432` |
| `Database__Name` | Database name | `bankweavedb` |
| `Database__User` | Database user | `admin` |
| `Database__Password` | Database password | `secretpass` |

All configured in `docker-compose.yml`.

## Troubleshooting

### Port Conflicts
```powershell
.\check-ports.ps1  # Check if ports 8082/5435 are available
```

### Database Connection Failed
```powershell
docker-compose ps              # Check container status
docker-compose logs postgres   # Check PostgreSQL logs
```

### CSV Import Failed
- See [CSV_IMPORT_GUIDE.md](CSV_IMPORT_GUIDE.md) for format requirements
- Check CSV has required columns (Date, Description, Amount)
- Verify date format is consistent
- Ensure amounts are numeric (no currency symbols)

### Container Won't Start
```powershell
docker-compose down           # Stop all containers
docker-compose up -d          # Start fresh
docker-compose logs -f web    # Check startup logs
```

## Tech Stack

- **.NET 8** - Backend framework
- **ASP.NET Core** - Web API with JWT authentication
- **PostgreSQL 16** - Encrypted data storage
- **Entity Framework Core 8** - ORM with user isolation
- **Web Crypto API** - Client-side AES-GCM-256 encryption
- **Vanilla JavaScript** - Zero-dependency frontend
- **Docker & Docker Compose** - Containerization

## Security & Data Privacy

### 🔐 Zero-Knowledge Encryption
- **AES-GCM-256 encryption** with 96-bit nonces for authenticated encryption
- **PBKDF2 key derivation** (100,000 iterations, SHA-256) from user password
- **16-byte random salt** per user for key derivation
- **Client-side only**: Encryption/decryption happens in your browser
- **Server never sees keys**: Your password-derived key never leaves your device
- **Session storage**: Keys stored in browser memory, cleared on logout
- **Database admin cannot read**: All sensitive data stored as encrypted blobs

### 🛡️ Privacy Guarantees
- ✅ All data stored locally on your machine
- ✅ No external API calls (CSV only)
- ✅ No bank credentials stored
- ✅ Works completely offline
- ✅ **Encrypted transactions**: Even database backups are encrypted
- ✅ **Multi-user isolation**: Users cannot see each other's data
- ✅ **End-to-end privacy**: Only you can decrypt your financial information

### 🔒 What's Protected
**Encrypted fields:**
- Transaction descriptions
- Transaction amounts  
- Merchant/counterparty names
- Transaction categories

**Unencrypted (for queries):**
- Transaction dates
- Account references
- User IDs
- Timestamps

### 📊 Migration for Existing Users
If you have existing unencrypted data, you'll see a one-time migration prompt:
1. Dashboard detects unencrypted transactions
2. Click "Encrypt My Data" button
3. All transactions encrypted in browser and re-uploaded
4. Original plaintext data deleted from database
5. **Note**: This is irreversible - encryption cannot be undone

## License

MIT License - Feel free to use for personal financial tracking!

## Support & Documentation

- **CSV Import**: See [CSV_IMPORT_GUIDE.md](CSV_IMPORT_GUIDE.md)
- **Trading212 Specific**: See [TRADING212_CSV_UI_GUIDE.md](TRADING212_CSV_UI_GUIDE.md)
- **Docker Setup**: See [DOCKER_QUICK_START.md](DOCKER_QUICK_START.md)
- **API Docs**: http://localhost:8082/swagger
- **Dashboard**: http://localhost:8082
- **Database**: PostgreSQL on port 5435

**Enjoy secure financial tracking! 💰🔐**
