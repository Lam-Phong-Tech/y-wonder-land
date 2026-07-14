# Deploy backend stub on Windows

This is the minimum setup for the current Unity demo backend. It is not the final production DB/backend.

## 1. Required network state

- `ywonder.net` stays on the existing web VPS.
- `api.ywonder.net` A record points to the physical game API server public IP.
- Router forwards TCP `80` and `443` to the Windows server.
- Windows Defender Firewall allows inbound TCP `80` and `443`.

Current Unity config uses:

```text
https://api.ywonder.net
```

## 2. Run Node server locally

Install Node.js LTS, then on the server:

```powershell
cd D:\LamGameUnity\BaChuKhuRung3D\server
npm install
$env:PORT="3000"
$env:JWT_SECRET="change-this-long-random-secret"
$env:WEB_AUTH_MODE="mock"
npm start
```

For the web auth contract handed off by the web team, switch to:

```powershell
$env:WEB_AUTH_MODE="http"
$env:WEB_AUTH_LOGIN_URL="https://api.ywonder.net/api/game/auth"
$env:WEB_AUTH_SECRET="<GAME_API_SECRET from VPS .env>"
$env:REALTIME_SHARED_ROOMS="city,mine"
$env:REALTIME_MAX_ROOM_PLAYERS="20"
```

Do not put `GAME_API_SECRET` in Unity. Only the Node game server should read it.
If `api.ywonder.net` SSL is still blocked by infrastructure, temporarily set `WEB_AUTH_LOGIN_URL=https://ywonder.net/api/game/auth`.

Local checks:

```powershell
irm http://127.0.0.1:3000/health
irm http://127.0.0.1:3000/game-api/health
```

Both should return `ok = true`.

## 3. Configure Caddy

Copy the useful parts from `server/Caddyfile.example` into the real Caddyfile.

If the website root is hosted by another app on the same machine, keep that web config and only add:

```caddyfile
handle /game-api* {
    reverse_proxy 127.0.0.1:3000
}
```

Reload Caddy after saving the Caddyfile.

## 3b. If Nginx holds ports 80/443

REST proxy alone is not enough for realtime. Nginx must forward WebSocket Upgrade requests:

```nginx
location /realtime {
    proxy_pass http://127.0.0.1:3000;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_set_header Host $host;
    proxy_read_timeout 75s;
}

location / {
    proxy_pass http://127.0.0.1:3000;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
}
```

If the team uses legacy `/game-api`, add the same Upgrade headers for `/game-api/realtime`.

## 4. Public checks

From another machine or phone using mobile data:

```powershell
irm https://ywonder.net/game-api/health
irm https://api.ywonder.net/health
```

Realtime check after REST works:

```powershell
# Browser/Unity should connect to:
wss://api.ywonder.net/realtime
```

For the Unity build, `https://api.ywonder.net/health` is the important check.
`https://ywonder.net/game-api/health` is only needed if the team later decides to proxy the game API through the web domain.

## 5. Current limitation

This backend only covers:

- register
- login
- player profile
- tutorial/character-created flags

Point, inventory, farm, animals, and resources still need the next backend phase with a real database.
Realtime MVP currently covers chat, presence, and remote player visuals only; it is not yet server-authoritative gameplay.
