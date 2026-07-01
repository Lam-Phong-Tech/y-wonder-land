using UnityEngine;
using System.Collections.Generic;

namespace YWonderLand.Environment
{
    public class ResourceSpawner : MonoBehaviour
    {
        [Header("Unique ID for Saving")]
        [Tooltip("Nhập ID khác nhau cho mỗi Spawner (VD: Farm, Mine) để không bị đè file save")]
        public string spawnerID = "Farm";

        [Header("Spawn Settings")]
        public int treeCount = 10;
        public int rockCount = 5;
        public float spawnRadius = 30f;
        public Vector3 spawnCenter = new Vector3(0, 0, 0);

        [Header("Controlled Spawn Areas")]
        [Tooltip("Neu co gan Collider o day, tai nguyen chi spawn ben trong cac vung nay. De trong thi dung spawnRadius nhu cu.")]
        [SerializeField] private List<Collider> spawnAreas = new List<Collider>();
        [SerializeField] private float minDistanceBetweenResources = 2f;
        [SerializeField] private int maxPositionAttempts = 80;
        [SerializeField] private bool drawSpawnGizmos = true;

        [Header("Resource Prefabs")]
        [SerializeField] private GameObject treePrefab;
        [SerializeField] private GameObject rockPrefab;

        [Header("Mine Respawn")]
        [Tooltip("Bat cho dao mo: moi lan tai nguyen hoi sinh se doi sang vi tri ngau nhien moi trong vung spawn.")]
        [SerializeField] private bool randomizePositionOnRespawn = false;
        [Tooltip("Neu bat, vi tri spawn se raycast xuong mat dat. Nen set mask chi gom layer nen dao.")]
        [SerializeField] private bool snapSpawnToGround = false;
        [SerializeField] private LayerMask spawnGroundMask = ~0;
        [SerializeField] private float groundRaycastHeight = 50f;
        [SerializeField] private float groundRaycastDistance = 120f;

        [System.Serializable]
        public class ResourceSaveData
        {
            public string id;
            public HarvestableResource.ResourceType type;
            public Vector3 position;
            public float respawnTimer;
            public double respawnEndUnix;
        }

        [System.Serializable]
        public class ResourceSaveList
        {
            public List<ResourceSaveData> resources = new List<ResourceSaveData>();
        }

        private List<HarvestableResource> activeResources = new List<HarvestableResource>();
        
        private string SaveKey => "ResourceSpawnerState_" + spawnerID;

        void Start()
        {
            LoadResourceState();
        }

        private void LoadResourceState()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                string json = PlayerPrefs.GetString(SaveKey);
                ResourceSaveList list = JsonUtility.FromJson<ResourceSaveList>(json);
                
                foreach (var data in list.resources)
                {
                    SpawnResourceFromData(data);
                }
            }
            else
            {
                // First time setup: Spawn new resources randomly
                GenerateInitialResources();
            }
        }

        private void GenerateInitialResources()
        {
            for (int i = 0; i < treeCount; i++)
            {
                if (TryGetRandomPosition(out Vector3 pos))
                    SpawnNewResource($"Tree_{i}", HarvestableResource.ResourceType.Tree, pos);
            }

            for (int i = 0; i < rockCount; i++)
            {
                if (TryGetRandomPosition(out Vector3 pos))
                    SpawnNewResource($"Rock_{i}", HarvestableResource.ResourceType.Rock, pos);
            }
            
            SaveResourceState();
        }

        private bool TryGetRandomPosition(out Vector3 pos, HarvestableResource ignoreSpacingFor = null)
        {
            if (HasControlledSpawnAreas())
                return TryGetRandomPositionInAreas(out pos, ignoreSpacingFor);

            pos = GetRandomCirclePosition();
            return true;
        }

        private Vector3 GetRandomCirclePosition()
        {
            Vector2 rand = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = transform.position + spawnCenter + new Vector3(rand.x, 0, rand.y);
            return SnapPositionToGround(pos);
        }

        private bool TryGetRandomPositionInAreas(out Vector3 pos, HarvestableResource ignoreSpacingFor)
        {
            int attempts = Mathf.Max(1, maxPositionAttempts);
            for (int i = 0; i < attempts; i++)
            {
                Collider area = PickWeightedArea();
                if (area == null) break;

                Bounds bounds = area.bounds;
                Vector3 candidate = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.center.y,
                    Random.Range(bounds.min.z, bounds.max.z));

                candidate = SnapPositionToGround(candidate);
                if (!IsPointInsideArea(area, candidate))
                    continue;

                if (!HasMinimumSpacing(candidate, ignoreSpacingFor))
                    continue;

                pos = candidate;
                return true;
            }

            pos = transform.position + spawnCenter;
            Debug.LogWarning($"[ResourceSpawner] Khong tim duoc vi tri spawn hop le trong vung '{spawnerID}'. Hay kiem tra Spawn Areas, Ground Mask, hoac giam Min Distance.");
            return false;
        }

        private bool HasControlledSpawnAreas()
        {
            if (spawnAreas == null || spawnAreas.Count == 0) return false;
            for (int i = 0; i < spawnAreas.Count; i++)
            {
                if (spawnAreas[i] != null) return true;
            }
            return false;
        }

        private Collider PickWeightedArea()
        {
            float totalWeight = 0f;
            for (int i = 0; i < spawnAreas.Count; i++)
            {
                Collider area = spawnAreas[i];
                if (area == null) continue;
                Bounds bounds = area.bounds;
                totalWeight += Mathf.Max(0.01f, bounds.size.x * bounds.size.z);
            }

            if (totalWeight <= 0f) return null;

            float roll = Random.Range(0f, totalWeight);
            for (int i = 0; i < spawnAreas.Count; i++)
            {
                Collider area = spawnAreas[i];
                if (area == null) continue;
                Bounds bounds = area.bounds;
                roll -= Mathf.Max(0.01f, bounds.size.x * bounds.size.z);
                if (roll <= 0f) return area;
            }

            return null;
        }

        private static bool IsPointInsideArea(Collider area, Vector3 point)
        {
            if (area == null) return false;

            Vector3 probe = point + Vector3.up * 0.05f;
            Vector3 closest = area.ClosestPoint(probe);
            return (closest - probe).sqrMagnitude <= 0.0004f;
        }

        private bool HasMinimumSpacing(Vector3 candidate, HarvestableResource ignore)
        {
            if (minDistanceBetweenResources <= 0f) return true;

            float minSqr = minDistanceBetweenResources * minDistanceBetweenResources;
            foreach (var res in activeResources)
            {
                if (res == null || res == ignore) continue;
                Vector3 delta = res.transform.position - candidate;
                delta.y = 0f;
                if (delta.sqrMagnitude < minSqr) return false;
            }

            return true;
        }

        private Vector3 SnapPositionToGround(Vector3 pos)
        {
            if (!snapSpawnToGround) return pos;

            Vector3 origin = pos + Vector3.up * Mathf.Max(1f, groundRaycastHeight);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRaycastDistance, spawnGroundMask, QueryTriggerInteraction.Ignore))
                return hit.point;

            return pos;
        }

        private void SpawnNewResource(string id, HarvestableResource.ResourceType type, Vector3 pos)
        {
            HarvestableResource res = CreateResource(id, type, pos);
            activeResources.Add(res);
        }

        private void SpawnResourceFromData(ResourceSaveData data)
        {
            HarvestableResource res = CreateResource(data.id, data.type, data.position);
            res.RestoreState(data.respawnTimer, data.respawnEndUnix);
            activeResources.Add(res);
        }

        private HarvestableResource CreateResource(string id, HarvestableResource.ResourceType type, Vector3 pos)
        {
            GameObject prefab = type == HarvestableResource.ResourceType.Tree ? treePrefab : rockPrefab;
            GameObject go;

            if (prefab != null)
            {
                go = Instantiate(prefab, pos, Quaternion.identity, transform);
                go.name = id;
            }
            else
            {
                go = new GameObject(id);
                go.transform.position = pos;
                go.transform.SetParent(transform);
            }

            HarvestableResource res = go.GetComponent<HarvestableResource>();
            if (res == null)
                res = go.AddComponent<HarvestableResource>();

            string toolId = type == HarvestableResource.ResourceType.Tree ? "axe_01" : "pickaxe_01";
            string yieldId = type == HarvestableResource.ResourceType.Tree ? "wood_01" : "stone_01";

            if (prefab == null)
            {
                res.Initialize(id, type, toolId, yieldId);
            }
            else
            {
                res.resourceId = id;
                res.type = type;
                res.requiredTool = toolId;
                res.yieldItemId = yieldId;
            }

            return res;
        }

        public void PrepareResourceRespawn(HarvestableResource resource)
        {
            if (!randomizePositionOnRespawn || resource == null) return;
            if (TryGetRandomPosition(out Vector3 pos, resource))
                resource.transform.position = pos;
        }

        public void SaveResourceState()
        {
            ResourceSaveList list = new ResourceSaveList();
            foreach (var res in activeResources)
            {
                if (res != null)
                {
                    list.resources.Add(new ResourceSaveData
                    {
                        id = res.resourceId,
                        type = res.type,
                        position = res.transform.position,
                        respawnTimer = res.respawnTimer,
                        respawnEndUnix = res.RespawnEndUnix
                    });
                }
            }

            string json = JsonUtility.ToJson(list);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        void OnApplicationQuit()
        {
            SaveResourceState();
        }

        void OnApplicationPause(bool pause)
        {
            if (pause)
                SaveResourceState();
        }

        [ContextMenu("Clear Saved Resource State")]
        private void ClearSavedResourceState()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            Debug.Log($"[ResourceSpawner] Cleared saved state: {SaveKey}");
        }

        void OnDrawGizmosSelected()
        {
            if (!drawSpawnGizmos) return;

            Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.9f);
            if (HasControlledSpawnAreas())
            {
                foreach (var area in spawnAreas)
                {
                    if (area == null) continue;

                    if (area is BoxCollider box)
                    {
                        Matrix4x4 oldMatrix = Gizmos.matrix;
                        Gizmos.matrix = box.transform.localToWorldMatrix;
                        Gizmos.DrawWireCube(box.center, box.size);
                        Gizmos.matrix = oldMatrix;
                    }
                    else
                    {
                        Gizmos.DrawWireCube(area.bounds.center, area.bounds.size);
                    }
                }
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position + spawnCenter, spawnRadius);
            }
        }
    }
}
