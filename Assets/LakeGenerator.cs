using UnityEngine;

/// <summary>
/// Generates a circular/elliptical lake mesh at runtime.
/// Attach this to an empty GameObject positioned where you want the lake center.
/// Assign a water material in the Inspector (or it will try to find one automatically).
/// </summary>
public class LakeGenerator : MonoBehaviour
{
    [Header("Lake Shape")]
    [Tooltip("Radius of the lake on the X axis (meters)")]
    public float radiusX = 15f;

    [Tooltip("Radius of the lake on the Z axis (meters). Set equal to radiusX for a perfect circle.")]
    public float radiusZ = 12f;

    [Tooltip("Number of edge vertices. Higher = smoother circle. 64 is plenty.")]
    [Range(16, 128)]
    public int segments = 64;

    [Header("Water Material")]
    [Tooltip("Drag your water material here. If left empty, will search project for a material named 'water'.")]
    public Material waterMaterial;

    [Header("Wave Animation")]
    [Tooltip("Enable gentle vertex wave animation")]
    public bool enableWaves = true;

    [Tooltip("Wave height (amplitude)")]
    public float waveHeight = 0.08f;

    [Tooltip("Wave speed")]
    public float waveSpeed = 1.5f;

    [Tooltip("Wave frequency (how many waves across the surface)")]
    public float waveFrequency = 0.5f;

    private Mesh lakeMesh;
    private Vector3[] baseVertices;
    private Vector3[] animatedVertices;

    void Start()
    {
        GenerateLake();
    }

    void Update()
    {
        if (enableWaves && lakeMesh != null && baseVertices != null)
        {
            AnimateWaves();
        }
    }

    void GenerateLake()
    {
        // Create child object for the lake mesh
        GameObject lakeObj = new GameObject("LakeMesh");
        lakeObj.transform.SetParent(transform, false);
        lakeObj.transform.localPosition = Vector3.zero;
        lakeObj.transform.localRotation = Quaternion.identity;

        // Add mesh components
        MeshFilter meshFilter = lakeObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = lakeObj.AddComponent<MeshRenderer>();

        // Try to find water material if not assigned
        if (waterMaterial == null)
        {
            // Search for any material with "water" in the name
            waterMaterial = FindWaterMaterial();
        }

        if (waterMaterial != null)
        {
            meshRenderer.material = waterMaterial;
        }
        else
        {
            // Fallback: create a simple semi-transparent blue material
            Material fallback = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            fallback.SetColor("_BaseColor", new Color(0.1f, 0.4f, 0.65f, 0.75f));
            fallback.SetFloat("_Surface", 1); // Transparent
            fallback.SetFloat("_Blend", 0);   // Alpha
            fallback.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            fallback.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            fallback.SetFloat("_ZWrite", 0);
            fallback.renderQueue = 3000;
            fallback.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            meshRenderer.material = fallback;
            Debug.LogWarning("[LakeGenerator] No water material found. Using fallback transparent blue.");
        }

        // Disable shadow casting for a flat water surface
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = true;

        // Generate circular mesh
        lakeMesh = CreateCircleMesh(radiusX, radiusZ, segments);
        meshFilter.mesh = lakeMesh;

        // Cache vertices for wave animation
        baseVertices = lakeMesh.vertices;
        animatedVertices = new Vector3[baseVertices.Length];
        System.Array.Copy(baseVertices, animatedVertices, baseVertices.Length);

        Debug.Log($"[LakeGenerator] Lake created at {transform.position} with radius ({radiusX}, {radiusZ}), {segments} segments.");
    }

    Mesh CreateCircleMesh(float rX, float rZ, int numSegments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "LakeMesh";

        int vertCount = numSegments + 1; // +1 for center
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] triangles = new int[numSegments * 3];

        // Center vertex
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        // Edge vertices
        float angleStep = 2f * Mathf.PI / numSegments;
        for (int i = 0; i < numSegments; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle) * rX;
            float z = Mathf.Sin(angle) * rZ;
            vertices[i + 1] = new Vector3(x, 0f, z);

            // UV mapping: map to 0-1 range
            uvs[i + 1] = new Vector2(
                Mathf.Cos(angle) * 0.5f + 0.5f,
                Mathf.Sin(angle) * 0.5f + 0.5f
            );
        }

        // Triangles (fan from center)
        for (int i = 0; i < numSegments; i++)
        {
            int triIdx = i * 3;
            triangles[triIdx] = 0;                                        // center
            triangles[triIdx + 1] = i + 1;                               // current edge
            triangles[triIdx + 2] = (i + 1) % numSegments + 1;           // next edge (wraps around)
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    void AnimateWaves()
    {
        float time = Time.time * waveSpeed;

        for (int i = 1; i < baseVertices.Length; i++) // Skip center vertex (index 0)
        {
            Vector3 v = baseVertices[i];

            // Two overlapping sine waves for natural look
            float wave1 = Mathf.Sin(v.x * waveFrequency + time) * waveHeight;
            float wave2 = Mathf.Sin(v.z * waveFrequency * 1.3f + time * 0.8f) * waveHeight * 0.5f;

            animatedVertices[i] = new Vector3(v.x, v.y + wave1 + wave2, v.z);
        }

        // Center vertex gets a gentle bob
        animatedVertices[0] = baseVertices[0] + Vector3.up * Mathf.Sin(time * 0.7f) * waveHeight * 0.3f;

        lakeMesh.vertices = animatedVertices;
        lakeMesh.RecalculateNormals();
    }

    Material FindWaterMaterial()
    {
        // Try to load from known paths in the project
        string[] paths = {
            "Assets/Environment/Materials/water",
            "Assets/Environment/Material/Water",
            "Assets/Environment/Materials/WaterNormal"
        };

        foreach (string path in paths)
        {
            Material mat = Resources.Load<Material>(path);
            if (mat != null) return mat;
        }

        return null;
    }

    // Helper gizmo to visualize the lake area in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;

        // Draw ellipse outline
        int steps = 36;
        float angleStep = 2f * Mathf.PI / steps;
        for (int i = 0; i < steps; i++)
        {
            float a1 = i * angleStep;
            float a2 = (i + 1) * angleStep;
            Vector3 p1 = new Vector3(Mathf.Cos(a1) * radiusX, 0, Mathf.Sin(a1) * radiusZ);
            Vector3 p2 = new Vector3(Mathf.Cos(a2) * radiusX, 0, Mathf.Sin(a2) * radiusZ);
            Gizmos.DrawLine(p1, p2);
        }

        // Draw fill disc
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.15f);
        Gizmos.DrawSphere(Vector3.zero, (radiusX + radiusZ) / 2f);
    }
}
