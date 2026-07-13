import { auth } from "@/auth";
import { db } from "@/lib/db";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

const REQUEST_ID_RE = /^[A-Za-z0-9_-]{43}$/;
const GAME_APPROVAL_URL = "http://127.0.0.1:3000/auth/browser/approve";
const PUBLIC_WEB_ORIGIN = "https://ywonder.net";
const GAME_DEEP_LINK = "ywondergreenfarm://auth/complete";
const REGISTER_COMPLETED_PARAM = "registration_completed";
const CONFIRMED_PARAM = "account_confirmed";
const ACTION_PARAM = "account_action";
const SWITCH_ACTION = "switch";

type BrowserWebUser = {
  id: string;
  username?: string | null;
  email?: string | null;
  refCode?: string | null;
  fullName?: string | null;
  status: string;
};

function escapeHtml(value: unknown): string {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function noStoreHeaders(contentType = "text/html; charset=utf-8"): Record<string, string> {
  return {
    "Content-Type": contentType,
    "Cache-Control": "no-store, max-age=0",
    Pragma: "no-cache",
    Expires: "0",
    "Referrer-Policy": "no-referrer",
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
  };
}

function pageResponse(
  title: string,
  message: string,
  status: number,
  actionMarkup: string,
  allowDeepLinkScript = false,
): Response {
  const scriptPolicy = allowDeepLinkScript ? "script-src 'unsafe-inline'; " : "script-src 'none'; ";
  const html = `<!doctype html>
<html lang="vi">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>${escapeHtml(title)}</title>
  <style>
    :root { color-scheme: dark; font-family: Inter, ui-sans-serif, system-ui, sans-serif; }
    * { box-sizing: border-box; }
    body { margin: 0; min-height: 100vh; display: grid; place-items: center; padding: 24px; background: #15101f; color: #f7f3ff; }
    main { width: min(520px, 100%); border: 1px solid #4b3d60; border-radius: 8px; padding: 32px; background: #21172e; text-align: center; }
    h1 { margin: 0 0 12px; font-size: 28px; letter-spacing: 0; }
    p { margin: 0 0 24px; color: #cbbfda; line-height: 1.55; }
    .account { margin: -8px 0 24px; padding: 14px 16px; border: 1px solid #5f4d76; border-radius: 6px; background: #171120; color: #fff; font-weight: 700; overflow-wrap: anywhere; }
    .actions { display: grid; gap: 12px; }
    a { display: inline-flex; min-height: 48px; align-items: center; justify-content: center; padding: 0 24px; border-radius: 6px; font-weight: 700; text-decoration: none; }
    .primary { background: #8b3dea; color: #fff; }
    .secondary { border: 1px solid #8b7aa2; color: #f7f3ff; }
    .hint { margin: 18px 0 0; font-size: 13px; }
  </style>
</head>
<body>
  <main>
    <h1>${escapeHtml(title)}</h1>
    <p>${escapeHtml(message)}</p>
    ${actionMarkup}
  </main>
</body>
</html>`;

  return new Response(html, {
    status,
    headers: {
      ...noStoreHeaders(),
      "Content-Security-Policy": `default-src 'none'; style-src 'unsafe-inline'; ${scriptPolicy}base-uri 'none'; form-action 'none'; frame-ancestors 'none'`,
    },
  });
}

function htmlResponse(title: string, message: string, status: number, returnToGame = false): Response {
  if (!returnToGame) {
    return pageResponse(title, message, status, '<a class="secondary" href="/vi/login">Quay lại đăng nhập</a>');
  }

  const deepLinkJson = JSON.stringify(GAME_DEEP_LINK).replace(/</g, "\\u003c");
  const returnMarkup = `<a class="primary" href="${GAME_DEEP_LINK}">Mở lại game</a>
    <p class="hint">Nếu dùng bản EXE, chỉ cần quay lại cửa sổ game. Game vẫn tự nhận kết quả xác thực.</p>
    <script>setTimeout(function () { window.location.href = ${deepLinkJson}; }, 350);</script>`;
  return pageResponse(title, message, status, returnMarkup, true);
}

function redirectResponse(location: URL, expiredCookies: string[] = []): Response {
  const headers = new Headers(noStoreHeaders("text/plain; charset=utf-8"));
  headers.set("Location", location.toString());
  for (const cookie of expiredCookies) headers.append("Set-Cookie", cookie);
  return new Response(null, { status: 302, headers });
}

function publicCallbackUrl(requestUrl: URL): URL {
  return new URL(`${requestUrl.pathname}${requestUrl.search}`, PUBLIC_WEB_ORIGIN);
}

function loginRedirect(requestUrl: URL): Response {
  const publicCallback = publicCallbackUrl(requestUrl);
  publicCallback.searchParams.delete(ACTION_PARAM);
  publicCallback.searchParams.set(CONFIRMED_PARAM, "1");

  const loginUrl = new URL("/vi/login", PUBLIC_WEB_ORIGIN);
  loginUrl.searchParams.set("callbackUrl", publicCallback.toString());
  return redirectResponse(loginUrl);
}

function registrationRedirect(requestUrl: URL): Response {
  const publicCallback = publicCallbackUrl(requestUrl);
  publicCallback.searchParams.delete(ACTION_PARAM);
  publicCallback.searchParams.set(REGISTER_COMPLETED_PARAM, "1");
  publicCallback.searchParams.set(CONFIRMED_PARAM, "1");

  const registrationUrl = new URL("/vi/register", PUBLIC_WEB_ORIGIN);
  registrationUrl.searchParams.set("callbackUrl", publicCallback.toString());
  return redirectResponse(registrationUrl);
}

function sessionCookieNames(request: Request): string[] {
  const names = new Set<string>([
    "authjs.session-token",
    "__Secure-authjs.session-token",
    "next-auth.session-token",
    "__Secure-next-auth.session-token",
  ]);
  const cookieHeader = request.headers.get("cookie") || "";
  for (const part of cookieHeader.split(";")) {
    const separator = part.indexOf("=");
    const name = (separator >= 0 ? part.slice(0, separator) : part).trim();
    if (/(?:^|[-_.])(?:authjs|next-auth)\.session-token(?:\.\d+)?$/i.test(name)) names.add(name);
  }
  return Array.from(names);
}

function expiredSessionCookies(request: Request): string[] {
  const expires = "Path=/; Max-Age=0; Expires=Thu, 01 Jan 1970 00:00:00 GMT; HttpOnly; Secure; SameSite=Lax";
  const cookies: string[] = [];
  for (const name of sessionCookieNames(request)) {
    cookies.push(`${name}=; ${expires}`);
    cookies.push(`${name}=; Domain=.ywonder.net; ${expires}`);
  }
  return cookies;
}

function switchAccountRedirect(request: Request, requestUrl: URL): Response {
  const publicCallback = publicCallbackUrl(requestUrl);
  publicCallback.searchParams.delete(ACTION_PARAM);
  publicCallback.searchParams.set(CONFIRMED_PARAM, "1");

  const loginUrl = new URL("/vi/login", PUBLIC_WEB_ORIGIN);
  loginUrl.searchParams.set("callbackUrl", publicCallback.toString());
  return redirectResponse(loginUrl, expiredSessionCookies(request));
}

function maskEmail(email: unknown): string {
  const normalized = String(email || "").trim();
  const at = normalized.indexOf("@");
  if (at <= 0) return normalized;
  const local = normalized.slice(0, at);
  const visible = local.slice(0, Math.min(2, local.length));
  return `${visible}${"*".repeat(Math.max(3, local.length - visible.length))}${normalized.slice(at)}`;
}

function accountConfirmationResponse(requestUrl: URL, webUser: BrowserWebUser): Response {
  const continueUrl = publicCallbackUrl(requestUrl);
  continueUrl.searchParams.delete(ACTION_PARAM);
  continueUrl.searchParams.set(CONFIRMED_PARAM, "1");

  const switchUrl = publicCallbackUrl(requestUrl);
  switchUrl.searchParams.delete(CONFIRMED_PARAM);
  switchUrl.searchParams.set(ACTION_PARAM, SWITCH_ACTION);

  const displayName = String(webUser.fullName || webUser.username || "Tài khoản website").trim();
  const accountId = maskEmail(webUser.email) || String(webUser.username || webUser.refCode || "").trim();
  const accountText = accountId && accountId !== displayName ? `${displayName} (${accountId})` : displayName;
  const actionMarkup = `<div class="account">${escapeHtml(accountText)}</div>
    <div class="actions">
      <a class="primary" href="${escapeHtml(continueUrl.toString())}">Tiếp tục với tài khoản này</a>
      <a class="secondary" href="${escapeHtml(switchUrl.toString())}">Đăng nhập tài khoản khác</a>
    </div>
    <p class="hint">Game chưa nhận quyền đăng nhập cho đến khi bạn chọn một tài khoản.</p>`;
  return pageResponse("Chọn tài khoản vào game", "Trình duyệt đang ghi nhớ tài khoản website dưới đây.", 200, actionMarkup);
}

export async function GET(request: Request): Promise<Response> {
  const requestUrl = new URL(request.url);
  const requestId = String(requestUrl.searchParams.get("request") || "").trim();
  if (!REQUEST_ID_RE.test(requestId)) {
    return htmlResponse("Liên kết không hợp lệ", "Phiên xác thực game không đúng định dạng. Hãy bắt đầu lại từ trong game.", 400);
  }

  const intent = requestUrl.searchParams.get("intent") === "register" ? "register" : "login";
  const registrationCompleted = requestUrl.searchParams.get(REGISTER_COMPLETED_PARAM) === "1";
  if (intent === "register" && !registrationCompleted) {
    return registrationRedirect(requestUrl);
  }

  if (requestUrl.searchParams.get(ACTION_PARAM) === SWITCH_ACTION) {
    return switchAccountRedirect(request, requestUrl);
  }

  const session = await auth();
  const webUserId = String((session?.user as { id?: string } | undefined)?.id || "").trim();
  if (!webUserId) return loginRedirect(requestUrl);

  const webUser = await db.user.findUnique({
    where: { id: webUserId },
    select: {
      id: true,
      username: true,
      email: true,
      refCode: true,
      fullName: true,
      status: true,
    },
  });
  if (!webUser || webUser.status !== "ACTIVE") {
    return htmlResponse("Tài khoản không khả dụng", "Tài khoản chưa kích hoạt hoặc đã bị khóa. Vui lòng liên hệ hỗ trợ.", 403);
  }

  const accountConfirmed = requestUrl.searchParams.get(CONFIRMED_PARAM) === "1";
  if (!accountConfirmed && !registrationCompleted) {
    return accountConfirmationResponse(requestUrl, webUser);
  }

  const approvalSecret = process.env.GAME_API_SECRET;
  if (!approvalSecret || approvalSecret.length < 16) {
    return htmlResponse("Dịch vụ chưa sẵn sàng", "Cầu nối game đang tạm bảo trì. Vui lòng thử lại sau.", 503);
  }

  let approval: Response;
  try {
    approval = await fetch(GAME_APPROVAL_URL, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${approvalSecret}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        requestId,
        webUser: {
          id: webUser.id,
          username: webUser.username || webUser.refCode || webUser.email || webUser.id,
          email: webUser.email || "",
          refCode: webUser.refCode || "",
          fullName: webUser.fullName || "",
          displayName: webUser.fullName || webUser.username || "Player",
          status: webUser.status,
          active: true,
        },
      }),
      cache: "no-store",
      signal: AbortSignal.timeout(5000),
    });
  } catch {
    return htmlResponse("Không kết nối được game", "Game-server chưa phản hồi. Hãy quay lại game và thử lại.", 502);
  }

  if (!approval.ok) {
    if (approval.status === 404 || approval.status === 410) {
      return htmlResponse("Phiên đã hết hạn", "Hãy quay lại game và bắt đầu đăng nhập website một lần nữa.", 410);
    }
    if (approval.status === 409) {
      return htmlResponse("Phiên đã được sử dụng", "Liên kết này không thể dùng lại. Hãy quay lại game để kiểm tra kết quả.", 409, true);
    }
    return htmlResponse("Chưa thể xác thực", "Cầu nối game từ chối yêu cầu. Hãy quay lại game và thử lại.", 502);
  }

  return htmlResponse("Xác thực thành công", "Tài khoản website đã được kết nối. Đang quay lại Y WONDER GREEN FARM...", 200, true);
}
