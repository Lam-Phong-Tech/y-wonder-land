using UnityEngine;

namespace YWonderLand.Backend
{
    /// <summary>
    /// Cấu hình kết nối backend (URL server, timeout). Tạo asset:
    /// Project -> Create -> YWonderLand -> Backend Config, đặt trong Resources/ tên "BackendConfig"
    /// để client tự nạp. Nếu KHÔNG có asset, client dùng giá trị mặc định (localhost:3000).
    /// </summary>
    [CreateAssetMenu(fileName = "BackendConfig", menuName = "YWonderLand/Backend Config")]
    public class BackendConfig : ScriptableObject
    {
        [Tooltip("Địa chỉ gốc của server REST (không có dấu / ở cuối). Dev mặc định: http://localhost:3000")]
        public string baseUrl = "http://localhost:3000";

        [Tooltip("Thời gian chờ tối đa cho 1 request (giây). Hết giờ -> coi như offline.")]
        public int requestTimeoutSec = 5;

        [Tooltip("Cho phép chạy offline (fallback dữ liệu local) khi không kết nối được server.")]
        public bool useOfflineFallback = true;

        private static BackendConfig _active;

        /// <summary>Lấy config đang dùng. Tự nạp từ Resources/BackendConfig, thiếu thì tạo mặc định.</summary>
        public static BackendConfig Active
        {
            get
            {
                if (_active != null) return _active;
                _active = Resources.Load<BackendConfig>("BackendConfig");
                if (_active == null)
                {
                    _active = CreateInstance<BackendConfig>();
                    Debug.Log("[Backend] Không tìm thấy Resources/BackendConfig -> dùng mặc định " + _active.baseUrl);
                }
                return _active;
            }
        }
    }
}
