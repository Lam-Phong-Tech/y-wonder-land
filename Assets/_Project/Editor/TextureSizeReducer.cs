using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ép Max Size + nén ASTC cho texture để giảm dung lượng build mobile.
/// CÁCH DÙNG: trong cửa sổ Project, chọn 1 hoặc nhiều THƯ MỤC (hoặc các texture) chứa
/// texture cần nén -> menu "Tools/Tối ưu Mobile/Nén Texture đang chọn (Max 512 + ASTC)".
/// Áp ĐỆ QUY cho cả thư mục con. File gốc KHÔNG đổi — chỉ đổi cách Unity import (bản đưa vào APK).
/// </summary>
public static class TextureSizeReducer
{
    private const int MAX_SIZE = 512;

    [MenuItem("Tools/Tối ưu Mobile/Nén Texture đang chọn (Max 512 + ASTC)")]
    public static void ReduceSelected()
    {
        List<string> paths = CollectTexturePaths();
        if (paths.Count == 0)
        {
            EditorUtility.DisplayDialog("Nén Texture",
                "Chưa chọn thư mục hoặc texture nào trong cửa sổ Project.\nHãy bấm chọn thư mục chứa texture rồi chạy lại.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Nén Texture",
            $"Sẽ ép {paths.Count} texture về Max Size {MAX_SIZE} + nén ASTC (Android).\n" +
            "File gốc KHÔNG đổi, chỉ đổi cách import (bản đưa vào APK). Tiếp tục?",
            "Làm", "Hủy")) return;

        int done = 0, skipped = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < paths.Count; i++)
            {
                string p = paths[i];
                EditorUtility.DisplayProgressBar("Nén Texture", p, (float)i / paths.Count);

                var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                if (imp == null) { skipped++; continue; }

                // Mặc định (áp cho mọi nền nếu không override riêng) — cũng cắt size cho Editor/PC.
                imp.maxTextureSize = MAX_SIZE;

                // Override Android: 512 + ASTC (giữ nguyên các cài đặt khác của texture).
                var android = imp.GetPlatformTextureSettings("Android");
                android.overridden = true;
                android.maxTextureSize = MAX_SIZE;
                android.format = TextureImporterFormat.ASTC_6x6;
                imp.SetPlatformTextureSettings(android);

                imp.SaveAndReimport();
                done++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        string msg = $"Xong! Đã nén {done} texture về Max {MAX_SIZE} + ASTC. Bỏ qua {skipped} (không phải texture).";
        Debug.Log($"[TextureSizeReducer] {msg} → Build lại APK để thấy dung lượng giảm.");
        EditorUtility.DisplayDialog("Nén Texture", msg + "\n\nBuild lại APK để thấy dung lượng giảm.", "OK");
    }

    // Gom đường dẫn texture từ những gì đang chọn: THƯ MỤC -> tìm đệ quy; TEXTURE -> lấy thẳng.
    private static List<string> CollectTexturePaths()
    {
        var set = new HashSet<string>();
        foreach (string guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            if (AssetDatabase.IsValidFolder(path))
            {
                foreach (string g in AssetDatabase.FindAssets("t:Texture2D", new[] { path }))
                    set.Add(AssetDatabase.GUIDToAssetPath(g));
            }
            else if (AssetImporter.GetAtPath(path) is TextureImporter)
            {
                set.Add(path);
            }
        }
        return new List<string>(set);
    }
}
