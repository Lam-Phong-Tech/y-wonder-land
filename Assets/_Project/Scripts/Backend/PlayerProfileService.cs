using UnityEngine;
using Newtonsoft.Json;

namespace YWonderLand.Backend
{
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
    /// Loads and saves one player profile per signed-in user. The legacy shared cache
    /// is kept only for old/offline flows that do not have an auth identity yet.
    /// </summary>
    public class PlayerProfileService : MonoBehaviour
    {
        public static PlayerProfileService Instance { get; private set; }

        private const string KEY_CACHE = "YW_Profile_Cache";
        private const string KEY_CACHE_PREFIX = "YW_Profile_Cache_";

        public PlayerProfile Profile { get; private set; }
        public bool IsLoaded { get; private set; }
        public bool HasCharacterCreated => Profile != null && Profile.characterCreated;

        [System.Serializable] private class ProfileEnvelope { public PlayerProfile player_profile; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Profile = LoadCacheForCurrentUser(true) ?? CreateDefaultProfileForCurrentAuth();
        }

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
                    Debug.Log($"[Profile] Loaded from server: name={Profile.name}, characterCreated={Profile.characterCreated}, tutorialCompleted={Profile.tutorialCompleted}");
                    return;
                }

                Debug.LogWarning($"[Profile] Server load failed for {GetCurrentCacheKey()}: {res.error}");
            }

            bool hasAuthIdentity = HasAuthIdentity();
            Profile = LoadCacheForCurrentUser(!hasAuthIdentity) ?? CreateDefaultProfileForCurrentAuth();
            IsLoaded = true;
            Debug.Log($"[Profile] Loaded local fallback for {GetCurrentCacheKey()}: name={Profile.name}, characterCreated={Profile.characterCreated}, tutorialCompleted={Profile.tutorialCompleted}");
        }

        public async Awaitable SaveProfileAsync()
        {
            if (Profile == null) Profile = CreateDefaultProfileForCurrentAuth();
            SaveCache();

            string token = AuthService.Instance != null ? AuthService.Instance.Token : null;
            if (string.IsNullOrEmpty(token)) return;

            var res = await ApiClient.PutAsync<object>("/player/profile",
                new ProfileEnvelope { player_profile = Profile }, token);
            if (!res.ok)
                Debug.LogWarning("[Profile] Failed to push profile to server, kept local cache: " + res.error);
        }

        public void ResetRuntimeProfileForAuthChange()
        {
            Profile = CreateDefaultProfileForCurrentAuth();
            IsLoaded = false;
        }

        public void AcceptServerProfile(PlayerProfile serverProfile)
        {
            if (serverProfile == null) return;
            Profile = serverProfile;
            IsLoaded = true;
            SaveCache();
        }

        public void SetTutorialCompleted(bool done)
        {
            if (Profile == null) Profile = CreateDefaultProfileForCurrentAuth();
            Profile.tutorialCompleted = done;
            _ = SaveProfileAsync();
        }

        public void ApplyCharacterInfo(string playerName, string gender, bool markCreated = true)
        {
            if (Profile == null) Profile = CreateDefaultProfileForCurrentAuth();
            if (!string.IsNullOrEmpty(playerName)) Profile.name = playerName;
            if (!string.IsNullOrEmpty(gender)) Profile.gender = gender;
            if (markCreated) Profile.characterCreated = true;
            _ = SaveProfileAsync();
        }

        private void SaveCache()
        {
            PlayerPrefs.SetString(GetCurrentCacheKey(), JsonConvert.SerializeObject(Profile));
            PlayerPrefs.Save();
        }

        private PlayerProfile LoadCacheForCurrentUser(bool allowLegacyFallback)
        {
            string key = GetCurrentCacheKey();
            PlayerProfile profile = LoadCache(key);
            if (profile != null) return profile;

            if (allowLegacyFallback && key != KEY_CACHE)
                return LoadCache(KEY_CACHE);

            return null;
        }

        private static PlayerProfile LoadCache(string key)
        {
            string json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonConvert.DeserializeObject<PlayerProfile>(json); }
            catch { return null; }
        }

        private static PlayerProfile CreateDefaultProfileForCurrentAuth()
        {
            var profile = new PlayerProfile();
            string username = AuthService.Instance != null ? AuthService.Instance.Username : "";
            if (!string.IsNullOrEmpty(username))
                profile.name = username;
            return profile;
        }

        private static bool HasAuthIdentity()
        {
            var auth = AuthService.Instance;
            return auth != null
                   && (!string.IsNullOrEmpty(auth.UserId) || !string.IsNullOrEmpty(auth.Username));
        }

        private static string GetCurrentCacheKey()
        {
            var auth = AuthService.Instance;
            if (auth != null)
            {
                if (!string.IsNullOrEmpty(auth.UserId))
                    return KEY_CACHE_PREFIX + SanitizeCacheKeyPart(auth.UserId);
                if (!string.IsNullOrEmpty(auth.Username))
                    return KEY_CACHE_PREFIX + "name_" + SanitizeCacheKeyPart(auth.Username.ToLowerInvariant());
            }

            return KEY_CACHE;
        }

        private static string SanitizeCacheKeyPart(string value)
        {
            if (string.IsNullOrEmpty(value)) return "unknown";
            return value.Replace("\\", "_")
                .Replace("/", "_")
                .Replace(":", "_")
                .Replace(" ", "_");
        }
    }
}
