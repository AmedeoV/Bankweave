-- View encrypted transaction data sample
-- This shows how data appears in the database after encryption

SELECT 
    "Id",
    "TransactionDate",
    "Amount" as "PlaintextAmount_Legacy",
    "Description" as "PlaintextDescription_Legacy",
    "Category" as "PlaintextCategory_Legacy",
    "CounterpartyName" as "PlaintextCounterparty_Legacy",
    "AmountEncrypted",
    "DescriptionEncrypted",
    "CategoryEncrypted",
    "CounterpartyNameEncrypted",
    "IsEssentialExpense",
    "CreatedAt"
FROM "MoneyMovements"
ORDER BY "CreatedAt" DESC
LIMIT 5;

-- Example of what encrypted data looks like:
-- 
-- Plaintext fields (legacy - before migration):
--   Amount: -45.99
--   Description: "Grocery shopping at Tesco"
--   Category: "Groceries"
--
-- After encryption, these same fields become:
--   AmountEncrypted: "gP3xKzN8mQ4vR2aB9Xc7Y1fL..."
--   DescriptionEncrypted: "hQ4yL0O9nR5wS3bC0Yd8Z2gM..."
--   CategoryEncrypted: "iR5zM1P0oS6xT4cD1Ze9A3hN..."
--
-- Each encrypted field is:
-- 1. A random 12-byte IV (Initialization Vector) prepended to the ciphertext
-- 2. The actual encrypted data using AES-GCM-256
-- 3. Base64 encoded for database storage
-- 4. Can ONLY be decrypted with the user's password-derived key
-- 5. Even with database access, the data is unreadable without the password

-- To verify encryption is working:
SELECT 
    COUNT(*) as "TotalTransactions",
    COUNT("DescriptionEncrypted") as "EncryptedTransactions",
    COUNT(*) - COUNT("DescriptionEncrypted") as "UnencryptedTransactions",
    ROUND(100.0 * COUNT("DescriptionEncrypted") / COUNT(*), 2) as "EncryptionPercentage"
FROM "MoneyMovements";
