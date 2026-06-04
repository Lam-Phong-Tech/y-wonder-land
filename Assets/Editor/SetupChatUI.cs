using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

public class SetupChatUI : Editor
{
    [MenuItem("Tools/Setup Chat UI")]
    public static void RunSetup()
    {
        // 1. Get the current active scene
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        
        // 2. Find or create the ChatPanel GameObject
        GameObject chatPanelGo = GameObject.Find("ChatPanel");
        if (chatPanelGo == null)
        {
            chatPanelGo = new GameObject("ChatPanel");
            Undo.RegisterCreatedObjectUndo(chatPanelGo, "Create ChatPanel");
            Debug.Log("[SetupChatUI] Created new GameObject: ChatPanel");
        }

        // Parent it under "UI" or keep it root. Let's parent under the "UI" gameobject if exists
        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot != null)
        {
            chatPanelGo.transform.SetParent(uiRoot.transform);
        }

        // 3. Add or Get UIDocument
        UIDocument uiDoc = chatPanelGo.GetComponent<UIDocument>();
        if (uiDoc == null)
        {
            uiDoc = chatPanelGo.AddComponent<UIDocument>();
            Debug.Log("[SetupChatUI] Added UIDocument component");
        }

        // Load PanelSettings
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/PanelSettings.asset");
        if (panelSettings != null)
        {
            uiDoc.panelSettings = panelSettings;
            Debug.Log("[SetupChatUI] Gán PanelSettings thành công!");
        }
        else
        {
            Debug.LogError("[SetupChatUI] Không tìm thấy PanelSettings tại 'Assets/UI Toolkit/PanelSettings.asset'!");
        }

        // Load ChatPanel.uxml
        VisualTreeAsset chatUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/ChatPanel.uxml");
        if (chatUxml != null)
        {
            uiDoc.visualTreeAsset = chatUxml;
            Debug.Log("[SetupChatUI] Gán visualTreeAsset (ChatPanel.uxml) thành công!");
        }
        else
        {
            Debug.LogError("[SetupChatUI] Không tìm thấy ChatPanel.uxml tại 'Assets/UI/ChatPanel.uxml'!");
        }

        // 4. Add or Get ChatPanelController
        ChatPanelController controller = chatPanelGo.GetComponent<ChatPanelController>();
        if (controller == null)
        {
            controller = chatPanelGo.AddComponent<ChatPanelController>();
            Debug.Log("[SetupChatUI] Added ChatPanelController component");
        }

        // Assign serialized field chatDocument via SerializedObject to support private fields
        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty chatDocProperty = serializedController.FindProperty("chatDocument");
        if (chatDocProperty != null)
        {
            chatDocProperty.objectReferenceValue = uiDoc;
            serializedController.ApplyModifiedProperties();
            Debug.Log("[SetupChatUI] Đã kết nối chatDocument vào controller!");
        }
        else
        {
            Debug.LogWarning("[SetupChatUI] Không tìm thấy thuộc tính 'chatDocument' trong controller!");
        }

        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log("[SetupChatUI] ✅ Thiết lập Chat UI thành công!");
    }
}
