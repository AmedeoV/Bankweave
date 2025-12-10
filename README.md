# Bankweave 💰

A privacy-focused personal financial dashboard with **zero-knowledge encryption**.

## Features

- **🔒 Zero-Knowledge Encryption**: AES-GCM-256 client-side encryption, your data stays private even from database admins
- **📂 CSV Import**: Trading212, Trade Republic, Raisin, Revolut, PTSB, and generic formats
- **📊 Analytics**: Income, expenses, balance tracking, spending insights
- **🎯 Auto-Categorization**: Custom rules for transaction categories
- **📈 What-If Scenarios**: Plan future expenses and see projected balances
- **📱 Mobile-Friendly**: Responsive design for all devices
- **🐳 Docker Ready**: One-command deployment

## Quick Start

### Try the Demo Account 🎮

Experience Bankweave instantly with pre-loaded sample data:

```powershell
# Start the application
docker-compose up -d

# Create demo account with sample data
curl -X POST http://localhost:8083/api/demo/create-demo-account

# Or use the test script
./test-demo-account.ps1  # Windows
./test-demo-account.sh   # Linux/Mac
```

**Demo Credentials:**
- Email: `demo@bankweave.app`
- Password: `Demo123!`

The demo account includes:
- 4 financial accounts (Checking, Savings, Credit Card, Investment)
- 100+ realistic transactions over 3 months
- Pre-configured categorization rules
- Sample income, expenses, and investment data

📖 **[Complete Demo Guide](DEMO_ACCOUNT_GUIDE.md)**

### Deploy Your Own Instance

```powershell
# Clone and start
git clone https://github.com/AmedeoV/Bankweave.git
cd Bankweave
docker-compose up -d

# Access at http://localhost:8083
```

## How It Works

1. Your password derives an encryption key (PBKDF2, 100k iterations)
2. All sensitive data encrypted in your browser before reaching server
3. Server stores only encrypted blobs
4. Only you can decrypt with your password

**Encrypted**: Transaction amounts, descriptions, merchant names, categories  
**Not encrypted**: Dates, timestamps (needed for queries)

## Documentation

- [🎮 Demo Account Guide](DEMO_ACCOUNT_GUIDE.md) - Try Bankweave with sample data
- [CSV Import Guide](CSV_IMPORT_GUIDE.md)
- [Docker Quick Start](DOCKER_QUICK_START.md)

## Tech Stack

.NET 8, PostgreSQL 16, Web Crypto API, Docker

## License

MIT
