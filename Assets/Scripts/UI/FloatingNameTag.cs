using UnityEngine;
using TMPro;

/// <summary>
/// Displays a floating name tag above a 3D character using TextMeshPro 3D.
/// Style: Minecraft-like — flat white text with black outline, no background.
/// SDF font rendering ensures crisp text at any distance.
/// Only rotation is updated each frame (billboard).
/// </summary>
public class FloatingNameTag : MonoBehaviour
{
    [Header("Name Settings")]
    public string displayName = "Player";
    public Color nameColor = new Color(0.94f, 0.94f, 0.94f, 1f); // #EFEFEF neutral surface
    public float heightOffset = 2.5f;

    [Header("Outline")]
    public Color outlineColor = new Color(0.18f, 0.20f, 0.21f, 1f); // #2D3436 text-dark
    public float outlineWidth = 0.35f; // Thicker for retro Tangible Playground feel

    [Header("Font Size")]
    public float tmpFontSize = 3.5f;

    [Header("Visibility")]
    public float maxVisibleDistance = 30f;

    // Design System color constants
    public static readonly Color COLOR_HERO = new Color(0.357f, 0.259f, 0.953f, 1f);       // #5B42F3
    public static readonly Color COLOR_CONFIRM = new Color(0.176f, 0.482f, 1f, 1f);        // #2D7BFF
    public static readonly Color COLOR_ACHIEVEMENT = new Color(1f, 0.757f, 0.027f, 1f);    // #FFC107
    public static readonly Color COLOR_NEUTRAL = new Color(0.94f, 0.94f, 0.94f, 1f);       // #EFEFEF
    public static readonly Color COLOR_TEXT_DARK = new Color(0.176f, 0.204f, 0.212f, 1f);  // #2D3436
    public static readonly Color COLOR_DANGER = new Color(1f, 0.294f, 0.294f, 1f);         // #FF4B4B

    private TextMeshPro nameText;
    private GameObject nameTagRoot;
    private Camera mainCamera;

    void Start()
    {
        CreateNameTag();
    }

    private void CreateNameTag()
    {
        mainCamera = Camera.main;

        // Root object parented to this character
        nameTagRoot = new GameObject($"NameTag_{displayName}");
        nameTagRoot.transform.SetParent(transform, false);
        nameTagRoot.transform.localPosition = new Vector3(0, heightOffset, 0);

        // TextMeshPro 3D component (NOT TextMeshProUGUI — that's for Canvas)
        nameText = nameTagRoot.AddComponent<TextMeshPro>();
        nameText.text = displayName;
        nameText.fontSize = tmpFontSize;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = nameColor;
        nameText.alignment = TextAlignmentOptions.Center;

        // Prevent frustum culling from hiding text during movement
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Overflow;

        // Outline (Minecraft style — black border around white text)
        nameText.outlineColor = outlineColor;
        nameText.outlineWidth = outlineWidth;

        // Make it render properly in 3D
        nameText.sortingOrder = 100;
        MeshRenderer mr = nameText.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.allowOcclusionWhenDynamic = false; // Prevent occlusion culling

        // Large rect to prevent bounds-based culling
        RectTransform rt = nameText.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(20f, 5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Force initial mesh update
        nameText.ForceMeshUpdate();
    }

    void LateUpdate()
    {
        if (nameTagRoot == null) return;

        // Always refresh camera reference (GameManager swaps cameras between states)
        mainCamera = Camera.main;
        if (mainCamera == null) return;

        // Distance check
        float dist = Vector3.Distance(mainCamera.transform.position, transform.position);
        bool visible = dist <= maxVisibleDistance;
        nameTagRoot.SetActive(visible);
        if (!visible) return;

        // Billboard: always face camera
        nameTagRoot.transform.rotation = mainCamera.transform.rotation;

        // Fixed scale in 3D world — no distance scaling
        // This means: near = readable, far = naturally smaller (like real life)
        nameTagRoot.transform.localScale = Vector3.one;

        // Fade out opacity when far away (reduce visual clutter)
        if (nameText != null)
        {
            float fadeStart = maxVisibleDistance * 0.6f; // Start fading at 60% of max distance
            if (dist > fadeStart)
            {
                float alpha = 1f - ((dist - fadeStart) / (maxVisibleDistance - fadeStart));
                alpha = Mathf.Clamp01(alpha);
                Color c = nameColor;
                c.a = alpha;
                nameText.color = c;
            }
            else
            {
                Color c = nameColor;
                c.a = 1f;
                nameText.color = c;
            }
        }
    }

    /// <summary>
    /// Update the displayed name at runtime.
    /// </summary>
    public void SetName(string newName)
    {
        displayName = newName;
        if (nameText != null) nameText.text = newName;
    }

    /// <summary>
    /// Update the name color at runtime.
    /// </summary>
    public void SetColor(Color color)
    {
        nameColor = color;
        if (nameText != null) nameText.color = color;
    }

    void OnDestroy()
    {
        if (nameTagRoot != null) Destroy(nameTagRoot);
    }
}
