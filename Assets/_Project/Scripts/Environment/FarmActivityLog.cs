using System;
using System.Collections.Generic;
using UnityEngine;
using YWonderLand.Backend;
using YWonderLand.Data;

namespace YWonderLand.Environment
{
    /// <summary>
    /// NHẬT KÝ nông trại (khách chốt 30/07): lưu lịch sử thao tác của TỪNG con vật và TỪNG cây,
    /// cùng danh sách con vật / cây đã chết.
    ///
    /// Ghi vào đâu, theo đúng ý khách:
    ///  - Cho ăn, Thu hoạch  -> popup của TỪNG con vật.
    ///  - Tưới nước, Thu hoạch -> popup "Xem ruộng", chọn từng cây.
    ///  - Chết (cả thú lẫn cây) -> HÒM THƯ, vì chết rồi thì không bấm vào đâu mà xem nữa.
    ///
    /// Lưu qua PlayerScopedPrefs (theo tài khoản) và đi kèm farm-state lên server — xem FarmStateSync.
    /// CẮT BỚT tự động cho khỏi phình: xem các hằng số Max* bên dưới.
    /// </summary>
    public static class FarmActivityLog
    {
        // ── Loại thao tác ──
        public const string KindFeed = "feed";
        public const string KindWater = "water";
        public const string KindHarvest = "harvest";
        public const string KindHeal = "heal";
        public const string KindVaccine = "vaccine";
        public const string KindFertilize = "fertilize";

        /// <summary>Một mốc thao tác. ownerId = animalInstanceId (thú) hoặc khoá ô đất (cây).</summary>
        [Serializable]
        public class LogEntry
        {
            public string ownerId;
            public string kind;
            public string detail;   // "2x Cỏ Voi" / "+3 Cà rốt"
            public long unixTime;
        }

        [Serializable]
        public class DeathEntry
        {
            public string id;         // khoá duy nhất, dùng làm id thư
            public string subjectName;
            public string reason;     // "chết đói" / "héo chết vì thiếu nước"
            public long unixTime;
            public bool isRead;
            public int count = 1;     // gộp nhiều cái chết cùng loại sát giờ nhau (xem RecordDeath)
        }

        /// <summary>Bản CŨ của mốc cho ăn — chỉ còn để nạp dữ liệu đã lưu trước 30/07 rồi chuyển sang events.</summary>
        [Serializable]
        public class FeedEntry
        {
            public string animalId;
            public string foodText;
            public long unixTime;
        }

        [Serializable]
        private class Store
        {
            public List<FeedEntry> feeds = new List<FeedEntry>();   // di sản, sẽ tự rỗng sau lần nạp đầu
            public List<LogEntry> events = new List<LogEntry>();
            public List<DeathEntry> deaths = new List<DeathEntry>();
        }

        /// <summary>Khoá PlayerPrefs. Public để FarmStateSync gói kèm nhật ký lên server cùng farm-state
        /// (con vật/cây đã lưu server thì lịch sử phải đi theo, không thì cài lại game là trắng nhật ký).</summary>
        public const string PrefKey = "YW_FarmActivityLog";

        /// <summary>Nhật ký rỗng — FarmStateSync dùng làm mặc định khi server chưa có gì.</summary>
        public const string EmptyJson = "{\"feeds\":[],\"events\":[],\"deaths\":[]}";

        /// <summary>Số mốc giữ lại cho MỖI (đối tượng × loại thao tác).</summary>
        private const int MaxPerOwnerKind = 10;
        /// <summary>Trần tổng số mốc của cả nông trại.</summary>
        private const int MaxEventsTotal = 400;
        /// <summary>Trần số lần chết lưu lại (cũng là số thư báo chết tối đa).</summary>
        private const int MaxDeaths = 50;
        /// <summary>Chết cùng loại trong khoảng này thì GỘP vào một thư. Cả ruộng khát nước sẽ chết
        /// gần như cùng lúc — không gộp thì hòm thư nhận vài chục thư một lúc.</summary>
        private const long DeathCoalesceWindowSec = 600;

        private static Store store;
        // Nhật ký lưu THEO TÀI KHOẢN. Nhớ scope lúc nạp để nếu người chơi đăng nhập/đổi tài khoản
        // giữa chừng thì tự nạp lại, không đưa nhầm nhật ký của người này cho người kia.
        private static string cachedScopeId;

        private static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private static Store Data
        {
            get
            {
                string scope = PlayerScopedPrefs.CurrentScopeId ?? "";
                if (store != null && cachedScopeId == scope) return store;

                store = null;
                cachedScopeId = scope;

                string json = PlayerScopedPrefs.GetString(PrefKey, "");
                if (!string.IsNullOrEmpty(json))
                {
                    try { store = JsonUtility.FromJson<Store>(json); }
                    catch (Exception e) { Debug.LogWarning($"[FarmLog] Nhật ký hỏng, dựng lại từ đầu: {e.Message}"); }
                }
                if (store == null) store = new Store();
                if (store.feeds == null) store.feeds = new List<FeedEntry>();
                if (store.events == null) store.events = new List<LogEntry>();
                if (store.deaths == null) store.deaths = new List<DeathEntry>();

                MigrateLegacyFeeds();
                return store;
            }
        }

        // Dữ liệu lưu trước 30/07 chỉ có mốc cho ăn ở danh sách riêng — chuyển sang events để
        // khỏi phải viết hai đường đọc. Chạy đúng MỘT lần cho mỗi tài khoản.
        private static void MigrateLegacyFeeds()
        {
            if (store.feeds.Count == 0) return;

            foreach (var f in store.feeds)
            {
                if (f == null) continue;
                store.events.Add(new LogEntry
                {
                    ownerId = f.animalId,
                    kind = KindFeed,
                    detail = f.foodText,
                    unixTime = f.unixTime
                });
            }
            store.feeds.Clear();
            store.events.Sort((a, b) => a.unixTime.CompareTo(b.unixTime));
            Save();
            Debug.Log("[FarmLog] Đã chuyển nhật ký cho ăn bản cũ sang định dạng mới.");
        }

        private static void Save()
        {
            if (store == null) return;
            PlayerScopedPrefs.SetString(PrefKey, JsonUtility.ToJson(store));
            PlayerScopedPrefs.Save();
        }

        /// <summary>Ép nạp lại từ đĩa ở lần đọc kế (bình thường Data tự lo khi scope đổi).</summary>
        public static void InvalidateCache()
        {
            store = null;
            cachedScopeId = null;
        }

        // ── Ghi thao tác ──

        /// <summary>Ghi một mốc thao tác. detail hiện nguyên văn trong popup, vd "2x Cỏ Voi", "+3 Cà rốt".</summary>
        public static void RecordEvent(string ownerId, string kind, string detail)
        {
            if (string.IsNullOrEmpty(ownerId) || string.IsNullOrEmpty(kind)) return;

            var d = Data;
            d.events.Add(new LogEntry { ownerId = ownerId, kind = kind, detail = detail ?? "", unixTime = NowUnix() });

            TrimForOwnerKind(d, ownerId, kind);
            while (d.events.Count > MaxEventsTotal) d.events.RemoveAt(0); // bỏ mốc cũ nhất trước

            Save();
        }

        public static void RecordFeed(string animalId, string foodText) => RecordEvent(animalId, KindFeed, foodText);
        public static void RecordWater(string tileKey, string detail) => RecordEvent(tileKey, KindWater, detail);
        public static void RecordHarvest(string ownerId, string detail) => RecordEvent(ownerId, KindHarvest, detail);

        private static void TrimForOwnerKind(Store d, string ownerId, string kind)
        {
            int count = 0;
            // Duyệt NGƯỢC: giữ MaxPerOwnerKind mốc mới nhất, xoá phần cũ hơn.
            for (int i = d.events.Count - 1; i >= 0; i--)
            {
                var e = d.events[i];
                if (e.ownerId != ownerId || e.kind != kind) continue;
                count++;
                if (count > MaxPerOwnerKind) d.events.RemoveAt(i);
            }
        }

        /// <summary>Lịch sử một loại thao tác của MỘT đối tượng, MỚI NHẤT trước. max &lt;= 0 = lấy hết.</summary>
        public static List<LogEntry> GetHistory(string ownerId, string kind, int max = 0)
        {
            var result = new List<LogEntry>();
            if (string.IsNullOrEmpty(ownerId)) return result;

            var events = Data.events;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                var e = events[i];
                if (e.ownerId != ownerId || e.kind != kind) continue;
                result.Add(e);
                if (max > 0 && result.Count >= max) break;
            }
            return result;
        }

        /// <summary>Đối tượng biến mất (thú chết/làm thịt, cây chết, gieo cây mới) thì dọn nhật ký của nó.</summary>
        public static void ClearHistory(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId)) return;
            var d = Data;
            if (d.events.RemoveAll(e => e.ownerId == ownerId) > 0) Save();
        }

        // ── Chết ──

        /// <summary>Ghi một cái chết (thú hoặc cây). Sẽ hiện thành THƯ trong hòm thư.
        /// Cùng tên + cùng nguyên nhân + sát giờ nhau thì GỘP, kẻo cả ruộng chết là ngập thư.</summary>
        public static void RecordDeath(string subjectName, string reason)
        {
            var d = Data;
            long now = NowUnix();
            string name = string.IsNullOrEmpty(subjectName) ? "Cây/con vật" : subjectName;
            string why = reason ?? "";

            if (d.deaths.Count > 0)
            {
                var last = d.deaths[d.deaths.Count - 1];
                if (last.subjectName == name && last.reason == why && now - last.unixTime <= DeathCoalesceWindowSec)
                {
                    last.count++;
                    last.unixTime = now;
                    last.isRead = false; // có cái mới -> báo lại như thư chưa đọc
                    Save();
                    return;
                }
            }

            d.deaths.Add(new DeathEntry
            {
                // Kèm Count cho khỏi trùng id khi 2 cái chết rơi vào cùng một giây.
                id = $"death_{now}_{d.deaths.Count}",
                subjectName = name,
                reason = why,
                unixTime = now,
                isRead = false,
                count = 1
            });

            while (d.deaths.Count > MaxDeaths) d.deaths.RemoveAt(0);
            Save();
        }

        /// <summary>Danh sách đã chết, MỚI NHẤT trước.</summary>
        public static List<DeathEntry> GetDeaths()
        {
            var result = new List<DeathEntry>(Data.deaths);
            result.Reverse();
            return result;
        }

        public static void MarkDeathRead(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            foreach (var e in Data.deaths)
            {
                if (e.id != id || e.isRead) continue;
                e.isRead = true;
                Save();
                return;
            }
        }

        /// <summary>Xoá thư báo chết (người chơi bấm Xoá trong hòm thư).</summary>
        public static void RemoveDeath(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            var d = Data;
            if (d.deaths.RemoveAll(e => e.id == id) > 0) Save();
        }

        // ── Hiển thị ──

        /// <summary>Đổi mốc unix thành chữ dễ đọc: "14:32 hôm nay" / "09:15 hôm qua" / "20:01 28/07".</summary>
        public static string FormatWhen(long unixTime)
        {
            DateTime local = DateTimeOffset.FromUnixTimeSeconds(unixTime).ToLocalTime().DateTime;
            DateTime today = DateTime.Now.Date;

            string clock = local.ToString("HH:mm");
            if (local.Date == today) return $"{clock} hôm nay";
            if (local.Date == today.AddDays(-1)) return $"{clock} hôm qua";
            return $"{clock} {local:dd/MM}";
        }

        /// <summary>Gộp lịch sử thành nhiều dòng cho ô thông tin trong popup.</summary>
        public static string FormatHistoryLines(List<LogEntry> entries, string emptyText)
        {
            if (entries == null || entries.Count == 0) return emptyText;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(FormatWhen(entries[i].unixTime));
                if (!string.IsNullOrEmpty(entries[i].detail)) sb.Append(" · ").Append(entries[i].detail);
            }
            return sb.ToString();
        }

        // ── Tiện ích tên vật phẩm (cây không có sẵn tên hiển thị, phải tra ItemDatabase) ──

        private static ItemDatabase itemDb;
        private static bool itemDbLoaded;

        /// <summary>Tên hiển thị của vật phẩm theo id; không tra được thì trả về fallback.</summary>
        public static string ItemName(string itemId, string fallback = "")
        {
            if (string.IsNullOrEmpty(itemId)) return fallback;
            if (!itemDbLoaded)
            {
                itemDb = Resources.Load<ItemDatabase>("ItemDatabase");
                itemDbLoaded = true;
            }
            var def = itemDb != null ? itemDb.GetItem(itemId) : null;
            return def != null && !string.IsNullOrEmpty(def.itemName) ? def.itemName : fallback;
        }
    }
}
