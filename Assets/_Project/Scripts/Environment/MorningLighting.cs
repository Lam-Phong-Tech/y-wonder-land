using UnityEngine;

/// <summary>
/// Áp tông ánh sáng "BUỔI SÁNG TRONG TRẺO" cho scene: Directional Light (màu/cường độ/góc) +
/// ánh sáng môi trường (ambient) + sương. Mọi số để SerializeField -> anh chỉnh trực tiếp Inspector.
///
/// Cách dùng:
///  • Tự động: KHÔNG cần làm gì — khi chạy game, nếu scene chưa có component này thì một cái
///    "MorningLighting (auto)" tự sinh ra + áp tông mặc định (đỡ quên gắn).
///  • Chỉnh tay: kéo script lên 1 GameObject (vd Directional Light) rồi chỉnh số. Chuột phải
///    component -> "Áp ngay (xem trước)" để thấy liền trong Editor (không cần Play).
///
/// Lưu ý: script chỉ chỉnh ÁNH SÁNG, KHÔNG đổi ảnh nền trời (skybox). Nếu bầu trời trông tối/lệch
/// tông, anh gán một skybox material ban ngày ở Window > Rendering > Lighting > Environment.
/// </summary>
public class MorningLighting : MonoBehaviour
{
    [Header("Mặt trời (Directional Light)")]
    [Tooltip("Đèn mặt trời cần chỉnh. Bỏ trống = tự tìm RenderSettings.sun hoặc Directional Light đầu tiên.")]
    public Light sun;
    [Tooltip("Màu nắng — trắng ấm nhẹ cho buổi sáng trong trẻo.")]
    public Color sunColor = new Color(1.0f, 0.957f, 0.886f);
    [Tooltip("Cường độ nắng (sáng rõ ~1.2–1.5).")]
    public float sunIntensity = 1.3f;
    [Tooltip("BẬT để đặt góc mặt trời buổi sáng. TẮT nếu muốn giữ nguyên góc đèn anh đã căn.")]
    public bool applySunAngle = true;
    [Tooltip("Góc ngẩng mặt trời (độ). Buổi sáng ~45–55 (không thấp như hoàng hôn).")]
    public float sunPitch = 50f;
    [Tooltip("Hướng mặt trời quanh trục dọc (độ).")]
    public float sunYaw = 30f;

    [Header("Ánh sáng môi trường (Ambient)")]
    [Tooltip("Lấp bóng cho sáng đều — xanh nhạt sớm mai.")]
    public Color ambientColor = new Color(0.60f, 0.64f, 0.68f);

    [Header("Sương (Fog) — tuỳ chọn")]
    [Tooltip("Buổi sáng trong trẻo thường TẮT sương; bật nếu muốn chút chiều sâu.")]
    public bool enableFog = false;
    public Color fogColor = new Color(0.82f, 0.88f, 0.94f);
    public float fogDensity = 0.0035f;

    private void Start() => Apply();

    [ContextMenu("Áp ngay (xem trước)")]
    public void Apply()
    {
        Light s = sun != null ? sun : RenderSettings.sun;
        if (s == null)
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { s = l; break; }
        }
        if (s != null)
        {
            s.color = sunColor;
            s.intensity = sunIntensity;
            if (applySunAngle) s.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0f);
        }

        // Ambient phải ở chế độ Flat thì màu mình đặt mới ăn (nếu đang Skybox sẽ bỏ qua ambientLight).
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;

        RenderSettings.fog = enableFog;
        if (enableFog)
        {
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
        }
        Debug.Log("[MorningLighting] Đã áp tông buổi sáng trong trẻo.");
    }

    // ĐÃ TẮT auto-install: anh muốn CHỈNH SÁNG BẰNG TAY (Directional Light + Lighting window) nên
    // script KHÔNG tự sinh + KHÔNG tự áp khi Play -> cài đặt tay của anh không bị ghi đè.
    // Vẫn dùng được nếu muốn: kéo component này lên 1 GameObject rồi bấm "Áp ngay (xem trước)".
    // Muốn bật lại auto thì bỏ comment khối dưới.
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // private static void AutoInstall()
    // {
    //     if (FindFirstObjectByType<MorningLighting>() != null) return;
    //     var go = new GameObject("MorningLighting (auto)");
    //     go.AddComponent<MorningLighting>();
    // }
}
