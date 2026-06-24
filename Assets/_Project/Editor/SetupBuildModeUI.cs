using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

public class SetupBuildModeUI : Editor
{
    [MenuItem("Tools/Setup Build Mode UI")]
    public static void RunSetup()
    {
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        // 1. Find or create the BuildModeOverlay GameObject
        GameObject buildGo = GameObject.Find("BuildModeOverlay");
        if (buildGo == null)
        {
            buildGo = new GameObject("BuildModeOverlay");
            Undo.RegisterCreatedObjectUndo(buildGo, "Create BuildModeOverlay");
            Debug.Log("[SetupBuildModeUI] Created new GameObject: BuildModeOverlay");
        }

        // Parent under UI root if exists
        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot != null)
        {
            buildGo.transform.SetParent(uiRoot.transform);
            Debug.Log("[SetupBuildModeUI] Parented under UI root");
        }

        // 2. Add or Get UIDocument
        UIDocument uiDoc = buildGo.GetComponent<UIDocument>();
        if (uiDoc == null)
        {
            uiDoc = buildGo.AddComponent<UIDocument>();
            Debug.Log("[SetupBuildModeUI] Added UIDocument component");
        }

        // Load PanelSettings
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/PanelSettings.asset");
        if (panelSettings != null)
        {
            uiDoc.panelSettings = panelSettings;
            Debug.Log("[SetupBuildModeUI] Assigned PanelSettings!");
        }
        else
        {
            Debug.LogError("[SetupBuildModeUI] Could not find PanelSettings!");
        }

        // Load BuildModeOverlay.uxml
        VisualTreeAsset buildUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BuildModeOverlay.uxml");
        if (buildUxml != null)
        {
            uiDoc.visualTreeAsset = buildUxml;
            Debug.Log("[SetupBuildModeUI] Assigned visualTreeAsset (BuildModeOverlay.uxml)!");
        }
        else
        {
            Debug.LogError("[SetupBuildModeUI] Could not find BuildModeOverlay.uxml!");
        }

        // 3. Add or Get BuildModeOverlayController
        BuildModeOverlayController controller = buildGo.GetComponent<BuildModeOverlayController>();
        if (controller == null)
        {
            controller = buildGo.AddComponent<BuildModeOverlayController>();
            Debug.Log("[SetupBuildModeUI] Added BuildModeOverlayController component");
        }

        // Connect buildDocument reference
        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty docProp = serializedController.FindProperty("buildDocument");
        if (docProp != null)
        {
            docProp.objectReferenceValue = uiDoc;
            Debug.Log("[SetupBuildModeUI] Connected buildDocument reference!");
        }
        serializedController.ApplyModifiedProperties();

        // 4. Connect buildModeOverlay reference in GameHUDController
        GameHUDController hudController = FindFirstObjectByType<GameHUDController>();
        if (hudController != null)
        {
            SerializedObject serializedHUD = new SerializedObject(hudController);
            SerializedProperty buildProp = serializedHUD.FindProperty("buildModeOverlay");
            if (buildProp != null)
            {
                buildProp.objectReferenceValue = controller;
                serializedHUD.ApplyModifiedProperties();
                Debug.Log("[SetupBuildModeUI] Connected buildModeOverlay in GameHUDController!");
            }
        }

        // 5. Setup 3D Build System objects
        // -- BuildSystem root --
        GameObject buildSystemRoot = GameObject.Find("BuildSystem");
        if (buildSystemRoot == null)
        {
            buildSystemRoot = new GameObject("BuildSystem");
            Undo.RegisterCreatedObjectUndo(buildSystemRoot, "Create BuildSystem");
            Debug.Log("[SetupBuildModeUI] Created BuildSystem root");
        }

        // -- BuildGridManager --
        BuildGridManager gridManager = FindFirstObjectByType<BuildGridManager>();
        if (gridManager == null)
        {
            GameObject gridGo = new GameObject("BuildGridManager");
            gridGo.transform.SetParent(buildSystemRoot.transform);
            gridManager = gridGo.AddComponent<BuildGridManager>();
            Undo.RegisterCreatedObjectUndo(gridGo, "Create BuildGridManager");
            Debug.Log("[SetupBuildModeUI] Created BuildGridManager");
        }

        // -- BuildGridRenderer (runtime grid visualization, same GO) --
        if (gridManager.GetComponent<BuildGridRenderer>() == null)
        {
            gridManager.gameObject.AddComponent<BuildGridRenderer>();
            Debug.Log("[SetupBuildModeUI] Added BuildGridRenderer");
        }

        // -- GhostPlacementController --
        GhostPlacementController ghostController = FindFirstObjectByType<GhostPlacementController>();
        if (ghostController == null)
        {
            GameObject ghostGo = new GameObject("GhostPlacementController");
            ghostGo.transform.SetParent(buildSystemRoot.transform);
            ghostController = ghostGo.AddComponent<GhostPlacementController>();
            Undo.RegisterCreatedObjectUndo(ghostGo, "Create GhostPlacementController");
            Debug.Log("[SetupBuildModeUI] Created GhostPlacementController");
        }

        // -- BuildCameraController --
        BuildCameraController buildCam = FindFirstObjectByType<BuildCameraController>();
        if (buildCam == null)
        {
            GameObject camGo = new GameObject("BuildCameraController");
            camGo.transform.SetParent(buildSystemRoot.transform);
            buildCam = camGo.AddComponent<BuildCameraController>();
            Undo.RegisterCreatedObjectUndo(camGo, "Create BuildCameraController");
            Debug.Log("[SetupBuildModeUI] Created BuildCameraController");
        }

        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log("[SetupBuildModeUI] Setup Build Mode UI + 3D System completed successfully!");
    }
}
