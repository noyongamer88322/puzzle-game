// ============================================================
// UIParticleEmitter.cs  –  Arrow Slide Puzzle
// High-performance Canvas UI particle emitter for celebrations:
//   • 4-5 color firework bursts (Gold, Magenta, Cyan, Orange, Purple)
//   • Floating upward sparkles with sine-wave drift and alpha twinkle
//   • Unscaled time updates for slow-mo compatibility
//   • Internal pooling to avoid runtime garbage allocation
//
// PRD Section 3.8 & Section 7: Assets/_Game/Scripts/UI/UIParticleEmitter.cs
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class UIParticleEmitter : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────
        [Header("Visual Asset")]
        [Tooltip("Sparkle / star particle sprite.")]
        public Sprite sparkleSprite;

        [Header("Colors (PRD 4-5 Firework Colors)")]
        public Color[] fireworkColors = new Color[]
        {
            new Color(1.0f, 0.84f, 0.0f),    // Gold / Bright Yellow (#FFD700)
            new Color(1.0f, 0.16f, 0.52f),   // Vivid Magenta (#FF2A85)
            new Color(0.0f, 0.90f, 1.0f),    // Bright Cyan (#00E5FF)
            new Color(1.0f, 0.48f, 0.0f),    // Bright Orange (#FF7A00)
            new Color(0.71f, 0.21f, 1.0f)    // Electric Purple (#B536FF)
        };

        [Header("Gravity & Drag")]
        public float gravity = -450f;
        public float drag = 0.96f;

        // ── Particle Data Structure ───────────────────────────
        private class UIParticle
        {
            public GameObject go;
            public RectTransform rect;
            public Image image;
            public Vector2 velocity;
            public float rotationSpeed;
            public float lifetime;
            public float maxLifetime;
            public float startScale;
            public Color baseColor;
            public bool isFloating;
            public float floatWobbleOffset;
        }

        private readonly List<UIParticle> _activeParticles = new List<UIParticle>(64);
        private readonly Stack<UIParticle> _particlePool    = new Stack<UIParticle>(64);
        private RectTransform _container;
        private bool _isSparkleFloatActive;
        private float _sparkleFloatSpawnTimer;

        // ── Unity Lifecycle ───────────────────────────────────
        private void Awake()
        {
            _container = GetComponent<RectTransform>();
            if (_container == null)
                _container = gameObject.AddComponent<RectTransform>();

#if UNITY_EDITOR
            if (sparkleSprite == null)
            {
                sparkleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/VFX/particle_sparkle.png");
            }
#endif
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            // 1. Update floating sparkle spawner
            if (_isSparkleFloatActive)
            {
                _sparkleFloatSpawnTimer -= dt;
                if (_sparkleFloatSpawnTimer <= 0f)
                {
                    _sparkleFloatSpawnTimer = Random.Range(0.06f, 0.14f);
                    SpawnSingleFloatSparkle();
                }
            }

            // 2. Update active particles
            for (int i = _activeParticles.Count - 1; i >= 0; i--)
            {
                var p = _activeParticles[i];
                p.lifetime += dt;

                if (p.lifetime >= p.maxLifetime)
                {
                    RecycleParticle(p);
                    _activeParticles.RemoveAt(i);
                    continue;
                }

                float progress = p.lifetime / p.maxLifetime;

                if (p.isFloating)
                {
                    // Floating sparkles: upward drift with gentle horizontal sine wobble
                    Vector2 pos = p.rect.anchoredPosition;
                    pos.y += p.velocity.y * dt;
                    pos.x += Mathf.Sin((Time.unscaledTime + p.floatWobbleOffset) * 3f) * 25f * dt;
                    p.rect.anchoredPosition = pos;

                    // Twinkle alpha
                    float twinkle = Mathf.Sin(progress * Mathf.PI);
                    Color c = p.baseColor;
                    c.a = twinkle;
                    p.image.color = c;

                    // Slight scale pulse
                    float s = p.startScale * (0.8f + 0.3f * Mathf.Sin(progress * Mathf.PI * 2f));
                    p.rect.localScale = new Vector3(s, s, 1f);
                }
                else
                {
                    // Firework burst particle
                    p.velocity.y += gravity * dt;
                    p.velocity *= Mathf.Pow(drag, dt * 60f);

                    p.rect.anchoredPosition += p.velocity * dt;
                    p.rect.Rotate(0, 0, p.rotationSpeed * dt);

                    // Fade & Shrink over life
                    float alpha = 1f - progress;
                    Color c = p.baseColor;
                    c.a = alpha;
                    p.image.color = c;

                    float s = p.startScale * (1f - progress * 0.7f);
                    p.rect.localScale = new Vector3(s, s, 1f);
                }
            }
        }

        // ── Public Emitter API ────────────────────────────────

        /// <summary>
        /// Triggers a multi-colored firework burst at the specified local Canvas position.
        /// </summary>
        public void EmitBurst(Vector2 localPos, int count = 28)
        {
            if (fireworkColors == null || fireworkColors.Length == 0) return;

            for (int i = 0; i < count; i++)
            {
                var p = GetOrCreateParticle();
                p.isFloating = false;
                p.rect.anchoredPosition = localPos;

                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = Random.Range(240f, 750f);
                p.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

                p.rotationSpeed = Random.Range(-360f, 360f);
                p.maxLifetime = Random.Range(0.65f, 1.25f);
                p.lifetime = 0f;

                p.startScale = Random.Range(0.6f, 1.25f);
                p.rect.localScale = Vector3.one * p.startScale;

                // Pick one of the 4-5 vibrant festive colors
                p.baseColor = fireworkColors[Random.Range(0, fireworkColors.Length)];
                p.image.color = p.baseColor;

                _activeParticles.Add(p);
            }
        }

        /// <summary>Starts generating continuous floating sparkles.</summary>
        public void StartSparkleFloat()
        {
            _isSparkleFloatActive = true;
        }

        /// <summary>Stops generating continuous floating sparkles.</summary>
        public void StopSparkleFloat()
        {
            _isSparkleFloatActive = false;
        }

        /// <summary>Clears and recycles all active particles.</summary>
        public void ClearAll()
        {
            _isSparkleFloatActive = false;
            for (int i = 0; i < _activeParticles.Count; i++)
            {
                RecycleParticle(_activeParticles[i]);
            }
            _activeParticles.Clear();
        }

        // ── Private Pool Helpers ──────────────────────────────

        private void SpawnSingleFloatSparkle()
        {
            var p = GetOrCreateParticle();
            p.isFloating = true;

            // Spawn across the screen width, starting from mid or bottom
            float spawnX = Random.Range(-480f, 480f);
            float spawnY = Random.Range(-650f, 200f);
            p.rect.anchoredPosition = new Vector2(spawnX, spawnY);

            p.velocity = new Vector2(0f, Random.Range(40f, 120f));
            p.rotationSpeed = Random.Range(-90f, 90f);
            p.floatWobbleOffset = Random.Range(0f, 10f);

            p.maxLifetime = Random.Range(1.8f, 3.2f);
            p.lifetime = 0f;

            p.startScale = Random.Range(0.45f, 0.9f);
            p.rect.localScale = Vector3.one * p.startScale;

            // Golden or soft tinted sparkle
            Color col = fireworkColors[Random.Range(0, fireworkColors.Length)];
            col = Color.Lerp(col, Color.white, 0.45f);
            p.baseColor = col;
            p.image.color = col;

            _activeParticles.Add(p);
        }

        private UIParticle GetOrCreateParticle()
        {
            UIParticle p;
            if (_particlePool.Count > 0)
            {
                p = _particlePool.Pop();
                p.go.SetActive(true);
            }
            else
            {
                p = new UIParticle();
                p.go = new GameObject("UIParticle", typeof(RectTransform), typeof(Image));
                p.rect = p.go.GetComponent<RectTransform>();
                p.rect.SetParent(_container, false);
                p.rect.sizeDelta = new Vector2(40f, 40f);

                p.image = p.go.GetComponent<Image>();
                p.image.sprite = sparkleSprite;
                p.image.raycastTarget = false;
            }
            return p;
        }

        private void RecycleParticle(UIParticle p)
        {
            if (p != null && p.go != null)
            {
                p.go.SetActive(false);
                _particlePool.Push(p);
            }
        }
    }
}

