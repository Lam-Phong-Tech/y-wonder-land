const assert = require("node:assert/strict");
const fs = require("node:fs");
const { stripTypeScriptTypes } = require("node:module");
const path = require("node:path");
const vm = require("node:vm");

const routePath = path.join(__dirname, "deploy", "web-browser-sso", "browser-callback-route.ts");
const requestId = "A".repeat(43);
const callbackBase = `https://ywonder.net/api/game/browser/callback?request=${requestId}`;

function loadRoute(deps) {
  let source = fs.readFileSync(routePath, "utf8");
  source = source
    .replace('import { auth } from "@/auth";', "const auth = __deps.auth;")
    .replace('import { db } from "@/lib/db";', "const db = __deps.db;")
    .replace(/export const /g, "const ")
    .replace("export async function GET", "async function GET");
  source = stripTypeScriptTypes(source, { mode: "strip" });
  source += "\nmodule.exports = { GET };\n";

  const context = {
    __deps: deps,
    module: { exports: {} },
    exports: {},
    URL,
    Request,
    Response,
    Headers,
    AbortSignal,
    JSON,
    Set,
    String,
    process: { env: { GAME_API_SECRET: "test-secret-with-at-least-16-characters" } },
    fetch: deps.fetch,
  };
  vm.runInNewContext(source, context, { filename: routePath });
  return context.module.exports;
}

function callbackFromRedirect(response) {
  const location = new URL(response.headers.get("location"));
  return {
    location,
    callback: new URL(location.searchParams.get("callbackUrl")),
  };
}

async function run() {
  const state = {
    session: null,
    webUser: {
      id: "web-user-1",
      username: "lam001",
      email: "trantunglam2025@gmail.com",
      refCode: "LAM001",
      fullName: "TRAN TUNG LAM",
      status: "ACTIVE",
    },
    approvalStatus: 200,
    approvalCalls: [],
  };

  const { GET } = loadRoute({
    auth: async () => state.session,
    db: {
      user: {
        findUnique: async () => state.webUser,
      },
    },
    fetch: async (url, options) => {
      state.approvalCalls.push({ url, options });
      return new Response("{}", { status: state.approvalStatus });
    },
  });

  let response = await GET(new Request("https://ywonder.net/api/game/browser/callback?request=bad"));
  assert.equal(response.status, 400, "Invalid request IDs must be rejected.");

  response = await GET(new Request(`${callbackBase}&intent=register`));
  assert.equal(response.status, 302, "A fresh registration intent must open the registration page.");
  let redirect = callbackFromRedirect(response);
  assert.equal(redirect.location.pathname, "/vi/register");
  assert.equal(redirect.callback.searchParams.get("registration_completed"), "1");
  assert.equal(redirect.callback.searchParams.get("account_confirmed"), "1");
  assert.match(response.headers.get("cache-control") || "", /no-store/);

  state.session = null;
  response = await GET(new Request(`${callbackBase}&intent=login`));
  assert.equal(response.status, 302, "A browser without a web session must open login.");
  redirect = callbackFromRedirect(response);
  assert.equal(redirect.location.pathname, "/vi/login");
  assert.equal(redirect.callback.searchParams.get("account_confirmed"), "1");
  assert.equal(state.approvalCalls.length, 0, "A missing web session must never approve the game request.");

  state.session = { user: { id: state.webUser.id } };
  state.webUser = { ...state.webUser, fullName: '<script>alert("x")</script>' };
  response = await GET(new Request(`${callbackBase}&intent=login`));
  assert.equal(response.status, 200);
  let body = await response.text();
  assert.match(body, /Tiếp tục với tài khoản này/);
  assert.match(body, /Đăng nhập tài khoản khác/);
  assert.doesNotMatch(body, /<script>alert\("x"\)<\/script>/, "Account data must be HTML escaped.");
  assert.match(response.headers.get("content-security-policy") || "", /script-src 'none'/);
  assert.equal(state.approvalCalls.length, 0, "A remembered web session must wait for explicit confirmation.");

  state.webUser = { ...state.webUser, fullName: "TRAN TUNG LAM" };
  response = await GET(new Request(`${callbackBase}&intent=login&account_confirmed=1`));
  assert.equal(response.status, 200);
  body = await response.text();
  assert.match(body, /Xác thực thành công/);
  assert.equal(state.approvalCalls.length, 1, "Explicit confirmation must approve exactly once.");

  state.approvalCalls.length = 0;
  response = await GET(new Request(`${callbackBase}&intent=login&account_action=switch`, {
    headers: {
      Cookie: "__Secure-authjs.session-token.0=abc; next-auth.session-token=def; unrelated=value",
    },
  }));
  assert.equal(response.status, 302, "Switch account must return to the login page.");
  redirect = callbackFromRedirect(response);
  assert.equal(redirect.location.pathname, "/vi/login");
  assert.equal(redirect.location.searchParams.has("locked"), false, "Switch account must not show a false locked-account warning.");
  assert.equal(redirect.callback.searchParams.get("account_confirmed"), "1");
  assert.equal(redirect.callback.searchParams.has("account_action"), false);
  const setCookie = response.headers.get("set-cookie") || "";
  assert.match(setCookie, /__Secure-authjs\.session-token\.0=/);
  assert.match(setCookie, /next-auth\.session-token=/);
  assert.doesNotMatch(setCookie, /unrelated=/, "Switch account must not clear unrelated website cookies.");
  assert.equal(state.approvalCalls.length, 0, "Switch account must not approve the remembered session.");

  state.webUser = { ...state.webUser, status: "LOCKED" };
  response = await GET(new Request(`${callbackBase}&intent=login&account_confirmed=1`));
  assert.equal(response.status, 403, "Locked web accounts must remain blocked.");

  state.webUser = { ...state.webUser, status: "ACTIVE" };
  state.approvalCalls.length = 0;
  response = await GET(new Request(`${callbackBase}&intent=register&registration_completed=1&account_confirmed=1`));
  assert.equal(response.status, 200, "A completed registration must continue to the game.");
  assert.equal(state.approvalCalls.length, 1);

  console.log("Web browser callback route tests passed.");
}

run().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
