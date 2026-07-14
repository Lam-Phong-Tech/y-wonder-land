using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YWonderLand.Backend
{
    /// <summary>Kết quả 1 lời gọi API. ok=false nghĩa là lỗi mạng/server (đọc error/status).</summary>
    public struct ApiResult<T>
    {
        public bool ok;
        public T data;
        public long status;   // HTTP status code (0 nếu không kết nối được)
        public string error;
        public string errorCode;
        public int retryAfterSec;
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
                string responseBody = req.downloadHandler != null ? req.downloadHandler.text : "";
                result.retryAfterSec = ExtractRetryAfterSec(req, responseBody);

                if (req.result == UnityWebRequest.Result.Success)
                {
                    result.data = string.IsNullOrEmpty(responseBody)
                        ? default
                        : JsonConvert.DeserializeObject<T>(responseBody);
                    result.ok = true;
                }
                else
                {
                    if (!string.IsNullOrEmpty(responseBody))
                    {
                        try { result.data = JsonConvert.DeserializeObject<T>(responseBody); }
                        catch { result.data = default; }
                    }
                    result.errorCode = ExtractErrorCode(responseBody);
                    string detail = !string.IsNullOrEmpty(result.errorCode) ? result.errorCode : req.error;
                    result.error = $"{req.result}: {detail} (code {req.responseCode})";
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

        private static string ExtractErrorCode(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody)) return "";
            try
            {
                var payload = JObject.Parse(responseBody);
                return payload.Value<string>("code") ?? payload.Value<string>("error") ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static int ExtractRetryAfterSec(UnityWebRequest request, string responseBody)
        {
            string header = request != null ? request.GetResponseHeader("Retry-After") : "";
            if (int.TryParse(header, out int headerValue) && headerValue > 0)
                return headerValue;

            if (string.IsNullOrWhiteSpace(responseBody)) return 0;
            try
            {
                var payload = JObject.Parse(responseBody);
                return Mathf.Max(0, payload.Value<int?>("retryAfterSec") ?? 0);
            }
            catch
            {
                return 0;
            }
        }
    }
}
