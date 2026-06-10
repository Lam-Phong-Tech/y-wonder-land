using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

public class SetupFishingUI : Editor
{
    [MenuItem("Tools/Setup Fishing UI")]
    public static void RunSetup()
    {
        // 1. Get the current active scene
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        
        // 2. Find or create the FishingOverlay GameObject
        GameObject fishingGo = GameObject.Find("FishingOverlay");
        if (fishingGo == null)
        {
            fishingGo = new GameObject("FishingOverlay");
            Undo.RegisterCreatedObjectUndo(fishingGo, "Create FishingOverlay");
            Debug.Log("[SetupFishingUI] Created new GameObject: FishingOverlay");
        }

        // Parent it under "UI" if it exists
        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot != null)
        {
            fishingGo.transform.SetParent(uiRoot.transform);
        }

        // 3. Add or Get UIDocument
        UIDocument uiDoc = fishingGo.GetComponent<UIDocument>();
        if (uiDoc == null)
        {
            uiDoc = fishingGo.AddComponent<UIDocument>();
            Debug.Log("[SetupFishingUI] Added UIDocument component");
        }

        // Load PanelSettings
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/PanelSettings.asset");
        if (panelSettings != null)
        {
            uiDoc.panelSettings = panelSettings;
            Debug.Log("[SetupFishingUI] Gán PanelSettings thành công!");
        }
        else
        {
            Debug.LogError("[SetupFishingUI] Không tìm thấy PanelSettings tại 'Assets/UI Toolkit/PanelSettings.asset'!");
        }

        // Load FishingOverlay.uxml
        VisualTreeAsset fishingUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/FishingOverlay.uxml");
        if (fishingUxml != null)
        {
            uiDoc.visualTreeAsset = fishingUxml;
            Debug.Log("[SetupFishingUI] Gán visualTreeAsset (FishingOverlay.uxml) thành công!");
        }
        else
        {
            Debug.LogError("[SetupFishingUI] Không tìm thấy FishingOverlay.uxml tại 'Assets/UI/FishingOverlay.uxml'!");
        }

        // 4. Add or Get FishingOverlayController
        FishingOverlayController controller = fishingGo.GetComponent<FishingOverlayController>();
        if (controller == null)
        {
            controller = fishingGo.AddComponent<FishingOverlayController>();
            Debug.Log("[SetupFishingUI] Added FishingOverlayController component");
        }

        // Connect fishingDocument reference in controller
        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty docProperty = serializedController.FindProperty("fishingDocument");
        if (docProperty != null)
        {
            docProperty.objectReferenceValue = uiDoc;
            Debug.Log("[SetupFishingUI] Đã kết nối fishingDocument vào controller!");
        }

        // Connect confirmDialog reference if ConfirmDialog GameObject exists
        ConfirmDialogController confirmController = FindFirstObjectByType<ConfirmDialogController>();
        if (confirmController != null)
        {
            SerializedProperty confirmProperty = serializedController.FindProperty("confirmDialog");
            if (confirmProperty != null)
            {
                confirmProperty.objectReferenceValue = confirmController;
                Debug.Log("[SetupFishingUI] Đã kết nối confirmDialog vào controller!");
            }
        }
        else
        {
            Debug.LogWarning("[SetupFishingUI] Không tìm thấy ConfirmDialogController trong scene!");
        }

        serializedController.ApplyModifiedProperties();

        // 5. Connect fishingOverlay reference in GameHUDController
        GameHUDController hudController = FindFirstObjectByType<GameHUDController>();
        if (hudController != null)
        {
            SerializedObject serializedHUD = new SerializedObject(hudController);
            SerializedProperty fishOverlayProperty = serializedHUD.FindProperty("fishingOverlay");
            if (fishOverlayProperty != null)
            {
                fishOverlayProperty.objectReferenceValue = controller;
                serializedHUD.ApplyModifiedProperties();
                Debug.Log("[SetupFishingUI] Đã kết nối fishingOverlay vào GameHUDController!");
            }
        }

        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log("[SetupFishingUI] ✅ Thiết lập Fishing UI thành công!");
    }
}
