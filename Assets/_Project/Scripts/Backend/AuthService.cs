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

        public event System.Action<string, string> IdentityChanging;
        public event System.Action<string, string> IdentityChanged;

        private const string KEY_TOKEN = "YW_Auth_Token";
        private const string KEY_USERID = "YW_Auth_UserId";
        private const string KEY_USERNAME = "YW_Auth_Username";
        private const string KEY_BACKEND_URL = "YW_Auth_BackendUrl";
        private int browserAuthAttempt;

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
            public string status;
            public PlayerProfile player_profile;
        }

        [System.Serializable]
        private class BrowserAuthStartRequest
        {
            public string code_challenge;
            public string intent;
        }

        [System.Serializable]
        private class BrowserAuthStartResponse
        {
            public string requestId;
            public string authUrl;
            public int expiresInSec;
            public int pollIntervalMs;
        }

        [System.Serializable]
        private class BrowserAuthExchangeRequest
        {
            public string requestId;
            public string code_verifier;
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

        public async Awaitable<bool> LoginLocalAsync(string username, string password)
        {
            var res = await ApiClient.PostAsync<AuthResponse>("/auth/login",
                new AuthRequest { username = username, password = password });
            return ApplyAuth(res, username);
        }

        public async Awaitable<bool> LoginWebAsync(string username, string password)
        {
            var res = await ApiClient.PostAsync<AuthResponse>("/auth/web-login",
                new AuthRequest { username = username, password = password });
            return ApplyAuth(res, username);
        }

        public async Awaitable<bool> LoginWithBrowserAsync(string intent, System.Action<string> onStatus = null)
        {
            var config = BackendConfig.Active;
            if (config == null || !config.browserAuthEnabled)
            {
                SetFailure(0, "Browser auth is disabled.", "BROWSER_AUTH_DISABLED");
                return false;
            }

            string verifier = CreateBrowserCodeVerifier();
            string challenge = CreateBrowserCodeChallenge(verifier);
            int attempt = ++browserAuthAttempt;
            var started = await ApiClient.PostAsync<BrowserAuthStartResponse>("/auth/browser/start",
                new BrowserAuthStartRequest
                {
                    code_challenge = challenge,
                    intent = string.Equals(intent, "register", System.StringComparison.OrdinalIgnoreCase)
                        ? "register"
                        : "login",
                });
            if (!started.ok || started.data == null || string.IsNullOrEmpty(started.data.requestId))
            {
                RememberFailure(started);
                return false;
            }
            bool hasTrustedWebOrigin = System.Uri.TryCreate(
                config.registrationUrl,
                System.UriKind.Absolute,
                out var trustedWebUri);
            if (!System.Uri.TryCreate(started.data.authUrl, System.UriKind.Absolute, out var authUri) ||
                !string.Equals(authUri.Scheme, System.Uri.UriSchemeHttps, System.StringComparison.OrdinalIgnoreCase) ||
                !hasTrustedWebOrigin ||
                !string.Equals(authUri.Scheme, trustedWebUri.Scheme, System.StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(authUri.Host, trustedWebUri.Host, System.StringComparison.OrdinalIgnoreCase))
            {
                SetFailure(0, "Browser auth URL is invalid.", "BROWSER_AUTH_INVALID_URL");
                return false;
            }

            onStatus?.Invoke("Đã mở website. Hoàn tất xác thực để quay lại game...");
            Application.OpenURL(authUri.AbsoluteUri);

            int timeoutSec = Mathf.Clamp(
                config.browserAuthTimeoutSec > 0 ? config.browserAuthTimeoutSec : started.data.expiresInSec,
                30,
                900);
            float pollSeconds = Mathf.Clamp(started.data.pollIntervalMs / 1000f, 0.5f, 5f);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (attempt == browserAuthAttempt && stopwatch.Elapsed.TotalSeconds < timeoutSec)
            {
                var exchanged = await ApiClient.PostAsync<AuthResponse>("/auth/browser/exchange",
                    new BrowserAuthExchangeRequest
                    {
                        requestId = started.data.requestId,
                        code_verifier = verifier,
                    });

                if (exchanged.ok && exchanged.data != null && !string.IsNullOrEmpty(exchanged.data.token))
                {
                    onStatus?.Invoke("Website đã xác thực. Đang nạp dữ liệu nhân vật...");
                    return ApplyAuth(exchanged, exchanged.data.username);
                }
                if (exchanged.status != 202)
                {
                    RememberFailure(exchanged);
                    return false;
                }

                await Awaitable.WaitForSecondsAsync(pollSeconds);
            }

            if (attempt != browserAuthAttempt)
            {
                SetFailure(0, "Browser auth was cancelled.", "BROWSER_AUTH_CANCELLED");
            }
            else
            {
                SetFailure(410, "Browser auth timed out.", "BROWSER_AUTH_EXPIRED");
            }
            return false;
        }

        public void CancelBrowserLogin()
        {
            browserAuthAttempt++;
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

            string previousScopeId = GetScopeId(UserId, Username);
            string nextUserId = ResolveUserId(res.data);
            string nextUsername = ResolveUsername(res.data, username);
            string nextScopeId = GetScopeId(nextUserId, nextUsername);
            bool identityChanged = !string.Equals(UserId, nextUserId, System.StringComparison.Ordinal)
                                   || !string.Equals(Username, nextUsername, System.StringComparison.OrdinalIgnoreCase);

            if (identityChanged)
                InvokeIdentityHandlers(IdentityChanging, previousScopeId, nextScopeId, "changing");

            Token = res.data.token;
            UserId = nextUserId;
            Username = nextUsername;
            PlayerPrefs.SetString(KEY_TOKEN, Token);
            PlayerPrefs.SetString(KEY_USERID, UserId);
            PlayerPrefs.SetString(KEY_USERNAME, Username);
            PlayerPrefs.SetString(KEY_BACKEND_URL, GetActiveBackendUrl());
            PlayerPrefs.Save();
            if (identityChanged)
            {
                PlayerProfileService.Instance?.ResetRuntimeProfileForAuthChange();
                InvokeIdentityHandlers(IdentityChanged, previousScopeId, nextScopeId, "changed");
            }

            if (res.data.player_profile != null)
                PlayerProfileService.Instance?.AcceptServerProfile(res.data.player_profile);
            Debug.Log($"[Auth] Đăng nhập thành công: {username} ({UserId})");
            return true;
        }

        private void RememberFailure<T>(ApiResult<T> res)
        {
            SetFailure(res.status, res.error, res.errorCode);
        }

        private void SetFailure(long status, string error, string errorCode)
        {
            LastStatus = status;
            LastError = error ?? "";
            LastErrorCode = errorCode ?? "";
        }

        private static string CreateBrowserCodeVerifier()
        {
            var bytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return Base64Url(bytes);
        }

        private static string CreateBrowserCodeChallenge(string verifier)
        {
            byte[] input = System.Text.Encoding.ASCII.GetBytes(verifier);
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return Base64Url(sha.ComputeHash(input));
        }

        private static string Base64Url(byte[] bytes)
        {
            return System.Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
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
            CancelBrowserLogin();
            string previousScopeId = GetScopeId(UserId, Username);
            if (!string.IsNullOrEmpty(previousScopeId))
                InvokeIdentityHandlers(IdentityChanging, previousScopeId, "", "changing");

            Token = "";
            UserId = "";
            Username = "";
            ClearCachedAuth();
            PlayerBootstrapService.Instance?.ResetRuntimeState();
            PlayerProfileService.Instance?.ResetRuntimeProfileForAuthChange();
            if (!string.IsNullOrEmpty(previousScopeId))
                InvokeIdentityHandlers(IdentityChanged, previousScopeId, "", "changed");
        }

        public static string GetCurrentPlayerScopeId()
        {
            if (Instance != null)
                return GetScopeId(Instance.UserId, Instance.Username);

            return GetScopeId(
                PlayerPrefs.GetString(KEY_USERID, ""),
                PlayerPrefs.GetString(KEY_USERNAME, ""));
        }

        private static string GetScopeId(string userId, string username)
        {
            if (!string.IsNullOrEmpty(userId)) return "id:" + userId;
            if (!string.IsNullOrEmpty(username)) return "name:" + username.ToLowerInvariant();
            return "";
        }

        private static void InvokeIdentityHandlers(
            System.Action<string, string> handlers,
            string previousScopeId,
            string nextScopeId,
            string phase)
        {
            if (handlers == null) return;
            foreach (System.Action<string, string> handler in handlers.GetInvocationList())
            {
                try { handler(previousScopeId, nextScopeId); }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Auth] Player scope {phase} handler failed: {handler.Method.DeclaringType?.Name}.{handler.Method.Name}");
                    Debug.LogException(ex);
                }
            }
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
