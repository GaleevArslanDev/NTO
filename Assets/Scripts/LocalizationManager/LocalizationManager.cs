using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Gameplay.Systems;
using LocalizationManager.Structs;
using UnityEngine;

namespace LocalizationManager
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        [SerializeField] private string defaultLanguage = "ru-RU";
        [SerializeField] private List<Localization> availableLanguages = new()
        {
            new() { localizationCode = "ru-RU", stringsFileName = "ru_RU" },
            new() { localizationCode = "en-US", stringsFileName = "en_US" }
        };

        private Dictionary<string, Dictionary<string, string>> _localizationData = new();
        private string _currentLanguage;
        private Dictionary<string, object> _cachedFormattedStrings = new();
        private bool _isInitialized = false;
        private CultureInfo _currentCulture;

        public event Action<string> OnLanguageChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => Initialize();

        private void Initialize()
        {
            if (_isInitialized) return;

            string targetLanguage = GetTargetLanguage();
            LoadLanguage(targetLanguage);
            _isInitialized = true;
        }
        
        public string GetCurrentLanguage() => _currentLanguage;
        
        public List<Localization> GetAvailableLanguages() => availableLanguages;

        private string GetTargetLanguage()
        {
            // Приоритеты загрузки
            if (!string.IsNullOrEmpty(Core.StaticSaveData.LanguageOverride))
                return Core.StaticSaveData.LanguageOverride;

            if (SaveManager.Instance?.LoadSettings() is {} settings && 
                !string.IsNullOrEmpty(settings.language))
                return settings.language;

            return GetSystemLanguage();
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
                Debug.LogWarning($"Language {languageCode} not available, falling back to {defaultLanguage}");
                languageCode = defaultLanguage;
            }

            // Очищаем кэш
            _cachedFormattedStrings.Clear();

            // Если язык уже загружен, просто переключаемся
            if (_localizationData.ContainsKey(languageCode))
            {
                SwitchToLanguage(languageCode);
                return;
            }

            LoadLanguageFromResources(languageCode);
        }

        private void LoadLanguageFromResources(string languageCode)
        {
            var localization = availableLanguages.Find(lang => lang.localizationCode == languageCode);
            if (localization == null)
            {
                Debug.LogError($"Localization for {languageCode} not found!");
                return;
            }

            var xml = Resources.Load<TextAsset>($"Localization/{localization.stringsFileName}");
            if (xml == null)
            {
                Debug.LogError($"Localization file not found: Localization/{localization.stringsFileName}");
                return;
            }

            try
            {
                var document = XDocument.Parse(xml.text);
                var languageDict = new Dictionary<string, string>();

                foreach (var element in document.Root.Elements("string"))
                {
                    var key = element.Attribute("name")?.Value;
                    if (string.IsNullOrEmpty(key)) continue;

                    // Правильно обрабатываем содержимое элемента
                    var value = DecodeXmlString(element);
                    languageDict[key] = value;
                }

                _localizationData[languageCode] = languageDict;
                SwitchToLanguage(languageCode);

                Debug.Log($"Language {languageCode} loaded: {languageDict.Count} strings");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading language {languageCode}: {e.Message}");
            }
        }

        private string DecodeXmlString(XElement element)
        {
            // Обрабатываем CDATA и экранированные символы
            var content = element.Nodes()
                .Select(node => node is XCData ? (node as XCData).Value : node.ToString())
                .Aggregate((current, next) => current + next);

            // Декодируем XML-сущности
            return System.Net.WebUtility.HtmlDecode(content);
        }

        private void SwitchToLanguage(string languageCode)
        {
            _currentLanguage = languageCode;
            _currentCulture = CultureInfo.GetCultureInfo(languageCode);
            
            // Устанавливаем культуру для текущего потока
            CultureInfo.CurrentCulture = _currentCulture;
            CultureInfo.CurrentUICulture = _currentCulture;
            
            OnLanguageChanged?.Invoke(languageCode);
        }

        public string GetString(string key, params object[] args)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("LocalizationManager not initialized!");
                return $"[{key}]";
            }

            // Создаем ключ кэша с учетом аргументов
            string cacheKey = CreateCacheKey(key, args);
            
            if (_cachedFormattedStrings.TryGetValue(cacheKey, out var cachedValue))
            {
                return cachedValue as string;
            }

            if (!_localizationData.TryGetValue(_currentLanguage, out var languageDict))
            {
                Debug.LogWarning($"Language {_currentLanguage} not loaded!");
                return $"[{key}]";
            }

            if (!languageDict.TryGetValue(key, out var rawString))
            {
                Debug.LogWarning($"Key '{key}' not found in {_currentLanguage}");
                return $"[{key}]";
            }

            string result = FormatString(rawString, args);
            
            // Кэшируем результат форматирования
            if (args.Length == 0) // Кэшируем только строки без аргументов
            {
                _cachedFormattedStrings[cacheKey] = result;
            }
            
            return result;
        }

        private string CreateCacheKey(string key, object[] args)
        {
            if (args.Length == 0) return key;
            
            var sb = new StringBuilder(key);
            foreach (var arg in args)
            {
                sb.Append($"_{arg?.GetHashCode() ?? 0}");
            }
            return sb.ToString();
        }

        private string FormatString(string format, object[] args)
        {
            if (args.Length == 0) return format;

            try
            {
                // Используем CultureInfo для правильного форматирования чисел, дат и т.д.
                return string.Format(_currentCulture, format, args);
            }
            catch (FormatException e)
            {
                Debug.LogError($"Format error for '{format}' with args: {string.Join(", ", args)}. Error: {e.Message}");
                return format;
            }
        }

        // Метод для работы с плюрализацией (множественное число)
        public string GetPluralString(string key, int count, params object[] args)
        {
            // Определяем форму множественного числа на основе культуры
            string pluralKey = GetPluralKey(key, count);
            var allArgs = new object[] { count }.Concat(args).ToArray();
            
            return GetString(pluralKey, allArgs);
        }

        private string GetPluralKey(string baseKey, int count)
        {
            // Простая реализация для русской локализации
            if (_currentLanguage.StartsWith("ru"))
            {
                int mod10 = count % 10;
                int mod100 = count % 100;

                if (mod10 == 1 && mod100 != 11) return $"{baseKey}_one";
                if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) return $"{baseKey}_few";
                return $"{baseKey}_many";
            }

            // Для английской локализации
            if (_currentLanguage.StartsWith("en"))
            {
                return count == 1 ? $"{baseKey}_one" : $"{baseKey}_other";
            }

            // По умолчанию
            return baseKey;
        }

        // Утилита для безопасного получения строки с проверкой
        public bool TryGetString(string key, out string result, params object[] args)
        {
            result = GetString(key, args);
            return !result.StartsWith("[") || !result.EndsWith("]");
        }

        // Метод для получения форматированной строки с поддержкой Rich Text
        public string GetRichString(string key, Color color, params object[] args)
        {
            string text = GetString(key, args);
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode) || _currentLanguage == languageCode)
                return;

            if (!availableLanguages.Exists(lang => lang.localizationCode == languageCode))
            {
                Debug.LogWarning($"Language {languageCode} not available!");
                return;
            }

            LoadLanguage(languageCode);
            
            // Сохраняем в настройках
            GameSettings.Instance.SetLanguage(languageCode);
        }

        // Метод для перезагрузки текущего языка
        public void ReloadCurrentLanguage()
        {
            if (!string.IsNullOrEmpty(_currentLanguage))
            {
                _localizationData.Remove(_currentLanguage);
                LoadLanguage(_currentLanguage);
            }
        }
        
        public static string GetLanguageDisplayName(string languageCode)
        {
            return languageCode switch
            {
                "ru-RU" => "Русский",
                "en-US" => "English",
                _ => languageCode
            };
        }

        // Очистка памяти
        private void OnDestroy()
        {
            _localizationData.Clear();
            _cachedFormattedStrings.Clear();
        }
    }
}