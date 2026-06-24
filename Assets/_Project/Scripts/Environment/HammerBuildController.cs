using UnityEngine;
using UnityEngine.InputSystem;
using YWonderLand.Managers;
using YWonderLand.Player;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Chế độ CẦM BÚA lát ô (kiểu Minecraft): bật bằng phím (tạm G — sẽ nối nút HUD ở Bước 2).
    /// Khi bật: cầm búa + hiện ô preview sáng mờ ngay trước mũi chân. Gõ (chuột trái) = lát 1 ô,
    /// tốn 4 đá + 4 gỗ. Ô lát ra đa dụng (trồng được + tính vào số ô xây chuồng).
    /// KHÔNG đụng Build Mode cũ (sẽ tắt ở Bước 2).
    /// </summary>
    public class HammerBuildController : MonoBehaviour
    {
        [Header("Phím & chi phí (tạm)")]
        public Key toggleKey = Key.G;       // sẽ thay bằng nút HUD ở Bước 2
        public string stoneId = "stone_01";
        public string woodId = "wood_01";
        public int stoneCost = 4;
        public int woodCost = 4;

        private bool _active;
        private GameObject _preview;
        private Material _previewMat;
        private Camera _cam;

        private static readonly Color OK_COLOR = new Color(1f, 1f, 1f, 0.35f);   // trắng mờ — đặt được
        private static readonly Color BAD_COLOR = new Color(1f, 0.2f, 0.2f, 0.35f); // đỏ mờ — không đặt được

        private void Start()
        {
            _cam = Camera.main;
            BuildPreview();
            SetActive(false);
        }

        private void BuildPreview()
        {
            _preview = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _preview.name = "HammerTilePreview";
            var col = _preview.GetComponent<Collider>();
            if (col != null) Destroy(col);
            _preview.transform.SetParent(transform, false);
            _preview.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // nằm phẳng trên mặt đất

            _previewMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            // Bật chế độ trong suốt cho URP Unlit
            _previewMat.SetFloat("_Surface", 1f);
            _previewMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _previewMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _previewMat.SetInt("_ZWrite", 0);
            _previewMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _previewMat.renderQueue = 3000;
            _previewMat.SetColor("_BaseColor", OK_COLOR);
            _preview.GetComponent<Renderer>().material = _previewMat;
        }

        private void SetActive(bool on)
        {
            _active = on;
            if (_preview != null) _preview.SetActive(on);

            var equip = EquipmentManager.Instance;
            if (equip != null)
            {
                if (on) equip.ShowTool(ToolType.Hammer);
                else equip.HideAllTools();
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[toggleKey].wasPressedThisFrame)
            {
                bool typing = ChatPanelController.Instance != null && ChatPanelController.Instance.IsTyping();
                if (!typing) SetActive(!_active);
            }

            if (!_active) return;
            if (PlayerController.Instance == null || TilePlacementSystem.Instance == null) return;

            // Giữ búa hiện (PlayActionAnimation có thể ẩn đồ sau khi gõ xong)
            var equip = EquipmentManager.Instance;
            if (equip != null && equip.hammerModel != null && !equip.hammerModel.activeSelf)
                equip.ShowTool(ToolType.Hammer);

            // Ô ngay trước mũi chân theo hướng nhân vật
            Transform p = PlayerController.Instance.transform;
            float cell = TilePlacementSystem.Instance.cellSize;
            Vector3 front = p.position + p.forward * cell;
            Vector2Int targetCell = TilePlacementSystem.Instance.WorldToCell(front);
            Vector3 center = TilePlacementSystem.Instance.CellToWorldCenter(targetCell, p.position.y + 0.07f);

            _preview.transform.position = center;
            _preview.transform.localScale = new Vector3(cell * 0.95f, cell * 0.95f, 1f);

            bool canPlace = !TilePlacementSystem.Instance.HasTile(targetCell) && HasEnoughResources();
            _previewMat.SetColor("_BaseColor", canPlace ? OK_COLOR : BAD_COLOR);

            // Gõ
            var mouse = Mouse.current;
            var es = UnityEngine.EventSystems.EventSystem.current;
            bool overUI = es != null && es.IsPointerOverGameObject();
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                TryPlace(targetCell, p.position.y);
            }
        }

        private bool HasEnoughResources()
        {
            var inv = InventoryManager.Instance;
            if (inv == null) return true; // không có inventory thì cho gõ thoải mái (demo)
            return inv.GetItemQuantity(stoneId) >= stoneCost && inv.GetItemQuantity(woodId) >= woodCost;
        }

        private void TryPlace(Vector2Int cell, float groundY)
        {
            if (TilePlacementSystem.Instance.HasTile(cell)) return;

            var inv = InventoryManager.Instance;
            if (inv != null)
            {
                if (!HasEnoughResources())
                {
                    Debug.Log($"[Hammer] Thiếu tài nguyên (cần {stoneCost} đá + {woodCost} gỗ).");
                    return;
                }
                inv.RemoveItem(stoneId, stoneCost);
                inv.RemoveItem(woodId, woodCost);
            }

            TilePlacementSystem.Instance.PlaceTile(cell, groundY);

            // Múa động tác gõ + cầm búa
            PlayerController.Instance.PlayActionAnimation("TreeCuttingV4", 0.5f, ToolType.Hammer);
            Debug.Log($"[Hammer] Lát ô tại {cell}. Tổng ô: {TilePlacementSystem.Instance.TileCount}");
        }
    }

    /// <summary>Tự sinh TilePlacementSystem + HammerBuildController khi game chạy (khỏi kéo thả).</summary>
    public class HammerBuildInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("[HammerBuild]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<TilePlacementSystem>();
            // go.AddComponent<HammerBuildController>(); // Đã vô hiệu hóa theo yêu cầu
        }
    }
}
