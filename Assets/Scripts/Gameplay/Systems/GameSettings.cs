using System;
using Data.Game;
using UnityEngine;

namespace Gameplay.Systems
{
    public class GameSettings : MonoBehaviour
    {
        public static GameSettings Instance;

        public event Action<SettingsSaveData> OnSettingsChanged;
        public event Action OnLanguageChanged;
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSfxVolumeChanged;

        private SettingsSaveData _currentSettings;
        private AudioSource _musicAudioSource;
        private AudioSource _sfxAudioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
                LoadSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudioSources()
        {
            // Создаем AudioSource для музыки
            GameObject musicObject = new GameObject("MusicAudioSource");
            musicObject.transform.SetParent(transform);
            _musicAudioSource = musicObject.AddComponent<AudioSource>();
            _musicAudioSource.loop = true;
            _musicAudioSource.playOnAwake = false;

            // Создаем AudioSource для звуковых эффектов
            GameObject sfxObject = new GameObject("SfxAudioSource");
            sfxObject.transform.SetParent(transform);
            _sfxAudioSource = sfxObject.AddComponent<AudioSource>();
            _sfxAudioSource.playOnAwake = false;
        }

        public void LoadSettings()
        {
            if (SaveManager.Instance != null)
            {
                var loadedSettings = SaveManager.Instance.LoadSettings();
                if (loadedSettings != null)
                {
                    _currentSettings = loadedSettings;
                }
                else
                {
                    _currentSettings = GetDefaultSettings();
                }
            }
            else
            {
                _currentSettings = GetDefaultSettings();
            }

            ApplySettings();
        }

        public SettingsSaveData GetDefaultSettings()
        {
            return new SettingsSaveData
            {
                language = GetSystemLanguage(),
                masterVolume = 1.0f,
                musicVolume = 0.8f,
                sfxVolume = 1.0f,
                fullscreen = true,
                resolutionWidth = Screen.currentResolution.width,
                resolutionHeight = Screen.currentResolution.height,
                qualityLevel = QualitySettings.GetQualityLevel(),
                vsyncEnabled = QualitySettings.vSyncCount > 0,
                textureQuality = QualitySettings.globalTextureMipmapLimit,
                shadowQuality = (int)QualitySettings.shadows,
                antiAliasing = QualitySettings.antiAliasing,
                mouseSensitivity = 300f,
                invertMouseY = false,
                autoSaveInterval = 300f,
                enableScreenshots = true,
                screenshotQuality = 75,
                enableChecksum = true,
                backupSaves = true
            };
        }

        private string GetSystemLanguage()
        {
            return Application.systemLanguage switch
            {
                SystemLanguage.Russian => "ru-RU",
                SystemLanguage.English => "en-US",
                SystemLanguage.Chinese => "zh-CN",
                SystemLanguage.ChineseSimplified => "zh-CN",
                SystemLanguage.ChineseTraditional => "zh-CN",
                _ => "ru-RU"
            };
        }

        public void ApplySettings()
        {
            // Язык
            if (LocalizationManager.LocalizationManager.Instance != null)
            {
                LocalizationManager.LocalizationManager.Instance.SetLanguage(_currentSettings.language);
            }

            // Аудио
            AudioListener.volume = _currentSettings.masterVolume;
            if (_musicAudioSource != null)
                _musicAudioSource.volume = _currentSettings.musicVolume;
            if (_sfxAudioSource != null)
                _sfxAudioSource.volume = _currentSettings.sfxVolume;

            // Графика
            Screen.SetResolution(_currentSettings.resolutionWidth, _currentSettings.resolutionHeight, 
                _currentSettings.fullscreen);
            
            QualitySettings.SetQualityLevel(_currentSettings.qualityLevel);
            QualitySettings.vSyncCount = _currentSettings.vsyncEnabled ? 1 : 0;
            QualitySettings.globalTextureMipmapLimit = _currentSettings.textureQuality;
            QualitySettings.shadows = (ShadowQuality)_currentSettings.shadowQuality;
            QualitySettings.antiAliasing = _currentSettings.antiAliasing;

            // Сохранения
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.autoSaveInterval = _currentSettings.autoSaveInterval;
            }

            OnSettingsChanged?.Invoke(_currentSettings);
        }

        public void SaveSettings()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveSettings(_currentSettings);
            }
        }

        // Свойства настроек
        public string Language
        {
            get => _currentSettings.language;
            set
            {
                if (_currentSettings.language != value)
                {
                    _currentSettings.language = value;
                    SaveSettings();
                    ApplySettings();
                    OnLanguageChanged?.Invoke();
                }
            }
        }

        public float MasterVolume
        {
            get => _currentSettings.masterVolume;
            set
            {
                if (Math.Abs(_currentSettings.masterVolume - value) > 0.01f)
                {
                    _currentSettings.masterVolume = Mathf.Clamp01(value);
                    AudioListener.volume = _currentSettings.masterVolume;
                    SaveSettings();
                    OnMasterVolumeChanged?.Invoke(value);
                }
            }
        }

        public float MusicVolume
        {
            get => _currentSettings.musicVolume;
            set
            {
                if (Math.Abs(_currentSettings.musicVolume - value) > 0.01f)
                {
                    _currentSettings.musicVolume = Mathf.Clamp01(value);
                    if (_musicAudioSource != null)
                        _musicAudioSource.volume = _currentSettings.musicVolume;
                    SaveSettings();
                    OnMusicVolumeChanged?.Invoke(value);
                }
            }
        }

        public float SfxVolume
        {
            get => _currentSettings.sfxVolume;
            set
            {
                if (Math.Abs(_currentSettings.sfxVolume - value) > 0.01f)
                {
                    _currentSettings.sfxVolume = Mathf.Clamp01(value);
                    if (_sfxAudioSource != null)
                        _sfxAudioSource.volume = _currentSettings.sfxVolume;
                    SaveSettings();
                    OnSfxVolumeChanged?.Invoke(value);
                }
            }
        }

        public bool Fullscreen
        {
            get => _currentSettings.fullscreen;
            set
            {
                if (_currentSettings.fullscreen != value)
                {
                    _currentSettings.fullscreen = value;
                    Screen.fullScreen = value;
                    SaveSettings();
                }
            }
        }

        public int ResolutionWidth => _currentSettings.resolutionWidth;
        public int ResolutionHeight => _currentSettings.resolutionHeight;

        public void SetResolution(int width, int height)
        {
            if (_currentSettings.resolutionWidth != width || _currentSettings.resolutionHeight != height)
            {
                _currentSettings.resolutionWidth = width;
                _currentSettings.resolutionHeight = height;
                Screen.SetResolution(width, height, _currentSettings.fullscreen);
                SaveSettings();
            }
        }

        public int QualityLevel
        {
            get => _currentSettings.qualityLevel;
            set
            {
                if (_currentSettings.qualityLevel != value)
                {
                    _currentSettings.qualityLevel = value;
                    QualitySettings.SetQualityLevel(value);
                    SaveSettings();
                }
            }
        }

        public bool VSyncEnabled
        {
            get => _currentSettings.vsyncEnabled;
            set
            {
                if (_currentSettings.vsyncEnabled != value)
                {
                    _currentSettings.vsyncEnabled = value;
                    QualitySettings.vSyncCount = value ? 1 : 0;
                    SaveSettings();
                }
            }
        }

        public float MouseSensitivity
        {
            get => _currentSettings.mouseSensitivity;
            set
            {
                if (Math.Abs(_currentSettings.mouseSensitivity - value) > 0.01f)
                {
                    _currentSettings.mouseSensitivity = Mathf.Clamp(value, 50f, 1000f);
                    SaveSettings();
                }
            }
        }

        public bool InvertMouseY
        {
            get => _currentSettings.invertMouseY;
            set
            {
                if (_currentSettings.invertMouseY != value)
                {
                    _currentSettings.invertMouseY = value;
                    SaveSettings();
                }
            }
        }

        // Методы для работы с аудио
        public void PlayMusic(AudioClip musicClip)
        {
            if (_musicAudioSource != null)
            {
                _musicAudioSource.clip = musicClip;
                _musicAudioSource.volume = _currentSettings.musicVolume;
                _musicAudioSource.Play();
            }
        }

        public void PlaySfx(AudioClip sfxClip, float volumeScale = 1.0f)
        {
            if (_sfxAudioSource != null)
            {
                _sfxAudioSource.PlayOneShot(sfxClip, volumeScale * _currentSettings.sfxVolume);
            }
        }

        public SettingsSaveData GetCurrentSettings() => _currentSettings;
        public void SetCurrentSettings(SettingsSaveData settings) => _currentSettings = settings;
    }
}