// Bảng cá + luật câu — NGUỒN SỰ THẬT phía SERVER (khách chốt 29/06, xem CacLoaiCa.md).
// Server tự bốc cá để client KHÔNG tự quyết được phần thưởng (chống câu cá hiếm bằng client hack).

const FISHING_FREE_PER_DAY = 10; // 10 lượt miễn phí/ngày
const FISHING_BAIT_ID = "bait_01"; // hết free thì mỗi lần câu ăn 1 mồi

// Nhóm theo giá trị Point + tỉ lệ trúng (%). Tổng tỉ lệ = 100.
const FISH_TIERS = Object.freeze([
  { pointValue: 2, chance: 45, items: ["fish_ca_com_01", "fish_ca_nuc_01", "fish_ca_hong_01"] },
  { pointValue: 4, chance: 25, items: ["fish_ca_su_tu_01", "fish_ca_naso_01", "fish_ca_nhong_01"] },
  { pointValue: 6, chance: 17, items: ["fish_ca_soc_dua_01", "fish_ca_khe_01", "fish_ca_mu_01"] },
  { pointValue: 10, chance: 7, items: ["fish_ca_mat_quy_01", "fish_ca_heo_bien_01"] },
  { pointValue: 15, chance: 4, items: ["fish_ca_hoang_de_01", "fish_ca_ngu_hoang_kim_01"] },
  { pointValue: 25, chance: 2, items: ["fish_ca_rong_do_01"] },
]);

const ALL_FISH_IDS = Object.freeze(FISH_TIERS.flatMap((t) => t.items));

// Bốc 1 con cá: random nhóm theo tỉ lệ, rồi chọn ngẫu nhiên 1 loài trong nhóm.
// rng: hàm trả [0,1) — cho phép test bơm số cố định.
function rollFish(rng = Math.random) {
  const totalChance = FISH_TIERS.reduce((sum, t) => sum + t.chance, 0);
  let roll = rng() * totalChance;
  let tier = FISH_TIERS[FISH_TIERS.length - 1];
  for (const t of FISH_TIERS) {
    if (roll < t.chance) { tier = t; break; }
    roll -= t.chance;
  }
  const idx = Math.min(tier.items.length - 1, Math.floor(rng() * tier.items.length));
  return { itemId: tier.items[idx], pointValue: tier.pointValue };
}

function isFishItem(itemId) {
  return ALL_FISH_IDS.includes(String(itemId || "").trim());
}

module.exports = {
  FISHING_FREE_PER_DAY,
  FISHING_BAIT_ID,
  FISH_TIERS,
  ALL_FISH_IDS,
  rollFish,
  isFishItem,
};
