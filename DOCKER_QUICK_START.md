# Quick Docker Commands

## Development Mode (Hot Reload Enabled) 🔥

### Start Development
```powershell
docker-compose -f docker-compose.dev.yml up -d --build
```

### View Logs
```powershell
docker logs -f bankweave-web-dev
```

### Stop Development
```powershell
docker-compose -f docker-compose.dev.yml down
```

### Access Points
- App: http://localhost:8082
- DB: localhost:5435

---

## Production Mode (Optimized Build) 🚀

### Start Production
```powershell
docker-compose up -d --build
```

### View Logs
```powershell
docker logs -f bankweave-web-prod
```

### Stop Production
```powershell
docker-compose down
```

### Access Points
- App: http://localhost:8083
- DB: localhost:5436

---

## Current Status: DEVELOPMENT MODE ACTIVE ✅

Hot reload is now enabled! Changes to C#, HTML, CSS, and JS files will automatically reload.

Test it:
1. Edit any file in Controllers/, wwwroot/, etc.
2. Watch the logs: `docker logs -f bankweave-web-dev`
3. See hot reload messages appear!
