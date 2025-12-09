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

- [CSV Import Guide](CSV_IMPORT_GUIDE.md)
- [Docker Quick Start](DOCKER_QUICK_START.md)

## Tech Stack

.NET 8, PostgreSQL 16, Web Crypto API, Docker

## License

MIT
