using System;
using UnityEngine;

namespace YWonderLand.Backend
{
    /// <summary>
    /// PlayerPrefs wrapper for gameplay state. Signed-in players receive isolated
    /// keys while device settings (audio, camera, auth token) remain unscoped.
    /// </summary>
    public static class PlayerScopedPrefs
    {
        private const string ScopedPrefix = "YW_PlayerScope_";
        private const string LegacyOwnerPrefix = "YW_PlayerScope_LegacyOwner_";

        public static string CurrentScopeId => AuthService.GetCurrentPlayerScopeId();
        public static bool HasPlayerScope => !string.IsNullOrEmpty(CurrentScopeId);

        public static bool HasKey(string baseKey)
        {
            if (!TryGetScopedKey(baseKey, out string scopedKey, out string scopeId))
                return CanUseUnscopedState() && PlayerPrefs.HasKey(baseKey);

            return PlayerPrefs.HasKey(scopedKey) || CanClaimLegacy(baseKey, scopeId);
        }

        public static int GetInt(string baseKey, int defaultValue = 0)
        {
            if (!TryGetScopedKey(baseKey, out string scopedKey, out string scopeId))
                return CanUseUnscopedState() ? PlayerPrefs.GetInt(baseKey, defaultValue) : defaultValue;

            if (PlayerPrefs.HasKey(scopedKey))
                return PlayerPrefs.GetInt(scopedKey, defaultValue);

            if (!CanClaimLegacy(baseKey, scopeId)) return defaultValue;
            int value = PlayerPrefs.GetInt(baseKey, defaultValue);
            PlayerPrefs.SetInt(scopedKey, value);
            PlayerPrefs.Save();
            LogMigration(baseKey, scopeId);
            return value;
        }

        public static float GetFloat(string baseKey, float defaultValue = 0f)
        {
            if (!TryGetScopedKey(baseKey, out string scopedKey, out string scopeId))
                return CanUseUnscopedState() ? PlayerPrefs.GetFloat(baseKey, defaultValue) : defaultValue;

            if (PlayerPrefs.HasKey(scopedKey))
                return PlayerPrefs.GetFloat(scopedKey, defaultValue);

            if (!CanClaimLegacy(baseKey, scopeId)) return defaultValue;
            float value = PlayerPrefs.GetFloat(baseKey, defaultValue);
            PlayerPrefs.SetFloat(scopedKey, value);
            PlayerPrefs.Save();
            LogMigration(baseKey, scopeId);
            return value;
        }

        public static string GetString(string baseKey, string defaultValue = "")
        {
            if (!TryGetScopedKey(baseKey, out string scopedKey, out string scopeId))
                return CanUseUnscopedState() ? PlayerPrefs.GetString(baseKey, defaultValue) : defaultValue;

            if (PlayerPrefs.HasKey(scopedKey))
                return PlayerPrefs.GetString(scopedKey, defaultValue);

            if (!CanClaimLegacy(baseKey, scopeId)) return defaultValue;
            string value = PlayerPrefs.GetString(baseKey, defaultValue);
            PlayerPrefs.SetString(scopedKey, value);
            PlayerPrefs.Save();
            LogMigration(baseKey, scopeId);
            return value;
        }

        public static void SetInt(string baseKey, int value)
        {
            if (TryGetWritableKey(baseKey, out string key)) PlayerPrefs.SetInt(key, value);
        }

        public static void SetFloat(string baseKey, float value)
        {
            if (TryGetWritableKey(baseKey, out string key)) PlayerPrefs.SetFloat(key, value);
        }

        public static void SetString(string baseKey, string value)
        {
            if (TryGetWritableKey(baseKey, out string key)) PlayerPrefs.SetString(key, value ?? "");
        }

        public static void DeleteKey(string baseKey)
        {
            if (TryGetScopedKey(baseKey, out string scopedKey, out _))
            {
                PlayerPrefs.DeleteKey(scopedKey);
                return;
            }

            if (CanUseUnscopedState()) PlayerPrefs.DeleteKey(baseKey);
        }

        public static void Save() => PlayerPrefs.Save();

        public static string GetResolvedKeyForDebug(string baseKey)
        {
            return TryGetScopedKey(baseKey, out string scopedKey, out _) ? scopedKey : baseKey;
        }

        private static bool TryGetWritableKey(string baseKey, out string key)
        {
            if (TryGetScopedKey(baseKey, out key, out _)) return true;
            if (CanUseUnscopedState())
            {
                key = baseKey;
                return true;
            }

            key = null;
            return false;
        }

        private static bool TryGetScopedKey(string baseKey, out string scopedKey, out string scopeId)
        {
            scopeId = CurrentScopeId;
            if (string.IsNullOrEmpty(scopeId))
            {
                scopedKey = null;
                return false;
            }

            scopedKey = ScopedPrefix + Sanitize(scopeId) + "__" + baseKey;
            return true;
        }

        private static bool CanUseUnscopedState()
        {
            return BackendConfig.Active == null || BackendConfig.Active.useOfflineFallback;
        }

        private static bool CanClaimLegacy(string baseKey, string scopeId)
        {
            if (string.IsNullOrEmpty(scopeId) || !PlayerPrefs.HasKey(baseKey)) return false;

            string ownerKey = LegacyOwnerPrefix + StableHash(baseKey);
            string owner = PlayerPrefs.GetString(ownerKey, "");
            if (string.IsNullOrEmpty(owner))
            {
                PlayerPrefs.SetString(ownerKey, scopeId);
                PlayerPrefs.Save();
                return true;
            }

            return string.Equals(owner, scopeId, StringComparison.Ordinal);
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "unknown";
            return value.Replace("\\", "_")
                .Replace("/", "_")
                .Replace(":", "_")
                .Replace(" ", "_");
        }

        private static string StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in value ?? "")
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return hash.ToString("x8");
            }
        }

        private static void LogMigration(string baseKey, string scopeId)
        {
            Debug.Log($"[PlayerPrefs] Migrated legacy '{baseKey}' to player scope '{scopeId}'.");
        }
    }
}
