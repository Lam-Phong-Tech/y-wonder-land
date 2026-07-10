const fs = require("fs");
const path = require("path");

const repoRoot = path.resolve(__dirname, "..", "..");
const itemsDir = path.join(repoRoot, "Assets", "Resources", "Items");
const shopsDir = path.join(repoRoot, "Assets", "_Project", "Data", "Shops");
const outputPath = path.join(repoRoot, "server", "shopCatalog.json");

function readScalar(source, field, required = true) {
  const match = source.match(new RegExp(`^  ${field}:\\s*(.*?)\\s*$`, "m"));
  if (!match) {
    if (!required) return "";
    throw new Error(`Missing '${field}' field.`);
  }
  return match[1].trim();
}

function readInteger(source, field) {
  const value = Number(readScalar(source, field));
  if (!Number.isInteger(value)) throw new Error(`Invalid integer '${field}': ${value}`);
  return value;
}

function readList(source, field) {
  const lines = source.split(/\r?\n/);
  const headerIndex = lines.findIndex((line) => line.startsWith(`  ${field}:`));
  if (headerIndex < 0) throw new Error(`Missing '${field}' list.`);

  const inline = lines[headerIndex].slice(lines[headerIndex].indexOf(":") + 1).trim();
  if (inline === "[]") return [];

  const values = [];
  for (let i = headerIndex + 1; i < lines.length; i += 1) {
    const line = lines[i];
    const item = line.match(/^  -\s+(.+?)\s*$/);
    if (item) {
      values.push(item[1]);
      continue;
    }
    if (/^  [A-Za-z_][A-Za-z0-9_]*:/.test(line)) break;
  }
  return values;
}

function assetFiles(directory) {
  return fs.readdirSync(directory)
    .filter((name) => name.endsWith(".asset"))
    .sort((a, b) => a.localeCompare(b));
}

function buildItems() {
  const items = {};
  for (const fileName of assetFiles(itemsDir)) {
    const source = fs.readFileSync(path.join(itemsDir, fileName), "utf8");
    if (!source.includes("YWonderLand.Data.ItemDefinition")) continue;
    const id = readScalar(source, "id");
    if (!id) throw new Error(`${fileName}: empty item id.`);
    if (items[id]) throw new Error(`${fileName}: duplicate item id '${id}'.`);

    items[id] = {
      category: readScalar(source, "category", false),
      buyPrice: readInteger(source, "buyPrice"),
      sellPrice: readInteger(source, "sellPrice"),
      canSell: readInteger(source, "canSell") === 1,
    };
  }
  return items;
}

function buildShops(items) {
  const accessModes = ["both", "buy_only", "sell_only"];
  const shops = {};

  for (const fileName of assetFiles(shopsDir)) {
    const source = fs.readFileSync(path.join(shopsDir, fileName), "utf8");
    const shopId = readScalar(source, "m_Name");
    const accessModeIndex = readInteger(source, "accessMode");
    const buyItemIds = readList(source, "buyItemIds");
    const sellItemIds = readList(source, "sellItemIds");
    const accessMode = accessModes[accessModeIndex];
    if (!accessMode) throw new Error(`${fileName}: unknown accessMode ${accessModeIndex}.`);
    if (shops[shopId]) throw new Error(`${fileName}: duplicate shop id '${shopId}'.`);

    for (const itemId of [...buyItemIds, ...sellItemIds]) {
      if (!items[itemId]) throw new Error(`${fileName}: unknown item id '${itemId}'.`);
    }

    shops[shopId] = { accessMode, buyItemIds, sellItemIds };
  }
  return shops;
}

const items = buildItems();
const shops = buildShops(items);
const catalog = {
  version: 1,
  source: "Unity ItemDefinition and ShopDefinition assets",
  items,
  shops,
};

fs.writeFileSync(outputPath, `${JSON.stringify(catalog, null, 2)}\n`, "utf8");
console.log(`[shop-catalog] Wrote ${Object.keys(items).length} items and ${Object.keys(shops).length} shops to ${outputPath}.`);
