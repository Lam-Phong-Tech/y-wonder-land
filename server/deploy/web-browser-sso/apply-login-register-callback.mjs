import fs from "node:fs";
import path from "node:path";

const webRoot = process.argv[2];
if (!webRoot) {
  throw new Error("Usage: node apply-login-register-callback.mjs <web-root>");
}

const loginPath = path.join(webRoot, "app", "[locale]", "login", "page.tsx");
const registerPath = path.join(webRoot, "app", "[locale]", "register", "page.tsx");

function block(lines) {
  return lines.join("\n");
}

function replaceOnce(source, before, after, label) {
  const occurrences = source.split(before).length - 1;
  if (occurrences !== 1) {
    throw new Error(`${label}: expected exactly one source match, found ${occurrences}`);
  }
  return source.replace(before, after);
}

function transformFile(filePath, transformations) {
  let source = fs.readFileSync(filePath, "utf8");
  const eol = source.includes("\r\n") ? "\r\n" : "\n";
  source = source.replace(/\r\n/g, "\n");
  for (const transformation of transformations) {
    source = replaceOnce(
      source,
      transformation.before,
      transformation.after,
      transformation.label
    );
  }
  fs.writeFileSync(filePath, source.replace(/\n/g, eol), "utf8");
}

const callbackGuard = block([
  "const TRUSTED_CALLBACK_HOSTS = new Set([\"ywonder.net\", \"www.ywonder.net\", \"agent.ywonder.net\", \"admin.ywonder.net\"]);",
  "",
  "function normalizeCallbackUrl(raw: string | null, locale: string): string {",
  "  const fallback = `/${locale}/dashboard`;",
  "  if (!raw) return fallback;",
  "  if (raw.startsWith(\"/\") && !raw.startsWith(\"//\")) return raw;",
  "  try {",
  "    const url = new URL(raw);",
  "    if (url.protocol === \"https:\" &&",
  "        (!url.port || url.port === \"443\") &&",
  "        TRUSTED_CALLBACK_HOSTS.has(url.hostname)) {",
  "      return url.toString();",
  "    }",
  "  } catch {}",
  "  return fallback;",
  "}",
]);

transformFile(loginPath, [
  {
    label: "login callback guard",
    before: block([
      "import { isAwaitingApprovalAction } from \"./actions\";",
      "",
      "export default function LoginPage() {",
    ]),
    after: block([
      "import { isAwaitingApprovalAction } from \"./actions\";",
      "",
      callbackGuard,
      "",
      "export default function LoginPage() {",
    ]),
  },
  {
    label: "login callback URL",
    before: "  const callbackUrl = sp.get(\"callbackUrl\") || `/${locale}/dashboard`;",
    after: block([
      "  const callbackUrl = normalizeCallbackUrl(sp.get(\"callbackUrl\"), locale);",
      "  const registrationUrl = `/${locale}/register?callbackUrl=${encodeURIComponent(callbackUrl)}`;",
    ]),
  },
  {
    label: "login registration link",
    before: block([
      "                <Link href=\"/register\" className=\"text-grass-d font-semibold hover:underline\">",
      "                  {t(\"createFarm\")}",
      "                </Link>",
    ]),
    after: block([
      "                <a href={registrationUrl} className=\"text-grass-d font-semibold hover:underline\">",
      "                  {t(\"createFarm\")}",
      "                </a>",
    ]),
  },
]);

transformFile(registerPath, [
  {
    label: "register obsolete routing import",
    before: "import { Link, useRouter } from \"@/i18n/routing\";\n",
    after: "",
  },
  {
    label: "register callback guard",
    before: block([
      "import { validateEmail } from \"@/lib/email\";",
      "",
      "export default function RegisterPage() {",
    ]),
    after: block([
      "import { validateEmail } from \"@/lib/email\";",
      "",
      callbackGuard,
      "",
      "export default function RegisterPage() {",
    ]),
  },
  {
    label: "register obsolete router",
    before: "  const router = useRouter();\n",
    after: "",
  },
  {
    label: "register callback URL",
    before: "  const tBrand = useTranslations(\"brand\");",
    after: block([
      "  const tBrand = useTranslations(\"brand\");",
      "  const callbackUrl = normalizeCallbackUrl(sp.get(\"callbackUrl\"), locale);",
      "  const loginUrl = `/${locale}/login?callbackUrl=${encodeURIComponent(callbackUrl)}`;",
    ]),
  },
  {
    label: "register OTP redirect",
    before: "    window.location.href = loginRes?.error ? `/${locale}/login` : `/${locale}/dashboard`;",
    after: "    window.location.href = loginRes?.error ? loginUrl : callbackUrl;",
  },
  {
    label: "register fallback login redirect",
    before: "        router.push(\"/login\");",
    after: "        window.location.href = loginUrl;",
  },
  {
    label: "register success redirect",
    before: "      window.location.href = `/${locale}/dashboard`;",
    after: "      window.location.href = callbackUrl;",
  },
  {
    label: "register login link",
    before: "            <Link href=\"/login\" className=\"text-grass-d font-semibold hover:underline\">{t(\"loginLink\")}</Link>",
    after: "            <a href={loginUrl} className=\"text-grass-d font-semibold hover:underline\">{t(\"loginLink\")}</a>",
  },
]);

console.log("WEB_LOGIN_REGISTER_TRANSFORM=success");
