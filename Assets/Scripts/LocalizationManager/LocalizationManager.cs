using System;
using System.Collections.Generic;
using UnityEngine;
using LocalizationManager.Structs;

namespace LocalizationManager
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        [Header("Settings")] [SerializeField] private string defaultLanguage = "ru-RU";
        [SerializeField] private string saveKey = "SelectedLanguage";

        [Header("Available Languages")] [SerializeField]
        private List<Localization> availableLanguages = new List<Localization>
        {
            new Localization { localizationCode = "ru-RU", stringsFileName = "ru_RU" },
            new Localization { localizationCode = "en-US", stringsFileName = "en_US" }
        };

        private Dictionary<string, Dictionary<string, string>> _localizationData = new();

        private string _currentLanguage;

        // Событие при смене языка
        public event Action<string> OnLanguageChanged;

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
            // Загрузка сохраненного языка или использование языка системы
            _currentLanguage = PlayerPrefs.GetString(saveKey, GetSystemLanguage());

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

            // Если язык уже загружен, просто переключаемся
            if (_localizationData.ContainsKey(languageCode))
            {
                _currentLanguage = languageCode;
                PlayerPrefs.SetString(saveKey, languageCode);
                OnLanguageChanged?.Invoke(languageCode);
                return;
            }

            // Загрузка нового языка
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
                    }
                }

                _localizationData[languageCode] = languageDict;
                _currentLanguage = languageCode;
                PlayerPrefs.SetString(saveKey, languageCode);
                OnLanguageChanged?.Invoke(languageCode);

                Debug.Log($"Language {languageCode} loaded successfully with {languageDict.Count} strings");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading language {languageCode}: {e.Message}");
            }
        }

        public string GetString(string key, params object[] args)
        {
            if (_localizationData.TryGetValue(_currentLanguage, out var languageDict))
            {
                if (languageDict.TryGetValue(key, out var value))
                {
                    // Поддержка плейсхолдеров {0}, {1}, etc.
                    if (args.Length <= 0) return value;
                    try
                    {
                        return string.Format(value, args);
                    }
                    catch (FormatException)
                    {
                        Debug.LogWarning($"Format error in localized string: {key}");
                        return value;
                    }
                }
            }

            Debug.LogWarning($"Localization key not found: {key} in language {_currentLanguage}");
            return $"[{key}]";
        }

        // Метод для вариативности фраз (случайный выбор из доступных вариантов)
        public string GetRandomString(string baseKey)
        {
            if (!_localizationData.TryGetValue(_currentLanguage, out var languageDict)) return $"[{baseKey}]";
            // Ищем варианты: baseKey, baseKey_1, baseKey_2, etc.
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

        // Получение списка доступных языков
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

        // Получение отображаемого имени языка
        public static string GetLanguageDisplayName(string languageCode)
        {
            switch (languageCode)
            {
                case "ru-RU": return "Русский";
                case "en-US": return "English";
                default: return languageCode;
            }
        }
    }
}