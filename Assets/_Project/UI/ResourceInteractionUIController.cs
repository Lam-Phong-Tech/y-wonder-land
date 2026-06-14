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

            // Đặt thanh tiến trình CỐ ĐỊNH ngay dưới tâm ngắm (reticle) cho dễ quan sát khi đang
            // chặt/đập. KHÔNG bám theo vật thể nữa -> tránh trôi/ghim mép màn hình khi cây cao
            // đứng gần. Mắt người chơi đang nhìn tâm ngắm nên đây là chỗ dễ theo dõi nhất.
            if (floatingBarContainer != null)
            {
                const float barW = 200f;   // khớp width trong ResourceInteractionUI.uxml
                floatingBarContainer.style.left = (Screen.width - barW) / 2f;
                floatingBarContainer.style.top = Screen.height / 2f + 40f; // ~40px dưới tâm ngắm
            }
        }

        private void UpdateProgress()
        {
            if (currentTarget != null && progressBarFill != null)
            {
                float dur = currentTarget.harvestDuration > 0f ? currentTarget.harvestDuration : 1f; // tránh chia 0 -> NaN
                float pct = Mathf.Clamp01(currentTarget.currentProgress / dur);
                progressBarFill.style.width = Length.Percent(pct * 100f);
            }
        }
    }
}
