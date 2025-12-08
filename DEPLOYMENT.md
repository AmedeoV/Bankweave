# Bankweave Deployment Guide

## Deploying to https://bankweave.step0fail.com

This guide covers deploying Bankweave to a production environment with Nginx reverse proxy and Cloudflare.

## Prerequisites

- WSL with Ubuntu/Debian
- Docker & Docker Compose installed in WSL
- Nginx installed in WSL
- Domain configured in Cloudflare (bankweave.step0fail.com)
- Cloudflare Origin Certificate

## Step 1: Generate Cloudflare Origin Certificate

1. Log in to Cloudflare Dashboard
2. Select your domain `step0fail.com`
3. Go to **SSL/TLS** → **Origin Server**
4. Click **Create Certificate**
5. Choose:
   - **Let Cloudflare generate a private key and a CSR**
   - **Hostnames**: `*.step0fail.com` and `step0fail.com`
   - **Expiration**: 15 years
6. Click **Create**
7. Save the certificate and private key

## Step 2: Install SSL Certificates in WSL

```bash
# Create SSL directory
sudo mkdir -p /etc/nginx/ssl

# Create certificate file (paste the Origin Certificate from Cloudflare)
sudo nano /etc/nginx/ssl/bankweave.step0fail.com.pem

# Create private key file (paste the Private Key from Cloudflare)
sudo nano /etc/nginx/ssl/bankweave.step0fail.com.key

# Set proper permissions
sudo chmod 600 /etc/nginx/ssl/bankweave.step0fail.com.key
sudo chmod 644 /etc/nginx/ssl/bankweave.step0fail.com.pem
```

## Step 3: Configure Nginx

```bash
# Copy nginx configuration from the repo
sudo cp /mnt/d/Projects/Bankweave/nginx/bankweave.conf /etc/nginx/sites-available/bankweave.conf

# Create symbolic link to enable the site
sudo ln -s /etc/nginx/sites-available/bankweave.conf /etc/nginx/sites-enabled/

# Test nginx configuration
sudo nginx -t

# Reload nginx
sudo systemctl reload nginx
```

## Step 4: Configure Environment Variables

```bash
# In WSL, navigate to project directory
cd /mnt/d/Projects/Bankweave

# Copy the production environment template
cp .env.production .env

# Edit the .env file with secure values
nano .env
```

Generate secure secrets:
```bash
# Generate JWT Secret (at least 32 characters)
openssl rand -base64 48

# Generate strong database password
openssl rand -base64 32
```

Update `.env` with:
- Strong `POSTGRES_PASSWORD`
- Strong `JWT_SECRET` (at least 32 characters)
- Keep other defaults or customize as needed

## Step 5: Configure Cloudflare DNS

1. Go to Cloudflare Dashboard → DNS
2. Add/Update A record:
   - **Type**: A
   - **Name**: bankweave
   - **IPv4 address**: Your server's public IP
   - **Proxy status**: ✅ Proxied (orange cloud)
   - **TTL**: Auto

## Step 6: Configure Cloudflare SSL/TLS Settings

1. Go to **SSL/TLS** → **Overview**
2. Set encryption mode to: **Full (strict)**
3. Go to **SSL/TLS** → **Edge Certificates**
4. Enable:
   - ✅ Always Use HTTPS
   - ✅ Automatic HTTPS Rewrites
   - ✅ Minimum TLS Version: 1.2

## Step 7: Deploy Bankweave with Docker

```bash
# In WSL, navigate to project directory
cd /mnt/d/Projects/Bankweave

# Stop any existing containers
docker-compose down

# Build and start production containers
docker-compose up -d --build

# Check container status
docker-compose ps

# View logs
docker-compose logs -f web
```

## Step 8: Verify Deployment

1. Visit https://bankweave.step0fail.com
2. You should see the Bankweave login page
3. Create a new account or login
4. Test CSV import functionality

## Troubleshooting

### Check Container Logs
```bash
docker-compose logs -f web
docker-compose logs -f postgres
```

### Check Nginx Logs
```bash
sudo tail -f /var/log/nginx/bankweave_access.log
sudo tail -f /var/log/nginx/bankweave_error.log
```

### Test Nginx Configuration
```bash
sudo nginx -t
```

### Restart Services
```bash
# Restart Nginx
sudo systemctl restart nginx

# Restart Docker containers
docker-compose restart
```

### Check if Port 8083 is Accessible
```bash
curl http://localhost:8083
```

### Database Connection Issues
- Verify database credentials in `.env` match what's in docker-compose.yml
- Check if PostgreSQL container is healthy: `docker-compose ps`
- Check PostgreSQL logs: `docker-compose logs postgres`

## Security Recommendations

1. **Keep secrets secure**: Never commit `.env` to git
2. **Use strong passwords**: Generate random passwords for database
3. **Regular backups**: Backup PostgreSQL data volume regularly
4. **Update regularly**: Keep Docker images and dependencies updated
5. **Monitor logs**: Regularly check application and nginx logs
6. **Firewall**: Only expose ports 80 and 443 to the internet

## Backup Database

```bash
# Backup PostgreSQL data
docker exec bankweave-postgres-prod pg_dump -U admin bankweavedb > backup_$(date +%Y%m%d).sql

# Restore from backup
cat backup_20231208.sql | docker exec -i bankweave-postgres-prod psql -U admin -d bankweavedb
```

## Update Deployment

```bash
# Pull latest changes
cd /mnt/d/Projects/Bankweave
git pull

# Rebuild and restart containers
docker-compose down
docker-compose up -d --build

# Check logs
docker-compose logs -f
```

## Maintenance Commands

```bash
# View running containers
docker-compose ps

# Stop containers
docker-compose stop

# Start containers
docker-compose start

# Remove containers (keeps data)
docker-compose down

# Remove containers and volumes (deletes all data!)
docker-compose down -v

# View resource usage
docker stats
```

## Production Configuration

The production setup includes:
- **Port**: 8083 (internal), 443 (HTTPS via Nginx)
- **Database**: PostgreSQL 16 with persistent volume
- **Environment**: Production (optimized builds)
- **SSL**: Cloudflare Origin Certificate with Full (strict) mode
- **Reverse Proxy**: Nginx with security headers
- **Max Upload**: 50MB for CSV files
- **Auto-restart**: Containers restart unless manually stopped

## URLs

- **Production**: https://bankweave.step0fail.com
- **Development**: http://localhost:8082 (docker-compose.dev.yml)
- **Database Port**: 5436 (host) → 5432 (container)
