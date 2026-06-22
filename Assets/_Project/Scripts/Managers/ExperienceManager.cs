using System;
using UnityEngine;

namespace YWonderLand.Managers
{
    /// <summary>
    /// Hệ EXP / Level TỐI GIẢN (KeHoach_BanGiao: "Level/EXP tối giản — chỉ cộng điểm").
    /// Cộng EXP khi thu hoạch cây/thú -> đủ ngưỡng thì lên cấp -> bắn overlay + cập nhật HUD.
    /// Tạm dùng PlayerPrefs (sau nối server). TỰ TẠO nếu scene chưa có (khỏi cần gắn Editor).
    /// </summary>
    public class ExperienceManager : MonoBehaviour
    {
        private static ExperienceManager _instance;
        public static ExperienceManager Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying)
                {
                    var go = new GameObject("ExperienceManager");
                    _instance = go.AddComponent<ExperienceManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        /// <summary>(level hiện tại, % tiến tới cấp kế 0..100)</summary>
        public event Action<int, float> OnEXPChanged;

        private int level = 1;
        private int expInLevel = 0; // EXP đã tích trong CẤP hiện tại

        private const string LEVEL_KEY = "YW_Level";
        private const string EXP_KEY = "YW_ExpInLevel";

        public int Level => level;
        public float ExpPercent => Mathf.Clamp01((float)expInLevel / ExpForNext(level)) * 100f;

        // EXP cần để lên cấp kế: ramp nhẹ (cấp 1->2 cần 100, mỗi cấp +50).
        private static int ExpForNext(int lv) => 100 + Mathf.Max(0, lv - 1) * 50;

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            level = Mathf.Max(1, PlayerPrefs.GetInt(LEVEL_KEY, 1));
            expInLevel = Mathf.Max(0, PlayerPrefs.GetInt(EXP_KEY, 0));
        }

        public void AddEXP(int amount)
        {
            if (amount <= 0) return;
            expInLevel += amount;

            bool leveledUp = false;
            // Cộng dồn nhiều cấp nếu EXP đủ.
            while (expInLevel >= ExpForNext(level))
            {
                expInLevel -= ExpForNext(level);
                level++;
                leveledUp = true;
            }

            PlayerPrefs.SetInt(LEVEL_KEY, level);
            PlayerPrefs.SetInt(EXP_KEY, expInLevel);
            PlayerPrefs.Save();

            OnEXPChanged?.Invoke(level, ExpPercent);

            if (leveledUp)
            {
                Debug.Log($"[EXP] LÊN CẤP {level}!");
                var overlay = FindFirstObjectByType<LevelUpOverlayController>();
                if (overlay != null) overlay.Show(level);
            }
            else
            {
                Debug.Log($"[EXP] +{amount} EXP (cấp {level}, {ExpPercent:0}% tới cấp kế).");
            }
        }
    }
}
