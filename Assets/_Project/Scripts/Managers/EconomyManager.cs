using System;
using UnityEngine;
using YWonderLand.Backend;

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
        public event Action<long> OnUPOSChanged;

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

        private void OnEnable()
        {
            if (AuthService.Instance != null)
                AuthService.Instance.IdentityChanged += HandleIdentityChanged;
        }

        private void OnDisable()
        {
            if (AuthService.Instance != null)
                AuthService.Instance.IdentityChanged -= HandleIdentityChanged;
        }

        private void HandleIdentityChanged(string previousScopeId, string nextScopeId)
        {
            LoadBalances();
            OnPOSChanged?.Invoke(currentPOS);
            OnUPOSChanged?.Invoke(currentUPOS);
            Debug.Log($"[Economy] Reloaded local cache for '{nextScopeId}'.");
        }

        private void LoadBalances()
        {
            // Nếu chưa có data, tặng 5000 Point làm vốn khởi nghiệp
            currentPOS = PlayerScopedPrefs.GetInt(POS_KEY, 5000);
            currentUPOS = PlayerScopedPrefs.GetInt(UPOS_KEY, 0);
        }

        private void SaveBalances()
        {
            PlayerScopedPrefs.SetInt(POS_KEY, (int)currentPOS);
            PlayerScopedPrefs.SetInt(UPOS_KEY, (int)currentUPOS);
            PlayerScopedPrefs.Save();
        }

        public long GetPOS() => currentPOS;
        public long GetUPOS() => currentUPOS;

        public void ApplyServerState(long pos, long upos, bool saveLocalCache = true)
        {
            currentPOS = Math.Max(0, pos);
            currentUPOS = Math.Max(0, upos);
            if (saveLocalCache) SaveBalances();
            OnPOSChanged?.Invoke(currentPOS);
            OnUPOSChanged?.Invoke(currentUPOS);
            Debug.Log($"[Economy] Applied server state. Point={currentPOS}, UPoint={currentUPOS}");
        }

        public void AddPOS(long amount, string syncReason = "gameplay_point_add")
        {
            if (amount <= 0) return;
            currentPOS += amount;
            SaveBalances();
            GameplayMutationSync.QueueEconomyDelta(amount, 0, syncReason);
            OnPOSChanged?.Invoke(currentPOS);
            Debug.Log($"[Economy] Add {amount} Point. Balance: {currentPOS}");
        }

        public bool SpendPOS(long amount, string syncReason = "gameplay_point_spend")
        {
            if (amount <= 0) return true;
            if (currentPOS >= amount)
            {
                currentPOS -= amount;
                SaveBalances();
                GameplayMutationSync.QueueEconomyDelta(-amount, 0, syncReason);
                OnPOSChanged?.Invoke(currentPOS);
                Debug.Log($"[Economy] Spend {amount} Point. Balance: {currentPOS}");
                return true;
            }
            Debug.LogWarning($"[Economy] Not enough Point! Needed: {amount}, Have: {currentPOS}");
            return false;
        }

        public void AddUPOS(long amount, string syncReason = "gameplay_upoint_add")
        {
            if (amount <= 0) return;
            currentUPOS += amount;
            SaveBalances();
            GameplayMutationSync.QueueEconomyDelta(0, amount, syncReason);
            OnUPOSChanged?.Invoke(currentUPOS);
            Debug.Log($"[Economy] Add {amount} UPoint. Balance: {currentUPOS}");
        }

        public bool SpendUPOS(long amount, string syncReason = "gameplay_upoint_spend")
        {
            if (amount <= 0) return true;
            if (currentUPOS >= amount)
            {
                currentUPOS -= amount;
                SaveBalances();
                GameplayMutationSync.QueueEconomyDelta(0, -amount, syncReason);
                OnUPOSChanged?.Invoke(currentUPOS);
                Debug.Log($"[Economy] Spend {amount} UPoint. Balance: {currentUPOS}");
                return true;
            }

            Debug.LogWarning($"[Economy] Not enough UPoint! Needed: {amount}, Have: {currentUPOS}");
            return false;
        }
        
        public bool CanAffordPOS(long amount)
        {
            return currentPOS >= amount;
        }

        public bool CanAffordUPOS(long amount)
        {
            return currentUPOS >= amount;
        }
    }
}
