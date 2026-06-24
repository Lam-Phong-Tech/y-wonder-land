using UnityEngine;
using System.Collections;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Dây câu + phao khi câu cá. Gắn lên NHÂN VẬT.
    /// Khi câu: phao bay ra mặt nước theo vòng cung, dây câu (LineRenderer) nối
    /// ngọn cần -> phao và nhấp nhô. Khi xong: thu dây + phao về.
    ///
    /// SETUP: Tạo 1 GameObject rỗng ở ĐẦU cần câu (con của model cần) đặt tên "RodTip",
    /// kéo vào ô Rod Tip. Line + Bobber để trống thì script tự tạo.
    /// </summary>
    public class FishingLineController : MonoBehaviour
    {
        public static FishingLineController Instance { get; private set; }

        [Header("Tham chiếu")]
        [Tooltip("Điểm NGỌN cần câu (tạo empty 'RodTip' ở đầu cần, kéo vào đây)")]
        public Transform rodTip;
        [Tooltip("LineRenderer vẽ dây câu (để trống = tự tạo)")]
        public LineRenderer line;
        [Tooltip("Phao câu (để trống = tự tạo quả cầu đỏ nhỏ)")]
        public Transform bobber;

        [Header("Thông số ném")]
        public float castDistance = 6f;    // ném xa bao nhiêu (nếu không truyền điểm đích)
        public float castDuration = 0.6f;  // thời gian phao bay ra
        public float arcHeight = 2f;       // độ cao vòng cung

        [Tooltip("Sau khi bắt đầu câu BAO LÂU thì bung dây (giây). ~2s = frame 62 ở 30fps. Chỉnh cho khớp lúc vung cần.")]
        public float castDelay = 2.0f;

        [Tooltip("Sau khi bắt đầu câu BAO LÂU thì THU dây về (giây). ~5.27s = frame 158 ở 30fps.")]
        public float reelDelay = 5.27f;

        [Tooltip("Thời gian thu dây (phao bay ngược về ngọn cần). ~3.13s = thu từ frame 158 -> 252.")]
        public float reelDuration = 3.13f;

        private Coroutine castRoutine;
        private Coroutine delayRoutine;
        private bool active;

        // Cast trì hoãn — đợi castDelay giây rồi bung (hoặc Animation Event gọi FireCast sớm hơn)
        private float pendingSurfaceY;
        private bool hasPendingCast;

        private void Awake()
        {
            Instance = this;
            if (line == null) line = CreateLine();
            if (bobber == null) bobber = CreateBobber();
            HideLine();
        }

        /// <summary>Ném phao tới 1 điểm cụ thể (vd vị trí vùng nước).</summary>
        public void Cast(Vector3 waterPoint)
        {
            if (rodTip == null)
            {
                Debug.LogWarning("[FishingLine] Chưa gán RodTip (ngọn cần) -> không vẽ được dây.");
                return;
            }
            if (castRoutine != null) StopCoroutine(castRoutine);
            castRoutine = StartCoroutine(CastRoutine(waterPoint));
        }

        /// <summary>Ném phao ra trước mặt nhân vật (tự dò mặt nước/đất bên dưới).</summary>
        public void Cast()
        {
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 target = transform.position + fwd * castDistance;
            if (Physics.Raycast(target + Vector3.up * 5f, Vector3.down, out var hit, 20f))
                target = hit.point;
            Cast(target);
        }

        /// <summary>
        /// Ném phao ra TRƯỚC MẶT nhân vật (castDistance mét) ở cao độ mặt nước cho trước.
        /// Dùng khi câu cá -> phao luôn rơi đúng trước mặt, không bị văng ra tâm FishingSpot lệch xa.
        /// </summary>
        public void CastForward(float surfaceY)
        {
            Vector3 fwd = transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f) fwd.Normalize();
            Vector3 target = transform.position + fwd * castDistance;
            target.y = surfaceY;
            Cast(target);
        }

        /// <summary>Lưu sẵn cao độ mặt nước, CHỜ Animation Event gọi FireCast() để bắn dây.</summary>
        public void PrepareCast(float surfaceY)
        {
            pendingSurfaceY = surfaceY;
            hasPendingCast = true;
            // Tự hẹn giờ bung dây sau castDelay giây (khỏi cần Animation Event)
            if (delayRoutine != null) StopCoroutine(delayRoutine);
            delayRoutine = StartCoroutine(DelayedFire());
            Debug.Log($"[Fishing] ✅ PrepareCast — sẽ bung dây sau {castDelay:F2}s (mặt nước y={surfaceY:F2}).");
        }

        private System.Collections.IEnumerator DelayedFire()
        {
            yield return new WaitForSeconds(castDelay);
            FireCast();
        }

        /// <summary>Bung dây ra. Tự gọi sau castDelay, HOẶC Animation Event gọi sớm hơn.</summary>
        public void FireCast()
        {
            if (!hasPendingCast) return; // đã bung rồi thì thôi
            hasPendingCast = false;
            Debug.Log("[Fishing] 🎣 FireCast — BUNG DÂY!");
            CastForward(pendingSurfaceY);
        }

        /// <summary>Thu dây + phao về (gọi khi kết thúc/hủy câu).</summary>
        public void Reel()
        {
            if (castRoutine != null) StopCoroutine(castRoutine);
            if (delayRoutine != null) StopCoroutine(delayRoutine);
            active = false;
            hasPendingCast = false;
            HideLine();
        }

        private IEnumerator CastRoutine(Vector3 target)
        {
            active = true;
            ShowLine();

            // 1. Phao bay ra theo vòng cung
            Vector3 start = rodTip.position;
            float t = 0f;
            while (t < castDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / castDuration);
                Vector3 pos = Vector3.Lerp(start, target, k);
                pos.y += Mathf.Sin(k * Mathf.PI) * arcHeight; // nhô lên giữa đường
                bobber.position = pos;
                yield return null;
            }
            bobber.position = target;

            // 2. Phao nổi nhấp nhô chờ (tới lúc nhân vật thu cần)
            float baseY = target.y;
            float floatDuration = Mathf.Max(0f, reelDelay - castDelay - castDuration);
            float ft = 0f;
            while (active && ft < floatDuration)
            {
                ft += Time.deltaTime;
                Vector3 p = bobber.position;
                p.y = baseY + Mathf.Sin(Time.time * 3f) * 0.05f;
                bobber.position = p;
                yield return null;
            }

            // 3. THU DÂY: phao bay ngược về ngọn cần rồi ẩn
            Vector3 from = bobber.position;
            float rt = 0f;
            while (active && rt < reelDuration)
            {
                rt += Time.deltaTime;
                bobber.position = Vector3.Lerp(from, rodTip.position, Mathf.Clamp01(rt / reelDuration));
                yield return null;
            }
            Debug.Log("[Fishing] 🪝 Thu dây xong -> ẩn dây + phao.");
            active = false;
            HideLine();
        }

        private void LateUpdate()
        {
            // Vẽ dây nối ngọn cần -> phao mỗi frame (tay/cần luôn động nên phải cập nhật liên tục)
            if (active && line != null && rodTip != null && bobber != null)
            {
                line.SetPosition(0, rodTip.position);
                line.SetPosition(1, bobber.position);
            }
        }

        // ── Tự tạo dây + phao mặc định ──
        private LineRenderer CreateLine()
        {
            var go = new GameObject("FishingLine");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.02f;
            lr.numCapVertices = 2;
            lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lr.startColor = lr.endColor = new Color(1f, 1f, 1f, 0.75f);
            return lr;
        }

        private Transform CreateBobber()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.name = "Bobber";
            go.transform.localScale = Vector3.one * 0.12f;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                r.material.color = Color.red;
            }
            return go.transform;
        }

        private void ShowLine()
        {
            if (line != null) line.enabled = true;
            if (bobber != null) bobber.gameObject.SetActive(true);
        }

        private void HideLine()
        {
            if (line != null) line.enabled = false;
            if (bobber != null) bobber.gameObject.SetActive(false);
        }
    }
}
