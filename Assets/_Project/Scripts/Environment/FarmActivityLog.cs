using System;
using System.Collections.Generic;
using UnityEngine;
using YWonderLand.Backend;

namespace YWonderLand.Environment
{
    /// <summary>
    /// NHẬT KÝ nông trại (khách chốt 30/07): lưu lại LỊCH SỬ CHO ĂN từng con vật và
    /// LỊCH SỬ CHẾT của cả nông trại.
    ///
    /// Hai nơi đọc, theo đúng ý khách:
    ///  - Lịch sử cho ăn -> popup của TỪNG con vật (AnimalInteractionPopupController).
    ///  - Con chết       -> HÒM THƯ (MailboxPopupController), vì con chết rồi thì
    ///                      không còn bấm vào đâu mà xem nữa.
    ///
    /// Lưu xuống đĩa qua PlayerScopedPrefs (theo tài khoản, giống dữ liệu gameplay khác).
    /// CẮT BỚT tự động cho khỏi phình PlayerPrefs — xem 3 hằng số Max* bên dưới.
    /// </summary>
    public static class FarmActivityLog
    {
        [Serializable]
        public class FeedEntry
        {
            public string animalId;   // FarmAnimal.animalInstanceId
            public string foodText;   // "2x Cỏ Voi"
            public long unixTime;
        }

        [Serializable]
        public class DeathEntry
        {
            public string id;         // khoá duy nhất, dùng làm id thư
            public string animalName;
            public string reason;     // "chết đói"
            public long unixTime;
            public bool isRead;       // đồng bộ với trạng thái đã-đọc của thư
        }

        [Serializable]
        private class Store
        {
            public List<FeedEntry> feeds = new List<FeedEntry>();
            public List<DeathEntry> deaths = new List<DeathEntry>();
        }

        /// <summary>Khoá PlayerPrefs. Public để FarmStateSync gói kèm nhật ký lên server cùng farm-state
        /// (con vật đã lưu server rồi thì lịch sử cho ăn của nó phải đi theo, không thì cài lại game
        /// con vật còn mà nhật ký trắng — nhìn như lỗi).</summary>
        public const string PrefKey = "YW_FarmActivityLog";

        /// <summary>Nhật ký rỗng — FarmStateSync dùng làm mặc định khi server chưa có gì.</summary>
        public const string EmptyJson = "{\"feeds\":[],\"deaths\":[]}";

        /// <summary>Số mốc cho ăn giữ lại cho MỖI con (cũ hơn thì bỏ).</summary>
        private const int MaxFeedPerAnimal = 10;
        /// <summary>Trần tổng số mốc cho ăn của cả nông trại.</summary>
        private const int MaxFeedTotal = 300;
        /// <summary>Trần số lần chết lưu lại (cũng là số thư báo chết tối đa).</summary>
        private const int MaxDeaths = 50;

        private static Store store;
        // Nhật ký lưu THEO TÀI KHOẢN. Nhớ luôn scope lúc nạp để nếu người chơi đăng nhập/đổi tài khoản
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
                if (store.deaths == null) store.deaths = new List<DeathEntry>();
                return store;
            }
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

        // ── CHO ĂN ──

        /// <summary>Ghi một lần cho ăn. foodText hiện nguyên văn trong popup, vd "2x Cỏ Voi".</summary>
        public static void RecordFeed(string animalId, string foodText)
        {
            if (string.IsNullOrEmpty(animalId)) return;

            var d = Data;
            d.feeds.Add(new FeedEntry { animalId = animalId, foodText = foodText ?? "", unixTime = NowUnix() });

            TrimFeedsForAnimal(d, animalId);
            // Trần tổng: bỏ mốc CŨ NHẤT trước (danh sách xếp theo thứ tự thêm vào).
            while (d.feeds.Count > MaxFeedTotal) d.feeds.RemoveAt(0);

            Save();
        }

        private static void TrimFeedsForAnimal(Store d, string animalId)
        {
            int count = 0;
            // Duyệt NGƯỢC: giữ MaxFeedPerAnimal mốc mới nhất của con này, xoá phần cũ hơn.
            for (int i = d.feeds.Count - 1; i >= 0; i--)
            {
                if (d.feeds[i].animalId != animalId) continue;
                count++;
                if (count > MaxFeedPerAnimal) d.feeds.RemoveAt(i);
            }
        }

        /// <summary>Lịch sử cho ăn của MỘT con, MỚI NHẤT trước. max &lt;= 0 = lấy hết.</summary>
        public static List<FeedEntry> GetFeedHistory(string animalId, int max = 0)
        {
            var result = new List<FeedEntry>();
            if (string.IsNullOrEmpty(animalId)) return result;

            var feeds = Data.feeds;
            for (int i = feeds.Count - 1; i >= 0; i--)
            {
                if (feeds[i].animalId != animalId) continue;
                result.Add(feeds[i]);
                if (max > 0 && result.Count >= max) break;
            }
            return result;
        }

        /// <summary>Con vật biến mất (chết / làm thịt) thì dọn luôn mốc cho ăn của nó cho nhẹ.</summary>
        public static void ClearFeedHistory(string animalId)
        {
            if (string.IsNullOrEmpty(animalId)) return;
            var d = Data;
            if (d.feeds.RemoveAll(f => f.animalId == animalId) > 0) Save();
        }

        // ── CHẾT ──

        /// <summary>Ghi một lần con vật chết. Sẽ hiện thành THƯ trong hòm thư.</summary>
        public static void RecordDeath(string animalName, string reason)
        {
            var d = Data;
            long now = NowUnix();
            d.deaths.Add(new DeathEntry
            {
                // Kèm Count cho khỏi trùng id khi 2 con chết trong cùng một giây.
                id = $"death_{now}_{d.deaths.Count}",
                animalName = string.IsNullOrEmpty(animalName) ? "Con vật" : animalName,
                reason = reason ?? "",
                unixTime = now,
                isRead = false
            });

            while (d.deaths.Count > MaxDeaths) d.deaths.RemoveAt(0);
            Save();
        }

        /// <summary>Danh sách con đã chết, MỚI NHẤT trước.</summary>
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
    }
}
