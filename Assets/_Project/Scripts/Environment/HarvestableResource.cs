using UnityEngine;
using System.Collections;
using YWonderLand.Managers;

namespace YWonderLand.Environment
{
    public class HarvestableResource : MonoBehaviour
    {
        public enum ResourceType
        {
            Tree,
            Rock
        }

        public static event System.Action<string, int> OnResourceHarvested;

        [Header("Configuration")]
        public string resourceId; // Unique ID for saving state
        public ResourceType type;
        public string requiredTool; // e.g. "axe_01", "pickaxe_01"
        public string yieldItemId; // e.g. "wood_01", "stone_01"
        public int minYield = 1;
        public int maxYield = 3;
        public float respawnTimeSec = 3600f; // Default 1 hour
        public float harvestDuration = 3f; // Seconds to harvest

        [Tooltip("Khoảng cách tối đa (mét) để hiện gợi ý + cho phép chặt/đập. Đặt 1-2 cho gần giống Minecraft.")]
        public float interactionRange = 2f;

        [Header("State")]
        public bool isHarvestable = true;
        public float currentProgress = 0f;
        public float respawnTimer = 0f;

        private GameObject visualObject;
        private Collider resourceCollider;
        private Quaternion _originalVisualRot = Quaternion.identity;
        private bool _rotCaptured = false;

        void Awake()
        {
            // Try to find child visual object or use self
            if (transform.childCount > 0)
                visualObject = transform.GetChild(0).gameObject;
            else
                visualObject = gameObject;

            resourceCollider = GetComponent<Collider>();
            if (resourceCollider == null)
                resourceCollider = gameObject.AddComponent<BoxCollider>();
        }

        public void Initialize(string id, ResourceType type, string tool, string yieldItem)
        {
            resourceId = id;
            this.type = type;
            requiredTool = tool;
            yieldItemId = yieldItem;
            
            CreateFallbackVisual();
        }

        private void CreateFallbackVisual()
        {
            // Remove existing primitive meshes
            var existingMesh = GetComponent<MeshFilter>();
            if (existingMesh != null) Destroy(existingMesh);
            var existingRend = GetComponent<MeshRenderer>();
            if (existingRend != null) Destroy(existingRend);

            if (visualObject == gameObject)
            {
                visualObject = new GameObject("Visuals");
                visualObject.transform.SetParent(transform);
                visualObject.transform.localPosition = Vector3.zero;
            }

            MeshFilter mf = visualObject.GetComponent<MeshFilter>();
            if (mf == null) mf = visualObject.AddComponent<MeshFilter>();
            MeshRenderer mr = visualObject.GetComponent<MeshRenderer>();
            if (mr == null) mr = visualObject.AddComponent<MeshRenderer>();

            if (type == ResourceType.Tree)
            {
                // Green Cylinder
                GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mf.mesh = cyl.GetComponent<MeshFilter>().sharedMesh;
                Destroy(cyl);
                
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.1f, 0.6f, 0.2f);
                mr.material = mat;
                
                visualObject.transform.localScale = new Vector3(1f, 2f, 1f);
                visualObject.transform.localPosition = new Vector3(0, 2f, 0);
            }
            else
            {
                // Gray Cube
                GameObject cub = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mf.mesh = cub.GetComponent<MeshFilter>().sharedMesh;
                Destroy(cub);
                
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.5f, 0.5f, 0.5f);
                mr.material = mat;
                
                visualObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                visualObject.transform.localPosition = new Vector3(0, 0.75f, 0);
            }

            // Adjust collider
            if (resourceCollider is BoxCollider box)
            {
                if (type == ResourceType.Tree)
                {
                    box.size = new Vector3(1f, 4f, 1f);
                    box.center = new Vector3(0, 2f, 0);
                }
                else
                {
                    box.size = new Vector3(1.5f, 1.5f, 1.5f);
                    box.center = new Vector3(0, 0.75f, 0);
                }
            }
        }

        public void RestoreState(float timer)
        {
            if (timer > 0)
            {
                respawnTimer = timer;
                SetHarvestable(false);
            }
            else
            {
                respawnTimer = 0;
                SetHarvestable(true);
            }
        }

        void Update()
        {
            if (!isHarvestable && respawnTimer > 0)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0)
                {
                    SetHarvestable(true);
                    var spawner = GetComponentInParent<ResourceSpawner>();
                    if (spawner != null)
                        spawner.SaveResourceState();
                }
            }
        }

        private void SetHarvestable(bool state)
        {
            isHarvestable = state;
            currentProgress = 0f;
            
            if (visualObject != null) visualObject.SetActive(state);
            if (resourceCollider != null) resourceCollider.enabled = state;
        }

        /// <summary>
        /// Called continuously while the player holds click on this resource.
        /// </summary>
        public bool Harvest(float deltaTime)
        {
            if (!isHarvestable) return false;

            // Check if player has the required tool
            var inv = InventoryManager.Instance;
            if (inv == null || inv.GetItemQuantity(requiredTool) <= 0)
            {
                Debug.Log($"[HarvestableResource] You need {requiredTool} to harvest this {type}!");
                // For demo/testing, auto-give tool
                Debug.Log($"[HarvestableResource] Auto-giving {requiredTool} for demo.");
                inv?.AddItem(requiredTool, 1);
            }

            // TODO: Apply tool level bonus (e.g. axe_02 reduces harvest duration)

            currentProgress += deltaTime;
            
            // Wiggle animation (rung lắc TƯƠNG ĐỐI quanh hướng gốc, không phá hướng đứng của model)
            if (visualObject != null)
            {
                if (!_rotCaptured)
                {
                    _originalVisualRot = visualObject.transform.localRotation;
                    _rotCaptured = true;
                }
                float wiggle = Mathf.Sin(Time.time * 20f) * 5f;
                visualObject.transform.localRotation = _originalVisualRot * Quaternion.Euler(0, 0, wiggle);
            }

            if (currentProgress >= harvestDuration)
            {
                CompleteHarvest();
                return true; // Harvest complete
            }

            return false; // Still harvesting
        }

        public void CancelHarvest()
        {
            currentProgress = 0f;
            if (visualObject != null && _rotCaptured)
            {
                visualObject.transform.localRotation = _originalVisualRot;
            }
        }

        private void CompleteHarvest()
        {
            int yield = Random.Range(minYield, maxYield + 1);
            
            var inv = InventoryManager.Instance;
            if (inv != null)
            {
                inv.AddItem(yieldItemId, yield);
                Debug.Log($"[HarvestableResource] Harvested {yield}x {yieldItemId}!");
                OnResourceHarvested?.Invoke(yieldItemId, yield);
            }

            // VFX/SFX could be added here

            respawnTimer = respawnTimeSec;

            // CÂY thì cho ĐỔ xuống rồi mới biến mất; ĐÁ thì ẩn ngay như cũ.
            if (type == ResourceType.Tree && visualObject != null && gameObject.activeInHierarchy)
            {
                isHarvestable = false;
                currentProgress = 0f;
                if (resourceCollider != null) resourceCollider.enabled = false;
                if (_rotCaptured) visualObject.transform.localRotation = _originalVisualRot; // xoá rung trước khi đổ
                StartCoroutine(FallAndHide());
            }
            else
            {
                SetHarvestable(false);
                CancelHarvest();
            }

            var spawner = GetComponentInParent<ResourceSpawner>();
            if (spawner != null)
                spawner.SaveResourceState();
        }

        // Cho cây ĐỔ GỤC xuống đất rồi mới ẩn — xoay quanh ĐÁY cây (điểm chạm đất),
        // không xoay quanh pivot object (tránh cảnh đổ lơ lửng giữa không trung).
        private System.Collections.IEnumerator FallAndHide()
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            // Điểm tựa = đáy mesh (min Y) chiếu xuống vị trí gốc -> mép chân cây.
            float baseY = transform.position.y;
            var rend = visualObject != null ? visualObject.GetComponentInChildren<Renderer>() : null;
            if (rend != null) baseY = rend.bounds.min.y;
            Vector3 pivot = new Vector3(transform.position.x, baseY, transform.position.z);

            Vector3 axis = transform.right; // trục ngang -> đổ nghiêng sang bên
            const float totalAngle = 84f;
            float dur = 0.9f, t = 0f, prevAngle = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                k = k * k; // gia tốc rơi (chậm -> nhanh)
                float targetAngle = totalAngle * k;
                transform.RotateAround(pivot, axis, targetAngle - prevAngle);
                prevAngle = targetAngle;
                yield return null;
            }
            transform.RotateAround(pivot, axis, totalAngle - prevAngle);
            yield return new WaitForSeconds(0.5f); // nằm 1 lúc cho thấy rõ

            if (visualObject != null) visualObject.SetActive(false);
            // Dựng lại nguyên trạng cho lần respawn sau
            transform.position = startPos;
            transform.rotation = startRot;
        }

        void OnDrawGizmosSelected()
        {
            // Vẽ vòng tròn tầm tương tác để dễ canh trong Scene view
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}
