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

        /// <summary>Ô đã có công trình chưa.</summary>
        public bool IsOccupied { get; private set; }

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

        public void SetOccupied(bool occupied) => IsOccupied = occupied;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }
    }
}
