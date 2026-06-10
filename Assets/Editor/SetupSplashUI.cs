using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

public class SetupSplashUI : Editor
{
    [MenuItem("Tools/Setup Splash UI")]
    public static void RunSetup()
    {
        // 1. Get the current active scene
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        // 2. Find or create the SplashLoadingScreen GameObject
        GameObject splashGo = GameObject.Find("SplashLoadingScreen");
        if (splashGo == null)
        {
            splashGo = new GameObject("SplashLoadingScreen");
            Undo.RegisterCreatedObjectUndo(splashGo, "Create SplashLoadingScreen");
            Debug.Log("[SetupSplashUI] Created new GameObject: SplashLoadingScreen");
        }

        // Parent it under "UI" if it exists
        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot != null)
        {
            splashGo.transform.SetParent(uiRoot.transform);
            Debug.Log("[SetupSplashUI] Parented under UI root");
        }

        // 3. Add or Get UIDocument
        UIDocument uiDoc = splashGo.GetComponent<UIDocument>();
        if (uiDoc == null)
        {
            uiDoc = splashGo.AddComponent<UIDocument>();
            Debug.Log("[SetupSplashUI] Added UIDocument component");
        }

        // Load PanelSettings
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/PanelSettings.asset");
        if (panelSettings != null)
        {
            uiDoc.panelSettings = panelSettings;
            Debug.Log("[SetupSplashUI] Assigned PanelSettings successfully!");
        }
        else
        {
            Debug.LogError("[SetupSplashUI] Could not find PanelSettings at 'Assets/UI Toolkit/PanelSettings.asset'!");
        }

        // Load SplashLoadingScreen.uxml
        VisualTreeAsset splashUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/SplashLoadingScreen.uxml");
        if (splashUxml != null)
        {
            uiDoc.visualTreeAsset = splashUxml;
            Debug.Log("[SetupSplashUI] Assigned visualTreeAsset (SplashLoadingScreen.uxml) successfully!");
        }
        else
        {
            Debug.LogError("[SetupSplashUI] Could not find SplashLoadingScreen.uxml at 'Assets/UI/SplashLoadingScreen.uxml'!");
        }

        // Set Sort Order = 10 to render above Login Screen
        uiDoc.sortingOrder = 10;
        Debug.Log("[SetupSplashUI] Set UIDocument Sort Order = 10");

        // 4. Add or Get SplashLoadingController
        SplashLoadingController controller = splashGo.GetComponent<SplashLoadingController>();
        if (controller == null)
        {
            controller = splashGo.AddComponent<SplashLoadingController>();
            Debug.Log("[SetupSplashUI] Added SplashLoadingController component");
        }

        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log("[SetupSplashUI] Setup Splash UI completed successfully!");
    }
}
