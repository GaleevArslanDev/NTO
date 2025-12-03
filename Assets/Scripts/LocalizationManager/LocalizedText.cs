using TMPro;
using UnityEngine;

namespace LocalizationManager
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [Header("Localization")]
        [SerializeField] private string localizationKey;
        [SerializeField] private bool useRandomVariants;
        [SerializeField] private bool updateOnLanguageChange = true;
    
        [Header("Dynamic Values")]
        [SerializeField] private bool hasDynamicValues;
    
        private TextMeshProUGUI _textComponent;
        private object[] _dynamicValues;
    
        private void Awake()
        {
            _textComponent = GetComponent<TextMeshProUGUI>();
        
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
            _dynamicValues = values;
            UpdateText();
        }
    
        public void UpdateDynamicValue(int index, object value)
        {
            if (_dynamicValues == null || index >= _dynamicValues.Length) return;
            _dynamicValues[index] = value;
            UpdateText();
        }
    
        private void OnLanguageChanged(string languageCode)
        {
            UpdateText();
        }
    
        private void UpdateText()
        {
            if (LocalizationManager.Instance == null || string.IsNullOrEmpty(localizationKey))
                return;

            var localizedText = LocalizationManager.Instance.GetString(localizationKey);

            // Применяем динамические значения если есть
            if (hasDynamicValues && _dynamicValues is { Length: > 0 })
            {
                try
                {
                    localizedText = string.Format(localizedText, _dynamicValues);
                }
                catch (System.FormatException)
                {
                    Debug.LogWarning($"Format error in localized text: {localizationKey}");
                }
            }
        
            _textComponent.text = localizedText;
        
            // Автоматическая подстройка шрифта под язык
            AdjustFontForLanguage();
        }
    
        private static void AdjustFontForLanguage()
        {
            var currentLang = LocalizationManager.Instance.GetCurrentLanguage();
        
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
}