using UnityEngine;

namespace YWonderLand.Data
{
    /// <summary>
    /// Định nghĩa cơ bản cho một vật phẩm trong game (ScriptableObject).
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Y WONDER GREEN FARM/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Thông tin cơ bản")]
        public string id;
        public string itemName;
        [TextArea] public string description;
        
        [Header("Hiển thị")]
        public string iconEmoji; // Emoji hiển thị tạm thời (do chưa có ảnh)
        public Sprite iconSprite; // Ảnh hiển thị thực tế
        public Texture2D iconTexture; // Ảnh icon import dạng Texture2D (không cần sửa .meta sang Sprite)
        
        [Header("Phân loại")]
        [Tooltip("tools, materials, seeds, food, outfit, special, animals, items, buildings")]
        public string category; 
        
        [Header("Kinh tế")]
        public int buyPrice;
        public int sellPrice;
        public bool canSell = true;
        
        [Header("Tồn kho")]
        public bool isStackable = true;
        public int maxStack = 999;
    }
}
