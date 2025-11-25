using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LocalizationManager
{
    public class LanguageSelector : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown languageDropdown;
    
        private void Start()
        {
            InitializeDropdown();
        
            if (languageDropdown != null)
            {
                languageDropdown.onValueChanged.AddListener(OnLanguageSelected);
            }
        }
    
        private void InitializeDropdown()
        {
            if (languageDropdown == null) return;
        
            languageDropdown.ClearOptions();
        
            var languages = LocalizationManager.Instance.GetAvailableLanguages();
            var options = new List<TMP_Dropdown.OptionData>();
            var currentIndex = 0;
        
            for (var i = 0; i < languages.Count; i++)
            {
                var displayName = LocalizationManager.GetLanguageDisplayName(languages[i].localizationCode);
                options.Add(new TMP_Dropdown.OptionData(displayName));
                if (languages[i].localizationCode == LocalizationManager.Instance.GetCurrentLanguage())
                {
                    currentIndex = i;
                }
            }
        
            languageDropdown.options = options;
            languageDropdown.value = currentIndex;
            languageDropdown.RefreshShownValue();
        }
    
        private void OnLanguageSelected(int index)
        {
            var languages = LocalizationManager.Instance.GetAvailableLanguages();
            if (index < languages.Count)
            {
                LocalizationManager.Instance.SetLanguage(languages[index].localizationCode);
            }
        }
    }
}