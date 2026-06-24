using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Vẽ 1 Ô VUÔNG viền quanh mỗi FarmTile để người chơi dễ nhận ra "đây là nơi gieo trồng".
    /// Màu đổi theo trạng thái ô đất. Là script ĐỘC LẬP (không sửa FarmTile.cs — module QC) và
    /// TỰ gắn vào mọi FarmTile lúc chạy (kể cả ô sinh runtime) qua installer bên dưới.
    /// </summary>
    public class FarmTileMarker : MonoBehaviour
    {
        private FarmTile _tile;
        private LineRenderer _line;
        private Material _mat;
        private FarmTile.TileState _lastState = (FarmTile.TileState)(-1);

        // Màu theo trạng thái
        private static readonly Color C_SOIL = new Color(1f, 1f, 1f, 0.55f);     // chưa cuốc — trắng mờ
        private static readonly Color C_PLOWED = new Color(1f, 0.85f, 0.1f);     // đã cuốc, GIEO ĐƯỢC — vàng nổi
        private static readonly Color C_GROWING = new Color(0.3f, 0.9f, 0.4f);   // đang lớn — xanh lá
        private static readonly Color C_RIPE = new Color(1f, 0.5f, 0.1f);        // chín, THU HOẠCH — cam

        private void Start()
        {
            _tile = GetComponent<FarmTile>();
            if (_tile == null) { Destroy(this); return; }
            BuildBorder();
        }

        private void BuildBorder()
        {
            var go = new GameObject("TileMarkerBorder");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            _line = go.AddComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.loop = true;
            _line.numCornerVertices = 2;
            _line.startWidth = _line.endWidth = 0.07f;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.alignment = LineAlignment.View;

            // Ô vuông cạnh ~2 (khớp khối đất scale 2), nhô nhẹ trên mặt đất tránh z-fighting.
            const float e = 1.0f, h = 0.06f;
            _line.positionCount = 4;
            _line.SetPosition(0, new Vector3(-e, h, -e));
            _line.SetPosition(1, new Vector3(e, h, -e));
            _line.SetPosition(2, new Vector3(e, h, e));
            _line.SetPosition(3, new Vector3(-e, h, e));

            // Unlit URP để không bị ánh sáng/bóng làm mờ; tô màu qua _BaseColor.
            _mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _line.material = _mat;
        }

        private void Update()
        {
            if (_tile == null) return;
            if (_tile.currentState != _lastState)
            {
                _lastState = _tile.currentState;
                ApplyColor(_lastState);
            }
        }

        private void ApplyColor(FarmTile.TileState s)
        {
            Color c;
            switch (s)
            {
                case FarmTile.TileState.Plowed: c = C_PLOWED; break;
                case FarmTile.TileState.Planted:
                case FarmTile.TileState.Watered: c = C_GROWING; break;
                case FarmTile.TileState.Ripe: c = C_RIPE; break;
                default: c = C_SOIL; break;
            }
            if (_mat != null) _mat.SetColor("_BaseColor", c);
            if (_line != null) { _line.startColor = c; _line.endColor = c; }
        }
    }

    /// <summary>
    /// Tự quét scene và gắn FarmTileMarker vào mọi FarmTile chưa có (kể cả ô spawn runtime).
    /// Sinh tự động khi game chạy — không cần kéo thả gì trong Editor.
    /// </summary>
    public class FarmTileMarkerInstaller : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("[FarmTileMarkerInstaller]");
            DontDestroyOnLoad(go);
            go.AddComponent<FarmTileMarkerInstaller>();
        }

        private void Start() => InvokeRepeating(nameof(Scan), 1f, 2f);

        private void Scan()
        {
            var tiles = FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
            foreach (var t in tiles)
                if (t.GetComponent<FarmTileMarker>() == null)
                    t.gameObject.AddComponent<FarmTileMarker>();
        }
    }
}
