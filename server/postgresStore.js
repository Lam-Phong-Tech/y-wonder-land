// PostgreSQL storage adapter scaffold.
// Query implementation is intentionally deferred until the DB host/driver is
// installed. Keeping this adapter separate lets routes depend on a stable store
// interface while local development continues to use JsonStore.

class PostgresStore {
  constructor(options = {}) {
    this.mode = "postgres";
    this.databaseUrl = options.databaseUrl || "";
    this.errorMessage =
      "STORE_MODE=postgres selected, but the PostgreSQL adapter is a scaffold. " +
      "Install/configure the DB driver and implement queries against server/schema.sql.";
  }

  notImplemented() {
    throw new Error(this.errorMessage);
  }

  readAll() { this.notImplemented(); }
  writeAll() { this.notImplemented(); }
  generateId() { this.notImplemented(); }
  findUserByName() { this.notImplemented(); }
  findUserById() { this.notImplemented(); }
  createUser() { this.notImplemented(); }
  getOrCreatePlayerForWebUser() { this.notImplemented(); }
  getPlayer() { this.notImplemented(); }
  getProfile() { this.notImplemented(); }
  setProfile() { this.notImplemented(); }
  ensurePlayerState() { this.notImplemented(); }
  getEconomy() { this.notImplemented(); }
  setEconomy() { this.notImplemented(); }
  applyEconomyDelta() { this.notImplemented(); }
  getInventory() { this.notImplemented(); }
  setInventory() { this.notImplemented(); }
  adjustInventoryItem() { this.notImplemented(); }
  getFarmState() { this.notImplemented(); }
  setFarmState() { this.notImplemented(); }
  getDailyLimits() { this.notImplemented(); }
  consumeDailyLimit() { this.notImplemented(); }
}

function createPostgresStore(options) {
  return new PostgresStore(options);
}

module.exports = {
  PostgresStore,
  createPostgresStore,
};
