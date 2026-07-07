using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Selects the BuildSurfaceCell directly in front of the player and draws the white
    /// interaction outline used by the new character-facing build workflow.
    /// </summary>
    public sealed class FrontBuildCellSelector : MonoBehaviour
    {
        public static FrontBuildCellSelector Instance { get; private set; }

        [Header("Selection")]
        [SerializeField] private float minForwardDistance = 0.25f;
        [SerializeField] private float maxForwardDistance = 2.8f;
        [SerializeField, Range(0.1f, 2f)] private float lateralToleranceMultiplier = 0.85f;

        [Header("Outline")]
        [SerializeField] private Color outlineColor = Color.white;
        [SerializeField] private float outlineHeightOffset = 0.055f;
        [SerializeField] private float outlineInset = 0.08f;
        [SerializeField] private float outlineWidth = 0.075f;

        private Transform player;
        private LineRenderer outline;
        private Material outlineMaterial;

        public BuildSurfaceCell CurrentCell { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Instance != null) return;

            var go = new GameObject("[FrontBuildCellSelector]");
            DontDestroyOnLoad(go);
            go.AddComponent<FrontBuildCellSelector>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CreateOutline();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (outlineMaterial != null)
                Destroy(outlineMaterial);
        }

        private void LateUpdate()
        {
            RefreshPlayer();
            CurrentCell = FindCellInFront();
            UpdateOutline(CurrentCell);
        }

        private void RefreshPlayer()
        {
            if (player != null && player.gameObject.activeInHierarchy) return;

            if (PlayerController.Instance != null)
            {
                player = PlayerController.Instance.transform;
                return;
            }

            var controller = FindFirstObjectByType<PlayerController>();
            player = controller != null ? controller.transform : null;
        }

        private BuildSurfaceCell FindCellInFront()
        {
            if (player == null || BuildSurfaceCell.All.Count == 0)
                return null;

            Vector3 forward = player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return null;
            forward.Normalize();

            Vector3 origin = player.position;
            origin.y = 0f;

            BuildSurfaceCell best = null;
            float bestScore = float.MaxValue;

            foreach (var cell in BuildSurfaceCell.All)
            {
                if (cell == null || !cell.isActiveAndEnabled) continue;

                Vector3 center = cell.SurfaceCenter;
                center.y = 0f;
                Vector3 toCell = center - origin;
                float forwardDistance = Vector3.Dot(toCell, forward);
                if (forwardDistance < minForwardDistance || forwardDistance > maxForwardDistance)
                    continue;

                Vector3 lateralOffset = toCell - forward * forwardDistance;
                float lateralDistance = lateralOffset.magnitude;
                Vector2 footprint = cell.FootprintSize;
                float cellSize = Mathf.Max(Mathf.Max(footprint.x, footprint.y), 0.5f);
                float allowedLateral = cellSize * lateralToleranceMultiplier;
                if (lateralDistance > allowedLateral)
                    continue;

                float preferredDistance = Mathf.Clamp(cellSize, minForwardDistance, maxForwardDistance);
                float score = Mathf.Abs(forwardDistance - preferredDistance) + lateralDistance * 2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = cell;
                }
            }

            return best;
        }

        private void CreateOutline()
        {
            var outlineGo = new GameObject("FrontBuildCellOutline");
            outlineGo.transform.SetParent(transform, false);

            outline = outlineGo.AddComponent<LineRenderer>();
            outline.useWorldSpace = true;
            outline.loop = true;
            outline.positionCount = 4;
            outline.startWidth = outlineWidth;
            outline.endWidth = outlineWidth;
            outline.numCornerVertices = 2;
            outline.alignment = LineAlignment.View;
            outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outline.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                outlineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                ApplyMaterialColor(outlineMaterial, outlineColor);
                outline.material = outlineMaterial;
            }

            outline.startColor = outlineColor;
            outline.endColor = outlineColor;
            outline.enabled = false;
        }

        private void UpdateOutline(BuildSurfaceCell cell)
        {
            if (outline == null) return;

            if (cell == null)
            {
                outline.enabled = false;
                return;
            }

            Vector2 footprint = cell.FootprintSize;
            float halfX = Mathf.Max(0.05f, footprint.x * 0.5f - outlineInset);
            float halfZ = Mathf.Max(0.05f, footprint.y * 0.5f - outlineInset);
            Vector3 center = cell.SurfaceCenter + Vector3.up * outlineHeightOffset;

            outline.SetPosition(0, center + new Vector3(-halfX, 0f, -halfZ));
            outline.SetPosition(1, center + new Vector3(halfX, 0f, -halfZ));
            outline.SetPosition(2, center + new Vector3(halfX, 0f, halfZ));
            outline.SetPosition(3, center + new Vector3(-halfX, 0f, halfZ));
            outline.enabled = true;
        }

        private static void ApplyMaterialColor(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }
    }
}
