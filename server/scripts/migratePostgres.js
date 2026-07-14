const fs = require("fs");
const path = require("path");
const { Pool } = require("pg");

function createPool() {
  const connectionString = process.env.DATABASE_URL || process.env.POSTGRES_URL || "";
  if (!connectionString) throw new Error("DATABASE_URL or POSTGRES_URL is required.");
  const sslMode = String(process.env.PGSSL || "").toLowerCase();
  return new Pool({
    connectionString,
    max: 2,
    ssl: sslMode === "require" ? { rejectUnauthorized: false } : undefined,
  });
}

async function main() {
  const migrationsDir = path.join(__dirname, "..", "migrations");
  const files = fs.readdirSync(migrationsDir)
    .filter((file) => /^\d+_.+\.sql$/i.test(file))
    .sort();
  if (files.length === 0) throw new Error(`No migrations found in ${migrationsDir}`);

  const pool = createPool();
  const client = await pool.connect();
  try {
    await client.query(
      `create table if not exists schema_migrations (
         version text primary key,
         applied_at timestamptz not null default now()
       )`
    );

    for (const file of files) {
      const version = file.replace(/\.sql$/i, "");
      const existing = await client.query(
        "select 1 from schema_migrations where version=$1",
        [version]
      );
      if (existing.rowCount > 0) {
        console.log(`[db:migrate] skip ${version}`);
        continue;
      }

      const sql = fs.readFileSync(path.join(migrationsDir, file), "utf8");
      await client.query("begin");
      try {
        await client.query(sql);
        await client.query("insert into schema_migrations(version) values ($1)", [version]);
        await client.query("commit");
        console.log(`[db:migrate] applied ${version}`);
      } catch (error) {
        await client.query("rollback");
        throw error;
      }
    }
  } finally {
    client.release();
    await pool.end();
  }
}

main().catch((error) => {
  console.error(`[db:migrate] FAIL: ${error.message}`);
  process.exitCode = 1;
});
