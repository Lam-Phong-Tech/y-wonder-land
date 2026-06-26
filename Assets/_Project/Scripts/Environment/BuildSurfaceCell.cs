using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Đánh dấu 1 khối cube là Ô ĐẤT XÂY DỰNG. Ghost build snap vào TÂM MẶT TRÊN của khối này,
    /// nhờ vậy hàng rào/công trình luôn khít đúng tâm khối — không lệch như lưới ảo.
    /// Mỗi ô tự lưu trạng thái trống/đã chiếm. Gắn vào GameObject có Collider (khối cube của map).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BuildSurfaceCell : MonoBehaviour
    {
        /// <summary>Danh sách mọi ô đất đang bật trong scene (đăng ký lúc OnEnable) — để quét/highlight.</summary>
        public static readonly List<BuildSurfaceCell> All = new List<BuildSurfaceCell>();

        /// <summary>Vật đang đặt trên ô (hàng rào / công trình / con vật). Null = ô trống.</summary>
        public GameObject Occupant { get; private set; }

        /// <summary>Ô đã có gì đó (rào/công trình/thú) chưa.</summary>
        public bool IsOccupied => Occupant != null;

        /// <summary>Ô trống hoàn toàn (để flood-fill đếm sức chứa).</summary>
        public bool IsFree => Occupant == null;

        /// <summary>Ô này có HÀNG RÀO (= 1 ô chuồng).</summary>
        public bool HasFence => Occupant != null && Occupant.GetComponentInChildren<FenceAutoConnect>() != null;

        /// <summary>Ô chuồng này đã có con vật đứng chưa.</summary>
        public bool HasAnimal { get; private set; }
        public void SetAnimal(bool v) => HasAnimal = v;

        /// <summary>Con vật ĐANG NEO tại ô này (chỉ ô đầu của cụm penSlots giữ tham chiếu; null nếu ô chỉ "mượn chỗ").</summary>
        public GameObject AnimalObject { get; private set; }

        /// <summary>itemId con giống đang neo tại ô — để trả con giống về túi khi phá chuồng.</summary>
        public string AnimalItemId { get; private set; }

        /// <summary>Đánh dấu ô là ô NEO của 1 con vật vừa thả (giữ tham chiếu GameObject + itemId để hoàn khi phá).</summary>
        public void SetAnimalOccupant(GameObject go, string itemId)
        {
            AnimalObject = go;
            AnimalItemId = itemId;
            HasAnimal = true;
        }

        /// <summary>Trả ô chuồng về trạng thái không có thú.</summary>
        public void ClearAnimal()
        {
            AnimalObject = null;
            AnimalItemId = null;
            HasAnimal = false;
        }

        /// <summary>SỐ LƯỢNG vật liệu đã tốn để xây vật đang chiếm ô — dùng hoàn lại khi phá chuồng/công trình.</summary>
        public int BuildCost { get; private set; }
        /// <summary>ID vật liệu đã tốn (wood_01/stone_01...). Rỗng = miễn phí (vd ô ruộng).</summary>
        public string BuildMaterialId { get; private set; }
        public void SetBuildCost(int cost) => BuildCost = cost; // giữ tương thích cũ
        public void SetBuildMaterial(string materialId, int amount) { BuildMaterialId = materialId; BuildCost = amount; }

        private Collider _col;
        private Collider Col => _col != null ? _col : (_col = GetComponent<Collider>());

        /// <summary>Tâm MẶT TRÊN của khối (XZ tâm, Y đỉnh) — nơi ghost ướm vào.</summary>
        public Vector3 SurfaceCenter
        {
            get
            {
                Bounds b = Col.bounds;
                return new Vector3(b.center.x, b.max.y, b.center.z);
            }
        }

        /// <summary>Bề rộng/sâu (XZ) của ô — để co giãn hàng rào khít khối (vd 0.8 x 0.8).</summary>
        public Vector2 FootprintSize
        {
            get { Bounds b = Col.bounds; return new Vector2(b.size.x, b.size.z); }
        }

        /// <summary>Gán vật chiếm ô (rào/công trình/thú). Truyền null để trả ô về trống.</summary>
        public void SetOccupant(GameObject go) => Occupant = go;

        /// <summary>Trả ô về trống.</summary>
        public void Clear() => Occupant = null;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        // Hiển thị ô đất trong Scene view: XANH = ô trống đã tag, ĐỎ = ô có rào, VÀNG = ô bị chiếm khác.
        void OnDrawGizmos()
        {
            if (Col == null) return;
            Bounds b = Col.bounds;
            Gizmos.color = HasAnimal ? new Color(0.3f, 0.7f, 1f, 0.9f)            // xanh dương = ô chuồng có thú
                : HasFence ? new Color(1f, 0.3f, 0.3f, 0.85f)                    // đỏ = ô chuồng (rào) trống
                : (IsOccupied ? new Color(1f, 0.9f, 0.3f, 0.7f)                  // vàng = ô bị chiếm khác
                : new Color(0.3f, 1f, 0.45f, 0.5f));                             // xanh lá = ô đất trống
            Gizmos.DrawWireCube(new Vector3(b.center.x, b.max.y, b.center.z),
                new Vector3(b.size.x * 0.9f, 0.02f, b.size.z * 0.9f));
        }
    }
}
