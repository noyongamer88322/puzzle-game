// ============================================================
// BoosterController.cs  –  Arrow Slide Puzzle
// Master controller for Hint and Magnet booster mechanics.
//
// HINT:
//   • Tap -> Glowing orb with sparkle trail flies in a curved path
//     from the Hint button to the next free arrow (in solve order).
//   • Arrow turns green (#3FD04A) and pulses gently.
//   • User must tap the green arrow themselves.
//   • When tapped, it slides out and shoots a white streak line across
//     the entire screen in the exit direction.
//   • First time FREE, then 650 coins. Insufficient coins fires OnShopRequested.
//
// MAGNET:
//   • Tap -> Big red-and-blue magnet pops in + bounces at grid/screen center.
//   • 3 free arrows (in solve order) are pulled towards the magnet in curved
//     paths with sparkle trails and magnet pull dust.
//   • Absorbed arrows are cleared. Magnet bounces and pops out.
//   • First time FREE, then 1300 coins. Insufficient coins fires OnShopRequested.
//
// BOTH:
//   • Updates coin pill and price/FREE tags.
//   • Disabled / ignored if no free arrows exist or an arrow is sliding.
//
// PRD Sections 3.5, 3.6, 5 & 7: Assets/_Game/Scripts/Gameplay/BoosterController.cs
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArrowPuzzle.Core;
using ArrowPuzzle.Systems;
using ArrowPuzzle.UI;

namespace ArrowPuzzle.Gameplay
{
    [DisallowMultipleComponent]
    public class BoosterController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        public static BoosterController Instance { get; private set; }

        // ── Events ────────────────────────────────────────────
        /// <summary>Fired when player has insufficient coins for a booster.</summary>
        public static event Action OnShopRequested;

        /// <summary>Fired when Hint booster starts.</summary>
        public static event Action<ArrowView> OnHintUsed;

        /// <summary>Fired when Magnet booster starts.</summary>
        public static event Action<List<ArrowView>> OnMagnetUsed;

        // ── Inspector / Sprites ───────────────────────────────
        [Header("Magnet Visuals")]
        [Tooltip("Magnet icon sprite (PRD: Art/UI/icon_magnet.png).")]
        public Sprite magnetSprite;

        [Header("Sparkle / Particle Visuals")]
        [Tooltip("Sparkle particle texture (PRD: Art/VFX/particle_sparkle.png).")]
        public Texture2D sparkleTexture;

        // ── Runtime ───────────────────────────────────────────
        private Camera _cam;

        // ── Unity Lifecycle ───────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _cam = Camera.main;

#if UNITY_EDITOR
            TryAutoLoadAssets();
#endif
        }

        private void Update()
        {
            // Debug hotkeys for quick testing: H = Hint, M = Magnet, C = +1000 Coins
            if (Input.GetKeyDown(KeyCode.H))
            {
                UseHint();
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                UseMagnet();
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                GameState.Instance?.AddCoins(1000);
                GameplayHUD.Instance?.AddCoins(1000);
                Debug.Log($"[Booster] Debug: Added 1000 Coins. New Total: {GameState.Instance?.Coins}");
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                OnShopRequested?.Invoke();
                Debug.Log("[Booster] Debug: S key pressed -> OnShopRequested fired.");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API: Hint ──────────────────────────────────

        /// <summary>
        /// Triggered when Hint button is tapped.
        /// </summary>
        public void UseHint()
        {
            var gc = GameplayController.Instance;
            if (gc == null || gc.IsLevelEnded || gc.IsBoosterRunning || gc.HasAnySlidingArrow())
            {
                Debug.Log("[Booster] Hint ignored: game state busy or ended.");
                return;
            }

            // 1. Check for free arrows in solve order
            var freeArrows = gc.GetFreeArrowsInSolveOrder();
            // Filter out already hinted arrow if possible
            ArrowView targetArrow = null;
            for (int i = 0; i < freeArrows.Count; i++)
            {
                if (!freeArrows[i].IsHinted)
                {
                    targetArrow = freeArrows[i];
                    break;
                }
            }
            if (targetArrow == null && freeArrows.Count > 0)
                targetArrow = freeArrows[0];

            if (targetArrow == null)
            {
                Debug.Log("[Booster] Hint ignored: No free arrows available!");
                GameplayHUD.Instance?.hintButton?.PlayErrorShake();
                return;
            }

            // 2. Economy check
            bool isFree = GameState.Instance != null && GameState.Instance.IsHintFree;
            int cost = Economy.HintCost; // 650

            if (isFree)
            {
                // First time free
                GameState.Instance.ConsumeFreeHint();
                GameplayHUD.Instance?.hintButton?.SetFreeTag(false);
                GameplayHUD.Instance?.hintButton?.SetCost(cost);
                Debug.Log("[Booster] Hint used for FREE (first time).");
            }
            else
            {
                // Requires 650 coins
                int currentCoins = GameState.Instance != null ? GameState.Instance.Coins : 0;
                if (currentCoins < cost)
                {
                    Debug.LogWarning($"[Booster] Insufficient coins for Hint! Need {cost}, have {currentCoins}. Firing OnShopRequested.");
                    GameplayHUD.Instance?.hintButton?.PlayErrorShake();
                    OnShopRequested?.Invoke();
                    return;
                }

                GameState.Instance.SpendCoins(cost);
                GameplayHUD.Instance?.coinPill?.DeductCoins(cost);
                Debug.Log($"[Booster] Hint used: -{cost} coins. Remaining: {GameState.Instance.Coins}.");
            }

            // 3. Launch glowing orb sequence
            StartCoroutine(HintOrbSequence(targetArrow));
            OnHintUsed?.Invoke(targetArrow);
        }

        // ── Public API: Magnet ────────────────────────────────

        /// <summary>
        /// Triggered when Magnet button is tapped.
        /// </summary>
        public void UseMagnet()
        {
            var gc = GameplayController.Instance;
            if (gc == null || gc.IsLevelEnded || gc.IsBoosterRunning || gc.HasAnySlidingArrow())
            {
                Debug.Log("[Booster] Magnet ignored: game state busy or ended.");
                return;
            }

            // 1. Check for free arrows in solve order (up to 3)
            var freeArrows = gc.GetFreeArrowsInSolveOrder();
            if (freeArrows == null || freeArrows.Count == 0)
            {
                Debug.Log("[Booster] Magnet ignored: No free arrows available!");
                GameplayHUD.Instance?.magnetButton?.PlayErrorShake();
                return;
            }

            int count = Mathf.Min(3, freeArrows.Count);
            var targets = freeArrows.GetRange(0, count);

            // 2. Economy check
            bool isFree = GameState.Instance != null && GameState.Instance.IsMagnetFree;
            int cost = Economy.MagnetCost; // 1300

            if (isFree)
            {
                // First time free
                GameState.Instance.ConsumeFreeMagnet();
                GameplayHUD.Instance?.magnetButton?.SetFreeTag(false);
                GameplayHUD.Instance?.magnetButton?.SetCost(cost);
                Debug.Log("[Booster] Magnet used for FREE (first time).");
            }
            else
            {
                // Requires 1300 coins
                int currentCoins = GameState.Instance != null ? GameState.Instance.Coins : 0;
                if (currentCoins < cost)
                {
                    Debug.LogWarning($"[Booster] Insufficient coins for Magnet! Need {cost}, have {currentCoins}. Firing OnShopRequested.");
                    GameplayHUD.Instance?.magnetButton?.PlayErrorShake();
                    OnShopRequested?.Invoke();
                    return;
                }

                GameState.Instance.SpendCoins(cost);
                GameplayHUD.Instance?.coinPill?.DeductCoins(cost);
                Debug.Log($"[Booster] Magnet used: -{cost} coins. Remaining: {GameState.Instance.Coins}.");
            }

            // 3. Launch Magnet pull sequence
            StartCoroutine(MagnetSequence(targets));
            OnMagnetUsed?.Invoke(targets);
        }

        // ── Hint Coroutine: Glowing Orb with Sparkle Trail ────

        private IEnumerator HintOrbSequence(ArrowView targetArrow)
        {
            var gc = GameplayController.Instance;
            if (gc != null) gc.IsBoosterRunning = true;

            if (_cam == null) _cam = Camera.main;

            // Start position: above the Hint button
            Vector3 startPos = Vector3.zero;
            var hintBtn = GameplayHUD.Instance?.hintButton;
            if (hintBtn != null && _cam != null)
            {
                Vector3 screenPoint = hintBtn.transform.position;
                startPos = _cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 10f));
            }
            else
            {
                startPos = new Vector3(3f, 4f, 0f);
            }
            startPos.z = -2f;

            Vector3 endPos = targetArrow != null ? targetArrow.transform.position : Vector3.zero;
            endPos.z = -2f;

            // Curved arc control point (arch upward and inward)
            Vector3 mid = (startPos + endPos) * 0.5f;
            Vector3 controlPoint = mid + Vector3.up * 1.8f + Vector3.left * 1.2f;

            // Create Glowing Orb GameObject
            var orbGO = new GameObject("HintGlowingOrb");
            orbGO.transform.position = startPos;

            // Visual Sprite: glowing gold circle
            var orbRenderer = orbGO.AddComponent<SpriteRenderer>();
            orbRenderer.sprite = CreateGlowOrbSprite();
            orbRenderer.color = Color.white;
            orbRenderer.sortingOrder = 30;
            orbGO.transform.localScale = Vector3.one * 0.75f;

            // Sparkle TrailRenderer
            var trailRenderer = orbGO.AddComponent<TrailRenderer>();
            trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trailRenderer.receiveShadows = false;
            trailRenderer.sortingOrder = 29;
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trailRenderer.startWidth = 0.42f;
            trailRenderer.endWidth = 0.02f;
            trailRenderer.time = 0.4f;
            trailRenderer.minVertexDistance = 0.05f;

            Gradient trailGrad = new Gradient();
            trailGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.4f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.05f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.6f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trailRenderer.colorGradient = trailGrad;

            // Flying animation (curved Bézier)
            float duration = 0.75f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease-in-out smoothstep
                float eased = Mathf.SmoothStep(0f, 1f, t);

                // Quadratic Bézier curve
                Vector3 p = (1 - eased) * (1 - eased) * startPos + 2 * (1 - eased) * eased * controlPoint + eased * eased * endPos;
                orbGO.transform.position = p;

                // Subtle pulsating scale while flying
                float pulse = 0.75f + 0.12f * Mathf.Sin(t * Mathf.PI * 4f);
                orbGO.transform.localScale = Vector3.one * pulse;

                yield return null;
            }

            // Reached target arrow!
            AudioManager.Instance?.PlayHint();

            // Spawn sparkle burst at arrival
            if (VFXManager.Instance != null)
                VFXManager.Instance.SpawnSparkleBurst(endPos, new Color(1f, 0.9f, 0.3f), 14);
            else
                SpawnSparkleBurst(endPos, new Color(1f, 0.9f, 0.3f), 14);

            Destroy(orbGO);

            // Turn arrow green (#3FD04A) - user taps it themselves
            if (targetArrow != null && !targetArrow.IsCleared)
            {
                targetArrow.SetHinted(true);
            }

            if (gc != null) gc.IsBoosterRunning = false;
        }

        // ── Magnet Coroutine: Pop-in + Pull 3 Free Arrows ─────

        private IEnumerator MagnetSequence(List<ArrowView> targetArrows)
        {
            var gc = GameplayController.Instance;
            if (gc != null) gc.IsBoosterRunning = true;

            // Unregister target arrows immediately so other logic treats them as captured
            foreach (var a in targetArrows)
            {
                if (a != null) gc.UnregisterArrow(a);
            }

            // Magnet Position: screen / grid center
            Vector3 center = GridManager.Instance != null
                ? (Vector3)(GridManager.Instance.GridOrigin +
                    new Vector2(GridManager.Instance.gridCols * GridManager.Instance.CellSize * 0.5f,
                                GridManager.Instance.gridRows * GridManager.Instance.CellSize * 0.5f))
                : Vector3.zero;
            center.z = -3f;

            // 1. Create Magnet GameObject
            var magnetGO = new GameObject("MagnetBooster");
            magnetGO.transform.position = center;
            var magnetRenderer = magnetGO.AddComponent<SpriteRenderer>();

            if (magnetSprite == null)
            {
#if UNITY_EDITOR
                magnetSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/icon_magnet.png");
#endif
            }
            magnetRenderer.sprite = magnetSprite;
            magnetRenderer.sortingOrder = 35;
            magnetGO.transform.localScale = Vector3.zero;

            // 2. Pop-in + Overshoot Bounce
            AudioManager.Instance?.PlayMagnet();
            SpawnSparkleBurst(center, new Color(0.2f, 0.7f, 1f), 16); // Blue pull dust

            float popDur = 0.35f;
            float elapsed = 0f;
            Vector3 targetScale = Vector3.one * 2.3f; // Big prominent magnet

            while (elapsed < popDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popDur);
                // Overshoot: 0 -> 1.25 -> 1.0
                float s = EaseOutBack(t);
                magnetGO.transform.localScale = targetScale * s;
                yield return null;
            }
            magnetGO.transform.localScale = targetScale;

            yield return new WaitForSeconds(0.08f);

            // 3. Pull each target arrow towards the magnet
            int remainingToArrive = targetArrows.Count;

            for (int i = 0; i < targetArrows.Count; i++)
            {
                var arrow = targetArrows[i];
                if (arrow == null)
                {
                    remainingToArrive--;
                    continue;
                }

                float delay = i * 0.08f; // Staggered curved suction
                var arrowData = arrow.Data;

                arrow.PullToMagnet(center, delay, () =>
                {
                    // Trigger cleared event for each absorbed arrow
                    if (arrowData != null)
                        GameplayController.RaiseArrowCleared(arrowData);

                    // Pull dust burst at magnet pole (PRD Section 5: magnet pull dust)
                    if (VFXManager.Instance != null)
                        VFXManager.Instance.SpawnMagnetPullDust(center, center, 8);
                    SpawnSparkleBurst(center, new Color(1f, 0.3f, 0.3f), 8);
                    remainingToArrive--;
                });
            }

            // Wait until all target arrows have arrived and cleared
            float timeout = 2.0f;
            while (remainingToArrive > 0 && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            // 4. Magnet exit bounce + pop out
            elapsed = 0f;
            float exitDur = 0.25f;
            while (elapsed < exitDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / exitDur);
                // Quick pulse then shrink to 0
                float scaleFactor = t < 0.3f ? (1f + t * 0.5f) : (1.15f - (t - 0.3f) * 1.64f);
                scaleFactor = Mathf.Max(0f, scaleFactor);
                magnetGO.transform.localScale = targetScale * scaleFactor;
                yield return null;
            }

            SpawnSparkleBurst(center, Color.white, 12);
            Destroy(magnetGO);

            if (gc != null)
            {
                gc.IsBoosterRunning = false;
                gc.CheckWinCondition();
            }
        }

        // ── VFX Helpers ───────────────────────────────────────

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>
        /// Creates a high-res glowing orb texture procedurally for the Hint orb.
        /// </summary>
        private static Sprite CreateGlowOrbSprite()
        {
            const int res = 128;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                name = "GlowOrbProcedural",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2((res - 1) * 0.5f, (res - 1) * 0.5f);
            float maxR = res * 0.5f;

            var cols = new Color[res * res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float norm = Mathf.Clamp01(dist / maxR);

                    // Inner bright core + soft outer gold glow
                    float core = Mathf.Clamp01(1f - norm * 2.2f);
                    float glow = Mathf.Pow(1f - norm, 2.5f);

                    Color col = Color.Lerp(new Color(1f, 0.95f, 0.4f, 1f), new Color(1f, 0.65f, 0.1f, 0.8f), norm);
                    col = Color.Lerp(col, Color.white, core);
                    col.a = Mathf.Clamp01(glow * 0.95f);

                    cols[y * res + x] = col;
                }
            }

            tex.SetPixels(cols);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Spawns a burst of tiny sparkle particles at world position.
        /// </summary>
        public void SpawnSparkleBurst(Vector3 position, Color color, int count = 12)
        {
            var burstGO = new GameObject("SparkleBurst");
            burstGO.transform.position = position;

            var ps = burstGO.AddComponent<ParticleSystem>();
            var pmain = ps.main;
            pmain.duration = 0.5f;
            pmain.loop = false;
            pmain.startLifetime = 0.45f;
            pmain.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            pmain.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            pmain.startColor = color;
            pmain.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.sortingOrder = 38;
            rend.material = new Material(Shader.Find("Sprites/Default"));

            ps.Play();
            Destroy(burstGO, 0.7f);
        }

#if UNITY_EDITOR
        private void TryAutoLoadAssets()
        {
            if (magnetSprite == null)
            {
                magnetSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/icon_magnet.png");
            }
            if (sparkleTexture == null)
            {
                sparkleTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/_Game/Art/VFX/particle_sparkle.png");
            }
        }
#endif
    }
}
