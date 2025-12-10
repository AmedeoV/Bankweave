#!/bin/bash

# Bankweave Demo Account - Quick Test Script (Bash version)
# This script demonstrates how to interact with the demo account

echo "=== Bankweave Demo Account Test ==="
echo ""

# Configuration
BASE_URL="${BANKWEAVE_URL:-http://localhost:8083}"
DEMO_EMAIL="demo@bankweave.app"
DEMO_PASSWORD="Demo123!"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Step 1: Create/Reset Demo Account
echo -e "${YELLOW}1. Creating demo account...${NC}"
CREATE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/demo/create-demo-account")
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Demo account created successfully!${NC}"
    echo -e "${GRAY}   Response: $CREATE_RESPONSE${NC}"
else
    echo -e "${RED}✗ Failed to create demo account${NC}"
    exit 1
fi

echo ""

# Step 2: Login
echo -e "${YELLOW}2. Logging in...${NC}"
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$DEMO_EMAIL\",\"password\":\"$DEMO_PASSWORD\"}")

TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -n "$TOKEN" ]; then
    echo -e "${GREEN}✓ Login successful!${NC}"
    echo -e "${GRAY}   Token: ${TOKEN:0:50}...${NC}"
else
    echo -e "${RED}✗ Login failed${NC}"
    echo -e "${GRAY}   Response: $LOGIN_RESPONSE${NC}"
    exit 1
fi

echo ""

# Step 3: Get Accounts
echo -e "${YELLOW}3. Fetching accounts...${NC}"
ACCOUNTS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/accounts" \
    -H "Authorization: Bearer $TOKEN")

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Accounts fetched successfully!${NC}"
    echo -e "${GRAY}$ACCOUNTS_RESPONSE${NC}" | python3 -m json.tool 2>/dev/null || echo "$ACCOUNTS_RESPONSE"
else
    echo -e "${RED}✗ Failed to fetch accounts${NC}"
fi

echo ""

# Step 4: Get Account Details (extract first account ID)
echo -e "${YELLOW}4. Fetching transactions...${NC}"
ACCOUNT_ID=$(echo $ACCOUNTS_RESPONSE | grep -o '"id":"[^"]*' | head -1 | cut -d'"' -f4)

if [ -n "$ACCOUNT_ID" ]; then
    TRANSACTIONS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/accounts/$ACCOUNT_ID/transactions?pageSize=10" \
        -H "Authorization: Bearer $TOKEN")
    
    echo -e "${GREEN}✓ Transactions fetched successfully!${NC}"
    echo -e "${GRAY}Showing first 10 transactions:${NC}"
    echo "$TRANSACTIONS_RESPONSE" | python3 -m json.tool 2>/dev/null | head -50 || echo "$TRANSACTIONS_RESPONSE" | head -50
else
    echo -e "${RED}✗ Could not extract account ID${NC}"
fi

echo ""

# Step 5: Get Statistics
echo -e "${YELLOW}5. Fetching spending statistics...${NC}"
STATS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/stats/spending-by-category?months=3" \
    -H "Authorization: Bearer $TOKEN")

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Statistics fetched successfully!${NC}"
    echo "$STATS_RESPONSE" | python3 -m json.tool 2>/dev/null | head -30 || echo "$STATS_RESPONSE" | head -30
else
    echo -e "${RED}✗ Failed to fetch statistics${NC}"
fi

echo ""

# Step 6: Get Categorization Rules
echo -e "${YELLOW}6. Fetching categorization rules...${NC}"
RULES_RESPONSE=$(curl -s -X GET "$BASE_URL/api/categorization-rules" \
    -H "Authorization: Bearer $TOKEN")

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Categorization rules fetched successfully!${NC}"
    echo "$RULES_RESPONSE" | python3 -m json.tool 2>/dev/null | head -30 || echo "$RULES_RESPONSE" | head -30
else
    echo -e "${RED}✗ Failed to fetch rules${NC}"
fi

echo ""
echo -e "${CYAN}=== Demo Test Complete! ===${NC}"
echo ""
echo -e "${NC}You can now login to the web interface with:${NC}"
echo -e "${CYAN}   Email: $DEMO_EMAIL${NC}"
echo -e "${CYAN}   Password: $DEMO_PASSWORD${NC}"
echo ""
echo -e "${NC}Or explore the API documentation at: $BASE_URL/swagger${NC}"
echo ""
echo -e "${GRAY}Tip: Set BANKWEAVE_URL environment variable to test against a different instance${NC}"
echo -e "${GRAY}     Example: export BANKWEAVE_URL=https://your-bankweave-instance.com${NC}"
