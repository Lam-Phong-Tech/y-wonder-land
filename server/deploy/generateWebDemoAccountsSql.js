#!/usr/bin/env node
// Sinh câu SQL tạo 5 tài khoản trình diễn R1..R5 trong DB WEBSITE (SQLite/Prisma).
//
// Script này CHỈ IN RA SQL, không tự chạy và không tự kết nối DB nào — người có
// quyền ghi phải đọc lại rồi mới áp. Lý do: DB website là DB tài chính thật
// (ví USDT, hoa hồng, lệnh rút), không phải chỗ để script tự ý ghi vào.
//
//   node deploy/generateWebDemoAccountsSql.js > /tmp/demo-users.sql
//   # ĐỌC LẠI file, backup DB, rồi mới:
//   # sqlite3 /var/www/ywonder/prisma/prisma/prod.db < /tmp/demo-users.sql
//
// Vì sao phải có bước này: game-server đã tự nhận diện web account tên "R1".
// Xem issueWebPlayerSession() -> canonicalizeDemoAuthPayload() trong server/index.js:
// đăng nhập web bằng tên R1 sẽ được trỏ về player demo_r1 (đã có sẵn 109 loại
// vật phẩm + Point nhờ scripts/seedDemoAccounts.js), KHÔNG tạo player rỗng mới.
//
// Biến môi trường:
//   DEMO_STARTER_PASSWORD  mật khẩu chung (mặc định trùng với demoAccounts.js)
//   DEMO_WEB_EMAIL_DOMAIN  đuôi email giả (mặc định demo.ywonder.local)

const bcrypt = require("bcryptjs");
const { DEMO_STARTER_ACCOUNTS } = require("../demoAccounts");

const PASSWORD = process.env.DEMO_STARTER_PASSWORD || "demo123@";
const EMAIL_DOMAIN = process.env.DEMO_WEB_EMAIL_DOMAIN || "demo.ywonder.local";

function q(value) {
  return `'${String(value).replace(/'/g, "''")}'`;
}

function main() {
  const hash = bcrypt.hashSync(PASSWORD, 10);
  const lines = [];

  lines.push("-- Tài khoản trình diễn R1..R5 cho DB website. Chạy lại nhiều lần được.");
  lines.push("-- emailVerified đặt sẵn để BỎ QUA bước OTP mail thật.");
  lines.push("BEGIN TRANSACTION;");

  for (const name of DEMO_STARTER_ACCOUNTS) {
    const key = name.toLowerCase();
    const columns = [
      ["id", q(`demo_web_${key}`)],
      ["email", q(`${key}@${EMAIL_DOMAIN}`)],
      ["emailVerified", "CURRENT_TIMESTAMP"],
      ["passwordHash", q(hash)],
      ["fullName", q(name)],
      ["username", q(name)],
      ["refCode", q(`DEMO${name}`)],
      ["role", q("MEMBER")],
      ["status", q("ACTIVE")],
      ["kycStatus", q("NOT_SUBMITTED")],
      ["language", q("vi")],
      ["createdAt", "CURRENT_TIMESTAMP"],
      ["updatedAt", "CURRENT_TIMESTAMP"],
    ];

    lines.push(
      `INSERT INTO "User" (${columns.map(([c]) => `"${c}"`).join(", ")})\n` +
      `VALUES (${columns.map(([, v]) => v).join(", ")})\n` +
      `ON CONFLICT("username") DO UPDATE SET\n` +
      `  "passwordHash" = excluded."passwordHash",\n` +
      `  "emailVerified" = excluded."emailVerified",\n` +
      `  "status" = 'ACTIVE',\n` +
      `  "updatedAt" = CURRENT_TIMESTAMP;`
    );
  }

  lines.push("COMMIT;");
  lines.push("-- Kiểm tra (không in mật khẩu/hash):");
  lines.push(
    `SELECT username, status, (passwordHash IS NOT NULL) AS has_password FROM "User" ` +
    `WHERE username IN (${DEMO_STARTER_ACCOUNTS.map(q).join(", ")});`
  );

  console.log(lines.join("\n"));
}

main();
