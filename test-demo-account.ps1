# Bankweave Demo Account - Quick Test Script
# This PowerShell script demonstrates how to interact with the demo account

Write-Host "=== Bankweave Demo Account Test ===" -ForegroundColor Cyan
Write-Host ""

# Configuration
$baseUrl = "http://localhost:8083"  # Change to your deployment URL
$demoEmail = "demo@bankweave.app"
$demoPassword = "Demo123!"

# Step 1: Create/Reset Demo Account
Write-Host "1. Creating demo account..." -ForegroundColor Yellow
try {
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/demo/create-demo-account" -Method POST
    Write-Host "✓ Demo account created successfully!" -ForegroundColor Green
    Write-Host "   Accounts: $($createResponse.accounts.Count)" -ForegroundColor Gray
    Write-Host "   Transactions: $($createResponse.stats.transactionsCount)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed to create demo account: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Login
Write-Host "2. Logging in..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = $demoEmail
        password = $demoPassword
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful!" -ForegroundColor Green
    Write-Host "   User: $($loginResponse.email)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Get Accounts
Write-Host "3. Fetching accounts..." -ForegroundColor Yellow
try {
    $headers = @{ Authorization = "Bearer $token" }
    $accounts = Invoke-RestMethod -Uri "$baseUrl/api/accounts" -Headers $headers
    Write-Host "✓ Found $($accounts.Count) accounts:" -ForegroundColor Green
    
    foreach ($account in $accounts) {
        $balanceColor = if ($account.balance -ge 0) { "Green" } else { "Red" }
        Write-Host "   • $($account.displayName): " -NoNewline -ForegroundColor Gray
        Write-Host "$($account.balance) $($account.currency)" -ForegroundColor $balanceColor
    }
} catch {
    Write-Host "✗ Failed to fetch accounts: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 4: Get Transactions
Write-Host "4. Fetching transactions from first account..." -ForegroundColor Yellow
try {
    $firstAccount = $accounts[0]
    $transactions = Invoke-RestMethod -Uri "$baseUrl/api/accounts/$($firstAccount.id)/transactions?pageSize=10" -Headers $headers
    Write-Host "✓ Found $($transactions.totalCount) transactions in '$($firstAccount.displayName)'" -ForegroundColor Green
    Write-Host "   Showing last 10:" -ForegroundColor Gray
    
    foreach ($tx in $transactions.transactions) {
        $amountColor = if ($tx.amount -ge 0) { "Green" } else { "Yellow" }
        $date = [DateTime]::Parse($tx.date).ToString("yyyy-MM-dd")
        $amount = "{0,10:N2}" -f $tx.amount
        Write-Host "   $date " -NoNewline -ForegroundColor Gray
        Write-Host "$amount" -NoNewline -ForegroundColor $amountColor
        Write-Host " $($tx.currency) - $($tx.description.Substring(0, [Math]::Min(30, $tx.description.Length)))" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed to fetch transactions: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Step 5: Get Statistics
Write-Host "5. Fetching spending statistics..." -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod -Uri "$baseUrl/api/stats/spending-by-category?months=3" -Headers $headers
    Write-Host "✓ Spending by category (last 3 months):" -ForegroundColor Green
    
    $stats | Sort-Object -Property amount -Descending | Select-Object -First 5 | ForEach-Object {
        Write-Host "   • $($_.category): " -NoNewline -ForegroundColor Gray
        Write-Host "$($_.amount) EUR" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Failed to fetch statistics: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Step 6: Get Categorization Rules
Write-Host "6. Fetching categorization rules..." -ForegroundColor Yellow
try {
    $rules = Invoke-RestMethod -Uri "$baseUrl/api/categorization-rules" -Headers $headers
    Write-Host "✓ Found $($rules.Count) categorization rules:" -ForegroundColor Green
    
    foreach ($rule in $rules | Select-Object -First 5) {
        Write-Host "   • Pattern: '$($rule.pattern)' → Category: '$($rule.category)'" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed to fetch rules: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Demo Test Complete! ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now login to the web interface with:" -ForegroundColor White
Write-Host "   Email: $demoEmail" -ForegroundColor Cyan
Write-Host "   Password: $demoPassword" -ForegroundColor Cyan
Write-Host ""
Write-Host "Or explore the API documentation at: $baseUrl/swagger" -ForegroundColor White
