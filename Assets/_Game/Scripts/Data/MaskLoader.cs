// ============================================================
// MaskLoader.cs  –  Arrow Slide Puzzle
// Reads mask PNG files (mask_heart.png, mask_phone.png, mask_book.png)
// from the Masks/ folder.
// White pixel = arrow cell, Black pixel = empty cell.
// If mask file does not exist, generates placeholder masks:
//   • heart: classic chunky heart silhouette
//   • phone: vertical rectangle
//   • book: rectangle
// PRD Section 7: Assets/_Game/Art/Masks
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ArrowPuzzle.Data
{
    public static class MaskLoader
    {
        private const int PlaceholderTextureSize = 128;

        /// <summary>
        /// Loads the shape mask for a grid of size (gridWidth x gridHeight).
        /// Returns a set of grid coordinates (col, row) that are inside the shape (white pixels).
        /// </summary>
        public static HashSet<Vector2Int> LoadMask(string fileName, int gridWidth, int gridHeight)
        {
            var validCells = new HashSet<Vector2Int>();
            if (string.IsNullOrEmpty(fileName) || gridWidth <= 0 || gridHeight <= 0)
                return validCells;

            string filePath = ResolveMaskPath(fileName);
            Texture2D maskTex = null;

            if (File.Exists(filePath))
            {
                try
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    maskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    maskTex.LoadImage(fileData);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MaskLoader] Error reading '{filePath}': {ex.Message}. Falling back to placeholder.");
                    maskTex = null;
                }
            }

            if (maskTex == null)
            {
                // Generate and save placeholder mask
                maskTex = GeneratePlaceholderMask(fileName, gridWidth, gridHeight);
                TrySaveMaskToDisk(maskTex, filePath);
            }

            // Sample the texture at the centre of each grid cell (col, row)
            // Note: In Unity texture coords, (0,0) is bottom-left, matching grid cell (0,0).
            for (int col = 0; col < gridWidth; col++)
            {
                for (int row = 0; row < gridHeight; row++)
                {
                    float u = (col + 0.5f) / gridWidth;
                    float v = (row + 0.5f) / gridHeight;

                    int px = Mathf.Clamp(Mathf.FloorToInt(u * maskTex.width), 0, maskTex.width - 1);
                    int py = Mathf.Clamp(Mathf.FloorToInt(v * maskTex.height), 0, maskTex.height - 1);

                    Color pixel = maskTex.GetPixel(px, py);

                    // White pixel = arrow area (grayscale > 0.5 and alpha > 0.5)
                    if (pixel.grayscale > 0.5f && pixel.a > 0.5f)
                    {
                        validCells.Add(new Vector2Int(col, row));
                    }
                }
            }

            return validCells;
        }

        /// <summary>
        /// Resolves the absolute file path to the mask PNG.
        /// Searches Assets/_Game/Art/Masks/, then fallback paths.
        /// </summary>
        public static string ResolveMaskPath(string fileName)
        {
            // Primary location: Assets/_Game/Art/Masks/
            string p1 = Path.Combine(Application.dataPath, "_Game/Art/Masks", fileName);
            if (File.Exists(p1)) return p1;

            // Alternative location: Assets/Masks/
            string p2 = Path.Combine(Application.dataPath, "Masks", fileName);
            if (File.Exists(p2)) return p2;

            // Default target path
            return p1;
        }

        /// <summary>
        /// Generates an in-memory Texture2D placeholder mask for the given shape type.
        /// </summary>
        public static Texture2D GeneratePlaceholderMask(string fileName, int gridWidth, int gridHeight)
        {
            string lower = (fileName ?? "").ToLowerInvariant();
            int texW = PlaceholderTextureSize;
            int texH = PlaceholderTextureSize;

            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[texW * texH];

            // Initialize all black
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.black;

            if (lower.Contains("phone"))
            {
                // Phone: vertical rectangle (e.g. 66% width, 75% height centered)
                int minX = Mathf.FloorToInt(texW * 0.17f);
                int maxX = Mathf.FloorToInt(texW * 0.83f);
                int minY = Mathf.FloorToInt(texH * 0.12f);
                int maxY = Mathf.FloorToInt(texH * 0.88f);

                for (int y = minY; y <= maxY; y++)
                    for (int x = minX; x <= maxX; x++)
                        pixels[y * texW + x] = Color.white;
            }
            else if (lower.Contains("book"))
            {
                // Book: horizontal/chunky rectangle
                int minX = Mathf.FloorToInt(texW * 0.12f);
                int maxX = Mathf.FloorToInt(texW * 0.88f);
                int minY = Mathf.FloorToInt(texH * 0.14f);
                int maxY = Mathf.FloorToInt(texH * 0.86f);

                for (int y = minY; y <= maxY; y++)
                    for (int x = minX; x <= maxX; x++)
                        pixels[y * texW + x] = Color.white;
            }
            else if (lower.Contains("diamond"))
            {
                // Diamond shape: Manhattan distance from center <= radius
                int cx = texW / 2;
                int cy = texH / 2;
                float rx = texW * 0.44f;
                float ry = texH * 0.44f;
                for (int y = 0; y < texH; y++)
                    for (int x = 0; x < texW; x++)
                    {
                        if (Mathf.Abs(x - cx) / rx + Mathf.Abs(y - cy) / ry <= 0.85f)
                            pixels[y * texW + x] = Color.white;
                    }
            }
            else if (lower.Contains("star"))
            {
                // Star / cross shape
                int cx = texW / 2;
                int cy = texH / 2;
                for (int y = 0; y < texH; y++)
                    for (int x = 0; x < texW; x++)
                    {
                        float dx = Mathf.Abs(x - cx);
                        float dy = Mathf.Abs(y - cy);
                        if ((dx <= texW * 0.42f && dy <= texH * 0.18f) ||
                            (dy <= texH * 0.42f && dx <= texW * 0.18f) ||
                            (dx + dy <= texW * 0.35f))
                        {
                            pixels[y * texW + x] = Color.white;
                        }
                    }
            }
            else if (lower.Contains("circle"))
            {
                // Circle shape
                int cx = texW / 2;
                int cy = texH / 2;
                float r = texW * 0.42f;
                for (int y = 0; y < texH; y++)
                    for (int x = 0; x < texW; x++)
                    {
                        float dx = x - cx;
                        float dy = y - cy;
                        if (dx * dx + dy * dy <= r * r)
                            pixels[y * texW + x] = Color.white;
                    }
            }
            else if (lower.Contains("shield"))
            {
                // Shield shape
                for (int y = 0; y < texH; y++)
                {
                    float ny = (float)y / (texH - 1);
                    for (int x = 0; x < texW; x++)
                    {
                        float nx = Mathf.Abs((float)x / (texW - 1) - 0.5f) * 2f;
                        if (ny >= 0.4f && nx <= 0.82f)
                            pixels[y * texW + x] = Color.white;
                        else if (ny < 0.4f && nx <= 0.82f * (ny / 0.4f + 0.12f))
                            pixels[y * texW + x] = Color.white;
                    }
                }
            }
            else // heart or default
            {
                // Heart: classic heart curve in normalized coordinates [-1, 1]
                for (int y = 0; y < texH; y++)
                {
                    float ny = (float)y / (texH - 1) * 2.2f - 1.0f; // range [-1.0, 1.2]
                    for (int x = 0; x < texW; x++)
                    {
                        float nx = (float)x / (texW - 1) * 2.4f - 1.2f; // range [-1.2, 1.2]

                        // Standard heart equation: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                        float a = nx * nx + ny * ny - 1f;
                        if (a * a * a - nx * nx * ny * ny * ny <= 0.05f)
                        {
                            pixels[y * texW + x] = Color.white;
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static void TrySaveMaskToDisk(Texture2D tex, string targetPath)
        {
            try
            {
                string dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                byte[] pngBytes = tex.EncodeToPNG();
                File.WriteAllBytes(targetPath, pngBytes);
                Debug.Log($"[MaskLoader] Generated and saved placeholder mask to: {targetPath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MaskLoader] Could not save generated mask to disk: {ex.Message}");
            }
        }
    }
}

