# GitHub Actions Self-Hosted Runner Setup

This guide covers setting up a self-hosted GitHub Actions runner in WSL for automated Bankweave deployments.

## Why Self-Hosted Runner?

- **Direct Access**: Runner has direct access to your WSL environment, Docker, and Nginx
- **Zero Secrets**: No need to store server credentials in GitHub
- **Fast Deployments**: Local deployment without SSH overhead
- **Free**: Unlimited minutes for private repositories

## Prerequisites

- WSL2 with Ubuntu/Debian
- Docker installed in WSL
- Nginx configured (see DEPLOYMENT.md)
- Admin access to GitHub repository settings

## Step 1: Setup the Runner

```bash
# In WSL, navigate to project
cd /mnt/d/Projects/Bankweave

# Make the setup script executable
chmod +x setup-runner.sh

# Run the setup script
./setup-runner.sh
```

The script will:
1. Download the latest GitHub Actions runner
2. Prompt you for a registration token
3. Configure the runner with name `bankweave-wsl2-runner`
4. Install it as a systemd service
5. Start the runner automatically

## Step 2: Get Runner Registration Token

1. Go to your GitHub repository: `https://github.com/AmedeoV/Bankweave`
2. Navigate to **Settings** → **Actions** → **Runners**
3. Click **New self-hosted runner**
4. Select **Linux** as the operating system
5. Copy the token from the configuration command (starts with `A...`)
6. Paste it when prompted by the setup script

## Step 3: Verify Runner is Connected

1. Go to: `https://github.com/AmedeoV/Bankweave/settings/actions/runners`
2. You should see `bankweave-wsl2-runner` with a green "Idle" status
3. Labels: `self-hosted`, `Linux`, `X64`, `wsl`, `linux`, `bankweave`

## Runner Management

### Check Runner Status
```bash
cd ~/actions-runner-bankweave
sudo ./svc.sh status
```

### Stop Runner
```bash
cd ~/actions-runner-bankweave
sudo ./svc.sh stop
```

### Start Runner
```bash
cd ~/actions-runner-bankweave
sudo ./svc.sh start
```

### Restart Runner
```bash
cd ~/actions-runner-bankweave
sudo ./svc.sh stop
sudo ./svc.sh start
```

### View Runner Logs
```bash
# View service logs
journalctl -u actions.runner.* -f

# View runner logs
cd ~/actions-runner-bankweave
tail -f _diag/Runner_*.log
```

### Remove Runner
```bash
cd ~/actions-runner-bankweave

# Stop and uninstall service
sudo ./svc.sh stop
sudo ./svc.sh uninstall

# Remove runner from GitHub (you'll need a new token)
./config.sh remove --token YOUR_TOKEN
```

## Using the Runner in GitHub Actions

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Production

on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  deploy:
    runs-on: [self-hosted, bankweave]
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Create .env file
        run: |
          cat > .env << EOF
          POSTGRES_DB=${{ secrets.POSTGRES_DB }}
          POSTGRES_USER=${{ secrets.POSTGRES_USER }}
          POSTGRES_PASSWORD=${{ secrets.POSTGRES_PASSWORD }}
          JWT_SECRET=${{ secrets.JWT_SECRET }}
          JWT_ISSUER=Bankweave
          JWT_AUDIENCE=BankweaveUsers
          ASPNETCORE_ENVIRONMENT=Production
          EOF
      
      - name: Deploy with Docker Compose
        run: |
          docker-compose down
          docker-compose up -d --build
      
      - name: Wait for services
        run: |
          sleep 10
          docker-compose ps
      
      - name: Check application health
        run: |
          curl -f http://localhost:8083 || exit 1
      
      - name: Reload Nginx
        run: |
          sudo nginx -t
          sudo systemctl reload nginx
```

### Required GitHub Secrets

Add these secrets in **Settings** → **Secrets and variables** → **Actions**:
- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `JWT_SECRET`

## Automatic Deployment Workflow

Once configured, every push to `main` will:
1. ✅ Checkout latest code on your WSL runner
2. ✅ Create `.env` file from GitHub secrets
3. ✅ Build and deploy Docker containers
4. ✅ Verify deployment health
5. ✅ Reload Nginx configuration

## Security Best Practices

1. **Runner Isolation**: The runner runs in your WSL environment
2. **Secrets Management**: Use GitHub Secrets for sensitive data
3. **Token Security**: Keep registration tokens private
4. **Limited Access**: Only grant necessary permissions to the runner
5. **Monitor Logs**: Regularly check runner and deployment logs

## Troubleshooting

### Runner Not Appearing in GitHub

- Check if runner service is running: `sudo ./svc.sh status`
- Verify network connectivity: `curl https://github.com`
- Check runner logs: `tail -f ~/actions-runner-bankweave/_diag/Runner_*.log`

### Permission Errors

```bash
# Ensure user has Docker permissions
sudo usermod -aG docker $USER
newgrp docker

# Test Docker without sudo
docker ps
```

### Service Won't Start

```bash
# Check systemd logs
journalctl -u actions.runner.* -n 50

# Try manual start for debugging
cd ~/actions-runner-bankweave
./run.sh
```

### Deployment Fails

```bash
# Check Docker Compose logs
docker-compose logs -f

# Verify .env file was created
cat .env

# Check nginx configuration
sudo nginx -t
```

## Runner Auto-Start on Boot

The runner is installed as a systemd service and will automatically start when WSL starts. If you need to manually configure it:

```bash
# Enable service
sudo systemctl enable actions.runner.*

# Check if enabled
sudo systemctl is-enabled actions.runner.*
```

## Updating the Runner

GitHub will automatically update the runner software. If you need to manually update:

```bash
cd ~/actions-runner-bankweave

# Stop service
sudo ./svc.sh stop

# Remove old runner
./config.sh remove --token YOUR_TOKEN

# Run setup script again
cd /mnt/d/Projects/Bankweave
./setup-runner.sh
```

## Multiple Runners

To run multiple runners for different projects:
- Use different directory names (e.g., `~/actions-runner-bankweave`, `~/actions-runner-hbdrop`)
- Give each runner a unique name
- Each runner can have different labels

## Monitoring

### Check Runner Uptime
```bash
systemctl status actions.runner.* | grep Active
```

### Monitor Resource Usage
```bash
# CPU and Memory usage
top -b -n 1 | grep Runner

# Docker container stats
docker stats --no-stream
```

### View Deployment History
Check workflow runs: `https://github.com/AmedeoV/Bankweave/actions`

## Next Steps

1. ✅ Set up the runner using `./setup-runner.sh`
2. ✅ Verify it appears in GitHub Settings
3. ✅ Add GitHub Secrets for deployment
4. ✅ Create `.github/workflows/deploy.yml`
5. ✅ Push to `main` and watch automatic deployment!

Your Bankweave app will now automatically deploy to https://bankweave.step0fail.com on every push! 🚀
