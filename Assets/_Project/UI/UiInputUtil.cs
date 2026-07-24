using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Tiện ích cho ô nhập UI Toolkit. Gom về một chỗ để MỌI ô SỐ (số lượng mua/bán,
/// số Point gửi Heo đất...) validate giống nhau: chỉ nhận chữ số 0-9, tự xoá ký tự
/// lạ ngay khi gõ, chặn tràn int, và bật bàn phím số trên mobile.
/// Lý do có file này: tester gõ được cả chữ vào ô số lượng -> phải chặn từ gốc.
/// </summary>
public static class UiInputUtil
{
    /// <summary>
    /// Thiết lập 1 TextField thành ô nhập SỐ NGUYÊN DƯƠNG. Gọi 1 lần lúc khởi tạo ô.
    /// Bật bàn phím số (mobile) + giới hạn số chữ số để int.Parse không tràn.
    /// </summary>
    public static void ConfigureNumeric(TextField field, int maxDigits = 9)
    {
        if (field == null) return;
        field.maxLength = maxDigits;
        field.keyboardType = TouchScreenKeyboardType.NumberPad;
    }

    /// <summary>
    /// Lọc giá trị hiện tại của ô về CHỈ chữ số, ghi lại bản sạch vào ô (dùng
    /// SetValueWithoutNotify để không bắn lại callback, tránh lặp vô hạn) rồi thử parse.
    /// Trả true + số nếu ô có ít nhất 1 chữ số. Gọi NGAY ĐẦU handler value-changed.
    /// </summary>
    public static bool TrySanitizeInt(TextField field, out int value, int maxDigits = 9)
    {
        value = 0;
        if (field == null) return false;

        string cleaned = KeepDigits(field.value, maxDigits);
        if (cleaned != field.value)
            field.SetValueWithoutNotify(cleaned);

        return int.TryParse(cleaned, out value);
    }

    // Giữ lại đúng các ký tự 0-9, cắt bớt nếu vượt maxDigits.
    private static string KeepDigits(string raw, int maxDigits)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        var sb = new StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            if (c < '0' || c > '9') continue;
            sb.Append(c);
            if (sb.Length >= maxDigits) break;
        }
        return sb.ToString();
    }
}
