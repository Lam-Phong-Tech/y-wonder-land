using UnityEngine;
using Newtonsoft.Json;

namespace YWonderLand.Backend
{
    /// <summary>Hồ sơ người chơi (player_profile) — khớp docs/DATA_SCHEMA.md + cờ tutorialCompleted.</summary>
    [System.Serializable]
    public class PlayerProfile
    {
        public int version = 1;
        public string name = "Player";
        public string gender = "male";
        public string avatarId = "";
        public int level = 1;
        public float exp = 0f;
        public bool characterCreated = false;
        public bool tutorialCompleted = false;
        public string createdAt;
        public string updatedAt;
    }

    /// <summary>
    /// Nạp/lưu player_profile. Offline-first:
    /// - Load: thử server -> fallback cache local (PlayerPrefs) -> mặc định.
    /// - Save: ghi cache local NGAY (không mất dữ liệu) rồi đẩy server best-effort.
    /// </summary>
    public class PlayerProfileService : MonoBehaviour
    {
        public static PlayerProfileService Instance { get; private set; }

        private const string KEY_CACHE = "YW_Profile_Cache";

        public PlayerProfile Profile { get; private set; }
        public bool IsLoaded { get; private set; }
        public bool HasCharacterCreated => Profile != null && Profile.characterCreated;

        [System.Serializable] private class ProfileEnvelope { public PlayerProfile player_profile; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Profile = LoadCache() ?? new PlayerProfile();
        }

        /// <summary>Nạp hồ sơ: ưu tiên server, lỗi thì dùng cache local.</summary>
        public async Awaitable LoadProfileAsync()
        {
            string token = AuthService.Instance != null ? AuthService.Instance.Token : null;
            if (!string.IsNullOrEmpty(token))
            {
                var res = await ApiClient.GetAsync<ProfileEnvelope>("/player/profile", token);
                if (res.ok && res.data != null && res.data.player_profile != null)
                {
                    Profile = res.data.player_profile;
                    SaveCache();
                    IsLoaded = true;
                    Debug.Log($"[Profile] Nạp từ server: characterCreated={Profile.characterCreated}, tutorialCompleted={Profile.tutorialCompleted}");
                    return;
                }
            }

            // Fallback: dùng cache local (đã nạp ở Awake) — game vẫn chạy offline
            IsLoaded = true;
            Debug.Log($"[Profile] Dùng cache local (offline): characterCreated={Profile.characterCreated}, tutorialCompleted={Profile.tutorialCompleted}");
        }

        /// <summary>Ghi cache local ngay + đẩy server (best-effort).</summary>
        public async Awaitable SaveProfileAsync()
        {
            SaveCache(); // không mất dữ liệu kể cả khi server fail

            string token = AuthService.Instance != null ? AuthService.Instance.Token : null;
            if (string.IsNullOrEmpty(token)) return;

            var res = await ApiClient.PutAsync<object>("/player/profile",
                new ProfileEnvelope { player_profile = Profile }, token);
            if (!res.ok)
                Debug.LogWarning("[Profile] Đẩy server thất bại (sẽ giữ ở local): " + res.error);
        }

        /// <summary>Đặt cờ hoàn thành tutorial rồi lưu (fire-and-forget).</summary>
        public void SetTutorialCompleted(bool done)
        {
            Profile.tutorialCompleted = done;
            _ = SaveProfileAsync();
        }

        public void ApplyCharacterInfo(string playerName, string gender, bool markCreated = true)
        {
            if (Profile == null) Profile = new PlayerProfile();
            if (!string.IsNullOrEmpty(playerName)) Profile.name = playerName;
            if (!string.IsNullOrEmpty(gender)) Profile.gender = gender;
            if (markCreated) Profile.characterCreated = true;
            _ = SaveProfileAsync();
        }

        // ── Cache local (PlayerPrefs) ──
        private void SaveCache()
        {
            PlayerPrefs.SetString(KEY_CACHE, JsonConvert.SerializeObject(Profile));
            PlayerPrefs.Save();
        }

        private PlayerProfile LoadCache()
        {
            string json = PlayerPrefs.GetString(KEY_CACHE, "");
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonConvert.DeserializeObject<PlayerProfile>(json); }
            catch { return null; }
        }
    }
}
