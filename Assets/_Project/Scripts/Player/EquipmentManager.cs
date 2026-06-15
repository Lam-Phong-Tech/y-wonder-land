using UnityEngine;

namespace YWonderLand.Player
{
    public enum ToolType
    {
        None,
        Axe,
        WateringCan,
        FishingRod,
        Hoe,
        SeedBag,
        Pickaxe,
        AnimalFeed
    }

    /// <summary>
    /// Gắn script này vào nhân vật chính. 
    /// Dùng để quản lý việc bật/tắt các nông cụ trên tay nhân vật.
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        public static EquipmentManager Instance { get; private set; }

        [Header("Tool Models (Kéo thả model công cụ vào đây)")]
        [Tooltip("Model cái Rìu gắn trên tay phải")]
        public GameObject axeModel;
        
        [Tooltip("Model cái Bình tưới gắn trên tay phải")]
        public GameObject wateringCanModel;
        
        [Tooltip("Model Cần câu gắn trên tay phải")]
        public GameObject fishingRodModel;
        
        [Tooltip("Model cái Cuốc gắn trên tay phải")]
        public GameObject hoeModel;

        [Tooltip("Model túi hạt giống gắn trên tay trái")]
        public GameObject seedBagModel;

        [Tooltip("Model cái Cúp (đập đá) gắn trên tay phải")]
        public GameObject pickaxeModel;

        [Tooltip("Model nắm cám/thức ăn (cho ăn) gắn trên tay")]
        public GameObject feedModel;

        [Header("Placeholder tự sinh (khi CHƯA có model 3D thật)")]
        [Tooltip("Tự tạo dụng cụ tạm bằng khối primitive nếu ô model còn trống")]
        public bool autoGeneratePlaceholders = true;

        [Tooltip("Xương tay PHẢI để gắn dụng cụ (để trống = tự tìm từ Animator)")]
        public Transform rightHandAnchor;
        [Tooltip("Xương tay TRÁI để gắn túi hạt (để trống = tự tìm từ Animator)")]
        public Transform leftHandAnchor;

        [Tooltip("Tinh chỉnh nếu dụng cụ bị lệch khỏi lòng bàn tay (chỉnh trong Play Mode)")]
        public Vector3 toolPositionOffset = Vector3.zero;
        public Vector3 toolRotationOffset = Vector3.zero;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Debug.Log($"[Equipment] ✅ EquipmentManager ĐANG CHẠY trên GameObject '{gameObject.name}'. AutoGenerate={autoGeneratePlaceholders}");
            if (autoGeneratePlaceholders)
                GeneratePlaceholdersIfMissing();

            // Khởi đầu game thì giấu hết đồ nghề đi
            HideAllTools();
        }

        // ── Tự sinh dụng cụ tạm (primitive) khi chưa có model thật ──
        private void GeneratePlaceholdersIfMissing()
        {
            // Tìm xương tay từ Animator humanoid nếu chưa gán tay
            var animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                if (rightHandAnchor == null) rightHandAnchor = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (leftHandAnchor == null) leftHandAnchor = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            }

            Transform rh = rightHandAnchor != null ? rightHandAnchor : transform;
            Transform lh = leftHandAnchor != null ? leftHandAnchor : transform;

            if (rightHandAnchor != null)
                Debug.Log($"[Equipment] ✅ Tìm thấy xương tay PHẢI: '{rightHandAnchor.name}' -> gắn dụng cụ vào đây.");
            else
                Debug.LogWarning("[Equipment] ⚠️ KHÔNG tìm thấy xương tay (rig không phải Humanoid?). Dụng cụ sẽ gắn vào GỐC nhân vật -> dễ nằm dưới chân. Hãy kéo bone tay vào ô Right/Left Hand Anchor.");

            if (axeModel == null) axeModel = BuildAxe(rh);
            if (pickaxeModel == null) pickaxeModel = BuildPickaxe(rh);
            if (hoeModel == null) hoeModel = BuildHoe(rh);
            if (wateringCanModel == null) wateringCanModel = BuildWateringCan(rh);
            if (fishingRodModel == null) fishingRodModel = BuildFishingRod(rh);
            if (seedBagModel == null) seedBagModel = BuildSeedBag(lh);
            if (feedModel == null) feedModel = BuildFeed(rh);
        }

        private GameObject NewToolRoot(string name, Transform hand)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(hand, false);
            root.transform.localPosition = toolPositionOffset;
            root.transform.localRotation = Quaternion.Euler(toolRotationOffset);
            return root;
        }

        private void Prim(Transform parent, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Color color)
        {
            GameObject g = GameObject.CreatePrimitive(type);
            var col = g.GetComponent<Collider>();
            if (col != null) Destroy(col); // dụng cụ không cần va chạm
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(euler);
            g.transform.localScale = scale;
            var r = g.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                r.material.color = color;
            }
        }

        private static readonly Color WOOD = new Color(0.5f, 0.35f, 0.2f);
        private static readonly Color METAL = new Color(0.6f, 0.62f, 0.66f);

        private GameObject BuildAxe(Transform hand)
        {
            var root = NewToolRoot("Placeholder_Axe", hand);
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0.18f, 0), Vector3.zero, new Vector3(0.025f, 0.18f, 0.025f), WOOD); // cán
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0.37f, 0.01f), Vector3.zero, new Vector3(0.14f, 0.09f, 0.03f), METAL); // lưỡi
            return root;
        }

        private GameObject BuildPickaxe(Transform hand)
        {
            var root = NewToolRoot("Placeholder_Pickaxe", hand);
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0.18f, 0), Vector3.zero, new Vector3(0.025f, 0.18f, 0.025f), WOOD); // cán
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0.37f, 0.01f), new Vector3(0, 0, 90), new Vector3(0.02f, 0.13f, 0.02f), METAL); // đầu cúp ngang (chữ T)
            return root;
        }

        private GameObject BuildHoe(Transform hand)
        {
            var root = NewToolRoot("Placeholder_Hoe", hand);
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0.2f, 0), Vector3.zero, new Vector3(0.022f, 0.22f, 0.022f), WOOD); // cán
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0.42f, 0.04f), new Vector3(60, 0, 0), new Vector3(0.1f, 0.04f, 0.02f), METAL); // lưỡi cuốc
            return root;
        }

        private GameObject BuildWateringCan(Transform hand)
        {
            var root = NewToolRoot("Placeholder_WateringCan", hand);
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0.1f, 0), Vector3.zero, new Vector3(0.1f, 0.08f, 0.1f), new Color(0.3f, 0.55f, 0.7f)); // thân
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0.1f, 0.14f, 0), new Vector3(0, 0, 55), new Vector3(0.02f, 0.08f, 0.02f), new Color(0.3f, 0.55f, 0.7f)); // vòi
            return root;
        }

        private GameObject BuildFishingRod(Transform hand)
        {
            var root = NewToolRoot("Placeholder_FishingRod", hand);
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0.5f, 0), new Vector3(15, 0, 0), new Vector3(0.012f, 0.6f, 0.012f), WOOD); // cần dài
            return root;
        }

        private GameObject BuildSeedBag(Transform hand)
        {
            var root = NewToolRoot("Placeholder_SeedBag", hand);
            Prim(root.transform, PrimitiveType.Sphere, new Vector3(0, 0.05f, 0), Vector3.zero, new Vector3(0.1f, 0.12f, 0.09f), new Color(0.55f, 0.4f, 0.25f)); // túi
            return root;
        }

        private GameObject BuildFeed(Transform hand)
        {
            var root = NewToolRoot("Placeholder_Feed", hand);
            Prim(root.transform, PrimitiveType.Sphere, new Vector3(0, 0.02f, 0.03f), Vector3.zero, new Vector3(0.09f, 0.04f, 0.09f), new Color(0.85f, 0.72f, 0.3f)); // nắm cám vàng
            return root;
        }

        /// <summary>
        /// Gọi hàm này để hiện một công cụ cụ thể lên tay
        /// </summary>
        public void ShowTool(ToolType toolType)
        {
            Debug.Log($"[Equipment] 🔨 ShowTool({toolType}) được gọi.");
            HideAllTools(); // Giấu cái cũ trước khi lấy cái mới ra

            switch (toolType)
            {
                case ToolType.Axe:
                    if (axeModel != null) axeModel.SetActive(true);
                    break;
                case ToolType.WateringCan:
                    if (wateringCanModel != null) wateringCanModel.SetActive(true);
                    break;
                case ToolType.FishingRod:
                    if (fishingRodModel != null) fishingRodModel.SetActive(true);
                    break;
                case ToolType.Hoe:
                    if (hoeModel != null) hoeModel.SetActive(true);
                    break;
                case ToolType.SeedBag:
                    if (seedBagModel != null) seedBagModel.SetActive(true);
                    break;
                case ToolType.Pickaxe:
                    if (pickaxeModel != null) pickaxeModel.SetActive(true);
                    break;
                case ToolType.AnimalFeed:
                    if (feedModel != null) feedModel.SetActive(true);
                    break;
            }
        }

        /// <summary>
        /// Gọi hàm này để cất hết công cụ đi (tay không)
        /// </summary>
        public void HideAllTools()
        {
            if (axeModel != null) axeModel.SetActive(false);
            if (wateringCanModel != null) wateringCanModel.SetActive(false);
            if (fishingRodModel != null) fishingRodModel.SetActive(false);
            if (hoeModel != null) hoeModel.SetActive(false);
            if (seedBagModel != null) seedBagModel.SetActive(false);
            if (pickaxeModel != null) pickaxeModel.SetActive(false);
            if (feedModel != null) feedModel.SetActive(false);
        }
    }
}
