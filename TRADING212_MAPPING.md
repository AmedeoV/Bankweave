# Trading 212 Transaction Mapping System

## Overview

This system maps Trading 212 transactions between two sources:
1. **CSV Statement Files** - Contains UUID-based transaction IDs
2. **Trading 212 API** - Returns numeric transaction IDs

The mapping allows you to:
- Import transactions from CSV files with their original UUIDs
- Sync with the Trading 212 API and automatically link API transactions to existing CSV transactions
- Avoid duplicate transactions when using both import methods

## How It Works

### 1. Database Schema

Added `ExternalId` field to `MoneyMovement` entity:
- Stores the UUID from CSV files (e.g., `82517ee8-75c4-433c-bed0-c95be3f2b9cf`)
- Remains `null` for API-only transactions

### 2. CSV Import

When importing Trading 212 CSV files:
- The UUID from the `ID` column is stored in `ExternalId`
- `TransactionId` is formatted as `t212-{UUID}`
- Example CSV format:
  ```
  Action,Time,Notes,ID,Total,Currency (Total),...
  Interest on cash,2024-11-28 02:08:19,"Interest",82517ee8-75c4-433c-bed0-c95be3f2b9cf,1.86,"EUR"
  ```

### 3. API Sync with Smart Mapping

When syncing via API (`POST /api/accounts/{accountId}/sync-trading212-transactions`):

1. **Fetch transactions** from Trading 212 API
2. **For each API transaction:**
   - Check if already imported (by API ID)
   - If not, search for matching CSV transaction by:
     - Same account
     - Transaction date within 24 hours
     - Exact amount match
   - If match found:
     - **Link** the CSV transaction to the API transaction
     - Update `TransactionId` to `t212-api-{ApiId}`
     - Keep the original `ExternalId` (CSV UUID)
   - If no match:
     - **Create new** transaction from API data
     - `TransactionId` = `t212-api-{ApiId}`
     - `ExternalId` = `null`

### 4. Matching Algorithm

The `Trading212MappingService` matches transactions using:
- **Date window**: ±24 hours from API transaction date
- **Amount**: Exact match required
- **Preference**: Exact timestamp match (within 1 minute) preferred
- **Fallback**: Closest time match if multiple candidates

## Usage

### Import from CSV

```bash
POST /api/CsvImport/import
Content-Type: multipart/form-data

accountId: {your-account-guid}
parser: Trading212
file: Trading 212 - from_2024-11-28_to_2025-11-28.csv
```

### Sync from API (with auto-mapping)

```bash
POST /api/accounts/{accountId}/sync-trading212-transactions
```

**Response:**
```json
{
  "message": "Synced 5 new transactions and linked 10 existing CSV transactions",
  "totalFetched": 15,
  "imported": 5,
  "linked": 10
}
```

## Benefits

1. **No Duplicates**: Automatically prevents duplicate transactions when using both CSV and API
2. **Data Reconciliation**: Links CSV and API representations of the same transaction
3. **Flexible Import**: Import historical data from CSV, then use API for ongoing sync
4. **Audit Trail**: Maintains both CSV UUID and API ID for complete traceability

## Example Scenario

1. **Initial Setup**: Import 6 months of history from CSV
   - All transactions have `ExternalId` = CSV UUID
   - `TransactionId` = `t212-{UUID}`

2. **Daily Sync**: Use API to sync new transactions
   - Transactions from today that match CSV: Linked (keep ExternalId, update TransactionId)
   - New API-only transactions: Created (no ExternalId)

3. **Result**: No duplicates, complete history, optimal API usage

## Files Modified

- `Entities/MoneyMovement.cs` - Added `ExternalId` field
- `Services/CsvParsers/Trading212CsvParser.cs` - Store CSV UUID in `ExternalId`
- `Services/Trading212MappingService.cs` - New service for matching logic
- `Controllers/AccountsController.cs` - Updated sync endpoint to use mapping
- `Program.cs` - Registered `Trading212MappingService`
- `Migrations/` - Added migration for `ExternalId` column

## Migration

Run the migration to add the `ExternalId` column:

```bash
dotnet ef database update
```
