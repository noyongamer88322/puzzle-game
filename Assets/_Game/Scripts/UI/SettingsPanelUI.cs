// ============================================================
// SettingsPanelUI.cs  –  Arrow Slide Puzzle
// Settings popup panel (PRD Section 3.11):
//   • Sound on/off toggle
//   • Music on/off toggle
//   • Vibration on/off toggle
//   • Privacy Policy link (opens URL)
//   • Restore Purchases button (mock – replace with platform billing later)
//   • Close (×) button
//
// Attach to a child panel GameObject inside any Canvas.
// Open via  SettingsPanelUI.Instance.Open()  from the gear button.
// Uses UIPanelOvershoot for the scale-in / scale-out animation.
// Reads/writes settings through GameState, which persists via SaveManager.
//
// PRD Section 7: Assets/_Game/Scripts/UI/SettingsPanelUI.cs
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArrowPuzzle.Systems;
using ArrowPuzzle.Core;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class SettingsPanelUI : MonoBehaviour
    {
        // ── Singleton ───────────────────────────────────────────────
        private static SettingsPanelUI _instance;
        public static SettingsPanelUI Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<SettingsPanelUI>(true);
                return _instance;
            }
            private set => _instance = value;
        }

        private bool _isOpen;
        public bool IsOpen => _isOpen;

        // ── Inspector: Panel Animation ───────────────────────────────
        [Header("Panel Animation")]
        [Tooltip("UIPanelOvershoot on the panel card – drives scale-in/out animation.")]
        public UIPanelOvershoot panelOvershoot;

        // ── Inspector: Sound ─────────────────────────────────────────
        [Header("Sound Toggle")]
        [Tooltip("Sound on/off toggle button.")]
        public Button soundToggleButton;

        [Tooltip("Label showing 'Sound: ON' or 'Sound: OFF'.")]
        public TextMeshProUGUI soundLabel;

        [Tooltip("Icon Image – green when ON, grey when OFF.")]
        public Image soundIcon;

        // ── Inspector: Music ─────────────────────────────────────────
        [Header("Music Toggle")]
        [Tooltip("Music on/off toggle button.")]
        public Button musicToggleButton;

        [Tooltip("Music label.")]
        public TextMeshProUGUI musicLabel;

        [Tooltip("Music icon Image.")]
        public Image musicIcon;

        // ── Inspector: Vibration ─────────────────────────────────────
        [Header("Vibration Toggle")]
        [Tooltip("Vibration on/off toggle button.")]
        public Button vibrationToggleButton;

        [Tooltip("Vibration label.")]
        public TextMeshProUGUI vibrationLabel;

        [Tooltip("Vibration icon Image.")]
        public Image vibrationIcon;

        // ── Inspector: Bottom Buttons ────────────────────────────────
        [Header("Bottom Buttons")]
        [Tooltip("Opens privacyPolicyURL in the device browser.")]
        public Button privacyPolicyButton;

        [Tooltip("Version label showing app version (e.g. v1.0.0).")]
        public TextMeshProUGUI versionLabel;

        [Tooltip("Triggers IAP restore flow (optional/deprecated).")]
        public Button restorePurchasesButton;

        [Tooltip("Text on the Restore button – updated to give feedback.")]
        public TextMeshProUGUI restoreLabel;

        [Tooltip("Close (×) button – hides the panel.")]
        public Button closeButton;

        // ── Config ───────────────────────────────────────────────────
        [Header("Config")]
        [Tooltip("URL opened by the Privacy Policy button.")]
        public string privacyPolicyURL = ArrowPuzzle.Core.AppLinks.PrivacyPolicyUrl;

        [Tooltip("Icon tint when setting is ON.")]
        public Color onTintColor  = new Color(0.25f, 0.85f, 0.45f, 1f);  // green

        [Tooltip("Icon tint when setting is OFF.")]
        public Color offTintColor = new Color(0.55f, 0.55f, 0.55f, 1f);  // grey

        // ── Runtime ──────────────────────────────────────────────────
        private bool _soundOn;
        private bool _musicOn;
        private bool _vibrationOn;

        // ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;

            // If the scene started with this panel active in the editor (and Open was not called), hide it.
            if (!_isOpen)
                gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            // Sync state from save
            var d = SaveManager.Instance?.Data;
            _soundOn     = d?.soundOn     ?? true;
            _musicOn     = d?.musicOn     ?? true;
            _vibrationOn = d?.vibrationOn ?? true;

            RefreshAllToggles();

            if (soundToggleButton      != null) soundToggleButton.onClick.AddListener(OnSoundToggle);
            if (musicToggleButton      != null) musicToggleButton.onClick.AddListener(OnMusicToggle);
            if (vibrationToggleButton  != null) vibrationToggleButton.onClick.AddListener(OnVibrationToggle);
            if (privacyPolicyButton    != null) privacyPolicyButton.onClick.AddListener(OnPrivacyPolicy);
            if (restorePurchasesButton != null) restorePurchasesButton.onClick.AddListener(OnRestorePurchases);
            if (closeButton            != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            if (soundToggleButton      != null) soundToggleButton.onClick.RemoveListener(OnSoundToggle);
            if (musicToggleButton      != null) musicToggleButton.onClick.RemoveListener(OnMusicToggle);
            if (vibrationToggleButton  != null) vibrationToggleButton.onClick.RemoveListener(OnVibrationToggle);
            if (privacyPolicyButton    != null) privacyPolicyButton.onClick.RemoveListener(OnPrivacyPolicy);
            if (restorePurchasesButton != null) restorePurchasesButton.onClick.RemoveListener(OnRestorePurchases);
            if (closeButton            != null) closeButton.onClick.RemoveListener(Close);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ───────────────────────────────────────────────

        /// <summary>Shows the settings panel with scale-in animation.</summary>
        public void Open()
        {
            _isOpen = true;
            gameObject.SetActive(true);
            if (panelOvershoot != null)
                panelOvershoot.Open();
            else
                AudioManager.Instance?.PlayPopupOpen();
        }

        /// <summary>Closes the settings panel.</summary>
        public void Close()
        {
            _isOpen = false;
            AudioManager.Instance?.PlayButton();
            if (panelOvershoot != null)
                panelOvershoot.Close();
            else
                gameObject.SetActive(false);
        }

        // ── Handlers ─────────────────────────────────────────────────

        private void OnSoundToggle()
        {
            AudioManager.Instance?.PlayButton();
            _soundOn = !_soundOn;
            GameState.Instance?.SetSound(_soundOn);           // saves + mutes/unmutes AudioManager
            RefreshToggle(soundLabel, soundIcon, _soundOn, "Sound");
        }

        private void OnMusicToggle()
        {
            AudioManager.Instance?.PlayButton();
            _musicOn = !_musicOn;
            GameState.Instance?.SetMusic(_musicOn);           // saves + starts/stops music
            RefreshToggle(musicLabel, musicIcon, _musicOn, "Music");
        }

        private void OnVibrationToggle()
        {
            AudioManager.Instance?.PlayButton();
            _vibrationOn = !_vibrationOn;
            GameState.Instance?.SetVibration(_vibrationOn);   // saves for future Handheld.Vibrate() gating
            RefreshToggle(vibrationLabel, vibrationIcon, _vibrationOn, "Vibration");
        }

        private void OnPrivacyPolicy()
        {
            AudioManager.Instance?.PlayButton();
            if (!string.IsNullOrEmpty(privacyPolicyURL))
                Application.OpenURL(privacyPolicyURL);
        }

        private void OnRestorePurchases()
        {
            AudioManager.Instance?.PlayButton();
            if (restoreLabel != null) restoreLabel.text = "Restoring\u2026";

            // u2500u2500 Real restore via IAPManager u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500u2500
            IAPManager.Service.RestorePurchases((success, message) =>
            {
                Debug.Log($"[SettingsPanelUI] Restore Purchases: {success} u2014 {message}");
                if (restoreLabel != null)
                    restoreLabel.text = success ? "Restored!" : "Failed";
            });
        }

        // ── Helpers ──────────────────────────────────────────────────

        private void RefreshAllToggles()
        {
            RefreshToggle(soundLabel,     soundIcon,     _soundOn,     "Sound");
            RefreshToggle(musicLabel,     musicIcon,     _musicOn,     "Music");
            RefreshToggle(vibrationLabel, vibrationIcon, _vibrationOn, "Vibration");
            if (versionLabel != null)
                versionLabel.text = string.IsNullOrEmpty(Application.version) ? "v1.0.0" : $"v{Application.version}";
            if (restoreLabel != null) restoreLabel.text = "Restore Purchases";
        }

        private void RefreshToggle(TextMeshProUGUI label, Image icon, bool isOn, string name)
        {
            if (label != null) label.text = $"{name}: {(isOn ? "ON" : "OFF")}";
            if (icon  != null) icon.color  = isOn ? onTintColor : offTintColor;
        }

#if UNITY_EDITOR
        [ContextMenu("Test Open")]  private void TestOpen()  => Open();
        [ContextMenu("Test Close")] private void TestClose() => Close();
#endif
    }
}
