using UnityEngine;
using UnityEngine.UIElements;
using YWonderLand.Environment;

namespace YWonderLand.UI
{
    public class ResourceInteractionUIController : MonoBehaviour
    {
        public static ResourceInteractionUIController Instance { get; private set; }

        private UIDocument document;
        private VisualElement container;
        private VisualElement floatingBarContainer;
        private VisualElement progressBarFill;

        private HarvestableResource currentTarget;
        private Camera mainCamera;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void OnEnable()
        {
            document = GetComponent<UIDocument>();
            if (document == null || document.rootVisualElement == null) return;

            var root = document.rootVisualElement;
            container = root.Q<VisualElement>("ResourceInteractionContainer");
            floatingBarContainer = root.Q<VisualElement>("FloatingBarContainer");
            progressBarFill = root.Q<VisualElement>("ProgressBarFill");

            mainCamera = Camera.main;

            Hide();
        }

        public void Show(HarvestableResource target)
        {
            if (container == null) return;
            currentTarget = target;
            container.style.display = DisplayStyle.Flex;
            UpdateProgress();
        }

        public void Hide()
        {
            if (container == null) return;
            currentTarget = null;
            container.style.display = DisplayStyle.None;
        }

        void Update()
        {
            if (currentTarget == null || container.style.display == DisplayStyle.None)
                return;

            // Update UI Progress
            UpdateProgress();

            // Follow 3D Target
            if (mainCamera != null && floatingBarContainer != null)
            {
                // Mặc định cây cao 4m, đá cao 1.5m -> tính điểm neo UI ở trên đỉnh
                float heightOffset = currentTarget.type == HarvestableResource.ResourceType.Tree ? 4.5f : 2.0f;
                Vector3 worldPos = currentTarget.transform.position + Vector3.up * heightOffset;
                
                Vector2 screenPos = mainCamera.WorldToScreenPoint(worldPos);
                
                // Screen space (bottom-left = 0,0) to Panel space (top-left = 0,0)
                screenPos.y = Screen.height - screenPos.y;
                
                // Căn giữa UI (width = 120, height = 24)
                floatingBarContainer.style.left = screenPos.x - 60f;
                floatingBarContainer.style.top = screenPos.y - 12f;
            }
        }

        private void UpdateProgress()
        {
            if (currentTarget != null && progressBarFill != null)
            {
                float pct = Mathf.Clamp01(currentTarget.currentProgress / currentTarget.harvestDuration);
                progressBarFill.style.width = Length.Percent(pct * 100f);
            }
        }
    }
}
