using UnityEngine;
using System;
using YWonderLand.Data;

namespace YWonderLand.Environment
{
    public class FarmAnimal : MonoBehaviour
    {
        public enum AnimalState
        {
            Healthy = 0,
            Hungry = 1,
            Sick = 2,
            Dead = 3
        }

        [Header("Identity")]
        public string animalInstanceId; // For saving
        public AnimalDefinition data;
        
        [Header("State")]
        public AnimalState currentState = AnimalState.Healthy;
        public float feedTimer = 0f;
        public float produceTimer = 0f;
        public int harvestsRemaining;
        public bool hasProductReady = false;
        public bool isVaccinated = false;

        private AnimalPen currentPen;

        // Visuals
        private GameObject visualObject;
        private GameObject productIndicator;
        private GameObject sickIndicator;
        private GameObject hungryIndicator;

        public event Action<FarmAnimal> OnAnimalStateChanged;

        void Awake()
        {
            if (string.IsNullOrEmpty(animalInstanceId))
            {
                animalInstanceId = Guid.NewGuid().ToString();
            }
        }

        public void Initialize(AnimalDefinition def)
        {
            data = def;
            harvestsRemaining = def.maxHarvests;
            currentState = AnimalState.Healthy;
            feedTimer = 0f;
            produceTimer = 0f;
            hasProductReady = false;
            isVaccinated = false;

            CreateVisuals();
            UpdateVisuals();
        }

        public void SetPen(AnimalPen pen)
        {
            currentPen = pen;
        }

        void Update()
        {
            if (data == null || currentState == AnimalState.Dead) return;

            // Produce logic
            if (currentState != AnimalState.Sick && !hasProductReady && harvestsRemaining > 0)
            {
                produceTimer += Time.deltaTime;
                if (produceTimer >= data.produceCycleTimeSec)
                {
                    hasProductReady = true;
                    produceTimer = 0f;
                    UpdateVisuals();
                    OnAnimalStateChanged?.Invoke(this);
                }
            }

            // Feed logic
            feedTimer += Time.deltaTime;
            
            // Becomes hungry after feedInterval
            if (feedTimer >= data.feedIntervalSec && currentState == AnimalState.Healthy)
            {
                currentState = AnimalState.Hungry;
                UpdateVisuals();
                OnAnimalStateChanged?.Invoke(this);
            }
            
            // Becomes sick if hungry for too long (e.g. 1.5x interval)
            if (feedTimer >= data.feedIntervalSec * 1.5f && currentState == AnimalState.Hungry)
            {
                currentState = AnimalState.Sick;
                UpdateVisuals();
                OnAnimalStateChanged?.Invoke(this);
            }

            // Dies if sick for too long
            if (feedTimer >= data.feedIntervalSec * 2.5f && currentState == AnimalState.Sick)
            {
                currentState = AnimalState.Dead;
                UpdateVisuals();
                OnAnimalStateChanged?.Invoke(this);
            }
        }

        // ── Interactions ──

        public bool Feed()
        {
            if (currentState == AnimalState.Dead) return false;
            
            feedTimer = 0f;
            if (currentState == AnimalState.Hungry)
            {
                currentState = AnimalState.Healthy;
            }
            UpdateVisuals();
            OnAnimalStateChanged?.Invoke(this);
            return true;
        }

        public bool HarvestProduct(out string itemId, out int amount)
        {
            itemId = "";
            amount = 0;

            if (!hasProductReady || currentState == AnimalState.Dead) return false;

            itemId = data.produceItemId;
            amount = data.produceAmount;
            
            hasProductReady = false;
            harvestsRemaining--;
            
            UpdateVisuals();
            OnAnimalStateChanged?.Invoke(this);
            return true;
        }

        public bool Heal()
        {
            if (currentState != AnimalState.Sick) return false;

            currentState = AnimalState.Healthy;
            feedTimer = 0f; // Healing also resets hunger temporarily
            isVaccinated = true;
            
            UpdateVisuals();
            OnAnimalStateChanged?.Invoke(this);
            return true;
        }

        // ── Visual Fallbacks ──

        private void CreateVisuals()
        {
            if (visualObject != null) return;

            visualObject = new GameObject("Visuals");
            visualObject.transform.SetParent(this.transform, false);

            GameObject body = null;
            Color bodyColor = Color.white;

            // Fallback shapes based on ID
            if (data.animalId.Contains("chicken"))
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                bodyColor = Color.white;
            }
            else if (data.animalId.Contains("cow"))
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.localScale = new Vector3(1.5f, 1f, 1f);
                bodyColor = new Color(0.9f, 0.9f, 0.9f); // Off-white
            }
            else if (data.animalId.Contains("pig"))
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.transform.localScale = new Vector3(1f, 0.8f, 1f);
                bodyColor = new Color(1f, 0.7f, 0.8f); // Pink
            }
            else
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }

            body.transform.SetParent(visualObject.transform, false);
            body.transform.localPosition = new Vector3(0, 0.5f, 0);

            // Setup color
            Renderer r = body.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                r.material.color = bodyColor;
            }
            
            // Add collider for raycast
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.5f, 0);
            col.size = new Vector3(1.5f, 1.5f, 1.5f);
            Destroy(body.GetComponent<Collider>());

            // Status Indicators
            productIndicator = CreateIndicator(Color.yellow, new Vector3(0, 1.5f, 0), "ProductReady");
            hungryIndicator = CreateIndicator(new Color(1f, 0.5f, 0f), new Vector3(-0.5f, 1.5f, 0), "Hungry");
            sickIndicator = CreateIndicator(Color.green, new Vector3(0.5f, 1.5f, 0), "Sick");
        }

        private GameObject CreateIndicator(Color color, Vector3 pos, string name)
        {
            GameObject ind = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ind.name = name;
            ind.transform.SetParent(visualObject.transform, false);
            ind.transform.localPosition = pos;
            ind.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            
            Renderer r = ind.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            r.material.color = color;
            r.material.EnableKeyword("_EMISSION");
            r.material.SetColor("_EmissionColor", color * 2f);
            
            Destroy(ind.GetComponent<Collider>());
            ind.SetActive(false);
            return ind;
        }

        public void UpdateVisuals()
        {
            if (visualObject == null) return;

            productIndicator?.SetActive(hasProductReady && currentState != AnimalState.Dead);
            hungryIndicator?.SetActive(currentState == AnimalState.Hungry);
            sickIndicator?.SetActive(currentState == AnimalState.Sick || currentState == AnimalState.Dead);

            if (currentState == AnimalState.Dead)
            {
                Renderer r = sickIndicator.GetComponent<Renderer>();
                if (r != null) r.material.color = Color.black; // Dead
                
                // Tip over
                visualObject.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                visualObject.transform.localPosition = new Vector3(0.5f, -0.4f, 0);
            }
            else
            {
                visualObject.transform.localRotation = Quaternion.identity;
                visualObject.transform.localPosition = Vector3.zero;
            }
        }
        
        // Load state method
        public void LoadState(AnimalState state, float fTimer, float pTimer, int harvests, bool ready, bool vacc)
        {
            currentState = state;
            feedTimer = fTimer;
            produceTimer = pTimer;
            harvestsRemaining = harvests;
            hasProductReady = ready;
            isVaccinated = vacc;
            
            if (visualObject == null) CreateVisuals();
            UpdateVisuals();
        }
    }
}
