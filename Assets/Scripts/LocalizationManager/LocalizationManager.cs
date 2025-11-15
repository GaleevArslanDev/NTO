using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Structs;
using UnityEngine;
using UnityEngine.SceneManagement;
using String = Structs.String;

// public class
// localization manager
public class LocalizationManager : MonoBehaviour
{
    // default localization code
    [Header("Parameters:")] [Tooltip("Localization Code (ru-Ru, en-US, ...)")]
    public string localizationCode;

    // player prefs key
    [Tooltip("PlayerPrefs Key")] [SerializeField]
    private string saveLocalizationCodeKey;

    // localisations
    [Tooltip("Available Localizations")] [SerializeField]
    private List<Localization> localizations;

    // strings
    [Tooltip("Loaded Strings For Selcted Localization")] [SerializeField]
    private List<String> strings;

    private void Awake()
    {
        // if player prefs was, get it
        if (PlayerPrefs.HasKey(saveLocalizationCodeKey))
            localizationCode = PlayerPrefs.GetString(saveLocalizationCodeKey);

        // if localization not exist, log error
        if (localizations.Exists(l => l.LocalizationCode == localizationCode) == false)
            throw new Exception(
                "Localization in localization list not found. Please, check this localization code: " +
                localizationCode + ". It isn`t correct!");

        // setting vars
        var localization = localizations.Find(l => l.LocalizationCode == localizationCode);
        var resourceName = "Values/" + localization.StringsFileName;
        var xml = Resources.Load<TextAsset>(resourceName);

        var doc_txt = "";

        // if xml var not set, check for mods
        if (xml)
        {
            doc_txt = xml.text;
        }
        else
        {
            throw new Exception("Localization file not found. Please, check this file: " + resourceName);
        }

        var document = XDocument.Parse(doc_txt);
        var elements = document.Root.Elements("string").ToList();

        // if elements not found, log error
        if (elements.Count == 0)
            throw new Exception("String tags are not found. Check \"Resources/" + resourceName +
                                "\" file again. (Examle of string tag: <string name=\"str_name\">String value</string>");

        foreach (var element in elements)
        {
            if (element.Attribute("name") == null)
                throw new Exception(
                    "Incorrect format of values. Please, check the format of \"string\" tags. They have to include name attribute. Example: <string name=\"str_name\">String value</string>");

            strings.Add(new String
            {
                Name = element.Attribute("name").Value,
                Value = element.Value
            });
        }
    }

    private void OnDestroy()
    {
        PlayerPrefs.SetString(saveLocalizationCodeKey, localizationCode);
    }

    private void OnApplicationPause(bool pause)
    {
        if (Application.platform == RuntimePlatform.Android)
            PlayerPrefs.SetString(saveLocalizationCodeKey, localizationCode);
    }

    public void SetLocalization(string code)
    {
        localizationCode = code;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public string GetLocalization()
    {
        return localizationCode;
    }

    public List<Localization> GetLocalizations()
    {
        return localizations;
    }

    public string GetValue(string name)
    {
        if (name == "") return "";

        var str = strings.Find(s => s.Name == name);

        return str.Value;
    }

    private string ReadTextFile(string filePath)
    {
        var inpStm = new StreamReader(filePath);
        return inpStm.ReadToEnd();
    }
}