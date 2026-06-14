using System.Collections.Generic;

/// <summary>
/// UIPopupTracker: công tắc trung tâm theo dõi "có popup nào đang mở không".
/// Các hệ thống khác (vd ThirdPersonCamera, FarmInteractionController) đọc cờ này
/// để TỰ TRẢ CHUỘT + ngừng xoay camera + ngừng tương tác thế giới khi đang mở UI.
///
/// CÁCH DÙNG trong mỗi popup controller:
///   - Trong Show():  UIPopupTracker.SetOpen(this, true);
///   - Trong Hide():  UIPopupTracker.SetOpen(this, false);
///
/// Dùng HashSet nên gọi trùng (Show 2 lần) cũng không bị đếm lệch.
/// </summary>
public static class UIPopupTracker
{
    private static readonly HashSet<object> _openPopups = new HashSet<object>();

    /// <summary>Có ít nhất 1 popup đang mở?</summary>
    public static bool AnyOpen => _openPopups.Count > 0;

    public static void SetOpen(object popup, bool isOpen)
    {
        if (popup == null) return;
        if (isOpen) _openPopups.Add(popup);
        else _openPopups.Remove(popup);
    }
}
