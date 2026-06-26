using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Gắn lên prefab HÀNG RÀO (root có các cạnh con, vd N/S/E/W).
    /// Khi 2 hàng rào kề nhau (trực giao) -> TẮT cạnh tiếp giáp ở cả hai để nối liền (kiểu Minecraft).
    ///
    /// Chọn cạnh để tắt theo VỊ TRÍ THỰC của cạnh (cạnh nào nằm về phía hàng xóm),
    /// KHÔNG phụ thuộc tên -> đúng kể cả khi đặt tên/hướng model lệch.
    /// </summary>
    public class FenceAutoConnect : MonoBehaviour
    {
        [Header("Tên các cạnh con (mặc định N/S/E/W). Bao nhiêu cạnh cũng được.")]
        public string[] edgeNames = { "N", "S", "E", "W" };

        [Header("Bán kính nối (× ô lưới)")]
        public float connectFactor = 1.25f;

        [Header("Bật log chẩn đoán")]
        public bool debugLog = false;

        private readonly List<Transform> edges = new List<Transform>();
        private Vector3[] edgeCenterSnapshot;   // tâm mỗi cạnh, chụp lúc tất cả cạnh đang bật
        private Vector3 selfCenterSnapshot;
        private float cell = 2f;

        private static readonly List<FenceAutoConnect> All = new List<FenceAutoConnect>();

        private void Awake() => FindEdges();

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            if (BuildGridManager.Instance != null) cell = BuildGridManager.Instance.CellSize;
            // Đợi 1 frame: lúc vừa Instantiate, vị trí thật được gán SAU OnEnable.
            // Nếu refresh ngay sẽ tính khi hàng rào còn ở (0,0,0) -> sai.
            StartCoroutine(RefreshNextFrame());
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            RefreshAll();
        }

        private void OnDisable()
        {
            All.Remove(this);
            RefreshAll();
        }

        private void FindEdges()
        {
            edges.Clear();
            foreach (var n in edgeNames)
            {
                var t = transform.Find(n);
                if (t != null) edges.Add(t);
            }
        }

        private void SetAllEdges(bool on)
        {
            foreach (var e in edges) if (e != null) e.gameObject.SetActive(on);
        }

        private static Vector3 BoundsCenter(Transform t)
        {
            var rends = t.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return t.position;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b.center;
        }

        // Chụp tâm bản thân + tâm từng cạnh (gọi lúc TẤT CẢ cạnh đang bật).
        private void Snapshot()
        {
            selfCenterSnapshot = BoundsCenter(transform);
            edgeCenterSnapshot = new Vector3[edges.Count];
            for (int i = 0; i < edges.Count; i++)
                edgeCenterSnapshot[i] = edges[i] != null ? BoundsCenter(edges[i]) : selfCenterSnapshot;
        }

        // Tắt cạnh nằm về phía 'dir' (hướng tới hàng xóm, đã chuẩn hóa XZ).
        private void TurnOffEdgeToward(Vector3 dir)
        {
            int best = -1;
            float bestDot = 0.3f; // ngưỡng: cạnh phải lệch rõ về phía đó
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i] == null) continue;
                Vector3 ev = edgeCenterSnapshot[i] - selfCenterSnapshot; ev.y = 0;
                if (ev.sqrMagnitude < 0.0001f) continue;
                float dot = Vector3.Dot(ev.normalized, dir);
                if (dot > bestDot) { bestDot = dot; best = i; }
            }
            if (best >= 0)
            {
                edges[best].gameObject.SetActive(false);
                if (debugLog) Debug.Log($"[Fence] '{name}' tắt cạnh '{edges[best].name}' (hướng {dir}, dot={bestDot:F2})");
            }
        }

        private static void RefreshAll()
        {
            for (int i = All.Count - 1; i >= 0; i--) if (All[i] == null) All.RemoveAt(i);

            // 1. Bật hết + chụp tâm (lúc đủ cạnh) cho từng hàng rào.
            foreach (var f in All) { f.SetAllEdges(true); }
            foreach (var f in All) { f.Snapshot(); }

            // 2. Xét từng cặp kề nhau (trực giao) -> tắt cạnh giáp 2 bên.
            int n = All.Count;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    Vector3 d = All[j].selfCenterSnapshot - All[i].selfCenterSnapshot; d.y = 0;
                    float dist = d.magnitude;
                    float limit = All[i].cell * All[i].connectFactor;
                    bool adjacent = dist > 0.1f && dist <= limit;

                    if (All[i].debugLog)
                        Debug.Log($"[Fence] cặp '{All[i].name}'-'{All[j].name}': dist={dist:F2}, limit={limit:F2} (cell={All[i].cell:F2}) -> {(adjacent ? "KỀ NHAU" : "xa")}");

                    if (adjacent)
                    {
                        Vector3 dir = d.normalized;
                        All[i].TurnOffEdgeToward(dir);
                        All[j].TurnOffEdgeToward(-dir);
                    }
                }
            }
        }
    }
}
