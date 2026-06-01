using UnityEngine;

public class WaterAnimator : MonoBehaviour
{
    [Header("Scroll Speeds")]
    public float scrollSpeedX = 0.02f;
    public float scrollSpeedY = 0.02f;

    [Header("Secondary Wave Scroll (For Ripple Variation)")]
    public float detailScrollSpeedX = -0.015f;
    public float detailScrollSpeedY = 0.015f;

    private Material waterMat;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Use sharedMaterial to avoid generating instance leaks in Editor mode
            // or use material at runtime.
            waterMat = renderer.material;
        }
        else
        {
            Debug.LogWarning("WaterAnimator requires a Renderer on the GameObject.");
        }
    }

    void Update()
    {
        if (waterMat != null)
        {
            // Scroll primary texture/normal map
            float offsetX = Time.time * scrollSpeedX;
            float offsetY = Time.time * scrollSpeedY;
            
            // Standard URP Lit property names
            if (waterMat.HasProperty("_BaseMap"))
            {
                waterMat.SetTextureOffset("_BaseMap", new Vector2(offsetX, offsetY));
            }
            if (waterMat.HasProperty("_BumpMap"))
            {
                waterMat.SetTextureOffset("_BumpMap", new Vector2(offsetX, offsetY));
            }
        }
    }
}
