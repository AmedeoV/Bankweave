# Docker Setup Guide

## Overview

This project has two Docker configurations:

- **Development**: Hot reload enabled for rapid development
- **Production**: Optimized build with no source mounting

## Development Mode (with Hot Reload)

### Start Development Environment
```powershell
docker-compose -f docker-compose.dev.yml up -d --build
```

### Features
- 🔥 **Hot Reload**: Changes to C#, HTML, CSS, JS files automatically reload
- 📦 **Source Mounting**: Your local files are mounted into the container
- 🐛 **Debug Mode**: Full logging and development tools enabled
- 🗄️ **Database**: PostgreSQL on port 5435

### Access
- **Application**: http://localhost:8082
- **Database**: localhost:5435

### View Logs
```powershell
docker logs -f bankweave-web-dev
```

### Stop Development Environment
```powershell
docker-compose -f docker-compose.dev.yml down
```

### Rebuild After Package Changes
```powershell
docker-compose -f docker-compose.dev.yml up -d --build
```

## Production Mode (Optimized)

### Start Production Environment
```powershell
docker-compose up -d --build
```

### Features
- 🚀 **Optimized**: Release build with no development overhead
- 🔒 **Secure**: No source code mounting
- 📦 **Self-Contained**: All files baked into the image
- 🔄 **Auto-Restart**: Containers restart automatically
- 🗄️ **Database**: PostgreSQL on port 5436

### Access
- **Application**: http://localhost:8083
- **Database**: localhost:5436

### View Logs
```powershell
docker logs -f bankweave-web-prod
```

### Stop Production Environment
```powershell
docker-compose down
```

## Port Reference

| Service | Dev Port | Prod Port |
|---------|----------|-----------|
| Web App | 8082 | 8083 |
| PostgreSQL | 5435 | 5436 |

## Database Data

Each environment has its own persistent volume:
- **Dev**: `postgres_data_dev`
- **Prod**: `postgres_data_prod`

### Reset Database (Dev)
```powershell
docker-compose -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.dev.yml up -d
```

### Reset Database (Prod)
```powershell
docker-compose down -v
docker-compose up -d
```

## Common Commands

### Rebuild Everything (Dev)
```powershell
docker-compose -f docker-compose.dev.yml down
docker-compose -f docker-compose.dev.yml up -d --build --force-recreate
```

### Access Database (Dev)
```powershell
docker exec -it bankweave-postgres-dev psql -U admin -d bankweavedb
```

### Access Database (Prod)
```powershell
docker exec -it bankweave-postgres-prod psql -U admin -d bankweavedb
```

### View Container Shell (Dev)
```powershell
docker exec -it bankweave-web-dev /bin/bash
```

## Hot Reload Notes

### What Reloads Automatically (Dev Mode):
- ✅ C# files (Controllers, Models, Entities, etc.)
- ✅ HTML files (wwwroot)
- ✅ CSS files (wwwroot)
- ✅ JavaScript files (wwwroot)
- ✅ JSON configuration files

### What Requires Rebuild:
- 🔄 NuGet package changes (.csproj)
- 🔄 Dockerfile changes
- 🔄 docker-compose.yml changes
- 🔄 New file creation (sometimes)

### Troubleshooting Hot Reload:
If changes aren't being detected:
```powershell
# Check if watch is running
docker logs bankweave-web-dev | Select-String "watch"

# Restart the container
docker-compose -f docker-compose.dev.yml restart web

# Force rebuild
docker-compose -f docker-compose.dev.yml up -d --build
```

## Switching Between Environments

### Stop Dev, Start Prod:
```powershell
docker-compose -f docker-compose.dev.yml down
docker-compose up -d
```

### Stop Prod, Start Dev:
```powershell
docker-compose down
docker-compose -f docker-compose.dev.yml up -d
```

### Run Both (Different Ports):
```powershell
docker-compose -f docker-compose.dev.yml up -d
docker-compose up -d
```

## Best Practices

### Development Workflow:
1. Start dev environment: `docker-compose -f docker-compose.dev.yml up -d`
2. Make changes to code
3. Changes auto-reload (C#) or refresh browser (HTML/CSS/JS)
4. View logs if needed: `docker logs -f bankweave-web-dev`
5. Stop when done: `docker-compose -f docker-compose.dev.yml down`

### Production Deployment:
1. Test in dev mode first
2. Stop dev: `docker-compose -f docker-compose.dev.yml down`
3. Build prod: `docker-compose build`
4. Start prod: `docker-compose up -d`
5. Verify: http://localhost:8083
6. Monitor logs: `docker logs -f bankweave-web-prod`

## Environment Variables

### Development (.env.dev)
Create a `.env.dev` file for development-specific settings:
```
ASPNETCORE_ENVIRONMENT=Development
Jwt__Secret=YourDevSecretKey
```

### Production (.env)
Create a `.env` file for production settings:
```
ASPNETCORE_ENVIRONMENT=Production
Jwt__Secret=YourProductionSecretKey
```

## Performance Notes

- **Dev Mode**: Slower startup (~10-15 seconds) due to watch initialization
- **Prod Mode**: Fast startup (~3-5 seconds) with optimized Release build
- **Dev Hot Reload**: Changes reflect in 2-5 seconds
- **Prod Rebuild**: Full rebuild takes ~30-40 seconds
