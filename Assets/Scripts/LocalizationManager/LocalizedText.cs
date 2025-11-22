using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Header("Localization")]
    [SerializeField] private string localizationKey;
    [SerializeField] private bool useRandomVariants = false;
    [SerializeField] private bool updateOnLanguageChange = true;
    
    [Header("Dynamic Values")]
    [SerializeField] private bool hasDynamicValues = false;
    
    private TextMeshProUGUI textComponent;
    private object[] dynamicValues;
    
    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        
        if (updateOnLanguageChange && LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }
    
    private void Start()
    {
        UpdateText();
    }
    
    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }
    
    public void SetDynamicValues(params object[] values)
    {
        dynamicValues = values;
        UpdateText();
    }
    
    public void UpdateDynamicValue(int index, object value)
    {
        if (dynamicValues != null && index < dynamicValues.Length)
        {
            dynamicValues[index] = value;
            UpdateText();
        }
    }
    
    private void OnLanguageChanged(string languageCode)
    {
        UpdateText();
    }
    
    private void UpdateText()
    {
        if (LocalizationManager.Instance == null || string.IsNullOrEmpty(localizationKey))
            return;
        
        string localizedText;
        
        if (useRandomVariants)
        {
            localizedText = LocalizationManager.Instance.GetRandomString(localizationKey);
        }
        else
        {
            localizedText = LocalizationManager.Instance.GetString(localizationKey);
        }
        
        // Применяем динамические значения если есть
        if (hasDynamicValues && dynamicValues != null && dynamicValues.Length > 0)
        {
            try
            {
                localizedText = string.Format(localizedText, dynamicValues);
            }
            catch (System.FormatException)
            {
                Debug.LogWarning($"Format error in localized text: {localizationKey}");
            }
        }
        
        textComponent.text = localizedText;
        
        // Автоматическая подстройка шрифта под язык
        AdjustFontForLanguage();
    }
    
    private void AdjustFontForLanguage()
    {
        var currentLang = LocalizationManager.Instance.GetCurrentLanguage();
        
        // Пример: для русского языка используем другой шрифт если нужно
        if (currentLang == "ru-RU")
        {
            // Можно загрузить кириллический шрифт
            // textComponent.font = Resources.Load<TMP_FontAsset>("Fonts/RussianFont");
        }
    }
    
    // Метод для смены ключа во время выполнения
    public void SetKey(string newKey, bool updateImmediately = true)
    {
        localizationKey = newKey;
        if (updateImmediately)
            UpdateText();
    }
}