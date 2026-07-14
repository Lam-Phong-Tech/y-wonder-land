using UnityEngine;
using System.Collections.Generic;
using YWonderLand.Data;
using YWonderLand.Environment;
using System.Linq;
using YWonderLand.Backend;

namespace YWonderLand.Managers
{
    public class AnimalManager : MonoBehaviour
    {
        public static AnimalManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private AnimalPen[] pens;
        [SerializeField] private CropDatabase cropDatabase; // Note: You might want a dedicated AnimalDatabase later, reusing for now if it holds AnimalDefinition

        private const string SAVE_KEY = "YW_AnimalState";
        private bool loadComplete;
        
        // Lookup dictionary for animal definitions
        private Dictionary<string, AnimalDefinition> animalDefs = new Dictionary<string, AnimalDefinition>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            LoadDefinitions();
        }

        private void OnEnable()
        {
            FarmStateSync.AuthoritativeSnapshotApplied += HandleAuthoritativeSnapshotApplied;
            if (AuthService.Instance == null) return;
            AuthService.Instance.IdentityChanging += HandleIdentityChanging;
            AuthService.Instance.IdentityChanged += HandleIdentityChanged;
        }

        private void OnDisable()
        {
            FarmStateSync.AuthoritativeSnapshotApplied -= HandleAuthoritativeSnapshotApplied;
            if (AuthService.Instance == null) return;
            AuthService.Instance.IdentityChanging -= HandleIdentityChanging;
            AuthService.Instance.IdentityChanged -= HandleIdentityChanged;
        }

        private void HandleIdentityChanging(string previousScopeId, string nextScopeId)
        {
            SaveAnimalState();
        }

        private void HandleIdentityChanged(string previousScopeId, string nextScopeId)
        {
            if (pens == null || pens.Length == 0)
                pens = FindObjectsByType<AnimalPen>(FindObjectsSortMode.None);

            ClearRuntimeAnimals();
            loadComplete = false;
            LoadAnimalState();
            loadComplete = true;
        }

        private void HandleAuthoritativeSnapshotApplied()
        {
            if (pens == null || pens.Length == 0)
                pens = FindObjectsByType<AnimalPen>(FindObjectsSortMode.None);

            ClearRuntimeAnimals();
            loadComplete = false;
            LoadAnimalState();
            loadComplete = true;
        }

        void Start()
        {
            pens = FindObjectsByType<AnimalPen>(FindObjectsSortMode.None);
            LoadAnimalState();
            loadComplete = true;
        }

        private void LoadDefinitions()
        {
            // For now, load all AnimalDefinitions from Resources
            AnimalDefinition[] loaded = Resources.LoadAll<AnimalDefinition>("");
            foreach (var def in loaded)
            {
                animalDefs[def.animalId] = def;
            }
        }

        public AnimalDefinition GetDefinition(string animalId)
        {
            if (animalDefs.ContainsKey(animalId)) return animalDefs[animalId];
            return null;
        }

        /// <summary>
        /// Tra AnimalDefinition theo id — CHẮC ĂN: ưu tiên AnimalManager (nếu có trong scene),
        /// nếu không có/không thấy thì load thẳng asset từ Resources (Assets/Resources/Items/Animal_&lt;id&gt;.asset).
        /// Dùng cho UI túi/shop + validate thả thú để không phụ thuộc việc scene có gắn AnimalManager hay chưa.
        /// </summary>
        public static AnimalDefinition LookupDefinition(string animalId)
        {
            if (string.IsNullOrEmpty(animalId)) return null;
            if (Instance != null)
            {
                var d = Instance.GetDefinition(animalId);
                if (d != null) return d;
            }
            return Resources.Load<AnimalDefinition>("Items/Animal_" + animalId);
        }

        public System.Action<string> OnAnimalBought;

        public bool BuyAndSpawnAnimal(string animalId)
        {
            AnimalDefinition def = GetDefinition(animalId);
            if (def == null) return false;

            // Find an empty slot
            AnimalPen emptyPen = pens.FirstOrDefault(p => p.HasSpace());
            if (emptyPen == null)
            {
                Debug.LogWarning("[AnimalManager] All pens are full!");
                return false; // No space
            }

            GameObject go = new GameObject($"Animal_{def.animalId}");
            FarmAnimal newAnimal = go.AddComponent<FarmAnimal>();
            newAnimal.Initialize(def);
            
            emptyPen.AddAnimal(newAnimal);
            
            Debug.Log($"[AnimalManager] Spawned {animalId} in pen {emptyPen.penId}");
            OnAnimalBought?.Invoke(animalId);
            return true;
        }

        // ── Save / Load ──

        public void SaveAnimalState()
        {
            if (!loadComplete)
            {
                Debug.LogWarning("[AnimalManager] Skip save before load completes to avoid overwriting existing animal state.");
                return;
            }

            if (pens == null)
            {
                Debug.LogWarning("[AnimalManager] Skip save because animal pens are not initialized.");
                return;
            }

            AnimalSaveData saveData = new AnimalSaveData();
            saveData.animals = new List<AnimalStateData>();

            foreach (var pen in pens)
            {
                if (pen == null) continue;
                foreach (var animal in pen.GetAnimals())
                {
                    if (animal == null || animal.data == null) continue;
                    saveData.animals.Add(new AnimalStateData
                    {
                        instanceId = animal.animalInstanceId,
                        definitionId = animal.data.animalId,
                        penId = pen.penId,
                        state = (int)animal.currentState,
                        feedTimer = animal.feedTimer,
                        produceTimer = animal.produceTimer,
                        harvests = animal.harvestsRemaining,
                        hasProduct = animal.hasProductReady,
                        vaccinated = animal.isVaccinated,
                        posX = animal.transform.position.x,
                        posY = animal.transform.position.y,
                        posZ = animal.transform.position.z
                    });
                }
            }

            string json = JsonUtility.ToJson(saveData);
            PlayerScopedPrefs.SetString(SAVE_KEY, json);
            PlayerScopedPrefs.Save();
            FarmStateSync.NotifyLocalStateSaved();
        }

        public void LoadAnimalState()
        {
            if (!PlayerScopedPrefs.HasKey(SAVE_KEY)) return;

            string json = PlayerScopedPrefs.GetString(SAVE_KEY);
            AnimalSaveData saveData = JsonUtility.FromJson<AnimalSaveData>(json);

            if (saveData == null || saveData.animals == null) return;

            ClearRuntimeAnimals();

            foreach (var aData in saveData.animals)
            {
                AnimalDefinition def = GetDefinition(aData.definitionId);
                if (def == null) continue;

                AnimalPen pen = pens.FirstOrDefault(p => p.penId == aData.penId);
                if (pen == null) pen = pens.FirstOrDefault(p => p.HasSpace()); // Fallback
                if (pen == null) continue;

                GameObject go = new GameObject($"Animal_{def.animalId}");
                go.transform.position = new Vector3(aData.posX, aData.posY, aData.posZ);
                
                FarmAnimal animal = go.AddComponent<FarmAnimal>();
                animal.animalInstanceId = aData.instanceId;
                animal.Initialize(def);
                animal.LoadState((FarmAnimal.AnimalState)aData.state, aData.feedTimer, aData.produceTimer, aData.harvests, aData.hasProduct, aData.vaccinated);
                
                pen.AddAnimal(animal);
            }
        }

        private void ClearRuntimeAnimals()
        {
            if (pens == null) return;
            foreach (var pen in pens)
            {
                if (pen == null) continue;
                foreach (var animal in pen.GetAnimals().ToArray())
                    if (animal != null) Destroy(animal.gameObject);
                pen.GetAnimals().Clear();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveAnimalState();
        }

        void OnApplicationQuit()
        {
            SaveAnimalState();
        }

        // ── Save Data Structures ──

        [System.Serializable]
        private class AnimalSaveData
        {
            public List<AnimalStateData> animals;
        }

        [System.Serializable]
        private class AnimalStateData
        {
            public string instanceId;
            public string definitionId;
            public string penId;
            public int state;
            public float feedTimer;
            public float produceTimer;
            public int harvests;
            public bool hasProduct;
            public bool vaccinated;
            public float posX;
            public float posY;
            public float posZ;
        }
    }
}
