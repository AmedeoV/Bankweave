# 🎮 Bankweave Demo Account - Quick Reference

## Instant Access

Try Bankweave without any setup using our demo account!

### 🔗 Quick Links

- **Demo Page**: `http://your-instance.com/demo.html`
- **API Endpoint**: `POST /api/demo/create-demo-account`
- **Swagger Docs**: `http://your-instance.com/swagger`

### 🔑 Demo Credentials

```
Email:    demo@bankweave.app
Password: Demo123!
```

## 🚀 One-Click Setup

### Using cURL

```bash
curl -X POST http://localhost:8083/api/demo/create-demo-account
```

### Using PowerShell

```powershell
Invoke-RestMethod -Uri "http://localhost:8083/api/demo/create-demo-account" -Method POST
```

### Using Test Script

```bash
# PowerShell (Windows)
./test-demo-account.ps1

# Bash (Linux/Mac)
chmod +x test-demo-account.sh
./test-demo-account.sh
```

## 📊 What You Get

### Accounts (4 total)

1. **Main Checking Account** - €7,850.13
   - Daily expenses, salary deposits, bill payments
   - 56 transactions

2. **Savings Account** - €16,025.25
   - Transfers from checking, interest payments
   - 4 transactions

3. **Visa Credit Card** - -€169.76 (balance due)
   - Online shopping, subscriptions, travel
   - 11 transactions

4. **Investment Account** (Trading212) - €6,250.00
   - Stock trades, dividends
   - 7 transactions

**Total Net Worth: €29,955.62**

### Transaction Categories

- ✅ **Income**: Salary (€3,500/month), dividends, interest
- 🏠 **Housing**: Rent (€1,200/month)
- 🛒 **Groceries**: Weekly shopping (~€100/week)
- 🍽️ **Dining**: Restaurants, cafes, coffee shops
- 🚗 **Transportation**: Transit passes, gas
- 🎬 **Entertainment**: Netflix, Spotify, cinema
- 🛍️ **Shopping**: Clothing, electronics, books
- 💊 **Healthcare**: Pharmacy, doctor visits
- 📈 **Investment**: Stock purchases and sales

### Smart Features Included

- ✨ 5 pre-configured categorization rules
- 🎯 Essential expense marking
- 📅 3 months of transaction history
- 💰 Multiple currency support (EUR)
- 🔄 Regular recurring transactions

## 🎯 Perfect For

- **Product Demos**: Show Bankweave's features with realistic data
- **User Testing**: Let users explore without setting up accounts
- **Development**: Test features against consistent data
- **Screenshots**: Generate professional marketing materials
- **Training**: Teach users how to use the platform

## 🔄 Resetting the Demo

Simply call the create endpoint again - it automatically:
1. Deletes existing demo account and all data
2. Creates fresh account with same credentials
3. Populates with new sample data

**No cleanup needed!**

## 📱 Sharing the Demo

### For General Users

1. Send them to `/demo.html` page
2. They click "Create/Reset Demo Account"
3. Auto-redirects to login with credentials filled

### For Developers

Share the API documentation:
- Swagger UI: `/swagger`
- Demo endpoint: `POST /api/demo/create-demo-account`
- Returns: Account details, transaction counts, credentials

### For Stakeholders

Prepare a demo by:
1. Creating demo account
2. Login to web interface
3. Navigate through: Accounts → Transactions → Statistics → Rules
4. Show real-time categorization and analytics

## 🎨 Customization

Want to customize the demo data? Edit:
```
Services/DemoDataSeederService.cs
```

Key methods:
- `CreateSampleAccounts()` - Account setup
- `CreateSampleTransactions()` - Transaction data
- `CreateSampleCategorizationRules()` - Auto-categorization rules

## 🔒 Security Notes

⚠️ **For Production Deployments:**

1. **Remove/Restrict Demo Endpoint**
   - Remove `[AllowAnonymous]` attribute
   - Or delete `DemoController.cs` entirely

2. **Change Demo Password**
   - Update in `DemoDataSeederService.cs`
   - Don't use publicly known credentials

3. **Rate Limit the Endpoint**
   - Prevent abuse of account creation
   - Add IP-based throttling

4. **Monitor Usage**
   - Log demo account creations
   - Alert on excessive usage

## 📊 Sample API Workflow

```bash
# 1. Create demo account
curl -X POST http://localhost:8083/api/demo/create-demo-account

# 2. Login
TOKEN=$(curl -X POST http://localhost:8083/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@bankweave.app","password":"Demo123!"}' \
  | jq -r '.token')

# 3. Get accounts
curl -X GET http://localhost:8083/api/accounts \
  -H "Authorization: Bearer $TOKEN"

# 4. Get transactions
curl -X GET "http://localhost:8083/api/accounts/{accountId}/transactions?pageSize=100" \
  -H "Authorization: Bearer $TOKEN"

# 5. Get statistics
curl -X GET "http://localhost:8083/api/stats/spending-by-category?months=3" \
  -H "Authorization: Bearer $TOKEN"

# 6. Get rules
curl -X GET http://localhost:8083/api/categorization-rules \
  -H "Authorization: Bearer $TOKEN"
```

## 📖 Documentation

- **Full Guide**: [DEMO_ACCOUNT_GUIDE.md](DEMO_ACCOUNT_GUIDE.md)
- **CSV Import**: [CSV_IMPORT_GUIDE.md](CSV_IMPORT_GUIDE.md)
- **Deployment**: [DOCKER_QUICK_START.md](DOCKER_QUICK_START.md)

## 💡 Tips

1. **Quick Reset**: The demo can be reset unlimited times
2. **Consistent Data**: Same structure each time for reliable testing
3. **Real Patterns**: Transactions follow realistic spending patterns
4. **Time-based**: Transactions span exactly 90 days back from creation
5. **Multi-currency Ready**: Easy to add more currencies in the seeder

## 🎬 Demo Script Example

### 30-Second Pitch

1. **Create Account** (5s)
   - Click "Create Demo Account"
   - Automatic login

2. **Show Overview** (10s)
   - Dashboard with 4 accounts
   - Total balance: ~€30,000
   - Visual balance chart

3. **Drill Into Transactions** (10s)
   - Open checking account
   - Scroll through categorized transactions
   - Show automatic categorization

4. **Analytics** (5s)
   - View spending by category
   - Show monthly trends
   - Highlight insights

### 5-Minute Deep Dive

1. Account Overview (1 min)
2. Transaction History with Filters (1 min)
3. Categorization Rules (1 min)
4. Statistics & Analytics (1 min)
5. What-If Scenarios (1 min)

## 🆘 Troubleshooting

### Demo account creation fails
- Check database connection
- Verify migrations are up to date
- Check application logs

### Can't login with demo credentials
- Ensure demo account was created successfully
- Check for typos in credentials
- Try creating demo account again

### No transactions showing
- Increase `pageSize` query parameter
- Check date filters
- Verify account ID is correct

### Statistics not showing
- Call demo creation endpoint again
- Check that transactions exist in database
- Verify time range parameters

## 📞 Support

For issues or questions:
1. Check application logs: `docker logs bankweave-web-prod`
2. Review database: `docker exec -it bankweave-postgres-prod psql -U bankweave_user -d bankweavedb`
3. Test API directly via Swagger UI
4. Check [DEMO_ACCOUNT_GUIDE.md](DEMO_ACCOUNT_GUIDE.md) for detailed instructions

---

**Ready to share Bankweave with the world! 🚀**
