// ============================================================
// AudioManager.cs
// Persistent singleton.  Plays SFX and music via AudioSource.
// Clip slots are assigned in the prefab inspector.
// ============================================================

using UnityEngine;
using ArrowPuzzle.Systems;

namespace ArrowPuzzle
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindFirstObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // ── Inspector slots ───────────────────────────────────
        [Header("Music (Soft Loop)")]
        [SerializeField] private AudioClip musicLoop;

        [Header("SFX (PRD Section 5)")]
        [Tooltip("Arrow tap press feedback sound.")]
        [SerializeField] private AudioClip sfxTap;

        [Tooltip("Arrow sliding whoosh sound.")]
        [SerializeField] private AudioClip sfxWhoosh;

        [Tooltip("Blocked tap error sound.")]
        [SerializeField] private AudioClip sfxError;

        [Tooltip("Heart crack / break sound.")]
        [SerializeField] private AudioClip sfxHeartBreak;

        [Tooltip("Hint glowing orb flight/arrival sound.")]
        [SerializeField] private AudioClip sfxHintOrb;

        [Tooltip("Magnet suction / pull sound.")]
        [SerializeField] private AudioClip sfxMagnet;

        [Tooltip("Coin collected / count-up roll sound.")]
        [SerializeField] private AudioClip sfxCoinCollect;

        [Tooltip("Star reward pop sound.")]
        [SerializeField] private AudioClip sfxStar;

        [Tooltip("Level cleared celebration jingle.")]
        [SerializeField] private AudioClip sfxLevelClearJingle;

        [Tooltip("Generic UI button click sound.")]
        [SerializeField] private AudioClip sfxButtonClick;

        [Tooltip("Popup panel scale-in open sound.")]
        [SerializeField] private AudioClip sfxPopupOpen;

        // ── Private ───────────────────────────────────────────
        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        // ── Unity lifecycle ───────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Expect two AudioSources on the GameObject: index 0 = music, index 1 = sfx.
            var sources = GetComponents<AudioSource>();
            _musicSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
            _sfxSource   = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

            _musicSource.loop = true;
            AutoLoadClipsIfEmpty();
            ApplySettings();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void AutoLoadClipsIfEmpty()
        {
#if UNITY_EDITOR
            if (musicLoop == null)
                musicLoop = LoadClip("Audio/Music/music_soft_loop") ?? LoadClip("Audio/Music/musicLoop");

            if (sfxTap == null)               sfxTap = LoadClip("Audio/SFX/sfx_tap") ?? LoadClip("Audio/SFX/tap");
            if (sfxWhoosh == null)            sfxWhoosh = LoadClip("Audio/SFX/sfx_whoosh") ?? LoadClip("Audio/SFX/whoosh");
            if (sfxError == null)             sfxError = LoadClip("Audio/SFX/sfx_error") ?? LoadClip("Audio/SFX/error");
            if (sfxHeartBreak == null)        sfxHeartBreak = LoadClip("Audio/SFX/sfx_heart_break") ?? LoadClip("Audio/SFX/heart_break");
            if (sfxHintOrb == null)           sfxHintOrb = LoadClip("Audio/SFX/sfx_hint_orb") ?? LoadClip("Audio/SFX/hint_orb");
            if (sfxMagnet == null)            sfxMagnet = LoadClip("Audio/SFX/sfx_magnet") ?? LoadClip("Audio/SFX/magnet");
            if (sfxCoinCollect == null)       sfxCoinCollect = LoadClip("Audio/SFX/sfx_coin_collect") ?? LoadClip("Audio/SFX/coin_collect");
            if (sfxStar == null)              sfxStar = LoadClip("Audio/SFX/sfx_star") ?? LoadClip("Audio/SFX/star");
            if (sfxLevelClearJingle == null)  sfxLevelClearJingle = LoadClip("Audio/SFX/sfx_level_cleared") ?? LoadClip("Audio/SFX/level_cleared");
            if (sfxButtonClick == null)       sfxButtonClick = LoadClip("Audio/SFX/sfx_button_click") ?? LoadClip("Audio/SFX/button_click");
            if (sfxPopupOpen == null)         sfxPopupOpen = LoadClip("Audio/SFX/sfx_popup_open") ?? LoadClip("Audio/SFX/popup_open");
#endif
        }

#if UNITY_EDITOR
        private static AudioClip LoadClip(string relativePath)
        {
            // Check Assets/_Game/ relative
            string path1 = "Assets/_Game/" + relativePath + ".wav";
            string path2 = "Assets/_Game/" + relativePath + ".mp3";
            string path3 = "Assets/_Game/" + relativePath + ".ogg";

            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path1);
            if (clip != null) return clip;
            clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path2);
            if (clip != null) return clip;
            return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path3);
        }
#endif

        // ── Public API ────────────────────────────────────────

        public void ApplySettings()
        {
            var state = GameState.Instance;
            if (state == null) return;

            if (_musicSource != null)
                _musicSource.mute = !state.MusicOn;
            if (_sfxSource != null)
                _sfxSource.mute   = !state.SoundOn;

            if (state.MusicOn && _musicSource != null && !_musicSource.isPlaying && musicLoop != null)
                _musicSource.Play();
        }

        public void PlayMusic()
        {
            if (musicLoop == null || GameState.Instance == null || !GameState.Instance.MusicOn) return;
            if (_musicSource == null) return;
            _musicSource.clip = musicLoop;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource != null) _musicSource.Stop();
        }

        // ── SFX API (PRD Section 5) ───────────────────────────
        public void PlayTap()              => PlaySFX(sfxTap);
        public void PlayWhoosh()           => PlaySFX(sfxWhoosh);
        public void PlayError()            => PlaySFX(sfxError);
        public void PlayHeartBreak()       => PlaySFX(sfxHeartBreak);
        public void PlayHintOrb()          => PlaySFX(sfxHintOrb);
        public void PlayMagnet()           => PlaySFX(sfxMagnet);
        public void PlayCoinCollect()      => PlaySFX(sfxCoinCollect);
        public void PlayStar()             => PlaySFX(sfxStar);
        public void PlayLevelClearJingle() => PlaySFX(sfxLevelClearJingle);
        public void PlayButtonClick()      => PlaySFX(sfxButtonClick);
        public void PlayPopupOpen()        => PlaySFX(sfxPopupOpen);

        // Backward compatibility
        public void PlayArrowSlide() => PlayWhoosh();
        public void PlayBlockedTap() => PlayError();
        public void PlayLevelClear() => PlayLevelClearJingle();
        public void PlayHint()       => PlayHintOrb();
        public void PlayCoin()       => PlayCoinCollect();
        public void PlayButton()     => PlayButtonClick();

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null || GameState.Instance == null || !GameState.Instance.SoundOn) return;
            if (_sfxSource != null)
                _sfxSource.PlayOneShot(clip);
        }
    }
}

