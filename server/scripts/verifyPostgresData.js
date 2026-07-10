const { Pool } = require("pg");

function expectedCount(name) {
  const raw = process.env[name];
  if (raw == null || raw === "") return null;
  const parsed = Number(raw);
  if (!Number.isInteger(parsed) || parsed < 0) throw new Error(`${name} must be a non-negative integer.`);
  return parsed;
}

async function main() {
  const connectionString = process.env.DATABASE_URL || process.env.POSTGRES_URL || "";
  if (!connectionString) throw new Error("DATABASE_URL or POSTGRES_URL is required.");
  const sslMode = String(process.env.PGSSL || "").toLowerCase();
  const pool = new Pool({
    connectionString,
    max: 1,
    ssl: sslMode === "require" ? { rejectUnauthorized: false } : undefined,
  });
  try {
    const result = await pool.query(
      `select
         (select count(*)::integer from game_accounts) as accounts,
         (select count(*)::integer from game_players) as players,
         (select count(*)::integer from game_transactions) as transactions`
    );
    const counts = result.rows[0];
    const expected = {
      accounts: expectedCount("VERIFY_EXPECT_ACCOUNTS"),
      players: expectedCount("VERIFY_EXPECT_PLAYERS"),
      transactions: expectedCount("VERIFY_EXPECT_TRANSACTIONS"),
    };
    for (const [key, value] of Object.entries(expected)) {
      if (value != null && counts[key] !== value) {
        throw new Error(`Expected ${key}=${value}, got ${counts[key]}.`);
      }
    }
    console.log(`[db:verify] PASS accounts=${counts.accounts} players=${counts.players} transactions=${counts.transactions}`);
  } finally {
    await pool.end();
  }
}

main().catch((error) => {
  console.error(`[db:verify] FAIL: ${error.message}`);
  process.exitCode = 1;
});
