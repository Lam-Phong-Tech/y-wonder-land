#!/usr/bin/env node
// Nạp/ghi đè các tài khoản trình diễn (DemoRich*, DemoRealtime*, R1..R5) vào store
// đang cấu hình. Chạy ĐỘC LẬP, không cần restart backend.
//
//   node scripts/seedDemoAccounts.js            # store theo STORE_MODE hiện tại
//   DEMO_ACCOUNTS_ENABLED=true node scripts/seedDemoAccounts.js   # bắt buộc khi STORE_MODE=postgres
//
// Biến môi trường:
//   DEMO_STARTER_PASSWORD  mật khẩu cho R1..R5 (mặc định trong demoAccounts.js)
//   DEMO_STARTER_STACK     số lượng mỗi loại tài nguyên (mặc định 1000)
//
// Script KHÔNG in mật khẩu ra màn hình/log.

const store = require("../store");
const bcrypt = require("bcryptjs");
const { ensureDemoAccounts, DEMO_ACCOUNTS, DEMO_STARTER_ACCOUNTS } = require("../demoAccounts");

async function main() {
  if (store.mode === "postgres" && process.env.DEMO_ACCOUNTS_ENABLED !== "true") {
    console.error(
      "[seed] STORE_MODE=postgres cần DEMO_ACCOUNTS_ENABLED=true để tránh nạp nhầm vào DB thật."
    );
    process.exitCode = 1;
    return;
  }

  console.log(`[seed] store mode = ${store.mode}`);
  await ensureDemoAccounts(store, bcrypt);

  for (const name of DEMO_STARTER_ACCOUNTS) {
    const user = await store.findUserByName(name);
    if (!user) {
      console.error(`[seed] THIẾU tài khoản ${name} sau khi seed.`);
      process.exitCode = 1;
      continue;
    }
    const playerId = user.player_id || user.id;
    const inventory = await store.getInventory(playerId);
    const economy = await store.getEconomy(playerId);
    const slotCount = inventory && Array.isArray(inventory.slots) ? inventory.slots.length : 0;
    console.log(`[seed] ${name}: ${slotCount} loại vật phẩm, ${economy ? economy.pos : "?"} Point`);
  }

  console.log(`[seed] xong ${DEMO_ACCOUNTS.length} tài khoản trình diễn.`);
}

main().catch((error) => {
  console.error("[seed] lỗi:", error.message);
  process.exitCode = 1;
});
