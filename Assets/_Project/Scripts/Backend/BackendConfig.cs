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

        [Tooltip("Trang đăng ký tài khoản web mở bằng trình duyệt ngoài. Chỉ chấp nhận HTTPS ở màn Login.")]
        public string registrationUrl = "";

        [Tooltip("Bật browser SSO: game mở website, chờ callback một lần và không nhận mật khẩu web.")]
        public bool browserAuthEnabled = false;

        [Tooltip("Bật form nhập tài khoản/mật khẩu NGAY TRONG GAME. Tắt (mặc định) -> màn Login chỉ còn " +
                 "2 nút mở website, mọi tài khoản (kể cả tài khoản demo cấp sẵn) đều đăng nhập qua web.")]
        public bool localAuthEnabled = false;

        [Tooltip("Hiện link \"Đăng nhập tài khoản demo\" (R1..R5) ở màn Login. Tắt (mặc định) -> người " +
                 "chơi thường không thấy lối vào này. Chỉ bật khi cần trình diễn bằng tài khoản cấp sẵn.")]
        public bool demoLoginLinkEnabled = false;

        [Tooltip("Thời gian tối đa chờ người dùng hoàn tất đăng nhập/đăng ký trên website.")]
        public int browserAuthTimeoutSec = 600;

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
