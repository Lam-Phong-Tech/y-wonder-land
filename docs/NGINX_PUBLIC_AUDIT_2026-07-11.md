# Public Nginx audit - 2026-07-11

## Scope

Read-only audit of the public path for `api.ywonder.net` after DNS and firewall
changes. No VPS file, service, firewall rule, certificate, or process was changed.

## Verified state

- Public DNS resolves `api.ywonder.net` to `42.96.18.14` through the system,
  Cloudflare and Google resolvers.
- Nginx is active/enabled and owns public ports `80/443`.
- HTTP redirects to HTTPS and the Let's Encrypt certificate is valid.
- Public ports `3000/5432/8080` remain closed.
- The hardened game backend is healthy on `127.0.0.1:3000` with
  `storage.mode=postgres`.
- Private Caddy remains healthy on `127.0.0.1:8080` and proxies to the game
  backend. It does not compete for public ports.
- PostgreSQL remains local on `127.0.0.1:5432`.

## Existing public ownership

The enabled Nginx site is `/etc/nginx/sites-enabled/ywonder.net.conf`.
For `api.ywonder.net` it currently routes:

- `/api/game/` to the existing web API upstream on `127.0.0.1:3033`.
- Every other path `/` to `127.0.0.1:3036`.

The service on `3036` identifies as `ywonderland-main-game-api` and runs as the
non-root user `greenxland` from `/var/www/ywonder/game-api-server`. Its public
`/health` works, while `/player/bootstrap` and `/realtime` return `404`.

The old `/api/game/auth` and `/api/game/balance` routes still exist and return
`401` to unauthenticated probes. They must be preserved until the web team says
otherwise.

## Safest integration plan

Do not replace Nginx with Caddy and do not overwrite the existing upstreams.
Add one isolated namespace for the PostgreSQL game backend:

- `/game-api/realtime` -> `127.0.0.1:3000`, with HTTP/1.1 WebSocket Upgrade.
- `/game-api/` -> `127.0.0.1:3000`, preserving the full request URI.

The Node server already mounts the same REST routes at `/game-api/*` and accepts
WebSocket at `/game-api/realtime`. Unity can therefore use:

```text
https://api.ywonder.net/game-api
```

This keeps the existing public routes unchanged:

- `/api/game/*` remains owned by the web API.
- `/` and `/health` remain owned by `ywonderland-main-game-api`.

## Controlled change and rollback

1. Create a timestamped root-only backup of the enabled Nginx site.
2. Add only the two `/game-api` locations.
3. Run `nginx -t`; do not reload if validation fails.
4. Reload Nginx without restarting Node/PostgreSQL.
5. Verify `https://api.ywonder.net/game-api/health` returns
   `storage.mode=postgres`.
6. Verify REST registration/login/bootstrap and WSS at
   `wss://api.ywonder.net/game-api/realtime`.
7. Recheck `/api/game/auth`, `/api/game/balance`, `/health`, and closed internal
   ports.
8. On any failure, restore the backup, run `nginx -t`, reload, and verify the old
   public API before stopping.

## Follow-up risks

- The existing public Node service exposes `X-Powered-By`, permissive CORS and a
  stack trace for malformed JSON. This is outside the hardened game backend and
  should be fixed separately without blocking the isolated `/game-api` route.
- `certbot.timer` is enabled but reported inactive during the audit. The current
  certificate is valid, but renewal scheduling must be verified before final
  production sign-off.
- Unity URL and EXE/APK builds must not change until external REST/WSS acceptance
  passes through the new namespace.

## Implemented result

The controlled change completed at `2026-07-11T10:45:09Z`:

- Nginx backup SHA-256:
  `87c987eb81767be2e121a4a3fc035600329cc95b73595ca4ddb192650c55a878`.
- Active config SHA-256:
  `b7b6cc5b28d89b37e35b94ac26099e992efef25d199cd6489e97d8248b5185d8`.
- `nginx -t` and reload passed; all four backend/proxy/database services stayed
  active.
- Public automated 20-client REST/WSS acceptance passed from the Windows work
  machine. P95 auth/bootstrap/WebSocket connect was `1666.4/64.9/173.7 ms`.
- Public full Phase 1 passed registration, login, atomic shop, persistence,
  idempotency, farm-state, realtime chat and duplicate-session replacement.
- All public test accounts were removed; the three P1 baseline accounts remain.
- External checks reconfirmed only `80/443` are public, while
  `3000/5432/8080` remain closed.

Unity may now use `https://api.ywonder.net/game-api`; real EXE/APK device
acceptance remains pending.
