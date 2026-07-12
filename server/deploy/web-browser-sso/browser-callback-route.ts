import { auth } from "@/auth";
import { db } from "@/lib/db";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

const REQUEST_ID_RE = /^[A-Za-z0-9_-]{43}$/;
const GAME_APPROVAL_URL = "http://127.0.0.1:3000/auth/browser/approve";
const PUBLIC_WEB_ORIGIN = "https://ywonder.net";
const GAME_DEEP_LINK = "ywondergreenfarm://auth/complete";

function htmlResponse(title: string, message: string, status: number, returnToGame = false): Response {
  const deepLinkJson = JSON.stringify(GAME_DEEP_LINK).replace(/</g, "\\u003c");
  const returnMarkup = returnToGame
    ? `<a class="primary" href="${GAME_DEEP_LINK}">Mở lại game</a>
       <p class="hint">Nếu dùng bản EXE, chỉ cần quay lại cửa sổ game. Game vẫn tự nhận kết quả xác thực.</p>
       <script>setTimeout(function () { window.location.href = ${deepLinkJson}; }, 350);</script>`
    : `<a class="secondary" href="/vi/login">Quay lại đăng nhập</a>`;

  const html = `<!doctype html>
<html lang="vi">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>${title}</title>
  <style>
    :root { color-scheme: dark; font-family: Inter, ui-sans-serif, system-ui, sans-serif; }
    * { box-sizing: border-box; }
    body { margin: 0; min-height: 100vh; display: grid; place-items: center; padding: 24px; background: #15101f; color: #f7f3ff; }
    main { width: min(520px, 100%); border: 1px solid #4b3d60; border-radius: 8px; padding: 32px; background: #21172e; text-align: center; }
    h1 { margin: 0 0 12px; font-size: 28px; letter-spacing: 0; }
    p { margin: 0 0 24px; color: #cbbfda; line-height: 1.55; }
    a { display: inline-flex; min-height: 48px; align-items: center; justify-content: center; padding: 0 24px; border-radius: 6px; font-weight: 700; text-decoration: none; }
    .primary { background: #8b3dea; color: #fff; }
    .secondary { border: 1px solid #8b7aa2; color: #f7f3ff; }
    .hint { margin: 18px 0 0; font-size: 13px; }
  </style>
</head>
<body>
  <main>
    <h1>${title}</h1>
    <p>${message}</p>
    ${returnMarkup}
  </main>
</body>
</html>`;

  return new Response(html, {
    status,
    headers: {
      "Content-Type": "text/html; charset=utf-8",
      "Cache-Control": "no-store, max-age=0",
      "Content-Security-Policy": "default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'",
      "Referrer-Policy": "no-referrer",
      "X-Content-Type-Options": "nosniff",
      "X-Frame-Options": "DENY",
    },
  });
}

function loginRedirect(requestUrl: URL): Response {
  const publicCallback = new URL(`${requestUrl.pathname}${requestUrl.search}`, PUBLIC_WEB_ORIGIN);
  const loginUrl = new URL("/vi/login", PUBLIC_WEB_ORIGIN);
  loginUrl.searchParams.set("callbackUrl", publicCallback.toString());
  return Response.redirect(loginUrl, 302);
}

export async function GET(request: Request): Promise<Response> {
  const requestUrl = new URL(request.url);
  const requestId = String(requestUrl.searchParams.get("request") || "").trim();
  if (!REQUEST_ID_RE.test(requestId)) {
    return htmlResponse("Liên kết không hợp lệ", "Phiên xác thực game không đúng định dạng. Hãy bắt đầu lại từ trong game.", 400);
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
