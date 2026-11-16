// ============================================================
// SplashAssetGenerator.cs  –  Arrow Slide Puzzle Editor Tool
// Procedurally generates placeholder sprites for the Splash / Transition UI:
//   1. bg_pattern_splash.png – 512x512 seamless repeating tile with faint icons (gift, star, puzzle, dice)
//   2. dot_white_circle.png – Antialiased white circle for loading dots
//   3. arrow_glossy_pink_up.png – 256x256 glossy 3D Pink Up arrow
//   4. arrow_glossy_orange_right.png – 256x256 glossy 3D Orange Right arrow
//   5. arrow_glossy_cyan_left.png – 256x256 glossy 3D Cyan Left arrow
//   6. arrow_glossy_purple_down.png – 256x256 glossy 3D Purple Down arrow
//
// Menu: Arrow Puzzle → Generate Splash & Transition Sprites
// ============================================================

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArrowPuzzle.Editor
{
    public static class SplashAssetGenerator
    {
        private const string ArtUIDir = "Assets/_Game/Art/UI";

        [MenuItem("Arrow Puzzle/Generate Splash & Transition Sprites", false, 150)]
        public static void GenerateAllSprites()
        {
            DoGenerate(force: true);
        }

        [InitializeOnLoadMethod]
        private static void AutoGenerateIfMissing()
        {
            string checkFile = Path.Combine(ArtUIDir, "arrow_glossy_pink_up.png");
            if (!File.Exists(checkFile))
            {
                DoGenerate(force: false);
            }
        }

        private static void DoGenerate(bool force)
        {
            if (!Directory.Exists(ArtUIDir))
            {
                Directory.CreateDirectory(ArtUIDir);
            }

            GenerateWhiteCircleDot();
            GeneratePatternTexture();
            GenerateGlossyArrow("arrow_glossy_pink_up.png",       new Color(1f, 0.24f, 0.75f), new Color(0.85f, 0.1f, 0.55f), ArrowDir.Up);
            GenerateGlossyArrow("arrow_glossy_orange_right.png",   new Color(1f, 0.65f, 0.18f), new Color(0.88f, 0.45f, 0.05f), ArrowDir.Right);
            GenerateGlossyArrow("arrow_glossy_cyan_left.png",      new Color(0.31f, 0.77f, 1f), new Color(0.12f, 0.55f, 0.85f), ArrowDir.Left);
            GenerateGlossyArrow("arrow_glossy_purple_down.png",    new Color(0.61f, 0.17f, 1f), new Color(0.45f, 0.08f, 0.82f), ArrowDir.Down);

            AssetDatabase.Refresh();
            Debug.Log("[SplashAssetGenerator] Generated all splash & transition sprites successfully.");
        }

        private enum ArrowDir { Up, Right, Left, Down }

        // ── 1. Circle Dot ─────────────────────────────────────────────
        private static void GenerateWhiteCircleDot()
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = 28f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            string path = Path.Combine(ArtUIDir, "dot_white_circle.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureAsSprite(path, TextureWrapMode.Clamp);
        }

        // ── 2. Seamless Pattern Texture ───────────────────────────────
        private static void GeneratePatternTexture()
        {
            int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(1f, 1f, 1f, 0f);

            // Fill transparent
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, transparent);

            // Draw 4 distinct low-contrast icons (Gift, Star, Puzzle, Dice)
            DrawGiftIcon(tex, 128, 128, 48);
            DrawStarIcon(tex, 384, 128, 48);
            DrawPuzzleIcon(tex, 128, 384, 48);
            DrawDiceIcon(tex, 384, 384, 48);

            tex.Apply();

            string path = Path.Combine(ArtUIDir, "bg_pattern_splash.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureAsSprite(path, TextureWrapMode.Repeat);
        }

        private static void DrawGiftIcon(Texture2D tex, int cx, int cy, int s)
        {
            Color c = new Color(1f, 1f, 1f, 0.45f);
            int half = s / 2;
            int x0 = cx - half, x1 = cx + half;
            int y0 = cy - half, y1 = cy + half - 6;

            // Box body
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    // Vertical or horizontal ribbon
                    bool isRibbon = Mathf.Abs(x - cx) <= 4 || Mathf.Abs(y - (cy - 3)) <= 4;
                    float alpha = isRibbon ? 0.70f : 0.35f;
                    BlendPixel(tex, x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            // Box lid
            int lidY0 = y1 + 1, lidY1 = y1 + 8;
            for (int y = lidY0; y <= lidY1; y++)
            {
                for (int x = x0 - 3; x <= x1 + 3; x++)
                {
                    bool isRibbon = Mathf.Abs(x - cx) <= 4;
                    float alpha = isRibbon ? 0.85f : 0.45f;
                    BlendPixel(tex, x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            // Bow on top
            DrawCircle(tex, cx - 8, lidY1 + 5, 6, new Color(1f, 1f, 1f, 0.60f), false);
            DrawCircle(tex, cx + 8, lidY1 + 5, 6, new Color(1f, 1f, 1f, 0.60f), false);
        }

        private static void DrawStarIcon(Texture2D tex, int cx, int cy, int r)
        {
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > r) continue;

                    float angle = Mathf.Atan2(dy, dx) - Mathf.PI * 0.5f;
                    float arm = Mathf.Cos(5f * angle);
                    float rBound = r * (0.55f + 0.45f * arm);

                    float diff = rBound - dist;
                    if (diff >= 0f)
                    {
                        float alpha = Mathf.Clamp01(diff + 0.5f) * 0.45f;
                        BlendPixel(tex, x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
            }
        }

        private static void DrawPuzzleIcon(Texture2D tex, int cx, int cy, int s)
        {
            int half = s / 2;
            int x0 = cx - half, x1 = cx + half;
            int y0 = cy - half, y1 = cy + half;

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    // Check main body
                    BlendPixel(tex, x, y, new Color(1f, 1f, 1f, 0.40f));
                }
            }

            // Right tab (male connector)
            DrawCircle(tex, x1 + 3, cy, 9, new Color(1f, 1f, 1f, 0.40f), true);
            // Top tab
            DrawCircle(tex, cx, y1 + 3, 9, new Color(1f, 1f, 1f, 0.40f), true);
            // Left indentation (female)
            PunchHole(tex, x0, cy, 8);
        }

        private static void DrawDiceIcon(Texture2D tex, int cx, int cy, int s)
        {
            int half = s / 2;
            int x0 = cx - half, x1 = cx + half;
            int y0 = cy - half, y1 = cy + half;
            float cornerRadius = 8f;

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float d = SignedDistRoundedBox(new Vector2(x - cx, y - cy), new Vector2(half, half), cornerRadius);
                    if (d <= 0f)
                    {
                        float alpha = Mathf.Clamp01(-d + 0.5f) * 0.40f;
                        BlendPixel(tex, x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
            }

            // Pips (Dice 5 pattern)
            int dOffset = 12;
            DrawPip(tex, cx, cy);
            DrawPip(tex, cx - dOffset, cy - dOffset);
            DrawPip(tex, cx + dOffset, cy - dOffset);
            DrawPip(tex, cx - dOffset, cy + dOffset);
            DrawPip(tex, cx + dOffset, cy + dOffset);
        }

        private static void DrawPip(Texture2D tex, int px, int py)
        {
            PunchHole(tex, px, py, 4);
        }

        private static void PunchHole(Texture2D tex, int cx, int cy, int r)
        {
            for (int y = cy - r - 1; y <= cy + r + 1; y++)
            {
                for (int x = cx - r - 1; x <= cx + r + 1; x++)
                {
                    if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) continue;
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (d < r)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                    }
                }
            }
        }

        private static void DrawCircle(Texture2D tex, int cx, int cy, int r, Color c, bool filled)
        {
            for (int y = cy - r - 1; y <= cy + r + 1; y++)
            {
                for (int x = cx - r - 1; x <= cx + r + 1; x++)
                {
                    if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) continue;
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (filled)
                    {
                        float alpha = Mathf.Clamp01(r - d + 0.5f) * c.a;
                        if (alpha > 0.01f)
                            BlendPixel(tex, x, y, new Color(c.r, c.g, c.b, alpha));
                    }
                    else
                    {
                        float ringDiff = Mathf.Abs(d - r);
                        if (ringDiff < 2f)
                        {
                            float alpha = Mathf.Clamp01(2f - ringDiff) * c.a;
                            BlendPixel(tex, x, y, new Color(c.r, c.g, c.b, alpha));
                        }
                    }
                }
            }
        }

        private static void BlendPixel(Texture2D tex, int x, int y, Color c)
        {
            if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) return;
            Color dst = tex.GetPixel(x, y);
            float outA = c.a + dst.a * (1f - c.a);
            if (outA <= 0.001f) return;
            Color outCol = (c * c.a + dst * dst.a * (1f - c.a)) / outA;
            outCol.a = outA;
            tex.SetPixel(x, y, outCol);
        }

        // ── 3. Glossy 3D Arrow Sprites ─────────────────────────────────
        private static void GenerateGlossyArrow(string fileName, Color baseCol, Color darkShadowCol, ArrowDir dir)
        {
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, transparent);

            Vector2 center = new Vector2(128, 128);
            Vector2 boxHalf = new Vector2(100, 100);
            float radius = 40f;

            // 1. Bottom 3D shadow layer (offset down by 14px)
            Vector2 shadowOffset = new Vector2(0, -12);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y) - (center + shadowOffset);
                    float d = SignedDistRoundedBox(p, boxHalf, radius);
                    if (d <= 1f)
                    {
                        float a = Mathf.Clamp01(1f - d);
                        tex.SetPixel(x, y, new Color(darkShadowCol.r, darkShadowCol.g, darkShadowCol.b, a));
                    }
                }
            }

            // 2. Main 3D tile face (with gradient)
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y) - center;
                    float d = SignedDistRoundedBox(p, boxHalf, radius);
                    if (d <= 1f)
                    {
                        float alpha = Mathf.Clamp01(1f - d);
                        float vGrad = Mathf.Clamp01((y - (center.y - boxHalf.y)) / (boxHalf.y * 2f));
                        // Gradient from lighter at top to rich base at bottom
                        Color faceCol = Color.Lerp(baseCol * 0.88f, baseCol * 1.15f, vGrad);
                        faceCol.a = alpha;
                        BlendPixel(tex, x, y, faceCol);
                    }
                }
            }

            // 3. Glossy Top Sheen (upper 45% glass reflection curve)
            for (int y = (int)(center.y); y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y) - center;
                    float d = SignedDistRoundedBox(p, boxHalf, radius);
                    if (d <= 0f)
                    {
                        // Curved highlight reflection line
                        float curveY = center.y + 10f + Mathf.Sin(((x - center.x + boxHalf.x) / (boxHalf.x * 2f)) * Mathf.PI) * 20f;
                        if (y >= curveY)
                        {
                            float t = (y - curveY) / (boxHalf.y);
                            float sheenA = Mathf.Clamp01(t) * 0.35f;
                            BlendPixel(tex, x, y, new Color(1f, 1f, 1f, sheenA));
                        }
                    }
                }
            }

            // 4. Inner bright rim at top
            for (int y = (int)(center.y + boxHalf.y - 12); y <= (int)(center.y + boxHalf.y); y++)
            {
                for (int x = (int)(center.x - boxHalf.x + 20); x <= (int)(center.x + boxHalf.x - 20); x++)
                {
                    Vector2 p = new Vector2(x, y) - center;
                    float d = SignedDistRoundedBox(p, boxHalf, radius);
                    if (d <= -2f && d >= -8f)
                    {
                        BlendPixel(tex, x, y, new Color(1f, 1f, 1f, 0.40f));
                    }
                }
            }

            // 5. Draw bold 3D Arrow Chevron in center
            Draw3DArrowChevron(tex, center, dir);

            tex.Apply();

            string path = Path.Combine(ArtUIDir, fileName);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureAsSprite(path, TextureWrapMode.Clamp);
        }

        private static void Draw3DArrowChevron(Texture2D tex, Vector2 center, ArrowDir dir)
        {
            // First draw arrow shadow offset by (0, -5)
            RenderArrowMesh(tex, center + new Vector2(0, -6), dir, new Color(0f, 0f, 0f, 0.35f));

            // Then draw white 3D arrow face
            RenderArrowMesh(tex, center, dir, Color.white);
        }

        private static void RenderArrowMesh(Texture2D tex, Vector2 c, ArrowDir dir, Color col)
        {
            float angle = 0f;
            switch (dir)
            {
                case ArrowDir.Up:    angle = 0f; break;
                case ArrowDir.Right: angle = -90f; break;
                case ArrowDir.Down:  angle = 180f; break;
                case ArrowDir.Left:  angle = 90f; break;
            }

            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            int minX = (int)(c.x - 65), maxX = (int)(c.x + 65);
            int minY = (int)(c.y - 65), maxY = (int)(c.y + 65);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    // Transform pixel to arrow local space (pointing UP)
                    float px = x - c.x;
                    float py = y - c.y;
                    float lx = px * cos - py * sin;
                    float ly = px * sin + py * cos;

                    if (IsInsideArrowShape(lx, ly))
                    {
                        BlendPixel(tex, x, y, col);
                    }
                }
            }
        }

        private static bool IsInsideArrowShape(float x, float y)
        {
            // Pointing UP:
            // Triangle head: tip at (0, 48), wings at (-46, 2) and (+46, 2), notched base at (0, 16)
            // Shaft: from y = -46 to y = 14, width 32 (x from -16 to +16)
            if (y >= 0f && y <= 48f)
            {
                float t = (48f - y) / 48f;
                float halfW = t * 46f;
                if (Mathf.Abs(x) <= halfW) return true;
            }
            if (y >= -44f && y <= 6f)
            {
                if (Mathf.Abs(x) <= 18f) return true;
            }
            return false;
        }

        private static float SignedDistRoundedBox(Vector2 p, Vector2 b, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x) - b.x + r, Mathf.Abs(p.y) - b.y + r);
            Vector2 maxQ = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return maxQ.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        private static void ConfigureAsSprite(string assetPath, TextureWrapMode wrap)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.wrapMode = wrap;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
