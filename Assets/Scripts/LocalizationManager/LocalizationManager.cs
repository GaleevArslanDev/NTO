using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Structs;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private string defaultLanguage = "ru-RU";
    [SerializeField] private string saveKey = "SelectedLanguage";
    
    [Header("Available Languages")]
    [SerializeField] private List<Localization> availableLanguages = new List<Localization>
    {
        new Localization { LocalizationCode = "ru-RU", StringsFileName = "ru_RU" },
        new Localization { LocalizationCode = "en-US", StringsFileName = "en_US" }
    };
    
    private Dictionary<string, Dictionary<string, string>> localizationData = new Dictionary<string, Dictionary<string, string>>();
    private string currentLanguage;
    
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
        currentLanguage = PlayerPrefs.GetString(saveKey, GetSystemLanguage());
        
        if (!availableLanguages.Exists(lang => lang.LocalizationCode == currentLanguage))
        {
            currentLanguage = defaultLanguage;
        }
        
        LoadLanguage(currentLanguage);
    }
    
    private string GetSystemLanguage()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Russian: return "ru-RU";
            case SystemLanguage.English: return "en-US";
            default: return defaultLanguage;
        }
    }
    
    public void LoadLanguage(string languageCode)
    {
        if (!availableLanguages.Exists(lang => lang.LocalizationCode == languageCode))
        {
            Debug.LogError($"Language {languageCode} not found!");
            return;
        }
        
        // Если язык уже загружен, просто переключаемся
        if (localizationData.ContainsKey(languageCode))
        {
            currentLanguage = languageCode;
            PlayerPrefs.SetString(saveKey, languageCode);
            OnLanguageChanged?.Invoke(languageCode);
            return;
        }
        
        // Загрузка нового языка
        var localization = availableLanguages.Find(lang => lang.LocalizationCode == languageCode);
        var xml = Resources.Load<TextAsset>($"Localization/{localization.StringsFileName}");
        
        if (xml == null)
        {
            Debug.LogError($"Localization file for {languageCode} not found!");
            return;
        }
        
        try
        {
            var document = System.Xml.Linq.XDocument.Parse(xml.text);
            var stringElements = document.Root.Elements("string");
            
            var languageDict = new Dictionary<string, string>();
            
            foreach (var element in stringElements)
            {
                string name = element.Attribute("name")?.Value;
                string value = element.Value;
                
                if (!string.IsNullOrEmpty(name))
                {
                    languageDict[name] = value;
                }
            }
            
            localizationData[languageCode] = languageDict;
            currentLanguage = languageCode;
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
        if (localizationData.TryGetValue(currentLanguage, out var languageDict))
        {
            if (languageDict.TryGetValue(key, out var value))
            {
                // Поддержка плейсхолдеров {0}, {1}, etc.
                if (args.Length > 0)
                {
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
                return value;
            }
        }
        
        Debug.LogWarning($"Localization key not found: {key} in language {currentLanguage}");
        return $"[{key}]";
    }
    
    // Метод для вариативности фраз (случайный выбор из доступных вариантов)
    public string GetRandomString(string baseKey)
    {
        if (localizationData.TryGetValue(currentLanguage, out var languageDict))
        {
            // Ищем варианты: baseKey, baseKey_1, baseKey_2, etc.
            var variants = new List<string>();
            
            // Основной ключ
            if (languageDict.TryGetValue(baseKey, out var mainVariant))
            {
                variants.Add(mainVariant);
            }
            
            // Дополнительные варианты
            int index = 1;
            while (languageDict.TryGetValue($"{baseKey}_{index}", out var variant))
            {
                variants.Add(variant);
                index++;
            }
            
            if (variants.Count > 0)
            {
                return variants[UnityEngine.Random.Range(0, variants.Count)];
            }
        }
        
        return $"[{baseKey}]";
    }
    
    // Получение списка доступных языков
    public List<Localization> GetAvailableLanguages()
    {
        return new List<Localization>(availableLanguages);
    }
    
    public string GetCurrentLanguage()
    {
        return currentLanguage;
    }
    
    public void SetLanguage(string languageCode)
    {
        LoadLanguage(languageCode);
    }
    
    // Получение отображаемого имени языка
    public string GetLanguageDisplayName(string languageCode)
    {
        switch (languageCode)
        {
            case "ru-RU": return "Русский";
            case "en-US": return "English";
            default: return languageCode;
        }
    }
} 