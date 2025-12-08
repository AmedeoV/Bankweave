# Security Configuration Guide

## 🔐 Before Deployment

This repository contains **placeholder credentials** that must be changed before deploying to production.

### Required Actions

1. **Update `.env` file** (never commit this file - it's gitignored):
   ```env
   POSTGRES_USER=your_secure_username
   POSTGRES_PASSWORD=your_strong_random_password
   JWT_SECRET=your_64_character_random_string
   ```

2. **Generate secure credentials**:
   ```powershell
   # Generate JWT Secret (64 characters)
   $jwtSecret = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})
   
   # Generate Database Password (40 characters)
   $dbPassword = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 40 | ForEach-Object {[char]$_})
   
   Write-Host "JWT_SECRET=$jwtSecret"
   Write-Host "POSTGRES_PASSWORD=$dbPassword"
   ```

3. **Update appsettings files** - Already configured to use environment variables in Docker

4. **Create admin user** - Use the `/api/auth/register` endpoint or update `Migrations/AssociateDataWithAdmin.sql` with your credentials

## 🚨 Security Checklist

- [ ] `.env` file contains unique, strong random values
- [ ] `.env` is in `.gitignore` (already configured)
- [ ] `appsettings.json` and `appsettings.Development.json` contain only placeholder values
- [ ] Database credentials changed from defaults
- [ ] JWT secret is at least 64 characters long
- [ ] Admin user created with a strong password
- [ ] No hardcoded passwords in migration scripts
- [ ] Production database has different credentials than development

## 📝 Environment Variables

The application uses environment variables with the following precedence:
1. Environment variables (highest priority)
2. `.env` file values
3. `appsettings.json` fallback values (placeholders only)

### Docker Configuration

Docker Compose automatically loads `.env` file and uses `${VAR:-default}` syntax for fallback values.

**Example:**
```yaml
POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-secretpass}
```
This uses the `.env` value if set, otherwise falls back to `secretpass` (for development only).

## 🔒 Production Deployment

For production environments:

1. **Never use default credentials**
2. **Never commit `.env` to version control**
3. **Use environment-specific `.env` files** (e.g., `.env.production`)
4. **Enable HTTPS** and update CORS settings
5. **Implement rate limiting** on authentication endpoints
6. **Enable database SSL connections**
7. **Regularly rotate JWT secrets** and update active tokens
8. **Use secrets management** (Azure Key Vault, AWS Secrets Manager, etc.)

## ⚠️ Common Security Mistakes to Avoid

- ❌ Committing `.env` file to git
- ❌ Using default passwords in production
- ❌ Hardcoding credentials in source code
- ❌ Using the same credentials for dev and prod
- ❌ Sharing JWT secrets between environments
- ❌ Setting weak passwords (< 12 characters)

## 📚 Additional Resources

- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
