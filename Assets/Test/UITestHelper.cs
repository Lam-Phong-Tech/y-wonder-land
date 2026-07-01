using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Script test nhanh cho Confirm Dialog và Reward Popup.
/// CHỈ DÙNG ĐỂ TEST — xóa khi không cần nữa.
/// 
/// Phím tắt:
///   F1 → Confirm Dialog (Warning — vàng)
///   F2 → Confirm Dialog (Danger — đỏ)
///   F3 → Confirm Dialog (Info — xanh)
///   F4 → Reward Popup (3 vật phẩm)
///   F5 → Reward Popup (1 vật phẩm)
///   F6 → Reward Popup (rỗng — test empty state)
/// </summary>
public class UITestHelper : MonoBehaviour
{
    [Header("Kéo thả GameObject chứa Controller vào đây")]
    [SerializeField] private ConfirmDialogController confirmDialog;
    [SerializeField] private RewardPopupController rewardPopup;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // === CONFIRM DIALOG ===

        if (keyboard.f1Key.wasPressedThisFrame)
        {
            TestConfirmWarning();
        }

        if (keyboard.f2Key.wasPressedThisFrame)
        {
            TestConfirmDanger();
        }

        if (keyboard.f3Key.wasPressedThisFrame)
        {
            TestConfirmInfo();
        }

        // === REWARD POPUP ===

        if (keyboard.f4Key.wasPressedThisFrame)
        {
            TestRewardMultiple();
        }

        if (keyboard.f5Key.wasPressedThisFrame)
        {
            TestRewardSingle();
        }

        if (keyboard.f6Key.wasPressedThisFrame)
        {
            TestRewardEmpty();
        }
    }

    // ── Confirm Dialog Tests ──

    private void TestConfirmWarning()
    {
        if (confirmDialog == null)
        {
            Debug.LogWarning("[UITest] ConfirmDialogController chưa được gán!");
            return;
        }

        confirmDialog.Show(
            "CẢNH BÁO",
            "Bạn KHÔNG THỂ thay đổi tên và giới tính sau khi xác nhận. Bạn có chắc chắn?",
            "Đồng ý",
            "Hủy bỏ",
            () => Debug.Log("[UITest] ✅ Warning → Đã bấm Đồng ý!"),
            ConfirmDialogType.Warning
        );

        Debug.Log("[UITest] Mở Confirm Dialog — Warning (F1)");
    }

    private void TestConfirmDanger()
    {
        if (confirmDialog == null)
        {
            Debug.LogWarning("[UITest] ConfirmDialogController chưa được gán!");
            return;
        }

        confirmDialog.Show(
            "XÓA TÀI KHOẢN",
            "Hành động này sẽ xóa vĩnh viễn tài khoản của bạn. Tất cả dữ liệu sẽ bị mất và không thể khôi phục.",
            "Xóa vĩnh viễn",
            "Giữ tài khoản",
            () => Debug.Log("[UITest] ❌ Danger → Đã bấm Xóa vĩnh viễn!"),
            ConfirmDialogType.Danger
        );

        Debug.Log("[UITest] Mở Confirm Dialog — Danger (F2)");
    }

    private void TestConfirmInfo()
    {
        if (confirmDialog == null)
        {
            Debug.LogWarning("[UITest] ConfirmDialogController chưa được gán!");
            return;
        }

        confirmDialog.Show(
            "XÁC NHẬN MUA",
            "Bạn muốn gửi 5000 Point vào Heo đất gói 30 ngày? Lãi suất 6%, không rút sớm được.",
            "Gửi tiền",
            "Để sau",
            () => Debug.Log("[UITest] ℹ️ Info → Đã bấm Gửi tiền!"),
            ConfirmDialogType.Info
        );

        Debug.Log("[UITest] Mở Confirm Dialog — Info (F3)");
    }

    // ── Reward Popup Tests ──

    private void TestRewardMultiple()
    {
        if (rewardPopup == null)
        {
            Debug.LogWarning("[UITest] RewardPopupController chưa được gán!");
            return;
        }

        List<RewardItemData> rewards = new List<RewardItemData>
        {
            new RewardItemData("🥕", "Cà rốt", 10),
            new RewardItemData("💰", "Point", 50),
            new RewardItemData("⭐", "EXP", 20)
        };

        rewardPopup.Show(
            "HOÀN THÀNH NHIỆM VỤ",
            rewards,
            "Nhận thưởng",
            () => Debug.Log("[UITest] 🎁 Reward → Đã nhận 3 phần thưởng!")
        );

        Debug.Log("[UITest] Mở Reward Popup — 3 items (F4)");
    }

    private void TestRewardSingle()
    {
        if (rewardPopup == null)
        {
            Debug.LogWarning("[UITest] RewardPopupController chưa được gán!");
            return;
        }

        List<RewardItemData> rewards = new List<RewardItemData>
        {
            new RewardItemData("🏆", "Cúp Vàng Mùa 1", 1)
        };

        rewardPopup.Show(
            "PHẦN THƯỞNG SỰ KIỆN",
            rewards,
            "Tuyệt vời!",
            () => Debug.Log("[UITest] 🏆 Reward → Đã nhận Cúp Vàng!")
        );

        Debug.Log("[UITest] Mở Reward Popup — 1 item (F5)");
    }

    private void TestRewardEmpty()
    {
        if (rewardPopup == null)
        {
            Debug.LogWarning("[UITest] RewardPopupController chưa được gán!");
            return;
        }

        rewardPopup.Show(
            "HẾT PHẦN THƯỞNG",
            new List<RewardItemData>(),
            "Đóng",
            () => Debug.Log("[UITest] 📭 Reward → Đóng popup rỗng")
        );

        Debug.Log("[UITest] Mở Reward Popup — empty state (F6)");
    }
}
