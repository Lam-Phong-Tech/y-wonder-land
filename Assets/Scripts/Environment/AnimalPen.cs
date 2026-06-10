using UnityEngine;
using System.Collections.Generic;

namespace YWonderLand.Environment
{
    /// <summary>
    /// AnimalPen represents a single pen holding multiple FarmAnimals.
    /// It manages the physical layout and limits the number of animals per pen.
    /// </summary>
    public class AnimalPen : MonoBehaviour
    {
        [Header("Pen Configuration")]
        public string penId; // Unique identifier for saving
        public int maxCapacity = 3;
        
        [Header("Animal Positioning")]
        public Transform[] spawnPoints; // Predefined points where animals stand

        [Header("State")]
        [SerializeField] private List<FarmAnimal> animals = new List<FarmAnimal>();

        void Awake()
        {
            if (string.IsNullOrEmpty(penId))
            {
                penId = System.Guid.NewGuid().ToString();
            }
        }

        public bool HasSpace()
        {
            return animals.Count < maxCapacity;
        }

        public bool AddAnimal(FarmAnimal animal)
        {
            if (!HasSpace()) return false;

            animals.Add(animal);
            animal.SetPen(this);

            // Assign position
            int idx = animals.Count - 1;
            if (spawnPoints != null && idx < spawnPoints.Length && spawnPoints[idx] != null)
            {
                animal.transform.position = spawnPoints[idx].position;
            }
            else
            {
                // Fallback random position within pen radius
                animal.transform.position = transform.position + new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f));
            }

            return true;
        }

        public void RemoveAnimal(FarmAnimal animal)
        {
            if (animals.Contains(animal))
            {
                animals.Remove(animal);
            }
        }

        public List<FarmAnimal> GetAnimals()
        {
            return animals;
        }
    }
}
