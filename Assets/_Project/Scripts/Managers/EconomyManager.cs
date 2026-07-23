using System;
using System.Globalization;
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
        private long currentPOS;

        private const string POS_KEY = "YW_POS_Balance_Long";
        private const string LEGACY_POS_KEY = "YW_POS_Balance";
        private const string LEGACY_UPOS_KEY = "YW_UPOS_Balance";

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
            Debug.Log($"[Economy] Reloaded local cache for '{nextScopeId}'.");
        }

        private void LoadBalances()
        {
            // Vốn khởi đầu tài khoản mới = 0 Point (KHÁCH CHỐT 22/07). Con số 5000 cũ chỉ là
            // số tạm lúc dựng thử, không phải khoản tặng — đã bỏ.
            long legacyBalance = PlayerScopedPrefs.GetInt(LEGACY_POS_KEY, 0);
            string cachedBalance = PlayerScopedPrefs.GetString(POS_KEY, "");
            if (!long.TryParse(cachedBalance, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentPOS))
                currentPOS = legacyBalance;

            currentPOS = Math.Max(0, currentPOS);
            PlayerScopedPrefs.SetString(POS_KEY, currentPOS.ToString(CultureInfo.InvariantCulture));
            PlayerScopedPrefs.DeleteKey(LEGACY_POS_KEY);
            PlayerScopedPrefs.DeleteKey(LEGACY_UPOS_KEY);
            PlayerScopedPrefs.Save();
        }

        private void SaveBalances()
        {
            PlayerScopedPrefs.SetString(POS_KEY, currentPOS.ToString(CultureInfo.InvariantCulture));
            PlayerScopedPrefs.Save();
        }

        public long GetPOS() => currentPOS;

        public void ApplyServerState(long pos, bool saveLocalCache = true)
        {
            currentPOS = Math.Max(0, pos);
            if (saveLocalCache) SaveBalances();
            OnPOSChanged?.Invoke(currentPOS);
            Debug.Log($"[Economy] Applied server state. Point={currentPOS}");
        }

        public void AddPOS(long amount, string syncReason = "gameplay_point_add")
        {
            if (amount <= 0) return;
            currentPOS += amount;
            SaveBalances();
            GameplayMutationSync.QueueEconomyDelta(amount, syncReason);
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
                GameplayMutationSync.QueueEconomyDelta(-amount, syncReason);
                OnPOSChanged?.Invoke(currentPOS);
                Debug.Log($"[Economy] Spend {amount} Point. Balance: {currentPOS}");
                return true;
            }
            Debug.LogWarning($"[Economy] Not enough Point! Needed: {amount}, Have: {currentPOS}");
            return false;
        }

        public bool CanAffordPOS(long amount)
        {
            return currentPOS >= amount;
        }

    }
}
