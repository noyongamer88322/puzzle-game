// ============================================================
// VFXManager.cs  –  Arrow Slide Puzzle
// Central Particle & Visual Effects Manager (PRD Section 5 & 7).
// Manages Particle Systems for:
//   • Sparkle (small twinkling stars, exit sparkle)
//   • Firework Burst (4-5 vibrant celebration bursts)
//   • Confetti (fluttering colorful shapes)
//   • Coin Burst (golden coin radial bursts)
//   • Glow Orb Trail (hint booster flight trail)
//   • Star Burst (bright golden star burst)
//   • Magnet Pull Dust (suction dust towards magnet pole)
//
// Performance tuned for mid-range Android (60fps target):
//   • Limited particle count per burst (8 - 25)
//   • Short lifetimes (0.35s - 0.75s)
//   • Mobile Sprites/Default unlit shader
//   • Internal pooling to prevent runtime GC allocations
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace ArrowPuzzle.Gameplay
{
    [DisallowMultipleComponent]
    public class VFXManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        private static VFXManager _instance;
        public static VFXManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var found = Object.FindFirstObjectByType<VFXManager>();
                    if (found != null)
                    {
                        _instance = found;
                    }
                    else
                    {
                        var go = new GameObject("VFXManager");
                        _instance = go.AddComponent<VFXManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // ── Inspector Sprites (Assets/_Game/Art/VFX) ──────────
        [Header("VFX Textures (PRD Section 5)")]
        [Tooltip("Sparkle star sprite (Art/VFX/particle_sparkle.png)")]
        public Sprite sparkleSprite;

        [Tooltip("Confetti ribbon sprite (Art/VFX/particle_confetti.png)")]
        public Sprite confettiSprite;

        [Tooltip("Coin particle sprite (Art/VFX/particle_coin.png)")]
        public Sprite coinSprite;

        [Tooltip("Star particle sprite (Art/VFX/particle_star.png)")]
        public Sprite starSprite;

        [Tooltip("Soft dust / glow particle sprite (Art/VFX/particle_dust.png)")]
        public Sprite dustSprite;

        // ── Firework Colors (PRD Section 5: 4-5 bright colors) 
        private static readonly Color[] FireworkColors = new Color[]
        {
            new Color(1.0f, 0.71f, 0.18f), // Yellow-Orange (#FFB52E)
            new Color(1.0f, 0.24f, 0.75f), // Magenta (#FF3DBE)
            new Color(0.61f, 0.17f, 1.0f),  // Purple (#9B2BFF)
            new Color(0.24f, 0.36f, 1.0f),  // Blue (#3D5BFF)
            new Color(0.31f, 0.76f, 1.0f)   // Cyan (#4FC3FF)
        };

        // ── Object Pooling ────────────────────────────────────
        private Material _vfxMaterial;
        private readonly Queue<ParticleSystem> _particlePool = new Queue<ParticleSystem>(16);

        // ── Unity Lifecycle ───────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            var shader = Shader.Find("Sprites/Default");
            _vfxMaterial = new Material(shader) { name = "VFX_Default_Mat" };

            AutoLoadSpritesIfEmpty();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (_vfxMaterial != null) Destroy(_vfxMaterial);
        }

        private void AutoLoadSpritesIfEmpty()
        {
#if UNITY_EDITOR
            if (sparkleSprite == null)
                sparkleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/VFX/particle_sparkle.png");
            if (confettiSprite == null)
                confettiSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/VFX/particle_confetti.png");
            if (coinSprite == null)
                coinSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/VFX/particle_coin.png");
            if (starSprite == null)
                starSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/VFX/particle_star.png");
            if (dustSprite == null)
                dustSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/VFX/particle_dust.png");
#endif
        }

        // ── Public Particle System Spawners ───────────────────

        /// <summary>
        /// PRD 5: "Arrow exit: baire gele chhoto sparkle."
        /// Spawns a small burst of sparkles when an arrow exits the board.
        /// </summary>
        public void SpawnExitSparkle(Vector3 position, Color color, int count = 10)
        {
            var ps = GetOrCreateParticleSystem("ExitSparkle");
            ConfigureSystem(ps, position, sparkleSprite, sortingOrder: 25);

            var main = ps.main;
            main.duration = 0.45f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            main.startColor = color;
            main.gravityModifier = 0.15f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            ps.Play();
            ReturnToPoolAfterDelay(ps, 0.5f);
        }

        /// <summary>
        /// Spawns a general sparkle burst with custom color.
        /// </summary>
        public void SpawnSparkleBurst(Vector3 position, Color color, int count = 12)
        {
            var ps = GetOrCreateParticleSystem("SparkleBurst");
            ConfigureSystem(ps, position, sparkleSprite, sortingOrder: 30);

            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.30f);
            main.startColor = color;
            main.gravityModifier = 0.1f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            ps.Play();
            ReturnToPoolAfterDelay(ps, 0.55f);
        }

        /// <summary>
        /// PRD 5: "fireworks (4-5 rang-er burst)"
        /// Spawns multi-colored firework particles.
        /// </summary>
        public void SpawnFireworkBurst(Vector3 position, int count = 20)
        {
            var ps = GetOrCreateParticleSystem("FireworkBurst");
            ConfigureSystem(ps, position, sparkleSprite, sortingOrder: 35);

            var main = ps.main;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 6.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);

            // Random between firework colors
            Color randomColor = FireworkColors[Random.Range(0, FireworkColors.Length)];
            main.startColor = randomColor;
            main.gravityModifier = 0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            ps.Play();
            ReturnToPoolAfterDelay(ps, 0.7f);
        }

        /// <summary>
        /// PRD 5: "confetti" - colorful fluttering pieces.
        /// </summary>
        public void SpawnConfettiBurst(Vector3 position, int count = 22)
        {
            var ps = GetOrCreateParticleSystem("ConfettiBurst");
            ConfigureSystem(ps, position, confettiSprite != null ? confettiSprite : sparkleSprite, sortingOrder: 32);

            var main = ps.main;
            main.duration = 0.75f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = FireworkColors[Random.Range(0, FireworkColors.Length)];
            main.gravityModifier = 0.8f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.2f;

            ps.Play();
            ReturnToPoolAfterDelay(ps, 0.85f);
        }

        /// <summary>
        /// PRD 5: "coin burst" - radial golden coins popping outward.
        /// </summary>
        public void SpawnCoinBurst(Vector3 position, int count = 12)
        {
            var ps = GetOrCreateParticleSystem("CoinBurst");
            ConfigureSystem(ps, position, coinSprite != null ? coinSprite : sparkleSprite, sortingOrder: 40);

            var main = ps.main;
            main.duration = 0.55f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.40f);
            main.startColor = Color.white; // texture has own gold color
            main.gravityModifier = 0.6f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            ps.Play();
            ReturnToPoolAfterDelay(ps, 0.6f);
        }

        /// <summary>
        /// PRD 5: "star burst" - golden glowing stars.
        /// </summary>
        public void SpawnStarBurst(Vector3 position, int count = 10)
        {
            var ps = GetOrCreateParticleSystem("StarBurst");
            ConfigureSystem(ps, position, starSprite != null ? starSprite : sparkleSprite, sortingOrder: 38);

            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.38f);
            main.startColor = new Color(1f, 0.88f, 0.25f);
            main.gravityModifier = 0.2f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            ps.Play();
            ReturnToPoolAfterDelay(ps, 0.55f);
        }

        /// <summary>
        /// PRD 5: "magnet pull dust" - suction dust pulled towards the magnet.
        /// </summary>
        public void SpawnMagnetPullDust(Vector3 magnetPosition, Vector3 fromPosition, int count = 8)
        {
            var ps = GetOrCreateParticleSystem("MagnetPullDust");
            ConfigureSystem(ps, fromPosition, dustSprite != null ? dustSprite : sparkleSprite, sortingOrder: 34);

            var main = ps.main;
            main.duration = 0.45f;
            main.startLifetime = 0.4f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor = new Color(0.35f, 0.65f, 1f, 0.8f); // Blue-ish magnetic glow
            main.gravityModifier = 0f;

            Vector3 pullDir = (magnetPosition - fromPosition).normalized;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.0f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.rotation = Quaternion.LookRotation(pullDir).eulerAngles;
            shape.angle = 20f;
            shape.radius = 0.15f;

            ps.Play();
            ReturnToPoolAfterDelay(ps, 0.45f);
        }

        /// <summary>
        /// PRD 5: "glow orb trail" - creates and configures a TrailRenderer on target.
        /// </summary>
        public TrailRenderer CreateGlowOrbTrail(GameObject target)
        {
            if (target == null) return null;

            var trail = target.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = target.AddComponent<TrailRenderer>();

            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows    = false;
            trail.sortingOrder      = 30;
            trail.material          = _vfxMaterial;
            trail.startWidth        = 0.40f;
            trail.endWidth          = 0.02f;
            trail.time              = 0.38f;
            trail.minVertexDistance = 0.04f;

            Gradient trailGrad = new Gradient();
            trailGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.35f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.05f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.6f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = trailGrad;

            return trail;
        }

        // ── Internal Pooling Helpers ──────────────────────────

        private ParticleSystem GetOrCreateParticleSystem(string effectName)
        {
            while (_particlePool.Count > 0)
            {
                var candidate = _particlePool.Dequeue();
                if (candidate != null)
                {
                    candidate.gameObject.name = effectName;
                    candidate.gameObject.SetActive(true);
                    return candidate;
                }
            }

            var go = new GameObject(effectName);
            go.transform.SetParent(transform);
            var ps = go.AddComponent<ParticleSystem>();
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.material = _vfxMaterial;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            return ps;
        }

        private void ConfigureSystem(ParticleSystem ps, Vector3 position, Sprite sprite, int sortingOrder)
        {
            ps.transform.position = position;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.sortingOrder = sortingOrder;

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // Apply texture sprite if available
            if (sprite != null)
            {
                var tsheet = ps.textureSheetAnimation;
                tsheet.enabled = true;
                tsheet.mode = ParticleSystemAnimationMode.Sprites;
                if (tsheet.spriteCount == 0)
                    tsheet.AddSprite(sprite);
                else
                    tsheet.SetSprite(0, sprite);
            }
        }

        private void ReturnToPoolAfterDelay(ParticleSystem ps, float delay)
        {
            StartCoroutine(PoolCoroutine(ps, delay));
        }

        private System.Collections.IEnumerator PoolCoroutine(ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
                ps.transform.SetParent(transform);
                _particlePool.Enqueue(ps);
            }
        }
    }
}
