using System.Collections.Generic;
using Data.Game;
using Gameplay.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SettingsUI : MonoBehaviour
    {
        public static SettingsUI Instance;

        [Header("Main References")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text titleText;

        [Header("General Settings")]
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private TMP_Text musicVolumeText;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text sfxVolumeText;

        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private TMP_Dropdown textureQualityDropdown;
        [SerializeField] private TMP_Dropdown shadowQualityDropdown;
        [SerializeField] private TMP_Dropdown antiAliasingDropdown;

        [Header("Controls Settings")]
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private TMP_Text mouseSensitivityText;
        [SerializeField] private Toggle invertMouseYToggle;

        [Header("Saves Settings")]
        [SerializeField] private Slider autoSaveSlider;
        [SerializeField] private TMP_Text autoSaveText;
        [SerializeField] private Toggle screenshotsToggle;
        [SerializeField] private Toggle checksumToggle;
        [SerializeField] private Toggle backupsToggle;

        private Resolution[] _availableResolutions;
        private SettingsSaveData _originalSettings;
        private SettingsSaveData _currentSettings;
        private bool _isInitialized;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeUI();
            LoadCurrentSettings();
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RegisterUIOpen();
            }
        }

        private void InitializeUI()
        {
            if (_isInitialized) return;

            // Кнопки
            closeButton.onClick.AddListener(CloseSettings);
            applyButton.onClick.AddListener(ApplySettings);
            resetButton.onClick.AddListener(ResetToDefaults);

            // Общие настройки
            InitializeLanguageDropdown();
            InitializeResolutionDropdown();
            InitializeQualityDropdowns();

            // Слайдеры
            masterVolumeSlider.onValueChanged.AddListener(value => UpdateVolumeText(masterVolumeText, value));
            musicVolumeSlider.onValueChanged.AddListener(value => UpdateVolumeText(musicVolumeText, value));
            sfxVolumeSlider.onValueChanged.AddListener(value => UpdateVolumeText(sfxVolumeText, value));
            mouseSensitivitySlider.onValueChanged.AddListener(value => 
                mouseSensitivityText.text = $"{value:F0}");
            autoSaveSlider.onValueChanged.AddListener(value => 
                autoSaveText.text = $"{value:F0}s");

            _isInitialized = true;
        }

        private void InitializeLanguageDropdown()
        {
            languageDropdown.ClearOptions();
            var languages = LocalizationManager.LocalizationManager.Instance.GetAvailableLanguages();
            var options = new List<TMP_Dropdown.OptionData>();

            foreach (var lang in languages)
            {
                options.Add(new TMP_Dropdown.OptionData(
                    LocalizationManager.LocalizationManager.GetLanguageDisplayName(lang.localizationCode)
                ));
            }

            languageDropdown.options = options;
        }

        private void InitializeResolutionDropdown()
        {
            resolutionDropdown.ClearOptions();
            _availableResolutions = Screen.resolutions;
            var options = new List<TMP_Dropdown.OptionData>();
            var currentResolutionIndex = 0;

            for (int i = 0; i < _availableResolutions.Length; i++)
            {
                var resolution = _availableResolutions[i];
                var option = $"{resolution.width} x {resolution.height} @ {resolution.refreshRate}Hz";
                options.Add(new TMP_Dropdown.OptionData(option));

                if (resolution.width == Screen.currentResolution.width && 
                    resolution.height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.options = options;
            resolutionDropdown.value = currentResolutionIndex;
        }

        private void InitializeQualityDropdowns()
        {
            // Качество графики
            qualityDropdown.ClearOptions();
            var qualityNames = QualitySettings.names;
            foreach (var name in qualityNames)
            {
                qualityDropdown.options.Add(new TMP_Dropdown.OptionData(name));
            }
            qualityDropdown.value = QualitySettings.GetQualityLevel();

            // Качество текстур
            textureQualityDropdown.ClearOptions();
            textureQualityDropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.LocalizationManager.Instance.GetString("settings_texture-quality_full")));
            textureQualityDropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.LocalizationManager.Instance.GetString("settings_texture-quality_half")));
            textureQualityDropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.LocalizationManager.Instance.GetString("settings_texture-quality_quarter")));
            textureQualityDropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.LocalizationManager.Instance.GetString("settings_texture-quality_eighth")));

            // Качество теней
            shadowQualityDropdown.ClearOptions();
            shadowQualityDropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.LocalizationManager.Instance.GetString("settings_shadow-quality_off")));
            shadowQualityDropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.LocalizationManager.Instance.GetString("settings_shadow-quality_hard")));
            shadowQualityDropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.LocalizationManager.Instance.GetString("settings_shadow-quality_all")));

            // Сглаживание
            antiAliasingDropdown.ClearOptions();
            antiAliasingDropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.LocalizationManager.Instance.GetString("settings_anti-aliasing_off")));
            antiAliasingDropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.LocalizationManager.Instance.GetString("settings_anti-aliasing_2x")));
            antiAliasingDropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.LocalizationManager.Instance.GetString("settings_anti-aliasing_4x")));
            antiAliasingDropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.LocalizationManager.Instance.GetString("settings_anti-aliasing_8x")));
        }

        private void UpdateVolumeText(TMP_Text text, float value)
        {
            text.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        public void ShowSettings()
        {
            settingsPanel.SetActive(true);
            LoadCurrentSettings();
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RegisterUIOpen();
            }
        }

        public void CloseSettings()
        {
            settingsPanel.SetActive(false);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RegisterUIClose();
            }
        }

        private void LoadCurrentSettings()
        {
            if (GameSettings.Instance != null)
            {
                _currentSettings = GameSettings.Instance.GetCurrentSettings();
                _originalSettings = JsonUtility.FromJson<SettingsSaveData>(
                    JsonUtility.ToJson(_currentSettings)
                );

                UpdateUIFromSettings();
            }
        }

        private void UpdateUIFromSettings()
        {
            // Общие
            var languages = LocalizationManager.LocalizationManager.Instance.GetAvailableLanguages();
            var languageIndex = languages.FindIndex(l => l.localizationCode == _currentSettings.language);
            if (languageIndex >= 0) languageDropdown.value = languageIndex;
            fullscreenToggle.isOn = _currentSettings.fullscreen;

            // Аудио
            masterVolumeSlider.value = _currentSettings.masterVolume;
            musicVolumeSlider.value = _currentSettings.musicVolume;
            sfxVolumeSlider.value = _currentSettings.sfxVolume;
            UpdateVolumeText(masterVolumeText, _currentSettings.masterVolume);
            UpdateVolumeText(musicVolumeText, _currentSettings.musicVolume);
            UpdateVolumeText(sfxVolumeText, _currentSettings.sfxVolume);

            // Графика
            var resIndex = System.Array.FindIndex(_availableResolutions, r => 
                r.width == _currentSettings.resolutionWidth && 
                r.height == _currentSettings.resolutionHeight);
            if (resIndex >= 0) resolutionDropdown.value = resIndex;
            
            qualityDropdown.value = _currentSettings.qualityLevel;
            vsyncToggle.isOn = _currentSettings.vsyncEnabled;
            textureQualityDropdown.value = _currentSettings.textureQuality;
            shadowQualityDropdown.value = _currentSettings.shadowQuality;
            antiAliasingDropdown.value = _currentSettings.antiAliasing / 2;

            // Управление
            mouseSensitivitySlider.value = _currentSettings.mouseSensitivity;
            mouseSensitivityText.text = $"{_currentSettings.mouseSensitivity:F0}";
            invertMouseYToggle.isOn = _currentSettings.invertMouseY;

            // Сохранения
            autoSaveSlider.value = _currentSettings.autoSaveInterval;
            autoSaveText.text = $"{_currentSettings.autoSaveInterval:F0}s";
            screenshotsToggle.isOn = _currentSettings.enableScreenshots;
            checksumToggle.isOn = _currentSettings.enableChecksum;
            backupsToggle.isOn = _currentSettings.backupSaves;
        }

        private void ApplySettings()
        {
            if (GameSettings.Instance == null) return;

            // Собираем настройки из UI
            var newSettings = new SettingsSaveData
            {
                // Общие
                language = GetSelectedLanguageCode(),
                fullscreen = fullscreenToggle.isOn,

                // Аудио
                masterVolume = masterVolumeSlider.value,
                musicVolume = musicVolumeSlider.value,
                sfxVolume = sfxVolumeSlider.value,

                // Графика
                resolutionWidth = _availableResolutions[resolutionDropdown.value].width,
                resolutionHeight = _availableResolutions[resolutionDropdown.value].height,
                qualityLevel = qualityDropdown.value,
                vsyncEnabled = vsyncToggle.isOn,
                textureQuality = textureQualityDropdown.value,
                shadowQuality = shadowQualityDropdown.value,
                antiAliasing = antiAliasingDropdown.value * 2,

                // Управление
                mouseSensitivity = mouseSensitivitySlider.value,
                invertMouseY = invertMouseYToggle.isOn,

                // Сохранения
                autoSaveInterval = autoSaveSlider.value,
                enableScreenshots = screenshotsToggle.isOn,
                enableChecksum = checksumToggle.isOn,
                backupSaves = backupsToggle.isOn
            };

            // Применяем настройки
            GameSettings.Instance.SetCurrentSettings(newSettings);
            GameSettings.Instance.SaveSettings();
            GameSettings.Instance.ApplySettings();

            Debug.Log("Settings applied and saved");
        }

        private void ResetToDefaults()
        {
            if (GameSettings.Instance != null)
            {
                var defaultSettings = GameSettings.Instance.GetDefaultSettings();
                GameSettings.Instance.SetCurrentSettings(defaultSettings);
                UpdateUIFromSettings();
                ApplySettings();
            }
        }

        private string GetSelectedLanguageCode()
        {
            var languages = LocalizationManager.LocalizationManager.Instance.GetAvailableLanguages();
            if (languageDropdown.value < languages.Count)
            {
                return languages[languageDropdown.value].localizationCode;
            }
            return "ru-RU";
        }

        private void Update()
        {
            if (settingsPanel.activeInHierarchy && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseSettings();
            }
        }

        private void OnDestroy()
        {
            if (UIManager.Instance != null && settingsPanel.activeSelf)
            {
                UIManager.Instance.RegisterUIClose();
            }
        }
    }
}