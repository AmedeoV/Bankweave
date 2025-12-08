# CSV Import Guide for Bankweave

All Open Banking integrations have been removed. Bankweave now operates as a **CSV-only** financial tracking solution.

## 🎯 Quick Start

1. **Export CSV from your banks** (see bank-specific guides below)
2. **Open Bankweave**: http://localhost:8082
3. **Click "Import CSV"** button
4. **Select bank provider** and upload file
5. **View your stats** - transactions import automatically

---

## 📊 Supported Banks

### 1. Trading 212
**Export Location:** Account → History → Export (CSV)

**Expected CSV Format:**
- Columns: `Date`, `Description`, `Amount`, `Currency`
- Date format: `yyyy-MM-dd`
- Transactions are both income and expenses (positive/negative amounts)

**Example:**
```csv
Date,Description,Amount,Currency
2025-11-27,Dividend from Apple Inc,25.50,EUR
2025-11-26,Interest payment,12.30,EUR
```

### 2. Trade Republic
**Export Location:** Timeline → ⚙️ Settings → Export → Transactions (CSV)

**Expected CSV Format:**
- Columns: `Date`, `Title`, `Amount`, `Currency`
- Date format: `dd/MM/yyyy` or `yyyy-MM-dd`
- Amount includes negative values for expenses

**Example:**
```csv
Date,Title,Amount,Currency
27/11/2025,Stock Purchase - Tesla,150.00,EUR
26/11/2025,Dividend - Microsoft,35.20,EUR
```

### 3. Raisin
**Export Location:** Transactions → Export → CSV format

**Expected CSV Format:**
- Columns: `Date`, `Description`, `Amount`, `Currency`
- Date format: `yyyy-MM-dd`
- Interest payments are positive

**Example:**
```csv
Date,Description,Amount,Currency
2025-11-27,Interest payment,125.75,EUR
2025-11-15,Deposit,5000.00,EUR
```

### 4. Revolut
**Export Location:** Home → Account → Statement → Export CSV

**Expected CSV Format:**
- Columns: `Completed Date`, `Description`, `Amount`, `Currency`
- Date format: `yyyy-MM-dd HH:mm:ss` or `dd MMM yyyy`
- Positive amounts = income, negative = expenses

**Example:**
```csv
Completed Date,Description,Amount,Currency
2025-11-27 14:30:00,Salary Deposit,2500.00,EUR
2025-11-26 09:15:30,Coffee Shop,-4.50,EUR
```

### 5. PTSB (Permanent TSB)
**Export Location:** Online Banking → Statements → Download CSV

**Expected CSV Format:**
- Standard bank statement CSV
- Columns: `Date`, `Description`, `Debit`, `Credit`, `Balance`
- Date format: `dd/MM/yyyy`
- Separate debit/credit columns

**Example:**
```csv
Date,Description,Debit,Credit,Balance
27/11/2025,Direct Debit - Electricity,-85.00,,2415.00
26/11/2025,ATM Withdrawal,-60.00,,2500.00
25/11/2025,Salary Deposit,,2560.00,2560.00
```

### 6. Generic CSV (Any Other Bank)
**For banks not listed above**, select "Generic" provider.

**Minimum Required Columns:**
- `Date` (various formats supported)
- `Description` or `Title`
- `Amount` (positive/negative) OR separate `Debit`/`Credit` columns
- `Currency` (optional, defaults to EUR)

---

## 🔧 How It Works

### Transaction Detection
The parser automatically detects:
- **Income**: Positive amounts, "Credit" column, keywords (salary, deposit, dividend, interest)
- **Expenses**: Negative amounts, "Debit" column, keywords (payment, purchase, withdrawal)

### Date Format Support
Bankweave supports multiple date formats:
- `yyyy-MM-dd` (2025-11-27)
- `dd/MM/yyyy` (27/11/2025)
- `dd MMM yyyy` (27 Nov 2025)
- `yyyy-MM-dd HH:mm:ss` (2025-11-27 14:30:00)

### Duplicate Prevention
- Transactions are checked for duplicates based on:
  - Date
  - Amount
  - Description
- Duplicate transactions are skipped during import

---

## 📝 Import Steps

1. **Clear Test Data** (optional, first time only):
   ```powershell
   Invoke-RestMethod -Uri "http://localhost:8082/api/test/clear-data" -Method Delete
   ```

2. **Export CSV from your bank** using guides above

3. **Open Bankweave Dashboard**:
   - Navigate to http://localhost:8082
   - You'll see your current account balances and stats

4. **Import CSV**:
   - Click **"Import CSV"** button
   - Select your bank from dropdown (Trading 212, Trade Republic, Raisin, Revolut, PTSB, Generic)
   - Choose CSV file
   - Click **Upload**

5. **View Results**:
   - Transactions appear immediately in the dashboard
   - Stats recalculate automatically
   - Check account balance and 30-day income/expenses

---

## 🎨 Dashboard Features

### Overview Stats
- **Total Balance**: Sum of all account balances
- **Account Count**: Number of connected accounts
- **Income (30 days)**: Total income in last 30 days
- **Expenses (30 days)**: Total expenses in last 30 days
- **Net (30 days)**: Income - Expenses

### Account List
- View all accounts with balances
- Click account to view transactions
- Shows provider name and IBAN

### Transaction History
- See all transactions for selected account
- Filtered by date range
- Shows amount, description, type (income/expense)

---

## 🔄 Regular Usage Pattern

**Weekly/Monthly Import Routine:**

1. **Export CSV** from each bank (once per week/month)
2. **Import each CSV** into Bankweave
3. **Review stats** - see your financial overview
4. **Track trends** - compare income vs expenses over time

**Benefits:**
- ✅ No API setup required
- ✅ No bank authentication needed
- ✅ Works with any Irish bank
- ✅ Complete data privacy (stays on your machine)
- ✅ Historical data available (import old statements)

---

## 🐛 Troubleshooting

### "Failed to parse CSV"
**Solution:** 
- Check CSV has required columns (Date, Description, Amount)
- Ensure date format is consistent
- Remove any header rows beyond the first one
- Verify CSV is UTF-8 encoded

### "No transactions imported"
**Solution:**
- Check CSV is not empty
- Verify transactions aren't duplicates (already imported)
- Ensure amounts are numeric (no currency symbols in Amount column)

### "Wrong balance showing"
**Solution:**
- Balances are calculated from transactions
- If balance is wrong, check if some transactions were skipped as duplicates
- Clear data and re-import if needed

### "Date parsing error"
**Solution:**
- Bankweave supports multiple formats, but check your CSV matches one of:
  - `yyyy-MM-dd`
  - `dd/MM/yyyy`
  - `dd MMM yyyy`
- Consistent date format throughout the file is required

---

## 💾 Data Management

### Clear All Data
```powershell
Invoke-RestMethod -Uri "http://localhost:8082/api/test/clear-data" -Method Delete
```

### View Specific Account Transactions
```powershell
# Get account ID first
$accounts = Invoke-RestMethod -Uri "http://localhost:8082/api/accounts"
$accountId = $accounts[0].id

# Get transactions
Invoke-RestMethod -Uri "http://localhost:8082/api/accounts/$accountId/transactions"
```

### Check Import Stats
```powershell
Invoke-RestMethod -Uri "http://localhost:8082/api/stats/overview"
```

---

## 🚀 Docker Commands

### Start Bankweave
```powershell
cd d:\Projects\Bankweave
docker-compose up -d
```

### Stop Bankweave
```powershell
docker-compose down
```

### View Logs
```powershell
docker-compose logs -f web
```

### Rebuild (after code changes)
```powershell
docker-compose build web
docker-compose up -d
```

---

## 📋 CSV Format Examples

### Example 1: Revolut Statement
```csv
Completed Date,Description,Amount,Currency
2025-11-27 14:30:00,Salary Deposit,2500.00,EUR
2025-11-26 09:15:30,Tesco Supermarket,-45.30,EUR
2025-11-25 18:22:15,Transfer to Savings,-500.00,EUR
2025-11-24 12:05:00,Restaurant Payment,-32.50,EUR
```

### Example 2: PTSB Statement
```csv
Date,Description,Debit,Credit,Balance
27/11/2025,Electricity Bill,85.00,,2415.00
26/11/2025,ATM Withdrawal,60.00,,2500.00
25/11/2025,Salary Payment,,2560.00,2560.00
24/11/2025,Grocery Shopping,78.50,,0.00
```

### Example 3: Trading 212 Dividends
```csv
Date,Description,Amount,Currency
2025-11-27,Dividend - Apple Inc,25.50,EUR
2025-11-20,Dividend - Microsoft,18.75,EUR
2025-11-15,Interest on Cash,3.20,EUR
```

---

## 🎓 Best Practices

1. **Export Regularly**: Weekly or monthly exports keep data fresh
2. **Label Accounts**: Use clear display names when creating accounts manually
3. **Consistent IBANs**: Always use same IBAN format for duplicate detection
4. **Backup CSV Files**: Keep original CSV files as backup
5. **Check Stats**: Verify stats after each import to ensure correctness
6. **One Account = One CSV**: Import one bank account at a time for clarity

---

## 🔒 Privacy & Security

- **All data stays local**: No external API calls
- **No credentials needed**: No bank passwords stored
- **Docker isolated**: Application runs in containers
- **PostgreSQL local**: Database on your machine only
- **No internet required**: Works completely offline (after setup)

---

## 📞 Need Help?

- **Dashboard**: http://localhost:8082
- **API Docs**: http://localhost:8082/swagger
- **Database**: PostgreSQL on port 5435
- **Logs**: `docker-compose logs -f web`

**Application fully functional with CSV import only! 🎉**
