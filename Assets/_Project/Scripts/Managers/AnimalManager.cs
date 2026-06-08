using UnityEngine;
using System.Collections.Generic;
using YWonderLand.Data;
using YWonderLand.Environment;
using System.Linq;

namespace YWonderLand.Managers
{
    public class AnimalManager : MonoBehaviour
    {
        public static AnimalManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private AnimalPen[] pens;
        [SerializeField] private CropDatabase cropDatabase; // Note: You might want a dedicated AnimalDatabase later, reusing for now if it holds AnimalDefinition

        private const string SAVE_KEY = "YW_AnimalState";
        
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

        void Start()
        {
            pens = FindObjectsByType<AnimalPen>(FindObjectsSortMode.None);
            LoadAnimalState();
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
            AnimalSaveData saveData = new AnimalSaveData();
            saveData.animals = new List<AnimalStateData>();

            foreach (var pen in pens)
            {
                foreach (var animal in pen.GetAnimals())
                {
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
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public void LoadAnimalState()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

            string json = PlayerPrefs.GetString(SAVE_KEY);
            AnimalSaveData saveData = JsonUtility.FromJson<AnimalSaveData>(json);

            if (saveData == null || saveData.animals == null) return;

            // Clear existing animals in pens
            foreach (var pen in pens)
            {
                foreach(var a in pen.GetAnimals().ToArray())
                {
                    Destroy(a.gameObject);
                }
                pen.GetAnimals().Clear();
            }

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
