-- Script to create admin user and associate existing data
-- This script should be run after the AddAuthentication migration has been applied
-- 
-- IMPORTANT: Before running this script:
-- 1. Replace 'your_admin_email@example.com' with your actual admin email
-- 2. Generate a new password hash using ASP.NET Core Identity's password hasher
-- 3. Update the password hash in the INSERT statement below
--
-- To generate a password hash, you can use the /api/auth/register endpoint
-- or create a simple C# console app with Identity's PasswordHasher

DO $$
DECLARE
    admin_user_id TEXT;
BEGIN
    -- Insert the admin user if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM "AspNetUsers" WHERE "Email" = 'your_admin_email@example.com') THEN
        -- Generate a new GUID for the user
        admin_user_id := gen_random_uuid()::TEXT;
        
        INSERT INTO "AspNetUsers" (
            "Id",
            "UserName",
            "NormalizedUserName",
            "Email",
            "NormalizedEmail",
            "EmailConfirmed",
            "PasswordHash",
            "SecurityStamp",
            "ConcurrencyStamp",
            "PhoneNumberConfirmed",
            "TwoFactorEnabled",
            "LockoutEnabled",
            "AccessFailedCount",
            "FirstName",
            "LastName",
            "CreatedAt"
        ) VALUES (
            admin_user_id,
            'your_admin_email@example.com',
            'YOUR_ADMIN_EMAIL@EXAMPLE.COM',
            'your_admin_email@example.com',
            'YOUR_ADMIN_EMAIL@EXAMPLE.COM',
            true,
            -- Replace this with your generated password hash
            'REPLACE_WITH_YOUR_PASSWORD_HASH_HERE',
            UPPER(REPLACE(gen_random_uuid()::TEXT, '-', '')),
            gen_random_uuid()::TEXT,
            false,
            false,
            true,
            0,
            'Admin',
            'User',
            NOW()
        );
        
        RAISE NOTICE 'Admin user created with ID: %', admin_user_id;
    ELSE
        SELECT "Id" INTO admin_user_id FROM "AspNetUsers" WHERE "Email" = 'your_admin_email@example.com';
        RAISE NOTICE 'Admin user already exists with ID: %', admin_user_id;
    END IF;
    
    -- Step 2: Associate all existing FinancialAccounts with admin user
    UPDATE "FinancialAccounts"
    SET "UserId" = admin_user_id
    WHERE "UserId" IS NULL;
    
    RAISE NOTICE 'Associated % FinancialAccounts with admin user', 
        (SELECT COUNT(*) FROM "FinancialAccounts" WHERE "UserId" = admin_user_id);
    
    -- Step 3: Associate all existing CategorizationRules with admin user
    UPDATE "CategorizationRules"
    SET "UserId" = admin_user_id
    WHERE "UserId" IS NULL;
    
    RAISE NOTICE 'Associated % CategorizationRules with admin user', 
        (SELECT COUNT(*) FROM "CategorizationRules" WHERE "UserId" = admin_user_id);
    
    -- Step 4: Associate all existing WhatIfScenarios with admin user
    UPDATE "WhatIfScenarios"
    SET "UserId" = admin_user_id
    WHERE "UserId" IS NULL;
    
    RAISE NOTICE 'Associated % WhatIfScenarios with admin user', 
        (SELECT COUNT(*) FROM "WhatIfScenarios" WHERE "UserId" = admin_user_id);
    
    RAISE NOTICE 'Data migration completed successfully!';
    RAISE NOTICE 'You can now login with your registered admin account';
    RAISE NOTICE 'IMPORTANT: Ensure you have a strong password and enable 2FA if available!';
END $$;
