using UnityEngine;

namespace YWonderLand.Player
{
    public enum ToolType
    {
        None,
        Axe,
        WateringCan,
        FishingRod,
        Hoe,
        SeedBag
    }

    /// <summary>
    /// Gắn script này vào nhân vật chính. 
    /// Dùng để quản lý việc bật/tắt các nông cụ trên tay nhân vật.
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        public static EquipmentManager Instance { get; private set; }

        [Header("Tool Models (Kéo thả model công cụ vào đây)")]
        [Tooltip("Model cái Rìu gắn trên tay phải")]
        public GameObject axeModel;
        
        [Tooltip("Model cái Bình tưới gắn trên tay phải")]
        public GameObject wateringCanModel;
        
        [Tooltip("Model Cần câu gắn trên tay phải")]
        public GameObject fishingRodModel;
        
        [Tooltip("Model cái Cuốc gắn trên tay phải")]
        public GameObject hoeModel;

        [Tooltip("Model túi hạt giống gắn trên tay trái")]
        public GameObject seedBagModel;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Khởi đầu game thì giấu hết đồ nghề đi
            HideAllTools();
        }

        /// <summary>
        /// Gọi hàm này để hiện một công cụ cụ thể lên tay
        /// </summary>
        public void ShowTool(ToolType toolType)
        {
            HideAllTools(); // Giấu cái cũ trước khi lấy cái mới ra

            switch (toolType)
            {
                case ToolType.Axe:
                    if (axeModel != null) axeModel.SetActive(true);
                    break;
                case ToolType.WateringCan:
                    if (wateringCanModel != null) wateringCanModel.SetActive(true);
                    break;
                case ToolType.FishingRod:
                    if (fishingRodModel != null) fishingRodModel.SetActive(true);
                    break;
                case ToolType.Hoe:
                    if (hoeModel != null) hoeModel.SetActive(true);
                    break;
                case ToolType.SeedBag:
                    if (seedBagModel != null) seedBagModel.SetActive(true);
                    break;
            }
        }

        /// <summary>
        /// Gọi hàm này để cất hết công cụ đi (tay không)
        /// </summary>
        public void HideAllTools()
        {
            if (axeModel != null) axeModel.SetActive(false);
            if (wateringCanModel != null) wateringCanModel.SetActive(false);
            if (fishingRodModel != null) fishingRodModel.SetActive(false);
            if (hoeModel != null) hoeModel.SetActive(false);
            if (seedBagModel != null) seedBagModel.SetActive(false);
        }
    }
}
