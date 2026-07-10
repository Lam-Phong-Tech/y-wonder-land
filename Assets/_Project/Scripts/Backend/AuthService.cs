using UnityEngine;

namespace YWonderLand.Backend
{
    /// <summary>
    /// Đăng nhập/đăng ký qua REST, giữ token + userId (cache PlayerPrefs để đăng nhập lại im lặng).
    /// Offline-first: nếu server không kết nối được, IsSignedIn = false nhưng game vẫn chạy
    /// (PlayerProfileService sẽ fallback dữ liệu local).
    /// </summary>
    public class AuthService : MonoBehaviour
    {
        public static AuthService Instance { get; private set; }

        private const string KEY_TOKEN = "YW_Auth_Token";
        private const string KEY_USERID = "YW_Auth_UserId";
        private const string KEY_USERNAME = "YW_Auth_Username";
        private const string KEY_BACKEND_URL = "YW_Auth_BackendUrl";

        public string Token { get; private set; }
        public string UserId { get; private set; }
        public string Username { get; private set; }
        public bool IsSignedIn => !string.IsNullOrEmpty(Token);
        public long LastStatus { get; private set; }
        public string LastError { get; private set; }
        public string LastErrorCode { get; private set; }
        public bool LastRequestCouldNotReachServer => LastStatus == 0 && !string.IsNullOrEmpty(LastError);

        // DTOs khớp với server stub
        [System.Serializable]
        private class AuthRequest
        {
            public string username;
            public string password;
            public string email;
            public string phone;
        }
        [System.Serializable]
        private class AuthResponse
        {
            public string token;
            public string userId;
            public string user_id;
            public string playerId;
            public string player_id;
            public string webUserId;
            public string web_user_id;
            public string username;
            public string refCode;
            public string ref_code;
            public PlayerProfile player_profile;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Token = PlayerPrefs.GetString(KEY_TOKEN, "");
            UserId = PlayerPrefs.GetString(KEY_USERID, "");
            Username = PlayerPrefs.GetString(KEY_USERNAME, "");

            string cachedBackendUrl = PlayerPrefs.GetString(KEY_BACKEND_URL, "");
            string activeBackendUrl = GetActiveBackendUrl();
            if (!string.IsNullOrEmpty(Token) &&
                !string.Equals(cachedBackendUrl, activeBackendUrl, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("[Auth] Backend URL changed, clearing cached token.");
                ClearCachedAuth();
            }
        }

        /// <summary>Thử đăng nhập; nếu tài khoản chưa tồn tại thì tự đăng ký. Trả về true nếu có token.</summary>
        public async Awaitable<bool> EnsureSignedInAsync(string username, string password)
        {
            if (await LoginAsync(username, password)) return true;
            return await RegisterAsync(username, password);
        }

        public async Awaitable<bool> LoginAsync(string username, string password)
        {
            var res = await ApiClient.PostAsync<AuthResponse>("/auth/login",
                new AuthRequest { username = username, password = password });
            if (ApplyAuth(res, username)) return true;

            // Phase 1 supports self-registered game accounts first. If the
            // account is not local, fall back to the web-auth bridge.
            if (res.status != 404)
                return false;

            var webRes = await ApiClient.PostAsync<AuthResponse>("/auth/web-login",
                new AuthRequest { username = username, password = password });

            // Demo/local mode intentionally disables web auth. Preserve the useful
            // USER_NOT_FOUND result instead of replacing it with a misleading 503.
            if (string.Equals(webRes.errorCode, "WEB_AUTH_DISABLED", System.StringComparison.OrdinalIgnoreCase))
            {
                RememberFailure(res);
                return false;
            }

            return ApplyAuth(webRes, username);
        }

        public async Awaitable<bool> RegisterAsync(string username, string password, string email = "", string phone = "")
        {
            var res = await ApiClient.PostAsync<AuthResponse>("/auth/register",
                new AuthRequest { username = username, password = password, email = email, phone = phone });
            return ApplyAuth(res, username);
        }

        private bool ApplyAuth(ApiResult<AuthResponse> res, string username)
        {
            if (!res.ok || res.data == null || string.IsNullOrEmpty(res.data.token))
            {
                RememberFailure(res);
                return false;
            }

            LastStatus = res.status;
            LastError = "";
            LastErrorCode = "";

            string nextUserId = ResolveUserId(res.data);
            string nextUsername = ResolveUsername(res.data, username);
            bool identityChanged = !string.Equals(UserId, nextUserId, System.StringComparison.Ordinal)
                                   || !string.Equals(Username, nextUsername, System.StringComparison.OrdinalIgnoreCase);

            Token = res.data.token;
            UserId = nextUserId;
            Username = nextUsername;
            PlayerPrefs.SetString(KEY_TOKEN, Token);
            PlayerPrefs.SetString(KEY_USERID, UserId);
            PlayerPrefs.SetString(KEY_USERNAME, Username);
            PlayerPrefs.SetString(KEY_BACKEND_URL, GetActiveBackendUrl());
            PlayerPrefs.Save();
            if (identityChanged)
                PlayerProfileService.Instance?.ResetRuntimeProfileForAuthChange();

            if (res.data.player_profile != null)
                PlayerProfileService.Instance?.AcceptServerProfile(res.data.player_profile);
            Debug.Log($"[Auth] Đăng nhập thành công: {username} ({UserId})");
            return true;
        }

        private void RememberFailure(ApiResult<AuthResponse> res)
        {
            LastStatus = res.status;
            LastError = res.error ?? "";
            LastErrorCode = res.errorCode ?? "";
        }

        private static string ResolveUserId(AuthResponse data)
        {
            if (data == null) return "";
            if (!string.IsNullOrEmpty(data.playerId)) return data.playerId;
            if (!string.IsNullOrEmpty(data.player_id)) return data.player_id;
            if (!string.IsNullOrEmpty(data.userId)) return data.userId;
            if (!string.IsNullOrEmpty(data.user_id)) return data.user_id;
            if (!string.IsNullOrEmpty(data.webUserId)) return data.webUserId;
            if (!string.IsNullOrEmpty(data.web_user_id)) return data.web_user_id;
            return "";
        }

        private static string ResolveUsername(AuthResponse data, string requestedUsername)
        {
            if (data != null)
            {
                if (!string.IsNullOrEmpty(data.username)) return data.username;
                if (!string.IsNullOrEmpty(data.refCode)) return data.refCode;
                if (!string.IsNullOrEmpty(data.ref_code)) return data.ref_code;
            }

            return requestedUsername;
        }

        public void SignOut()
        {
            Token = "";
            UserId = "";
            Username = "";
            ClearCachedAuth();
            PlayerBootstrapService.Instance?.ResetRuntimeState();
            PlayerProfileService.Instance?.ResetRuntimeProfileForAuthChange();
        }

        private static string GetActiveBackendUrl()
        {
            return BackendConfig.Active != null ? BackendConfig.Active.baseUrl.TrimEnd('/') : "";
        }

        private void ClearCachedAuth()
        {
            Token = "";
            UserId = "";
            Username = "";
            PlayerPrefs.DeleteKey(KEY_TOKEN);
            PlayerPrefs.DeleteKey(KEY_USERID);
            PlayerPrefs.DeleteKey(KEY_USERNAME);
            PlayerPrefs.DeleteKey(KEY_BACKEND_URL);
            PlayerPrefs.Save();
        }
    }
}
