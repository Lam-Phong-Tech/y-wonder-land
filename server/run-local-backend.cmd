@echo off
setlocal

cd /d "%~dp0"

set PORT=3000
set WEB_AUTH_MODE=mock
set JWT_SECRET=local-test-secret
set REALTIME_SHARED_ROOMS=city,mine
set REALTIME_MAX_ROOM_PLAYERS=20

echo Starting YWONDERLAND local backend on http://127.0.0.1:%PORT%
echo Close this window to stop the local backend.
echo.

"C:\Program Files\nodejs\node.exe" index.js

echo.
echo Local backend stopped. Press any key to close this window.
pause >nul
