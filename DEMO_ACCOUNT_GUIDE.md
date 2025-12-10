# Bankweave Demo Account Guide

## Quick Start

Bankweave includes a fully functional demo account with realistic financial data that you can use to explore all features without needing to set up your own accounts.

### Demo Account Credentials

- **Email:** `demo@bankweave.app`
- **Password:** `Demo123!`

## Creating/Resetting the Demo Account

### Option 1: Using the API

Make a POST request to create or reset the demo account:

```bash
curl -X POST http://localhost:5000/api/demo/create-demo-account
```

Or using PowerShell:

```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/demo/create-demo-account" -Method POST
```

The response will include:
- Success status
- User credentials
- List of created accounts
- Transaction statistics

### Option 2: Using Swagger UI

1. Navigate to `http://localhost:5000/swagger`
2. Find the `Demo` section
3. Expand `POST /api/demo/create-demo-account`
4. Click "Try it out" and then "Execute"

### Option 3: Get Demo Info (Without Creating)

```bash
curl -X GET http://localhost:5000/api/demo/demo-info
```

## What's Included in the Demo Account

### Financial Accounts

The demo account includes 4 different account types:

1. **Main Checking Account** (EUR)
   - Starting balance: €5,000
   - Daily expenses, income, and bill payments

2. **Savings Account** (EUR)
   - Starting balance: €15,000
   - Regular transfers and interest payments

3. **Visa Credit Card** (EUR)
   - Starting balance: €0
   - Online shopping, subscriptions, and payments

4. **Investment Account (Trading212)** (EUR)
   - Starting balance: €10,000
   - Stock purchases, sales, and dividend income

### Transactions

The demo account contains **100+ realistic transactions** spanning the last 3 months, including:

#### Income
- Monthly salary payments (€3,500)
- Investment dividends
- Interest payments

#### Essential Expenses
- Rent (€1,200/month)
- Utilities (Electric, Internet)
- Groceries (weekly shopping)
- Transportation (transit passes, gas)
- Healthcare (pharmacy, doctor visits)

#### Discretionary Spending
- Dining out (restaurants, cafes)
- Entertainment (Netflix, Spotify, cinema)
- Shopping (clothing, electronics, books)
- Travel expenses

#### Investments
- Stock purchases (AAPL, MSFT, GOOGL, TSLA)
- Stock sales with profits
- Dividend income

### Categorization Rules

Pre-configured smart categorization rules:
- Automatic grocery categorization
- Salary recognition
- Subscription identification (Netflix, Spotify)
- Rent and housing expenses
- Dining and restaurant detection

## Using the Demo Account

### 1. Login

First, log in to get your JWT token:

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "demo@bankweave.app",
    "password": "Demo123!"
  }'
```

Save the returned `token` for subsequent requests.

### 2. View Accounts

```bash
curl -X GET http://localhost:5000/api/accounts \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### 3. View Transactions

```bash
curl -X GET http://localhost:5000/api/accounts/{accountId}/transactions \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### 4. View Statistics

Get spending insights:

```bash
curl -X GET http://localhost:5000/api/stats/spending-by-category?months=3 \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### 5. Explore Categorization Rules

```bash
curl -X GET http://localhost:5000/api/categorization-rules \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Features to Explore

With the demo account, you can test:

✅ **Multi-account management** - View balances across different account types  
✅ **Transaction history** - Browse 3 months of realistic financial data  
✅ **Automatic categorization** - See how transactions are automatically categorized  
✅ **Spending analytics** - Analyze spending patterns by category and time  
✅ **Income tracking** - Monitor salary and investment income  
✅ **What-if scenarios** - Create financial planning scenarios  
✅ **CSV import** - Test importing additional transactions  
✅ **Rule management** - Create and manage categorization rules  
✅ **Investment tracking** - See stock portfolio transactions  

## Resetting the Demo Account

The demo account can be reset at any time by calling the create endpoint again. This will:
1. Delete the existing demo account and all associated data
2. Create a fresh demo account with the same credentials
3. Populate it with new sample data

This is useful for:
- Testing from a clean state
- Demonstrating features to new users
- Recovering from any data modifications

## Docker Deployment

If you're running Bankweave in Docker, the demo account endpoints are accessible at:

```
http://localhost:8080/api/demo/create-demo-account
```

Make sure the container is running:

```bash
wsl docker-compose up -d
```

## Security Notes

⚠️ **Important:** The demo account feature is designed for demonstration and testing purposes. In a production environment, you should:

1. **Disable or protect the demo endpoint** - Add authentication or remove it entirely
2. **Use a different demo password** - The current password is publicly known
3. **Regularly clean up demo data** - Prevent accumulation of test data
4. **Monitor demo account usage** - Track who is accessing the demo account

To disable demo account creation in production, you can:
- Remove the `[AllowAnonymous]` attribute from `DemoController`
- Add authentication requirements
- Disable the entire `DemoController` via configuration

## Troubleshooting

### "User already exists" error
Call the endpoint again - it will automatically delete and recreate the account.

### Can't login with demo credentials
Ensure you've called the create-demo-account endpoint first.

### No transactions visible
Check that the account was created successfully and that you're using the correct authorization token.

### Database connection errors
Ensure your PostgreSQL database is running and accessible. Check `appsettings.json` for connection settings.

## Support

For issues or questions about the demo account:
1. Check the API response messages for details
2. Review the application logs
3. Consult the main README.md for general setup instructions
4. Check Swagger UI documentation at `/swagger`

---

**Enjoy exploring Bankweave with the demo account!** 🚀
