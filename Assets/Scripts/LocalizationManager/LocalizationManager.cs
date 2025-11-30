using System;
using System.Collections.Generic;
using Gameplay.Systems;
using UnityEngine;
using LocalizationManager.Structs;

namespace LocalizationManager
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private string defaultLanguage = "ru-RU";
        [SerializeField] private string saveKey = "SelectedLanguage";

        [Header("Available Languages")]
        [SerializeField] private List<Localization> availableLanguages = new List<Localization>
        {
            new Localization { localizationCode = "ru-RU", stringsFileName = "ru_RU" },
            new Localization { localizationCode = "en-US", stringsFileName = "en_US" },
            new Localization { localizationCode = "zh-CN", stringsFileName = "zh_CN" }
        };

        private Dictionary<string, Dictionary<string, string>> _localizationData = new();
        private string _currentLanguage;

        public event Action<string> OnLanguageChanged;

        // Кэш для часто используемых строк
        private Dictionary<string, string> _cachedStrings = new();
        
        // Поддержка плейсхолдеров
        private readonly char[] _placeholderChars = { '{', '}' };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // Загружаем язык из сохранений или используем системный
            if (SaveManager.Instance != null && SaveManager.Instance.GetSettingsData() != null)
            {
                var settings = SaveManager.Instance.GetSettingsData();
                _currentLanguage = settings.language;
            }
            else
            {
                _currentLanguage = GetSystemLanguage();
            }

            if (!availableLanguages.Exists(lang => lang.localizationCode == _currentLanguage))
            {
                _currentLanguage = defaultLanguage;
            }

            LoadLanguage(_currentLanguage);
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
                _ => defaultLanguage
            };
        }

        private void LoadLanguage(string languageCode)
        {
            if (!availableLanguages.Exists(lang => lang.localizationCode == languageCode))
            {
                Debug.LogError($"Language {languageCode} not found!");
                return;
            }

            // Очищаем кэш
            _cachedStrings.Clear();

            if (_localizationData.ContainsKey(languageCode))
            {
                _currentLanguage = languageCode;
                SaveLanguagePreference(languageCode);
                OnLanguageChanged?.Invoke(languageCode);
                return;
            }

            var localization = availableLanguages.Find(lang => lang.localizationCode == languageCode);
            var xml = Resources.Load<TextAsset>($"Localization/{localization.stringsFileName}");

            if (xml == null)
            {
                Debug.LogError($"Localization file for {languageCode} not found!");
                return;
            }

            try
            {
                var document = System.Xml.Linq.XDocument.Parse(xml.text);
                if (document.Root == null) return;
                var stringElements = document.Root.Elements("string");

                var languageDict = new Dictionary<string, string>();

                foreach (var element in stringElements)
                {
                    var key = element.Attribute("name")?.Value;
                    var value = element.Value;

                    if (!string.IsNullOrEmpty(key))
                    {
                        languageDict[key] = value;
                        // Кэшируем строку
                        _cachedStrings[key] = value;
                    }
                }

                _localizationData[languageCode] = languageDict;
                _currentLanguage = languageCode;
                SaveLanguagePreference(languageCode);
                OnLanguageChanged?.Invoke(languageCode);

                Debug.Log($"Language {languageCode} loaded successfully with {languageDict.Count} strings");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading language {languageCode}: {e.Message}");
            }
        }

        private void SaveLanguagePreference(string languageCode)
        {
            if (SaveManager.Instance != null)
            {
                var settings = SaveManager.Instance.GetSettingsData();
                if (settings != null)
                {
                    settings.language = languageCode;
                    SaveManager.Instance.AutoSave();
                }
            }
        }

        public string GetString(string key, params object[] args)
        {
            // Пытаемся получить из кэша
            if (_cachedStrings.TryGetValue(key, out var cachedValue))
            {
                return FormatString(cachedValue, args);
            }

            // Ищем в загруженных данных
            if (_localizationData.TryGetValue(_currentLanguage, out var languageDict))
            {
                if (languageDict.TryGetValue(key, out var value))
                {
                    // Кэшируем найденное значение
                    _cachedStrings[key] = value;
                    return FormatString(value, args);
                }
            }

            Debug.LogWarning($"Localization key not found: {key} in language {_currentLanguage}");
            return $"[{key}]";
        }

        private string FormatString(string value, object[] args)
        {
            if (args.Length == 0) return value;

            try
            {
                return string.Format(value, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning($"Format error in localized string. Value: {value}, Args: {string.Join(", ", args)}");
                return value;
            }
        }

        // Метод для получения случайного варианта из группы ключей
        public string GetRandomString(string baseKey)
        {
            if (!_localizationData.TryGetValue(_currentLanguage, out var languageDict))
                return $"[{baseKey}]";

            var variants = new List<string>();

            // Основной ключ
            if (languageDict.TryGetValue(baseKey, out var mainVariant))
            {
                variants.Add(mainVariant);
            }

            // Дополнительные варианты
            var index = 1;
            while (languageDict.TryGetValue($"{baseKey}_{index}", out var variant))
            {
                variants.Add(variant);
                index++;
            }

            return variants.Count > 0 ? variants[UnityEngine.Random.Range(0, variants.Count)] : $"[{baseKey}]";
        }

        // Метод для проверки существования ключа
        public bool HasKey(string key)
        {
            return _localizationData.TryGetValue(_currentLanguage, out var languageDict) && 
                   languageDict.ContainsKey(key);
        }

        // Метод для получения всех ключей (полезно для отладки)
        public List<string> GetAllKeys()
        {
            return _localizationData.TryGetValue(_currentLanguage, out var languageDict) 
                ? new List<string>(languageDict.Keys) 
                : new List<string>();
        }

        public List<Localization> GetAvailableLanguages()
        {
            return new List<Localization>(availableLanguages);
        }

        public string GetCurrentLanguage()
        {
            return _currentLanguage;
        }

        public void SetLanguage(string languageCode)
        {
            LoadLanguage(languageCode);
        }

        public static string GetLanguageDisplayName(string languageCode)
        {
            return languageCode switch
            {
                "ru-RU" => "Русский",
                "en-US" => "English",
                "zh-CN" => "中文",
                _ => languageCode
            };
        }

        // Метод для принудительной перезагрузки языка (например, при добавлении новых ключей)
        public void ReloadCurrentLanguage()
        {
            LoadLanguage(_currentLanguage);
        }
    }
}