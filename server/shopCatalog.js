const catalog = require("./shopCatalog.json");

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

module.exports = {
  catalog,
  resolveShopOffer,
};
