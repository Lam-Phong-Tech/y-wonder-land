using UnityEditor;
using UnityEngine;

/// <summary>
/// Ép thiết lập import cho bộ icon nút tương tác (Resources/UI/InteractionIcons).
///
/// Vì sao cần: icon là hình trắng viền mảnh trên nền trong suốt. Để Unity import mặc
/// định thì nó nén DXT/ETC + sinh mipmap, ra màn hình icon bị rỗ viền và mờ nhoè —
/// nhất là mấy nét nhỏ như kim tiêm hay chữ "i". Chỉnh tay trong Inspector cũng được
/// nhưng thêm icon mới là quên, nên để script lo.
/// </summary>
public class InteractionIconImporter : AssetPostprocessor
{
    private const string TargetFolder = "Assets/Resources/UI/InteractionIcons/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(TargetFolder, System.StringComparison.OrdinalIgnoreCase)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;   // giữ viền mượt, không bị quầng đen
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.mipmapEnabled = false;        // UI vẽ 1:1, bật mipmap chỉ làm mờ
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 128;         // ảnh gốc đã đúng 128, không cần hơn
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }

    // Script này chỉ chạy lúc Unity IMPORT file. Icon nào lỡ được import trước khi có
    // script thì dùng menu này ép nạp lại, khỏi phải xoá file đi thêm lại.
    [MenuItem("YWonderLand/UI/Nạp lại icon nút tương tác")]
    private static void ReimportAll()
    {
        AssetDatabase.ImportAsset(
            TargetFolder.TrimEnd('/'),
            ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        Debug.Log("[InteractionIconImporter] Đã nạp lại " + TargetFolder);
    }
}
