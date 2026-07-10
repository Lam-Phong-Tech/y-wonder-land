const DEMO_RICH_ACCOUNTS = ["DemoRich01", "DemoRich02", "DemoRich03", "DemoRich04", "DemoRich05"];
const DEMO_REALTIME_ACCOUNTS = ["DemoRealtime01", "DemoRealtime02", "DemoRealtime03", "DemoRealtime04", "DemoRealtime05"];
const DEMO_ACCOUNTS = [...DEMO_RICH_ACCOUNTS, ...DEMO_REALTIME_ACCOUNTS];
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

function isAllowedDemoPassword(username, password) {
  if (!isDemoAccount(username)) return false;
  const canonicalName = canonicalDemoName(username);
  const pass = String(password || "");
  return pass === "demo" || pass.toLowerCase() === canonicalName.toLowerCase();
}

function makeProfile(name) {
  return {
    version: 1,
    name,
    gender: "male",
    avatarId: "",
    level: isRichDemoAccount(name) ? 12 : 1,
    exp: isRichDemoAccount(name) ? 45 : 0,
    characterCreated: true,
    tutorialCompleted: true,
    createdAt: nowISO(),
    updatedAt: nowISO(),
  };
}

function makeEconomy(name) {
  if (isRichDemoAccount(name)) {
    return {
      version: 1,
      pos: 500000,
      upos: 2500,
      updatedAt: nowISO(),
    };
  }

  return {
    version: 1,
    pos: 5000,
    upos: 0,
    updatedAt: nowISO(),
  };
}

function makeInventory(name) {
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
    user.password_hash = bcrypt.hashSync(name, 8);
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
  if (!db.inventories[user.id] || isRichDemoAccount(name)) {
    db.inventories[user.id] = makeInventory(name);
  }
  if (!db.farmStates[user.id]) db.farmStates[user.id] = makeFarmState();
  if (!db.dailyLimits[user.id]) db.dailyLimits[user.id] = makeDailyLimits();

  return { user, changed };
}

function ensureDemoAccounts(store, bcrypt) {
  if (process.env.DEMO_ACCOUNTS_ENABLED === "false") return;

  const db = store.readAll();
  if (!Array.isArray(db.users)) db.users = [];
  if (!db.profiles) db.profiles = {};
  if (!db.economies) db.economies = {};
  if (!db.inventories) db.inventories = {};
  if (!db.farmStates) db.farmStates = {};
  if (!db.dailyLimits) db.dailyLimits = {};

  for (const name of DEMO_ACCOUNTS) {
    upsertDemoUser(db, bcrypt, name);
  }

  store.writeAll(db);
  console.log(`[demo] ensured ${DEMO_ACCOUNTS.length} local demo accounts`);
}

function canonicalizeDemoAuthPayload(payload, store) {
  const source = payload || {};
  let username = source.username || source.displayName || source.name || "";
  if (!username && (source.uid || source.userId || source.playerId)) {
    const legacyPlayer = store.getPlayer(source.uid || source.userId || source.playerId);
    username = (legacyPlayer && (legacyPlayer.username || legacyPlayer.displayName)) || "";
  }
  if (!isDemoAccount(username)) return source;

  const user = store.findUserByName(canonicalDemoName(username));
  if (!user || !user.id) return source;

  return {
    ...source,
    uid: user.id,
    userId: user.id,
    playerId: user.id,
    username: user.username,
    displayName: user.username,
    authSource: "local-demo",
  };
}

module.exports = {
  DEMO_ACCOUNTS,
  isDemoAccount,
  isAllowedDemoPassword,
  ensureDemoAccounts,
  canonicalizeDemoAuthPayload,
};
