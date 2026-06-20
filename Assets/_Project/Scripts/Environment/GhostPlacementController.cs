using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ghost preview khi Build Mode. Ưu tiên hiển thị BẢN MỜ của chính prefab thật
/// (kiểu ROK/Hay Day) — kéo thấy luôn hình công trình mờ xanh (đặt được) / đỏ (không),
/// dễ căn vị trí. Item chưa khai báo prefab thì fallback khối Cube như cũ.
/// Vị trí/xoay/scale của ghost = y hệt lúc đặt thật (WYSIWYG): đặt = clone ghost ra.
/// </summary>
public class GhostPlacementController : MonoBehaviour
{
    public static GhostPlacementController Instance { get; private set; }

    /// <summary>Bắn khi đặt 1 công trình (tham số: tên item + giá).</summary>
    public static event System.Action<string, int> OnBuildingPlaced;

    [Header("Ghost Settings")]
    [SerializeField] private Color validColor = new Color(0.3f, 1f, 0.4f, 0.45f);   // xanh lá mờ — đặt được
    [SerializeField] private Color invalidColor = new Color(1f, 0.25f, 0.25f, 0.45f); // đỏ mờ — không đặt được
    [SerializeField] private float ghostHeight = 0.6f; // chỉ dùng cho fallback Cube

    [Tooltip("Cạnh ô đất xây dựng (khối cube map = 0.8). Dùng co giãn hàng rào cho khít khối khi prefab cần stretch.")]
    [SerializeField] private float surfaceCellSize = 0.8f;

    [Header("Raycast")]
    [SerializeField] private LayerMask groundMask = ~0;

    // Ghost object
    private GameObject ghostObject;
    private bool ghostIsPrefab = false;
    private readonly List<Material> ghostMats = new List<Material>(); // các material mờ để đổi màu xanh/đỏ

    // Current placement state
    private bool isActive = false;
    private Vector2Int currentItemSize = new Vector2Int(1, 1);
    private string currentItemName = "";
    private int currentItemPrice = 0;
    private int rotationAngle = 0;
    private bool currentPlacementValid = false;
    private float currentGroundY = 0f;
    private YWonderLand.Environment.BuildPrefabLibrary.Entry currentEntry;

    // Surface-cell snapping: ô đất (cube) đang ngắm + vị trí snap (tâm mặt trên ô).
    private YWonderLand.Environment.BuildSurfaceCell currentCell;
    private Vector3 currentSnapPos;

    public bool IsPinned { get; private set; } = false;
    public Vector3 GhostPosition => ghostObject != null ? ghostObject.transform.position : Vector3.zero;

    private Camera mainCamera;
    private BuildGridManager gridManager;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        mainCamera = Camera.main;
        gridManager = BuildGridManager.Instance;
    }

    void Update()
    {
        if (!isActive || ghostObject == null) return;
        if (IsPinned) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));

        var hits = Physics.RaycastAll(ray, 200f, groundMask);
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // Tìm Ô ĐẤT (BuildSurfaceCell) GẦN NHẤT — bỏ qua collider nền/mesh đảo chắn phía trước.
            currentCell = null;
            Vector3 fallback = hits[0].point;
            foreach (var h in hits)
            {
                var bsc = h.collider.GetComponentInParent<YWonderLand.Environment.BuildSurfaceCell>();
                if (bsc != null) { currentCell = bsc; break; }
            }

            currentSnapPos = (currentCell != null) ? currentCell.SurfaceCenter : fallback;
            currentGroundY = currentSnapPos.y;
            ApplyGhostTransform();

            // Chỉ đặt được khi đang trỏ vào ô đất hợp lệ và ô còn TRỐNG.
            currentPlacementValid = (currentCell != null && !currentCell.IsOccupied);
            UpdateGhostColor(currentPlacementValid);
        }

        if (mouse.rightButton.wasPressedThisFrame) Deactivate();
    }

    // ── Activation ──

    public void Activate(string itemName, Vector2Int size, int price)
    {
        currentItemName = itemName;
        currentItemSize = size;
        currentItemPrice = price;
        rotationAngle = 0;
        isActive = true;
        IsPinned = false;

        if (gridManager == null) gridManager = BuildGridManager.Instance;

        var lib = YWonderLand.Environment.BuildPrefabLibrary.Instance;
        currentEntry = lib != null ? lib.Find(itemName) : null;

        BuildGhost();
        Debug.Log($"[GhostPlacement] Activated '{itemName}' size ({size.x},{size.y}) — ghost={(ghostIsPrefab ? "PREFAB mờ" : "Cube")}");
    }

    public void Deactivate()
    {
        isActive = false;
        if (ghostObject != null) ghostObject.SetActive(false);
    }

    public bool IsActive => isActive;
    public void SetPinned(bool pinned) => IsPinned = pinned;
    public bool IsPlacementValid => currentPlacementValid;

    // ── Rotation ──

    public void Rotate()
    {
        rotationAngle = (rotationAngle + 90) % 360;
        ApplyGhostTransform();
        currentPlacementValid = (currentCell != null && !currentCell.IsOccupied);
        UpdateGhostColor(currentPlacementValid);
    }

    private Vector2Int GetEffectiveSize()
    {
        if (rotationAngle == 90 || rotationAngle == 270)
            return new Vector2Int(currentItemSize.y, currentItemSize.x);
        return currentItemSize;
    }

    // ── Tạo ghost ──

    private void BuildGhost()
    {
        // Mỗi item có thể là prefab khác nhau -> hủy ghost cũ, tạo lại.
        if (ghostObject != null) { Destroy(ghostObject); ghostObject = null; }
        ghostMats.Clear();

        if (currentEntry != null && currentEntry.prefab != null)
            BuildPrefabGhost();
        else
            BuildCubeGhost();

        ApplyGhostTransform();
        UpdateGhostColor(false);
    }

    // Ghost = bản mờ của prefab thật (bọc wrapper căn tâm để pivot lệch cũng đúng vị trí).
    private void BuildPrefabGhost()
    {
        float cell = surfaceCellSize;
        ghostObject = MakeCenteredClone(currentEntry.prefab, currentEntry.stretchToFootprint, currentItemSize, cell);
        ghostObject.name = "BuildGhost_" + currentEntry.prefab.name;
        ghostIsPrefab = true;

        // Bỏ mọi collider + script để ghost chỉ là hình ảnh (không chạy logic, không chặn tia).
        foreach (var col in ghostObject.GetComponentsInChildren<Collider>(true))
            if (col != null) Destroy(col);
        foreach (var mb in ghostObject.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb != null) { try { Destroy(mb); } catch { } }

        TintAllRenderers(ghostObject);
    }

    /// <summary>
    /// Clone prefab vào 1 wrapper, dịch để TÂM cụm mesh (XZ) về gốc wrapper.
    /// Nhờ vậy prefab có pivot lệch tâm bao nhiêu cũng hiện/đặt đúng ngay vị trí nhắm.
    /// Xoay wrapper = xoay quanh tâm cụm (đúng cảm giác kéo thả).
    /// </summary>
    private GameObject MakeCenteredClone(GameObject prefab, bool stretch, Vector2Int footprint, float cell)
    {
        GameObject wrap = new GameObject("BuildWrap_" + prefab.name);
        wrap.transform.position = Vector3.zero;
        wrap.transform.rotation = Quaternion.identity;

        GameObject inner = Instantiate(prefab, wrap.transform, false);
        inner.transform.localPosition = Vector3.zero;
        inner.transform.localRotation = prefab.transform.rotation; // giữ tư thế gốc (Blender offset)
        inner.transform.localScale = prefab.transform.localScale;

        // Stretch ĐÚNG 1 ô (100%, không chừa mép) để hàng rào/đất khít sát nhau khi đặt liền kề.
        if (stretch)
            StretchToFootprint(inner, footprint.x * cell, footprint.y * cell);

        // Căn tâm XZ: dịch inner sao cho tâm bounds trùng gốc wrapper.
        var rends = inner.GetComponentsInChildren<Renderer>(true);
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            Vector3 c = b.center; // world; wrap đang ở (0,0,0)
            inner.transform.position -= new Vector3(c.x, 0f, c.z);
        }
        return wrap;
    }

    // Fallback: khối Cube mờ như cũ (cho item chưa khai báo prefab).
    private void BuildCubeGhost()
    {
        ghostObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ghostObject.name = "BuildGhost_Cube";
        ghostIsPrefab = false;

        var col = ghostObject.GetComponent<Collider>();
        if (col != null) Destroy(col);

        TintAllRenderers(ghostObject);

        // Mũi chỉ hướng (để 1x1 thấy được khi xoay)
        GameObject front = GameObject.CreatePrimitive(PrimitiveType.Cube);
        front.name = "FrontIndicator";
        front.transform.SetParent(ghostObject.transform, false);
        front.transform.localPosition = new Vector3(0f, 0.5f, 0.5f);
        front.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);
        var fc = front.GetComponent<Collider>();
        if (fc != null) Destroy(fc);
    }

    // Phủ material mờ lên TẤT CẢ renderer của ghost (lưu lại để đổi xanh/đỏ).
    private void TintAllRenderers(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            int count = Mathf.Max(1, r.sharedMaterials.Length);
            var mats = new Material[count];
            for (int i = 0; i < count; i++)
            {
                var m = CreateGhostMaterial(validColor);
                mats[i] = m;
                ghostMats.Add(m);
            }
            r.materials = mats;
        }
    }

    private Material CreateGhostMaterial(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetFloat("_Surface", 1f); // Transparent
        m.SetFloat("_Blend", 0f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        m.color = c;
        m.SetColor("_BaseColor", c);
        return m;
    }

    // Đặt ghost đúng vị trí/xoay/scale (giống y lúc đặt thật).
    private void ApplyGhostTransform()
    {
        if (ghostObject == null) return;

        Vector3 worldPos = currentSnapPos; // tâm mặt trên ô đất đang ngắm

        if (ghostIsPrefab)
        {
            worldPos.y = currentGroundY + (currentEntry != null ? currentEntry.yOffset : 0f);
            ghostObject.transform.position = worldPos;
            // Wrapper đã giữ tư thế gốc prefab (ở inner) -> chỉ cần xoay yaw quanh tâm.
            ghostObject.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
        }
        else
        {
            worldPos.y = currentGroundY + ghostHeight * 0.5f;
            ghostObject.transform.position = worldPos;
            ghostObject.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
            // Khối fallback co theo kích thước ô đất (vd 0.8 x 0.8).
            float fw = currentCell != null ? currentCell.FootprintSize.x : surfaceCellSize;
            float fd = currentCell != null ? currentCell.FootprintSize.y : surfaceCellSize;
            ghostObject.transform.localScale = new Vector3(fw, ghostHeight, fd);
        }
    }

    private void UpdateGhostColor(bool valid)
    {
        Color c = valid ? validColor : invalidColor;
        foreach (var m in ghostMats)
        {
            if (m == null) continue;
            m.color = c;
            m.SetColor("_BaseColor", c);
        }
    }

    // ── Placement ──

    public void ConfirmPlacement()
    {
        if (ghostObject == null) return;
        // Chỉ đặt khi đang trỏ vào ô đất hợp lệ còn TRỐNG (snap theo cube, không theo lưới ảo).
        if (currentCell == null || currentCell.IsOccupied) return;

        if (ghostIsPrefab && currentEntry != null)
        {
            // WYSIWYG: clone căn tâm (GIỮ script/collider) đặt đúng vị trí/xoay của ghost.
            GameObject go = MakeCenteredClone(currentEntry.prefab, currentEntry.stretchToFootprint, currentItemSize, surfaceCellSize);
            go.transform.position = ghostObject.transform.position;
            go.transform.rotation = ghostObject.transform.rotation;
            go.name = $"Building_{currentItemName}_{currentCell.name}";
            if (go.CompareTag("Untagged")) go.tag = "PlacedBuilding";
            currentCell.SetOccupant(go); // ghi vật vào ô (để biết ô có rào/công trình)
            Debug.Log($"[GhostPlacement] Đặt PREFAB '{currentEntry.prefab.name}' (căn tâm) tại {go.transform.position} trên ô '{currentCell.name}'");
        }
        else
        {
            SpawnCubeBuilding(GetEffectiveSize());
        }

        OnBuildingPlaced?.Invoke(currentItemName, currentItemPrice);
        // Giữ ghost để đặt tiếp nhiều cái.
    }

    private void SpawnCubeBuilding(Vector2Int size)
    {
        float cell = surfaceCellSize;
        Vector3 pos = ghostObject.transform.position;
        pos.y = currentGroundY + ghostHeight * 0.75f;

        GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = $"Building_{currentItemName}_{(currentCell != null ? currentCell.name : "cell")}";
        building.transform.position = pos;
        building.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
        building.transform.localScale = new Vector3(size.x * cell * 0.95f, ghostHeight * 1.5f, size.y * cell * 0.95f);

        Renderer renderer = building.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = GetBuildingColor();
            renderer.material = mat;
        }
        building.tag = "PlacedBuilding";
        if (currentCell != null) currentCell.SetOccupant(building); // ghi vật vào ô
    }

    private Color GetBuildingColor()
    {
        int hash = currentItemName.GetHashCode();
        float hue = Mathf.Abs(hash % 360) / 360f;
        return Color.HSVToRGB(hue, 0.5f, 0.8f);
    }

    /// <summary>Co giãn theo trục X/Z (world) cho vừa footprint ô lưới, giữ chiều cao.</summary>
    private void StretchToFootprint(GameObject go, float targetX, float targetZ)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        float curX = b.size.x, curZ = b.size.z;
        if (curX < 0.0001f || curZ < 0.0001f) return;

        Vector3 s = go.transform.localScale;
        s.x *= targetX / curX;
        s.z *= targetZ / curZ;
        go.transform.localScale = s;
    }
}
