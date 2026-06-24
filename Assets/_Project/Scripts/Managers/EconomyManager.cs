using System;
using UnityEngine;

namespace YWonderLand.Managers
{
    /// <summary>
    /// Quản lý tiền tệ (Soft/Premium Currency).
    /// Tạm thời sử dụng PlayerPrefs. Sau này sẽ thay thế bằng UGS Economy.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        public event Action<long> OnPOSChanged;
        // public event Action<long> OnUPOSChanged;

        private long currentPOS;
        private long currentUPOS;

        private const string POS_KEY = "YW_POS_Balance";
        private const string UPOS_KEY = "YW_UPOS_Balance";

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadBalances();
            }
            else if (Instance != this)
            {
                // Chỉ huỷ COMPONENT trùng, KHÔNG Destroy(gameObject) (tránh huỷ nhầm GameObject nếu gắn chung GameManager).
                Destroy(this);
            }
        }

        private void LoadBalances()
        {
            // Nếu chưa có data, tặng 5000 POS làm vốn khởi nghiệp
            currentPOS = PlayerPrefs.GetInt(POS_KEY, 5000); 
            currentUPOS = PlayerPrefs.GetInt(UPOS_KEY, 0);
        }

        private void SaveBalances()
        {
            PlayerPrefs.SetInt(POS_KEY, (int)currentPOS);
            PlayerPrefs.SetInt(UPOS_KEY, (int)currentUPOS);
            PlayerPrefs.Save();
        }

        public long GetPOS() => currentPOS;
        public long GetUPOS() => currentUPOS;

        public void AddPOS(long amount)
        {
            if (amount <= 0) return;
            currentPOS += amount;
            SaveBalances();
            OnPOSChanged?.Invoke(currentPOS);
            Debug.Log($"[Economy] Add {amount} POS. Balance: {currentPOS}");
        }

        public bool SpendPOS(long amount)
        {
            if (amount <= 0) return true;
            if (currentPOS >= amount)
            {
                currentPOS -= amount;
                SaveBalances();
                OnPOSChanged?.Invoke(currentPOS);
                Debug.Log($"[Economy] Spend {amount} POS. Balance: {currentPOS}");
                return true;
            }
            Debug.LogWarning($"[Economy] Not enough POS! Needed: {amount}, Have: {currentPOS}");
            return false;
        }
        
        public bool CanAffordPOS(long amount)
        {
            return currentPOS >= amount;
        }
    }
}
