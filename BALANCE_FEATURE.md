# Balance Detection & Management Feature

## Overview
Implemented intelligent balance handling that detects account balances from CSV files when available (like PTSB) and allows manual entry for banks that don't provide balance columns (like Trading212).

## Implementation Details

### 1. Database Schema
Added `StartingBalance` field to `FinancialAccount` entity:
```csharp
public decimal StartingBalance { get; set; }
```

Balance calculation formula:
```
CurrentBalance = StartingBalance + Sum(All Transactions)
```

### 2. CSV Parser Changes
- **New Return Type**: All parsers now return `CsvParseResult` instead of `List<MoneyMovement>`
- **CsvParseResult Properties**:
  - `List<MoneyMovement> Transactions` - The parsed transactions
  - `decimal? DetectedBalance` - Balance detected from CSV (null if not available)
  - `bool HasBalanceColumn` - Indicates if CSV format includes balance

### 3. Bank-Specific Balance Detection

#### PTSB (Balance Auto-Detection)
- Extracts last balance from "Balance €" column
- Example: If CSV shows final balance of €3,319.00, this is automatically detected
- Parser logs: "Detected ending balance: €3,319.00"

#### Trading212, Revolut, Trade Republic, Raisin (Manual Entry)
- These CSVs don't include balance columns
- Returns `DetectedBalance = null` and `HasBalanceColumn = false`
- User must enter balance manually from their app/website

### 4. API Endpoints

#### POST /api/csvimport/upload
Imports CSV and detects balance if available.

**Response**:
```json
{
  "message": "CSV imported successfully",
  "accountId": "guid",
  "importedCount": 156,
  "skippedCount": 0,
  "detectedBalance": 3319.0,    // or null if not detected
  "hasBalanceColumn": true,      // or false
  "currentBalance": 0.0,         // Will be 0 until balance is set
  "isNewAccount": true           // Triggers balance confirmation modal
}
```

#### POST /api/csvimport/set-balance/{accountId}
Sets the starting balance for an account.

**Request**:
```json
{
  "startingBalance": 3319.0
}
```

**Response**:
```json
{
  "message": "Balance updated successfully",
  "startingBalance": 3319.0,
  "currentBalance": 81370.18
}
```

### 5. Frontend UI

#### Balance Confirmation Modal
After CSV import for new accounts, a modal appears:

**When Balance Detected (PTSB)**:
- Title: "Set Account Balance"
- Description: "A balance of €3,319.00 was detected in your CSV. Please confirm or adjust:"
- Input: Pre-filled with detected balance
- Buttons: "Skip (Use €0.00)" | "Confirm"

**When Balance Not Detected (Trading212)**:
- Title: "Set Account Balance"
- Description: "No balance was detected in the CSV. Please enter the current balance from your account:"
- Input: Empty, user must enter
- Buttons: "Skip (Use €0.00)" | "Confirm"

#### User Flow
1. User uploads CSV via "Import CSV" button
2. System parses file and imports transactions
3. If new account, balance modal appears:
   - For PTSB: Shows detected balance, user confirms or adjusts
   - For Trading212: User enters balance from app
4. User clicks "Confirm" → Balance is saved
5. Success message: "Success! Starting balance set to €3,319.00. Current balance: €81,370.18"
6. Dashboard refreshes with accurate balances

## Testing Results

### Test Case 1: PTSB with Balance Detection
```powershell
# Import PTSB CSV
POST /api/csvimport/upload
File: PTSB - Transaction List.csv
Provider: ptsb
AccountName: PTSB Current Account

# Response
✓ Detected Balance: €3,319.00
✓ Has Balance Column: true
✓ Imported: 156 transactions

# Set balance
POST /api/csvimport/set-balance/{accountId}
Body: { "startingBalance": 3319.0 }

# Result
✓ Starting Balance: €3,319.00
✓ Transaction Sum: €78,051.18
✓ Current Balance: €81,370.18 ✓
```

### Test Case 2: Trading212 with Manual Entry
```powershell
# Import Trading212 CSV
POST /api/csvimport/upload
File: Trading212 - from_2025-11-08_to_2025-11-27.csv
Provider: trading212
AccountName: Trading 212 ISA

# Response
✓ Detected Balance: null
✓ Has Balance Column: false
✓ Imported: 92 transactions

# Set balance manually
POST /api/csvimport/set-balance/{accountId}
Body: { "startingBalance": 5000.0 }

# Result
✓ Starting Balance: €5,000.00
✓ Transaction Sum: €1,656.44
✓ Current Balance: €6,656.44 ✓
```

### Overall Results
```
Total Balance: €88,026.62
├── PTSB Current Account: €81,370.18
│   └── (Auto-detected starting balance from CSV)
└── Trading 212 ISA: €6,656.44
    └── (Manual starting balance entry)

30-Day Stats:
├── Income: €21,888.58
├── Expenses: €1,372.59
└── Net: €20,515.99
```

## Migration
Created migration: `AddStartingBalanceToFinancialAccount`
- Adds `StartingBalance` column to `FinancialAccounts` table
- Default value: 0
- Applied automatically on startup

## Benefits

1. **Accuracy**: Balances reflect actual account state, not just imported transactions
2. **Flexibility**: Works with partial CSV imports (e.g., last 3 months only)
3. **Intelligence**: Auto-detects balance when available (PTSB)
4. **User-Friendly**: Clear modal guides user through balance setup
5. **Verification**: Shows calculated balance immediately for verification
6. **Universal**: Works for all banks, whether they provide balance or not

## Code Files Changed

### Backend
- `Entities/FinancialAccount.cs` - Added StartingBalance property
- `Services/CsvParsers/ICsvParser.cs` - New CsvParseResult class
- `Services/CsvParsers/PtsbCsvParser.cs` - Balance extraction logic
- `Services/CsvParsers/GenericCsvParser.cs` - Updated return type
- `Services/CsvParsers/Trading212CsvParser.cs` - Updated return type
- `Services/CsvParsers/RevolutCsvParser.cs` - Updated return type
- `Services/CsvParsers/TradeRepublicCsvParser.cs` - Updated return type
- `Services/CsvParsers/RaisinCsvParser.cs` - Updated return type
- `Controllers/CsvImportController.cs` - Balance detection logic + new endpoint
- `Migrations/AddStartingBalanceToFinancialAccount.cs` - Database migration

### Frontend
- `wwwroot/index.html` - Balance confirmation modal + JavaScript handlers

## Future Enhancements (Optional)

1. **Balance History**: Track balance snapshots over time
2. **Balance Validation**: Warn if detected balance seems incorrect
3. **Multi-Currency**: Handle balance in different currencies
4. **Edit Balance**: Allow editing balance after initial import
5. **Balance Trends**: Graph showing balance evolution over time
