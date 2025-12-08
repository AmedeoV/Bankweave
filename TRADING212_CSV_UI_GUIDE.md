# Trading 212 CSV Mapping - UI Guide

## Overview

The UI now includes a dedicated feature for Trading 212 accounts to upload CSV statements for transaction mapping. This enables automatic linking between CSV transactions and API transactions.

## How to Use

### 1. Access Your Trading 212 Account

In the Bankweave dashboard, find your Trading 212 account in the "Connected Accounts" section.

### 2. Available Actions

For Trading 212 accounts, you'll see these buttons:

- **🔄 Sync Balance** - Sync your current balance from the API
- **📁 Upload CSV for Mapping** - New feature! Upload CSV to enable transaction mapping
- **🔑 Update API Key** - Update your API credentials

### 3. Upload CSV for Mapping

Click the **"📁 Upload CSV for Mapping"** button to:

1. **Open the modal** with upload instructions
2. **Select your CSV file**:
   - Download from Trading 212 app → History → Export
   - File format: `Trading 212 - from_YYYY-MM-DD_to_YYYY-MM-DD.csv`
3. **Click "Upload & Map"**

### 4. What Happens

The system performs two operations automatically:

1. **CSV Import**:
   - Imports all transactions from the CSV
   - Stores the UUID from each transaction for mapping
   - Skips duplicates if already imported

2. **API Sync & Mapping**:
   - Fetches transactions from Trading 212 API
   - Matches API transactions with CSV transactions (by date, amount, type)
   - Links matched transactions to avoid duplicates
   - Creates new transactions for API-only data

### 5. Success Message

You'll see a detailed summary:

```
✅ CSV Mapping Complete!

📥 CSV Import:
  • Imported: 45 transactions
  • Skipped duplicates: 5

🔗 API Sync & Mapping:
  • API fetched: 50 transactions
  • Linked to CSV: 42
  • New from API: 8

Your transactions are now mapped and ready!
```

## Benefits

- **No Duplicates**: Automatically prevents duplicate transactions
- **Complete History**: Import historical data from CSV, then use API for ongoing sync
- **Smart Matching**: Links transactions based on date, amount, and type
- **Easy Workflow**: Single button does everything automatically

## Typical Workflow

### First Time Setup

1. Download your Trading 212 CSV statement (historical data)
2. Click **"Upload CSV for Mapping"**
3. Upload the CSV file
4. Wait for the mapping to complete

### Ongoing Use

- Use **"Sync Balance"** button for regular API syncs
- System automatically links new transactions with existing CSV data
- No need to upload CSV again unless you want to update historical data

## Technical Details

- **CSV IDs**: Stored in `ExternalId` field (UUID format)
- **API IDs**: Stored in `TransactionId` field (numeric format)
- **Matching**: ±24 hours time window, exact amount match
- **Preference**: Exact timestamp match (within 1 minute) preferred

## Troubleshooting

### "API key not set"
- Click "Update API Key" and enter your Trading 212 credentials
- Get them from: Trading 212 → Settings → API (Beta)

### "CSV import failed"
- Ensure you're uploading the correct Trading 212 CSV format
- File should have columns: Action, Time, Notes, ID, Total, Currency

### "Some transactions not linked"
- This is normal if CSV and API have different time ranges
- CSV might have older transactions not in API response
- API might have very recent transactions not in your CSV export

## Example Use Case

**Scenario**: You want to import 6 months of history and keep it synced

1. **Step 1**: Export 6-month CSV from Trading 212
2. **Step 2**: Click "Upload CSV for Mapping" and upload
3. **Step 3**: System imports CSV (e.g., 150 transactions) and syncs with API (e.g., links 50, adds 10 new)
4. **Step 4**: Daily/weekly click "Sync Balance" to get new transactions from API
5. **Result**: Complete history with no duplicates!

## Files Modified

- `wwwroot/index.html` - Added UI components and JavaScript functions

## Related Documentation

- See `TRADING212_MAPPING.md` for backend implementation details
- See `CSV_IMPORT_GUIDE.md` for general CSV import information
