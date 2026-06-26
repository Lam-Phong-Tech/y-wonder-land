using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace YWonderLand.Backend
{
    /// <summary>Kết quả 1 lời gọi API. ok=false nghĩa là lỗi mạng/server (đọc error/status).</summary>
    public struct ApiResult<T>
    {
        public bool ok;
        public T data;
        public long status;   // HTTP status code (0 nếu không kết nối được)
        public string error;
    }

    /// <summary>
    /// Wrapper gọi REST bằng UnityWebRequest + Newtonsoft.Json (đều có sẵn trong dự án).
    /// Mọi hàm đều bọc try/catch + timeout -> KHÔNG bao giờ ném exception ra ngoài
    /// (offline-first: lỗi trả về ApiResult.ok=false để caller fallback local).
    /// </summary>
    public static class ApiClient
    {
        private static string Url(string path)
        {
            var cfg = BackendConfig.Active;
            string baseUrl = cfg != null ? cfg.baseUrl.TrimEnd('/') : "http://localhost:3000";
            return baseUrl + (path.StartsWith("/") ? path : "/" + path);
        }

        private static int TimeoutSec =>
            BackendConfig.Active != null ? Mathf.Max(1, BackendConfig.Active.requestTimeoutSec) : 5;

        public static Awaitable<ApiResult<T>> GetAsync<T>(string path, string token = null)
            => SendAsync<T>("GET", path, null, token);

        public static Awaitable<ApiResult<TRes>> PostAsync<TRes>(string path, object body, string token = null)
            => SendAsync<TRes>("POST", path, body, token);

        public static Awaitable<ApiResult<TRes>> PutAsync<TRes>(string path, object body, string token = null)
            => SendAsync<TRes>("PUT", path, body, token);

        private static async Awaitable<ApiResult<T>> SendAsync<T>(string method, string path, object body, string token)
        {
            var result = new ApiResult<T> { ok = false, status = 0 };
            UnityWebRequest req = null;
            try
            {
                req = new UnityWebRequest(Url(path), method);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.timeout = TimeoutSec;

                if (body != null)
                {
                    string json = JsonConvert.SerializeObject(body);
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    req.SetRequestHeader("Content-Type", "application/json");
                }
                if (!string.IsNullOrEmpty(token))
                    req.SetRequestHeader("Authorization", "Bearer " + token);

                var op = req.SendWebRequest();
                while (!op.isDone)
                    await Awaitable.NextFrameAsync();

                result.status = req.responseCode;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string text = req.downloadHandler != null ? req.downloadHandler.text : null;
                    result.data = string.IsNullOrEmpty(text) ? default : JsonConvert.DeserializeObject<T>(text);
                    result.ok = true;
                }
                else
                {
                    result.error = $"{req.result}: {req.error} (code {req.responseCode})";
                    Debug.LogWarning($"[ApiClient] {method} {path} lỗi -> {result.error}");
                }
            }
            catch (System.Exception e)
            {
                result.error = e.Message;
                Debug.LogWarning($"[ApiClient] {method} {path} exception -> {e.Message}");
            }
            finally
            {
                req?.Dispose();
            }
            return result;
        }
    }
}
