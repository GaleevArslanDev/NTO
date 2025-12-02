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

        [Header("Available Languages")]
        [SerializeField] private List<Localization> availableLanguages = new List<Localization>
        {
            new Localization { localizationCode = "ru-RU", stringsFileName = "ru_RU" },
            new Localization { localizationCode = "en-US", stringsFileName = "en_US" },
            new Localization { localizationCode = "zh-CN", stringsFileName = "zh_CN" }
        };

        private Dictionary<string, Dictionary<string, string>> _localizationData = new();
        private string _currentLanguage;
        private Dictionary<string, string> _cachedStrings = new();
        private bool _isInitialized = false;

        public event Action<string> OnLanguageChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            // Определяем язык в порядке приоритета:
            // 1. Из статических данных (если загружаем сейв)
            // 2. Из сохраненных настроек
            // 3. Системный язык
            // 4. Язык по умолчанию
            
            string targetLanguage = defaultLanguage;
            
            // Проверяем статические данные для загрузки игры
            if (!string.IsNullOrEmpty(Core.StaticSaveData.LanguageOverride))
            {
                targetLanguage = Core.StaticSaveData.LanguageOverride;
            }
            // Пытаемся загрузить из сохранений
            else if (SaveManager.Instance != null && SaveManager.Instance.LoadSettings() != null)
            {
                var settings = SaveManager.Instance.LoadSettings();
                if (!string.IsNullOrEmpty(settings.language))
                {
                    targetLanguage = settings.language;
                }
            }
            // Используем системный язык
            else
            {
                targetLanguage = GetSystemLanguage();
            }

            // Проверяем, что язык доступен
            if (!availableLanguages.Exists(lang => lang.localizationCode == targetLanguage))
            {
                Debug.LogWarning($"Language {targetLanguage} not available, falling back to {defaultLanguage}");
                targetLanguage = defaultLanguage;
            }

            LoadLanguage(targetLanguage);
            _isInitialized = true;
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
                Debug.LogError($"Language {languageCode} not found in available languages!");
                languageCode = defaultLanguage;
            }

            // Очищаем кэш
            _cachedStrings.Clear();

            // Если язык уже загружен, просто переключаемся
            if (_localizationData.ContainsKey(languageCode))
            {
                _currentLanguage = languageCode;
                OnLanguageChanged?.Invoke(languageCode);
                Debug.Log($"Switched to language: {languageCode}");
                return;
            }

            var localization = availableLanguages.Find(lang => lang.localizationCode == languageCode);
            if (localization == null)
            {
                Debug.LogError($"Localization for {languageCode} not found!");
                return;
            }

            var xml = Resources.Load<TextAsset>($"Localization/{localization.stringsFileName}");
            if (xml == null)
            {
                Debug.LogError($"Localization file for {languageCode} not found at: Localization/{localization.stringsFileName}");
                return;
            }

            try
            {
                var document = System.Xml.Linq.XDocument.Parse(xml.text);
                if (document.Root == null)
                {
                    Debug.LogError($"XML root is null for {languageCode}");
                    return;
                }

                var languageDict = new Dictionary<string, string>();
                foreach (var element in document.Root.Elements("string"))
                {
                    var key = element.Attribute("name")?.Value;
                    var value = element.Value;

                    if (!string.IsNullOrEmpty(key))
                    {
                        languageDict[key] = value;
                    }
                }

                _localizationData[languageCode] = languageDict;
                _currentLanguage = languageCode;
                
                Debug.Log($"Language {languageCode} loaded successfully with {languageDict.Count} strings");
                OnLanguageChanged?.Invoke(languageCode);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading language {languageCode}: {e.Message}\n{e.StackTrace}");
            }
        }

        public string GetString(string key, params object[] args)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("LocalizationManager not initialized!");
                return $"[{key}]";
            }

            // Пытаемся получить из кэша текущего языка
            string cacheKey = $"{_currentLanguage}_{key}";
            if (_cachedStrings.TryGetValue(cacheKey, out var cachedValue))
            {
                return FormatString(cachedValue, args);
            }

            // Ищем в загруженных данных
            if (_localizationData.TryGetValue(_currentLanguage, out var languageDict))
            {
                if (languageDict.TryGetValue(key, out var value))
                {
                    // Кэшируем
                    _cachedStrings[cacheKey] = value;
                    return FormatString(value, args);
                }
            }

            Debug.LogWarning($"Localization key not found: '{key}' in language {_currentLanguage}");
            return $"[{key}]";
        }

        private string FormatString(string value, object[] args)
        {
            if (args.Length == 0) return value;

            try
            {
                return string.Format(value, args);
            }
            catch (FormatException e)
            {
                Debug.LogWarning($"Format error in localized string. Value: {value}, Args: {string.Join(", ", args)}\nError: {e.Message}");
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
            if (string.IsNullOrEmpty(languageCode) || _currentLanguage == languageCode)
                return;

            LoadLanguage(languageCode);
            
            // Сохраняем в настройках
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.Language = languageCode;
            }
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

        public void ReloadCurrentLanguage()
        {
            if (!string.IsNullOrEmpty(_currentLanguage))
            {
                LoadLanguage(_currentLanguage);
            }
        }
    }
}