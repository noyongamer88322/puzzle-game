// ============================================================
// UIAssetGenerator.cs  –  Arrow Slide Puzzle Editor Tool
// Procedurally generates cartoon, glossy, high-quality sprites
// for UI popups, ribbons, checkmarks, and placeholder cards:
//   1. icon_check.png        – Vibrant green badge with white checkmark
//   2. icon_heart_broken.png – Large stylized broken heart for Game Over
//   3. ui_ribbon_gold.png    – Orange-yellow glossy ribbon banner (9-slice)
//   4. ui_ribbon_red.png     – Coral-red glossy ribbon banner for Out of Hearts
//   5. ui_panel_popup.png    – Sliced rounded panel card with soft border & shadow
// ============================================================

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArrowPuzzle.Editor
{
    public static class UIAssetGenerator
    {
        private const string ArtUIDir = "Assets/_Game/Art/UI";

        [MenuItem("Arrow Puzzle/Generate UI Sprite Assets", false, 151)]
        public static void GenerateAll()
        {
            if (!Directory.Exists(ArtUIDir))
                Directory.CreateDirectory(ArtUIDir);

            GenerateCheckmarkBadge();
            GenerateBrokenHeart();
            GenerateRibbon("ui_ribbon_gold.png", new Color(1f, 0.74f, 0.12f), new Color(0.96f, 0.48f, 0.04f), new Color(0.70f, 0.28f, 0.02f));
            GenerateRibbon("ui_ribbon_red.png",  new Color(1f, 0.35f, 0.42f), new Color(0.88f, 0.16f, 0.24f), new Color(0.60f, 0.08f, 0.15f));
            GeneratePopupPanel();

            AssetDatabase.Refresh();
            Debug.Log("[UIAssetGenerator] Generated all UI sprite assets successfully.");
        }

        [InitializeOnLoadMethod]
        private static void AutoGenerateIfMissing()
        {
            string check = Path.Combine(ArtUIDir, "icon_check.png");
            if (!File.Exists(check))
            {
                EditorApplication.delayCall += () => GenerateAll();
            }
        }

        // ── 1. Green Checkmark Badge (icon_check.png) ───────────────
        private static void GenerateCheckmarkBadge()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.44f;

            Color greenTop = new Color(0.24f, 0.88f, 0.42f, 1f); // #3DE06B
            Color greenBot = new Color(0.08f, 0.68f, 0.26f, 1f); // #15AD42
            Color outlineCol = new Color(0.04f, 0.45f, 0.15f, 1f);

            // Points for checkmark: A = (36, 62), B = (54, 42), C = (94, 86)
            Vector2 pA = new Vector2(size * 0.28f, size * 0.48f);
            Vector2 pB = new Vector2(size * 0.44f, size * 0.32f);
            Vector2 pC = new Vector2(size * 0.74f, size * 0.68f);
            float checkThickness = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pt = new Vector2(x, y);
                    float dCenter = Vector2.Distance(pt, center);

                    // Circle mask
                    float circleAlpha = Mathf.Clamp01(radius - dCenter + 1f);
                    if (circleAlpha <= 0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Base circle color with vertical gradient
                    float gradT = Mathf.Clamp01((float)y / size);
                    Color baseCol = Color.Lerp(greenBot, greenTop, gradT);

                    // Dark outline ring at edge
                    float edgeDist = radius - dCenter;
                    if (edgeDist < 3.5f)
                    {
                        baseCol = Color.Lerp(outlineCol, baseCol, Mathf.Clamp01(edgeDist / 3.5f));
                    }

                    // Checkmark stroke distance
                    float d1 = DistToSegment(pt, pA, pB);
                    float d2 = DistToSegment(pt, pB, pC);
                    float dCheck = Mathf.Min(d1, d2);
                    float checkAlpha = Mathf.Clamp01(checkThickness - dCheck + 1f);

                    Color finalCol = Color.Lerp(baseCol, Color.white, checkAlpha);
                    finalCol.a *= circleAlpha;
                    tex.SetPixel(x, y, finalCol);
                }
            }
            tex.Apply();

            SaveAndConfigureSprite(tex, "icon_check.png", Vector4.zero);
        }

        // ── 2. Broken Heart Icon (icon_heart_broken.png) ────────────
        private static void GenerateBrokenHeart()
        {
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Color emptyBeige = new Color(0.85f, 0.77f, 0.65f, 1f);
            Color shadowBeige = new Color(0.68f, 0.58f, 0.46f, 1f);
            Color crackCol = new Color(0.38f, 0.30f, 0.22f, 1f);

            // Zig-zag crack points down center
            Vector2[] crackPts = new Vector2[]
            {
                new Vector2(size * 0.50f, size * 0.78f),
                new Vector2(size * 0.44f, size * 0.62f),
                new Vector2(size * 0.55f, size * 0.48f),
                new Vector2(size * 0.46f, size * 0.34f),
                new Vector2(size * 0.50f, size * 0.18f)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Normalized to -1.4 .. 1.4
                    float nx = (x - size * 0.5f) / (size * 0.38f);
                    float ny = (y - size * 0.45f) / (size * 0.38f);

                    // Heart implicit curve: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                    float xx = nx * nx;
                    float yy = ny * ny;
                    float term = xx + yy - 1f;
                    float val = term * term * term - xx * ny * yy;

                    if (val > 0.02f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float heartAlpha = Mathf.Clamp01(-val * 20f);

                    // Vertical gradient
                    float t = Mathf.Clamp01((float)y / size);
                    Color heartCol = Color.Lerp(shadowBeige, emptyBeige, t);

                    // Crack line
                    Vector2 pt = new Vector2(x, y);
                    float minCrackDist = float.MaxValue;
                    for (int i = 0; i < crackPts.Length - 1; i++)
                    {
                        float d = DistToSegment(pt, crackPts[i], crackPts[i + 1]);
                        if (d < minCrackDist) minCrackDist = d;
                    }

                    float crackAlpha = Mathf.Clamp01(size * 0.022f - minCrackDist + 1f);
                    Color col = Color.Lerp(heartCol, crackCol, crackAlpha);
                    col.a *= heartAlpha;

                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();

            SaveAndConfigureSprite(tex, "icon_heart_broken.png", Vector4.zero);
        }

        // ── 3. Glossy Cartoon Ribbon (ui_ribbon_gold.png, ui_ribbon_red.png) ──
        private static void GenerateRibbon(string filename, Color topCol, Color botCol, Color borderCol)
        {
            int w = 512;
            int h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                float ty = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float tx = (float)x / (w - 1);

                    // Trapezoid / banner edge indent on left and right wings
                    float marginY = 12f;
                    if (y < marginY || y > h - marginY)
                    {
                        // Rounded top & bottom corners
                        float dy = y < marginY ? marginY - y : y - (h - marginY);
                        if (x < 28 && (28 - x) * (28 - x) + dy * dy > 28 * 28)
                        {
                            tex.SetPixel(x, y, Color.clear);
                            continue;
                        }
                        if (x > w - 28 && (x - (w - 28)) * (x - (w - 28)) + dy * dy > 28 * 28)
                        {
                            tex.SetPixel(x, y, Color.clear);
                            continue;
                        }
                    }

                    // Border check
                    bool isBorder = (y <= 4 || y >= h - 5 || x <= 4 || x >= w - 5);

                    if (isBorder)
                    {
                        tex.SetPixel(x, y, borderCol);
                    }
                    else
                    {
                        Color c = Color.Lerp(botCol, topCol, ty);
                        // Top gloss highlight
                        if (y > h * 0.65f)
                        {
                            float highlightT = (y - h * 0.65f) / (h * 0.35f);
                            c = Color.Lerp(c, Color.white, highlightT * 0.32f);
                        }
                        // Bottom ambient bevel
                        if (y < h * 0.20f)
                        {
                            c = Color.Lerp(borderCol, c, (float)y / (h * 0.20f));
                        }
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();

            // Sliced border: left 48, right 48, top 16, bottom 16
            SaveAndConfigureSprite(tex, filename, new Vector4(48, 16, 48, 16));
        }

        // ── 4. Popup Panel Card (ui_panel_popup.png) ─────────────────
        private static void GeneratePopupPanel()
        {
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Color cardBlue = new Color(0.18f, 0.44f, 0.88f, 1f); // #2E70E0
            Color innerCream = new Color(0.99f, 0.97f, 0.93f, 1f); // Cream center
            Color darkBlueBorder = new Color(0.09f, 0.24f, 0.58f, 1f);

            int cornerRadius = 36;
            int borderWidth = 10;
            int creamInset = 24;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dOuter = RoundedBoxDistance(x, y, size, size, cornerRadius);
                    if (dOuter > 0)
                    {
                        // Antialias outer border
                        float a = Mathf.Clamp01(1f - dOuter);
                        tex.SetPixel(x, y, new Color(darkBlueBorder.r, darkBlueBorder.g, darkBlueBorder.b, a));
                        continue;
                    }

                    if (dOuter > -borderWidth)
                    {
                        tex.SetPixel(x, y, darkBlueBorder);
                        continue;
                    }

                    float dCream = RoundedBoxDistance(x, y, size, size, cornerRadius - creamInset);
                    if (dCream > -(creamInset - borderWidth))
                    {
                        // Blue rounded frame
                        float tGrad = (float)y / size;
                        Color frameCol = Color.Lerp(darkBlueBorder, cardBlue, tGrad);
                        tex.SetPixel(x, y, frameCol);
                    }
                    else
                    {
                        // Soft cream center
                        tex.SetPixel(x, y, innerCream);
                    }
                }
            }
            tex.Apply();

            SaveAndConfigureSprite(tex, "ui_panel_popup.png", new Vector4(48, 48, 48, 48));
        }

        // ── Helper Math ──────────────────────────────────────────────
        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float l2 = ab.sqrMagnitude;
            if (l2 < 0.0001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
            Vector2 proj = a + t * ab;
            return Vector2.Distance(p, proj);
        }

        private static float RoundedBoxDistance(int x, int y, int w, int h, int r)
        {
            int cx = x < w / 2 ? r : w - r;
            int cy = y < h / 2 ? r : h - r;

            bool inCornerX = (x < r) || (x >= w - r);
            bool inCornerY = (y < r) || (y >= h - r);

            if (inCornerX && inCornerY)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                return d - r;
            }

            int dx = Mathf.Max(0 - x, x - (w - 1));
            int dy = Mathf.Max(0 - y, y - (h - 1));
            return Mathf.Max(dx, dy);
        }

        private static void SaveAndConfigureSprite(Texture2D tex, string filename, Vector4 border)
        {
            string path = Path.Combine(ArtUIDir, filename);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif

