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

        public string Token { get; private set; }
        public string UserId { get; private set; }
        public bool IsSignedIn => !string.IsNullOrEmpty(Token);

        // DTOs khớp với server stub
        [System.Serializable] private class AuthRequest { public string username; public string password; }
        [System.Serializable] private class AuthResponse { public string token; public string userId; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Token = PlayerPrefs.GetString(KEY_TOKEN, "");
            UserId = PlayerPrefs.GetString(KEY_USERID, "");
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
            return ApplyAuth(res, username);
        }

        public async Awaitable<bool> RegisterAsync(string username, string password)
        {
            var res = await ApiClient.PostAsync<AuthResponse>("/auth/register",
                new AuthRequest { username = username, password = password });
            return ApplyAuth(res, username);
        }

        private bool ApplyAuth(ApiResult<AuthResponse> res, string username)
        {
            if (!res.ok || res.data == null || string.IsNullOrEmpty(res.data.token))
                return false;

            Token = res.data.token;
            UserId = res.data.userId;
            PlayerPrefs.SetString(KEY_TOKEN, Token);
            PlayerPrefs.SetString(KEY_USERID, UserId);
            PlayerPrefs.SetString(KEY_USERNAME, username);
            PlayerPrefs.Save();
            Debug.Log($"[Auth] Đăng nhập thành công: {username} ({UserId})");
            return true;
        }

        public void SignOut()
        {
            Token = "";
            UserId = "";
            PlayerPrefs.DeleteKey(KEY_TOKEN);
            PlayerPrefs.DeleteKey(KEY_USERID);
            PlayerPrefs.Save();
        }
    }
}
