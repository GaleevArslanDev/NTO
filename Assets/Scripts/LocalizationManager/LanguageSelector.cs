using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
        int currentIndex = 0;
        
        for (int i = 0; i < languages.Count; i++)
        {
            string displayName = LocalizationManager.Instance.GetLanguageDisplayName(languages[i].LocalizationCode);
            options.Add(new TMP_Dropdown.OptionData(displayName));
            if (languages[i].LocalizationCode == LocalizationManager.Instance.GetCurrentLanguage())
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
            LocalizationManager.Instance.SetLanguage(languages[index].LocalizationCode);
        }
    }
}