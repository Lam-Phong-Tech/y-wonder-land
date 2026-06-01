using UnityEngine;
using UnityEditor;
using System.IO;

public class WaterTextureGenerator
{
    [MenuItem("Tools/Generate Water Normal Map")]
    public static void GenerateWaterNormal()
    {
        int width = 512;
        int height = 512;
        Texture2D heightMap = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Texture2D normalMap = new Texture2D(width, height, TextureFormat.RGBA32, false);

        float scale = 25f;
        
        // 1. Generate heightmap using Perlin Noise
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Make it seamless by sampling
                float nx = (float)x / width * scale;
                float ny = (float)y / height * scale;
                
                // Add some detail octaves
                float noise = Mathf.PerlinNoise(nx, ny) * 1.0f;
                noise += Mathf.PerlinNoise(nx * 2.1f, ny * 2.1f) * 0.5f;
                noise += Mathf.PerlinNoise(nx * 4.3f, ny * 4.3f) * 0.25f;
                
                heightMap.SetPixel(x, y, new Color(noise, noise, noise));
            }
        }
        heightMap.Apply();

        // 2. Convert Heightmap to Normal Map (Sobel filter style)
        float strength = 4f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Seamless wrap lookup
                float hL = heightMap.GetPixel((x - 1 + width) % width, y).r;
                float hR = heightMap.GetPixel((x + 1) % width, y).r;
                float hD = heightMap.GetPixel(x, (y - 1 + height) % height).r;
                float hU = heightMap.GetPixel(x, (y + 1) % height).r;

                // Calculate horizontal and vertical gradients
                float dX = (hR - hL) * strength;
                float dY = (hU - hD) * strength;

                // Create normal vector and normalize it
                Vector3 normal = new Vector3(-dX, -dY, 1.0f).normalized;

                // Pack normal vector into RGB channels: [-1, 1] mapped to [0, 1]
                float r = (normal.x + 1f) * 0.5f;
                float g = (normal.y + 1f) * 0.5f;
                float b = (normal.z + 1f) * 0.5f;

                normalMap.SetPixel(x, y, new Color(r, g, b, 1.0f));
            }
        }
        normalMap.Apply();

        // 3. Save to PNG
        string dirPath = Application.dataPath + "/Environment/Textures";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        
        string filePath = dirPath + "/WaterNormal.png";
        byte[] bytes = normalMap.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
        
        // Destroy temporary textures
        Object.DestroyImmediate(heightMap);
        Object.DestroyImmediate(normalMap);

        // 4. Refresh asset database and set import settings
        AssetDatabase.Refresh();
        
        string relativePath = "Assets/Environment/Textures/WaterNormal.png";
        TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            Debug.Log("Successfully generated and imported Water Normal Map!");
        }
        else
        {
            Debug.LogError("Failed to find importer for " + relativePath);
        }
    }
}
