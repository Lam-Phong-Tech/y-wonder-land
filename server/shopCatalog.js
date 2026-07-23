const catalog = require("./shopCatalog.json");

// Server-authoritative husbandry rules. These values mirror the approved
// AnimalDefinition assets and are intentionally not accepted from clients.
const animalPlacementRules = Object.freeze({
  chicken_01: Object.freeze({ penSlots: 1, maxHarvests: 45 }),
  rabbit_01: Object.freeze({ penSlots: 1, maxHarvests: 2 }),
  ostrich_01: Object.freeze({ penSlots: 1, maxHarvests: 30 }),
  goat_01: Object.freeze({ penSlots: 9, maxHarvests: 60 }),
  cow_01: Object.freeze({ penSlots: 9, maxHarvests: 38 }),
  deer_01: Object.freeze({ penSlots: 9, maxHarvests: 2 }),
  pig_01: Object.freeze({ penSlots: 9, maxHarvests: 1 }),
  duck_01: Object.freeze({ penSlots: 1, maxHarvests: 45 }),
  goose_01: Object.freeze({ penSlots: 1, maxHarvests: 30 }),
  turtle_01: Object.freeze({ penSlots: 1, maxHarvests: 1 }),
});

function normalizeMode(value) {
  const mode = String(value || "").trim().toLowerCase();
  return mode === "buy" || mode === "sell" ? mode : "";
}

function resolveShopOffer(shopIdValue, modeValue, itemIdValue) {
  const shopId = String(shopIdValue || "").trim();
  const itemId = String(itemIdValue || "").trim();
  const mode = normalizeMode(modeValue);
  if (!shopId || !mode || !itemId) return { ok: false, error: "INVALID_SHOP_REQUEST" };

  const shop = catalog.shops[shopId];
  if (!shop) return { ok: false, error: "SHOP_NOT_FOUND" };

  const item = catalog.items[itemId];
  if (!item) return { ok: false, error: "ITEM_NOT_FOUND" };

  if (mode === "buy") {
    if (shop.accessMode === "sell_only" || !shop.buyItemIds.includes(itemId)) {
      return { ok: false, error: "SHOP_ITEM_NOT_ALLOWED" };
    }
    if (!Number.isSafeInteger(item.buyPrice) || item.buyPrice < 0) {
      return { ok: false, error: "INVALID_ITEM_PRICE" };
    }
    return { ok: true, shopId, mode, itemId, unitPrice: item.buyPrice };
  }

  const acceptsAllSellable = shop.sellItemIds.length === 0;
  const acceptsItem = acceptsAllSellable || shop.sellItemIds.includes(itemId);
  if (shop.accessMode === "buy_only" || !acceptsItem || !item.canSell) {
    return { ok: false, error: "SHOP_ITEM_NOT_ALLOWED" };
  }
  if (!Number.isSafeInteger(item.sellPrice) || item.sellPrice <= 0) {
    return { ok: false, error: "INVALID_ITEM_PRICE" };
  }
  return { ok: true, shopId, mode, itemId, unitPrice: item.sellPrice };
}

function resolveAnimalPlacementRule(itemIdValue) {
  const itemId = String(itemIdValue || "").trim();
  const item = catalog.items[itemId];
  const rule = animalPlacementRules[itemId];
  if (!item || item.category !== "animals" || !rule) {
    return { ok: false, error: "INVALID_ANIMAL_ITEM" };
  }
  return { ok: true, itemId, penSlots: rule.penSlots, maxHarvests: rule.maxHarvests };
}

module.exports = {
  catalog,
  animalPlacementRules,
  resolveAnimalPlacementRule,
  resolveShopOffer,
};
