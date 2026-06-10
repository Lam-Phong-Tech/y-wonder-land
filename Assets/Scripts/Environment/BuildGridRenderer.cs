using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renders the build grid at runtime using GL.Lines.
/// Only visible when Build Mode is active.
/// Shows white grid lines on the ground plane.
/// </summary>
[RequireComponent(typeof(BuildGridManager))]
public class BuildGridRenderer : MonoBehaviour
{
    [Header("Grid Visual Settings")]
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color borderColor = new Color(0.55f, 0.37f, 0.24f, 0.6f); // Wood brown
    [SerializeField] private float gridYOffset = 0.02f;

    private Material lineMaterial;
    private BuildGridManager gridManager;
    private bool isVisible = false;

    void Start()
    {
        gridManager = GetComponent<BuildGridManager>();
        CreateLineMaterial();
    }

    /// <summary>
    /// Show the grid overlay.
    /// </summary>
    public void Show()
    {
        isVisible = true;
    }

    /// <summary>
    /// Hide the grid overlay.
    /// </summary>
    public void Hide()
    {
        isVisible = false;
    }

    private void CreateLineMaterial()
    {
        // Use Sprites/Default shader which works in both Built-in and URP pipelines
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("[BuildGridRenderer] Could not find Internal-Colored shader!");
            return;
        }

        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        // Turn on alpha blending
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        // Turn off backface culling and depth writes
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
        // Render on top of geometry
        lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
    }

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!isVisible || gridManager == null || lineMaterial == null) return;

        // Only render for the main camera to avoid rendering in scene view etc.
        if (camera == null || camera != Camera.main) return;

        float cellSize = gridManager.CellSize;
        Vector3 origin = gridManager.GridOrigin;
        int width = gridManager.Width;
        int height = gridManager.Height;

        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);

        // Draw inner grid lines
        GL.Begin(GL.LINES);
        GL.Color(gridColor);

        float y = origin.y + gridYOffset;

        // Vertical lines (along Z)
        for (int x = 0; x <= width; x++)
        {
            float xPos = origin.x + x * cellSize;
            GL.Vertex3(xPos, y, origin.z);
            GL.Vertex3(xPos, y, origin.z + height * cellSize);
        }

        // Horizontal lines (along X)
        for (int z = 0; z <= height; z++)
        {
            float zPos = origin.z + z * cellSize;
            GL.Vertex3(origin.x, y, zPos);
            GL.Vertex3(origin.x + width * cellSize, y, zPos);
        }

        GL.End();

        // Draw border (thicker by drawing 2 offset lines)
        GL.Begin(GL.LINES);
        GL.Color(borderColor);

        float bx0 = origin.x;
        float bx1 = origin.x + width * cellSize;
        float bz0 = origin.z;
        float bz1 = origin.z + height * cellSize;
        float by = y + 0.005f;

        // Bottom edge
        GL.Vertex3(bx0, by, bz0);
        GL.Vertex3(bx1, by, bz0);
        // Top edge
        GL.Vertex3(bx0, by, bz1);
        GL.Vertex3(bx1, by, bz1);
        // Left edge
        GL.Vertex3(bx0, by, bz0);
        GL.Vertex3(bx0, by, bz1);
        // Right edge
        GL.Vertex3(bx1, by, bz0);
        GL.Vertex3(bx1, by, bz1);

        GL.End();

        GL.PopMatrix();
    }

    void OnDestroy()
    {
        if (lineMaterial != null)
        {
            DestroyImmediate(lineMaterial);
        }
    }
}
