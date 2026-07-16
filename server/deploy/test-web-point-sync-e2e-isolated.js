const crypto = require("crypto");
const fs = require("fs");
const http = require("http");
const net = require("net");
const os = require("os");
const path = require("path");
const { spawn, spawnSync } = require("child_process");

const GAME_ENV_FILE = "/etc/ywonder-game/game-server.env";
const CANONICAL_WEB_ROOT = "/var/www/ywonder";
const PRODUCTION_WEB_ENV_FILE = path.join(CANONICAL_WEB_ROOT, ".env");
const GAME_SERVICE = "ywonder-game-server.service";
const WEB_SERVICE = "greenxland.service";
const TEMP_DATABASE_PREFIX = "yw_point_e2e_";
const PRODUCTION_GAME_HEALTH_URL = "http://127.0.0.1:3000/health";
const PRODUCTION_WEB_HEALTH_URL = "http://127.0.0.1:3033/api/health";
const PRODUCTION_TOPUP_URL = "http://127.0.0.1:3000/internal/web/point-credit";
let currentStage = "startup";

function enterStage(name) {
  currentStage = name;
  console.log(`[web-point-e2e-isolated] stage=${name}`);
}

function safeErrorMessage(error) {
  return String(error && error.message ? error.message : error || "UNKNOWN_ERROR")
    .replace(/postgres(?:ql)?:\/\/[^\s]+/gi, "[REDACTED_POSTGRES_URL]")
    .replace(/file:\/[^\s]+/gi, "[REDACTED_FILE_URL]")
    .replace(/(?:password|secret|token)=([^\s&]+)/gi, "$1=[REDACTED]")
    .replace(/[\r\n\0]+/g, " ")
    .slice(-4000);
}

function assertSafeIdentifier(value, pattern, label) {
  const text = String(value || "");
  if (!pattern.test(text)) throw new Error(`Invalid ${label}.`);
  return text;
}

function assertSafeTemporaryRoot(value, label) {
  if (!value) throw new Error(`${label} is required.`);
  const resolved = fs.realpathSync(path.resolve(value));
  const tempRoot = fs.realpathSync(os.tmpdir());
  if (resolved === tempRoot || !resolved.startsWith(`${tempRoot}${path.sep}`)) {
    throw new Error(`${label} must be a dedicated directory below ${tempRoot}.`);
  }
  if (!fs.statSync(resolved).isDirectory()) throw new Error(`${label} is not a directory.`);
  return resolved;
}

function assertDisjointRoots(namedRoots) {
  const entries = Object.entries(namedRoots);
  for (let index = 0; index < entries.length; index += 1) {
    for (let other = index + 1; other < entries.length; other += 1) {
      const [leftName, left] = entries[index];
      const [rightName, right] = entries[other];
      if (left === right || left.startsWith(`${right}${path.sep}`) || right.startsWith(`${left}${path.sep}`)) {
        throw new Error(`${leftName} and ${rightName} must be disjoint E2E directories.`);
      }
    }
  }
}

function requireStageFiles(root, label, relativePaths) {
  for (const relativePath of relativePaths) {
    const candidate = path.join(root, relativePath);
    if (!fs.existsSync(candidate) || !fs.statSync(candidate).isFile()) {
      throw new Error(`${label} is missing ${relativePath}.`);
    }
  }
}

function reservePort() {
  return new Promise((resolve, reject) => {
    const server = net.createServer();
    server.once("error", reject);
    server.listen(0, "127.0.0.1", () => {
      const port = server.address().port;
      server.close((error) => error ? reject(error) : resolve(port));
    });
  });
}

function capture(child) {
  const output = { text: "" };
  const append = (chunk) => {
    output.text = `${output.text}${chunk.toString("utf8")}`.slice(-8000);
  };
  child.stdout.on("data", append);
  child.stderr.on("data", append);
  return output;
}

function writePidFile(runtimeRoot, label, child) {
  if (!child || !Number.isInteger(child.pid) || child.pid < 2) {
    throw new Error(`Isolated ${label} process did not expose a safe pid.`);
  }
  fs.writeFileSync(path.join(runtimeRoot, `${label}.pid`), `${child.pid}\n`, { mode: 0o600 });
}

function writeDatabaseMarker(runtimeRoot, databaseName) {
  assertSafeIdentifier(databaseName, /^yw_point_e2e_[a-z0-9_]{8,40}$/, "temporary database name");
  fs.writeFileSync(path.join(runtimeRoot, "database.name"), `${databaseName}\n`, { mode: 0o600 });
}

function removeDatabaseMarker(runtimeRoot) {
  fs.rmSync(path.join(runtimeRoot, "database.name"), { force: true });
}

async function waitForHttp(child, output, url, expectedStatus = 200) {
  const deadline = Date.now() + 30_000;
  while (Date.now() < deadline) {
    if (child.exitCode != null) {
      throw new Error(`Isolated process exited (${child.exitCode}): ${output.text}`);
    }
    try {
      const response = await fetch(url, { redirect: "manual" });
      if (response.status === expectedStatus) return;
    } catch (error) {
      // Process is still starting.
    }
    await new Promise((resolve) => setTimeout(resolve, 200));
  }
  throw new Error(`Timed out waiting for ${url}: ${output.text}`);
}

async function stopChild(child) {
  if (!child || child.exitCode != null) return;
  try {
    process.kill(-child.pid, "SIGTERM");
  } catch (error) {
    child.kill("SIGTERM");
  }
  const deadline = Date.now() + 5000;
  while (child.exitCode == null && Date.now() < deadline) {
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
  if (child.exitCode == null) {
    try {
      process.kill(-child.pid, "SIGKILL");
    } catch (error) {
      child.kill("SIGKILL");
    }
  }
}

function startCommitThenTimeoutProxy(port, targetPort) {
  const state = {
    intercepted: 0,
    committed: 0,
    passedThrough: 0,
    upstreamStatus: 0,
  };
  const sockets = new Set();
  const server = http.createServer((incoming, outgoing) => {
    const chunks = [];
    incoming.on("data", (chunk) => chunks.push(chunk));
    incoming.on("error", () => outgoing.destroy());
    incoming.on("end", () => {
      const shouldIntercept = state.intercepted === 0
        && incoming.method === "POST"
        && incoming.url === "/internal/web/point-credit";
      if (shouldIntercept) state.intercepted += 1;

      const upstream = http.request({
        hostname: "127.0.0.1",
        port: targetPort,
        path: incoming.url,
        method: incoming.method,
        headers: {
          ...incoming.headers,
          host: `127.0.0.1:${targetPort}`,
        },
        agent: false,
      }, (upstreamResponse) => {
        const responseChunks = [];
        upstreamResponse.on("data", (chunk) => responseChunks.push(chunk));
        upstreamResponse.on("end", () => {
          const status = Number(upstreamResponse.statusCode || 502);
          state.upstreamStatus = status;
          if (shouldIntercept && status >= 200 && status < 300) {
            state.committed += 1;
            const destroyTimer = setTimeout(() => outgoing.destroy(), 12_000);
            outgoing.once("close", () => clearTimeout(destroyTimer));
            return;
          }

          if (!shouldIntercept) state.passedThrough += 1;
          if (outgoing.destroyed) return;
          outgoing.writeHead(status, upstreamResponse.headers);
          outgoing.end(Buffer.concat(responseChunks));
        });
      });
      upstream.on("error", () => {
        if (!outgoing.destroyed) {
          outgoing.writeHead(502, { "Content-Type": "application/json" });
          outgoing.end('{"error":"ISOLATED_FAULT_PROXY_UPSTREAM_FAILED"}');
        }
      });
      upstream.end(Buffer.concat(chunks));
    });
  });
  server.on("connection", (socket) => {
    sockets.add(socket);
    socket.once("close", () => sockets.delete(socket));
  });

  return new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(port, "127.0.0.1", () => {
      server.removeListener("error", reject);
      resolve({ server, sockets, state });
    });
  });
}

async function closeFaultProxy(proxy) {
  if (!proxy || !proxy.server) return;
  for (const socket of proxy.sockets) socket.destroy();
  await new Promise((resolve, reject) => {
    proxy.server.close((error) => error ? reject(error) : resolve());
  });
}

function runChecked(executable, args, options = {}) {
  const result = spawnSync(executable, args, {
    encoding: "utf8",
    maxBuffer: 8 * 1024 * 1024,
    ...options,
  });
  if (result.status !== 0) {
    const output = `${result.stdout || ""}\n${result.stderr || ""}`.trim().slice(-4000);
    throw new Error(`${path.basename(executable)} failed (${result.status}): ${output}`);
  }
  return String(result.stdout || "").trim();
}

function systemdServiceUser(serviceName) {
  const user = runChecked("systemctl", ["show", "--property=User", "--value", serviceName]).trim();
  return assertSafeIdentifier(user, /^[a-z_][a-z0-9_-]*[$]?$/i, `systemd user for ${serviceName}`);
}

function accountHome(user) {
  const fields = runChecked("getent", ["passwd", user]).split(":");
  if (fields.length < 7 || !path.isAbsolute(fields[5])) throw new Error(`No safe home for ${user}.`);
  return fields[5];
}

function accountGroup(user) {
  return assertSafeIdentifier(
    runChecked("id", ["-gn", user]),
    /^[a-z_][a-z0-9_-]*[$]?$/i,
    `primary group for ${user}`
  );
}

function runAsUserChecked(user, executable, args, options = {}) {
  return runChecked("runuser", [
    "-u",
    user,
    "--preserve-environment",
    "--",
    executable,
    ...args,
  ], options);
}

function runGameDatabaseScript(user, serverRoot, databaseUrl, script, extraEnv = {}) {
  return runAsUserChecked(user, "/usr/local/bin/node", ["-e", script], {
    cwd: serverRoot,
    env: {
      ...process.env,
      ...extraEnv,
      DATABASE_URL: databaseUrl,
      GAME_SERVER_ROOT: serverRoot,
      NODE_PATH: path.join(serverRoot, "node_modules"),
      PGUSER: user,
      USER: user,
      LOGNAME: user,
      HOME: accountHome(user),
    },
  });
}

function parseProductionDatabase(productionDatabaseUrl, runId, expectedOwner) {
  const parsed = new URL(productionDatabaseUrl);
  if (!/^postgres(?:ql)?:$/.test(parsed.protocol)) {
    throw new Error("Production game database is not PostgreSQL.");
  }
  const hostname = parsed.hostname.toLowerCase();
  const socketHost = parsed.searchParams.get("host") || "";
  const allowedSocketHosts = new Set(["/var/run/postgresql", "/run/postgresql"]);
  if (hostname) {
    if (!["127.0.0.1", "localhost", "::1"].includes(hostname) || socketHost) {
      throw new Error("Isolated E2E only supports local PostgreSQL without host overrides.");
    }
  } else if (!allowedSocketHosts.has(socketHost)) {
    throw new Error("Isolated E2E requires the approved local PostgreSQL Unix socket.");
  }

  const fallbackOwner = assertSafeIdentifier(
    expectedOwner,
    /^[a-z_][a-z0-9_-]{0,62}$/i,
    "expected PostgreSQL owner"
  );
  const owner = decodeURIComponent(parsed.username || fallbackOwner);
  assertSafeIdentifier(owner, /^[a-z_][a-z0-9_-]{0,62}$/i, "PostgreSQL owner");
  if (owner !== fallbackOwner) {
    throw new Error("PostgreSQL URL owner does not match the game systemd service user.");
  }
  const productionName = decodeURIComponent(parsed.pathname.replace(/^\//, ""));
  assertSafeIdentifier(productionName, /^[a-z_][a-z0-9_-]{0,62}$/i, "production database name");
  if (productionName.startsWith(TEMP_DATABASE_PREFIX)) {
    throw new Error("Production database name collides with the E2E namespace.");
  }

  const temporaryName = `${TEMP_DATABASE_PREFIX}${runId}`;
  assertSafeIdentifier(temporaryName, /^yw_point_e2e_[a-z0-9_]{8,40}$/, "temporary database name");
  if (temporaryName.length > 63 || temporaryName === productionName) {
    throw new Error("Temporary database name is unsafe.");
  }

  const isolated = new URL(parsed.toString());
  isolated.pathname = `/${temporaryName}`;
  isolated.searchParams.delete("options");
  return { owner, productionName, temporaryName, isolatedUrl: isolated.toString() };
}

function databaseExists(databaseName) {
  assertSafeIdentifier(databaseName, /^yw_point_e2e_[a-z0-9_]{8,40}$/, "temporary database name");
  const sql = `select count(*) from pg_database where datname='${databaseName}'`;
  return runChecked("runuser", [
    "-u",
    "postgres",
    "--",
    "psql",
    "-d",
    "postgres",
    "-At",
    "-v",
    "ON_ERROR_STOP=1",
    "-c",
    sql,
  ]) === "1";
}

function createTemporaryGameDatabase(owner, databaseName) {
  if (databaseExists(databaseName)) throw new Error("Temporary E2E database already exists.");
  runChecked("runuser", [
    "-u",
    "postgres",
    "--",
    "createdb",
    "--template=template0",
    "--encoding=UTF8",
    `--owner=${owner}`,
    databaseName,
  ]);
  if (!databaseExists(databaseName)) throw new Error("Temporary E2E database was not created.");
}

function dropTemporaryGameDatabase(databaseName) {
  assertSafeIdentifier(databaseName, /^yw_point_e2e_[a-z0-9_]{8,40}$/, "temporary database name");
  runChecked("runuser", [
    "-u",
    "postgres",
    "--",
    "dropdb",
    "--if-exists",
    "--force",
    databaseName,
  ]);
  if (databaseExists(databaseName)) throw new Error("Temporary E2E database still exists after cleanup.");
}

function migrateTemporaryGameDatabase(user, serverRoot, databaseUrl) {
  const script = `
    const fs = require("fs");
    const path = require("path");
    const { Pool } = require("pg");
    const pool = new Pool({ connectionString: process.env.DATABASE_URL, max: 1 });
    (async () => {
      await pool.query("create table if not exists schema_migrations (version text primary key, applied_at timestamptz not null default now())");
      const dir = path.join(process.env.GAME_SERVER_ROOT, "migrations");
      const files = fs.readdirSync(dir).filter((name) => /^\\d+_.+\\.sql$/i.test(name)).sort();
      for (const file of files) {
        const version = file.replace(/\\.sql$/i, "");
        await pool.query("begin");
        try {
          await pool.query(fs.readFileSync(path.join(dir, file), "utf8"));
          await pool.query("insert into schema_migrations(version) values ($1)", [version]);
          await pool.query("commit");
        } catch (error) {
          await pool.query("rollback");
          throw error;
        }
      }
    })().finally(() => pool.end());
  `;
  runGameDatabaseScript(user, serverRoot, databaseUrl, script);
}

function runPostgresStoreSmoke(user, serverRoot, databaseUrl) {
  const output = runAsUserChecked(user, "/usr/local/bin/npm", ["run", "test:postgres"], {
    cwd: serverRoot,
    env: {
      ...process.env,
      DATABASE_URL: "",
      POSTGRES_TEST_DATABASE_URL: databaseUrl,
      POSTGRES_TEST_KEEP_SCHEMA: "false",
      USER: user,
      LOGNAME: user,
      HOME: accountHome(user),
    },
  });
  if (!output.includes("[postgres-smoke] PASS")) {
    throw new Error("Isolated PostgreSQL store smoke did not report success.");
  }
}

function envFileValue(filePath, key) {
  const lines = fs.readFileSync(filePath, "utf8").split(/\r?\n/);
  let value = "";
  for (const line of lines) {
    const match = /^\s*([A-Za-z_][A-Za-z0-9_]*)=(.*)$/.exec(line);
    if (!match || match[1] !== key) continue;
    value = match[2].trim();
    if ((value.startsWith('"') && value.endsWith('"'))
        || (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }
  }
  return value;
}

function envList(value) {
  return Array.from(new Set(String(value || "")
    .split(",")
    .map((entry) => entry.trim())
    .filter(Boolean)));
}

function classifyProductionTopupState(config) {
  const webEnabled = String(config.webEnabled || "").trim().toLowerCase();
  const gameEnabled = String(config.gameEnabled || "").trim().toLowerCase();
  const webMode = String(config.webMode || "").trim().toLowerCase();
  const gameMode = String(config.gameMode || "").trim().toLowerCase();
  const allowRemote = String(config.allowRemote || "").trim().toLowerCase();
  const clientGrantsEnabled = String(config.clientGrantsEnabled || "").trim().toLowerCase();
  const webAllowed = envList(config.webAllowed);
  const gameAllowed = envList(config.gameAllowed);
  const blockedGrants = envList(config.blockedGrants);
  const disabled = (value) => value === "" || value === "false";
  const safeMode = (value) => value === "" || value === "canary";

  if (allowRemote !== "false") {
    throw new Error("Production web Point remote ingress must remain disabled.");
  }

  if (disabled(webEnabled) && disabled(gameEnabled)) {
    if (!safeMode(webMode) || !safeMode(gameMode)
        || webAllowed.length !== 0 || gameAllowed.length !== 0 || blockedGrants.length !== 0) {
      throw new Error("Production dormant web Point configuration is inconsistent.");
    }
    return { name: "dormant", callbackStatus: 404 };
  }

  if (webEnabled === "true" && gameEnabled === "true") {
    if (webMode !== "canary" || gameMode !== "canary"
        || webAllowed.length !== 1 || gameAllowed.length !== 1
        || blockedGrants.length !== 1 || clientGrantsEnabled !== "true"
        || webAllowed[0] !== gameAllowed[0] || webAllowed[0] !== blockedGrants[0]) {
      throw new Error("Production web Point canary is not scoped to exactly one matching identity.");
    }
    return { name: "exact-one-canary", callbackStatus: 401 };
  }

  throw new Error("Production web Point state is neither dormant nor an exact-one canary.");
}

function productionTopupState() {
  return classifyProductionTopupState({
    webEnabled: envFileValue(PRODUCTION_WEB_ENV_FILE, "WEB_TOPUP_ENABLED"),
    webMode: envFileValue(PRODUCTION_WEB_ENV_FILE, "WEB_TOPUP_MODE"),
    webAllowed: envFileValue(PRODUCTION_WEB_ENV_FILE, "WEB_TOPUP_ALLOWED_WEB_USER_IDS"),
    gameEnabled: envFileValue(GAME_ENV_FILE, "WEB_TOPUP_ENABLED"),
    gameMode: envFileValue(GAME_ENV_FILE, "WEB_TOPUP_MODE"),
    gameAllowed: envFileValue(GAME_ENV_FILE, "WEB_TOPUP_ALLOWED_WEB_USER_IDS"),
    allowRemote: envFileValue(GAME_ENV_FILE, "WEB_TOPUP_ALLOW_REMOTE"),
    clientGrantsEnabled: envFileValue(GAME_ENV_FILE, "CLIENT_ASSET_GRANTS_ENABLED"),
    blockedGrants: envFileValue(GAME_ENV_FILE, "CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS"),
  });
}

function sha256File(filePath) {
  return crypto.createHash("sha256").update(fs.readFileSync(filePath)).digest("hex");
}

function serviceSnapshot(serviceName) {
  const fields = ["ActiveState", "MainPID", "ActiveEnterTimestampMonotonic", "WorkingDirectory"];
  const output = runChecked("systemctl", ["show", ...fields.map((field) => `--property=${field}`), serviceName]);
  const result = {};
  for (const line of output.split(/\r?\n/)) {
    const index = line.indexOf("=");
    if (index > 0) result[line.slice(0, index)] = line.slice(index + 1);
  }
  if (result.ActiveState !== "active" || !/^\d+$/.test(result.MainPID || "") || result.MainPID === "0") {
    throw new Error(`Production service ${serviceName} is not stably active.`);
  }
  return result;
}

function assertProductionWebRoot(value) {
  const configured = String(value || "").trim();
  if (!configured || !fs.existsSync(configured)) {
    throw new Error("Production web service WorkingDirectory is missing.");
  }
  const resolved = fs.realpathSync(configured);
  if (!/^\/var\/www\/(?:ywonder|ywonder-releases\/[A-Za-z0-9._-]+)$/.test(resolved)) {
    throw new Error("Production web service WorkingDirectory is outside an approved release root.");
  }
  const activeEnv = path.join(resolved, ".env");
  if (!fs.existsSync(activeEnv)
      || fs.realpathSync(activeEnv) !== fs.realpathSync(PRODUCTION_WEB_ENV_FILE)) {
    throw new Error("Active web release does not use the canonical production environment.");
  }
  return resolved;
}

async function httpStatus(url, options = {}) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 5000);
  try {
    const response = await fetch(url, { redirect: "manual", ...options, signal: controller.signal });
    await response.arrayBuffer().catch(() => {});
    return response.status;
  } finally {
    clearTimeout(timeout);
  }
}

function productionFileHashes(gameWorkingDirectory, webWorkingDirectory) {
  const candidates = [
    GAME_ENV_FILE,
    PRODUCTION_WEB_ENV_FILE,
    path.join(webWorkingDirectory, ".env"),
    path.join(webWorkingDirectory, "package-lock.json"),
    path.join(webWorkingDirectory, "prisma", "schema.prisma"),
    path.join(webWorkingDirectory, "lib", "game-point-sync.ts"),
    path.join(webWorkingDirectory, "app", "api", "cron", "game-point-sync", "route.ts"),
    path.join(webWorkingDirectory, ".next", "BUILD_ID"),
    path.join(gameWorkingDirectory, "index.js"),
    path.join(gameWorkingDirectory, "security.js"),
    path.join(gameWorkingDirectory, "webPointCredit.js"),
    path.join(gameWorkingDirectory, "package-lock.json"),
  ];
  const hashes = {};
  for (const candidate of candidates) {
    if (!fs.existsSync(candidate) || !fs.statSync(candidate).isFile()) {
      throw new Error(`Production integrity file is missing: ${candidate}`);
    }
    hashes[candidate] = sha256File(candidate);
  }
  return hashes;
}

async function captureProductionBaseline() {
  const topupState = productionTopupState();
  const gameService = serviceSnapshot(GAME_SERVICE);
  const webService = serviceSnapshot(WEB_SERVICE);
  const activeWebRoot = assertProductionWebRoot(webService.WorkingDirectory);
  const statuses = await Promise.all([
    httpStatus(PRODUCTION_GAME_HEALTH_URL),
    httpStatus(PRODUCTION_WEB_HEALTH_URL),
    httpStatus(PRODUCTION_TOPUP_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: "{}",
    }),
  ]);
  if (statuses[0] !== 200 || statuses[1] !== 200 || statuses[2] !== topupState.callbackStatus) {
    throw new Error("Production health/top-up baseline is not safe for isolated E2E.");
  }
  return {
    topupState,
    activeWebRoot,
    gameService,
    webService,
    files: productionFileHashes(gameService.WorkingDirectory, activeWebRoot),
  };
}

async function verifyProductionBaseline(baseline) {
  const current = await captureProductionBaseline();
  if (current.activeWebRoot !== baseline.activeWebRoot
      || JSON.stringify(current.topupState) !== JSON.stringify(baseline.topupState)
      || JSON.stringify(current.gameService) !== JSON.stringify(baseline.gameService)
      || JSON.stringify(current.webService) !== JSON.stringify(baseline.webService)
      || JSON.stringify(current.files) !== JSON.stringify(baseline.files)) {
    throw new Error("Production service identity, environment, source or build changed during E2E.");
  }
}

function productionWebDatabasePath(webWorkingDirectory) {
  const script = `
    const path = require("path");
    process.loadEnvFile(".env");
    const raw = String(process.env.DATABASE_URL || "");
    if (!raw.startsWith("file:")) process.exit(65);
    let ref = raw.slice(5).split("?", 1)[0];
    if (!path.isAbsolute(ref)) ref = path.resolve(process.cwd(), "prisma", ref.replace(/^\\.\\//, ""));
    process.stdout.write(ref);
  `;
  const webEnv = { ...process.env };
  delete webEnv.DATABASE_URL;
  delete webEnv.POSTGRES_URL;
  const value = runChecked("/usr/local/bin/node", ["-e", script], {
    cwd: webWorkingDirectory,
    env: webEnv,
  });
  if (!value || !fs.existsSync(value)) throw new Error("Production web SQLite database was not found.");
  return fs.realpathSync(value);
}

function productionGameDatabaseUrl() {
  const script = `
    process.loadEnvFile(process.argv[1]);
    process.stdout.write(String(process.env.DATABASE_URL || process.env.POSTGRES_URL || ""));
  `;
  const env = { ...process.env };
  delete env.DATABASE_URL;
  delete env.POSTGRES_URL;
  const value = runChecked("/usr/local/bin/node", ["-e", script, GAME_ENV_FILE], { env });
  if (!value) throw new Error("Game PostgreSQL URL is not configured.");
  return value;
}

function copySqlite(source, target) {
  runChecked("python3", [
    "-c",
    "import sqlite3,sys; s=sqlite3.connect('file:'+sys.argv[1]+'?mode=ro',uri=True,timeout=10); t=sqlite3.connect(sys.argv[2]); s.backup(t,pages=256,sleep=0.01); t.close(); s.close()",
    source,
    target,
  ]);
}

function assertCopiedWebSchema(databasePath) {
  runChecked("python3", [
    "-c",
    "import sqlite3,sys; db=sqlite3.connect('file:'+sys.argv[1]+'?mode=ro',uri=True); cols={r[1] for r in db.execute('pragma table_info(\"GamePointSyncOutbox\")')}; db.close(); required={'id','sourceTransactionId','userId','pointAmount','status','attempts'}; assert required.issubset(cols), 'copied outbox schema missing'",
    databasePath,
  ]);
}

function grantWebRuntimeAccess(runtimeRoot, user) {
  const group = accountGroup(user);
  runChecked("chown", ["-R", `${user}:${group}`, runtimeRoot]);
  runChecked("chmod", ["0750", runtimeRoot]);
}

function seedCopiedWebDatabase(user, webRoot, databaseUrl, allowedPointAmount) {
  const script = `
    const crypto = require("crypto");
    const { PrismaClient } = require("@prisma/client");
    const db = new PrismaClient();
    const createSeed = async (label, pointAmount) => {
      const suffix = crypto.randomUUID().replace(/-/g, "");
      const email = "point-e2e-" + label + "-" + suffix + "@invalid.test";
      const user = await db.user.create({
        data: {
          email,
          fullName: "Point E2E " + label,
          refCode: "E2E" + suffix.slice(0, 12),
          status: "ACTIVE",
        },
      });
      const transactionId = "e2e-swap-" + label + "-" + suffix;
      const occurredAt = new Date();
      const outbox = await db.gamePointSyncOutbox.create({
        data: {
          sourceTransactionId: transactionId,
          userId: user.id,
          pointAmount,
          occurredAt,
          source: "ywonder-web-usdt-to-point",
        },
      });
      return {
        userId: user.id,
        email,
        fullName: user.fullName,
        outboxId: outbox.id,
        transactionId,
        pointAmount,
        occurredAt: occurredAt.toISOString(),
        source: outbox.source,
      };
    };
    (async () => {
      const allowed = await createSeed("allowed", process.env.E2E_POINT_AMOUNT);
      const timeoutSuffix = crypto.randomUUID().replace(/-/g, "");
      const timeoutOccurredAt = new Date();
      const timeoutOutbox = await db.gamePointSyncOutbox.create({
        data: {
          sourceTransactionId: "e2e-swap-timeout-" + timeoutSuffix,
          userId: allowed.userId,
          pointAmount: "2.000000",
          occurredAt: timeoutOccurredAt,
          source: "ywonder-web-usdt-to-point",
          nextAttemptAt: new Date("9999-12-31T23:59:59.000Z"),
        },
      });
      const timeout = {
        userId: allowed.userId,
        outboxId: timeoutOutbox.id,
        transactionId: timeoutOutbox.sourceTransactionId,
        pointAmount: timeoutOutbox.pointAmount,
        occurredAt: timeoutOccurredAt.toISOString(),
        source: timeoutOutbox.source,
      };
      const blocked = await createSeed("blocked", "1.000000");
      process.stdout.write(JSON.stringify({ allowed, timeout, blocked }));
    })().finally(() => db.$disconnect());
  `;
  const output = runAsUserChecked(user, "/usr/local/bin/node", ["-e", script], {
    cwd: webRoot,
    env: {
      ...process.env,
      DATABASE_URL: databaseUrl,
      E2E_POINT_AMOUNT: allowedPointAmount,
      USER: user,
      LOGNAME: user,
      HOME: accountHome(user),
    },
  });
  return JSON.parse(output);
}

function readOutbox(databasePath, outboxId) {
  const output = runChecked("python3", [
    "-c",
    "import json,sqlite3,sys; db=sqlite3.connect('file:'+sys.argv[1]+'?mode=ro',uri=True); row=db.execute('select status,attempts,lastError from GamePointSyncOutbox where id=?',(sys.argv[2],)).fetchone(); db.close(); print(json.dumps(row))",
    databasePath,
    outboxId,
  ]);
  const row = JSON.parse(output);
  if (!row) throw new Error("Copied web outbox row disappeared.");
  return { status: row[0], attempts: Number(row[1]), lastError: row[2] };
}

function queueOutboxRetry(databasePath, outboxId) {
  runChecked("python3", [
    "-c",
    "import sqlite3,sys,time; db=sqlite3.connect(sys.argv[1],timeout=10); db.execute(\"update GamePointSyncOutbox set status='RETRY', sentAt=null, nextAttemptAt=0, updatedAt=? where id=?\",(int(time.time()*1000),sys.argv[2])); db.commit(); db.close()",
    databasePath,
    outboxId,
  ]);
}

function makeOutboxDue(databasePath, outboxId) {
  runChecked("python3", [
    "-c",
    "import sqlite3,sys,time; db=sqlite3.connect(sys.argv[1],timeout=10); db.execute('update GamePointSyncOutbox set nextAttemptAt=0, updatedAt=? where id=?',(int(time.time()*1000),sys.argv[2])); db.commit(); db.close()",
    databasePath,
    outboxId,
  ]);
}

async function postCron(port, secret) {
  const response = await fetch(`http://127.0.0.1:${port}/api/cron/game-point-sync`, {
    method: "POST",
    headers: { Authorization: `Bearer ${secret}` },
  });
  const body = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(`Isolated outbox cron returned HTTP ${response.status}.`);
  return body;
}

async function postBlockedCanaryProbe(port, secret, blocked) {
  const timestamp = String(Math.floor(Date.now() / 1000));
  const input = {
    transactionId: blocked.transactionId,
    webUserId: blocked.userId,
    pointAmount: blocked.pointAmount,
    occurredAt: blocked.occurredAt,
    source: blocked.source,
    username: blocked.email,
    displayName: blocked.fullName || "",
  };
  const canonical = JSON.stringify([
    "ywonder-point-credit-v1",
    timestamp,
    input.transactionId,
    input.webUserId,
    input.pointAmount,
    input.occurredAt,
    input.source,
    input.username,
    input.displayName,
  ]);
  const signature = crypto.createHmac("sha256", secret).update(canonical, "utf8").digest("hex");
  const response = await fetch(`http://127.0.0.1:${port}/internal/web/point-credit`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-YWonder-Timestamp": timestamp,
      "X-YWonder-Signature": signature,
    },
    body: JSON.stringify({
      transaction_id: input.transactionId,
      web_user_id: input.webUserId,
      point_amount: input.pointAmount,
      occurred_at: input.occurredAt,
      source: input.source,
      username: input.username,
      display_name: input.displayName,
    }),
  });
  const body = await response.json().catch(() => ({}));
  if (response.status !== 425 || body.error !== "WEB_TOPUP_CANARY_USER_NOT_ALLOWED") {
    throw new Error("Isolated game accepted a web user outside the canary allowlist.");
  }
}

function verifyGameCredit(
  user,
  serverRoot,
  databaseUrl,
  webUserId,
  expectedPos,
  expectedRemainder,
  expectedTransactionIds
) {
  const script = `
    const { Pool } = require("pg");
    const pool = new Pool({ connectionString: process.env.DATABASE_URL, max: 1 });
    pool.query(
      "select e.pos, e.web_point_micros_remainder, " +
      "(select count(*)::integer from game_transactions t where t.player_id=p.id and t.type='web_topup_credit') as credit_count, " +
      "(select count(distinct t.ref)::integer from game_transactions t where t.player_id=p.id and t.type='web_topup_credit' and t.ref=any($2::text[])) as matching_ref_count " +
      "from game_players p join player_economy e on e.player_id=p.id where p.web_user_id=$1",
      [process.env.E2E_WEB_USER_ID, JSON.parse(process.env.E2E_TRANSACTION_IDS)]
    ).then((result) => process.stdout.write(JSON.stringify(result.rows))).finally(() => pool.end());
  `;
  const rows = JSON.parse(runGameDatabaseScript(user, serverRoot, databaseUrl, script, {
    E2E_WEB_USER_ID: webUserId,
    E2E_TRANSACTION_IDS: JSON.stringify(expectedTransactionIds),
  }));
  if (rows.length !== 1) throw new Error("Isolated game player was not created from web identity.");
  const row = rows[0];
  if (Number(row.pos) !== expectedPos
      || Number(row.web_point_micros_remainder) !== expectedRemainder
      || Number(row.credit_count) !== expectedTransactionIds.length
      || Number(row.matching_ref_count) !== expectedTransactionIds.length) {
    throw new Error("Isolated game Point balance or idempotency ledger is incorrect.");
  }
}

function verifyNoGamePlayer(user, serverRoot, databaseUrl, webUserId) {
  const script = `
    const { Pool } = require("pg");
    const pool = new Pool({ connectionString: process.env.DATABASE_URL, max: 1 });
    pool.query("select count(*)::integer as count from game_players where web_user_id=$1", [process.env.E2E_WEB_USER_ID])
      .then((result) => process.stdout.write(String(result.rows[0].count)))
      .finally(() => pool.end());
  `;
  const count = Number(runGameDatabaseScript(user, serverRoot, databaseUrl, script, {
    E2E_WEB_USER_ID: webUserId,
  }));
  if (count !== 0) throw new Error("Blocked canary user was created in the isolated game database.");
}

function verifyNoProductionGameLeak(user, serverRoot, productionDatabaseUrl, seeded) {
  const script = `
    const { Pool } = require("pg");
    const pool = new Pool({ connectionString: process.env.DATABASE_URL, max: 1 });
    pool.query(
      "select count(*)::integer as count from public.game_players where web_user_id = any($1::text[])",
      [JSON.parse(process.env.E2E_WEB_USER_IDS)]
    ).then((result) => process.stdout.write(String(result.rows[0].count))).finally(() => pool.end());
  `;
  const count = Number(runGameDatabaseScript(user, serverRoot, productionDatabaseUrl, script, {
    E2E_WEB_USER_IDS: JSON.stringify([seeded.allowed.userId, seeded.blocked.userId]),
  }));
  if (count !== 0) throw new Error("Synthetic E2E identity leaked into production game player data.");
}

function verifyNoProductionWebLeak(productionDatabasePath, seeded) {
  const output = runChecked("python3", [
    "-c",
    "import json,sqlite3,sys; db=sqlite3.connect('file:'+sys.argv[1]+'?mode=ro',uri=True); ids=json.loads(sys.argv[2]); emails=json.loads(sys.argv[3]); tx=json.loads(sys.argv[4]); users=db.execute('select count(*) from User where id in (?,?) or email in (?,?)',ids+emails).fetchone()[0]; outbox=db.execute('select count(*) from GamePointSyncOutbox where sourceTransactionId in (?,?,?)',tx).fetchone()[0]; db.close(); print(json.dumps({'users':users,'outbox':outbox}))",
    productionDatabasePath,
    JSON.stringify([seeded.allowed.userId, seeded.blocked.userId]),
    JSON.stringify([seeded.allowed.email, seeded.blocked.email]),
    JSON.stringify([seeded.allowed.transactionId, seeded.timeout.transactionId, seeded.blocked.transactionId]),
  ]);
  const counts = JSON.parse(output);
  if (Number(counts.users) !== 0 || Number(counts.outbox) !== 0) {
    throw new Error("Synthetic E2E identity or outbox row leaked into production web data.");
  }
}

function spawnIsolatedGame(user, serverRoot, databaseUrl, port, secret, allowedWebUserId) {
  return spawn("runuser", [
    "-u",
    user,
    "--preserve-environment",
    "--",
    "/usr/local/bin/node",
    "index.js",
  ], {
    cwd: serverRoot,
    detached: true,
    env: {
      ...process.env,
      NODE_ENV: "production",
      HOST: "127.0.0.1",
      PORT: String(port),
      STORE_MODE: "postgres",
      DATABASE_URL: databaseUrl,
      POSTGRES_URL: "",
      JWT_SECRET: crypto.randomBytes(48).toString("hex"),
      WEB_AUTH_MODE: "disabled",
      AUTH_TRANSITION_MODE: "local-primary",
      BROWSER_AUTH_ENABLED: "false",
      LOCAL_REGISTRATION_ENABLED: "false",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      RATE_LIMIT_ENABLED: "false",
      HTTP_ACCESS_LOG: "false",
      WEB_TOPUP_ENABLED: "true",
      WEB_TOPUP_ALLOW_REMOTE: "false",
      WEB_TOPUP_SECRET: secret,
      WEB_TOPUP_MODE: "canary",
      WEB_TOPUP_ALLOWED_WEB_USER_IDS: allowedWebUserId,
      CLIENT_ASSET_GRANTS_ENABLED: "false",
      PGUSER: user,
      USER: user,
      LOGNAME: user,
      HOME: accountHome(user),
    },
    stdio: ["ignore", "pipe", "pipe"],
  });
}

function spawnIsolatedWeb(user, webRoot, databaseUrl, port, gamePort, topupSecret, cronSecret, allowedWebUserId) {
  return spawn("runuser", [
    "-u",
    user,
    "--preserve-environment",
    "--",
    "/usr/local/bin/node",
    "node_modules/next/dist/bin/next",
    "start",
    "-p",
    String(port),
    "-H",
    "127.0.0.1",
  ], {
    cwd: webRoot,
    detached: true,
    env: {
      ...process.env,
      NODE_ENV: "production",
      PORT: String(port),
      DATABASE_URL: databaseUrl,
      GAME_POINT_SYNC_URL: `http://127.0.0.1:${gamePort}/internal/web/point-credit`,
      WEB_TOPUP_SECRET: topupSecret,
      WEB_TOPUP_MODE: "canary",
      WEB_TOPUP_ALLOWED_WEB_USER_IDS: allowedWebUserId,
      CRON_SECRET: cronSecret,
      USER: user,
      LOGNAME: user,
      HOME: accountHome(user),
    },
    stdio: ["ignore", "pipe", "pipe"],
  });
}

async function main() {
  enterStage("preflight-isolation-boundaries");
  if (typeof process.loadEnvFile !== "function") {
    throw new Error("Node.js process.loadEnvFile() is required.");
  }
  if (typeof process.getuid !== "function" || process.getuid() !== 0) {
    throw new Error("Run the isolated production-artifact E2E as root.");
  }

  const runId = assertSafeIdentifier(
    process.env.E2E_RUN_ID,
    /^[a-z0-9][a-z0-9_]{7,39}$/,
    "E2E_RUN_ID"
  );
  const serverRoot = assertSafeTemporaryRoot(process.env.GAME_SERVER_ROOT, "GAME_SERVER_ROOT");
  const isolatedWebRoot = assertSafeTemporaryRoot(process.env.E2E_WEB_ROOT, "E2E_WEB_ROOT");
  const runtimeRoot = assertSafeTemporaryRoot(process.env.E2E_RUNTIME_ROOT, "E2E_RUNTIME_ROOT");
  assertDisjointRoots({ serverRoot, isolatedWebRoot, runtimeRoot });
  if (isolatedWebRoot === fs.realpathSync(CANONICAL_WEB_ROOT)) {
    throw new Error("Isolated web root must not be the canonical production web root.");
  }
  requireStageFiles(serverRoot, "GAME_SERVER_ROOT", [
    "index.js",
    "package.json",
    path.join("node_modules", "pg", "package.json"),
  ]);
  requireStageFiles(isolatedWebRoot, "E2E_WEB_ROOT", [
    "package.json",
    path.join(".next", "BUILD_ID"),
    path.join("node_modules", "next", "package.json"),
    path.join("lib", "game-point-sync.ts"),
  ]);

  enterStage("capture-production-baseline");
  const productionBaseline = await captureProductionBaseline();
  if (isolatedWebRoot === productionBaseline.activeWebRoot) {
    throw new Error("Isolated web root must not be the active production web release.");
  }
  const productionDatabaseUrl = productionGameDatabaseUrl();
  const gameServiceUser = systemdServiceUser(GAME_SERVICE);
  const webServiceUser = systemdServiceUser(WEB_SERVICE);
  const database = parseProductionDatabase(productionDatabaseUrl, runId, gameServiceUser);
  const productionWebDb = productionWebDatabasePath(productionBaseline.activeWebRoot);
  const copiedWebDb = path.join(runtimeRoot, "web-copy.db");
  const copiedWebUrl = `file:${copiedWebDb}`;
  const gamePort = await reservePort();
  const webPort = await reservePort();
  const restartedWebPort = await reservePort();
  const faultProxyPort = await reservePort();
  const topupSecret = crypto.randomBytes(48).toString("hex");
  const cronSecret = crypto.randomBytes(48).toString("hex");
  const pointAmount = "12.345678";
  let game;
  let web;
  let faultProxy;
  let seeded;
  let databaseCreated = false;
  let testFailure = null;
  const cleanupFailures = [];

  try {
    enterStage("create-temporary-postgres-database");
    createTemporaryGameDatabase(database.owner, database.temporaryName);
    databaseCreated = true;
    writeDatabaseMarker(runtimeRoot, database.temporaryName);
    migrateTemporaryGameDatabase(gameServiceUser, serverRoot, database.isolatedUrl);

    enterStage("postgres-store-smoke");
    runPostgresStoreSmoke(gameServiceUser, serverRoot, database.isolatedUrl);

    enterStage("copy-production-web-sqlite");
    copySqlite(productionWebDb, copiedWebDb);
    assertCopiedWebSchema(copiedWebDb);
    grantWebRuntimeAccess(runtimeRoot, webServiceUser);

    enterStage("seed-copied-web-outbox");
    seeded = seedCopiedWebDatabase(webServiceUser, isolatedWebRoot, copiedWebUrl, pointAmount);

    enterStage("start-isolated-game-server");
    game = spawnIsolatedGame(
      gameServiceUser,
      serverRoot,
      database.isolatedUrl,
      gamePort,
      topupSecret,
      seeded.allowed.userId
    );
    writePidFile(runtimeRoot, "game", game);
    const gameOutput = capture(game);
    await waitForHttp(game, gameOutput, `http://127.0.0.1:${gamePort}/health`);

    enterStage("verify-canary-rejection");
    await postBlockedCanaryProbe(gamePort, topupSecret, seeded.blocked);
    verifyNoGamePlayer(gameServiceUser, serverRoot, database.isolatedUrl, seeded.blocked.userId);

    enterStage("start-isolated-web-server");
    web = spawnIsolatedWeb(
      webServiceUser,
      isolatedWebRoot,
      copiedWebUrl,
      webPort,
      gamePort,
      topupSecret,
      cronSecret,
      seeded.allowed.userId
    );
    writePidFile(runtimeRoot, "web", web);
    const webOutput = capture(web);
    await waitForHttp(web, webOutput, `http://127.0.0.1:${webPort}/api/health`);

    enterStage("dispatch-first-outbox-row");
    const first = await postCron(webPort, cronSecret);
    if (first.processed !== 1 || first.sent !== 1 || first.retry !== 0 || first.failed !== 0) {
      throw new Error("First isolated web outbox dispatch did not report one sent row.");
    }
    verifyGameCredit(
      gameServiceUser,
      serverRoot,
      database.isolatedUrl,
      seeded.allowed.userId,
      5012,
      345678,
      [seeded.allowed.transactionId]
    );
    const firstOutbox = readOutbox(copiedWebDb, seeded.allowed.outboxId);
    const timeoutOutbox = readOutbox(copiedWebDb, seeded.timeout.outboxId);
    const blockedOutbox = readOutbox(copiedWebDb, seeded.blocked.outboxId);
    if (firstOutbox.status !== "SENT" || firstOutbox.attempts !== 1 || firstOutbox.lastError) {
      throw new Error("First isolated web outbox state is incorrect.");
    }
    if (blockedOutbox.status !== "PENDING" || blockedOutbox.attempts !== 0 || blockedOutbox.lastError) {
      throw new Error("Canary filter touched a non-allowlisted outbox row.");
    }
    if (timeoutOutbox.status !== "PENDING" || timeoutOutbox.attempts !== 0 || timeoutOutbox.lastError) {
      throw new Error("Future post-commit timeout row was dispatched too early.");
    }

    enterStage("dispatch-idempotent-retry");
    queueOutboxRetry(copiedWebDb, seeded.allowed.outboxId);
    const second = await postCron(webPort, cronSecret);
    if (second.processed !== 1 || second.sent !== 1 || second.retry !== 0 || second.failed !== 0) {
      throw new Error("Duplicate isolated web outbox retry did not complete as SENT.");
    }
    verifyGameCredit(
      gameServiceUser,
      serverRoot,
      database.isolatedUrl,
      seeded.allowed.userId,
      5012,
      345678,
      [seeded.allowed.transactionId]
    );
    const secondOutbox = readOutbox(copiedWebDb, seeded.allowed.outboxId);
    if (secondOutbox.status !== "SENT" || secondOutbox.attempts !== 2 || secondOutbox.lastError) {
      throw new Error("Duplicate isolated web outbox state is incorrect.");
    }

    enterStage("verify-empty-allowlisted-outbox");
    const empty = await postCron(webPort, cronSecret);
    if (empty.processed !== 0) throw new Error("Sent or blocked outbox row was dispatched again.");

    enterStage("restart-isolated-web-with-durable-outbox");
    const firstWebPid = web.pid;
    await stopChild(web);
    web = null;
    const persistedFirstOutbox = readOutbox(copiedWebDb, seeded.allowed.outboxId);
    const persistedTimeoutOutbox = readOutbox(copiedWebDb, seeded.timeout.outboxId);
    if (persistedFirstOutbox.status !== "SENT" || persistedFirstOutbox.attempts !== 2
        || persistedTimeoutOutbox.status !== "PENDING" || persistedTimeoutOutbox.attempts !== 0) {
      throw new Error("Copied web outbox state did not survive isolated web shutdown.");
    }

    faultProxy = await startCommitThenTimeoutProxy(faultProxyPort, gamePort);
    web = spawnIsolatedWeb(
      webServiceUser,
      isolatedWebRoot,
      copiedWebUrl,
      restartedWebPort,
      faultProxyPort,
      topupSecret,
      cronSecret,
      seeded.allowed.userId
    );
    if (web.pid === firstWebPid) throw new Error("Isolated web restart reused the original process identity.");
    writePidFile(runtimeRoot, "web", web);
    const restartedWebOutput = capture(web);
    await waitForHttp(web, restartedWebOutput, `http://127.0.0.1:${restartedWebPort}/api/health`);

    enterStage("post-commit-response-loss");
    makeOutboxDue(copiedWebDb, seeded.timeout.outboxId);
    const lostResponse = await postCron(restartedWebPort, cronSecret);
    if (lostResponse.processed !== 1 || lostResponse.sent !== 0
        || lostResponse.retry !== 1 || lostResponse.failed !== 0) {
      throw new Error("Post-commit response loss did not leave exactly one retryable outbox row.");
    }
    if (faultProxy.state.intercepted !== 1 || faultProxy.state.committed !== 1
        || faultProxy.state.upstreamStatus < 200 || faultProxy.state.upstreamStatus >= 300) {
      throw new Error("Fault proxy did not prove the isolated game committed before dropping the response.");
    }
    const retryableTimeoutOutbox = readOutbox(copiedWebDb, seeded.timeout.outboxId);
    if (retryableTimeoutOutbox.status !== "RETRY" || retryableTimeoutOutbox.attempts !== 1
        || !retryableTimeoutOutbox.lastError) {
      throw new Error("Post-commit response loss produced an incorrect retry state.");
    }
    verifyGameCredit(
      gameServiceUser,
      serverRoot,
      database.isolatedUrl,
      seeded.allowed.userId,
      5014,
      345678,
      [seeded.allowed.transactionId, seeded.timeout.transactionId]
    );

    enterStage("retry-after-post-commit-response-loss");
    queueOutboxRetry(copiedWebDb, seeded.timeout.outboxId);
    const recovered = await postCron(restartedWebPort, cronSecret);
    if (recovered.processed !== 1 || recovered.sent !== 1
        || recovered.retry !== 0 || recovered.failed !== 0) {
      throw new Error("Retry after post-commit response loss did not complete as SENT.");
    }
    if (faultProxy.state.passedThrough !== 1) {
      throw new Error("Fault proxy did not pass the idempotent retry through exactly once.");
    }
    const recoveredTimeoutOutbox = readOutbox(copiedWebDb, seeded.timeout.outboxId);
    if (recoveredTimeoutOutbox.status !== "SENT" || recoveredTimeoutOutbox.attempts !== 2
        || recoveredTimeoutOutbox.lastError) {
      throw new Error("Recovered post-commit outbox state is incorrect.");
    }
    verifyGameCredit(
      gameServiceUser,
      serverRoot,
      database.isolatedUrl,
      seeded.allowed.userId,
      5014,
      345678,
      [seeded.allowed.transactionId, seeded.timeout.transactionId]
    );

    enterStage("verify-empty-after-fault-recovery");
    const recoveredEmpty = await postCron(restartedWebPort, cronSecret);
    if (recoveredEmpty.processed !== 0) {
      throw new Error("Recovered or blocked outbox row was dispatched again.");
    }
  } catch (error) {
    testFailure = error;
  }

  enterStage("cleanup-isolated-processes");
  try {
    await stopChild(web);
  } catch (error) {
    cleanupFailures.push(error);
  }
  try {
    await closeFaultProxy(faultProxy);
  } catch (error) {
    cleanupFailures.push(error);
  }
  try {
    await stopChild(game);
  } catch (error) {
    cleanupFailures.push(error);
  }
  if (databaseCreated) {
    try {
      enterStage("drop-temporary-postgres-database");
      dropTemporaryGameDatabase(database.temporaryName);
      removeDatabaseMarker(runtimeRoot);
    } catch (error) {
      cleanupFailures.push(error);
    }
  }

  if (seeded) {
    try {
      enterStage("verify-no-production-data-leak");
      verifyNoProductionGameLeak(gameServiceUser, serverRoot, productionDatabaseUrl, seeded);
      verifyNoProductionWebLeak(productionWebDb, seeded);
    } catch (error) {
      cleanupFailures.push(error);
    }
  }
  try {
    enterStage("verify-production-baseline-unchanged");
    await verifyProductionBaseline(productionBaseline);
  } catch (error) {
    cleanupFailures.push(error);
  }

  if (testFailure || cleanupFailures.length > 0) {
    const messages = [];
    if (testFailure) messages.push(safeErrorMessage(testFailure));
    messages.push(...cleanupFailures.map((error) => `cleanup/verification: ${safeErrorMessage(error)}`));
    throw new Error(messages.join(" | "));
  }

  console.log("WEB_POINT_SYNC_E2E_ISOLATED=success");
  console.log("TEMPORARY_POSTGRES_DATABASE=removed");
  console.log("CANARY_ALLOWLIST=pass");
  console.log("FIRST_DISPATCH=pass");
  console.log("DUPLICATE_RETRY=pass");
  console.log("ISOLATED_WEB_RESTART=pass");
  console.log("POST_COMMIT_RESPONSE_LOSS=pass");
  console.log("POST_COMMIT_IDEMPOTENT_RECOVERY=pass");
  console.log("POINT_DECIMAL_REMAINDER=pass");
  console.log(`PRODUCTION_TOPUP_BASELINE=${productionBaseline.topupState.name}`);
  console.log("PRODUCTION_SERVICES_RESTARTED=no");
  console.log("PRODUCTION_PLAYER_DATA_MUTATED=no");
}

if (require.main === module) {
  main().catch((error) => {
    console.log(`[web-point-e2e-isolated] FAIL stage=${currentStage}: ${safeErrorMessage(error)}`);
    process.exitCode = 1;
  });
}

module.exports = {
  assertDisjointRoots,
  classifyProductionTopupState,
  parseProductionDatabase,
  safeErrorMessage,
};
