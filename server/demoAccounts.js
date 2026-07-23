const { catalog } = require("./shopCatalog");

const DEMO_RICH_ACCOUNTS = ["DemoRich01", "DemoRich02", "DemoRich03", "DemoRich04", "DemoRich05"];
const DEMO_REALTIME_ACCOUNTS = ["DemoRealtime01", "DemoRealtime02", "DemoRealtime03", "DemoRealtime04", "DemoRealtime05"];

// Tài khoản trình diễn cho khách: tên ngắn, mật khẩu chung, kho đầy sẵn mọi thứ,
// KHÔNG cần email thật và KHÔNG phải tạo nhân vật (characterCreated = true).
const DEMO_STARTER_ACCOUNTS = ["R1", "R2", "R3", "R4", "R5"];

// Mật khẩu demo dùng chung — CỐ Ý công khai, chỉ dành cho 5 tài khoản trình diễn ở trên.
// Đổi bằng biến môi trường DEMO_STARTER_PASSWORD, không sửa file.
const STARTER_PASSWORD = process.env.DEMO_STARTER_PASSWORD || "demo123@";
const STARTER_STACK = Math.max(1, Number(process.env.DEMO_STARTER_STACK) || 1000);

const DEMO_ACCOUNTS = [...DEMO_RICH_ACCOUNTS, ...DEMO_REALTIME_ACCOUNTS, ...DEMO_STARTER_ACCOUNTS];
const DEMO_ACCOUNT_SET = new Set(DEMO_ACCOUNTS.map((name) => name.toLowerCase()));

function nowISO() {
  return new Date().toISOString();
}

function normalizeIdentity(value) {
  return String(value || "").trim().toLowerCase();
}

function isDemoAccount(username) {
  return DEMO_ACCOUNT_SET.has(normalizeIdentity(username));
}

function isRichDemoAccount(username) {
  const key = normalizeIdentity(username);
  return DEMO_RICH_ACCOUNTS.some((name) => normalizeIdentity(name) === key);
}

function canonicalDemoName(username) {
  const key = normalizeIdentity(username);
  return DEMO_ACCOUNTS.find((name) => normalizeIdentity(name) === key) || "";
}

function isStarterDemoAccount(username) {
  const key = normalizeIdentity(username);
  return DEMO_STARTER_ACCOUNTS.some((name) => normalizeIdentity(name) === key);
}

/// Tài khoản được nạp sẵn đồ (giàu hoặc trình diễn) -> lên cấp sẵn, bỏ qua tạo nhân vật.
function isLoadedDemoAccount(username) {
  return isRichDemoAccount(username) || isStarterDemoAccount(username);
}

/// Mật khẩu THẬT sẽ được bcrypt và ghi vào DB. Bản production không có đường tắt nào
/// khác, nên đây là thứ duy nhất quyết định đăng nhập được hay không.
function demoPasswordFor(username) {
  return isStarterDemoAccount(username) ? STARTER_PASSWORD : canonicalDemoName(username) || String(username || "");
}

function isAllowedDemoPassword(username, password) {
  if (!isDemoAccount(username)) return false;
  const canonicalName = canonicalDemoName(username);
  const pass = String(password || "");
  if (isStarterDemoAccount(username) && pass === STARTER_PASSWORD) return true;
  return pass === "demo" || pass.toLowerCase() === canonicalName.toLowerCase();
}

function makeProfile(name) {
  return {
    version: 1,
    name,
    gender: "male",
    avatarId: "",
    level: isLoadedDemoAccount(name) ? 12 : 1,
    exp: isLoadedDemoAccount(name) ? 45 : 0,
    characterCreated: true,
    tutorialCompleted: true,
    createdAt: nowISO(),
    updatedAt: nowISO(),
  };
}

function makeEconomy(name) {
  if (isStarterDemoAccount(name)) {
    return {
      version: 1,
      pos: 1000000,
      updatedAt: nowISO(),
    };
  }

  if (isRichDemoAccount(name)) {
    return {
      version: 1,
      pos: 500000,
      updatedAt: nowISO(),
    };
  }

  return {
    version: 1,
    pos: 5000,
    updatedAt: nowISO(),
  };
}

/// Kho "đủ mọi thứ" cho tài khoản trình diễn. Danh sách item lấy THẲNG từ
/// shopCatalog.json (sinh ra từ asset Unity) chứ không gõ tay, để mỗi lần chạy
/// `npm run catalog:generate` là tài khoản demo tự có luôn item mới.
function makeStarterInventory(name) {
  const items = (catalog && catalog.items) || {};
  const slots = Object.keys(items)
    .sort()
    .map((itemId) => ({
      itemId,
      // Dụng cụ là đồ nghề, không phải tài nguyên -> 1 cái là đủ, cầm 1000 cái cuốc vô nghĩa.
      quantity: items[itemId] && items[itemId].category === "tools" ? 1 : STARTER_STACK,
    }));

  return {
    version: 1,
    maxSlots: Math.max(80, slots.length + 20),
    slots,
    updatedAt: nowISO(),
  };
}

function makeInventory(name) {
  if (isStarterDemoAccount(name)) return makeStarterInventory(name);

  const baseSlots = [
    { itemId: "hoe_01", quantity: 1 },
    { itemId: "axe_01", quantity: 1 },
    { itemId: "pickaxe_01", quantity: 1 },
    { itemId: "fishing_rod_01", quantity: 1 },
    { itemId: "watering_can_01", quantity: 1 },
    { itemId: "carrot_seed_01", quantity: 5 },
  ];

  if (!isRichDemoAccount(name)) {
    return {
      version: 1,
      maxSlots: 50,
      slots: baseSlots,
      updatedAt: nowISO(),
    };
  }

  return {
    version: 1,
    maxSlots: 80,
    slots: [
      ...baseSlots,
      { itemId: "cabbage_seed_01", quantity: 50 },
      { itemId: "corn_seed_01", quantity: 50 },
      { itemId: "watermelon_seed_01", quantity: 50 },
      { itemId: "passion_fruit_seed_01", quantity: 50 },
      { itemId: "wood_01", quantity: 500 },
      { itemId: "stone_01", quantity: 500 },
      { itemId: "ore_01", quantity: 200 },
      { itemId: "water_bucket_01", quantity: 100 },
    ],
    updatedAt: nowISO(),
  };
}

function makeFarmState() {
  return {
    version: 1,
    buildings: [],
    tiles: [],
    animals: [],
    resources: [],
    updatedAt: nowISO(),
  };
}

function makeDailyLimits() {
  const periodKey = new Date().toISOString().slice(0, 10);
  return {
    version: 1,
    limits: {
      fishing: { limitKey: "fishing", periodKey, used: 0, maxCount: 10, remaining: 10, updatedAt: nowISO() },
      mining: { limitKey: "mining", periodKey, used: 0, maxCount: 10, remaining: 10, updatedAt: nowISO() },
    },
    updatedAt: nowISO(),
  };
}

function findUserInDb(db, username) {
  const key = normalizeIdentity(username);
  return (db.users || []).find((user) => normalizeIdentity(user.username) === key) || null;
}

function upsertDemoUser(db, bcrypt, name) {
  let user = findUserInDb(db, name);
  let changed = false;
  if (!user) {
    user = {
      id: `demo_${normalizeIdentity(name)}`,
      username: name,
      email: "",
      phone: "",
      password_hash: "",
      created_at: nowISO(),
      updated_at: nowISO(),
    };
    db.users.push(user);
    changed = true;
  }

  if (user.username !== name) {
    user.username = name;
    changed = true;
  }

  if (!user.password_hash || process.env.DEMO_ACCOUNT_RESET_PASSWORD !== "false") {
    user.password_hash = bcrypt.hashSync(demoPasswordFor(name), 8);
    user.updated_at = nowISO();
    changed = true;
  }

  if (!db.profiles[user.id]) {
    db.profiles[user.id] = makeProfile(name);
    changed = true;
  } else {
    db.profiles[user.id] = {
      ...db.profiles[user.id],
      name,
      characterCreated: true,
      tutorialCompleted: true,
      updatedAt: nowISO(),
    };
    changed = true;
  }

  db.economies[user.id] = makeEconomy(name);
  if (!db.inventories[user.id] || isLoadedDemoAccount(name)) {
    db.inventories[user.id] = makeInventory(name);
  }
  if (!db.farmStates[user.id]) db.farmStates[user.id] = makeFarmState();
  if (!db.dailyLimits[user.id]) db.dailyLimits[user.id] = makeDailyLimits();

  return { user, changed };
}

async function ensureDemoAccounts(store, bcrypt) {
  if (process.env.DEMO_ACCOUNTS_ENABLED === "false") return;

  if (store.mode === "postgres") {
    if (process.env.DEMO_ACCOUNTS_ENABLED !== "true") {
      console.log("[demo] PostgreSQL mode skips demo accounts unless DEMO_ACCOUNTS_ENABLED=true");
      return;
    }

    for (const name of DEMO_ACCOUNTS) {
      const passwordHash = bcrypt.hashSync(demoPasswordFor(name), 8);
      let user = await store.findUserByName(name);
      if (!user) {
        user = await store.createUser({
          id: `demo_${normalizeIdentity(name)}`,
          username: name,
          email: "",
          phone: "",
          password_hash: passwordHash,
          created_at: nowISO(),
          updated_at: nowISO(),
        });
      } else if (process.env.DEMO_ACCOUNT_RESET_PASSWORD !== "false" &&
                 typeof store.setAccountPassword === "function") {
        // Account đã tồn tại từ đợt seed trước có thể còn mật khẩu cũ -> đặt lại,
        // kẻo đổi DEMO_STARTER_PASSWORD xong vẫn không đăng nhập được.
        await store.setAccountPassword(user.id, passwordHash);
      }
      const playerId = user.player_id || user.id;
      await store.setProfile(playerId, makeProfile(name));
      await store.setEconomy(playerId, makeEconomy(name));
      await store.setInventory(playerId, makeInventory(name));
      await store.setFarmState(playerId, makeFarmState());
      await store.setDailyLimits(playerId, makeDailyLimits());
    }

    console.log(`[demo] ensured ${DEMO_ACCOUNTS.length} PostgreSQL demo accounts`);
    return;
  }

  const db = await store.readAll();
  if (!Array.isArray(db.users)) db.users = [];
  if (!db.profiles) db.profiles = {};
  if (!db.economies) db.economies = {};
  if (!db.inventories) db.inventories = {};
  if (!db.farmStates) db.farmStates = {};
  if (!db.dailyLimits) db.dailyLimits = {};

  for (const name of DEMO_ACCOUNTS) {
    upsertDemoUser(db, bcrypt, name);
  }

  await store.writeAll(db);
  console.log(`[demo] ensured ${DEMO_ACCOUNTS.length} local demo accounts`);
}

async function canonicalizeDemoAuthPayload(payload, store) {
  const source = payload || {};
  let username = source.username || source.displayName || source.name || "";
  if (!username && (source.uid || source.userId || source.playerId)) {
    const legacyPlayer = await store.getPlayer(source.uid || source.userId || source.playerId);
    username = (legacyPlayer && (legacyPlayer.username || legacyPlayer.displayName)) || "";
  }
  if (!isDemoAccount(username)) return source;

  const user = await store.findUserByName(canonicalDemoName(username));
  if (!user || !user.id) return source;
  const playerId = user.player_id || user.id;

  return {
    ...source,
    uid: playerId,
    userId: user.id,
    playerId,
    username: user.username,
    displayName: user.username,
    authSource: "local-demo",
  };
}

module.exports = {
  DEMO_ACCOUNTS,
  DEMO_STARTER_ACCOUNTS,
  isDemoAccount,
  isStarterDemoAccount,
  isAllowedDemoPassword,
  ensureDemoAccounts,
  canonicalizeDemoAuthPayload,
};
