using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using YWonderLand.Environment;

/// <summary>
/// Tiện ích Editor: gắn BuildSurfaceCell vào các khối cube đất — KHÔNG cần gắn tay từng khối.
///
/// Map "cả nghìn khối tên giống nhau, không nhóm, đảo méo mó" → dùng kiểu "SƠN" theo VÙNG:
///   - Đặt NHIỀU BoxCollider nhỏ ướm theo hình đảo (vùng buildable), chọn HẾT rồi "Gắn theo VÙNG"
///     → tool tag mọi khối "cube*" nằm trong BẤT KỲ hộp nào (gộp vùng).
///   - Lỡ lấn sang chỗ không muốn? Đặt hộp ở chỗ đó, chọn, "Gỡ theo VÙNG" để tẩy.
///
/// Map có nhóm cha gọn → dùng "Gắn đệ quy". Prefix khối đất = "cube" (grass); "stone" bị bỏ qua.
/// </summary>
public static class BuildSurfaceCellSetup
{
    private const string BuildablePrefix = "cube"; // khối cỏ buildable; đổi nếu cần

    private const string MenuAddRegion = "YWonder/Build/Gắn BuildSurfaceCell theo VÙNG (các BoxCollider đang chọn)";
    private const string MenuRemoveRegion = "YWonder/Build/Gỡ BuildSurfaceCell theo VÙNG (các BoxCollider đang chọn)";
    private const string MenuRecursive = "YWonder/Build/Gắn BuildSurfaceCell đệ quy (object đang chọn + con)";
    private const string MenuRemoveAll = "YWonder/Build/Gỡ TẤT CẢ BuildSurfaceCell trong scene";

    // ── SƠN: gắn cho khối nằm trong BẤT KỲ hộp nào đang chọn (gộp nhiều vùng) ──
    [MenuItem(MenuAddRegion)]
    private static void AddInRegion()
    {
        var zones = GetZoneBounds(out var zoneSet);
        if (zones.Count == 0)
        {
            Debug.LogWarning("[BuildSurfaceCell] Chọn 1+ object có BoxCollider/Renderer (các hộp ướm vùng buildable) rồi chạy lại.");
            return;
        }

        int cells = 0, colliders = 0, scanned = 0;
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (mr == null) continue;
            var go = mr.gameObject;
            if (zoneSet.Contains(go)) continue; // không tag chính cái hộp
            if (!go.name.StartsWith(BuildablePrefix, System.StringComparison.OrdinalIgnoreCase)) continue;
            if (!InAnyZone(mr.bounds.center, zones)) continue;

            scanned++;
            if (go.GetComponent<Collider>() == null) { Undo.AddComponent<BoxCollider>(go); colliders++; }
            if (go.GetComponent<BuildSurfaceCell>() == null) { Undo.AddComponent<BuildSurfaceCell>(go); cells++; }
        }
        Debug.Log($"[BuildSurfaceCell] Gắn theo VÙNG: +{cells} ô (thêm {colliders} collider) — quét {scanned} khối '{BuildablePrefix}*' trong {zones.Count} hộp.");
    }

    // ── TẨY: gỡ cho khối nằm trong các hộp đang chọn ──
    [MenuItem(MenuRemoveRegion)]
    private static void RemoveInRegion()
    {
        var zones = GetZoneBounds(out var zoneSet);
        if (zones.Count == 0)
        {
            Debug.LogWarning("[BuildSurfaceCell] Chọn 1+ object hộp để tẩy vùng rồi chạy lại.");
            return;
        }

        int removed = 0;
        foreach (var cell in Object.FindObjectsByType<BuildSurfaceCell>(FindObjectsSortMode.None))
        {
            if (cell == null || zoneSet.Contains(cell.gameObject)) continue;
            if (!InAnyZone(cell.transform.position, zones)) continue;
            Undo.DestroyObjectImmediate(cell);
            removed++;
        }
        Debug.Log($"[BuildSurfaceCell] Gỡ theo VÙNG: -{removed} ô.");
    }

    // ── Đệ quy theo cha (khi map có nhóm gọn) ──
    [MenuItem(MenuRecursive)]
    private static void AddRecursive()
    {
        var roots = Selection.gameObjects;
        if (roots == null || roots.Length == 0) return;

        int cells = 0, colliders = 0;
        foreach (var root in roots)
        {
            if (root == null) continue;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf == null || mf.sharedMesh == null) continue;
                var go = mf.gameObject;
                if (go.GetComponent<Collider>() == null) { Undo.AddComponent<BoxCollider>(go); colliders++; }
                if (go.GetComponent<BuildSurfaceCell>() == null) { Undo.AddComponent<BuildSurfaceCell>(go); cells++; }
            }
        }
        Debug.Log($"[BuildSurfaceCell] Đệ quy: +{cells} ô (thêm {colliders} collider).");
    }

    [MenuItem(MenuRemoveAll)]
    private static void RemoveAll()
    {
        var cells = Object.FindObjectsByType<BuildSurfaceCell>(FindObjectsSortMode.None);
        foreach (var c in cells) if (c != null) Undo.DestroyObjectImmediate(c);
        Debug.Log($"[BuildSurfaceCell] Đã gỡ {cells.Length} BuildSurfaceCell trong scene.");
    }

    // ── Helpers ──
    private static List<Bounds> GetZoneBounds(out HashSet<GameObject> zoneSet)
    {
        zoneSet = new HashSet<GameObject>();
        var list = new List<Bounds>();
        foreach (var go in Selection.gameObjects)
        {
            if (go == null) continue;
            zoneSet.Add(go);
            if (TryGetBounds(go, out var b)) list.Add(b);
        }
        return list;
    }

    private static bool InAnyZone(Vector3 p, List<Bounds> zones)
    {
        for (int i = 0; i < zones.Count; i++)
            if (zones[i].Contains(p)) return true;
        return false;
    }

    private static bool TryGetBounds(GameObject go, out Bounds b)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) { b = col.bounds; return true; }
        var r = go.GetComponent<Renderer>();
        if (r != null) { b = r.bounds; return true; }
        b = default;
        return false;
    }

    [MenuItem(MenuAddRegion, validate = true)]
    [MenuItem(MenuRemoveRegion, validate = true)]
    [MenuItem(MenuRecursive, validate = true)]
    private static bool ValidateSelection() => Selection.gameObjects != null && Selection.gameObjects.Length > 0;
}
