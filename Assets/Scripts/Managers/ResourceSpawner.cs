using UnityEngine;
using System.Collections.Generic;

namespace YWonderLand.Environment
{
    public class ResourceSpawner : MonoBehaviour
    {
        public static ResourceSpawner Instance { get; private set; }

        [Header("Spawn Settings")]
        public int treeCount = 10;
        public int rockCount = 5;
        public float spawnRadius = 30f;
        public Vector3 spawnCenter = new Vector3(0, 0, 0);

        [System.Serializable]
        public class ResourceSaveData
        {
            public string id;
            public HarvestableResource.ResourceType type;
            public Vector3 position;
            public float respawnTimer;
        }

        [System.Serializable]
        public class ResourceSaveList
        {
            public List<ResourceSaveData> resources = new List<ResourceSaveData>();
        }

        private List<HarvestableResource> activeResources = new List<HarvestableResource>();
        private const string SAVE_KEY = "ResourceSpawnerState";

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        void Start()
        {
            LoadResourceState();
        }

        private void LoadResourceState()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
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
                Vector3 pos = GetRandomPosition();
                SpawnNewResource($"Tree_{i}", HarvestableResource.ResourceType.Tree, pos);
            }

            for (int i = 0; i < rockCount; i++)
            {
                Vector3 pos = GetRandomPosition();
                SpawnNewResource($"Rock_{i}", HarvestableResource.ResourceType.Rock, pos);
            }
            
            SaveResourceState();
        }

        private Vector3 GetRandomPosition()
        {
            Vector2 rand = Random.insideUnitCircle * spawnRadius;
            return spawnCenter + new Vector3(rand.x, 0, rand.y);
        }

        private void SpawnNewResource(string id, HarvestableResource.ResourceType type, Vector3 pos)
        {
            GameObject go = new GameObject(id);
            go.transform.position = pos;
            go.transform.SetParent(transform);

            HarvestableResource res = go.AddComponent<HarvestableResource>();
            
            if (type == HarvestableResource.ResourceType.Tree)
                res.Initialize(id, type, "axe_01", "wood_01");
            else
                res.Initialize(id, type, "pickaxe_01", "stone_01"); // Changed to pickaxe_01

            activeResources.Add(res);
        }

        private void SpawnResourceFromData(ResourceSaveData data)
        {
            GameObject go = new GameObject(data.id);
            go.transform.position = data.position;
            go.transform.SetParent(transform);

            HarvestableResource res = go.AddComponent<HarvestableResource>();
            
            if (data.type == HarvestableResource.ResourceType.Tree)
                res.Initialize(data.id, data.type, "axe_01", "wood_01");
            else
                res.Initialize(data.id, data.type, "pickaxe_01", "stone_01"); // Changed to pickaxe_01

            res.RestoreState(data.respawnTimer);
            activeResources.Add(res);
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
                        respawnTimer = res.respawnTimer
                    });
                }
            }

            string json = JsonUtility.ToJson(list);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        void OnApplicationQuit()
        {
            SaveResourceState();
        }
    }
}
